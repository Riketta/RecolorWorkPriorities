using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace RecolorWorkPriorities
{
    /// <summary>Fluffy's Work Tab ships its own private priority color
    /// function (a green-white-grey gradient over its "max priority"
    /// setting), so the vanilla color patch cannot reach it. When the mod is
    /// present, its gradient is replicated for the tier shift: 2 wears 1's
    /// gradient color, 3 wears 2's, and 1 wears the configured cyan, while
    /// tiers 4+ keep the gradient untouched. Its work type tooltip is tiered
    /// up with the shared transpilers so border and text stay in sync (the
    /// tab's boxes are drawn by the already-patched vanilla background
    /// helper). Everything degrades to "Work Tab stays unpatched" if its
    /// internals ever change.</summary>
    internal static class Patch_WorkTab_DrawUtilities
    {
        private const string DrawUtilitiesTypeName = "WorkTab.DrawUtilities";
        private const string SettingsTypeName = "WorkTab.Settings";

        private static MethodInfo colorOfPriorityMethod;
        private static FieldInfo maxPriorityField;
        private static bool compatible;

        /// <summary>One-shot per frame refresh of Work Tab's max priority
        /// setting: keeps the replicated gradient exact when the player
        /// changes Work Tab's own settings, without per-cell reflection on
        /// the drawing hot path.</summary>
        private static int cachedFrame = -1;
        private static int cachedMaxPriority = 4;

        public static void TryApply(Harmony harmony)
        {
            try
            {
                Type drawUtilities = AccessTools.TypeByName(DrawUtilitiesTypeName);
                if (drawUtilities == null)
                {
                    DebugLog.Message("Fluffy's Work Tab not found - skipping its compatibility patch.");
                    return;
                }
                colorOfPriorityMethod = AccessTools.DeclaredMethod(drawUtilities, "ColorOfPriority", new[] { typeof(int) });
                MethodInfo tipForWorkType = AccessTools.DeclaredMethod(drawUtilities, "TipForPawnWorker",
                    new[] { typeof(Pawn), typeof(WorkTypeDef), typeof(bool) });
                maxPriorityField = AccessTools.Field(AccessTools.TypeByName(SettingsTypeName), "maxPriority");
                if (colorOfPriorityMethod == null || tipForWorkType == null || maxPriorityField == null)
                {
                    DebugLog.Warning("Work Tab found but its color/tooltip internals are missing (update?) - it stays unpatched.");
                    return;
                }
                compatible = true;
                harmony.Patch(colorOfPriorityMethod,
                    postfix: new HarmonyMethod(typeof(Patch_WorkTab_DrawUtilities), nameof(ColorOfPriorityPostfix)));
                harmony.Patch(tipForWorkType,
                    transpiler: new HarmonyMethod(typeof(Patch_WorkTab_DrawUtilities), nameof(WorkTypeTipTranspiler)));
                DebugLog.Message("Fluffy's Work Tab detected - shifted its priority colors and warning threshold as well.");
            }
            catch (Exception e)
            {
                Log.Error("[RecolorWorkPriorities] Work Tab compatibility patch failed (it stays unpatched). " + e.Message);
            }
        }

        private static IEnumerable<CodeInstruction> WorkTypeTipTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            instructions = WarningThresholdTranspiler.ReplaceSkillThreshold(
                instructions, "WorkTab.DrawUtilities.TipForPawnWorker");
            return WarningThresholdTranspiler.BumpColorOfPriorityTwoToThree(
                instructions, colorOfPriorityMethod, "WorkTab.DrawUtilities.TipForPawnWorker");
        }

        private static void ColorOfPriorityPostfix(int priority, ref Color __result)
        {
            if (!compatible || !RecolorWorkPrioritiesMod.Active)
            {
                return;
            }
            RecolorWorkPrioritiesSettings settings = RecolorWorkPrioritiesMod.Settings;
            if (settings == null)
            {
                return;
            }
            if (priority == 1)
            {
                __result = settings.priority1Color;
            }
            else if (priority == 2 || priority == 3)
            {
                __result = GradientColorOf(priority - 1);
            }
        }

        /// <summary>Replica of Work Tab's private gradient (green at the top,
        /// white at half, grey at max priority), evaluated one tier below the
        /// requested priority to shift the ladder down.</summary>
        private static Color GradientColorOf(int priority)
        {
            if (priority == 0)
            {
                return Color.grey;
            }
            int maxPriority = CurrentMaxPriority();
            float halfway = maxPriority / 2f;
            if (priority <= halfway)
            {
                return Color.Lerp(Color.green, Color.white, Mathf.InverseLerp(1, halfway, priority));
            }
            return Color.Lerp(Color.white, Color.grey, Mathf.InverseLerp(halfway, maxPriority, priority));
        }

        private static int CurrentMaxPriority()
        {
            if (Time.frameCount != cachedFrame)
            {
                cachedFrame = Time.frameCount;
                try
                {
                    cachedMaxPriority = maxPriorityField != null ? (int)maxPriorityField.GetValue(null) : 4;
                    DebugLog.Verbose("Work Tab max priority read as " + cachedMaxPriority + ".");
                }
                catch (Exception e)
                {
                    cachedMaxPriority = 4;
                    compatible = false;
                    DebugLog.Warning("could not read Work Tab's max priority anymore (update?) - its tab stays unpatched. " + e.Message);
                }
            }
            return cachedMaxPriority;
        }
    }
}
