using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.MODSuit;

/// <summary>
///     Makes the MODSuit spawn and equip it's items upon interaction.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MODSuitToggleableClothingComponent : Component
{
    /// <summary>
    ///     Prototypes that will be spawned and attached on toggle.
    ///     String is for inventory slot, e.g. head, outerClothing.
    /// </summary>
    [DataField] public Dictionary<string, EntProtoId>? Prototypes;
}
