using Content.Shared.Goobstation.Cult;
using Content.Shared.Goobstation.Cult.Components;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Goobstation.Cult;

public sealed class CultistSystem : SharedCultistSystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultistComponent, GetStatusIconsEvent>(GetStatusIcon);
    }

    private void GetStatusIcon(Entity<CultistComponent> ent, ref GetStatusIconsEvent args)
    {
        if (_prototype.TryIndex(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }
}
