using System;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace RecolorWorkPriorities
{
    /// <summary>Debug verbosity levels. Stored as int in settings so enum
    /// reordering can never corrupt saves.</summary>
    public enum DebugLogLevel
    {
        Off = 0,
        Basic = 1,
        Verbose = 2
    }

    public class RecolorWorkPrioritiesSettings : ModSettings
    {
        public bool enabled = true;

        /// <summary>Priority 1 color. Default cyan (0, 1, 1) - the new top
        /// color that replaces vanilla's green, chosen so it reads as
        /// "highest priority" and no longer collides with tier 2.</summary>
        public Color priority1Color = new Color(0f, 1f, 1f);

        /// <summary>Priority 2 color. Default is vanilla's priority 1 green
        /// (0, 1, 0) - the color moved one tier down.</summary>
        public Color priority2Color = new Color(0f, 1f, 0f);

        /// <summary>Priority 3 color. Default is vanilla's priority 2 light
        /// yellow (1, 0.9, 0.5) - the color moved one tier down.</summary>
        public Color priority3Color = new Color(1f, 0.9f, 0.5f);

        /// <summary>Skill below which the low-skill warning border (and the
        /// matching tooltip line) is shown for active work. Vanilla compares
        /// against 2; the default 3 moves the border one tier up.</summary>
        public float warningSkillThreshold = 3f;

        public int debugLevel = (int)DebugLogLevel.Off;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref enabled, "enabled", true);
            Scribe_Values.Look(ref priority1Color, "priority1Color", new Color(0f, 1f, 1f));
            Scribe_Values.Look(ref priority2Color, "priority2Color", new Color(0f, 1f, 0f));
            Scribe_Values.Look(ref priority3Color, "priority3Color", new Color(1f, 0.9f, 0.5f));
            Scribe_Values.Look(ref warningSkillThreshold, "warningSkillThreshold", 3f);
            Scribe_Values.Look(ref debugLevel, "debugLevel", (int)DebugLogLevel.Off);
        }
    }

    public class RecolorWorkPrioritiesMod : Mod
    {
        public const string PackageId = "Riketta.RecolorWorkPriorities";

        public static RecolorWorkPrioritiesSettings Settings;

        /// <summary>Master switch, read by every patch on each call. Null-safe:
        /// without settings the patches stay active rather than silently
        /// disabling the mod.</summary>
        public static bool Active => Settings?.enabled ?? true;

        /// <summary>Live mirror of Settings.warningSkillThreshold for the
        /// transpiled checks: the patched IL loads this static field, so
        /// slider changes apply to the work tab immediately, without a
        /// restart. Kept clamped to sane skill values.</summary>
        public static float WarningSkillThreshold = 3f;

        /// <summary>Vanilla 1.6 priority colors, kept for the settings reset
        /// button and as the documented source of the shifted defaults.</summary>
        public static readonly Color VanillaPriority1 = new Color(0f, 1f, 0f);
        public static readonly Color VanillaPriority2 = new Color(1f, 0.9f, 0.5f);
        public static readonly Color VanillaPriority3 = new Color(0.8f, 0.7f, 0.5f);

        public RecolorWorkPrioritiesMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<RecolorWorkPrioritiesSettings>();
            SyncStatics();
            // Patch each class separately: a game update that renames one target
            // must degrade to "that vanilla behavior stays", never break the rest.
            Harmony harmony = new Harmony(PackageId);
            PatchSafe(harmony, typeof(Patch_WidgetsWork_ColorOfPriority));
            PatchSafe(harmony, typeof(Patch_WidgetsWork_DrawWorkBoxBackground));
            PatchSafe(harmony, typeof(Patch_WidgetsWork_TipForPawnWorker));
            Patch_WorkTab_DrawUtilities.TryApply(harmony);
            DebugLog.Message("loaded (enabled=" + (Settings.enabled ? "true" : "false")
                + ", p1=" + Settings.priority1Color
                + ", p2=" + Settings.priority2Color
                + ", p3=" + Settings.priority3Color
                + ", warningSkillThreshold=" + Settings.warningSkillThreshold
                + ", debugLevel=" + (DebugLogLevel)Settings.debugLevel + ").");
        }

        /// <summary>Pushes settings into the static field the patched work
        /// tab IL reads. Called on load and whenever the threshold slider
        /// moves.</summary>
        public static void SyncStatics()
        {
            WarningSkillThreshold = Mathf.Clamp(Settings?.warningSkillThreshold ?? 3f, 0f, 20f);
        }

        private static void PatchSafe(Harmony harmony, Type patchClass)
        {
            try
            {
                harmony.CreateClassProcessor(patchClass).Patch();
                DebugLog.Message("applied " + patchClass.Name + ".");
            }
            catch (Exception e)
            {
                Log.Error("[RecolorWorkPriorities] Patch " + patchClass.Name + " could not be applied (game update?). " + e.Message);
            }
        }

        public override string SettingsCategory()
        {
            return "RecolorWorkPriorities.SettingsCategory".Translate();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard list = new Listing_Standard();
            list.Begin(inRect);

            list.CheckboxLabeled("RecolorWorkPriorities.Enabled".Translate(), ref Settings.enabled,
                "RecolorWorkPriorities.Enabled.Tip".Translate());
            list.Gap(8f);

            ColorRow(list, "RecolorWorkPriorities.Priority1Color".Translate(),
                "RecolorWorkPriorities.Priority1Color.Tip".Translate(),
                () => Settings.priority1Color, c => Settings.priority1Color = c);
            ColorRow(list, "RecolorWorkPriorities.Priority2Color".Translate(),
                "RecolorWorkPriorities.Priority2Color.Tip".Translate(),
                () => Settings.priority2Color, c => Settings.priority2Color = c);
            ColorRow(list, "RecolorWorkPriorities.Priority3Color".Translate(),
                "RecolorWorkPriorities.Priority3Color.Tip".Translate(),
                () => Settings.priority3Color, c => Settings.priority3Color = c);
            list.Gap(4f);

            Rect resetRect = list.GetRect(30f);
            if (Widgets.ButtonText(resetRect, "RecolorWorkPriorities.ResetColors".Translate()))
            {
                Settings.priority1Color = new Color(0f, 1f, 1f);
                Settings.priority2Color = VanillaPriority1;
                Settings.priority3Color = VanillaPriority2;
            }
            TooltipHandler.TipRegion(resetRect, "RecolorWorkPriorities.ResetColors.Tip".Translate());
            list.Gap(8f);

            float threshold = Settings.warningSkillThreshold;
            threshold = list.SliderLabeled(
                "RecolorWorkPriorities.WarningThreshold".Translate(Mathf.RoundToInt(threshold)),
                threshold, 0f, 20f, 0.6f, "RecolorWorkPriorities.WarningThreshold.Tip".Translate());
            threshold = Mathf.Round(threshold);
            if (!Mathf.Approximately(threshold, Settings.warningSkillThreshold))
            {
                Settings.warningSkillThreshold = threshold;
                SyncStatics();
            }
            list.Gap(8f);

            Rect debugRect = list.GetRect(30f);
            string levelName = ((DebugLogLevel)Settings.debugLevel).ToString();
            if (Widgets.ButtonText(debugRect, "RecolorWorkPriorities.DebugLevel".Translate(levelName)))
            {
                Settings.debugLevel = (Settings.debugLevel + 1) % 3;
            }
            TooltipHandler.TipRegion(debugRect, "RecolorWorkPriorities.DebugLevel.Tip".Translate());
            list.Gap(12f);

            GUI.color = ColoredText.SubtleGrayColor;
            list.Label("RecolorWorkPriorities.BehaviorNote".Translate());
            GUI.color = Color.white;
            list.End();
        }

        /// <summary>One settings row: label on the left, color swatch on the
        /// right; clicking the swatch opens the vanilla color picker dialog.</summary>
        private static void ColorRow(Listing_Standard list, TaggedString label, TaggedString tip,
            Func<Color> get, Action<Color> set)
        {
            Rect row = list.GetRect(30f);
            Rect swatch = row.RightPartPixels(36f);
            Rect labelRect = row.LeftPartPixels(row.width - swatch.width - 8f);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(labelRect, label);
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.DrawBoxSolid(swatch.ContractedBy(4f), get());
            Widgets.DrawBox(swatch, 1);
            if (Widgets.ButtonInvisible(swatch))
            {
                Find.WindowStack.Add(new Dialog_PriorityColorPicker(get(), set));
            }
            TooltipHandler.TipRegion(row, tip);
            list.Gap(2f);
        }
    }
}
