﻿using Content.Shared.Destructible.Thresholds;

namespace Content.Server._Lavaland.Weather.Gamerule;

[RegisterComponent]
public sealed partial class LavalandStormSchedulerRuleComponent : Component
{
    /// <summary>
    ///     How long until the next check for an event runs
    /// </summary>
    [DataField] public float EventClock = 600f; // Ten minutes (no)

    /// <summary>
    ///     How much time it takes in seconds for an antag event to be raised.
    /// </summary>
    [DataField] public MinMax Delays = new(15 * 60, 30 * 60); // actually 25-45 minutes
}
