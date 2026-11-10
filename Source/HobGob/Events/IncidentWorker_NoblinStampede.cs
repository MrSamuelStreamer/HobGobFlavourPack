using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace HobGob.Events
{
    /// <summary>E5 Noblin Stampede: the Noblins bolt through the colony, leaving doors held open behind them.</summary>
    public class IncidentWorker_NoblinStampede : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return parms.target is Verse.Map map && Candidates(map).Count >= EventRules.StampedeMinNoblins;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            if (!(parms.target is Verse.Map map))
            {
                return false;
            }
            List<Pawn> candidates = Candidates(map);
            if (candidates.Count < EventRules.StampedeMinNoblins)
            {
                return false;
            }
            List<Pawn> stampeding = new List<Pawn>();
            foreach (Pawn pawn in candidates)
            {
                if (pawn.mindState.mentalStateHandler.TryStartMentalState(
                        HobGobDefOf.MSSG_NoblinStampede, forced: true, forceWake: true, transitionSilently: true))
                {
                    stampeding.Add(pawn);
                }
            }
            if (stampeding.Count == 0)
            {
                return false;
            }
            SendStandardLetter(def.letterLabel, def.letterText, LetterDefOf.ThreatSmall, parms, new LookTargets(stampeding));
            return true;
        }

        private static List<Pawn> Candidates(Verse.Map map)
        {
            return map.mapPawns.FreeColonistsSpawned
                .Where(p => NoblinRegistry.ShouldManage(p) && !p.InMentalState && !p.Downed)
                .ToList();
        }
    }
}
