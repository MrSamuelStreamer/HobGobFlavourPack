using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace HobGob.Slap
{
    /// <summary>Re-rolls the target's passions. A better ritual means more passion; a terrible one knocks one out.</summary>
    public class RitualOutcomeEffectWorker_SlapSense : RitualOutcomeEffectWorker_FromQuality
    {
        public RitualOutcomeEffectWorker_SlapSense()
        {
        }

        public RitualOutcomeEffectWorker_SlapSense(RitualOutcomeEffectDef def) : base(def)
        {
        }

        protected override void ApplyExtraOutcome(Dictionary<Pawn, int> totalPresence, LordJob_Ritual jobRitual, RitualOutcomePossibility outcome, out string extraOutcomeDesc, ref LookTargets letterLookTargets)
        {
            extraOutcomeDesc = null;
            Pawn target = jobRitual.PawnWithRole(RitualBehaviorWorker_SlapSense.TargetRoleId);
            if (target?.skills == null || target.Dead)
            {
                return;
            }
            letterLookTargets = target;
            if (!target.health.hediffSet.HasHediff(HobGobDefOf.MSSG_RecentlySlapped))
            {
                target.health.AddHediff(HobGobDefOf.MSSG_RecentlySlapped);
            }
            List<SkillRecord> skills = target.skills.skills;
            List<PassionSlot> slots = skills.Select(s => SlotFor(target, s)).ToList();
            int[] rolled = SlapRules.Reroll(slots, outcome.positivityIndex, new System.Random(Rand.Int));

            var changes = new List<string>();
            for (int i = 0; i < skills.Count; i++)
            {
                Passion before = skills[i].passion;
                Passion after = (Passion)rolled[i];
                if (before == after)
                {
                    continue;
                }
                skills[i].passion = after;
                changes.Add("MSSG_SlapPassionChange".Translate(skills[i].def.LabelCap, before.GetLabel(), after.GetLabel()));
            }
            extraOutcomeDesc = changes.Count == 0
                ? "MSSG_SlapNoChange".Translate(target.Named("PAWN")).Resolve()
                : "MSSG_SlapPassionsChanged".Translate(target.Named("PAWN")).Resolve() + "\n" + changes.ToLineList("  - ");
        }

        private static PassionSlot SlotFor(Pawn pawn, SkillRecord skill)
        {
            bool locked = pawn.genes != null && pawn.genes.GenesListForReading.Any(g => g.Active && g.def.passionMod != null && g.def.passionMod.skill == skill.def);
            List<Trait> traits = pawn.story?.traits?.allTraits ?? new List<Trait>();
            bool blocked = skill.TotallyDisabled || traits.Any(t => t.def.ConflictsWithPassion(skill.def));
            bool forced = traits.Any(t => t.def.RequiresPassion(skill.def));
            return new PassionSlot((int)skill.passion, locked, blocked, forced);
        }
    }
}
