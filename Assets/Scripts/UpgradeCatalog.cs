using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Catalog of purchasable pre-game upgrades. Indexed by position — matches SaveData.upgradeLevels.
/// Cost of next level = (int)(baseCost * pow(growth, currentLevel)).
/// </summary>
public static class UpgradeCatalog
{
    public enum IconType { Shield, Heart, Magnet, Wrench, Feather, Star, Crosshair }

    public class UpgradeDef
    {
        public string id, name, description;
        public int maxLevel = Infinite;   // purchases never run out
        public int baseCost;
        public IconType icon;

        public const int Infinite = 999;

        /// <summary>Quadratic cost curve — grows forever without overflowing, so upgrades
        /// remain an infinite coin sink for endless runs.</summary>
        public int CostForLevel(int currentLevel) =>
            Mathf.RoundToInt(baseCost * (1f + 0.8f * currentLevel + 0.06f * currentLevel * currentLevel));
    }

    public static readonly UpgradeDef[] All =
    {
        new UpgradeDef
        {
            id = "shield", name = "Bubble Shield",
            description = "Start each run with a protective bubble. Each shield absorbs one crash and pops.",
            maxLevel = UpgradeDef.Infinite, baseCost = 60, icon = IconType.Shield
        },
        new UpgradeDef
        {
            id = "extraLife", name = "Phoenix Feather",
            description = "Revive on the spot after crashing. Feathers are consumed on use.",
            maxLevel = UpgradeDef.Infinite, baseCost = 150, icon = IconType.Heart
        },
        new UpgradeDef
        {
            id = "magnet", name = "Coin Magnet",
            description = "Coins fly into your beak from a distance. Radius grows per level.",
            maxLevel = UpgradeDef.Infinite, baseCost = 80, icon = IconType.Magnet
        },
        new UpgradeDef
        {
            id = "grease", name = "Pipe Grease",
            description = "Slippery pipes widen every gap by +0.5 units per level.",
            maxLevel = UpgradeDef.Infinite, baseCost = 100, icon = IconType.Wrench
        },
        new UpgradeDef
        {
            id = "feather", name = "Feather Light",
            description = "Gentler gravity (-6% per level). Slow, floaty falls are easier to control.",
            maxLevel = UpgradeDef.Infinite, baseCost = 90, icon = IconType.Feather
        },
        new UpgradeDef
        {
            id = "golden", name = "Golden Beak",
            description = "Pipe payouts +15% per level. The streak multiplier compounds on top.",
            maxLevel = UpgradeDef.Infinite, baseCost = 120, icon = IconType.Star
        },
        new UpgradeDef
        {
            id = "sharpshooter", name = "Sharp Shooter",
            description = "Your auto-turret reloads faster (+18% fire rate per level) and deals +1 damage at max.",
            maxLevel = UpgradeDef.Infinite, baseCost = 110, icon = IconType.Crosshair
        },
    };

    public static int Count => All.Length;

    public static int IndexOf(string id)
    {
        for (int i = 0; i < All.Length; i++)
            if (All[i].id == id) return i;
        return -1;
    }
}
