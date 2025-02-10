﻿using Content.Shared.Damage;
using Content.Shared.Weather;
using Robust.Shared.Prototypes;

namespace Content.Server._Lavaland.Weather;

[Prototype]
public sealed class LavalandWeatherPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    [DataField]
    public float Duration = 150;

    [DataField]
    public float Variety = 20;

    [DataField]
    public ProtoId<WeatherPrototype> WeatherType;

    [DataField]
    public string PopupStartMessage = "You feel like wind starts blowing stronger...";

    [DataField]
    public string PopupEndMessage = "The wind is going out.";

    /// <summary>
    /// Amount of temperature to apply every tick.
    /// Be careful changing this number.
    /// </summary>
    [DataField]
    public float TemperatureChange = 3000f;

    [DataField]
    public DamageSpecifier Damage = new();

    /// <summary>
    /// How many seconds the weather goes without doing any damage
    /// </summary>
    [DataField]
    public int WeatherDelay = 30;
}
