using System.Collections.Generic;
using RimWorld;
using Verse;

namespace HobGob.Rumble
{
    /// <summary>The Nobby Nobby Rumble ritual is only a gathering. It needs enough noblins and no rumble already on.</summary>
    public class RitualBehaviorWorker_NobbyRumble : RitualBehaviorWorker
    {
        public RitualBehaviorWorker_NobbyRumble()
        {
        }

        public RitualBehaviorWorker_NobbyRumble(RitualBehaviorDef def) : base(def)
        {
        }

        public override string CanStartRitualNow(TargetInfo target, Precept_Ritual ritual, Pawn selectedPawn = null, Dictionary<string, Pawn> forcedForRole = null)
        {
            Map map = target.Map;
            if (map != null)
            {
                if (NobbyRumbleComponent.For(map)?.Active == true)
                {
                    return "MSSG_RumbleAlreadyOn".Translate();
                }
                int eligible = NobbyRumbleComponent.CountEligible(map);
                if (!RumbleRules.EnoughNoblins(eligible))
                {
                    return "MSSG_RumbleNotEnough".Translate(RumbleRules.MinNoblins, eligible);
                }
            }
            return base.CanStartRitualNow(target, ritual, selectedPawn, forcedForRole);
        }
    }

    /// <summary>When the gathering ends, every noblin on the map starts fighting.</summary>
    public class RitualOutcomeEffectWorker_NobbyRumble : RitualOutcomeEffectWorker
    {
        public RitualOutcomeEffectWorker_NobbyRumble()
        {
        }

        public RitualOutcomeEffectWorker_NobbyRumble(RitualOutcomeEffectDef def) : base(def)
        {
        }

        public override bool ShowQuality => false;

        public override void Apply(float progress, Dictionary<Pawn, int> totalPresence, LordJob_Ritual jobRitual)
        {
            NobbyRumbleComponent.For(jobRitual.Map)?.RequestStart();
        }
    }
}
