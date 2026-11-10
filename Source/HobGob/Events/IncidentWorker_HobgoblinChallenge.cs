using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace HobGob.Events
{
    /// <summary>E2 Hobgoblin Challenge: a rival hobgoblin demands a duel with the colony's strongest goblin.</summary>
    public class IncidentWorker_HobgoblinChallenge : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!(parms.target is Verse.Map map))
            {
                return false;
            }
            HobgoblinChallengeComponent challenge = map.GetComponent<HobgoblinChallengeComponent>();
            return challenge != null
                && !challenge.Active
                && ChallengeUtility.CensusAllowsChallenge()
                && ChallengeUtility.BestChampion(map) != null
                && RCellFinder.TryFindRandomPawnEntryCell(out IntVec3 _, map, CellFinder.EdgeRoadChance_Neutral);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            if (!(parms.target is Verse.Map map))
            {
                return false;
            }
            HobgoblinChallengeComponent challenge = map.GetComponent<HobgoblinChallengeComponent>();
            Pawn champion = ChallengeUtility.BestChampion(map);
            if (challenge == null || challenge.Active || champion == null
                || !RCellFinder.TryFindRandomPawnEntryCell(out IntVec3 entry, map, CellFinder.EdgeRoadChance_Neutral))
            {
                return false;
            }
            Faction faction = ChallengeUtility.PickChallengerFaction();
            Pawn challenger = GenerateChallenger(faction, map);
            if (challenger == null)
            {
                return false;
            }
            GenSpawn.Spawn(challenger, entry, map);
            if (!RCellFinder.TryFindRandomSpotJustOutsideColony(challenger, out IntVec3 waitSpot))
            {
                waitSpot = CellFinder.RandomClosewalkCellNear(map.Center, map, 10);
            }
            LordMaker.MakeNewLord(faction, new LordJob_DefendPoint(waitSpot, 4f, 4f, addFleeToil: false), map, new List<Pawn> { challenger });
            challenge.Begin(challenger);

            string factionText = faction != null
                ? "MSSG_ChallengeFromFaction".Translate(faction.Name).ToString()
                : "MSSG_ChallengeNoFaction".Translate().ToString();
            ChoiceLetter_HobgoblinChallenge letter = (ChoiceLetter_HobgoblinChallenge)LetterMaker.MakeLetter(
                def.letterLabel,
                def.letterText.Formatted(challenger.LabelShort.Named("CHALLENGER"), champion.LabelShort.Named("CHAMPION"), factionText.Named("FACTION")),
                HobGobDefOf.MSSG_HobgoblinChallengeLetter,
                new LookTargets(challenger));
            letter.map = map;
            letter.StartTimeout(EventRules.ChallengeAnswerTicks);
            Find.LetterStack.ReceiveLetter(letter);
            return true;
        }

        private static Pawn GenerateChallenger(Faction faction, Verse.Map map)
        {
            PawnKindDef kind = faction?.RandomPawnKind() ?? PawnKindDefOf.Villager;
            if (kind == null || !kind.RaceProps.Humanlike)
            {
                kind = PawnKindDefOf.Villager;
            }
            PawnGenerationRequest request = new PawnGenerationRequest(
                kind,
                faction,
                PawnGenerationContext.NonPlayer,
                map.Tile,
                forceGenerateNewPawn: true,
                canGeneratePawnRelations: false,
                mustBeCapableOfViolence: true,
                forcedXenotype: HobGobDefOf.MSSG_Hobgoblin,
                developmentalStages: DevelopmentalStage.Adult);
            Pawn pawn = PawnGenerator.GeneratePawn(request);
            // D2: the challenger fights unarmed.
            pawn?.equipment?.DestroyAllEquipment();
            pawn?.inventory?.DestroyAll();
            return pawn;
        }
    }
}
