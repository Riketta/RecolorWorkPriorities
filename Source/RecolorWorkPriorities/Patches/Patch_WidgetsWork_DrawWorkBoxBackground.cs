using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;

namespace RecolorWorkPriorities
{
    /// <summary>The low-skill warning border around a work box is drawn by
    /// this private vanilla helper - and Fluffy's Work Tab calls it through
    /// reflection for its own boxes, so patching the skill threshold here
    /// moves the border in both work tabs at once. Only the skill comparison
    /// is touched; the warning textures themselves stay vanilla.</summary>
    [HarmonyPatch]
    internal static class Patch_WidgetsWork_DrawWorkBoxBackground
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(WidgetsWork), "DrawWorkBoxBackground");
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return WarningThresholdTranspiler.ReplaceSkillThreshold(
                instructions, "WidgetsWork.DrawWorkBoxBackground");
        }
    }
}
