using System;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace HobGob.Slap
{
    /// <summary>
    /// Quality from something measured around the ritual spot. The value is fixed when the ritual starts, like
    /// vanilla RitualOutcomeComp_RoomStat. Unlike RoomStat it also works outdoors and allows any curve shape.
    /// </summary>
    public abstract class RitualOutcomeComp_SpotMeasure : RitualOutcomeComp_Quality
    {
        public override bool DataRequired => true;

        public override RitualOutcomeComp_Data MakeData() => new RitualOutcomeComp_DataRoomStatCached();

        /// <summary>Raw measure at a cell. Bigger means better quality.</summary>
        protected abstract float Measure(IntVec3 spot, Map map);

        private float Clamped(IntVec3 spot, Map map)
        {
            return GenMath.RoundTo(Mathf.Clamp(Measure(spot, map), 0f, MaxValue), 0.1f);
        }

        public override void Tick(LordJob_Ritual ritual, RitualOutcomeComp_Data data, float progressAmount)
        {
            base.Tick(ritual, data, progressAmount);
            if (data is RitualOutcomeComp_DataRoomStatCached cached && !cached.startingVal.HasValue)
            {
                cached.startingVal = Clamped(ritual.Spot, ritual.Map);
            }
        }

        public override float Count(LordJob_Ritual ritual, RitualOutcomeComp_Data data)
        {
            if (data is RitualOutcomeComp_DataRoomStatCached cached && cached.startingVal.HasValue)
            {
                return cached.startingVal.Value;
            }
            return Clamped(ritual.Spot, ritual.Map);
        }

        public override QualityFactor GetQualityFactor(Precept_Ritual ritual, TargetInfo ritualTarget, RitualObligation obligation, RitualRoleAssignments assignments, RitualOutcomeComp_Data data)
        {
            float x = 0f;
            if (ritualTarget.Map != null)
            {
                x = (data as RitualOutcomeComp_DataRoomStatCached)?.startingVal ?? Clamped(ritualTarget.Cell, ritualTarget.Map);
            }
            float quality = curve.Evaluate(x);
            return new QualityFactor
            {
                label = label.CapitalizeFirst(),
                count = x + " / " + MaxValue,
                qualityChange = ExpectedOffsetDesc(positive: true, quality),
                quality = quality,
                positive = quality > 0f,
                priority = 0f,
            };
        }
    }

    /// <summary>Total filth thickness near the spot. The mucky-er the better.</summary>
    public class RitualOutcomeComp_FilthNearSpot : RitualOutcomeComp_SpotMeasure
    {
        public float radius = 7.9f;

        protected override float Measure(IntVec3 spot, Map map)
        {
            int total = 0;
            foreach (Thing thing in GenRadial.RadialDistinctThingsAround(spot, map, radius, useCenter: true))
            {
                if (thing is Filth filth)
                {
                    total += filth.thickness;
                }
            }
            return total;
        }
    }

    /// <summary>How ugly the spot looks (negative beauty). Pretty places count as zero.</summary>
    public class RitualOutcomeComp_Ugliness : RitualOutcomeComp_SpotMeasure
    {
        protected override float Measure(IntVec3 spot, Map map)
        {
            return Math.Max(0f, -BeautyUtility.AverageBeautyPerceptible(spot, map));
        }
    }

    /// <summary>
    /// Participant count for a ritual that ends early. Vanilla ParticipantCount only counts pawns present for half
    /// of durationTicks, which a short slapping never reaches.
    /// </summary>
    public class RitualOutcomeComp_Thumpers : RitualOutcomeComp_Quality
    {
        public override bool DataRequired => false;

        public override float Count(LordJob_Ritual ritual, RitualOutcomeComp_Data data)
        {
            int count = ritual.assignments.SpectatorsForReading.Count(p => p.Spawned && !p.Dead && p.RaceProps.Humanlike);
            return Math.Min(count, MaxValue);
        }

        public override QualityFactor GetQualityFactor(Precept_Ritual ritual, TargetInfo ritualTarget, RitualObligation obligation, RitualRoleAssignments assignments, RitualOutcomeComp_Data data)
        {
            float x = Math.Min(assignments?.SpectatorsForReading.Count(p => p.RaceProps.Humanlike) ?? 0, MaxValue);
            float quality = curve.Evaluate(x);
            return new QualityFactor
            {
                label = label.CapitalizeFirst(),
                count = x + " / " + MaxValue,
                qualityChange = ExpectedOffsetDesc(positive: true, quality),
                quality = quality,
                positive = quality > 0f,
                priority = 0f,
            };
        }
    }
}
