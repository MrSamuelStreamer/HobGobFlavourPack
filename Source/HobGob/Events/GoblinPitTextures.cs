using UnityEngine;
using Verse;

namespace HobGob.Events
{
    [StaticConstructorOnStartup]
    public static class GoblinPitTextures
    {
        public static readonly Texture2D Rummage = ContentFinder<Texture2D>.Get("UI/Designators/Haul");
    }
}
