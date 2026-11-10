using System.Collections.Generic;
using System.Linq;
using HobGob.Census;
using RimWorld;
using Verse;

namespace HobGob.Events
{
    /// <summary>Who can fight, who watches, and who sends the challenger (E2 Hobgoblin Challenge).</summary>
    public static class ChallengeUtility
    {
        public static bool IsGoblin(Pawn pawn)
        {
            XenotypeDef xenotype = pawn.genes?.Xenotype;
            return xenotype != null && xenotype.HasModExtension<CensusCountedExtension>();
        }

        /// <summary>Goblin free colonists on the map, Noblins included.</summary>
        public static List<Pawn> GoblinColonists(Verse.Map map)
        {
            return map.mapPawns.FreeColonistsSpawned.Where(IsGoblin).ToList();
        }

        public static bool CanBeChampion(Pawn pawn)
        {
            return pawn.Spawned
                && !pawn.Dead
                && !pawn.Downed
                && !pawn.InMentalState
                && IsGoblin(pawn)
                && pawn.genes.Xenotype != HobGobDefOf.MSSG_Noblin
                && !pawn.WorkTagIsDisabled(WorkTags.Violent);
        }

        public static List<Pawn> EligibleChampions(Verse.Map map)
        {
            return map.mapPawns.FreeColonistsSpawned.Where(CanBeChampion).ToList();
        }

        public static float Score(Pawn pawn)
        {
            int melee = pawn.skills?.GetSkill(SkillDefOf.Melee)?.Level ?? 0;
            return EventRules.ChampionScore(melee, pawn.BodySize);
        }

        public static Pawn BestChampion(Verse.Map map)
        {
            return EligibleChampions(map).OrderByDescending(Score).ThenBy(p => p.thingIDNumber).FirstOrDefault();
        }

        /// <summary>
        /// D1: a goblin faction if one exists, else a tribal one, else no faction. Only factions that are not
        /// hostile (their pawn would be attacked on sight) and that do not turn hostile when harmed.
        /// </summary>
        public static Faction PickChallengerFaction()
        {
            List<Faction> usable = Find.FactionManager.AllFactionsListForReading
                .Where(f => !f.IsPlayer && !f.Hidden && !f.temporary && !f.defeated && !f.deactivated
                    && f.def.humanlikeFaction && !f.factionHostileOnHarmByPlayer && !f.HostileTo(Faction.OfPlayer))
                .ToList();
            Faction goblin = usable.Where(HasGoblinXenotype).RandomElementWithFallback();
            if (goblin != null)
            {
                return goblin;
            }
            return usable.Where(f => f.def.techLevel <= TechLevel.Neolithic).RandomElementWithFallback();
        }

        private static bool HasGoblinXenotype(Faction faction)
        {
            XenotypeSet set = faction.def.xenotypeSet;
            if (set == null)
            {
                return false;
            }
            for (int i = 0; i < set.Count; i++)
            {
                if (set[i].xenotype != null && set[i].xenotype.HasModExtension<CensusCountedExtension>())
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Census tier gate: the colony must be at least a shed.</summary>
        public static bool CensusAllowsChallenge()
        {
            GoblinCensusTierDef tier = Current.Game.GetComponent<GoblinCensusComponent>()?.CurrentTier;
            GoblinCensusTierDef shed = HobGobDefOf.MSSG_Census_Shed;
            return tier != null && shed != null && tier.minPopulation >= shed.minPopulation;
        }
    }
}
