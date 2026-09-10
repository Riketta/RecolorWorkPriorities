using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RecolorWorkPriorities
{
    /// <summary>The only IL edit left in the tooltip: the drawn priority
    /// label is built as ("Priority" + n).Translate(), and rewriting that
    /// block lets tooltips use the same shifted glyphs as the cells
    /// ("Priority A" ... "Priority 3") in every language. Everything else
    /// about the tooltip - the warning line threshold and its color - stays
    /// vanilla by design: vanilla colors warnings with priority 2's color,
    /// which under the shifted ladder is simply green.
    ///
    /// The block shape (string constant, int load, box, Concat, implicit
    /// cast, Translate) is validated before it is touched; on any mismatch
    /// the method stays vanilla and a warning is logged.</summary>
    [HarmonyPatch(typeof(WidgetsWork), "TipForPawnWorker",
        new[] { typeof(Pawn), typeof(WorkTypeDef), typeof(bool) })]
    internal static class Patch_WidgetsWork_TipForPawnWorker
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return PriorityLabelTranspiler.ReplaceTipPriorityBlock(
                instructions, "WidgetsWork.TipForPawnWorker");
        }
    }
}
