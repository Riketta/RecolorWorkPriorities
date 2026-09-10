using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RecolorWorkPriorities
{
    /// <summary>Keeps the priority tooltip consistent with the moved border:
    /// the "very bad skill" warning line appears under the same configured
    /// threshold, and the warning text keeps its color by reading it one
    /// tier up (post-shift tier 3 wears what tier 2 wore in vanilla), so
    /// warnings stay yellow instead of turning green with the shifted
    /// ladder. The "Priority N" line itself is recolored by the
    /// ColorOfPriority postfix.</summary>
    [HarmonyPatch]
    internal static class Patch_WidgetsWork_TipForPawnWorker
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(WidgetsWork), "TipForPawnWorker",
                new[] { typeof(Pawn), typeof(WorkTypeDef), typeof(bool) });
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            instructions = WarningThresholdTranspiler.ReplaceSkillThreshold(
                instructions, "WidgetsWork.TipForPawnWorker");
            return WarningThresholdTranspiler.BumpColorOfPriorityTwoToThree(
                instructions,
                AccessTools.Method(typeof(WidgetsWork), nameof(WidgetsWork.ColorOfPriority)),
                "WidgetsWork.TipForPawnWorker");
        }
    }
}
