using System.Collections.Generic;
using RimWorld;
using Verse;

namespace HobGob.Events
{
    /// <summary>
    /// "A hobgoblin challenges your strongest goblin." Accept with the proposed champion, pick another,
    /// or refuse. Holds only the map; the challenge state lives in <see cref="HobgoblinChallengeComponent"/>.
    /// </summary>
    public class ChoiceLetter_HobgoblinChallenge : ChoiceLetter
    {
        public Verse.Map map;

        private HobgoblinChallengeComponent Challenge => map?.GetComponent<HobgoblinChallengeComponent>();

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
                HobgoblinChallengeComponent challenge = Challenge;
                Pawn proposed = map != null ? ChallengeUtility.BestChampion(map) : null;
                DiaOption accept = new DiaOption(proposed != null
                    ? "MSSG_ChallengeAccept".Translate(proposed.LabelShort).ToString()
                    : "MSSG_ChallengeAcceptNobody".Translate().ToString())
                {
                    action = () =>
                    {
                        if (challenge != null && challenge.Accept(proposed))
                        {
                            Find.LetterStack.RemoveLetter(this);
                        }
                    },
                    resolveTree = true,
                };
                if (proposed == null)
                {
                    accept.Disable("MSSG_ChallengeNoChampion".Translate());
                }
                yield return accept;

                List<Pawn> others = map != null ? ChallengeUtility.EligibleChampions(map) : new List<Pawn>();
                DiaOption pick = new DiaOption("MSSG_ChallengePickOther".Translate())
                {
                    action = () => OpenChampionMenu(challenge, others),
                    resolveTree = true,
                };
                if (others.Count == 0)
                {
                    pick.Disable("MSSG_ChallengeNoChampion".Translate());
                }
                yield return pick;

                yield return new DiaOption("MSSG_ChallengeRefuse".Translate())
                {
                    action = () =>
                    {
                        challenge?.Refuse(timedOut: false);
                        Find.LetterStack.RemoveLetter(this);
                    },
                    resolveTree = true,
                };
                yield return Option_Postpone;
            }
        }

        private void OpenChampionMenu(HobgoblinChallengeComponent challenge, List<Pawn> candidates)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            foreach (Pawn pawn in candidates)
            {
                Pawn chosen = pawn;
                int melee = pawn.skills?.GetSkill(SkillDefOf.Melee)?.Level ?? 0;
                options.Add(new FloatMenuOption(
                    "MSSG_ChallengeChampionOption".Translate(pawn.LabelShort, melee, pawn.BodySize.ToString("0.##")),
                    () =>
                    {
                        if (challenge != null && challenge.Accept(chosen))
                        {
                            Find.LetterStack.RemoveLetter(this);
                        }
                    }));
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref map, "map");
        }
    }

    /// <summary>The beaten challenger asks to join the colony.</summary>
    public class ChoiceLetter_ChallengerJoins : ChoiceLetter
    {
        public Pawn challenger;

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
                yield return new DiaOption("MSSG_ChallengerJoinAccept".Translate())
                {
                    action = () =>
                    {
                        challenger?.MapHeld?.GetComponent<HobgoblinChallengeComponent>()?.AcceptJoin(challenger);
                        Find.LetterStack.RemoveLetter(this);
                    },
                    resolveTree = true,
                };
                yield return new DiaOption("MSSG_ChallengerJoinDecline".Translate())
                {
                    action = () =>
                    {
                        challenger?.MapHeld?.GetComponent<HobgoblinChallengeComponent>()?.DeclineJoin(challenger);
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
            Scribe_References.Look(ref challenger, "challenger");
        }
    }
}
