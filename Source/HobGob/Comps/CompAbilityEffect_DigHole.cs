using System.Linq;
using HobGob.Comps.Game;
using RimWorld;
using UnityEngine;
using Verse;

namespace HobGob.Comps
{
    /// <summary>Places a goblin spawning hole on the chosen cells, then removes the caster.</summary>
    public class CompAbilityEffect_DigHole : CompAbilityEffect
    {
        public new CompProperties_AbilityDigHole Props => (CompProperties_AbilityDigHole)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn caster = parent.pawn;
            Verse.Map map = caster.Map;
            Thing hole = GenSpawn.Spawn(Props.thingDef, target.Cell, map);
            if (hole == null || hole.Destroyed)
            {
                return;
            }
            if (caster.Faction != null)
            {
                hole.SetFaction(caster.Faction);
            }
            // The cast job is still using the pawn. Vanish them on the following tick, after it finishes.
            GoblinEntombComponent entomb = Current.Game.GetComponent<GoblinEntombComponent>();
            if (entomb == null)
            {
                Log.Error("GoblinEntombComponent is missing; the hobnobgoblin was not entombed.");
                return;
            }
            entomb.Enqueue(caster, hole);
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (!base.Valid(target, throwMessages))
            {
                return false;
            }
            Verse.Map map = parent.pawn.Map;
            if (map == null || Props.thingDef == null)
            {
                return false;
            }
            // The hole is 6x6. The vanilla spawn effect only checks the clicked cell.
            AcceptanceReport report = GenConstruct.CanPlaceBlueprintAt(Props.thingDef, target.Cell, Rot4.North, map);
            if (!report.Accepted)
            {
                if (throwMessages)
                {
                    Messages.Message("CannotUseAbility".Translate(parent.def.label) + ": " + report.Reason,
                        target.ToTargetInfo(map), MessageTypeDefOf.RejectInput, false);
                }
                return false;
            }
            return true;
        }

        public override void DrawEffectPreview(LocalTargetInfo target)
        {
            if (Props.thingDef == null)
            {
                return;
            }
            CellRect rect = GenAdj.OccupiedRect(target.Cell, Rot4.North, Props.thingDef.size);
            GenDraw.DrawFieldEdges(rect.Cells.ToList(), Valid(target, false) ? Color.white : Color.red);
        }
    }
}
