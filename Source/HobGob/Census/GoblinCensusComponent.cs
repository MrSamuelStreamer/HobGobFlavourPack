using System.Collections.Generic;
using RimWorld;
using Verse;

namespace HobGob.Census
{
    /// <summary>
    /// Counts the colony's goblins once a day at <see cref="CensusRules.CensusHour"/> (home map local time)
    /// and moves the colony between <see cref="GoblinCensusTierDef"/> tiers.
    /// </summary>
    public class GoblinCensusComponent : GameComponent
    {
        private int lastCensusDay = -1;
        private int lastCount;
        private GoblinCensusTierDef currentTier;

        public GoblinCensusComponent(Verse.Game game)
        {
        }

        /// <summary>The tier from the last census. Null before the first census or when no tiers exist.</summary>
        public GoblinCensusTierDef CurrentTier => currentTier;

        /// <summary>Goblins counted at the last census.</summary>
        public int LastCount => lastCount;

        public override void GameComponentTick()
        {
            if (Find.TickManager.TicksGame % GenDate.TicksPerHour != 0)
            {
                return;
            }
            Map map = Find.AnyPlayerHomeMap;
            if (map == null)
            {
                return;
            }
            int day = CensusRules.DayStamp(GenLocalDate.Year(map), GenLocalDate.DayOfYear(map), GenDate.DaysPerYear);
            if (!CensusRules.IsDue(GenLocalDate.HourOfDay(map), CensusRules.CensusHour, day, lastCensusDay))
            {
                return;
            }
            lastCensusDay = day;
            RunCensus();
        }

        /// <summary>Counts goblins, updates the tier and tells the player. Public so it can be run from the dev console.</summary>
        public void RunCensus()
        {
            List<GoblinCensusTierDef> tiers = GoblinCensusTierDef.Ordered;
            if (tiers.Count == 0)
            {
                return;
            }
            List<int> mins = GoblinCensusTierDef.OrderedMins;
            int count = CountGoblins();
            int previousIndex = currentTier == null ? 0 : tiers.IndexOf(currentTier);
            if (previousIndex < 0)
            {
                previousIndex = 0;
            }
            int newIndex = CensusRules.TierIndexFor(count, mins);
            GoblinCensusTierDef tier = tiers[newIndex];
            lastCount = count;
            currentTier = tier;

            string untilNext = UntilNextLine(count, newIndex, tiers, mins);
            switch (CensusRules.Compare(previousIndex, newIndex))
            {
                case CensusChange.Promoted:
                    SendLetter("MSSG_CensusPromotedLabel", tier.promotedText, tier, count, untilNext, LetterDefOf.PositiveEvent);
                    break;
                case CensusChange.Demoted:
                    SendLetter("MSSG_CensusDemotedLabel", tier.demotedText, tier, count, untilNext, LetterDefOf.NegativeEvent);
                    break;
                default:
                    Messages.Message("MSSG_CensusNoChange".Translate(count.ToString(), tier.label, untilNext),
                        MessageTypeDefOf.SilentInput, historical: false);
                    break;
            }
        }

        private static void SendLetter(string labelKey, string tierText, GoblinCensusTierDef tier, int count, string untilNext, LetterDef letterDef)
        {
            TaggedString label = labelKey.Translate(tier.label);
            TaggedString body = "MSSG_CensusLetterBody".Translate(tierText, count.ToString(), untilNext);
            Find.LetterStack.ReceiveLetter(label, body, letterDef);
        }

        private static string UntilNextLine(int count, int tierIndex, List<GoblinCensusTierDef> tiers, List<int> mins)
        {
            int? more = CensusRules.MoreUntilNext(count, tierIndex, mins);
            return more == null
                ? "MSSG_CensusTopTier".Translate()
                : "MSSG_CensusUntilNext".Translate(more.Value.ToString(), tiers[tierIndex + 1].label);
        }

        public static int CountGoblins()
        {
            int count = 0;
            foreach (Pawn pawn in PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_OfPlayerFaction)
            {
                if (pawn.RaceProps.Humanlike && pawn.genes?.Xenotype != null && pawn.genes.Xenotype.HasModExtension<CensusCountedExtension>())
                {
                    count++;
                }
            }
            return count;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref lastCensusDay, "lastCensusDay", -1);
            Scribe_Values.Look(ref lastCount, "lastCount");
            Scribe_Defs.Look(ref currentTier, "currentTier");
        }
    }
}
