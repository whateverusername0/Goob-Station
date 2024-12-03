namespace Content.Shared._Lavaland.Mobs;

/// <summary>
///     Keeps track of whoever attacked our mob, so that it could prioritize or randomize targets.
/// </summary>
[RegisterComponent]
public sealed partial class AggressorsComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)] public List<EntityUid> Aggressors = new();
}
