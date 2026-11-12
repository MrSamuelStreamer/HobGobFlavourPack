using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace HobGob.Surgery
{
    /// <summary>Goblin patients need no medicine: the bill's ingredient search skips the medicine entries.</summary>
    [HarmonyPatch(typeof(WorkGiver_DoBill), "TryFindBestIngredientsInSet_NoMixHelper")]
    public static class Patch_WorkGiver_DoBill_NoMedicineForGoblins
    {
        public static void Prefix(ref List<IngredientCount> ingredients, Bill bill)
        {
            if (bill != null && GoblinSurgery.IsGoblinPatient(bill))
            {
                ingredients = GoblinSurgery.WithoutMedicine(bill.recipe);
            }
        }
    }

    /// <summary>Goblin patients stay awake: blocks the anaesthetic while a goblin's surgery starts.</summary>
    [HarmonyPatch(typeof(Bill_Medical), nameof(Bill_Medical.Notify_BillWorkStarted))]
    public static class Patch_Bill_Medical_NoAnaesthetic
    {
        public static void Prefix(Bill_Medical __instance)
        {
            GoblinSurgery.SkipAnaesthetic = GoblinSurgery.IsGoblinPatient(__instance);
        }

        public static void Finalizer()
        {
            GoblinSurgery.SkipAnaesthetic = false;
        }
    }

    [HarmonyPatch(typeof(HealthUtility), nameof(HealthUtility.TryAnesthetize))]
    public static class Patch_HealthUtility_TryAnesthetize
    {
        public static bool Prefix(ref bool __result)
        {
            if (!GoblinSurgery.SkipAnaesthetic)
            {
                return true;
            }
            __result = false;
            return false;
        }
    }

    /// <summary>
    /// Every surgery on a goblin: flat success chance. On failure, an install may go in badly attached;
    /// otherwise vanilla picks the failure outcome, with the quality forced to 0 so it cannot succeed.
    /// </summary>
    [HarmonyPatch(typeof(Recipe_Surgery), "CheckSurgeryFail")]
    public static class Patch_Recipe_Surgery_CheckSurgeryFail
    {
        public static bool Prefix(Recipe_Surgery __instance, Pawn patient, BodyPartRecord part, ref bool __result)
        {
            RecipeDef recipe = __instance.recipe;
            if (recipe?.surgeryOutcomeEffect == null || !GoblinSurgery.IsGoblin(patient))
            {
                return true;
            }
            switch (SurgeryRules.Roll(Rand.Value, Rand.Value, GoblinSurgery.InstallsPart(recipe)))
            {
                case GoblinSurgeryResult.Success:
                    __result = false;
                    return false;
                case GoblinSurgeryResult.BadlyAttached:
                    GoblinSurgery.MarkBadlyAttached(patient, part);
                    __result = false;
                    return false;
                default:
                    GoblinSurgery.ForceFailure = true;
                    return true;
            }
        }

        public static void Finalizer()
        {
            GoblinSurgery.ForceFailure = false;
        }
    }

    [HarmonyPatch(typeof(SurgeryOutcomeEffectDef), nameof(SurgeryOutcomeEffectDef.GetQuality))]
    public static class Patch_SurgeryOutcomeEffectDef_GetQuality
    {
        public static void Postfix(ref float __result)
        {
            if (GoblinSurgery.ForceFailure)
            {
                __result = 0f;
            }
        }
    }

    /// <summary>Adds the badly attached hediff after the install has put the part in.</summary>
    [HarmonyPatch(typeof(Bill_Medical), nameof(Bill_Medical.Notify_IterationCompleted))]
    public static class Patch_Bill_Medical_Notify_IterationCompleted
    {
        public static void Prefix(Bill_Medical __instance, out Pawn __state)
        {
            __state = __instance.billStack?.billGiver as Pawn;
        }

        public static void Postfix(Pawn __state)
        {
            GoblinSurgery.ApplyPending(__state);
        }
    }

    /// <summary>"Harvest part" gizmo on player Noblins: pick a part, and a doctor comes to chop it off.</summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public static class Patch_Pawn_GetGizmos_HarvestPart
    {
        private static Texture2D icon;

        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn __instance)
        {
            foreach (Gizmo gizmo in __result)
            {
                yield return gizmo;
            }
            if (__instance.Dead || !__instance.Spawned || __instance.Faction != Faction.OfPlayerSilentFail || !NoblinRegistry.IsNoblin(__instance))
            {
                yield break;
            }
            icon ??= ContentFinder<Texture2D>.Get("UI/Icons/Gobbo_HarvestPart");
            RecipeDef recipe = HobGobDefOf.MSSG_HarvestGoblinPart;
            Pawn pawn = __instance;
            yield return new Command_Action
            {
                defaultLabel = "MSSG_HarvestPartGizmo".Translate(),
                defaultDesc = "MSSG_HarvestPartGizmoDesc".Translate(),
                icon = icon,
                action = () => Find.WindowStack.Add(new FloatMenu(PartOptions(pawn, recipe))),
            };
        }

        private static List<FloatMenuOption> PartOptions(Pawn pawn, RecipeDef recipe)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            if (recipe.Worker.AvailableOnNow(pawn))
            {
                foreach (BodyPartRecord part in recipe.Worker.GetPartsToApplyOn(pawn, recipe).Where(p => recipe.Worker.AvailableOnNow(pawn, p)))
                {
                    BodyPartRecord target = part;
                    options.Add(new FloatMenuOption(part.LabelCap, () => HealthCardUtility.CreateSurgeryBill(pawn, recipe, target)));
                }
            }
            if (options.Count == 0)
            {
                options.Add(new FloatMenuOption("MSSG_HarvestPartNone".Translate(), null));
            }
            return options;
        }
    }
}

namespace HobGob.Surgery
{
    /// <summary>
    /// Goblins nibble harvested body parts whatever their food policy says. Default food policies are built
    /// from food categories, so body parts are not in them, and other pawns keep leaving them for transplants.
    /// </summary>
    public static class GoblinFood
    {
        private static ThingCategoryDef bodyPartsNatural;

        public static bool IsEdibleBodyPart(ThingDef def)
        {
            bodyPartsNatural ??= DefDatabase<ThingCategoryDef>.GetNamedSilentFail("BodyPartsNatural");
            return def != null && def.IsIngestible && def.thingCategories != null && def.thingCategories.Contains(bodyPartsNatural);
        }

        public static bool GoblinMayEat(Pawn eater, ThingDef food) =>
            IsEdibleBodyPart(food) && GoblinSurgery.IsGoblin(eater) && eater.FoodIsSuitable(food);
    }

    [HarmonyPatch(typeof(FoodUtility), nameof(FoodUtility.WillEat), typeof(Pawn), typeof(ThingDef), typeof(Pawn), typeof(bool), typeof(bool))]
    public static class Patch_FoodUtility_WillEatDef_GoblinParts
    {
        public static void Postfix(Pawn p, ThingDef food, ref bool __result)
        {
            if (!__result && GoblinFood.GoblinMayEat(p, food))
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(FoodUtility), nameof(FoodUtility.WillEat), typeof(Pawn), typeof(Thing), typeof(Pawn), typeof(bool), typeof(bool))]
    public static class Patch_FoodUtility_WillEatThing_GoblinParts
    {
        public static void Postfix(Pawn p, Thing food, ref bool __result)
        {
            if (!__result && food != null && GoblinFood.GoblinMayEat(p, food.def))
            {
                __result = true;
            }
        }
    }
}
