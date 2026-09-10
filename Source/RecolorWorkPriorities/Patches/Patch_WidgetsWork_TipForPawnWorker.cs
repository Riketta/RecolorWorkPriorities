using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RecolorWorkPriorities
{
    /// <summary>Two jobs on the vanilla priority tooltip:
    ///
    /// - A transpiler rewrites the ("Priority" + n).Translate() block so the
    ///   tooltip uses the same shifted glyphs as the cells ("Priority A" ...
    ///   "Priority 3") in every language. The Colorize call is left on the
    ///   real priority, so tooltip colors keep matching cell colors.
    /// - A postfix appends the "very bad skill" warning line for the
    ///   extension band (skill 2..threshold) that vanilla does not cover,
    ///   mirroring the border extension: same translation key, same tier-2
    ///   color convention, same activation check.
    ///
    /// Both degrade independently: the transpiler falls back to vanilla text
    /// if the block shape changes, the postfix simply does not fire.</summary>
    [HarmonyPatch(typeof(WidgetsWork), "TipForPawnWorker",
        new[] { typeof(Pawn), typeof(WorkTypeDef), typeof(bool) })]
    internal static class Patch_WidgetsWork_TipForPawnWorker
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return PriorityLabelTranspiler.ReplaceTipPriorityBlock(
                instructions, "WidgetsWork.TipForPawnWorker");
        }

        private static void Postfix(Pawn p, WorkTypeDef wDef, ref string __result)
        {
            if (!RecolorWorkPrioritiesMod.Active
                || RecolorWorkPrioritiesMod.WarningSkillThreshold <= 2f
                || wDef.relevantSkills.Count == 0
                || p.workSettings == null)
            {
                return;
            }
            float skill = p.skills.AverageOfRelevantSkillsFor(wDef);
            if (skill <= 2f
                || skill > RecolorWorkPrioritiesMod.WarningSkillThreshold
                || !p.workSettings.WorkIsActive(wDef))
            {
                return;
            }
            // Same line, same key and tier-2 color convention as vanilla's own
            // band - only the skill range differs.
            __result = __result + "\n\n"
                + ((string)"SelectedWorkTypeWithVeryBadSkill".Translate())
                .Colorize(WidgetsWork.ColorOfPriority(2));
        }
    }
}
