#nullable enable
using System;
using System.Collections.Generic;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace WisteriaTreeMod
{
    /// <summary>
    /// Companion SMAPI mod for the Wisteria Tree content pack.
    /// - Hides the fruit overlay on Wisteria trees (the tree sprite already has flowers baked in).
    /// - Caps fruit at 1 per day so it doesn't stack up to the vanilla limit of 3.
    /// - Optionally fast-grows Wisteria trees (one growth stage per day).
    /// </summary>
    public class ModEntry : Mod
    {
        /// <summary>The fruit tree ID for the Wisteria tree (matches the sapling key in Data/FruitTrees).</summary>
        private const string WisteriaTreeId = "Custom_WisteriaSapling";

        /// <summary>Current mod configuration (loaded from config.json).</summary>
        private ModConfig Config = null!;

        public override void Entry(IModHelper helper)
        {
            // Load config
            this.Config = helper.ReadConfig<ModConfig>();

            // Harmony patches: hide fruit sprites drawn on the tree canopy
            var harmony = new Harmony(this.ModManifest.UniqueID);
            harmony.Patch(
                original: AccessTools.Method(typeof(FruitTree), "draw", new[] { typeof(SpriteBatch) }),
                prefix: new HarmonyMethod(typeof(ModEntry), nameof(Before_FruitTree_Draw)),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(After_FruitTree_Draw))
            );

            // Cap Wisteria tree fruit at 1 each morning + fast growth
            helper.Events.GameLoop.DayStarted += this.OnDayStarted;

            // Register GMCM config menu when game finishes launching
            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        }

        /*************
        ** GMCM integration
        *************/

        /// <summary>Register the mod's config menu with Generic Mod Config Menu if available.</summary>
        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            var configMenu = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            // Register the mod
            configMenu.Register(
                mod: this.ModManifest,
                reset: () => this.Config = new ModConfig(),
                save: () => this.Helper.WriteConfig(this.Config)
            );

            // Growth section
            configMenu.AddSectionTitle(
                mod: this.ModManifest,
                text: () => "Growth"
            );

            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Fast Growth",
                tooltip: () => "Wisteria trees advance one growth stage each day instead of the normal season-long growth cycle.",
                getValue: () => this.Config.FastGrowth,
                setValue: value => this.Config.FastGrowth = value
            );
        }

        /*************
        ** Event: cap fruit count + fast growth
        *************/

        /// <summary>Each morning, trim any Wisteria tree's fruit list down to at most 1.</summary>
        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            // Only the host manages terrain features in multiplayer
            if (!Context.IsMainPlayer)
                return;

            Utility.ForEachLocation(delegate(GameLocation location)
            {
                foreach (var pair in location.terrainFeatures.Pairs)
                {
                    if (pair.Value is FruitTree fruitTree
                        && fruitTree.treeId.Value == WisteriaTreeId)
                    {
                        // --- Cap fruit at 1 ---
                        if (fruitTree.fruit.Count > 1)
                        {
                            while (fruitTree.fruit.Count > 1)
                                fruitTree.fruit.RemoveAt(fruitTree.fruit.Count - 1);
                        }

                        // --- Fast growth ---
                        if (this.Config.FastGrowth && fruitTree.growthStage.Value < 4)
                        {
                            fruitTree.growthStage.Value++;
                            fruitTree.daysUntilMature.Value = Math.Max(fruitTree.daysUntilMature.Value - 7, 0);
                        }
                    }
                }
                return true; // continue iterating locations
            });
        }

        /*************
        ** Harmony: hide fruit overlay on tree
        *************/

        /// <summary>Temporarily held fruit items, cleared before draw so nothing renders on the tree.</summary>
        private static List<Item>? _storedFruit;

        /// <summary>
        /// Prefix: if this is a Wisteria tree with fruit, temporarily clear the fruit list
        /// so the vanilla draw method skips the fruit overlay sprites.
        /// </summary>
        private static bool Before_FruitTree_Draw(FruitTree __instance)
        {
            if (__instance.treeId.Value == WisteriaTreeId && __instance.fruit.Count > 0)
            {
                _storedFruit = new List<Item>(__instance.fruit);
                __instance.fruit.Clear();
            }
            else
            {
                _storedFruit = null;
            }

            return true; // always run the original draw method
        }

        /// <summary>
        /// Postfix: restore the fruit list after drawing so game logic (shake, harvest)
        /// still sees the fruit items.
        /// </summary>
        private static void After_FruitTree_Draw(FruitTree __instance)
        {
            if (_storedFruit != null)
            {
                foreach (var item in _storedFruit)
                    __instance.fruit.Add(item);

                _storedFruit = null;
            }
        }
    }
}
