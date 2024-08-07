using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.MODSuit;

[RegisterComponent, NetworkedComponent]
public sealed partial class MODSuitComponent : Component
{
    /// <summary>
    ///     Maximum total completixy of all modules in the suit combined.
    ///     Any more and the suit will not accept any more modules.
    /// </summary>
    [DataField] public float MaxModuleComplexity = 15f;
}

[Serializable, NetSerializable]
public enum MODSuitVisualLayers : byte
{
    Base
}
