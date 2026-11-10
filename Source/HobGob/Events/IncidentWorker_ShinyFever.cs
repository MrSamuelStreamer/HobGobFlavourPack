using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace HobGob.Events
{
    /// <summary>E4 Shiny Fever: every Noblin steals one shiny and hides it in a pit stash.</summary>
    public class IncidentWorker_ShinyFever : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!(parms.target is Verse.Map map))
            {
                return false;
            }
            return Noblins(map).Count >= EventRules.ShinyFeverMinNoblins
                && map.listerThings.ThingsOfDef(HobGobDefOf.MSSG_GoblinHole).Any(t => t.Faction == Faction.OfPlayer)
                && ShinyFinder.ShiniesInHome(map).Any();
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            if (!(parms.target is Verse.Map map))
            {
                return false;
            }
            List<Pawn> noblins = Noblins(map);
            if (noblins.Count < EventRules.ShinyFeverMinNoblins)
            {
                return false;
            }
            foreach (Pawn pawn in noblins)
            {
                if (!pawn.health.hediffSet.HasHediff(HobGobDefOf.MSSG_ShinyFever))
                {
                    pawn.health.AddHediff(HobGobDefOf.MSSG_ShinyFever);
                }
            }
            SendStandardLetter(def.letterLabel, def.letterText, LetterDefOf.NegativeEvent, parms, new LookTargets(noblins));
            return true;
        }

        private static List<Pawn> Noblins(Verse.Map map)
        {
            return map.mapPawns.FreeColonistsSpawned.Where(NoblinRegistry.ShouldManage).ToList();
        }
    }
}
