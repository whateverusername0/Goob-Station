using Content.Server.Chat.Systems;
using Content.Server.DoAfter;
using Content.Server.Pinpointer;
using Content.Server.Popups;
using Content.Server.WhiteDream.BloodCult.Gamerule;
using Content.Shared.DoAfter;
using Content.Shared.WhiteDream.BloodCult.Runes;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.WhiteDream.BloodCult.Runes.Rending;

public sealed class CultRuneRendingSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly BloodCultRuleSystem _cultRule = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CultRuneRendingComponent, TryInvokeCultRuneEvent>(OnRendingRuneInvoked);
        SubscribeLocalEvent<CultRuneRendingComponent, RendingRuneDoAfter>(SpawnNarSie);
    }

    private void OnRendingRuneInvoked(Entity<CultRuneRendingComponent> rune, ref TryInvokeCultRuneEvent args)
    {
        if (!_cultRule.IsObjectiveFinished())
        {
            _popup.PopupEntity(Loc.GetString("cult-rending-target-alive"), rune, args.User);
            args.Cancel();
            return;
        }

        if (rune.Comp.CurrentDoAfter.HasValue)
        {
            _popup.PopupEntity(Loc.GetString("cult-rending-already-summoning"), rune, args.User);
            args.Cancel();
            return;
        }

        var ev = new RendingRuneDoAfter();
        var argsDoAfterEvent = new DoAfterArgs(EntityManager, args.User, rune.Comp.SummonTime, ev, rune)
        {
            BreakOnMove = true
        };

        if (!_doAfter.TryStartDoAfter(argsDoAfterEvent, out rune.Comp.CurrentDoAfter))
        {
            args.Cancel();
            return;
        }

        var message = Loc.GetString(
            "cult-rending-started",
            ("location", FormattedMessage.RemoveMarkupPermissive(_navMap.GetNearestBeaconString(rune.Owner))));
        _chat.DispatchGlobalAnnouncement(
            message,
            "Central Command",
            true,
            colorOverride: Color.DarkRed,
            announcementSound: new SoundPathSpecifier("/Audio/_Goobstation/Ambience/Antag/bloodcult_scribe.ogg"));

        _appearance.SetData(rune, RendingRuneVisuals.Active, true);
        rune.Comp.AudioEntity =
            _audio.PlayGlobal(rune.Comp.SummonAudio, Filter.Broadcast(), false, AudioParams.Default.WithLoop(true));
    }

    private void SpawnNarSie(Entity<CultRuneRendingComponent> rune, ref RendingRuneDoAfter args)
    {
        rune.Comp.CurrentDoAfter = null;
        _audio.Stop(rune.Comp.AudioEntity);
        _appearance.SetData(rune, RendingRuneVisuals.Active, false);

        if (args.Cancelled)
        {
            _chat.DispatchGlobalAnnouncement(
                Loc.GetString("cult-rending-prevented"),
                Loc.GetString("blood-cult-title"),
                false,
                colorOverride: Color.DarkRed);
            return;

        var ev = new BloodCultNarsieSummoned();
        RaiseLocalEvent(ev);
        Spawn(rune.Comp.NarsiePrototype, _transform.GetMapCoordinates(rune));
    }
}
