using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RecolorWorkPriorities
{
    /// <summary>Keeps the priority tooltip consistent with the moved border
    /// and the shifted labels: the "very bad skill" warning line appears
    /// under the same configured threshold, the warning text keeps its color
    /// by reading it one tier up (post-shift tier 3 wears what tier 2 wore in
    /// vanilla), and the "Priority N" text is rebuilt from the same shifted
    /// glyphs the cells draw, so an "A" box says "Priority A". The
    /// Colorize call itself stays on the real priority, so tooltip colors
    /// keep matching cell colors.</summary>
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
            instructions = WarningThresholdTranspiler.BumpColorOfPriorityTwoToThree(
                instructions,
                AccessTools.Method(typeof(WidgetsWork), nameof(WidgetsWork.ColorOfPriority)),
                "WidgetsWork.TipForPawnWorker");
            return PriorityLabelTranspiler.ReplaceTipPriorityBlock(
                instructions, "WidgetsWork.TipForPawnWorker");
        }
    }
}
