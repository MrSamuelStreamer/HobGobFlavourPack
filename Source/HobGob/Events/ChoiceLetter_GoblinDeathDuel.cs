using System.Collections.Generic;
using RimWorld;
using Verse;

namespace HobGob.Events
{
    /// <summary>
    /// Two Noblins, or two Goblins, want to fight to the death. Let them, or forbid it.
    /// Holds only the map; the duel state lives in <see cref="GoblinDeathDuelComponent"/>.
    /// </summary>
    public class ChoiceLetter_GoblinDeathDuel : ChoiceLetter
    {
        public Verse.Map map;

        private GoblinDeathDuelComponent Duel => map?.GetComponent<GoblinDeathDuelComponent>();

        public override bool CanDismissWithRightClick => false;

        public override IEnumerable<DiaOption> Choices
        {
            get
            {
                if (ArchivedOnly)
                {
                    yield return Option_Close;
                    yield break;
                }
                GoblinDeathDuelComponent duel = Duel;
                DiaOption accept = new DiaOption("MSSG_DeathDuelAccept".Translate())
                {
                    action = () =>
                    {
                        if (duel != null && duel.Accept())
                        {
                            Find.LetterStack.RemoveLetter(this);
                        }
                    },
                    resolveTree = true,
                };
                if (duel == null || !duel.CanAccept)
                {
                    accept.Disable("MSSG_DeathDuelCannotStart".Translate());
                }
                yield return accept;

                yield return new DiaOption("MSSG_DeathDuelForbid".Translate())
                {
                    action = () =>
                    {
                        duel?.Refuse(timedOut: false);
                        Find.LetterStack.RemoveLetter(this);
                    },
                    resolveTree = true,
                };
                yield return Option_Postpone;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref map, "map");
        }
    }
}
