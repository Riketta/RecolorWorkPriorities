using HarmonyLib;
using RimWorld;
using UnityEngine;

namespace RecolorWorkPriorities
{
    /// <summary>Shifts the priority color ladder one tier up: the number 2
    /// wears 1's color, 3 wears 2's color, and 1 wears the configured cyan.
    /// Priority 4 and the inactive grey keep their vanilla colors. Vanilla
    /// routes every priority number it draws (work tab cells, tooltips)
    /// through this one function, so a single postfix covers them all;
    /// Fluffy's Work Tab ships an independent color function and is handled
    /// by its own compatibility patch. Per-cell hot path: no allocations,
    /// no logging here.</summary>
    [HarmonyPatch(typeof(WidgetsWork), nameof(WidgetsWork.ColorOfPriority))]
    internal static class Patch_WidgetsWork_ColorOfPriority
    {
        private static void Postfix(int prio, ref Color __result)
        {
            RecolorWorkPrioritiesSettings settings = RecolorWorkPrioritiesMod.Settings;
            if (settings == null || !settings.enabled)
            {
                return;
            }
            switch (prio)
            {
                case 1: __result = settings.priority1Color; break;
                case 2: __result = settings.priority2Color; break;
                case 3: __result = settings.priority3Color; break;
            }
        }
    }
}
