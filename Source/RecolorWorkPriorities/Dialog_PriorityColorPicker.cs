using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace RecolorWorkPriorities
{
    /// <summary>Vanilla color picker wired to a settings color. Unlike the
    /// glower picker it does not force the HSV value, so dark colors can be
    /// picked; the preset palette includes the vanilla priority colors for
    /// reference. SaveColor fires when the player confirms.</summary>
    internal class Dialog_PriorityColorPicker : Dialog_ColorPickerBase
    {
        private readonly Color initial;
        private readonly Action<Color> onSave;

        private static readonly List<Color> Presets = new List<Color>
        {
            new Color(0f, 1f, 1f),          // cyan (default priority 1)
            RecolorWorkPrioritiesMod.VanillaPriority1,
            RecolorWorkPrioritiesMod.VanillaPriority2,
            RecolorWorkPrioritiesMod.VanillaPriority3,
            RecolorWorkPrioritiesMod.VanillaPriority4,
            Color.red,
            Color.yellow,
            Color.white,
            Color.grey,
            Color.blue,
            Color.magenta
        };

        public Dialog_PriorityColorPicker(Color current, Action<Color> onSave)
            : base(Widgets.ColorComponents.All, Widgets.ColorComponents.All)
        {
            initial = current;
            color = current;
            oldColor = current;
            this.onSave = onSave;
        }

        protected override bool ShowDarklight => false;

        protected override Color DefaultColor => initial;

        protected override List<Color> PickableColors => Presets;

        // Negative disables the forced HSV value, leaving brightness to the player.
        protected override float ForcedColorValue => -1f;

        protected override bool ShowColorTemperatureBar => false;

        protected override void SaveColor(Color savedColor)
        {
            savedColor.a = 1f;
            onSave(savedColor);
        }
    }
}
