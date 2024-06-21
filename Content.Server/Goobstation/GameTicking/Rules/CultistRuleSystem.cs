using Content.Server.Antag;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.Goobstation.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Server.RoundEnd;
using Content.Shared.GameTicking.Components;
using Content.Shared.Goobstation.Cult.Components;
using Content.Shared.Mind;
using Content.Shared.Mindshield.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server.Goobstation.GameTicking.Rules;

public sealed partial class CultistRuleSystem : GameRuleSystem<CultistRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly SharedRoleSystem _roleSystem = default!;

    public ProtoId<AntagPrototype> CultistPrototypeId = "Cultist";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultistRuleComponent, AfterAntagEntitySelectedEvent>(AfterEntitySelected);
    }

    protected override void Started(EntityUid uid, CultistRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);
    }

    protected override void ActiveTick(EntityUid uid, CultistRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);
        if (NarsieSummoned())
        {
            _roundEnd.DoRoundEndBehavior(RoundEndBehavior.InstantEnd, TimeSpan.FromMinutes(3));
            GameTicker.EndGameRule(uid, gameRule);
        }
        else if (AllCultistsDead())
        {
            //_roundEnd.DoRoundEndBehavior(RoundEndBehavior.ShuttleCall, TimeSpan.FromMinutes(5));
            //GameTicker.EndGameRule(uid, gameRule);
        }
    }

    protected override void AppendRoundEndText(EntityUid uid, CultistRuleComponent component, GameRuleComponent gameRule, ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(uid, component, gameRule, ref args);

        if (NarsieSummoned())
        {
            args.AddLine(Loc.GetString("cult-roundend-win"));
        }
        if (AllCultistsDead())
        {
            args.AddLine(Loc.GetString("cult-roundend-lose"));
        }
    }

    private void AfterEntitySelected(Entity<CultistRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        MakeCultist(args.EntityUid, ent);
    }

    public void MakeCultist(EntityUid uid, bool force = false)
    {
        foreach (var q in EntityQuery<CultistRuleComponent>())
            MakeCultist(uid, q, force);
    }

    public bool MakeCultist(EntityUid uid, CultistRuleComponent comp, bool force = false)
    {
        if (!_mindSystem.TryGetMind(uid, out var mindId, out var mind))
            return false;

        if (HasComp<MindShieldComponent>(uid) && force)
            RemCompDeferred<MindShieldComponent>(uid);

        _antag.SendBriefing(uid, "\n" + Loc.GetString("cultist-role-greeting"), Color.Crimson, comp.BriefingSound);
        comp.Cultists.Add(mindId);

        _roleSystem.MindAddRole(mindId, new RoleBriefingComponent
        {
            Briefing = Loc.GetString("cultist-role-briefing")
        }, mind, true);

        _npcFaction.RemoveFaction(uid, "Nanotrasen", false);
        _npcFaction.AddFaction(uid, "Cultist");

        return true;
    }

    public bool NarsieSummoned()
        => EntityQuery<GeometerComponent>().Count() > 0;
    public bool AllCultistsDead()
        => EntityQuery<CultistComponent>().Count() == 0;
}
