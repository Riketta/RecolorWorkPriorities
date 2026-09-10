using System.Collections.Generic;
using HarmonyLib;
using RimWorld;

namespace RecolorWorkPriorities
{
    /// <summary>Vanilla draws the priority number for every work box through
    /// this helper - replacing its ToStringCached call routes the glyph
    /// through DisplayLabelOf, so cells read TopLabel / 1 / 2 / 3 while the
    /// real priority values stay 0-4 underneath. Colors come from the
    /// already-patched ColorOfPriority, so labels and colors always agree.</summary>
    [HarmonyPatch(typeof(WidgetsWork), nameof(WidgetsWork.DrawWorkBoxFor))]
    internal static class Patch_WidgetsWork_DrawWorkBoxFor
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return PriorityLabelTranspiler.ReplaceToStringWithLabel(
                instructions, "WidgetsWork.DrawWorkBoxFor");
        }
    }
}
