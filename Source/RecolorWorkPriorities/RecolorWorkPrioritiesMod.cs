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

        /// <summary>Priority 4 color. Default is vanilla's priority 4 grey
        /// (0.74, 0.74, 0.74), so the lowest tier is unchanged unless the
        /// player picks a custom color.</summary>
        public Color priority4Color = RecolorWorkPrioritiesMod.VanillaPriority4;

        /// <summary>Relabels drawn priorities: 2 shows as 1, 3 as 2, 4 as 3,
        /// and the top tier shows topLabel instead of a number. The real
        /// values 0-4 stay untouched underneath - saves, job logic and other
        /// mods keep seeing vanilla priorities.</summary>
        public bool shiftLabels = true;

        /// <summary>Label shown for top-priority (1) work boxes while labels
        /// are shifted. Default "A" (afterburner). Kept to 2 characters so
        /// it fits the work box; empty falls back to "A" in display.</summary>
        public string topLabel = "A";

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
            Scribe_Values.Look(ref priority4Color, "priority4Color", RecolorWorkPrioritiesMod.VanillaPriority4);
            Scribe_Values.Look(ref warningSkillThreshold, "warningSkillThreshold", 3f);
            Scribe_Values.Look(ref shiftLabels, "shiftLabels", true);
            Scribe_Values.Look(ref topLabel, "topLabel", "A");
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
        /// warning-border postfix: read on every drawn work box, so slider
        /// changes apply to the work tab immediately, without a restart.
        /// Kept clamped to sane skill values.</summary>
        public static float WarningSkillThreshold = 3f;

        /// <summary>Live mirror of Settings.topLabel for the relabelled cell
        /// drawing: DisplayLabelOf (called from the patched DrawWorkBoxFor)
        /// reads this cached, sanitized string instead of trimming settings
        /// text on every drawn box. Updated by SyncStatics; never empty
        /// (falls back to "A").</summary>
        public static string TopLabel = "A";

        /// <summary>Vanilla 1.6 priority colors, kept for the settings reset
        /// button and as the documented source of the shifted defaults.</summary>
        public static readonly Color VanillaPriority1 = new Color(0f, 1f, 0f);
        public static readonly Color VanillaPriority2 = new Color(1f, 0.9f, 0.5f);
        public static readonly Color VanillaPriority3 = new Color(0.8f, 0.7f, 0.5f);
        public static readonly Color VanillaPriority4 = new Color(0.74f, 0.74f, 0.74f);

        /// <summary>True when the priority 4 color was changed from its
        /// vanilla default. Work Tab's own tier-4 look is a gradient endpoint
        /// rather than vanilla grey, so it is only overridden when the player
        /// actually picks a custom fourth color.</summary>
        public static bool Priority4Customized =>
            Settings != null && !(Settings.priority4Color == VanillaPriority4);

        public RecolorWorkPrioritiesMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<RecolorWorkPrioritiesSettings>();
            SyncStatics();
            // Patch each class separately: a game update that renames one target
            // must degrade to "that vanilla behavior stays", never break the rest.
            Harmony harmony = new Harmony(PackageId);
            PatchSafe(harmony, typeof(Patch_WidgetsWork_ColorOfPriority));
            PatchSafe(harmony, typeof(Patch_WidgetsWork_DrawWorkBoxBackground));
            PatchSafe(harmony, typeof(Patch_WidgetsWork_DrawWorkBoxFor));
            PatchSafe(harmony, typeof(Patch_WidgetsWork_TipForPawnWorker));
            Patch_WorkTab_DrawUtilities.TryApply(harmony);
            DebugLog.Message("loaded (enabled=" + (Settings.enabled ? "true" : "false")
                + ", p1=" + Settings.priority1Color
                + ", p2=" + Settings.priority2Color
                + ", p3=" + Settings.priority3Color
                + ", p4=" + Settings.priority4Color
                + ", warningSkillThreshold=" + Settings.warningSkillThreshold
                + ", shiftLabels=" + (Settings.shiftLabels ? "true" : "false")
                + ", topLabel=" + Settings.topLabel
                + ", debugLevel=" + (DebugLogLevel)Settings.debugLevel + ").");
        }

        /// <summary>Pushes settings into the static field the patched work
        /// tab IL reads. Called on load and whenever the threshold slider
        /// moves.</summary>
        public static void SyncStatics()
        {
            WarningSkillThreshold = Mathf.Clamp(Settings?.warningSkillThreshold ?? 3f, 0f, 20f);
            string label = Settings?.topLabel;
            TopLabel = string.IsNullOrEmpty(label) ? "A" : label.Trim();
        }

        /// <summary>The glyph drawn for a priority: with label shifting on,
        /// the ladder reads TopLabel / 1 / 2 / 3 instead of 1 / 2 / 3 / 4.
        /// Returns literals or vanilla's cached ints - allocation-free for
        /// the per-cell drawing hot path.</summary>
        public static string DisplayLabelOf(int priority)
        {
            RecolorWorkPrioritiesSettings settings = Settings;
            if (settings == null || !settings.shiftLabels)
            {
                return priority.ToStringCached();
            }
            switch (priority)
            {
                case 1: return TopLabel;
                case 2: return "1";
                case 3: return "2";
                case 4: return "3";
                default: return priority.ToStringCached();
            }
        }

        /// <summary>Tooltip text for a priority, using the same shifted
        /// glyphs as the cells ("Priority A" ... "Priority 3"); with label
        /// shifting off this resolves to the vanilla wording. Returns a
        /// plain string because the transpiler splices this call in place of
        /// a block whose result was already cast back to string.</summary>
        public static string PriorityTip(int priority)
        {
            return (string)"RecolorWorkPriorities.PriorityTip".Translate(DisplayLabelOf(priority));
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

        /// <summary>Clamps the top-tier label to what fits a work box:
        /// trimmed and at most 2 characters. Empty stays empty in the text
        /// field (mid-typing is not fought with); display falls back to
        /// "A" in SyncStatics.</summary>
        private static string TruncateTopLabel(string value)
        {
            value = value?.Trim() ?? string.Empty;
            return value.Length <= 2 ? value : value.Substring(0, 2);
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
            ColorRow(list, "RecolorWorkPriorities.Priority4Color".Translate(),
                "RecolorWorkPriorities.Priority4Color.Tip".Translate(),
                () => Settings.priority4Color, c => Settings.priority4Color = c);
            list.Gap(4f);

            Rect resetRect = list.GetRect(30f);
            if (Widgets.ButtonText(resetRect, "RecolorWorkPriorities.ResetColors".Translate()))
            {
                Settings.priority1Color = new Color(0f, 1f, 1f);
                Settings.priority2Color = VanillaPriority1;
                Settings.priority3Color = VanillaPriority2;
                Settings.priority4Color = VanillaPriority4;
            }
            TooltipHandler.TipRegion(resetRect, "RecolorWorkPriorities.ResetColors.Tip".Translate());
            list.Gap(8f);

            list.CheckboxLabeled("RecolorWorkPriorities.ShiftLabels".Translate(), ref Settings.shiftLabels,
                "RecolorWorkPriorities.ShiftLabels.Tip".Translate());
            list.Gap(4f);

            Rect topLabelRect = list.GetRect(Text.LineHeight);
            string topLabelInput = Widgets.TextEntryLabeled(topLabelRect,
                "RecolorWorkPriorities.TopLabel".Translate(), Settings.topLabel);
            TooltipHandler.TipRegion(topLabelRect, "RecolorWorkPriorities.TopLabel.Tip".Translate());
            list.Gap(list.verticalSpacing);
            Settings.topLabel = TruncateTopLabel(topLabelInput);
            SyncStatics();
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
