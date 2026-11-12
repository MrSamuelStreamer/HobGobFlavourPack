using System.Collections.Generic;
using RimWorld;
using Verse;

namespace HobGob.Surgery
{
    /// <summary>One harvestable body part. <see cref="thing"/> is a defName so it can point at another mod's item.</summary>
    public class HarvestablePart
    {
        public BodyPartDef part;

        /// <summary>Item dropped when the part comes out clean. Empty means the part's own spawnThingOnRemoved.</summary>
        public string thing;
    }

    public class HarvestablePartsExtension : DefModExtension
    {
        public List<HarvestablePart> parts = new List<HarvestablePart>();

        /// <summary>The item a clean harvest of <paramref name="partDef"/> drops, or null if it cannot be harvested.</summary>
        public ThingDef ItemFor(BodyPartDef partDef)
        {
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i].part != partDef)
                {
                    continue;
                }
                return string.IsNullOrEmpty(parts[i].thing)
                    ? partDef.spawnThingOnRemoved
                    : DefDatabase<ThingDef>.GetNamedSilentFail(parts[i].thing);
            }
            return null;
        }
    }

    /// <summary>
    /// Chops a listed part off or out of a goblin and drops it as an item.
    /// Vanilla removal already drops organs with spawnThingOnRemoved; limbs use the extension's item.
    /// </summary>
    public class Recipe_HarvestGoblinPart : Recipe_RemoveBodyPart
    {
        private HarvestablePartsExtension Parts => recipe.GetModExtension<HarvestablePartsExtension>();

        public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null) =>
            base.AvailableOnNow(thing, part) && thing is Pawn pawn && GoblinSurgery.IsGoblin(pawn);

        public override IEnumerable<BodyPartRecord> GetPartsToApplyOn(Pawn pawn, RecipeDef recipeDef)
        {
            HarvestablePartsExtension ext = recipeDef.GetModExtension<HarvestablePartsExtension>();
            if (ext == null)
            {
                yield break;
            }
            foreach (BodyPartRecord part in pawn.health.hediffSet.GetNotMissingParts())
            {
                if (ext.ItemFor(part.def) != null && !pawn.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(part))
                {
                    yield return part;
                }
            }
        }

        /// <summary>
        /// Vanilla removal, with the harvested item dropped only on a successful cut. A botched surgery can
        /// destroy the part too, and the missing-part hediff cannot tell the two apart, so this mirrors
        /// <see cref="Recipe_RemoveBodyPart.ApplyOnPawn"/> instead of calling it.
        /// </summary>
        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            bool clean = MedicalRecipesUtility.IsClean(pawn, part);
            bool violation = IsViolationOnPawn(pawn, part, Faction.OfPlayer);
            if (billDoer != null)
            {
                if (CheckSurgeryFail(billDoer, pawn, ingredients, part, bill))
                {
                    return;
                }
                TaleRecorder.RecordTale(TaleDefOf.DidSurgery, billDoer, pawn);
                // Organs drop their own item; listed limbs drop the extension's item. Parts with added parts are never offered.
                MedicalRecipesUtility.SpawnNaturalPartIfClean(pawn, part, billDoer.Position, billDoer.Map);
                ThingDef item = Parts?.ItemFor(part.def);
                if (clean && item != null && part.def.spawnThingOnRemoved == null)
                {
                    GenPlace.TryPlaceThing(ThingMaker.MakeThing(item), billDoer.Position, billDoer.Map, ThingPlaceMode.Near);
                }
            }
            DamagePart(pawn, part);
            pawn.Drawer.renderer.SetAllGraphicsDirty();
            if (clean)
            {
                ApplyThoughts(pawn, billDoer);
            }
            if (violation)
            {
                ReportViolation(pawn, billDoer, pawn.HomeFaction, -70);
            }
        }

        public override string GetLabelWhenUsedOn(Pawn pawn, BodyPartRecord part) => recipe.label;
    }
}
