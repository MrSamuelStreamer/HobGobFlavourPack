using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace HobGob.Slap
{
    /// <summary>Walk up to the target (A), wind up, give them one thump, step back to watch.</summary>
    public class JobDriver_ThumpTarget : JobDriver
    {
        private const TargetIndex TargetInd = TargetIndex.A;
        private const int WindUpTicks = 30;
        private const int FollowThroughTicks = 20;

        private Pawn Target => (Pawn)job.GetTarget(TargetInd).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetInd);
            AddFinishAction(condition => RitualBehaviorWorker_SlapSense.For(pawn, out _)?.EndTurn(pawn));
            yield return Toils_Goto.GotoThing(TargetInd, PathEndMode.Touch);
            yield return Toils_General.Wait(WindUpTicks, TargetInd);
            Toil thump = ToilMaker.MakeToil("Thump");
            thump.initAction = () => Thump(pawn, Target);
            thump.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return thump;
            yield return Toils_General.Wait(FollowThroughTicks, TargetInd);
        }

        /// <summary>Swing and land a small blunt hit, unless it could down or kill the target.</summary>
        private static void Thump(Pawn thumper, Pawn target)
        {
            if (target == null || target.Dead || target.Downed)
            {
                return;
            }
            thumper.Drawer.Notify_MeleeAttackOn(target);
            SoundDefOf.Pawn_Melee_Punch_HitPawn.PlayOneShot(new TargetInfo(target.Position, target.Map));

            int damage = Rand.RangeInclusive(SlapRules.MinThumpDamage, SlapRules.MaxThumpDamage);
            float pain = target.health.hediffSet.PainTotal;
            List<BodyPartRecord> parts = SafeParts(target)
                .Where(p => SlapRules.SafeToThump(pain, target.health.hediffSet.GetPartHealth(p), p.def.GetMaxHealth(target), damage))
                .ToList();
            if (parts.Count == 0)
            {
                return;
            }
            // No instigator: a ritual thump must not start fights, set off retaliation or count as an attack.
            // Weight by coverage like vanilla hit selection, so arms and legs get hit more than fingers and toes.
            BodyPartRecord part = parts.RandomElementByWeight(p => p.coverageAbs + 0.001f);
            var dinfo = new DamageInfo(DamageDefOf.Blunt, damage, 0f, -1f, null, part);
            target.TakeDamage(dinfo);
        }

        /// <summary>
        /// Limbs only: arms, legs, hands, feet. Never the torso, head or neck, and never a part with an organ
        /// inside it, because vanilla blunt damage can pass through to inner parts.
        /// </summary>
        private static IEnumerable<BodyPartRecord> SafeParts(Pawn target)
        {
            return target.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Outside)
                .Where(p => p.parent != null
                    && !p.IsInGroup(BodyPartGroupDefOf.FullHead)
                    && IsLimbTagOnly(p)
                    && !HasOrganInside(p));
        }

        /// <summary>Only limb tags (moving, manipulation, mirrored). Any other tag marks a vital part such as the neck or an organ.</summary>
        private static bool IsLimbTagOnly(BodyPartRecord part)
        {
            return part.def.tags.All(IsLimbTag);
        }

        private static bool IsLimbTag(BodyPartTagDef tag)
        {
            return tag == BodyPartTagDefOf.MovingLimbCore || tag == BodyPartTagDefOf.MovingLimbSegment
                || tag == BodyPartTagDefOf.MovingLimbDigit || tag == BodyPartTagDefOf.ManipulationLimbCore
                || tag == BodyPartTagDefOf.ManipulationLimbSegment || tag == BodyPartTagDefOf.ManipulationLimbDigit
                || tag.defName == "Mirrored";
        }

        /// <summary>True if any inner part below this one is not a limb part (bones like the humerus are fine; organs are not).</summary>
        private static bool HasOrganInside(BodyPartRecord part)
        {
            foreach (BodyPartRecord child in part.GetDirectChildParts())
            {
                if (child.depth == BodyPartDepth.Inside && (!IsLimbTagOnly(child) || HasOrganInside(child)))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
