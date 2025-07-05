using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace RuinedEggsBecomeUnfertilizedEggs
{
    class RuinedEggsBecomeUnfertilizedEggs : Mod
    {
        public static Harmony harmony;

        public RuinedEggsBecomeUnfertilizedEggs(ModContentPack content) : base(content)
        {
            harmony = new Harmony("AddUnfertilizedEggs");
            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(CompTemperatureRuinable), "DoTicks")]
        static class CompTemperatureRuinable_Patches
        {
            [HarmonyPostfix]
            private static void DoTicks_Postfix(ref CompTemperatureRuinable __instance)
            {
                if (!__instance.Ruined)
                {
                    return;
                }
                IntVec3 position = __instance.parent.Position;
                Map map = __instance.parent.Map;
                Thing thing = new Thing();
                if (__instance.parent.def.defName.Contains("Fertilized"))
                {
                    thing = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed(__instance.parent.def.defName.Replace("Fertilized", "Unfertilized")));
                }
                else
                {
                    thing = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed(__instance.parent.def.defName + "Unfertilized"));
                }
                thing.stackCount = __instance.parent.stackCount;
                __instance.parent.Destroy();
                GenSpawn.Spawn(thing, position, map);
            }
        }

        [HarmonyPatch(typeof(DefGenerator), "GenerateImpliedDefs_PreResolve")]
        static class DefGenerator_Patches
        {
            [HarmonyPostfix]
            private static void GenerateImpliedDefs_PreResolve_Postfix()
            {
                AddEggs();
            }
        }

        private static void AddEggs()
        {
            List<ThingDef> fertEggsList = new List<ThingDef>();
            List<ThingDef> unfertEggsList = new List<ThingDef>();

            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs.ToList())
            {
                if (def.thingCategories != null && def.thingCategories.Contains(ThingCategoryDefOf.EggsFertilized))
                {
                    fertEggsList.Add(def);
                }
                if (def.thingCategories != null && def.thingCategories.Contains(ThingCategoryDefOf.EggsUnfertilized))
                {
                    unfertEggsList.Add(def);
                }
            }

            foreach (ThingDef fertEgg in fertEggsList)
            {
                bool unfertEggExists = false;
                foreach (ThingDef unfertEgg in unfertEggsList)
                {
                    if (fertEgg.defName.Replace("Fertilized", "").Equals(unfertEgg.defName.Replace("Unfertilized", "")))
                    {
                        unfertEggExists = true;
                        break;
                    }
                }
                if (!unfertEggExists)
                {
                    CreateAndAddEggs(fertEgg);
                }
            }
            Log.Message("RuinedEggsBecomeUnfertilizedEggs: Added New Unfertilized Eggs");
        }

        private static void CreateAndAddEggs(ThingDef item)
        {
            ThingDef thingDef = new ThingDef();

            thingDef.drawerType = DrawerType.MapMeshOnly;
            thingDef.resourceReadoutPriority = ResourceCountPriority.Middle;
            thingDef.category = ThingCategory.Item;
            thingDef.thingClass = typeof(ThingWithComps);
            thingDef.useHitPoints = true;
            thingDef.selectable = true;
            thingDef.altitudeLayer = AltitudeLayer.Item;
            thingDef.stackLimit = item.stackLimit;
            thingDef.comps.Add(new CompProperties_Forbiddable());
            CompProperties_Rottable compProperties_Rottable = new CompProperties_Rottable();
            compProperties_Rottable.daysToRotStart = 15f;
            compProperties_Rottable.rotDestroys = true;
            compProperties_Rottable.disableIfHatcher = true;
            thingDef.comps.Add(compProperties_Rottable);
            thingDef.tickerType = TickerType.Rare;
            thingDef.healthAffectsPrice = false;
            thingDef.SetStatBaseValue(StatDefOf.Beauty, -4f);
            thingDef.alwaysHaulable = true;
            thingDef.rotatable = false;
            thingDef.pathCost = item.pathCost;
            thingDef.drawGUIOverlay = true;
            thingDef.socialPropernessMatters = true;
            thingDef.modContentPack = item.modContentPack;
            thingDef.SetStatBaseValue(StatDefOf.MaxHitPoints, item.BaseMaxHitPoints);
            thingDef.SetStatBaseValue(StatDefOf.DeteriorationRate, 2f);
            thingDef.SetStatBaseValue(StatDefOf.Mass, item.BaseMass);
            thingDef.SetStatBaseValue(StatDefOf.Flammability, item.BaseFlammability);
            thingDef.SetStatBaseValue(StatDefOf.Nutrition, 0.25f);
            thingDef.SetStatBaseValue(StatDefOf.FoodPoisonChanceFixedHuman, 0.02f);
            thingDef.SetStatBaseValue(StatDefOf.MarketValue, item.BaseMarketValue *= 0.8f);
            thingDef.thingCategories = new List<ThingCategoryDef>();
            DirectXmlCrossRefLoader.RegisterListWantsCrossRef(thingDef.thingCategories, "EggsUnfertilized", thingDef);
            thingDef.ingestible = new IngestibleProperties();
            thingDef.ingestible.foodType = FoodTypeFlags.AnimalProduct;
            thingDef.ingestible.preferability = FoodPreferability.RawBad;
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(thingDef.ingestible, "tasteThought", ThoughtDefOf.AteRawFood.defName);
            thingDef.ingestible.ingestEffect = EffecterDefOf.EatMeat;
            thingDef.ingestible.ingestSound = SoundDefOf.RawMeat_Eat;
            thingDef.graphicData = new GraphicData();
            thingDef.graphicData.graphicClass = item.graphicData.graphicClass;
            thingDef.graphicData.color = new Color(item.graphicData.color.r, item.graphicData.color.g, item.graphicData.color.b);
            thingDef.graphicData.texPath = item.graphicData.texPath.ToString();
            if (item.defName.Contains("Fertilized"))
            {
                thingDef.defName = item.defName.Replace("Fertilized", "Unfertilized");
            }
            else
            {
                thingDef.defName = item.defName+"Unfertilized";
            }
            thingDef.label = item.label.Replace("fert", "unfert");
            thingDef.description = item.description.Replace("fertilized", "unfertilized");
            thingDef.allowedArchonexusCount = item.allowedArchonexusCount;

            thingDef.ResolveReferences();
            DefGenerator.AddImpliedDef(thingDef);
        }
    }
}
