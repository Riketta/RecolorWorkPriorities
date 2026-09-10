using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RecolorWorkPriorities
{
    /// <summary>Shared IL edits for the "shifted priority labels" feature.
    ///
    /// Both work tabs draw the raw priority number through int.ToStringCached
    /// immediately before coloring it, so one surgical call replacement turns
    /// the glyphs into the shifted ladder (top label / 1 / 2 / 3) without
    /// touching the real priority values underneath. The tooltip edit rewrites
    /// the ("Priority" + n).Translate() block into a lookup that uses the same
    /// glyphs, so hovering an "A" box says "Priority A", not "Priority 1".
    ///
    /// Failed shape matching degrades to vanilla numbers plus a warning, the
    /// same contract as every other patch in this mod.</summary>
    internal static class PriorityLabelTranspiler
    {
        internal static IEnumerable<CodeInstruction> ReplaceToStringWithLabel(
            IEnumerable<CodeInstruction> instructions, string targetName)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            MethodInfo displayLabel = AccessTools.Method(
                typeof(RecolorWorkPrioritiesMod), nameof(RecolorWorkPrioritiesMod.DisplayLabelOf));
            int replaced = 0;
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Call
                    && codes[i].operand is MethodInfo called
                    && called.Name == "ToStringCached")
                {
                    codes[i] = new CodeInstruction(OpCodes.Call, displayLabel);
                    replaced++;
                }
            }
            if (replaced != 1)
            {
                DebugLog.Warning("expected exactly one priority ToStringCached call in " + targetName
                    + " but found " + replaced + " - " + targetName + " keeps vanilla numbers (game update?).");
                return instructions;
            }
            DebugLog.Message("relabelled the drawn priorities in " + targetName + ".");
            return codes;
        }

        /// <summary>Matches the compiler-generated shape of
        /// ("Priority" + priority).Translate(): string constant, int load,
        /// box, Concat, then the Translate call and the (string) cast to
        /// TaggedString - Translate is an extension on string, so it binds
        /// directly on the Concat result and the cast follows; both orders
        /// are accepted in case a future compiler binds differently. The int
        /// load survives; everything around it is folded into one call to
        /// PriorityTip(int), which returns a plain string and resolves the
        /// same shifted labels the cells draw.</summary>
        internal static IEnumerable<CodeInstruction> ReplaceTipPriorityBlock(
            IEnumerable<CodeInstruction> instructions, string targetName)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            MethodInfo priorityTip = AccessTools.Method(
                typeof(RecolorWorkPrioritiesMod), nameof(RecolorWorkPrioritiesMod.PriorityTip));
            for (int i = 0; i + 5 < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldstr
                    && codes[i].operand is string prefix
                    && prefix == "Priority"
                    && codes[i + 2].opcode == OpCodes.Box
                    && IsNamedCall(codes[i + 3], "Concat")
                    && ((IsNamedCall(codes[i + 4], "Translate") && IsNamedCall(codes[i + 5], "op_Implicit"))
                        || (IsNamedCall(codes[i + 4], "op_Implicit") && IsNamedCall(codes[i + 5], "Translate"))))
                {
                    CodeInstruction loadPriority = codes[i + 1];
                    loadPriority.blocks.AddRange(codes[i].blocks);
                    CodeInstruction callPriorityTip = new CodeInstruction(OpCodes.Call, priorityTip);
                    codes.RemoveRange(i, 6);
                    codes.InsertRange(i, new[] { loadPriority, callPriorityTip });
                    DebugLog.Message("relabelled the tooltip priority in " + targetName + ".");
                    return codes;
                }
            }
            DebugLog.Warning("could not find the tooltip 'Priority' + n block in " + targetName
                + " - it stays vanilla (game update?).");
            return instructions;
        }

        private static bool IsNamedCall(CodeInstruction instruction, string name)
        {
            return (instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt)
                && instruction.operand is MethodInfo called
                && called.Name == name;
        }
    }
}
