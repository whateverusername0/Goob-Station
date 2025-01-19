using Content.Server._Goobstation._Pirates.GameTicking.Rules;
using Content.Server._Goobstation._Pirates.Objectives;
using Content.Server.Cargo.Components;
using Content.Server.Cargo.Systems;
using Content.Server.Chat.Systems;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Systems;
using Content.Shared.Cargo.Components;
using Content.Shared.Destructible;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Stacks;
using Robust.Server.GameObjects;

namespace Content.Server._Goobstation._Pirates.Pirates.Siphon;

public sealed partial class ResourceSiphonSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly StationAnchorSystem _anchor = default!;
    [Dependency] private readonly CargoSystem _cargo = default!;
    [Dependency] private readonly PricingSystem _pricing = default!;
    [Dependency] private readonly TransformSystem _xform = default!;
    [Dependency] private readonly MindSystem _mind = default!;

    private float TickTimer = 1f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ResourceSiphonComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ResourceSiphonComponent, InteractHandEvent>(OnInteract);
        SubscribeLocalEvent<ResourceSiphonComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<ResourceSiphonComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<ResourceSiphonComponent, DestructionEventArgs>(OnDestruction);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var eqe = EntityQueryEnumerator<ResourceSiphonComponent>();
        while (eqe.MoveNext(out var uid, out var siphon))
        {
            siphon.ActivationRewindClock -= frameTime;
            if (siphon.ActivationRewindClock <= 0)
            {
                siphon.ActivationRewindClock = siphon.ActivationRewindTime;
                siphon.ActivationPhase = 0; // reset
            }
        }

        TickTimer -= frameTime;
        if (TickTimer <= 0)
        {
            TickTimer = 1;
            eqe = EntityQueryEnumerator<ResourceSiphonComponent>(); // reset it ig
            while (eqe.MoveNext(out var uid, out var siphon))
                Tick((uid, siphon));
        }
    }

    private void Tick(Entity<ResourceSiphonComponent> ent)
    {
        if (ent.Comp.Active)
            ActiveTick(ent);

        SyncWithGamerule(ent);
    }
    private void ActiveTick(Entity<ResourceSiphonComponent> ent)
    {
        if (!GetBank(ent, out var nbank))
            return;

        var bank = nbank!.Value;

        var funds = bank.Comp.Balance - ent.Comp.DrainRate;
        if (funds > 0)
        {
            _cargo.DeductFunds(bank.Comp, (int) ent.Comp.DrainRate);
            UpdateCredits(ent, ent.Comp.DrainRate);
        }
    }

    #region Event Handlers
    private void OnInit(Entity<ResourceSiphonComponent> ent, ref ComponentInit args)
    {
        if (!TryBindRule(ent)) return;
    }

    private void OnInteract(Entity<ResourceSiphonComponent> ent, ref InteractHandEvent args)
    {
        if (ent.Comp.Active) return;

        if (!GetBank(ent, out _))
        {
            _popup.PopupEntity(Loc.GetString("pirate-siphon-activate-fail"), ent, args.User, Shared.Popups.PopupType.Medium);
            return;
        }

        ent.Comp.ActivationPhase += 1;
        if (ent.Comp.ActivationPhase < 3)
        {
            var loc = Loc.GetString($"pirate-siphon-activate-{ent.Comp.ActivationPhase}");
            _popup.PopupEntity(loc, ent, args.User, Shared.Popups.PopupType.LargeCaution);
        }
        else ActivateSiphon(ent);
    }

    private void OnInteractUsing(Entity<ResourceSiphonComponent> ent, ref InteractUsingEvent args)
    {
        if (HasComp<CashComponent>(args.Used))
        {
            var price = _pricing.GetPrice(args.Used);
            if (price == 0) return;

            UpdateCredits(ent, (float) price);
            QueueDel(args.Used);
        }

        // add more stuff here if needed
    }

    private void OnExamine(Entity<ResourceSiphonComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("pirate-siphon-examine", ("num", ent.Comp.Credits)));
    }

    private void OnDestruction(Entity<ResourceSiphonComponent> ent, ref DestructionEventArgs args)
    {
        var speso = Spawn("SpaceCash", Transform(ent).Coordinates);
        if (TryComp<StackComponent>(speso, out var stack))
            stack.Count = (int) ent.Comp.Credits;
    }
    #endregion

    public void ActivateSiphon(Entity<ResourceSiphonComponent> ent)
    {
        ent.Comp.Active = true;

        if (TryComp<StationAnchorComponent>(ent, out var anchor))
            _anchor.SetStatus((ent, anchor), true);

        var coords = _xform.GetWorldPosition(Transform(ent));
        _popup.PopupCoordinates(Loc.GetString("data-siphon-activated"), Transform(ent).Coordinates, Shared.Popups.PopupType.Medium);

        var anloc = Loc.GetString("data-siphon-activated-announcement", ("pos", $"X: {coords.X}; Y: {coords.Y}"));
        _chat.DispatchGlobalAnnouncement(anloc, "Priority", colorOverride: Color.Red);
    }

    public bool TryBindRule(Entity<ResourceSiphonComponent> ent)
    {
        var eqe = EntityQueryEnumerator<ActivePirateRuleComponent>();
        while (eqe.MoveNext(out var ruid, out var rule))
        {
            if (rule.BoundSiphon == null)
            {
                rule.BoundSiphon = ent;
                ent.Comp.BoundGamerule = ruid;
                return true;
            }
        }
        return false;
    }
    public EntityUid? GetRule(Entity<ResourceSiphonComponent> ent)
    {
        if (ent.Comp.BoundGamerule == null)
            TryBindRule(ent);

        return ent.Comp.BoundGamerule;
    }

    public bool SyncWithGamerule(Entity<ResourceSiphonComponent> ent)
    {
        if (GetRule(ent) == null
        || !TryComp<ActivePirateRuleComponent>(ent.Comp.BoundGamerule, out var prule))
            return false;

        prule.Credits = ent.Comp.Credits;

        foreach (var pirate in prule.Pirates)
            UpdateObjective(pirate, ent);

        return true;
    }
    public void UpdateObjective(EntityUid pirate, Entity<ResourceSiphonComponent> siphon)
    {
        if (_mind.TryGetMind(pirate, out var mindId, out var mind))
            if (_mind.TryGetObjectiveComp<ObjectivePlunderComponent>(mindId, out var objective, mind))
                objective.Plundered = siphon.Comp.Credits;
    }

    public void UpdateCredits(Entity<ResourceSiphonComponent> ent, float amount)
    {
        var newAmount = ent.Comp.Credits + amount;
        ent.Comp.Credits = Math.Min(ent.Comp.CreditsThreshold, newAmount);

        if (newAmount > ent.Comp.CreditsThreshold)
        {
            if (ent.Comp.Active)
                ent.Comp.Active = false; // stop siphoning
        }
    }

    private bool GetBank(Entity<ResourceSiphonComponent> ent, out Entity<StationBankAccountComponent>? bank)
    {
        bank = null;
        var stationent = _station.GetStationInMap(Transform(ent).MapID);

        if (stationent == null)
            return false;

        if (!TryComp<StationBankAccountComponent>(stationent, out var bankaccount))
            return false;

        bank = ((EntityUid) stationent!, (StationBankAccountComponent) bankaccount!);
        return true;
    }
}
