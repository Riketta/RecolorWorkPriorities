using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;

namespace RecolorWorkPriorities
{
    /// <summary>Shared IL edits for the "warning border one tier up" feature.
    ///
    /// Vanilla compares the average relevant skill against a hard-coded 2f
    /// when deciding whether to draw the low-skill warning border and to show
    /// the matching tooltip warning; both checks are transpiled to load the
    /// settings-driven threshold field instead. Each patched method contains
    /// exactly one positive 2f constant (the other float constants are
    /// different or negative), so an ambiguous or missing match means the
    /// game changed - the method then stays vanilla and a warning is logged.
    ///
    /// The second edit moves the "warning" color references one tier up as
    /// well: the tooltip warning lines are vanilla-colored with priority 2's
    /// color, which after the ladder shift would read as green; pointing them
    /// at tier 3 keeps warnings wearing the color 2 wore before the shift.</summary>
    internal static class WarningThresholdTranspiler
    {
        internal static IEnumerable<CodeInstruction> ReplaceSkillThreshold(
            IEnumerable<CodeInstruction> instructions, string targetName)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            FieldInfo thresholdField = AccessTools.Field(
                typeof(RecolorWorkPrioritiesMod), nameof(RecolorWorkPrioritiesMod.WarningSkillThreshold));
            int replaced = 0;
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldc_R4
                    && codes[i].operand is float value
                    && Mathf.Approximately(value, 2f))
                {
                    CodeInstruction loadThreshold = new CodeInstruction(OpCodes.Ldsfld, thresholdField);
                    loadThreshold.blocks.AddRange(codes[i].blocks);
                    codes[i] = loadThreshold;
                    replaced++;
                }
            }
            if (replaced != 1)
            {
                DebugLog.Warning("expected exactly one 'skill <= 2' constant in " + targetName
                    + " but found " + replaced + " - " + targetName + " stays vanilla (game update?).");
                return instructions;
            }
            DebugLog.Message("moved the low-skill warning threshold in " + targetName
                + " from 2 to the settings value (" + RecolorWorkPrioritiesMod.WarningSkillThreshold + ").");
            return codes;
        }

        /// <summary>Changes the constant 2 passed to ColorOfPriority into 3,
        /// so warning text keeps its pre-shift look. Tolerates zero matches
        /// (Fluffy's Work Tab appends its warning line uncolored) - anything
        /// found is tiered up and counted for the verbose log.</summary>
        internal static IEnumerable<CodeInstruction> BumpColorOfPriorityTwoToThree(
            IEnumerable<CodeInstruction> instructions, MethodInfo colorOfPriority, string targetName)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            int bumped = 0;
            if (colorOfPriority != null)
            {
                for (int i = 1; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Call
                        && codes[i].operand is MethodInfo called
                        && called == colorOfPriority
                        && LoadsConstant(codes[i - 1], 2))
                    {
                        SetConstant(codes[i - 1], 3);
                        bumped++;
                    }
                }
            }
            DebugLog.Verbose("tiered up " + bumped + " warning color reference(s) in " + targetName + ".");
            return codes;
        }

        private static bool LoadsConstant(CodeInstruction instruction, int value)
        {
            if (instruction.opcode == OpCodes.Ldc_I4)
            {
                return instruction.operand is int i && i == value;
            }
            if (instruction.opcode == OpCodes.Ldc_I4_S)
            {
                return instruction.operand is sbyte b && b == value;
            }
            return instruction.opcode switch
            {
                _ when value == -1 => instruction.opcode == OpCodes.Ldc_I4_M1,
                _ when value == 0 => instruction.opcode == OpCodes.Ldc_I4_0,
                _ when value == 1 => instruction.opcode == OpCodes.Ldc_I4_1,
                _ when value == 2 => instruction.opcode == OpCodes.Ldc_I4_2,
                _ when value == 3 => instruction.opcode == OpCodes.Ldc_I4_3,
                _ when value == 4 => instruction.opcode == OpCodes.Ldc_I4_4,
                _ when value == 5 => instruction.opcode == OpCodes.Ldc_I4_5,
                _ when value == 6 => instruction.opcode == OpCodes.Ldc_I4_6,
                _ when value == 7 => instruction.opcode == OpCodes.Ldc_I4_7,
                _ when value == 8 => instruction.opcode == OpCodes.Ldc_I4_8,
                _ => false
            };
        }

        private static void SetConstant(CodeInstruction instruction, int value)
        {
            if (instruction.opcode == OpCodes.Ldc_I4)
            {
                instruction.operand = value;
            }
            else if (instruction.opcode == OpCodes.Ldc_I4_S)
            {
                instruction.operand = (sbyte)value;
            }
            else
            {
                instruction.opcode = OpCodes.Ldc_I4;
                instruction.operand = value;
            }
        }
    }
}
