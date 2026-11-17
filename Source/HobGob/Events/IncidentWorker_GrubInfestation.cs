using System.Linq;
using RimWorld;
using Verse;

namespace HobGob.Events
{
    /// <summary>Grubs in me 'ole: for 0.5 to 3 days, every player pit spawns a noblin every 2 hours.</summary>
    public class IncidentWorker_GrubInfestation : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!(parms.target is Verse.Map map))
            {
                return false;
            }
            GrubInfestationComponent grubs = GrubInfestationComponent.For(map);
            return grubs != null && !grubs.Active && HasPlayerPit(map);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            if (!(parms.target is Verse.Map map))
            {
                return false;
            }
            GrubInfestationComponent grubs = GrubInfestationComponent.For(map);
            if (grubs == null || grubs.Active || !HasPlayerPit(map))
            {
                return false;
            }
            int duration = EventRules.GrubDurationTicks(Rand.Range(EventRules.GrubDaysMin, EventRules.GrubDaysMax));
            grubs.Start(duration);
            SendStandardLetter(
                def.letterLabel,
                def.letterText.Formatted(duration.ToStringTicksToPeriod().Named("DURATION")),
                LetterDefOf.PositiveEvent,
                parms,
                new LookTargets(map.listerThings.ThingsOfDef(HobGobDefOf.MSSG_GoblinHole)
                    .Where(t => t.Faction == Faction.OfPlayer)));
            return true;
        }

        private static bool HasPlayerPit(Verse.Map map)
        {
            return map.listerThings.ThingsOfDef(HobGobDefOf.MSSG_GoblinHole).Any(t => t.Faction == Faction.OfPlayer);
        }
    }
}
