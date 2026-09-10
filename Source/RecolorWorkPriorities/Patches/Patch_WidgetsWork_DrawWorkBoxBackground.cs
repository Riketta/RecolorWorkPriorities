using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace RecolorWorkPriorities
{
    /// <summary>Vanilla draws its low-skill warning border (plus the ideo
    /// precept overlay and passion icon) inside this private background
    /// helper - and Fluffy's Work Tab calls the same method through
    /// reflection for its own boxes, so a postfix here covers both tabs.
    ///
    /// Instead of editing the method's IL, the border for the extra tiers is
    /// added additively: whenever the configured threshold sits above
    /// vanilla's 2, boxes in the 2..threshold skill band get the same overlay
    /// texture vanilla draws for its own band. If a game update reworks the
    /// vanilla condition, the worst case is a cosmetically off extra border -
    /// never a missing or broken one.</summary>
    [HarmonyPatch]
    internal static class Patch_WidgetsWork_DrawWorkBoxBackground
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(WidgetsWork), "DrawWorkBoxBackground");
        }

        private static void Postfix(Rect rect, Pawn p, WorkTypeDef workDef)
        {
            if (!RecolorWorkPrioritiesMod.Active
                || RecolorWorkPrioritiesMod.WarningSkillThreshold <= 2f
                || workDef.relevantSkills.Count == 0
                || p.workSettings == null)
            {
                return;
            }
            float skill = p.skills.AverageOfRelevantSkillsFor(workDef);
            if (skill <= 2f
                || skill > RecolorWorkPrioritiesMod.WarningSkillThreshold
                || !p.workSettings.WorkIsActive(workDef))
            {
                return;
            }
            GUI.color = Color.white;
            GUI.DrawTexture(rect.ContractedBy(-2f), WidgetsWork.WorkBoxOverlay_Warning);
        }
    }
}
