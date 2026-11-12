using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace HobGob.Surgery
{
    /// <summary>
    /// Shared state and checks for goblin surgery (issue #10): no medicine, no anaesthetic,
    /// a flat success chance, and badly attached parts. The Harmony patches call into this.
    /// </summary>
    public static class GoblinSurgery
    {
        private static readonly Dictionary<RecipeDef, List<IngredientCount>> IngredientsWithoutMedicine =
            new Dictionary<RecipeDef, List<IngredientCount>>();

        /// <summary>Installs that failed into a badly attached part, applied once the install has run.</summary>
        private static readonly Dictionary<Pawn, BodyPartRecord> PendingBadlyAttached = new Dictionary<Pawn, BodyPartRecord>();

        /// <summary>Set while vanilla resolves a goblin surgery that must fail. Read by the surgery quality patch.</summary>
        [ThreadStatic]
        public static bool ForceFailure;

        /// <summary>Set while a goblin's surgery starts, so the patient is not anaesthetised.</summary>
        [ThreadStatic]
        public static bool SkipAnaesthetic;

        /// <summary>Any goblin xenotype. Every goblin stage carries an evolution gene.</summary>
        public static bool IsGoblin(Pawn pawn)
        {
            List<Gene> genes = pawn?.genes?.GenesListForReading;
            if (genes == null)
            {
                return false;
            }
            for (int i = 0; i < genes.Count; i++)
            {
                if (genes[i] is Gene_GoblinEvolution)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsGoblinPatient(Bill bill) =>
            bill is Bill_Medical && bill.billStack?.billGiver is Pawn patient && IsGoblin(patient);

        public static bool InstallsPart(RecipeDef recipe) =>
            recipe.Worker is Recipe_InstallNaturalBodyPart || recipe.Worker is Recipe_InstallArtificialBodyPart;

        /// <summary>The recipe's ingredients minus medicine. Same medicine test as vanilla's medical care check.</summary>
        public static List<IngredientCount> WithoutMedicine(RecipeDef recipe)
        {
            if (!IngredientsWithoutMedicine.TryGetValue(recipe, out List<IngredientCount> list))
            {
                list = new List<IngredientCount>();
                foreach (IngredientCount ingredient in recipe.ingredients)
                {
                    if (!ingredient.filter.Allows(ThingDefOf.MedicineIndustrial))
                    {
                        list.Add(ingredient);
                    }
                }
                IngredientsWithoutMedicine[recipe] = list;
            }
            return list;
        }

        public static void MarkBadlyAttached(Pawn patient, BodyPartRecord part)
        {
            if (part != null)
            {
                PendingBadlyAttached[patient] = part;
            }
        }

        /// <summary>Adds the badly attached hediff once the install has put the part in.</summary>
        public static void ApplyPending(Pawn patient)
        {
            if (patient == null || !PendingBadlyAttached.TryGetValue(patient, out BodyPartRecord part))
            {
                return;
            }
            PendingBadlyAttached.Remove(patient);
            if (patient.Dead || patient.health.hediffSet.PartIsMissing(part))
            {
                return;
            }
            patient.health.AddHediff(HobGobDefOf.MSSG_BadlyAttached, part);
            if (patient.Faction == Faction.OfPlayerSilentFail)
            {
                Messages.Message("MSSG_PartBadlyAttached".Translate(patient.Named("PAWN"), part.Label),
                    patient, MessageTypeDefOf.NegativeHealthEvent);
            }
        }
    }
}
