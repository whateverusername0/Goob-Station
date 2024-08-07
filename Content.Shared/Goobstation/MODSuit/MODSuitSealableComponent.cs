using Robust.Shared.GameStates;

namespace Content.Shared.MODSuit;

/// <summary>
///     Makes the MODSuit related entities "sealed", which provide space resistance.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MODSuitSealableComponent : Component
{
    [DataField] public bool Sealed = false;
}
