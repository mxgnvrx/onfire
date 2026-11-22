using Content.Shared._Shitmed.Body.Organ;
using Content.Shared.Actions;
using Content.Shared.Body.Events;
using Content.Shared.Body.Organ;
using Content.Shared.Humanoid;
using Content.Shared.Toggleable;
using Robust.Shared.Timing;

namespace Content.Shared._Arcane.Body.Organs;

public sealed class GlowCyberneticEyesSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedPointLightSystem _light = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GlowCyberneticEyesComponent, OrganComponentsModifyEvent>(OnOrganModified);
        SubscribeLocalEvent<GlowCyberneticEyesComponent, OrganAddedToBodyEvent>(OnAddedToBody);
        SubscribeLocalEvent<GlowCyberneticEyesComponent, OrganRemovedFromBodyEvent>(OnRemovedFromBody);
        SubscribeLocalEvent<GlowCyberneticEyesComponent, ToggleActionEvent>(OnToggle);
    }

    private void OnAddedToBody(Entity<GlowCyberneticEyesComponent> ent, ref OrganAddedToBodyEvent args)
    {
        if (Comp<OrganComponent>(ent).Enabled)
            SetBodyEnabled(ent, args.Body, true);
    }

    private void OnRemovedFromBody(Entity<GlowCyberneticEyesComponent> ent, ref OrganRemovedFromBodyEvent args)
    {
        SetBodyEnabled(ent, args.OldBody, false);
    }

    private void OnOrganModified(Entity<GlowCyberneticEyesComponent> ent, ref OrganComponentsModifyEvent args)
    {
        SetBodyEnabled(ent, args.Body, args.Add);
    }

    private void SetBodyEnabled(Entity<GlowCyberneticEyesComponent> ent, EntityUid body, bool enabled)
    {
        if (_timing.ApplyingState)
            return;

        if (enabled)
        {
            _actions.AddAction(body, ref ent.Comp.Action, ent.Comp.ToggleAction, ent);
            Dirty(ent);
        }
        else
        {
            if (ent.Comp.LightOn)
                RestoreLight(ent, body);
            _actions.RemoveAction(ent.Comp.Action);
        }
    }

    private void RestoreLight(Entity<GlowCyberneticEyesComponent> ent, EntityUid body)
    {
        if (ent.Comp.HadLight)
        {
            _light.SetRadius(body, ent.Comp.PreviousRadius);
            _light.SetEnergy(body, ent.Comp.PreviousEnergy);
            _light.SetColor(body, ent.Comp.PreviousColor);
            _light.SetEnabled(body, ent.Comp.PreviousEnabled);
        }
        else
            _light.SetEnabled(body, false);

        ent.Comp.LightOn = false;
        _actions.SetToggled(ent.Comp.Action, false);
        Dirty(ent);
    }

    private void OnToggle(Entity<GlowCyberneticEyesComponent> ent, ref ToggleActionEvent args)
    {
        if (_timing.ApplyingState || args.Handled || !TryComp<OrganComponent>(ent, out var organ) || !organ.Enabled
            || organ.Body != args.Performer)
            return;

        args.Handled = true;
        if (ent.Comp.LightOn)
        {
            RestoreLight(ent, args.Performer);
            return;
        }

        ent.Comp.HadLight = _light.TryGetLight(args.Performer, out var previous);
        if (previous != null)
        {
            ent.Comp.PreviousEnabled = previous.Enabled;
            ent.Comp.PreviousRadius = previous.Radius;
            ent.Comp.PreviousEnergy = previous.Energy;
            ent.Comp.PreviousColor = previous.Color;
        }

        var light = _light.EnsureLight(args.Performer);
        _light.SetRadius(args.Performer, ent.Comp.Radius, light);
        _light.SetEnergy(args.Performer, ent.Comp.Energy, light);
        if (TryComp<HumanoidAppearanceComponent>(args.Performer, out var appearance))
            _light.SetColor(args.Performer, appearance.EyeColor, light);
        _light.SetEnabled(args.Performer, true, light);
        ent.Comp.LightOn = true;
        _actions.SetToggled(ent.Comp.Action, true);
        Dirty(ent);
    }
}
