using Content.Shared._Lavaland.Weapons;
using Robust.Client.GameObjects;

namespace Content.Client._Lavaland.Weapons;

public sealed class WeaponAttachmentSystem : SharedWeaponAttachmentSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WeaponAttachmentComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleStateEvent);
    }
    private void OnAfterAutoHandleStateEvent(EntityUid uid, WeaponAttachmentComponent component, ref AfterAutoHandleStateEvent args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        sprite.LayerSetVisible(WeaponVisualLayers.Bayonet, component.BayonetAttached);
        sprite.LayerSetVisible(WeaponVisualLayers.FlightOff, component.LightAttached && !component.LightOn);
        sprite.LayerSetVisible(WeaponVisualLayers.FlightOn, component.LightAttached && component.LightOn);
    }
}