using Content.Shared._Shitmed.Body.Organ;
using Content.Shared.Actions;
using Content.Shared.Body.Organ;
using Content.Shared.Humanoid;
using Content.Shared.Toggleable;

namespace Content.Shared._Arcane.Body.Organs;

public sealed class GlowCyberneticEyesSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedPointLightSystem _light = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GlowCyberneticEyesComponent, OrganEnabledEvent>(OnEnabled);
        SubscribeLocalEvent<GlowCyberneticEyesComponent, OrganDisabledEvent>(OnDisabled);
        SubscribeLocalEvent<GlowCyberneticEyesComponent, ToggleActionEvent>(OnToggle);
    }

    private void OnEnabled(Entity<GlowCyberneticEyesComponent> ent, ref OrganEnabledEvent args)
    {
        if (args.Organ.Comp.Body is not {} body)
            return;

        ent.Comp.HadLight = _light.TryGetLight(body, out var previous);
        if (previous != null)
        {
            ent.Comp.PreviousEnabled = previous.Enabled;
            ent.Comp.PreviousRadius = previous.Radius;
            ent.Comp.PreviousEnergy = previous.Energy;
            ent.Comp.PreviousColor = previous.Color;
        }

        var light = _light.EnsureLight(body);
        _light.SetEnabled(body, false, light);
        _light.SetRadius(body, ent.Comp.Radius, light);
        _light.SetEnergy(body, ent.Comp.Energy, light);

        _actions.AddAction(body, ref ent.Comp.Action, ent.Comp.ToggleAction, ent);
        _actions.SetToggled(ent.Comp.Action, false);
        ent.Comp.LightOn = false;
        Dirty(ent);
    }

    private void OnDisabled(Entity<GlowCyberneticEyesComponent> ent, ref OrganDisabledEvent args)
    {
        if (args.Organ.Comp.Body is not {} body)
            return;

        _actions.RemoveAction(body, ent.Comp.Action);
        ent.Comp.LightOn = false;
        Dirty(ent);

        if (ent.Comp.HadLight)
        {
            _light.SetRadius(body, ent.Comp.PreviousRadius);
            _light.SetEnergy(body, ent.Comp.PreviousEnergy);
            _light.SetColor(body, ent.Comp.PreviousColor);
            _light.SetEnabled(body, ent.Comp.PreviousEnabled);
        }
        else
            _light.SetEnabled(body, false);
    }

    private void OnToggle(Entity<GlowCyberneticEyesComponent> ent, ref ToggleActionEvent args)
    {
        if (args.Handled || !TryComp<OrganComponent>(ent, out var organ) || !organ.Enabled
            || organ.Body != args.Performer)
            return;

        if (TryComp<HumanoidAppearanceComponent>(args.Performer, out var appearance))
            _light.SetColor(args.Performer, appearance.EyeColor);

        ent.Comp.LightOn = !ent.Comp.LightOn;
        _light.SetEnabled(args.Performer, ent.Comp.LightOn);
        _actions.SetToggled(ent.Comp.Action, ent.Comp.LightOn);
        Dirty(ent);
        args.Handled = true;
    }
}
