﻿using Content.Server._Lavaland.Weather;
using Content.Shared.Atmos;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Parallax.Biomes.Markers;
using Content.Shared.Weather;
using Robust.Shared.Prototypes;

namespace Content.Server._Lavaland.Procedural.Prototypes;

/// <summary>
/// Contains information about Lavaland planet configuration.
/// </summary>
[Prototype]
public sealed partial class LavalandMapPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    [DataField] public string Name = "Lavaland Planet";

    [DataField] public string OutpostName = "Lavaland Outpost";

    [DataField]
    public string OutpostPath = "";

    [DataField]
    public float RestrictedRange = 256f;

    [DataField(required: true)]
    public ProtoId<LavalandRuinPoolPrototype> RuinPool;

    #region Atmos

    [DataField]
    public float[] Atmosphere = new float[Atmospherics.AdjustedNumberOfGases];

    [DataField]
    public float Temperature = Atmospherics.T20C;

    [DataField]
    public Color? PlanetColor;

    #endregion

    #region Biomes

    [DataField("biome", required: true)]
    public ProtoId<BiomeTemplatePrototype> BiomePrototype = "Lava";

    [DataField("ore")]
    public List<ProtoId<BiomeMarkerLayerPrototype>> OreLayers = new()
    {
        "OreIron",
        "OreCoal",
        "OreQuartz",
        "OreGold",
        "OreSilver",
        "OrePlasma",
        "OreUranium",
        "OreArtifactFragment",
        "BSCrystal",
    };

    [DataField("mobs")]
    public List<ProtoId<BiomeMarkerLayerPrototype>> MobLayers = [];

    [DataField("weather")]
    public List<ProtoId<LavalandWeatherPrototype>>? AvailableWeather;

    #endregion
}
