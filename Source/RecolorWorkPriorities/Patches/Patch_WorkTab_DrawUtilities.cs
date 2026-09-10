using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace RecolorWorkPriorities
{
    /// <summary>Fluffy's Work Tab compatibility, postfix-first: its priority
    /// colors come from a private green-white-grey gradient function (a
    /// postfix shifts tiers 1-3 through a replica of that gradient and
    /// applies tier 4 once customized), its work type tooltip gains the same
    /// extended low-skill warning line as the vanilla tooltip (uncolored,
    /// matching its own style), and its drawn numbers are relabelled through
    /// the one shared ToStringCached transpiler. Everything degrades to
    /// "Work Tab stays unpatched" if its internals ever change.</summary>
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
                MethodInfo drawPriority = AccessTools.DeclaredMethod(drawUtilities, "DrawPriority");
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
                    postfix: new HarmonyMethod(typeof(Patch_WorkTab_DrawUtilities), nameof(WorkTypeTipPostfix)));
                DebugLog.Message("Fluffy's Work Tab detected - shifted its priority colors and warning threshold as well.");
                if (drawPriority != null)
                {
                    harmony.Patch(drawPriority,
                        transpiler: new HarmonyMethod(typeof(Patch_WorkTab_DrawUtilities), nameof(DrawPriorityTranspiler)));
                    DebugLog.Message("Work Tab priority numbers relabelled to the shifted ladder.");
                }
                else
                {
                    DebugLog.Warning("Work Tab's DrawPriority is missing (update?) - its cells keep vanilla numbers.");
                }
            }
            catch (Exception e)
            {
                Log.Error("[RecolorWorkPriorities] Work Tab compatibility patch failed (it stays unpatched). " + e.Message);
            }
        }

        private static void WorkTypeTipPostfix(Pawn pawn, WorkTypeDef worktype, ref string __result)
        {
            if (!RecolorWorkPrioritiesMod.Active
                || RecolorWorkPrioritiesMod.WarningSkillThreshold <= 2f
                || worktype.relevantSkills.Count == 0
                || pawn.workSettings == null)
            {
                return;
            }
            float skill = pawn.skills.AverageOfRelevantSkillsFor(worktype);
            if (skill <= 2f
                || skill > RecolorWorkPrioritiesMod.WarningSkillThreshold
                || !pawn.workSettings.WorkIsActive(worktype))
            {
                return;
            }
            // Work Tab appends this line uncolored - match its own style.
            __result += "\n\n" + "SelectedWorkTypeWithVeryBadSkill".Translate();
        }

        private static IEnumerable<CodeInstruction> DrawPriorityTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            return PriorityLabelTranspiler.ReplaceToStringWithLabel(
                instructions, "WorkTab.DrawUtilities.DrawPriority");
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
            else if (priority == 4 && RecolorWorkPrioritiesMod.Priority4Customized)
            {
                __result = settings.priority4Color;
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
