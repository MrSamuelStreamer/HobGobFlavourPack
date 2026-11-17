using System.Collections.Generic;
using RimWorld;
using Verse;

namespace HobGob.Events
{
    /// <summary>Knob Gob Gobbo Gone Wild: a colony-wide social-fight brawl, noblins against goblins.</summary>
    public class IncidentWorker_GobboGoneWild : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!(parms.target is Verse.Map map))
            {
                return false;
            }
            GobBrawlComponent brawl = GobBrawlComponent.For(map);
            return brawl != null
                && !brawl.Active
                && !GobBrawlComponent.OtherFightActive(map)
                && EventRules.BrawlCanStart(
                    GobBrawlComponent.Eligible(map, GobBrawlComponent.IsNoblinSide).Count,
                    GobBrawlComponent.Eligible(map, GobBrawlComponent.IsGoblinSide).Count);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            if (!(parms.target is Verse.Map map))
            {
                return false;
            }
            GobBrawlComponent brawl = GobBrawlComponent.For(map);
            List<Pawn> noblins = GobBrawlComponent.Eligible(map, GobBrawlComponent.IsNoblinSide);
            List<Pawn> goblins = GobBrawlComponent.Eligible(map, GobBrawlComponent.IsGoblinSide);
            if (brawl == null || brawl.Active || !EventRules.BrawlCanStart(noblins.Count, goblins.Count))
            {
                return false;
            }
            List<Pawn> fighters = brawl.Start(noblins, goblins);
            SendStandardLetter(
                def.letterLabel,
                def.letterText.Formatted(
                    brawl.NoblinCount.Named("NOBLINS"),
                    brawl.GoblinCount.Named("GOBLINS")),
                LetterDefOf.NegativeEvent,
                parms,
                new LookTargets(fighters));
            return true;
        }
    }
}
