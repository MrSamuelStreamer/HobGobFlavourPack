using System.Collections.Generic;
using RimWorld;
using Verse;

namespace HobGob.Events
{
    /// <summary>
    /// E3 Noblin Strike state for one map. While active, NoblinWorkComponent sets every Noblin
    /// work priority to 0. Ends when the tribute is paid at a pit or when time runs out.
    /// </summary>
    public class NoblinStrikeComponent : MapComponent
    {
        private const int CheckInterval = 250;

        private int strikeUntilTick = -1;
        private float tributeOwed;
        private float tributePaid;

        public NoblinStrikeComponent(Verse.Map map) : base(map)
        {
        }

        public bool Active => strikeUntilTick >= 0;
        public float TributeOwed => tributeOwed;
        public float TributePaid => tributePaid;
        public float TributeRemaining => tributeOwed - tributePaid;

        public void StartStrike(List<Pawn> strikers)
        {
            tributeOwed = EventRules.StrikeTribute(strikers.Count);
            tributePaid = 0f;
            strikeUntilTick = Find.TickManager.TicksGame + EventRules.StrikeDurationTicks;
            foreach (Pawn pawn in strikers)
            {
                if (!pawn.health.hediffSet.HasHediff(HobGobDefOf.MSSG_OnStrike))
                {
                    pawn.health.AddHediff(HobGobDefOf.MSSG_OnStrike);
                }
            }
        }

        /// <summary>Adds tribute value. Ends the strike when the full amount is in.</summary>
        public void Pay(float value)
        {
            if (!Active || value <= 0f)
            {
                return;
            }
            tributePaid += value;
            if (tributePaid >= tributeOwed)
            {
                EndStrike(won: true);
            }
        }

        public override void MapComponentTick()
        {
            if (Active && map.IsHashIntervalTick(CheckInterval) && Find.TickManager.TicksGame >= strikeUntilTick)
            {
                EndStrike(won: false);
            }
        }

        public void EndStrike(bool won)
        {
            strikeUntilTick = -1;
            tributeOwed = 0f;
            tributePaid = 0f;
            ThoughtDef thought = won ? HobGobDefOf.MSSG_StrikeWon : HobGobDefOf.MSSG_StrikeBroken;
            List<Pawn> strikers = new List<Pawn>();
            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                Hediff hediff = pawn.health?.hediffSet?.GetFirstHediffOfDef(HobGobDefOf.MSSG_OnStrike);
                if (hediff == null)
                {
                    continue;
                }
                pawn.health.RemoveHediff(hediff);
                pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(thought);
                strikers.Add(pawn);
            }
            if (won)
            {
                Find.LetterStack.ReceiveLetter(
                    "MSSG_StrikeWonLabel".Translate(),
                    "MSSG_StrikeWonText".Translate(),
                    LetterDefOf.PositiveEvent,
                    new LookTargets(strikers));
            }
            else
            {
                Messages.Message("MSSG_StrikeBrokenMessage".Translate(), new LookTargets(strikers), MessageTypeDefOf.NegativeEvent);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref strikeUntilTick, "strikeUntilTick", -1);
            Scribe_Values.Look(ref tributeOwed, "tributeOwed");
            Scribe_Values.Look(ref tributePaid, "tributePaid");
        }
    }
}
