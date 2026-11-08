using System.Linq;
using RimWorld;
using Verse;

namespace HobGob;

// code assumes the xml lines are using mayrequires anyway so no DLC defence necessary unless outcomedoer used elsewhere
public class IngestionOutcomeDoer_AbilityGain : IngestionOutcomeDoer
{
    public GeneDef requiredGene;
    public bool sendMessage = false;

    protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested, int ingestedCount)
    {
        if (pawn.abilities == null) return;

        if (requiredGene != null)
        {
            Gene x = pawn.genes?.GetGene(requiredGene);
            if (x == null || !x.Active) return;
        }

        //deliberate decision not to bother with psylink. the ability will only show on a drafted pawn who has no psylink, otherwise no other deliberate indication of a pawn having an ability
        var abilitylist = DefDatabase<AbilityDef>.AllDefs.Where(y => y.IsPsycast && pawn.abilities.GetAbility(y) == null).ToList();
        if (abilitylist.Count == 0) return;

        AbilityDef chosenability = abilitylist.RandomElement();
        pawn.abilities.GainAbility(chosenability);

        if (sendMessage) Messages.Message("MSSG_AbilityGained".Translate(chosenability.LabelCap, pawn.Named("PAWN")), pawn, MessageTypeDefOf.PositiveEvent);
    }
}
