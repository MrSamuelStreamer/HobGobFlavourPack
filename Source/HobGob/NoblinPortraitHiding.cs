using System.Runtime.CompilerServices;
using RimWorld;
using Verse;

namespace HobGob
{
    /// <summary>
    /// Pit Noblins join the flavour pack's hidden-colonist list, and leave it when they evolve.
    /// MSSFP types stay in <see cref="Apply"/>, which runs only after that mod is confirmed active.
    /// </summary>
    public static class NoblinPortraitHiding
    {
        private const string FlavourPackId = "MSS.FlavourPack";

        public static void Hide(Pawn pawn)
        {
            if (pawn == null || !pawn.IsColonist || !FlavourPackActive())
            {
                return;
            }
            Apply(pawn, hide: true);
        }

        public static void Show(Pawn pawn)
        {
            if (pawn == null || !FlavourPackActive())
            {
                return;
            }
            Apply(pawn, hide: false);
        }

        private static bool FlavourPackActive() => ModLister.GetActiveModWithIdentifier(FlavourPackId) != null;

        /// <summary>Not inlined: compiling this method is what loads MSSFP, and that must not happen when the mod is absent.</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Apply(Pawn pawn, bool hide)
        {
            MSSFP.ColonistHidingWorldComponent worldComp = Find.World?.GetComponent<MSSFP.ColonistHidingWorldComponent>();
            if (worldComp == null)
            {
                return;
            }
            if (hide)
            {
                worldComp.HideColonist(pawn);
            }
            else
            {
                worldComp.ShowColonist(pawn);
            }
            Find.ColonistBar?.MarkColonistsDirty();
        }
    }
}
