namespace Content.Server._Goobstation._Pirates.GameTicking.Rules;

[RegisterComponent]
public sealed partial class ActivePirateRuleComponent : Component
{
    public List<EntityUid> Pirates = new();
    [ViewVariables(VVAccess.ReadWrite)] public float Credits = 0f;
    [ViewVariables(VVAccess.ReadWrite)] public EntityUid? BoundSiphon;
}
