using System.Collections.Generic;
using RimWorld;
using Verse;

namespace HobGob.Comps.Game
{
    /// <summary>Holds casters until the tick after the pit is placed, then removes them with no corpse.</summary>
    public class GoblinEntombComponent : GameComponent
    {
        private List<Pawn> casters = new List<Pawn>();
        private List<Thing> holes = new List<Thing>();
        private List<int> ticks = new List<int>();

        public GoblinEntombComponent(Verse.Game game)
        {
        }

        public void Enqueue(Pawn caster, Thing hole)
        {
            casters.Add(caster);
            holes.Add(hole);
            ticks.Add(Find.TickManager.TicksGame);
        }

        public override void GameComponentTick()
        {
            if (casters.Count == 0)
            {
                return;
            }
            int now = Find.TickManager.TicksGame;
            for (int i = casters.Count - 1; i >= 0; i--)
            {
                if (ticks[i] >= now)
                {
                    continue;
                }
                Pawn caster = casters[i];
                Thing hole = holes[i];
                casters.RemoveAt(i);
                holes.RemoveAt(i);
                ticks.RemoveAt(i);
                if (caster == null || caster.Destroyed)
                {
                    continue;
                }
                LookTargets look = hole != null && !hole.Destroyed ? hole : caster;
                Messages.Message("MSSG_HobnobEntombed".Translate(caster.Named("PAWN")), look, MessageTypeDefOf.NeutralEvent);
                caster.Destroy(DestroyMode.Vanish);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref casters, "casters", LookMode.Reference);
            Scribe_Collections.Look(ref holes, "holes", LookMode.Reference);
            Scribe_Collections.Look(ref ticks, "ticks", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                casters ??= new List<Pawn>();
                holes ??= new List<Thing>();
                ticks ??= new List<int>();
                int count = casters.Count;
                if (holes.Count < count)
                {
                    count = holes.Count;
                }
                if (ticks.Count < count)
                {
                    count = ticks.Count;
                }
                if (casters.Count > count)
                {
                    casters.RemoveRange(count, casters.Count - count);
                }
                if (holes.Count > count)
                {
                    holes.RemoveRange(count, holes.Count - count);
                }
                if (ticks.Count > count)
                {
                    ticks.RemoveRange(count, ticks.Count - count);
                }
                for (int i = count - 1; i >= 0; i--)
                {
                    if (casters[i] == null)
                    {
                        casters.RemoveAt(i);
                        holes.RemoveAt(i);
                        ticks.RemoveAt(i);
                    }
                }
            }
        }
    }
}
