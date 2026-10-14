# PR DIFF AGAINST MASTER
```diff
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
```

# FULL CONTENT OF CHANGED FILES

## File: Content.Goobstation.Server/Devil/CheatDeath/CheatDeathSystem.cs
```
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Body;
using Content.Goobstation.Common.DelayedDeath;
using Content.Goobstation.Shared.CheatDeath;
using Content.Goobstation.Shared.Devour.Events;
using Content.Server.Actions;
using Content.Server.Administration.Systems;
using Content.Server.Jittering;
using Content.Shared.Administration.Systems;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Traits.Assorted;

namespace Content.Goobstation.Server.Devil.CheatDeath;

public sealed partial class CheatDeathSystem : EntitySystem
{
    [Dependency] private readonly RejuvenateSystem _rejuvenateSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly ActionsSystem _actionsSystem = default!;
    [Dependency] private readonly JitteringSystem _jitter = default!;
    [Dependency] private readonly MobThresholdSystem _thresholdSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CheatDeathComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<CheatDeathComponent, ComponentRemove>(OnRemoval);

        SubscribeLocalEvent<CheatDeathComponent, CheatDeathEvent>(OnDeathCheatAttempt);

        SubscribeLocalEvent<CheatDeathComponent, ExaminedEvent>(OnExamined);

        SubscribeLocalEvent<CheatDeathComponent, DelayedDeathEvent>(OnDelayedDeath);
        SubscribeLocalEvent<CheatDeathComponent, BeforeBrainRemovedEvent>(OnBrainRemoveAttempt); // Arcane
    }

    private void OnInit(Entity<CheatDeathComponent> ent, ref MapInitEvent args) =>
        _actionsSystem.AddAction(ent, ref ent.Comp.ActionEntity, ent.Comp.ActionCheatDeath);

    private void OnRemoval(Entity<CheatDeathComponent> ent, ref ComponentRemove args) =>
        _actionsSystem.RemoveAction(ent.Owner, ent.Comp.ActionEntity);

    private void OnExamined(Entity<CheatDeathComponent> ent, ref ExaminedEvent args)
    {
        if (args.Examined != args.Examiner)
            return;

        if (ent.Comp.InfiniteRevives)
        {
            var unlimited = Loc.GetString("cheat-death-component-remaining-revives-unlimited");
            args.PushMarkup(unlimited);

            return;
        }

        var remaining = Loc.GetString("cheat-death-component-remaining-revives", ("amount", ent.Comp.ReviveAmount));
        args.PushMarkup(remaining);

    }

    private void OnDelayedDeath(Entity<CheatDeathComponent> ent, ref DelayedDeathEvent args)
    {
        if (args.PreventRevive)
            RemCompDeferred(ent.Owner, ent.Comp);
    }

    // Arcane-Start
    // Losing the head would move the mind into it, away from the body that has the revive action.
    private void OnBrainRemoveAttempt(Entity<CheatDeathComponent> ent, ref BeforeBrainRemovedEvent args) =>
        args.Blocked = true;
    // Arcane-End

    private void OnDeathCheatAttempt(Entity<CheatDeathComponent> ent, ref CheatDeathEvent args)
    {
        if (args.Handled)
            return;

        if (!_mobStateSystem.IsDead(ent) && !ent.Comp.CanCheatStanding)
        {
            var failPopup = Loc.GetString("action-cheat-death-fail-not-dead");
            _popupSystem.PopupEntity(failPopup, ent, ent, PopupType.LargeCaution);

            return;
        }

        // check if we're allowed to revive
        var reviveEv = new BeforeSelfRevivalEvent(ent, "self-revive-fail");
        RaiseLocalEvent(ent, ref reviveEv);

        if (reviveEv.Cancelled)
            return;

        // If the entity is out of revives, or if they are unrevivable, return.
        if (ent.Comp.ReviveAmount <= 0 || HasComp<UnrevivableComponent>(ent))
        {
            var failPopup = Loc.GetString("action-cheat-death-fail-no-lives");
            _popupSystem.PopupEntity(failPopup, ent, ent, PopupType.LargeCaution);

            return;
        }

        // If the holy damage exceeds the crit state, do not allow revives.
        if (!TryComp<DamageableComponent>(ent, out var damageable)
            || !_thresholdSystem.TryGetIncapThreshold(ent, out var incapThreshold)
            || damageable.Damage.DamageDict["Holy"] >= incapThreshold)
        {
            var failPopup = Loc.GetString("action-cheat-death-holy-damage");
            _popupSystem.PopupEntity(failPopup, ent, ent, PopupType.LargeCaution);

            return;
        }

        // Show popup
        if (_mobStateSystem.IsDead(ent) && !ent.Comp.CanCheatStanding)
        {
            var popup = Loc.GetString("action-cheated-death-dead", ("name", Name(ent)));
            _popupSystem.PopupEntity(popup, ent, PopupType.LargeCaution);
        }
        else
        {
            var popup = Loc.GetString("action-cheated-death-alive", ("name", Name(ent)));
            _popupSystem.PopupEntity(popup, ent, PopupType.LargeCaution);
        }

        // Revive entity
        _rejuvenateSystem.PerformRejuvenate(ent);
        _jitter.DoJitter(ent, TimeSpan.FromSeconds(5), true);

        // Decrement remaining revives.
        if (!ent.Comp.InfiniteRevives)
            ent.Comp.ReviveAmount--;

        // remove comp if at zero
        if (ent.Comp.ReviveAmount <= 0 && !ent.Comp.InfiniteRevives)
            RemComp(ent.Owner, ent.Comp);

        args.Handled = true;
    }
}

```

## File: Content.Goobstation.Server/Shadowling/Systems/ShadowlingThrallSystem.cs
```
using Content.Goobstation.Shared.Devil;
using Content.Goobstation.Shared.Overlays;
using Content.Goobstation.Shared.Shadowling;
using Content.Goobstation.Shared.Shadowling.Components;
using Content.Goobstation.Shared.Shadowling.Components.Abilities.Thrall;
using Content.Server.Antag;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Shared._Starlight.CollectiveMind;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Server.Shadowling.Systems;

/// <summary>
/// This handles Thralls antag briefing and abilities
/// </summary>
public sealed class ShadowlingThrallSystem : EntitySystem
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly RoleSystem _roles = default!;
    [Dependency] private readonly ShadowlingSystem _shadowling = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThrallComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ThrallComponent, ComponentShutdown>(OnRemove);
        SubscribeLocalEvent<ThrallComponent, ExaminedEvent>(OnExamined);
    }

    public ProtoId<CollectiveMindPrototype> ShadowMind = "Shadowmind";
    private void OnStartup(EntityUid uid, ThrallComponent component, ComponentStartup args)
    {
        // antag stuff
        if (!_mind.TryGetMind(uid, out var mindId, out _))
            return;

        if (!_roles.MindHasRole<ShadowlingRoleComponent>(mindId))
            _roles.MindAddRole(mindId, "MindRoleThrall");

        EnsureComp<CollectiveMindComponent>(uid).Channels.Add(ShadowMind);

        _antag.SendBriefing(uid, Loc.GetString("thrall-role-greeting"), Color.MediumPurple, component.ThrallConverted);
    }

    private void OnRemove(EntityUid uid, ThrallComponent component, ComponentShutdown args)
    {
        if (_mind.TryGetMind(uid, out var mindId, out _))
            _roles.MindRemoveRole<ShadowlingRoleComponent>(mindId);

        if (!component.InnateNightVision) // Arcane
            RemComp<NightVisionComponent>(uid); // Arcane-Edit
        RemComp<ThrallGuiseComponent>(uid);
        RemComp<LesserShadowlingComponent>(uid);

        if (TryComp<CollectiveMindComponent>(uid, out var collective))
            collective.Channels.Remove(ShadowMind);

        if (component.Converter == null)
            return;

        // Adjust lightning resistance for shadowling
        var shadowling = component.Converter.Value;
        if (TryComp<ShadowlingComponent>(shadowling, out var shadowlingComp))
            _shadowling.OnThrallRemoved((shadowling, shadowlingComp));
    }

    private void OnExamined(EntityUid uid, ThrallComponent component, ExaminedEvent args)
    {
        if (HasComp<ShadowlingComponent>(args.Examiner)
            && component.Converter == args.Examiner)
            args.PushMarkup($"[color=red]{Loc.GetString("shadowling-thrall-examined")}[/color]"); // Indicates that it is your Thrall

        var ev = new IsEyesCoveredCheckEvent();
        RaiseLocalEvent(uid, ev);

        if (ev.IsEyesProtected)
            return;

        args.PushMarkup($"[color=pink]{Loc.GetString("shadowling-thrall-other-examined", ("target", Identity.Entity(uid, EntityManager)))}[/color]");
    }
}

```

## File: Content.Goobstation.Shared/Shadowling/Components/ThrallComponent.cs
```
using Content.Shared.StatusIcon;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.Shadowling.Components;

/// <summary>
/// This is used for marking Thralls and storing their icons
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ThrallComponent : Component
{
    [ViewVariables]
    public EntityUid? Converter;

    // Arcane-Start
    [ViewVariables]
    public bool InnateNightVision;
    // Arcane-End

    [DataField]
    public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "ThrallFaction";

    [DataField]
    public SoundSpecifier? ThrallConverted = new SoundPathSpecifier("/Audio/_EinsteinEngines/Shadowling/thrall.ogg");
}

```

## File: Content.Goobstation.Shared/Shadowling/Systems/Abilities/Ascension/ShadowlingHypnosisSystem.cs
```
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Shadowling.Components;
using Content.Goobstation.Shared.Shadowling.Components.Abilities.Ascension;
using Content.Shared.Actions;
using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.Shadowling.Systems.Abilities.Ascension;

/// <summary>
/// This handles Hypnosis.
/// Instant Thrall from afar!
/// </summary>
public sealed class ShadowlingHypnosisSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    // [Dependency] private readonly IPrototypeManager _proto = default!; // Arcane-Edit
    [Dependency] private readonly SharedShadowlingSystem _shadowling = default!; // Arcane

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShadowlingHypnosisComponent, HypnosisEvent>(OnHypnosis);
        SubscribeLocalEvent<ShadowlingHypnosisComponent, MapInitEvent>(OnStartup);
        SubscribeLocalEvent<ShadowlingHypnosisComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(Entity<ShadowlingHypnosisComponent> ent, ref MapInitEvent args)
        => _actions.AddAction(ent.Owner, ref ent.Comp.ActionEnt, ent.Comp.ActionId);

    private void OnShutdown(Entity<ShadowlingHypnosisComponent> ent, ref ComponentShutdown args)
        => _actions.RemoveAction(ent.Owner, ent.Comp.ActionEnt);

    private void OnHypnosis(EntityUid uid, ShadowlingHypnosisComponent component, HypnosisEvent args)
    {
        var target = args.Target;
        if (args.Handled
            || !HasComp<HumanoidAppearanceComponent>(target)
            || HasComp<ThrallComponent>(target)
            || HasComp<ShadowlingComponent>(target))
            return;

        // Arcane-Edit-Start
        // var comps = _proto.Index(component.HypnosisComponents);
        // EntityManager.AddComponents(target, comps);
        // Arcane-Edit-End
        _shadowling.AddThrallComponents(target, component.HypnosisComponents); // Arcane
        args.Handled = true;
    }
}

```

## File: Content.Goobstation.Shared/Shadowling/Systems/SharedShadowlingSystem.cs
```
using Content.Goobstation.Common.Conversion;
using Content.Goobstation.Shared.Changeling.Components;
using Content.Goobstation.Shared.LightDetection.Components;
using Content.Goobstation.Shared.LightDetection.Systems;
using Content.Goobstation.Shared.Mindcontrol;
using Content.Goobstation.Shared.Overlays;
using Content.Goobstation.Shared.Shadowling.Components;
using Content.Shared._Starlight.CollectiveMind;
using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Stunnable;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.Shadowling.Systems;

public abstract class SharedShadowlingSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly SharedLightDetectionDamageSystem _lightDamage = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MovementModStatusSystem _movementMod = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShadowlingComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<ShadowlingComponent, HatchEvent>(OnHatch);
        SubscribeLocalEvent<ShadowlingComponent, BeforeDamageChangedEvent>(BeforeDamageChanged);
        SubscribeLocalEvent<ShadowlingComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<ShadowlingComponent, DamageModifyEvent>(OnDamageModify);
        SubscribeLocalEvent<ShadowlingComponent, ExaminedEvent>(OnExamined);
    }

    #region Event Handlers

    private void OnMobStateChanged(EntityUid uid, ShadowlingComponent component, MobStateChangedEvent args)
    {
        // Remove all Thralls if shadowling is dead
        if (args.NewMobState is not (MobState.Dead or MobState.Invalid)
            || component.CurrentPhase == ShadowlingPhases.Ascension)
            return;

        foreach (var thrall in component.Thralls)
        {
            _popup.PopupEntity(Loc.GetString("shadowling-dead"), thrall, thrall, PopupType.LargeCaution);
            RemCompDeferred<ThrallComponent>(thrall);
        }

        var ev = new ShadowlingDeathEvent();
        RaiseLocalEvent(ev);
    }

    private void OnDamageModify(EntityUid uid, ShadowlingComponent component, DamageModifyEvent args)
    {
        if (args.Origin is not {} origin
            || !HasComp<ProjectileComponent>(origin))
            return;

        foreach (var (key,_) in args.Damage.DamageDict)
        {
            if (key == "Heat")
                args.Damage += component.HeatDamageProjectileModifier;
        }
    }

    public void OnThrallRemoved(Entity<ShadowlingComponent> ent)
    {
        if (!TryComp<LightDetectionDamageComponent>(ent, out var lightDet))
            return;

        _lightDamage.AddResistance((ent.Owner, lightDet), -ent.Comp.LightResistanceModifier);
    }

    public ProtoId<CollectiveMindPrototype> ShadowMind = "Shadowmind";
    private void OnInit(EntityUid uid, ShadowlingComponent component, ref MapInitEvent args)
    {
        _actions.AddAction(uid, ref component.ActionHatchEntity, component.ActionHatch);

        EnsureComp<CollectiveMindComponent>(uid).Channels.Add(ShadowMind);
    }

    private void OnHatch(Entity<ShadowlingComponent> ent, ref HatchEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        _actions.RemoveAction(ent.Owner, ent.Comp.ActionHatchEntity);

        StartHatchingProgress(ent);
    }

    protected virtual void StartHatchingProgress(Entity<ShadowlingComponent> ent) { }

    private void BeforeDamageChanged(EntityUid uid, ShadowlingComponent comp, BeforeDamageChangedEvent args)
    {
        // Can't take damage during hatching
        if (comp.IsHatching)
            args.Cancelled = true;
    }

    public void OnPhaseChanged(EntityUid uid, ShadowlingComponent component, ShadowlingPhases phase)
    {
        var defaultAbilities = _protoMan.Index(component.PostHatchComponents);
        switch (phase)
        {
            case ShadowlingPhases.PostHatch:
            {
                EntityManager.AddComponents(uid, defaultAbilities);
                _actions.RemoveAction(uid, component.ActionHatchEntity);
                break;
            }
            case ShadowlingPhases.Ascension:
            {
                // Remove all previous actions
                EntityManager.RemoveComponents(uid, defaultAbilities);
                EntityManager.RemoveComponents(uid, _protoMan.Index(component.ObtainableComponents));

                EntityManager.AddComponents(uid, _protoMan.Index(component.PostAscensionComponents));

                var ev = new ShadowlingAscendEvent(uid);
                RaiseLocalEvent(ev);
                break;
            }
            case ShadowlingPhases.FailedAscension:
            {
                // git gud bro :sob: :pray:
                EntityManager.RemoveComponents(uid, defaultAbilities);
                EntityManager.RemoveComponents(uid, _protoMan.Index(component.ObtainableComponents));

                // this is such a big L that even the code is losing and all variables are hardcoded.
                // upstreaming note - on god slowdowncomponent no longer exists so im straight up slowing the shadowlings for a DAY fuck it.
                _movementMod.TryUpdateMovementSpeedModDuration(uid, SharedStunSystem.StunId, TimeSpan.FromDays(1), 0.5f, 0.5f);
                _appearance.AddMarking(uid, "AbominationTorso");
                _appearance.AddMarking(uid, "AbominationHorns");

                // take another hardcoded variable
                _damageable.SetDamageModifierSetId(uid, "ShadowlingAbomination");
                break;
            }
        }
    }

    private void OnExamined(EntityUid uid, ShadowlingComponent comp, ExaminedEvent args)
    {
        if (args.Examiner != uid
            || !TryComp<LightDetectionDamageComponent>(uid, out var lightDet))
            return;

        args.PushMarkup(Loc.GetString("shadowling-examine-self", ("damage", lightDet.ResistanceModifier * lightDet.DamageToDeal.GetTotal())));
    }

    #endregion

    public bool CanEnthrall(EntityUid uid, EntityUid target)
    {
        if (HasComp<ShadowlingComponent>(target))
        {
            _popup.PopupPredicted(Loc.GetString("shadowling-enthrall-shadowling"), uid, uid, PopupType.SmallCaution);
            return false;
        }

        if (HasComp<ThrallComponent>(target))
        {
            _popup.PopupPredicted(Loc.GetString("shadowling-enthrall-already-thrall"), uid, uid, PopupType.SmallCaution);
            return false;
        }

        if (!TryComp<MindControllableComponent>(target, out var mindControllable) || mindControllable.ControlledBySomeone)
        {
            _popup.PopupPredicted(Loc.GetString("shadowling-enthrall-cant-be-controlled"), uid, uid, PopupType.SmallCaution);
            return false;
        }

        if (!TryComp<MindContainerComponent>(target, out var mind) || !mind.HasMind)
        {
            _popup.PopupPredicted(Loc.GetString("shadowling-enthrall-no-mind"), uid, uid, PopupType.SmallCaution);
            return false;
        }

        if (!HasComp<HumanoidAppearanceComponent>(target))
        {
            _popup.PopupPredicted(Loc.GetString("shadowling-enthrall-non-humanoid"), uid, uid, PopupType.SmallCaution);
            return false;
        }

        // Target needs to be alive
        if (!TryComp<MobStateComponent>(target, out var mobState)
            || !_mobStateSystem.IsCritical(target, mobState) && !_mobStateSystem.IsCritical(target, mobState))
            return true;

        _popup.PopupPredicted(Loc.GetString("shadowling-enthrall-dead"), uid, uid, PopupType.SmallCaution);
        return false;
    }

    public bool CanGlare(EntityUid target)
    {
        var convEv = new BeforeConversionEvent(target);
        RaiseLocalEvent(target, ref convEv, true);

        if (convEv.Blocked) // make all the shit below to use the event in the future tm
            return false;

        return HasComp<MobStateComponent>(target)
               && !HasComp<ShadowlingComponent>(target)
               && !HasComp<ThrallComponent>(target);
    }

    // Arcane-Start
    // removeExisting would reset the caller's ThrallComponent and a species' own night vision.
    public void AddThrallComponents(EntityUid target, EntProtoId components)
    {
        var innateNightVision = HasComp<NightVisionComponent>(target);
        EntityManager.AddComponents(target, _protoMan.Index(components), removeExisting: false);

        if (TryComp<ThrallComponent>(target, out var thrall))
            thrall.InnateNightVision = innateNightVision;
    }
    // Arcane-End

    public void DoEnthrall(EntityUid uid, EntProtoId components, SimpleDoAfterEvent args)
    {
        if (args.Cancelled
            || args.Handled
            || args.Target == null)
            return;

        var target = args.Target.Value;

        // Arcane-Start
        if (!CanEnthrall(uid, target))
            return;
        // Arcane-End

        var thrall = EnsureComp<ThrallComponent>(target);
        thrall.Converter = uid;
        // Arcane-Edit-Start
        // var comps = _protoMan.Index(components);
        // EntityManager.AddComponents(target, comps);
        // Arcane-Edit-End
        AddThrallComponents(target, components); // Arcane

        if (TryComp<ShadowlingComponent>(uid, out var sling))
            sling.Thralls.Add(target);

        // Arcane-Start
        // Upstream got this from re-adding ThrallComponent, which AddThrallComponents no longer does.
        if (sling != null && TryComp<LightDetectionDamageComponent>(uid, out var lightDet))
            _lightDamage.AddResistance((uid, lightDet), -sling.LightResistanceModifier);
        // Arcane-End

        _audio.PlayPredicted(
            new SoundPathSpecifier("/Audio/Items/Defib/defib_zap.ogg"),
            target,
            uid,
            AudioParams.Default);

        args.Handled = true;
    }
}

```

## File: Content.Server/_Shitmed/Body/Systems/EyesSystem.cs
```
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Body.Systems;
using Content.Shared._Shitmed.Body.Organ;
using Content.Shared._Shitmed.Medical.Surgery.Traumas;
using Content.Shared.Body.Components;
using Content.Shared.Body.Organ;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server._Shitmed.Body.Systems
{
    public sealed class EyesSystem : EntitySystem
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly BlindableSystem _blindableSystem = default!;
        [Dependency] private readonly BodySystem _bodySystem = default!;
        // Arcane-Start
        [Dependency] private readonly IComponentFactory _componentFactory = default!;
        [Dependency] private readonly IPrototypeManager _proto = default!;
        // Arcane-End

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<EyesComponent, OrganIntegrityChangedEvent>(OnOrganIntegrityChanged);
            SubscribeLocalEvent<EyesComponent, OrganEnabledEvent>(OnOrganEnabled);
            SubscribeLocalEvent<EyesComponent, OrganDisabledEvent>(OnOrganDisabled);
            SubscribeLocalEvent<EyesComponent, EntGotRemovedFromContainerMessage>(OnEyesRemoved);
        }

        private void CheckMissingEyes(EntityUid body, EntityUid eye)
        {
            if (TerminatingOrDeleted(body) || TerminatingOrDeleted(eye))
                return;

            var hasOtherEyes = false;

            if (TryComp<BodyComponent>(body, out var bodyComp))
                if (_bodySystem.TryGetBodyOrganEntityComps<EyesComponent>((body, bodyComp), out var eyes)
                    && eyes.Count > 1)
                    hasOtherEyes = true;

            if (!hasOtherEyes
                && HasComp<EyesComponent>(eye)
                && TryComp(body, out BlindableComponent? blindable))
                _blindableSystem.SetEyeDamage((body, blindable), HasInnateEyes(bodyComp) ? blindable.MaxDamage : blindable.MinDamage); // Arcane-Edit
        }

        // Arcane-Start
        private bool HasInnateEyes(BodyComponent? body)
        {
            if (body?.Prototype is not { } bodyProto)
                return true;

            foreach (var slot in _proto.Index(bodyProto).Slots.Values)
            {
                foreach (var organ in slot.Organs.Values)
                {
                    if (_proto.Index<EntityPrototype>(organ).TryGetComponent<EyesComponent>(out _, _componentFactory))
                        return true;
                }
            }

            return false;
        }
        // Arcane-End

        // Too much shit would break if I were to nuke blindablecomponent rn. Guess we shitcoding this one.
        private void OnOrganIntegrityChanged(EntityUid uid, EyesComponent component, OrganIntegrityChangedEvent args)
        {
            if (args.NewIntegrity <= 0
                || !TryComp(uid, out OrganComponent? organ)
                || !organ.Body.HasValue
                || !TryComp(organ.Body.Value, out BlindableComponent? blindable)
                || organ.OrganIntegrity <= 0
                || organ.IntegrityCap <= 0)
                return;

            
            // Omu: The expected input is scaled as a number between 12 and 3,
            // such that "blindness" occurs at 25% eye integrity.
            
            var blindnessSeverity = (int) ((organ.IntegrityCap - organ.OrganIntegrity) / (organ.IntegrityCap / 12)); // Omu
            _blindableSystem.SetEyeDamage((organ.Body.Value, blindable), blindnessSeverity); // Omu
        }

        private void OnOrganEnabled(EntityUid uid, EyesComponent component, OrganEnabledEvent args)
        {
            if (TerminatingOrDeleted(uid)
            || args.Organ.Comp.Body is not { Valid: true } body
            || !TryComp(body, out BlindableComponent? blindable))
                return;

            var lost = args.Organ.Comp.IntegrityCap > 0
                ? 1f - (float) (args.Organ.Comp.OrganIntegrity / args.Organ.Comp.IntegrityCap)
                : 1f;
            _blindableSystem.SetEyeDamage((body, blindable), (int) (blindable.MaxDamage * lost));
        }

        private void OnOrganDisabled(EntityUid uid, EyesComponent component, OrganDisabledEvent args)
        {
            if (TerminatingOrDeleted(uid)
            || args.Organ.Comp.Body is not { Valid: true } body)
                return;

            CheckMissingEyes(body, uid);
        }

        private void OnEyesRemoved(EntityUid uid, EyesComponent component, EntGotRemovedFromContainerMessage args)
        {
            if (TerminatingOrDeleted(uid)
                || !TryComp(args.Entity, out OrganComponent? organ)
                || !organ.Body.HasValue
                || !TryComp(organ.Body.Value, out BlindableComponent? blindable))
                return;

            CheckMissingEyes(organ.Body.Value, uid);
        }
    }
}

```

## File: Content.Shared/Body/Systems/SharedBodySystem.Body.cs
```
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using System.Numerics;
using Content.Shared._Arcane.Body.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Content.Shared.Body.Prototypes;
using Content.Shared.DragDrop;
using Content.Shared.Gibbing.Components;
using Content.Shared.Gibbing.Events;
using Content.Shared.Gibbing.Systems;
using Content.Shared.Inventory;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Utility;

// Shitmed Change
using Content.Shared._Shitmed.Body.Part;
using Content.Shared._Shitmed.CCVar;
using Content.Shared._Shitmed.Humanoid.Events;
using Content.Shared._Shitmed.Medical.Surgery;
using Content.Shared._Shitmed.Medical.Surgery.Wounds.Components;
using Content.Shared._Shitmed.Medical.Surgery.Wounds.Systems;
using Content.Shared._Shitmed.Medical.Surgery.Traumas.Systems;
using Content.Shared._Shitmed.Medical.Surgery.Traumas.Components;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Mobs.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Inventory.Events;
using Content.Shared.Pulling.Events;
using Content.Shared.Standing;
using Content.Shared.Rejuvenate;
using Content.Shared.Popups;
using Robust.Shared.Configuration;

namespace Content.Shared.Body.Systems;

public partial class SharedBodySystem
{
    /*
     * tl;dr of how bobby works
     * - BodyComponent uses a BodyPrototype as a template.
     * - On MapInit we spawn the root entity in the prototype and spawn all connections outwards from here
     * - Each "connection" is a body part (e.g. arm, hand, etc.) and each part can also contain organs.
     */

    //[Dependency] private readonly InventorySystem _inventory = default!; goob todo marty kill later prob.
    [Dependency] private readonly GibbingSystem _gibbingSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

    // Shitmed Change Start
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly ItemSlotsSystem _slots = default!;
    [Dependency] private readonly WoundSystem _woundSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TraumaSystem _trauma = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    // Shitmed Change End
    [Dependency] private readonly LostPartMarkingsSystem _lostPartMarkings = default!; // Arcane

    private const float GibletLaunchImpulse = 8;
    private const float GibletLaunchImpulseVariance = 3;
    private float _medicalHealingTickrate = 2f;

    private void InitializeBody()
    {
        // Body here to handle root body parts.
        SubscribeLocalEvent<BodyComponent, EntInsertedIntoContainerMessage>(OnBodyInserted);
        SubscribeLocalEvent<BodyComponent, EntRemovedFromContainerMessage>(OnBodyRemoved);

        SubscribeLocalEvent<BodyComponent, ComponentInit>(OnBodyInit);
        SubscribeLocalEvent<BodyComponent, MapInitEvent>(OnBodyMapInit);
        SubscribeLocalEvent<BodyComponent, CanDragEvent>(OnBodyCanDrag);
        SubscribeLocalEvent<BodyComponent, StandAttemptEvent>(OnStandAttempt); // Shitmed Change
        SubscribeLocalEvent<BodyComponent, ProfileLoadFinishedEvent>(OnProfileLoadFinished); // Shitmed change
        SubscribeLocalEvent<BodyComponent, IsEquippingAttemptEvent>(OnBeingEquippedAttempt); // Shitmed Change
        SubscribeLocalEvent<BodyComponent, AttemptStopPullingEvent>(OnAttemptStopPulling); // Goobstation

        // Shitmed Change: to prevent people from falling immediately as rejuvenated
        SubscribeLocalEvent<BodyComponent, RejuvenateEvent>(OnRejuvenate);
        Subs.CVar(_cfg, SurgeryCVars.MedicalHealingTickrate, val => _medicalHealingTickrate = val, true);
    }

    private void OnAttemptStopPulling(Entity<BodyComponent> ent, ref AttemptStopPullingEvent args) // Goobstation
    {
        if (args.User == null || !Exists(args.User.Value))
            return;

        if (args.User.Value != ent.Owner)
            return;

        if (ent.Comp.LegEntities.Count > 0 || ent.Comp.RequiredLegs == 0)
            return;

        args.Cancelled = true;
    }

    private void OnBodyInserted(Entity<BodyComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        // Root body part?
        var slotId = args.Container.ID;

        if (slotId != BodyRootContainerId)
            return;

        var insertedUid = args.Entity;

        if (TryComp(insertedUid, out BodyPartComponent? part))
        {
            AddPart((ent, ent), (insertedUid, part), slotId);
            RecursiveBodyUpdate((insertedUid, part), ent);
        }

        if (TryComp(insertedUid, out OrganComponent? organ))
        {
            AddOrgan((insertedUid, organ), ent, ent);
        }
    }

    private void OnBodyRemoved(Entity<BodyComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        // Root body part?
        var slotId = args.Container.ID;

        if (slotId != BodyRootContainerId)
            return;

        var removedUid = args.Entity;
        DebugTools.Assert(!TryComp(removedUid, out BodyPartComponent? b) || b.Body == ent);
        DebugTools.Assert(!TryComp(removedUid, out OrganComponent? o) || o.Body == ent);

        if (TryComp(removedUid, out BodyPartComponent? part))
        {
            RemovePart((ent, ent), (removedUid, part), slotId);
            RecursiveBodyUpdate((removedUid, part), null);
        }

        if (TryComp(removedUid, out OrganComponent? organ))
            RemoveOrgan((removedUid, organ), ent);
    }

    private void OnBodyInit(Entity<BodyComponent> ent, ref ComponentInit args)
    {
        // Setup the initial container.
        ent.Comp.RootContainer = Containers.EnsureContainer<ContainerSlot>(ent, BodyRootContainerId);
    }

    private void OnBodyMapInit(Entity<BodyComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.Prototype is null)
            return;

        // One-time setup
        // Obviously can't run in Init to avoid double-spawns on save / load.
        var prototype = Prototypes.Index(ent.Comp.Prototype.Value);
        MapInitBody(ent, prototype);
        EnsureComp<SurgeryTargetComponent>(ent); // Shitmed change
    }

    private void MapInitBody(EntityUid bodyEntity, BodyPrototype prototype)
    {
        var protoRoot = prototype.Slots[prototype.Root];
        if (protoRoot.Part is null)
            return;

        // This should already handle adding the entity to the root.
        var rootPartUid = SpawnInContainerOrDrop(protoRoot.Part, bodyEntity, BodyRootContainerId);
        var rootPart = Comp<BodyPartComponent>(rootPartUid);
        rootPart.Body = bodyEntity;
        Dirty(rootPartUid, rootPart);

        // Setup the rest of the body entities.
        SetupOrgans((rootPartUid, rootPart), protoRoot.Organs);
        MapInitParts(bodyEntity, rootPartUid, rootPart, prototype); // Shitmed Change
    }

    private void OnBodyCanDrag(Entity<BodyComponent> ent, ref CanDragEvent args)
    {
        args.Handled = true;
    }

    /// <summary>
    /// Sets up all of the relevant body parts for a particular body entity and root part.
    /// </summary>
    private void MapInitParts(EntityUid body, EntityUid rootPartId, BodyPartComponent rootPart, BodyPrototype prototype) // Shitmed Change
    {
        // Start at the root part and traverse the body graph, setting up parts as we go.
        // Basic BFS pathfind.
        var rootSlot = prototype.Root;
        var frontier = new Queue<string>();
        frontier.Enqueue(rootSlot);

        // Child -> Parent connection.
        var cameFrom = new Dictionary<string, string>();
        cameFrom[rootSlot] = rootSlot;
        // Maps slot to its relevant entity.
        var cameFromEntities = new Dictionary<string, EntityUid>();
        cameFromEntities[rootSlot] = rootPartId;

        while (frontier.TryDequeue(out var currentSlotId))
        {
            var currentSlot = prototype.Slots[currentSlotId];

            foreach (var connection in currentSlot.Connections)
            {
                // Already been handled
                if (!cameFrom.TryAdd(connection, currentSlotId))
                    continue;

                // Setup part
                var connectionSlot = prototype.Slots[connection];
                var parentEntity = cameFromEntities[currentSlotId];
                var parentPartComponent = Comp<BodyPartComponent>(parentEntity);

                // Spawn the entity on the target
                // then get the body part type, create the slot, and finally
                // we can insert it into the container.
                var childPart = Spawn(connectionSlot.Part, new EntityCoordinates(parentEntity, Vector2.Zero));
                cameFromEntities[connection] = childPart;

                var childPartComponent = Comp<BodyPartComponent>(childPart);
                TryCreatePartSlot(parentEntity, connection, childPartComponent.PartType, childPartComponent.Symmetry, out var partSlot, parentPartComponent);
                // Shitmed Change Start
                childPartComponent.Body = body;
                childPartComponent.ParentSlot = partSlot;
                Dirty(childPart, childPartComponent);
                // Shitmed Change End
                var cont = Containers.GetContainer(parentEntity, GetPartSlotContainerId(connection));

                if (partSlot is null || !Containers.Insert(childPart, cont))
                {
                    Log.Error($"Could not create slot for connection {connection} in body {prototype.ID}");
                    QueueDel(childPart);
                    continue;
                }

                // Add organs
                SetupOrgans((childPart, childPartComponent), connectionSlot.Organs);

                // Enqueue it so we can also get its neighbors.
                frontier.Enqueue(connection);
            }
        }
    }

    private void SetupOrgans(Entity<BodyPartComponent> ent, Dictionary<string, string> organs)
    {
        foreach (var (organSlotId, organProto) in organs)
        {
            TryCreateOrganSlot(ent, organSlotId, out var slot); // Shitmed Change
            SpawnInContainerOrDrop(organProto, ent, GetOrganContainerId(organSlotId));

            if (slot is null)
            {
                Log.Error($"Could not create organ for slot {organSlotId} in {ToPrettyString(ent)}");
            }
        }
    }

    /// <summary>
    /// Gets all body containers on this entity including the root one.
    /// </summary>
    public IEnumerable<BaseContainer> GetBodyContainers(
        EntityUid id,
        BodyComponent? body = null,
        BodyPartComponent? rootPart = null)
    {
        if (!Resolve(id, ref body, logMissing: false)
            || body.RootContainer.ContainedEntity is null
            || !Resolve(body.RootContainer.ContainedEntity.Value, ref rootPart))
        {
            yield break;
        }

        yield return body.RootContainer;

        foreach (var childContainer in GetPartContainers(body.RootContainer.ContainedEntity.Value, rootPart))
        {
            yield return childContainer;
        }
    }

    /// <summary>
    /// Gets all child body parts of this entity, including the root entity.
    /// </summary>
    public IEnumerable<(EntityUid Id, BodyPartComponent Component)> GetBodyChildren(
        EntityUid? id,
        BodyComponent? body = null,
        BodyPartComponent? rootPart = null)
    {
        if (id is null
            || !Resolve(id.Value, ref body, logMissing: false)
            || body is null // Shitmed Change
            || body.RootContainer == null // Shitmed Change
            || body.RootContainer.ContainedEntity is null
            || !Resolve(body.RootContainer.ContainedEntity.Value, ref rootPart))
        {
            yield break;
        }

        foreach (var child in GetBodyPartChildren(body.RootContainer.ContainedEntity.Value, rootPart))
        {
            yield return child;
        }
    }

    // Goobstation start
    public IEnumerable<(EntityUid Id, BodyPartComponent Component)> GetVitalBodyChildren(
        EntityUid? id,
        BodyComponent? body = null,
        BodyPartComponent? rootPart = null)
    {
        if (id is null
            || !Resolve(id.Value, ref body, logMissing: false)
            || body is null // Shitmed Change
            || body.RootContainer == null // Shitmed Change
            || body.RootContainer.ContainedEntity is null
            || !Resolve(body.RootContainer.ContainedEntity.Value, ref rootPart))
        {
            yield break;
        }

        foreach (var child in GetBodyPartChildren(body.RootContainer.ContainedEntity.Value, rootPart))
        {
            if ((int) (child.Component.PartType & BodyPartType.Vital) != 0)
                yield return child;
        }
    }
    // Goobstation end

    public IEnumerable<(EntityUid Id, OrganComponent Component)> GetBodyOrgans(
        EntityUid? bodyId,
        BodyComponent? body = null)
    {
        if (bodyId is null || !Resolve(bodyId.Value, ref body, logMissing: false))
            yield break;

        foreach (var part in GetBodyChildren(bodyId, body))
        {
            foreach (var organ in GetPartOrgans(part.Id, part.Component))
            {
                yield return organ;
            }
        }
    }

    /// <summary>
    /// Returns all body part slots for this entity.
    /// </summary>
    /// <param name="bodyId"></param>
    /// <param name="body"></param>
    /// <returns></returns>
    public IEnumerable<BodyPartSlot> GetBodyAllSlots(
        EntityUid bodyId,
        BodyComponent? body = null)
    {
        if (!Resolve(bodyId, ref body, logMissing: false)
            || body.RootContainer.ContainedEntity is null)
        {
            yield break;
        }

        foreach (var slot in GetAllBodyPartSlots(body.RootContainer.ContainedEntity.Value))
        {
            yield return slot;
        }
    }

    public virtual HashSet<EntityUid> GibBody(
        EntityUid bodyId,
        bool gibOrgans = false,
        BodyComponent? body = null,
        bool launchGibs = true,
        Vector2? splatDirection = null,
        float splatModifier = 1,
        Angle splatCone = default,
        SoundSpecifier? gibSoundOverride = null,
        // Shitmed Change
        GibType gib = GibType.Gib,
        GibContentsOption contents = GibContentsOption.Drop,
        List<string>? allowedContainers = null,
        List<string>? excludedContainers = null)
    {
        var gibs = new HashSet<EntityUid>();

        if (!Resolve(bodyId, ref body, logMissing: false))
            return gibs;

        if (TryGetRootPart(bodyId, out var root) && TryComp(root.Value.Owner, out GibbableComponent? gibbable)) // ShitMed - TryGet
            gibSoundOverride ??= gibbable.GibSound;


        var parts = GetBodyChildren(bodyId, body).ToArray();
        gibs.EnsureCapacity(parts.Length);
        foreach (var part in parts)
        {

            _gibbingSystem.TryGibEntityWithRef(bodyId, part.Id, gib, contents, ref gibs, playAudio: false,
                launchGibs: true, launchDirection: splatDirection, launchImpulse: GibletLaunchImpulse * splatModifier,
                launchImpulseVariance: GibletLaunchImpulseVariance, launchCone: splatCone,
                allowedContainers: allowedContainers, excludedContainers: excludedContainers);

            if (!gibOrgans)
                continue;

            foreach (var organ in GetPartOrgans(part.Id, part.Component))
            {
                _gibbingSystem.TryGibEntityWithRef(bodyId, organ.Id, GibType.Drop, GibContentsOption.Skip,
                    ref gibs, playAudio: false, launchImpulse: GibletLaunchImpulse * splatModifier,
                    launchImpulseVariance: GibletLaunchImpulseVariance, launchCone: splatCone,
                    allowedContainers: allowedContainers, excludedContainers: excludedContainers);
            }
        }

        var bodyTransform = Transform(bodyId);
        if (TryComp<InventoryComponent>(bodyId, out var inventory))
        {
            foreach (var item in _inventory.GetHandOrInventoryEntities(bodyId))
            {
                SharedTransform.DropNextTo(item, (bodyId, bodyTransform));
                gibs.Add(item);
            }
        }
        _audioSystem.PlayPredicted(gibSoundOverride, bodyTransform.Coordinates, null);
        return gibs;
    }

    // Shitmed Change Start

    public virtual HashSet<EntityUid> GibPart(
        EntityUid partId,
        BodyPartComponent? part = null,
        bool launchGibs = true,
        Vector2? splatDirection = null,
        float splatModifier = 1,
        Angle splatCone = default,
        SoundSpecifier? gibSoundOverride = null)
    {
        var gibs = new HashSet<EntityUid>();

        if (!Resolve(partId, ref part, logMissing: false)
            || part.Body is not null)
            return gibs;

        _gibbingSystem.TryGibEntityWithRef(partId, partId, GibType.Gib, GibContentsOption.Drop, ref gibs,
                playAudio: true, launchGibs: true, launchDirection: splatDirection, launchImpulse: GibletLaunchImpulse * splatModifier,
                launchImpulseVariance: GibletLaunchImpulseVariance, launchCone: splatCone);

        if (HasComp<InventoryComponent>(partId))
        {
            foreach (var item in _inventory.GetHandOrInventoryEntities(partId))
            {
                SharedTransform.AttachToGridOrMap(item);
                gibs.Add(item);
            }
        }
        _audioSystem.PlayPredicted(gibSoundOverride, Transform(partId).Coordinates, null);
        return gibs;
    }

    public virtual bool BurnPart(EntityUid partId,
        BodyPartComponent? part = null)
    {
        if (!Resolve(partId, ref part, logMissing: false))
            return false;

        if (part.Body is { } bodyEnt)
        {
            if (IsPartRoot(bodyEnt, partId, part: part))
                return false;

            DropSlotContents((partId, part));
            QueueDel(partId);
            return true;
        }

        return false;
    }

    private void OnProfileLoadFinished(EntityUid uid, BodyComponent component, ProfileLoadFinishedEvent args)
    {
        if (!TryComp<HumanoidAppearanceComponent>(uid, out var humanoid)
            || TerminatingOrDeleted(uid)
            || !Initialized(uid))
            return;

        foreach (var part in GetBodyChildren(uid, component))
            EnsureComp<BodyPartAppearanceComponent>(part.Id);

        humanoid.ProfileLoaded = true;
        Dirty(uid, humanoid);
    }

    private void OnStandAttempt(Entity<BodyComponent> ent, ref StandAttemptEvent args)
    {
        if (ent.Comp.LegEntities.Count < ent.Comp.RequiredLegs)
            args.Cancel();
    }

    private void OnBeingEquippedAttempt(Entity<BodyComponent> ent, ref IsEquippingAttemptEvent args)
    {
        if (!TryComp(args.EquipTarget, out BodyComponent? targetBody)
            || targetBody.Prototype == null
            || HasComp<BorgChassisComponent>(args.EquipTarget))
            return;

        if (TryGetPartFromSlotContainer(args.Slot, out var bodyPart)
            && bodyPart is not null)
        {
            var bodyPartString = bodyPart.Value.ToString().ToLower();
            var prototype = Prototypes.Index(targetBody.Prototype.Value);
            var hasPartConnection = prototype.Slots.Values.Any(slot =>
                slot.Connections.Contains(bodyPartString));

            if (hasPartConnection
                && !GetBodyChildrenOfType(args.EquipTarget, bodyPart.Value).Any())
            {
                _popup.PopupClient(Loc.GetString("equip-part-missing-error",
                    ("target", args.EquipTarget), ("part", bodyPartString)), args.Equipee, args.Equipee);
                args.Cancel();
            }
        }
    }

    private void OnRejuvenate(EntityUid ent, BodyComponent body, ref RejuvenateEvent args)
    {
        RestoreBody((ent, body)); // Goobstation
    }

    // Goob edit start
    public void RestoreBody(Entity<BodyComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return;

        var ent = entity.Owner;
        var body = entity.Comp;

        if (body.Prototype == null)
            return;

        var prototype = Prototypes.Index(body.Prototype.Value);

        if (!TryGetRootPart(ent, out var rootPart))
            return;

        var rootSlot = prototype.Root;
        foreach (var organ in prototype.Slots[rootSlot].Organs)
        {
            if (!Containers.TryGetContainer(rootPart.Value.Owner, GetOrganContainerId(organ.Key), out var organContainer))
                continue;

            var organEnt = organContainer.ContainedEntities.FirstOrNull();
            if (organEnt != null)
            {
                foreach (var modifier in Comp<OrganComponent>(organEnt.Value).IntegrityModifiers)
                {
                    _trauma.TryRemoveOrganDamageModifier(organEnt.Value, modifier.Key.Item2, modifier.Key.Item1);
                }
            }
            else
            {
                SpawnInContainerOrDrop(organ.Value, rootPart.Value.Owner, GetOrganContainerId(organ.Key));
            }
        }

        Dirty(rootPart.Value.Owner, rootPart.Value.Comp);

        var frontier = new Queue<string>();
        frontier.Enqueue(rootSlot);

        var cameFrom = new Dictionary<string, string>();
        cameFrom[rootSlot] = rootSlot;

        var cameFromEntities = new Dictionary<string, EntityUid>();
        cameFromEntities[rootSlot] = rootPart.Value.Owner;

        while (frontier.TryDequeue(out var currentSlotId))
        {
            var currentSlot = prototype.Slots[currentSlotId];

            foreach (var connection in currentSlot.Connections)
            {
                if (!cameFrom.TryAdd(connection, currentSlotId))
                    continue;

                var connectionSlot = prototype.Slots[connection];
                var parentEntity = cameFromEntities[currentSlotId];
                var parentPartComponent = Comp<BodyPartComponent>(parentEntity);

                if (Containers.TryGetContainer(parentEntity, GetPartSlotContainerId(connection), out var container))
                {
                    if (container.ContainedEntities.Count > 0)
                    {
                        var containedEnt = container.ContainedEntities[0];
                        var containedPartComp = Comp<BodyPartComponent>(containedEnt);
                        cameFromEntities[connection] = containedEnt;

                        foreach (var organ in connectionSlot.Organs)
                        {
                            if (Containers.TryGetContainer(containedEnt, GetOrganContainerId(organ.Key), out var organContainer))
                            {
                                var organEnt = organContainer.ContainedEntities.FirstOrNull();
                                if (organEnt != null)
                                {
                                    foreach (var modifier in Comp<OrganComponent>(organEnt.Value).IntegrityModifiers)
                                    {
                                        _trauma.TryRemoveOrganDamageModifier(organEnt.Value, modifier.Key.Item2, modifier.Key.Item1);
                                    }
                                }
                                else
                                {
                                    SpawnInContainerOrDrop(organ.Value, containedEnt, GetOrganContainerId(organ.Key));
                                }
                            }
                            else
                            {
                                var slot = CreateOrganSlot((containedEnt, containedPartComp), organ.Key);
                                SpawnInContainerOrDrop(organ.Value, containedEnt, GetOrganContainerId(organ.Key));

                                if (slot is null)
                                {
                                    Log.Error($"Could not create organ for slot {organ.Key} in {ToPrettyString(ent)}");
                                }
                            }
                        }
                    }
                    else
                    {
                        var childPart = Spawn(connectionSlot.Part, new EntityCoordinates(parentEntity, Vector2.Zero));
                        cameFromEntities[connection] = childPart;

                        var childPartComponent = Comp<BodyPartComponent>(childPart);

                        var partSlot = new BodyPartSlot(connection, childPartComponent.PartType, childPartComponent.Symmetry);
                        childPartComponent.ParentSlot = partSlot;
                        parentPartComponent.Children.TryAdd(connection, partSlot);

                        Dirty(parentEntity, parentPartComponent);
                        Dirty(childPart, childPartComponent);

                        Containers.Insert(childPart, container);
                        _lostPartMarkings.RestoreMarkings(ent, (childPart, childPartComponent)); // Arcane

                        SetupOrgans((childPart, childPartComponent), connectionSlot.Organs);
                    }
                }
                else
                {
                    var childPart = Spawn(connectionSlot.Part, new EntityCoordinates(parentEntity, Vector2.Zero));
                    cameFromEntities[connection] = childPart;

                    var childPartComponent = Comp<BodyPartComponent>(childPart);

                    var partSlot = CreatePartSlot(parentEntity, connection, childPartComponent.PartType, childPartComponent.Symmetry, parentPartComponent);
                    childPartComponent.ParentSlot = partSlot;

                    Dirty(parentEntity, parentPartComponent);
                    Dirty(childPart, childPartComponent);

                    if (partSlot is null)
                    {
                        Log.Error($"Could not create slot for connection {connection} in body {prototype.ID}");
                        QueueDel(childPart);
                        continue;
                    }

                    container = Containers.GetContainer(parentEntity, GetPartSlotContainerId(connection));
                    Containers.Insert(childPart, container);
                    _lostPartMarkings.RestoreMarkings(ent, (childPart, childPartComponent)); // Arcane

                    SetupOrgans((childPart, childPartComponent), connectionSlot.Organs);
                }

                frontier.Enqueue(connection);
            }
        }


        if (_trauma.TryGetBodyTraumas(ent, out var traumas, bodyComp: body))
            foreach (var trauma in traumas)
                _trauma.RemoveTrauma(trauma);

        foreach (var bodyPart in GetBodyChildren(ent, body))
        {
            if (!TryComp<WoundableComponent>(bodyPart.Id, out var woundable))
                continue;

            var bone = woundable.Bone.ContainedEntities.FirstOrNull();
            if (TryComp<BoneComponent>(bone, out var boneComp))
                _trauma.SetBoneIntegrity(bone.Value, boneComp.IntegrityCap, boneComp);

            _woundSystem.TryHaltAllBleeding(bodyPart.Id, woundable);
            _woundSystem.ForceHealWoundsOnWoundable(bodyPart.Id, out _);
        }
    }
    // Goob edit end

    /// <summary>
    /// Gets all child body parts of this entity that have component T, including the root entity if it has component T.
    /// </summary>
    public IEnumerable<(EntityUid Id, BodyPartComponent BodyPart, T Component)> GetBodyChildrenWithComponent<T>(
        EntityUid? id,
        BodyComponent? body = null,
        BodyPartComponent? rootPart = null)
        where T : IComponent
    {
        if (id is null
            || !Resolve(id.Value, ref body, logMissing: false)
            || body is null
            || body.RootContainer == null
            || body.RootContainer.ContainedEntity is null
            || !Resolve(body.RootContainer.ContainedEntity.Value, ref rootPart))
        {
            yield break;
        }

        foreach (var child in GetBodyPartChildrenWithComponent<T>(body.RootContainer.ContainedEntity.Value, rootPart))
        {
            yield return child;
        }
    }

    /// <summary>
    /// Returns all body part components for this entity including itself that have component T.
    /// </summary>
    public IEnumerable<(EntityUid Id, BodyPartComponent BodyPart, T Component)> GetBodyPartChildrenWithComponent<T>(
        EntityUid partId,
        BodyPartComponent? part = null)
        where T : IComponent
    {
        if (!Resolve(partId, ref part, logMissing: false))
            yield break;

        var query = GetEntityQuery<T>();

        // Check if the current part has the component
        if (query.TryGetComponent(partId, out var component))
            yield return (partId, part, component);

        foreach (var slotId in part.Children.Keys)
        {
            var containerSlotId = GetPartSlotContainerId(slotId);

            if (Containers.TryGetContainer(partId, containerSlotId, out var container))
            {
                foreach (var containedEnt in container.ContainedEntities)
                {
                    if (!TryComp(containedEnt, out BodyPartComponent? childPart))
                        continue;

                    foreach (var value in GetBodyPartChildrenWithComponent<T>(containedEnt, childPart))
                    {
                        yield return value;
                    }
                }
            }
        }
    }

    // Shitmed Change End
}

```

## File: Content.Shared/_Arcane/Body/Components/LostPartMarkingsComponent.cs
```
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;

namespace Content.Shared._Arcane.Body.Components;

[RegisterComponent]
public sealed partial class LostPartMarkingsComponent : Component
{
    [ViewVariables]
    public Dictionary<string, Dictionary<HumanoidVisualLayers, List<Marking>>> Markings = new();
}

```

## File: Content.Shared/_Arcane/Body/Systems/LostPartMarkingsSystem.cs
```
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared._Arcane.Body.Components;
using Content.Shared._Shitmed.Body.Part;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Arcane.Body.Systems;

public sealed class LostPartMarkingsSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        // The body drops the part's markings, so read them first while it still knows which are forced.
        SubscribeLocalEvent<HumanoidAppearanceComponent, BodyPartRemovedEvent>(OnBodyPartRemoved, before: [typeof(SharedBodySystem)]);
    }

    private void OnBodyPartRemoved(Entity<HumanoidAppearanceComponent> ent, ref BodyPartRemovedEvent args)
    {
        if (!_net.IsServer || TerminatingOrDeleted(ent))
            return;

        if (TryComp<LostPartMarkingsComponent>(ent, out var lost))
            lost.Markings.Remove(args.Slot);

        if (!TryComp<BodyPartAppearanceComponent>(args.Part, out var appearance))
            return;

        var markings = new Dictionary<HumanoidVisualLayers, List<Marking>>();
        foreach (var (layer, list) in appearance.Markings)
        {
            var category = MarkingCategoriesConversion.FromHumanoidVisualLayers(layer);
            foreach (var marking in list)
            {
                if (ent.Comp.MarkingSet.TryGetMarking(category, marking.MarkingId, out var bodyMarking))
                    markings.GetOrNew(layer).Add(new Marking(bodyMarking));
            }
        }

        if (markings.Count > 0)
            EnsureComp<LostPartMarkingsComponent>(ent).Markings[args.Slot] = markings;
    }

    public void RestoreMarkings(Entity<HumanoidAppearanceComponent?> body, Entity<BodyPartComponent> part)
    {
        if (!Resolve(body, ref body.Comp, false))
            return;

        // Losing a part hides all its sublayers, but attaching it shows only its own layer.
        if (part.Comp.ToHumanoidLayers() is { } partLayer)
        {
            var partLayers = GetPartLayers(body);
            var sublayers = HumanoidVisualLayersExtension.Sublayers(partLayer).Where(layer => !partLayers.Contains(layer));
            _humanoid.SetLayersVisibility(body, sublayers, true);
        }

        if (!TryComp<LostPartMarkingsComponent>(body, out var lost)
            || !_container.TryGetContainingContainer(part.Owner, out var slot)
            || !lost.Markings.Remove(slot.ID, out var markings)
            || !TryComp<BodyPartAppearanceComponent>(part, out var appearance))
            return;

        appearance.Markings = markings;
        Dirty(part, appearance);

        foreach (var (layer, list) in markings)
        {
            var category = MarkingCategoriesConversion.FromHumanoidVisualLayers(layer);
            foreach (var marking in list)
            {
                if (!body.Comp.MarkingSet.TryGetMarking(category, marking.MarkingId, out _))
                    _humanoid.AddMarking(body, marking.MarkingId, marking.MarkingColors, false, marking.Forced, body.Comp);
            }
        }

        Dirty(body.Owner, body.Comp);
    }

    private HashSet<HumanoidVisualLayers> GetPartLayers(EntityUid body)
    {
        var layers = new HashSet<HumanoidVisualLayers>();
        if (!TryComp<BodyComponent>(body, out var bodyComp) || bodyComp.Prototype is not { } bodyProto)
            return layers;

        foreach (var slot in _proto.Index(bodyProto).Slots.Values)
        {
            if (slot.Part is { } partProto
                && _proto.Index(partProto).TryGetComponent<BodyPartComponent>(out var part, _componentFactory)
                && part.ToHumanoidLayers() is { } layer)
                layers.Add(layer);
        }

        return layers;
    }
}

```

## File: Content.Shared/_Arcane/Slime/SharedSlimeRegrowSystem.cs
```
using System.Numerics;
using Content.Shared.Actions;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Prototypes;
using Content.Shared.Body.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared._Arcane.Body.Systems;
using Content.Shared._Shitmed.Medical.Surgery.Traumas.Systems;
using Content.Shared._Shitmed.Medical.Surgery.Wounds.Systems;
using Content.Shared._Shitmed.Targeting;
using Content.Shared._Shitmed.Targeting.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._Arcane.Slime;

/// <summary>
/// Handles granting and removing the slime limb regrow action, and the action itself.
/// The server is authoritative: it alone finds the missing limb, picks one, grows it and
/// spends the hunger/thirst, and drives the popup/audio feedback. The client performs no
/// predicted feedback, so it can't show a success that the server would reject.
/// </summary>
public abstract partial class SharedSlimeRegrowSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly LostPartMarkingsSystem _lostPartMarkings = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ThirstSystem _thirst = default!;
    [Dependency] private readonly TraumaSystem _trauma = default!;
    [Dependency] private readonly WoundSystem _wound = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlimeRegrowComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SlimeRegrowComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<SlimeRegrowComponent, SlimeRegrowLimbEvent>(OnSlimeRegrowLimb);
    }

    private void OnMapInit(Entity<SlimeRegrowComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.ActionEnt = _actions.AddAction(ent, ent.Comp.ActionId);
        Dirty(ent, ent.Comp);
    }

    private void OnShutdown(Entity<SlimeRegrowComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.ActionEnt);
    }

    private void OnSlimeRegrowLimb(Entity<SlimeRegrowComponent> ent, ref SlimeRegrowLimbEvent args)
    {
        if (args.Handled)
            return;

        var user = args.Performer;

        if (!TryComp<BodyComponent>(user, out var body)
            || body.Prototype is null
            || !_body.TryGetRootPart(user, out _, body))
            return;

        if (!_net.IsServer)
            return;

        var candidates = FindMissingLimbs(user, body);

        if (candidates.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString(ent.Comp.NoLimbPopup), user, user);
            return;
        }

        if (!TryComp<HungerComponent>(user, out var hunger)
            || _hunger.GetHunger(hunger) < ent.Comp.HungerCost)
        {
            _popup.PopupEntity(Loc.GetString(ent.Comp.TooHungryPopup), user, user);
            return;
        }

        if (!TryComp<ThirstComponent>(user, out var thirst)
            || thirst.CurrentThirst < ent.Comp.ThirstCost)
        {
            _popup.PopupEntity(Loc.GetString(ent.Comp.TooThirstyPopup), user, user);
            return;
        }

        var candidate = _random.Pick(candidates);

        if (!TryGrowLimb(ent, candidate.ParentId, candidate.SlotId, candidate.Slot))
        {
            _popup.PopupEntity(Loc.GetString(ent.Comp.NoLimbPopup), user, user);
            return;
        }

        RefreshBodyStatus(user);

        // Resources are only spent once the limb actually regrew.
        _hunger.ModifyHunger(user, -ent.Comp.HungerCost, hunger);
        _thirst.ModifyThirst(user, thirst, -ent.Comp.ThirstCost);

        _popup.PopupEntity(Loc.GetString(ent.Comp.RegrowPopup), user, user);
        _audio.PlayEntity(ent.Comp.Sound, user, user);

        args.Handled = true;
    }

    /// <summary>
    /// Traverses the body prototype starting from the root, collecting every missing
    /// part slot that could be regrown. The head counts; the chest and groin never do.
    /// </summary>
    private List<MissingLimb> FindMissingLimbs(EntityUid uid, BodyComponent body)
    {
        var missing = new List<MissingLimb>();

        if (body.Prototype is not { } protoId
            || !_body.TryGetRootPart(uid, out var rootPart, body))
            return missing;

        var prototype = _proto.Index(protoId);

        var frontier = new Queue<string>();
        frontier.Enqueue(prototype.Root);

        // Slots already traversed.
        var visited = new HashSet<string> { prototype.Root };

        // Maps slot to its relevant entity.
        var slotEntities = new Dictionary<string, EntityUid>();
        slotEntities[prototype.Root] = rootPart.Value.Owner;

        while (frontier.TryDequeue(out var currentSlotId))
        {
            var currentSlot = prototype.Slots[currentSlotId];

            foreach (var connection in currentSlot.Connections)
            {
                if (!visited.Add(connection))
                    continue;

                var connectionSlot = prototype.Slots[connection];
                var parentEntity = slotEntities[currentSlotId];

                if (_container.TryGetContainer(parentEntity, SharedBodySystem.GetPartSlotContainerId(connection), out var container)
                    && container.ContainedEntities.Count > 0)
                {
                    slotEntities[connection] = container.ContainedEntities[0];
                    frontier.Enqueue(connection);
                    continue;
                }

                if (connectionSlot.Part is not { } partId
                    || !_proto.TryIndex<EntityPrototype>(partId, out var partProto)
                    || !partProto.TryGetComponent<BodyPartComponent>(out var partComp, _componentFactory)
                    || (partComp.PartType & (BodyPartType.Chest | BodyPartType.Groin)) != 0)
                    continue;

                missing.Add(new MissingLimb(parentEntity, connection, connectionSlot));
            }
        }

        return missing;
    }

    private bool TryGrowLimb(Entity<SlimeRegrowComponent> ent, EntityUid parentId, string slotId, BodyPrototypeSlot slot)
    {
        if (slot.Part is not { } partId)
            return false;

        var childPart = Spawn(partId, new EntityCoordinates(parentId, Vector2.Zero));
        var childPartComp = Comp<BodyPartComponent>(childPart);

        if (!_body.TryCreatePartSlotAndAttach(parentId, slotId, childPart, childPartComp.PartType, childPartComp.Symmetry))
        {
            Log.Error($"Failed to regrow part {partId} into slot {slotId} of {ToPrettyString(parentId)}");
            QueueDel(childPart);
            return false;
        }

        _lostPartMarkings.RestoreMarkings(ent.Owner, (childPart, childPartComp));

        // Regrowing a limb also heals the stump (Dismemberment trauma) its removal left behind,
        // otherwise it would still need surgery to clean up before the socket is usable again.
        // Only clear the trauma matching the regrown part so unrelated dismemberments stay intact.
        if (_trauma.TryGetWoundableTrauma(parentId, out var stumpTraumas, TraumaSystem.Dismemberment))
        {
            foreach (var trauma in stumpTraumas)
            {
                if (trauma.Comp.TargetType is not { } targetType
                    || targetType != (childPartComp.PartType, childPartComp.Symmetry))
                    continue;

                _trauma.RemoveTrauma(trauma);
            }
        }

        foreach (var (organSlotId, organProtoId) in slot.Organs)
        {
            _body.TryCreateOrganSlot(childPart, organSlotId, out _);
            SpawnInContainerOrDrop(organProtoId, childPart, SharedBodySystem.GetOrganContainerId(organSlotId));
        }

        return true;
    }

    // A regrown part starts undamaged, so no wound severity change would update the body status doll.
    private void RefreshBodyStatus(EntityUid body)
    {
        if (!TryComp<TargetingComponent>(body, out var targeting))
            return;

        targeting.BodyStatus = _wound.GetWoundableStatesOnBodyPainFeels(body);
        Dirty(body, targeting);
        RaiseNetworkEvent(new TargetIntegrityChangeEvent(GetNetEntity(body)), body);
    }

    private readonly record struct MissingLimb(EntityUid ParentId, string SlotId, BodyPrototypeSlot Slot);
}

```

## File: Content.Shared/_Arcane/Slime/SlimeRegrowComponent.cs
```
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Arcane.Slime;

/// <summary>
/// Allows slime people to regrow a missing limb at the cost of
/// their own food and water reserves.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class SlimeRegrowComponent : Component
{
    /// <summary>
    /// The action that triggers the limb regrow.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? ActionEnt;

    /// <summary>
    /// The action prototype granted when the entity is initialized.
    /// </summary>
    [DataField]
    public EntProtoId ActionId = "ActionSlimeRegrowLimb";

    /// <summary>
    /// Hunger consumed each time a limb is regrown.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float HungerCost = 60f;

    /// <summary>
    /// Thirst consumed each time a limb is regrown.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ThirstCost = 100f;

    /// <summary>
    /// Popup shown when a limb is regrown successfully.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId RegrowPopup = "slime-regrow-limb-success";

    /// <summary>
    /// Popup shown when there is no missing limb or head to regrow.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId NoLimbPopup = "slime-regrow-limb-none";

    /// <summary>
    /// Popup shown when the entity does not have enough hunger to regrow a limb.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId TooHungryPopup = "slime-regrow-limb-too-hungry";

    /// <summary>
    /// Popup shown when the entity does not have enough thirst to regrow a limb.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId TooThirstyPopup = "slime-regrow-limb-too-thirsty";

    /// <summary>
    /// Sound played when a limb is regrown.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Voice/Slime/slime_squish.ogg");
}

```

## File: Content.Shared/_Arcane/Surgery/SharedSurgeryToolSystem.cs
```
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Shitmed.CCVar;
using Content.Shared._Shitmed.Medical.Surgery;
using Content.Shared._Shitmed.Medical.Surgery.Conditions;
using Content.Shared._Shitmed.Medical.Surgery.Effects.Step;
using Content.Shared._Arcane.OfferItem;
using Content.Shared._Shitmed.Medical.Surgery.Steps;
using Content.Shared._Shitmed.Medical.Surgery.Steps.Parts;
using Content.Shared._Shitmed.Medical.Surgery.Tools;
using Content.Shared._Shitmed.Targeting;
using Content.Shared.ActionBlocker;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Buckle;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Kitchen.Components;
using Content.Shared.Medical.Healing;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._Arcane.Surgery;

/// <summary>
/// Performs surgery steps by clicking a lying patient with a tool, on the body part the surgeon is targeting.
/// </summary>
public abstract class SharedSurgeryToolSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSurgerySystem _surgery = default!;

    private bool _canOperateOnSelf;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SurgeryTargetComponent, InteractUsingEvent>(OnInteractUsing);
        // An empty-hand click would otherwise unbuckle the patient from the operating table.
        SubscribeLocalEvent<SurgeryTargetComponent, InteractHandEvent>(OnInteractHand, before: [typeof(SharedBuckleSystem)]);
        SubscribeAllEvent<SurgeryToolOptionPickedEvent>(OnOptionPicked);
        SubscribeLocalEvent<SurgeryPlanComponent, SurgeryStepEvent>(OnPlanStep);
        Subs.BuiEvents<SurgeryTargetComponent>(SurgeryUIKey.Key, subs => subs.Event<SurgeryPlanBuiMsg>(OnPlanMessage));
        Subs.CVar(_config, SurgeryCVars.CanOperateOnSelf, value => _canOperateOnSelf = value, true);
    }

    private void OnInteractUsing(Entity<SurgeryTargetComponent> ent, ref InteractUsingEvent args)
    {
        var user = args.User;
        var tool = args.Used;
        if (args.Handled
            || IsOffering(user)
            || IsButchering(ent, tool)
            || !CanOperate(ent, user, IsSurgicalTool(tool))
            || !TryGetTargetPart(ent, user, out var part, out var missing))
            return;

        var options = GetPlannedOption(ent, part, missing, user, tool) is { } planned
            ? new List<SurgeryToolOption> { planned }
            : GetToolOptions(ent, part, user, tool, missing);

        // Surgery tools may have their own use on a patient, so a click with nothing to do is left to them.
        if (options.Count == 0)
        {
            // A held part would otherwise be fed to the patient, so a part meant for attaching never falls through.
            if (TryComp(tool, out BodyPartComponent? held) && AcceptsHeldPart(ent, part, missing, held) is { } fits)
            {
                args.Handled = true;
                if (_timing.IsFirstTimePredicted)
                {
                    _popup.PopupClient(fits ? GetHint(ent, part, user) : Loc.GetString("surgery-tool-wrong-part", ("part", tool)),
                        ent,
                        user);
                }

                return;
            }

            if (IsSurgicalTool(tool) && _timing.IsFirstTimePredicted)
                _popup.PopupClient(GetHint(ent, part, user), ent, user);
            return;
        }

        args.Handled = true;
        if (_timing.IsFirstTimePredicted)
            Perform(ent, part, user, options);
    }

    // Planned steps that need no tool, like taking an item out of a cavity, are done with an empty hand.
    private void OnInteractHand(Entity<SurgeryTargetComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled
            || !CanOperate(ent, args.User, false)
            || !TryGetTargetPart(ent, args.User, out var part, out var missing)
            || GetPlannedOption(ent, part, missing, args.User, args.User) is not { } planned)
            return;

        args.Handled = true;
        // Putting an item into a cavity needs one in hand, so an empty hand does nothing and keeps the plan.
        if (_timing.IsFirstTimePredicted && !IsCavityInsert(planned.Step))
            Perform(ent, part, args.User, new List<SurgeryToolOption> { planned });
    }

    private void OnOptionPicked(SurgeryToolOptionPickedEvent msg, EntitySessionEventArgs args)
    {
        if (!_timing.IsFirstTimePredicted
            || args.SenderSession.AttachedEntity is not { } user
            || !TryGetEntity(msg.Part, out var part)
            || !TryComp(part, out BodyPartComponent? partComp)
            || partComp.Body is not { } body
            || !TryComp(body, out SurgeryTargetComponent? target)
            || !_actionBlocker.CanInteract(user, body)
            || !_interaction.InRangeUnobstructed(user, body)
            || !CanOperate((body, target), user, true))
            return;

        var tool = _hands.GetActiveItemOrSelf(user);
        if (tool != user && !_actionBlocker.CanUseHeldEntity(user, tool))
            return;

        // The patient may have changed since the options were shown, so the pick has to still be one of them.
        var missing = TryGetTargetPart(body, user, out var targeted, out var slot) && targeted == part ? slot : null;
        var option = msg.Option;
        if (GetPlannedOption(body, part.Value, missing, user, tool) != option
            && !IsToolOption(body, part.Value, user, tool, option))
            return;

        if (option.Target is { } targetSurgery && !_surgery.GetValidSurgeries(body, part.Value).Contains(targetSurgery))
            option = option with { Target = null };

        DoOption(body, part.Value, user, option);
    }

    private void OnPlanMessage(Entity<SurgeryTargetComponent> ent, ref SurgeryPlanBuiMsg args)
    {
        if (!TryGetEntity(args.Part, out var part)
            || !TryComp(part, out BodyPartComponent? partComp)
            || partComp.Body != ent.Owner)
            return;

        if (args.Surgery is not { } surgery)
        {
            if (CompOrNull<SurgeryPlanComponent>(args.Actor)?.Part == part)
                SetPlan(args.Actor, null, null);
            return;
        }

        if (_surgery.GetValidSurgeries(ent, part.Value).Contains(surgery))
            SetPlan(args.Actor, part, surgery);
    }

    /// <summary>
    /// Steps the tool can perform next on the part, merged when several surgeries share a step, or steps with the same effect, that does not depend on them.
    /// With <paramref name="missing"/> set, only surgeries attaching that part to the empty slot are considered.
    /// </summary>
    public List<SurgeryToolOption> GetToolOptions(EntityUid body,
        EntityUid part,
        EntityUid user,
        EntityUid tool,
        (BodyPartType Type, BodyPartSymmetry Symmetry)? missing = null)
    {
        var options = new List<(EntityUid? Owner, SurgeryToolOption Option)>();
        foreach (var surgeryId in _surgery.GetValidSurgeries(body, part))
        {
            if (_surgery.GetSingleton(surgeryId) is not { } surgery
                || missing is { } slot && !AttachesPart(surgery, slot.Type, slot.Symmetry)
                || GetNextStep(body, part, surgery, user, tool) is not { } next)
                continue;

            EntityUid? owner = DependsOnSurgery(next.StepEnt) ? next.Owner : null;
            var index = options.FindIndex(o => o.Owner == owner
                && (o.Option.Step == next.Step
                    || owner == null
                    && _surgery.GetSingleton(o.Option.Step) is { } shown
                    && (Covers(body, part, shown, next.StepEnt) || Covers(body, part, next.StepEnt, shown))));
            if (index < 0)
            {
                options.Add((owner, new SurgeryToolOption(next.OwnerId, next.Step, surgeryId)));
                continue;
            }

            var option = options[index].Option;
            if (option.Step != next.Step
                && _surgery.GetSingleton(option.Step) is { } kept
                && !Covers(body, part, kept, next.StepEnt))
                option = option with { Surgery = next.OwnerId, Step = next.Step };

            if (option.Target != surgeryId)
                option = option with { Target = null };

            options[index] = (owner, option);
        }

        return options.ConvertAll(o => o.Option);
    }

    /// <summary>
    /// The part the user is targeting on the body, or the parent holding its slot when that part is missing or
    /// still being reattached.
    /// </summary>
    public bool TryGetTargetPart(EntityUid body,
        EntityUid user,
        out EntityUid part,
        out (BodyPartType Type, BodyPartSymmetry Symmetry)? missing)
    {
        var (type, symmetry) = _body.ConvertTargetBodyPart(CompOrNull<TargetingComponent>(user)?.Target ?? TargetBodyPart.Chest);
        return TryGetPart(body, type, symmetry, out part, out missing);
    }

    private bool TryGetPart(EntityUid body,
        BodyPartType type,
        BodyPartSymmetry symmetry,
        out EntityUid part,
        out (BodyPartType Type, BodyPartSymmetry Symmetry)? missing)
    {
        missing = null;

        foreach (var (id, _) in _body.GetBodyChildrenOfType(body, type, symmetry: symmetry))
        {
            // A reattached part is finished by its attach surgery, which lives on the parent.
            if (HasComp<BodyPartReattachedComponent>(id) && _body.GetParentPartOrNull(id) is { } parent)
            {
                part = parent;
                missing = (type, symmetry);
                return true;
            }

            part = id;
            return true;
        }

        foreach (var (id, comp) in _body.GetBodyChildren(body))
        {
            foreach (var (slotId, slot) in comp.Children)
            {
                if (slot.Type != type
                    || slot.Symmetry != symmetry
                    || _container.TryGetContainer(id, SharedBodySystem.GetPartSlotContainerId(slotId), out var container)
                    && container.ContainedEntities.Count > 0)
                    continue;

                part = id;
                missing = (type, symmetry);
                return true;
            }
        }

        // A hand or foot lost with its limb has no slot left on the body, so the limb's slot is used.
        if (type is BodyPartType.Hand or BodyPartType.Foot)
            return TryGetPart(body, type == BodyPartType.Hand ? BodyPartType.Arm : BodyPartType.Leg, symmetry, out part, out missing);

        part = default;
        return false;
    }

    public void SetPlan(EntityUid user, EntityUid? part, EntProtoId? surgery)
    {
        if (part == null && surgery == null && !HasComp<SurgeryPlanComponent>(user))
            return;

        var plan = EnsureComp<SurgeryPlanComponent>(user);
        if (plan.Part == part && plan.Surgery == surgery)
            return;

        plan.Part = part;
        plan.Surgery = surgery;
        Dirty(user, plan);
    }

    // The menu is sent by the server, since the client may predict a different set of options.
    private void Perform(EntityUid body, EntityUid part, EntityUid user, List<SurgeryToolOption> options)
    {
        if (options.Count == 1 && !NeedsConfirmation(options[0].Step))
            DoOption(body, part, user, options[0]);
        else if (_net.IsServer)
            RaiseNetworkEvent(new SurgeryToolOptionsEvent(GetNetEntity(part), options), user);
    }

    private void DoOption(EntityUid body, EntityUid part, EntityUid user, SurgeryToolOption option)
    {
        if (option.Target is { } target)
            SetPlan(user, part, target);

        _surgery.TryDoSurgeryStep(body, part, user, option.Surgery, option.Step, out _);
    }

    // Tool-less steps take whatever is in hand and may start over once undone, so their plan ends with the surgery's
    // last step. Kept until it is done, as a refused, interrupted or ineffective step is reached again only through it.
    private void OnPlanStep(Entity<SurgeryPlanComponent> ent, ref SurgeryStepEvent args)
    {
        if (args.Complete
            && ent.Comp.Part == args.Part
            && !HasToolRequirement(args.Step)
            && TryComp(args.Surgery, out SurgeryComponent? surgery)
            && surgery.Steps.Count > 0
            && surgery.Steps[^1].Id == Prototype(args.Step)?.ID)
            SetPlan(ent, null, null);
    }

    private SurgeryToolOption? GetPlannedOption(EntityUid body,
        EntityUid part,
        (BodyPartType Type, BodyPartSymmetry Symmetry)? missing,
        EntityUid user,
        EntityUid tool)
    {
        if (!TryComp(user, out SurgeryPlanComponent? plan)
            || plan.Part != part
            || plan.Surgery is not { } planned
            || !_surgery.GetValidSurgeries(body, part).Contains(planned)
            || _surgery.GetSingleton(planned) is not { } surgery
            || missing is { } slot && !AttachesPart(surgery, slot.Type, slot.Symmetry)
            || GetNextStep(body, part, surgery, user, tool, fromPlan: true) is not { } next)
            return null;

        return new SurgeryToolOption(next.OwnerId, next.Step, planned);
    }

    // Checked against the owning surgery directly, since merged options may name any of the surgeries sharing the step.
    private bool IsToolOption(EntityUid body, EntityUid part, EntityUid user, EntityUid tool, SurgeryToolOption option)
    {
        return _surgery.GetValidSurgeries(body, part).Contains(option.Surgery)
            && _surgery.GetSingleton(option.Surgery) is { } surgery
            && GetNextStep(body, part, surgery, user, tool) is { } next
            && next.OwnerId == option.Surgery
            && next.Step == option.Step;
    }

    private string GetHint(EntityUid body, EntityUid part, EntityUid user)
    {
        if (TryComp(user, out SurgeryPlanComponent? plan)
            && plan.Part == part
            && plan.Surgery is { } planned
            && _surgery.GetSingleton(planned) is { } surgery
            && GetNextStep(body, part, surgery, user) is { } next)
        {
            if (GetToolName(next.StepEnt) is not { } tool)
            {
                return Loc.GetString("surgery-tool-next-step-no-tool",
                    ("surgery", Name(surgery)),
                    ("step", Name(next.StepEnt)));
            }

            return Loc.GetString("surgery-tool-next-step",
                ("surgery", Name(surgery)),
                ("step", Name(next.StepEnt)),
                ("tool", tool));
        }

        return Loc.GetString("surgery-tool-nothing-to-do", ("part", part));
    }

    private bool CanOperate(Entity<SurgeryTargetComponent> body, EntityUid user, bool popup)
    {
        if (!body.Comp.CanOperate)
            return false;

        if (user == body.Owner && !_canOperateOnSelf)
        {
            if (popup)
                _popup.PopupClient(Loc.GetString("surgery-error-self-surgery"), user, user);
            return false;
        }

        return _surgery.IsLyingDown(body, user, popup);
    }

    // With a tool given, only a step it can perform counts as next.
    private (EntityUid Owner, EntProtoId OwnerId, EntProtoId Step, EntityUid StepEnt)? GetNextStep(EntityUid body,
        EntityUid part,
        EntityUid surgery,
        EntityUid user,
        EntityUid? tool = null,
        bool fromPlan = false)
    {
        if (_surgery.GetNextStep(body, part, surgery, user) is not { } next
            || Prototype(next.Surgery)?.ID is not { } ownerId)
            return null;

        // In any order a negative index marks the last incomplete step, and earlier ones may be open too.
        var steps = next.Surgery.Comp.Steps;
        var anyOrder = next.Step < 0;
        var (first, last) = anyOrder ? (0, -next.Step - 1) : (next.Step, next.Step);
        for (var i = first; i <= last; i++)
        {
            if (anyOrder && _surgery.IsStepComplete(body, part, steps[i], next.Surgery)
                || _surgery.GetSingleton(steps[i]) is not { } stepEnt
                || tool is { } held && !CanPerformWith(next.Surgery, stepEnt, held, fromPlan))
                continue;

            return (next.Surgery.Owner, ownerId, steps[i], stepEnt);
        }

        return null;
    }

    private bool CanPerformWith(EntityUid surgery, EntityUid step, EntityUid tool, bool fromPlan)
    {
        return (AcceptsTool(step, tool) || fromPlan && !HasToolRequirement(step) && CanUseWithoutTool(tool))
            && FitsHeldItem(surgery, step, tool);
    }

    // Steps without a tool only run from the plan, otherwise any held item would start them.
    private bool AcceptsTool(EntityUid step, EntityUid tool)
    {
        if (!TryComp(step, out SurgeryStepComponent? comp) || comp.Tool is not { } tools)
            return false;

        foreach (var registry in tools.Values)
        {
            if (EntityManager.HasComponent(tool, registry.Component.GetType()))
                return true;
        }

        return false;
    }

    private bool HasToolRequirement(EntityUid step)
    {
        return TryComp(step, out SurgeryStepComponent? comp) && comp.Tool is { Count: > 0 };
    }

    // Inserting steps accept any organ or part, but only the one the surgery is about gets inserted.
    private bool FitsHeldItem(EntityUid surgery, EntityUid step, EntityUid tool)
    {
        if (HasComp<SurgeryAddOrganStepComponent>(step))
        {
            if (!TryComp(surgery, out SurgeryOrganConditionComponent? organ) || organ.Organ is not { } organs)
                return false;

            foreach (var registry in organs.Values)
            {
                if (!EntityManager.HasComponent(tool, registry.Component.GetType()))
                    return false;
            }

            return true;
        }

        if (HasComp<SurgeryAddPartStepComponent>(step))
        {
            return TryComp(tool, out BodyPartComponent? held)
                && TryComp(surgery, out SurgeryPartRemovedConditionComponent? removed)
                && held.PartType == removed.Part
                && (removed.Symmetry == null || held.Symmetry == removed.Symmetry);
        }

        return true;
    }

    // These steps read the owning surgery to pick the organ, part or slot they act on.
    private bool DependsOnSurgery(EntityUid step)
    {
        return HasComp<SurgeryAddPartStepComponent>(step)
            || HasComp<SurgeryAffixPartStepComponent>(step)
            || HasComp<SurgeryAddOrganStepComponent>(step)
            || HasComp<SurgeryAffixOrganStepComponent>(step)
            || HasComp<SurgeryRemoveOrganStepComponent>(step)
            || HasComp<SurgeryAddOrganSlotStepComponent>(step);
    }

    // Steps of different surgeries, like an incision and a careful incision, are one choice when doing one also completes
    // the other and they differ only in how much they hurt.
    private bool Covers(EntityUid body, EntityUid part, EntityUid step, EntityUid other)
    {
        if (!TryComp(step, out SurgeryStepComponent? stepComp)
            || !TryComp(other, out SurgeryStepComponent? otherComp)
            || stepComp.AddOrganOnAdd != null || stepComp.RemoveOrganOnAdd != null
            || otherComp.AddOrganOnAdd != null || otherComp.RemoveOrganOnAdd != null
            || !GetEffectTypes(step).SetEquals(GetEffectTypes(other)))
            return false;

        var covered = GetChanges(body, part, otherComp);
        return covered.Count > 0 && covered.IsSubsetOf(GetChanges(body, part, stepComp));
    }

    private HashSet<Type> GetEffectTypes(EntityUid step)
    {
        var types = new HashSet<Type>();
        foreach (var comp in EntityManager.GetComponents(step))
        {
            if (comp is not (SurgeryStepComponent or SurgeryDamageChangeEffectComponent or SurgeryStepPainInflicterComponent))
                types.Add(comp.GetType());
        }

        return types;
    }

    private HashSet<(EntityUid Target, Type Type, bool Add)> GetChanges(EntityUid body, EntityUid part, SurgeryStepComponent step)
    {
        var changes = new HashSet<(EntityUid, Type, bool)>();
        AddChanges(changes, part, step.Add, true);
        AddChanges(changes, part, step.Remove, false);
        AddChanges(changes, body, step.BodyAdd, true);
        AddChanges(changes, body, step.BodyRemove, false);
        return changes;
    }

    private void AddChanges(HashSet<(EntityUid, Type, bool)> changes, EntityUid target, ComponentRegistry? registry, bool add)
    {
        if (registry == null)
            return;

        foreach (var entry in registry.Values)
        {
            var type = entry.Component.GetType();
            if (EntityManager.HasComponent(target, type) != add)
                changes.Add((target, type, add));
        }
    }

    private bool IsCavityInsert(EntProtoId step)
    {
        return _surgery.GetSingleton(step) is { } stepEnt
            && TryComp(stepEnt, out SurgeryStepCavityEffectComponent? cavity)
            && cavity.Action == "Insert";
    }

    private bool NeedsConfirmation(EntProtoId step)
    {
        return _surgery.GetSingleton(step) is { } stepEnt && HasComp<SurgeryRemovePartStepComponent>(stepEnt);
    }

    public string? GetToolName(EntityUid step)
    {
        if (!TryComp(step, out SurgeryStepComponent? comp) || comp.Tool is not { } tools)
            return null;

        foreach (var registry in tools.Values)
        {
            if (registry.Component is ISurgeryToolComponent tool)
                return tool.ToolName;
        }

        return null;
    }

    // Null when no attach surgery on the part could take a part, otherwise whether one takes the held one.
    private bool? AcceptsHeldPart(EntityUid body,
        EntityUid part,
        (BodyPartType Type, BodyPartSymmetry Symmetry)? missing,
        BodyPartComponent held)
    {
        bool? accepts = null;
        foreach (var surgeryId in _surgery.GetValidSurgeries(body, part))
        {
            if (_surgery.GetSingleton(surgeryId) is not { } surgery
                || !TryComp(surgery, out SurgeryPartRemovedConditionComponent? removed)
                || missing is { } slot && !AttachesPart(surgery, slot.Type, slot.Symmetry))
                continue;

            if (removed.Part == held.PartType && (removed.Symmetry == null || removed.Symmetry == held.Symmetry))
                return true;

            accepts = false;
        }

        return accepts;
    }

    private bool AttachesPart(EntityUid surgery, BodyPartType type, BodyPartSymmetry symmetry)
    {
        return TryComp(surgery, out SurgeryPartRemovedConditionComponent? removed)
            && removed.Part == type
            && (removed.Symmetry == null || removed.Symmetry == symmetry);
    }

    // Healing items double as surgery tools but have their own use on a patient.
    private bool IsSurgicalTool(EntityUid tool)
    {
        return HasComp<SurgeryToolComponent>(tool) && !HasComp<HealingComponent>(tool);
    }

    // Organs and parts held for insertion must reach their own steps instead of a cavity implant.
    private bool CanUseWithoutTool(EntityUid held)
    {
        return !IsSurgicalTool(held) && !HasComp<OrganComponent>(held) && !HasComp<BodyPartComponent>(held);
    }

    private bool IsOffering(EntityUid user)
    {
        return TryComp(user, out OfferItemComponent? offer) && offer.IsInOfferMode;
    }

    private bool IsButchering(EntityUid body, EntityUid tool)
    {
        return HasComp<SharpComponent>(tool)
            && TryComp(body, out ButcherableComponent? butcherable)
            && butcherable.Type == ButcheringType.Knife
            && _mobState.IsDead(body);
    }
}

```
