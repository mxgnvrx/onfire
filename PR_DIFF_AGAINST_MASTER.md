diff --git a/Content.Goobstation.Server/Devil/CheatDeath/CheatDeathSystem.cs b/Content.Goobstation.Server/Devil/CheatDeath/CheatDeathSystem.cs
index ecf7786898..892fd820b5 100644
--- a/Content.Goobstation.Server/Devil/CheatDeath/CheatDeathSystem.cs
+++ b/Content.Goobstation.Server/Devil/CheatDeath/CheatDeathSystem.cs
@@ -1,5 +1,6 @@
 // SPDX-License-Identifier: AGPL-3.0-or-later
 
+using Content.Goobstation.Common.Body;
 using Content.Goobstation.Common.DelayedDeath;
 using Content.Goobstation.Shared.CheatDeath;
 using Content.Goobstation.Shared.Devour.Events;
@@ -36,6 +37,7 @@ public override void Initialize()
         SubscribeLocalEvent<CheatDeathComponent, ExaminedEvent>(OnExamined);
 
         SubscribeLocalEvent<CheatDeathComponent, DelayedDeathEvent>(OnDelayedDeath);
+        SubscribeLocalEvent<CheatDeathComponent, BeforeBrainRemovedEvent>(OnBrainRemoveAttempt); // Arcane
     }
 
     private void OnInit(Entity<CheatDeathComponent> ent, ref MapInitEvent args) =>
@@ -68,6 +70,11 @@ private void OnDelayedDeath(Entity<CheatDeathComponent> ent, ref DelayedDeathEve
             RemCompDeferred(ent.Owner, ent.Comp);
     }
 
+    // Arcane-Start
+    // Losing the head would move the mind into it, away from the body that has the revive action.
+    private void OnBrainRemoveAttempt(Entity<CheatDeathComponent> ent, ref BeforeBrainRemovedEvent args) =>
+        args.Blocked = true;
+    // Arcane-End
 
     private void OnDeathCheatAttempt(Entity<CheatDeathComponent> ent, ref CheatDeathEvent args)
     {
diff --git a/Content.Goobstation.Server/Shadowling/Systems/ShadowlingThrallSystem.cs b/Content.Goobstation.Server/Shadowling/Systems/ShadowlingThrallSystem.cs
index a79910a33e..14ab197ef1 100644
--- a/Content.Goobstation.Server/Shadowling/Systems/ShadowlingThrallSystem.cs
+++ b/Content.Goobstation.Server/Shadowling/Systems/ShadowlingThrallSystem.cs
@@ -51,7 +51,8 @@ private void OnRemove(EntityUid uid, ThrallComponent component, ComponentShutdow
         if (_mind.TryGetMind(uid, out var mindId, out _))
             _roles.MindRemoveRole<ShadowlingRoleComponent>(mindId);
 
-        RemComp<NightVisionComponent>(uid);
+        if (!component.InnateNightVision) // Arcane
+            RemComp<NightVisionComponent>(uid); // Arcane-Edit
         RemComp<ThrallGuiseComponent>(uid);
         RemComp<LesserShadowlingComponent>(uid);
 
diff --git a/Content.Goobstation.Shared/Shadowling/Components/ThrallComponent.cs b/Content.Goobstation.Shared/Shadowling/Components/ThrallComponent.cs
index f7f0a800fb..4b82e98dfc 100644
--- a/Content.Goobstation.Shared/Shadowling/Components/ThrallComponent.cs
+++ b/Content.Goobstation.Shared/Shadowling/Components/ThrallComponent.cs
@@ -14,6 +14,11 @@ public sealed partial class ThrallComponent : Component
     [ViewVariables]
     public EntityUid? Converter;
 
+    // Arcane-Start
+    [ViewVariables]
+    public bool InnateNightVision;
+    // Arcane-End
+
     [DataField]
     public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "ThrallFaction";
 
diff --git a/Content.Goobstation.Shared/Shadowling/Systems/Abilities/Ascension/ShadowlingHypnosisSystem.cs b/Content.Goobstation.Shared/Shadowling/Systems/Abilities/Ascension/ShadowlingHypnosisSystem.cs
index 5df8621e9b..e44896a7d3 100644
--- a/Content.Goobstation.Shared/Shadowling/Systems/Abilities/Ascension/ShadowlingHypnosisSystem.cs
+++ b/Content.Goobstation.Shared/Shadowling/Systems/Abilities/Ascension/ShadowlingHypnosisSystem.cs
@@ -15,7 +15,8 @@ namespace Content.Goobstation.Shared.Shadowling.Systems.Abilities.Ascension;
 public sealed class ShadowlingHypnosisSystem : EntitySystem
 {
     [Dependency] private readonly SharedActionsSystem _actions = default!;
-    [Dependency] private readonly IPrototypeManager _proto = default!;
+    // [Dependency] private readonly IPrototypeManager _proto = default!; // Arcane-Edit
+    [Dependency] private readonly SharedShadowlingSystem _shadowling = default!; // Arcane
 
     public override void Initialize()
     {
@@ -41,8 +42,11 @@ private void OnHypnosis(EntityUid uid, ShadowlingHypnosisComponent component, Hy
             || HasComp<ShadowlingComponent>(target))
             return;
 
-        var comps = _proto.Index(component.HypnosisComponents);
-        EntityManager.AddComponents(target, comps);
+        // Arcane-Edit-Start
+        // var comps = _proto.Index(component.HypnosisComponents);
+        // EntityManager.AddComponents(target, comps);
+        // Arcane-Edit-End
+        _shadowling.AddThrallComponents(target, component.HypnosisComponents); // Arcane
         args.Handled = true;
     }
 }
diff --git a/Content.Goobstation.Shared/Shadowling/Systems/SharedShadowlingSystem.cs b/Content.Goobstation.Shared/Shadowling/Systems/SharedShadowlingSystem.cs
index 799aefbf4c..3567ed500c 100644
--- a/Content.Goobstation.Shared/Shadowling/Systems/SharedShadowlingSystem.cs
+++ b/Content.Goobstation.Shared/Shadowling/Systems/SharedShadowlingSystem.cs
@@ -3,6 +3,7 @@
 using Content.Goobstation.Shared.LightDetection.Components;
 using Content.Goobstation.Shared.LightDetection.Systems;
 using Content.Goobstation.Shared.Mindcontrol;
+using Content.Goobstation.Shared.Overlays;
 using Content.Goobstation.Shared.Shadowling.Components;
 using Content.Shared._Starlight.CollectiveMind;
 using Content.Shared.Actions;
@@ -223,6 +224,18 @@ public bool CanGlare(EntityUid target)
                && !HasComp<ThrallComponent>(target);
     }
 
+    // Arcane-Start
+    // removeExisting would reset the caller's ThrallComponent and a species' own night vision.
+    public void AddThrallComponents(EntityUid target, EntProtoId components)
+    {
+        var innateNightVision = HasComp<NightVisionComponent>(target);
+        EntityManager.AddComponents(target, _protoMan.Index(components), removeExisting: false);
+
+        if (TryComp<ThrallComponent>(target, out var thrall))
+            thrall.InnateNightVision = innateNightVision;
+    }
+    // Arcane-End
+
     public void DoEnthrall(EntityUid uid, EntProtoId components, SimpleDoAfterEvent args)
     {
         if (args.Cancelled
@@ -232,14 +245,28 @@ public void DoEnthrall(EntityUid uid, EntProtoId components, SimpleDoAfterEvent
 
         var target = args.Target.Value;
 
+        // Arcane-Start
+        if (!CanEnthrall(uid, target))
+            return;
+        // Arcane-End
+
         var thrall = EnsureComp<ThrallComponent>(target);
         thrall.Converter = uid;
-        var comps = _protoMan.Index(components);
-        EntityManager.AddComponents(target, comps);
+        // Arcane-Edit-Start
+        // var comps = _protoMan.Index(components);
+        // EntityManager.AddComponents(target, comps);
+        // Arcane-Edit-End
+        AddThrallComponents(target, components); // Arcane
 
         if (TryComp<ShadowlingComponent>(uid, out var sling))
             sling.Thralls.Add(target);
 
+        // Arcane-Start
+        // Upstream got this from re-adding ThrallComponent, which AddThrallComponents no longer does.
+        if (sling != null && TryComp<LightDetectionDamageComponent>(uid, out var lightDet))
+            _lightDamage.AddResistance((uid, lightDet), -sling.LightResistanceModifier);
+        // Arcane-End
+
         _audio.PlayPredicted(
             new SoundPathSpecifier("/Audio/Items/Defib/defib_zap.ogg"),
             target,
diff --git a/Content.Server/_Shitmed/Body/Systems/EyesSystem.cs b/Content.Server/_Shitmed/Body/Systems/EyesSystem.cs
index e681d07221..9d19d94db1 100644
--- a/Content.Server/_Shitmed/Body/Systems/EyesSystem.cs
+++ b/Content.Server/_Shitmed/Body/Systems/EyesSystem.cs
@@ -8,6 +8,7 @@
 using Content.Shared.Eye.Blinding.Components;
 using Content.Shared.Eye.Blinding.Systems;
 using Robust.Shared.Containers;
+using Robust.Shared.Prototypes;
 
 namespace Content.Server._Shitmed.Body.Systems
 {
@@ -16,6 +17,10 @@ public sealed class EyesSystem : EntitySystem
         [Dependency] private readonly IEntityManager _entityManager = default!;
         [Dependency] private readonly BlindableSystem _blindableSystem = default!;
         [Dependency] private readonly BodySystem _bodySystem = default!;
+        // Arcane-Start
+        [Dependency] private readonly IComponentFactory _componentFactory = default!;
+        [Dependency] private readonly IPrototypeManager _proto = default!;
+        // Arcane-End
 
         public override void Initialize()
         {
@@ -42,9 +47,28 @@ private void CheckMissingEyes(EntityUid body, EntityUid eye)
             if (!hasOtherEyes
                 && HasComp<EyesComponent>(eye)
                 && TryComp(body, out BlindableComponent? blindable))
-                _blindableSystem.SetEyeDamage((body, blindable), blindable.MaxDamage);
+                _blindableSystem.SetEyeDamage((body, blindable), HasInnateEyes(bodyComp) ? blindable.MaxDamage : blindable.MinDamage); // Arcane-Edit
         }
 
+        // Arcane-Start
+        private bool HasInnateEyes(BodyComponent? body)
+        {
+            if (body?.Prototype is not { } bodyProto)
+                return true;
+
+            foreach (var slot in _proto.Index(bodyProto).Slots.Values)
+            {
+                foreach (var organ in slot.Organs.Values)
+                {
+                    if (_proto.Index<EntityPrototype>(organ).TryGetComponent<EyesComponent>(out _, _componentFactory))
+                        return true;
+                }
+            }
+
+            return false;
+        }
+        // Arcane-End
+
         // Too much shit would break if I were to nuke blindablecomponent rn. Guess we shitcoding this one.
         private void OnOrganIntegrityChanged(EntityUid uid, EyesComponent component, OrganIntegrityChangedEvent args)
         {
diff --git a/Content.Shared/Body/Systems/SharedBodySystem.Body.cs b/Content.Shared/Body/Systems/SharedBodySystem.Body.cs
index 080f0d14ba..63ba407c6a 100644
--- a/Content.Shared/Body/Systems/SharedBodySystem.Body.cs
+++ b/Content.Shared/Body/Systems/SharedBodySystem.Body.cs
@@ -2,6 +2,7 @@
 
 using System.Linq;
 using System.Numerics;
+using Content.Shared._Arcane.Body.Systems;
 using Content.Shared.Body.Components;
 using Content.Shared.Body.Organ;
 using Content.Shared.Body.Part;
@@ -60,6 +61,7 @@ public partial class SharedBodySystem
     [Dependency] private readonly TraumaSystem _trauma = default!;
     [Dependency] private readonly IConfigurationManager _cfg = default!;
     // Shitmed Change End
+    [Dependency] private readonly LostPartMarkingsSystem _lostPartMarkings = default!; // Arcane
 
     private const float GibletLaunchImpulse = 8;
     private const float GibletLaunchImpulseVariance = 3;
@@ -643,6 +645,7 @@ public void RestoreBody(Entity<BodyComponent?> entity)
                         Dirty(childPart, childPartComponent);
 
                         Containers.Insert(childPart, container);
+                        _lostPartMarkings.RestoreMarkings(ent, (childPart, childPartComponent)); // Arcane
 
                         SetupOrgans((childPart, childPartComponent), connectionSlot.Organs);
                     }
@@ -669,6 +672,7 @@ public void RestoreBody(Entity<BodyComponent?> entity)
 
                     container = Containers.GetContainer(parentEntity, GetPartSlotContainerId(connection));
                     Containers.Insert(childPart, container);
+                    _lostPartMarkings.RestoreMarkings(ent, (childPart, childPartComponent)); // Arcane
 
                     SetupOrgans((childPart, childPartComponent), connectionSlot.Organs);
                 }
diff --git a/Content.Shared/_Arcane/Body/Components/LostPartMarkingsComponent.cs b/Content.Shared/_Arcane/Body/Components/LostPartMarkingsComponent.cs
new file mode 100644
index 0000000000..a10458e5dc
--- /dev/null
+++ b/Content.Shared/_Arcane/Body/Components/LostPartMarkingsComponent.cs
@@ -0,0 +1,13 @@
+// SPDX-License-Identifier: AGPL-3.0-or-later
+
+using Content.Shared.Humanoid;
+using Content.Shared.Humanoid.Markings;
+
+namespace Content.Shared._Arcane.Body.Components;
+
+[RegisterComponent]
+public sealed partial class LostPartMarkingsComponent : Component
+{
+    [ViewVariables]
+    public Dictionary<string, Dictionary<HumanoidVisualLayers, List<Marking>>> Markings = new();
+}
diff --git a/Content.Shared/_Arcane/Body/Systems/LostPartMarkingsSystem.cs b/Content.Shared/_Arcane/Body/Systems/LostPartMarkingsSystem.cs
new file mode 100644
index 0000000000..a66a0c2fd1
--- /dev/null
+++ b/Content.Shared/_Arcane/Body/Systems/LostPartMarkingsSystem.cs
@@ -0,0 +1,111 @@
+// SPDX-License-Identifier: AGPL-3.0-or-later
+
+using System.Linq;
+using Content.Shared._Arcane.Body.Components;
+using Content.Shared._Shitmed.Body.Part;
+using Content.Shared.Body.Components;
+using Content.Shared.Body.Part;
+using Content.Shared.Body.Systems;
+using Content.Shared.Humanoid;
+using Content.Shared.Humanoid.Markings;
+using Robust.Shared.Containers;
+using Robust.Shared.Network;
+using Robust.Shared.Prototypes;
+using Robust.Shared.Utility;
+
+namespace Content.Shared._Arcane.Body.Systems;
+
+public sealed class LostPartMarkingsSystem : EntitySystem
+{
+    [Dependency] private readonly IComponentFactory _componentFactory = default!;
+    [Dependency] private readonly SharedContainerSystem _container = default!;
+    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoid = default!;
+    [Dependency] private readonly INetManager _net = default!;
+    [Dependency] private readonly IPrototypeManager _proto = default!;
+
+    public override void Initialize()
+    {
+        base.Initialize();
+
+        // The body drops the part's markings, so read them first while it still knows which are forced.
+        SubscribeLocalEvent<HumanoidAppearanceComponent, BodyPartRemovedEvent>(OnBodyPartRemoved, before: [typeof(SharedBodySystem)]);
+    }
+
+    private void OnBodyPartRemoved(Entity<HumanoidAppearanceComponent> ent, ref BodyPartRemovedEvent args)
+    {
+        if (!_net.IsServer || TerminatingOrDeleted(ent))
+            return;
+
+        if (TryComp<LostPartMarkingsComponent>(ent, out var lost))
+            lost.Markings.Remove(args.Slot);
+
+        if (!TryComp<BodyPartAppearanceComponent>(args.Part, out var appearance))
+            return;
+
+        var markings = new Dictionary<HumanoidVisualLayers, List<Marking>>();
+        foreach (var (layer, list) in appearance.Markings)
+        {
+            var category = MarkingCategoriesConversion.FromHumanoidVisualLayers(layer);
+            foreach (var marking in list)
+            {
+                if (ent.Comp.MarkingSet.TryGetMarking(category, marking.MarkingId, out var bodyMarking))
+                    markings.GetOrNew(layer).Add(new Marking(bodyMarking));
+            }
+        }
+
+        if (markings.Count > 0)
+            EnsureComp<LostPartMarkingsComponent>(ent).Markings[args.Slot] = markings;
+    }
+
+    public void RestoreMarkings(Entity<HumanoidAppearanceComponent?> body, Entity<BodyPartComponent> part)
+    {
+        if (!Resolve(body, ref body.Comp, false))
+            return;
+
+        // Losing a part hides all its sublayers, but attaching it shows only its own layer.
+        if (part.Comp.ToHumanoidLayers() is { } partLayer)
+        {
+            var partLayers = GetPartLayers(body);
+            var sublayers = HumanoidVisualLayersExtension.Sublayers(partLayer).Where(layer => !partLayers.Contains(layer));
+            _humanoid.SetLayersVisibility(body, sublayers, true);
+        }
+
+        if (!TryComp<LostPartMarkingsComponent>(body, out var lost)
+            || !_container.TryGetContainingContainer(part.Owner, out var slot)
+            || !lost.Markings.Remove(slot.ID, out var markings)
+            || !TryComp<BodyPartAppearanceComponent>(part, out var appearance))
+            return;
+
+        appearance.Markings = markings;
+        Dirty(part, appearance);
+
+        foreach (var (layer, list) in markings)
+        {
+            var category = MarkingCategoriesConversion.FromHumanoidVisualLayers(layer);
+            foreach (var marking in list)
+            {
+                if (!body.Comp.MarkingSet.TryGetMarking(category, marking.MarkingId, out _))
+                    _humanoid.AddMarking(body, marking.MarkingId, marking.MarkingColors, false, marking.Forced, body.Comp);
+            }
+        }
+
+        Dirty(body.Owner, body.Comp);
+    }
+
+    private HashSet<HumanoidVisualLayers> GetPartLayers(EntityUid body)
+    {
+        var layers = new HashSet<HumanoidVisualLayers>();
+        if (!TryComp<BodyComponent>(body, out var bodyComp) || bodyComp.Prototype is not { } bodyProto)
+            return layers;
+
+        foreach (var slot in _proto.Index(bodyProto).Slots.Values)
+        {
+            if (slot.Part is { } partProto
+                && _proto.Index(partProto).TryGetComponent<BodyPartComponent>(out var part, _componentFactory)
+                && part.ToHumanoidLayers() is { } layer)
+                layers.Add(layer);
+        }
+
+        return layers;
+    }
+}
diff --git a/Content.Shared/_Arcane/Slime/SharedSlimeRegrowSystem.cs b/Content.Shared/_Arcane/Slime/SharedSlimeRegrowSystem.cs
index 1239afa406..6129969de3 100644
--- a/Content.Shared/_Arcane/Slime/SharedSlimeRegrowSystem.cs
+++ b/Content.Shared/_Arcane/Slime/SharedSlimeRegrowSystem.cs
@@ -1,16 +1,13 @@
-using System.Linq;
 using System.Numerics;
 using Content.Shared.Actions;
 using Content.Shared.Body.Components;
 using Content.Shared.Body.Part;
 using Content.Shared.Body.Prototypes;
 using Content.Shared.Body.Systems;
-using Content.Shared.Humanoid;
-using Content.Shared.Humanoid.Markings;
-using Content.Shared._Shitmed.Body.Part;
 using Content.Shared.Nutrition.Components;
 using Content.Shared.Nutrition.EntitySystems;
 using Content.Shared.Popups;
+using Content.Shared._Arcane.Body.Systems;
 using Content.Shared._Shitmed.Medical.Surgery.Traumas.Systems;
 using Content.Shared._Shitmed.Medical.Surgery.Wounds.Systems;
 using Content.Shared._Shitmed.Targeting;
@@ -38,10 +35,10 @@ public abstract partial class SharedSlimeRegrowSystem : EntitySystem
     [Dependency] private readonly IComponentFactory _componentFactory = default!;
     [Dependency] private readonly SharedContainerSystem _container = default!;
     [Dependency] private readonly HungerSystem _hunger = default!;
+    [Dependency] private readonly LostPartMarkingsSystem _lostPartMarkings = default!;
     [Dependency] private readonly INetManager _net = default!;
     [Dependency] private readonly SharedPopupSystem _popup = default!;
     [Dependency] private readonly IPrototypeManager _proto = default!;
-    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoid = default!;
     [Dependency] private readonly IRobustRandom _random = default!;
     [Dependency] private readonly ThirstSystem _thirst = default!;
     [Dependency] private readonly TraumaSystem _trauma = default!;
@@ -54,32 +51,6 @@ public override void Initialize()
         SubscribeLocalEvent<SlimeRegrowComponent, MapInitEvent>(OnMapInit);
         SubscribeLocalEvent<SlimeRegrowComponent, ComponentShutdown>(OnShutdown);
         SubscribeLocalEvent<SlimeRegrowComponent, SlimeRegrowLimbEvent>(OnSlimeRegrowLimb);
-        SubscribeLocalEvent<SlimeRegrowComponent, BodyPartRemovedEvent>(OnBodyPartRemoved);
-        SubscribeLocalEvent<SlimeRegrowComponent, BodyPartAddedEvent>(OnBodyPartAdded);
-    }
-
-    // The body drops a lost part's markings, so keep them for the part regrown into that slot.
-    private void OnBodyPartRemoved(Entity<SlimeRegrowComponent> ent, ref BodyPartRemovedEvent args)
-    {
-        if (!_net.IsServer)
-            return;
-
-        ent.Comp.LostPartMarkings.Remove(args.Slot);
-
-        if (!TryComp<BodyPartAppearanceComponent>(args.Part, out var appearance)
-            || appearance.Markings.Count == 0)
-            return;
-
-        ent.Comp.LostPartMarkings[args.Slot] = appearance.Markings.ToDictionary(
-            pair => pair.Key,
-            pair => pair.Value.Select(m => new Marking(m.MarkingId, m.MarkingColors.ToList())).ToList());
-    }
-
-    // A part attached any other way (e.g. surgery) brings its own markings.
-    private void OnBodyPartAdded(Entity<SlimeRegrowComponent> ent, ref BodyPartAddedEvent args)
-    {
-        if (_net.IsServer)
-            ent.Comp.LostPartMarkings.Remove(args.Slot);
     }
 
     private void OnMapInit(Entity<SlimeRegrowComponent> ent, ref MapInitEvent args)
@@ -215,9 +186,6 @@ private bool TryGrowLimb(Entity<SlimeRegrowComponent> ent, EntityUid parentId, s
         var childPart = Spawn(partId, new EntityCoordinates(parentId, Vector2.Zero));
         var childPartComp = Comp<BodyPartComponent>(childPart);
 
-        // Taken before attaching, since attaching clears the slot's stored markings.
-        ent.Comp.LostPartMarkings.Remove(SharedBodySystem.GetPartSlotContainerId(slotId), out var markings);
-
         if (!_body.TryCreatePartSlotAndAttach(parentId, slotId, childPart, childPartComp.PartType, childPartComp.Symmetry))
         {
             Log.Error($"Failed to regrow part {partId} into slot {slotId} of {ToPrettyString(parentId)}");
@@ -225,32 +193,7 @@ private bool TryGrowLimb(Entity<SlimeRegrowComponent> ent, EntityUid parentId, s
             return false;
         }
 
-        if (TryComp<HumanoidAppearanceComponent>(ent, out var humanoid))
-        {
-            // Losing the head hides all of its sublayers, but attaching it only shows its own layer,
-            // so markings it never recorded (e.g. hair) would otherwise stay hidden.
-            // Limb sublayers are hands and feet: separate parts that have not regrown yet.
-            if (childPartComp.ToHumanoidLayers() is { } partLayer)
-            {
-                var sublayers = HumanoidVisualLayersExtension.Sublayers(partLayer).Where(layer => layer != partLayer);
-                _humanoid.SetLayersVisibility((ent, humanoid), sublayers, childPartComp.PartType == BodyPartType.Head);
-            }
-
-            // Applied after attaching, so the part appearance first picks up its base layer from the body.
-            if (markings != null && TryComp<BodyPartAppearanceComponent>(childPart, out var appearance))
-            {
-                appearance.Markings = markings;
-                Dirty(childPart, appearance);
-
-                foreach (var list in markings.Values)
-                {
-                    foreach (var marking in list)
-                        _humanoid.AddMarking(ent, marking.MarkingId, marking.MarkingColors, false, true, humanoid);
-                }
-
-                Dirty(ent, humanoid);
-            }
-        }
+        _lostPartMarkings.RestoreMarkings(ent.Owner, (childPart, childPartComp));
 
         // Regrowing a limb also heals the stump (Dismemberment trauma) its removal left behind,
         // otherwise it would still need surgery to clean up before the socket is usable again.
diff --git a/Content.Shared/_Arcane/Slime/SlimeRegrowComponent.cs b/Content.Shared/_Arcane/Slime/SlimeRegrowComponent.cs
index e75e29d177..7ad5849477 100644
--- a/Content.Shared/_Arcane/Slime/SlimeRegrowComponent.cs
+++ b/Content.Shared/_Arcane/Slime/SlimeRegrowComponent.cs
@@ -1,7 +1,5 @@
 // SPDX-License-Identifier: AGPL-3.0-or-later
 
-using Content.Shared.Humanoid;
-using Content.Shared.Humanoid.Markings;
 using Robust.Shared.Audio;
 using Robust.Shared.GameStates;
 using Robust.Shared.Prototypes;
@@ -69,7 +67,4 @@ public sealed partial class SlimeRegrowComponent : Component
     /// </summary>
     [DataField, AutoNetworkedField]
     public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Voice/Slime/slime_squish.ogg");
-
-    [ViewVariables]
-    public Dictionary<string, Dictionary<HumanoidVisualLayers, List<Marking>>> LostPartMarkings = new();
 }
diff --git a/Content.Shared/_Arcane/Surgery/SharedSurgeryToolSystem.cs b/Content.Shared/_Arcane/Surgery/SharedSurgeryToolSystem.cs
index a512b527d0..7cee119acb 100644
--- a/Content.Shared/_Arcane/Surgery/SharedSurgeryToolSystem.cs
+++ b/Content.Shared/_Arcane/Surgery/SharedSurgeryToolSystem.cs
@@ -16,7 +16,10 @@
 using Content.Shared.Buckle;
 using Content.Shared.Hands.EntitySystems;
 using Content.Shared.Interaction;
+using Content.Shared.Kitchen.Components;
 using Content.Shared.Medical.Healing;
+using Content.Shared.Mobs.Systems;
+using Content.Shared.Nutrition.Components;
 using Content.Shared.Popups;
 using Robust.Shared.Configuration;
 using Robust.Shared.Containers;
@@ -39,6 +42,7 @@ public abstract class SharedSurgeryToolSystem : EntitySystem
     [Dependency] private readonly SharedContainerSystem _container = default!;
     [Dependency] private readonly SharedHandsSystem _hands = default!;
     [Dependency] private readonly SharedInteractionSystem _interaction = default!;
+    [Dependency] private readonly MobStateSystem _mobState = default!;
     [Dependency] private readonly SharedPopupSystem _popup = default!;
     [Dependency] private readonly SharedSurgerySystem _surgery = default!;
 
@@ -63,6 +67,7 @@ private void OnInteractUsing(Entity<SurgeryTargetComponent> ent, ref InteractUsi
         var tool = args.Used;
         if (args.Handled
             || IsOffering(user)
+            || IsButchering(ent, tool)
             || !CanOperate(ent, user, IsSurgicalTool(tool))
             || !TryGetTargetPart(ent, user, out var part, out var missing))
             return;
@@ -592,4 +597,12 @@ private bool IsOffering(EntityUid user)
     {
         return TryComp(user, out OfferItemComponent? offer) && offer.IsInOfferMode;
     }
+
+    private bool IsButchering(EntityUid body, EntityUid tool)
+    {
+        return HasComp<SharpComponent>(tool)
+            && TryComp(body, out ButcherableComponent? butcherable)
+            && butcherable.Type == ButcheringType.Knife
+            && _mobState.IsDead(body);
+    }
 }
