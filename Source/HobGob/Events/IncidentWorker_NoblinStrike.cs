using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace HobGob.Events
{
    /// <summary>
    /// E3 Noblin Strike: the Noblins stop work until shinies are left beside a pit, or 3 days pass.
    /// </summary>
    public class IncidentWorker_NoblinStrike : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!(parms.target is Verse.Map map))
            {
                return false;
            }
            NoblinStrikeComponent strike = map.GetComponent<NoblinStrikeComponent>();
            return strike != null
                && !strike.Active
                && HasPlayerPit(map)
                && Strikers(map).Count >= EventRules.StrikeMinNoblins;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            if (!(parms.target is Verse.Map map))
            {
                return false;
            }
            NoblinStrikeComponent strike = map.GetComponent<NoblinStrikeComponent>();
            List<Pawn> strikers = Strikers(map);
            if (strike == null || strike.Active || strikers.Count < EventRules.StrikeMinNoblins)
            {
                return false;
            }
            strike.StartStrike(strikers);
            int days = EventRules.StrikeDurationTicks / GenDate.TicksPerDay;
            SendStandardLetter(
                def.letterLabel,
                def.letterText.Formatted(
                    ((int)strike.TributeOwed).Named("TRIBUTE"),
                    days.Named("DAYS")),
                LetterDefOf.NegativeEvent,
                parms,
                new LookTargets(strikers));
            return true;
        }

        private static List<Pawn> Strikers(Verse.Map map)
        {
            return map.mapPawns.FreeColonistsSpawned.Where(NoblinRegistry.ShouldManage).ToList();
        }

        private static bool HasPlayerPit(Verse.Map map)
        {
            return map.listerThings.ThingsOfDef(HobGobDefOf.MSSG_GoblinHole).Any(t => t.Faction == Faction.OfPlayer);
        }
    }
}
