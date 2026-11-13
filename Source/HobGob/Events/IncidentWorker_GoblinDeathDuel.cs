using System.Collections.Generic;
using RimWorld;
using Verse;

namespace HobGob.Events
{
    /// <summary>Two adult Noblins, or two adult Goblins, demand a duel to the death.</summary>
    public class IncidentWorker_GoblinDeathDuel : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!(parms.target is Verse.Map map))
            {
                return false;
            }
            GoblinDeathDuelComponent duel = map.GetComponent<GoblinDeathDuelComponent>();
            return duel != null && !duel.Active && PairAvailable(map);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            if (!(parms.target is Verse.Map map))
            {
                return false;
            }
            GoblinDeathDuelComponent duel = map.GetComponent<GoblinDeathDuelComponent>();
            if (duel == null || duel.Active || !TryPickPair(map, out Pawn fighterA, out Pawn fighterB))
            {
                return false;
            }
            duel.Begin(fighterA, fighterB);
            ChoiceLetter_GoblinDeathDuel letter = (ChoiceLetter_GoblinDeathDuel)LetterMaker.MakeLetter(
                def.letterLabel,
                def.letterText.Formatted(fighterA.LabelShort.Named("FIGHTER_A"), fighterB.LabelShort.Named("FIGHTER_B")),
                HobGobDefOf.MSSG_GoblinDeathDuelLetter,
                new LookTargets(new List<Pawn> { fighterA, fighterB }));
            letter.map = map;
            letter.StartTimeout(EventRules.ChallengeAnswerTicks);
            Find.LetterStack.ReceiveLetter(letter);
            return true;
        }

        private static bool PairAvailable(Verse.Map map)
        {
            CountFighters(map, out int noblins, out int goblins);
            return noblins >= 2 || goblins >= 2;
        }

        private static bool TryPickPair(Verse.Map map, out Pawn fighterA, out Pawn fighterB)
        {
            fighterA = null;
            fighterB = null;
            CountFighters(map, out int noblins, out int goblins);
            XenotypeDef caste;
            if (noblins >= 2 && goblins >= 2)
            {
                caste = Rand.Bool ? HobGobDefOf.MSSG_Noblin : HobGobDefOf.MSSG_Goblin;
            }
            else if (noblins >= 2)
            {
                caste = HobGobDefOf.MSSG_Noblin;
            }
            else if (goblins >= 2)
            {
                caste = HobGobDefOf.MSSG_Goblin;
            }
            else
            {
                return false;
            }
            List<Pawn> fighters = FightersOf(map, caste);
            if (fighters.Count < 2)
            {
                return false;
            }
            fighters.Shuffle();
            fighterA = fighters[0];
            fighterB = fighters[1];
            return true;
        }

        private static void CountFighters(Verse.Map map, out int noblins, out int goblins)
        {
            noblins = 0;
            goblins = 0;
            List<Pawn> colonists = map.mapPawns.FreeColonistsSpawned;
            for (int i = 0; i < colonists.Count; i++)
            {
                Pawn pawn = colonists[i];
                if (!CanBeFighter(pawn))
                {
                    continue;
                }
                XenotypeDef xenotype = pawn.genes?.Xenotype;
                if (xenotype == HobGobDefOf.MSSG_Noblin)
                {
                    noblins++;
                }
                else if (xenotype == HobGobDefOf.MSSG_Goblin)
                {
                    goblins++;
                }
            }
        }

        private static List<Pawn> FightersOf(Verse.Map map, XenotypeDef xenotype)
        {
            List<Pawn> fighters = new List<Pawn>();
            List<Pawn> colonists = map.mapPawns.FreeColonistsSpawned;
            for (int i = 0; i < colonists.Count; i++)
            {
                Pawn pawn = colonists[i];
                if (CanBeFighter(pawn) && pawn.genes?.Xenotype == xenotype)
                {
                    fighters.Add(pawn);
                }
            }
            return fighters;
        }

        /// <summary>Free adult colonist, on their feet, not already breaking, and able to fight.</summary>
        private static bool CanBeFighter(Pawn pawn)
        {
            return pawn.Spawned
                && !pawn.Dead
                && !pawn.Downed
                && !pawn.InMentalState
                && pawn.DevelopmentalStage.Adult()
                && !pawn.WorkTagIsDisabled(WorkTags.Violent);
        }
    }
}
