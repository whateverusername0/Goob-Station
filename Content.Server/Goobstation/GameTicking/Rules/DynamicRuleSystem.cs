using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.CCVar;
using Content.Shared.Dataset;
using Content.Shared.EntityTable;
using Content.Shared.GameTicking.Components;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Text;

namespace Content.Server.GameTicking.Rules;

// basically all that it does is.
// generate random amount of points.
// choose random gamerule which can only spawn 1 antag.
// decrease points + spawn gamerule.
// if there's a major gamerule like nukies cancel other major gamemodes out.

// btw this code is god awful.
// a single look at it burns my retinas.
// i do not wish to refactor it.
// all that matters is that it works.
// regards.

public sealed partial class DynamicRuleSystem : GameRuleSystem<DynamicRuleComponent>
{
    [Dependency] private readonly EntityTableSystem _entTable = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly IRobustRandom _rand = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IComponentFactory _compfact = default!;
    [Dependency] private readonly INetConfigurationManager _cfg = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GameRuleAddedEvent>(OnGameRuleAdded);
    }

    #region gamerule processing

    /// <summary>
    ///     Special TG sauce formula thing.
    /// </summary>
    private float LorentzToAmount(float centre = 0f, float scale = 1.8f, float maxThreat = 100f, float interval = 1f)
    {
        var location = (float) ((decimal) _rand.Next(-5, 5) * _rand.Next());
        var lorentzResult = 1 / Math.PI * MathHelper.DegreesToRadians(MathF.Atan((centre - location) / scale)) + .5f;
        var stdThreat = lorentzResult * maxThreat;

        var lowerDeviation = Math.Max(stdThreat * (location - centre) / 5f, 0);
        var upperDeviation = Math.Max(maxThreat - stdThreat * (centre - location) / 5f, 0);

        return (float) Math.Clamp(Math.Round((double) (stdThreat + upperDeviation - lowerDeviation), (int) interval), 0, 100);
    }

    private List<(EntityPrototype, DynamicRulesetComponent)> GetRuleset(ProtoId<DatasetPrototype> dataset)
    {
        var l = new List<(EntityPrototype, DynamicRulesetComponent)>();

        foreach (var rprot in _proto.Index(dataset).Values)
        {
            var ruleset = _proto.Index(rprot);

            if (!ruleset.TryGetComponent<DynamicRulesetComponent>(out var comp, _compfact))
                continue;

            if (comp.Weight == 0)
                continue;

            l.Add((ruleset, comp));
        }
        return l;
    }
    private (EntityPrototype, DynamicRulesetComponent)? WeightedPickRule(List<(EntityPrototype, DynamicRulesetComponent)?> rules)
    {
        // get total weight of all rules
        var sum = 0f;
        foreach (var rule in rules)
            if (rule != null)
                sum += rule.Value.Item2.Weight;

        var accumulated = 0f;

        var rand = _rand.NextFloat() * sum;

        foreach (var rule in rules)
        {
            if (rule == null)
                continue;

            accumulated += rule.Value.Item2.Weight;

            if (accumulated >= rand)
                return rule;
        }

        return null;
    }

    protected override void Added(EntityUid uid, DynamicRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);

        var players = _antag.GetAliveConnectedPlayers(_playerManager.Sessions);

        // calculate max threat
        var lowpopThreshold = (float) _cfg.GetCVar(CCVars.LowpopThreshold.Name);
        var lowpopThreat = MathHelper.Lerp(component.LowpopMaxThreat, component.MaxThreat, players.Count / lowpopThreshold);
        var maxThreat = (players.Count < lowpopThreshold) ? lowpopThreat : component.MaxThreat;
        component.ThreatLevel = _rand.NextFloat(0, maxThreat);

        // distribute budgets
        component.RoundstartBudget = _rand.NextFloat(0, component.ThreatLevel);
        component.MidroundBudget = component.ThreatLevel - component.RoundstartBudget;

        // add rules
        var draftedRules = new List<(EntityPrototype, DynamicRulesetComponent)?>();
        if (component.RoundstartRulesPool != null)
        {
            var roundstartRules = GetRuleset((ProtoId<DatasetPrototype>) component.RoundstartRulesPool);
            foreach (var rule in roundstartRules)
            {
                if (!rule.Item1.TryGetComponent<DynamicRulesetComponent>(out var comp, _compfact)
                || !rule.Item1.TryGetComponent<GameRuleComponent>(out var gamerule, _compfact))
                    continue;

                if (comp.Weight == 0
                || gamerule.MinPlayers > players.Count
                || component.RoundstartBudget < comp.Cost)
                    continue;

                draftedRules.Add(rule);
            }
        }

        // the "jesus christ how does that work??" part starts here.
        // have fun figuring this out.
        // even i forgot how it works.
        var pickedRules = new List<(EntityPrototype, DynamicRulesetComponent)>();
        var roundstartBudgetLeft = component.RoundstartBudget;
        while (roundstartBudgetLeft > 0)
        {
            var ruleset = WeightedPickRule(draftedRules);

            if (ruleset == null)
                // todo write something here
                break;

            var r = ruleset.Value.Item2;
            var rulesetNonNull = ((EntityPrototype, DynamicRulesetComponent)) ruleset;

            var cost = pickedRules.Contains(rulesetNonNull) ? r.ScalingCost : r.Cost;
            if (cost > roundstartBudgetLeft)
            {
                draftedRules[draftedRules.IndexOf(ruleset)] = null;
                continue;
            }

            roundstartBudgetLeft -= cost;
            pickedRules.Add(rulesetNonNull);

            // if one chosen ruleset is high impact we cancel every other high impact ruleset
            if (r.HighImpact)
                foreach (var otherRule in draftedRules)
                    if (otherRule != null && otherRule.Value.Item2.HighImpact)
                        draftedRules[draftedRules.IndexOf(otherRule)] = null;
        }

        // spend budget and start the gamer rule
        foreach (var rule in pickedRules)
        {
            component.RoundstartBudget = Math.Max(component.RoundstartBudget - rule.Item2.Cost, 0);
            _gameTicker.AddGameRule(rule.Item1.ID);
            component.ExecutedRules.Add(rule.Item1.ID);
        }
    }

    #endregion

    #region roundend text

    protected override void AppendRoundEndText(EntityUid uid, DynamicRuleComponent component, GameRuleComponent gameRule, ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(uid, component, gameRule, ref args);
        var sb = new StringBuilder();

        foreach (var dynamicRule in EntityQuery<DynamicRuleComponent>())
        {
            // total threat & points:
            sb.AppendLine(Loc.GetString("dynamic-roundend-totalthreat", ("points", (int) dynamicRule.ThreatLevel)));
            sb.AppendLine(Loc.GetString("dynamic-roundend-points-roundstart", ("points", (int) dynamicRule.RoundstartBudget)));
            sb.AppendLine(Loc.GetString("dynamic-roundend-points-midround", ("points", (int) dynamicRule.MidroundBudget)));

            // executed roundstart gamerules:
            sb.AppendLine($"\n{Loc.GetString("dynamic-roundend-gamerules-title")}");
            sb.AppendLine(GenerateLocalizedGameruleList(component.ExecutedRules));

            // executed midround gamerules: TODO
        }

        args.AppendAtStart(sb.ToString());
    }
    private string GenerateLocalizedGameruleList(List<EntProtoId> executedGameRules)
    {
        var sb = new StringBuilder();

        var grd = new Dictionary<string, (int, float)>();
        foreach (var gamerule in executedGameRules)
        {
            if (!_proto.Index(gamerule).TryGetComponent<DynamicRulesetComponent>(out var dynset, _compfact))
                continue;

            var name = dynset.NameLoc;

            var executed = grd.ContainsKey(name);
            int executedTimes = executed ? grd[name].Item1 + 1 : 1;
            float cost = executed ? grd[name].Item2 + dynset.ScalingCost : dynset.Cost;

            if (executed)
                grd[name] = (executedTimes, cost);
            else grd.Add(name, (executedTimes, cost));
        }
        foreach (var gr in grd)
            sb.AppendLine($"{Loc.GetString(gr.Key)} (x{grd[gr.Key].Item1}) - {Loc.GetString("dynamic-gamerule-threat-perrule", ("num", grd[gr.Key].Item2))}");

        return sb.ToString();
    }

    #endregion

    #region events

    private void OnGameRuleAdded(ref GameRuleAddedEvent args)
    {
        // nothing goes unnoticed
        foreach (var dgr in EntityQuery<DynamicRuleComponent>())
            dgr.ExecutedRules.Add(args.RuleId);
    }

    #endregion
}
