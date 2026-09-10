# Recolor Work Priorities

A RimWorld mod that moves the work tab's priority color ladder one tier up.
Priority **2** wears priority 1's green, priority **3** wears priority 2's
yellow, and top priority gets a new **cyan** that stands out from both at a
glance - so the colors read like urgency instead of a rainbow. The low-skill
warning border moves one tier up as well.

Requires the [Harmony mod](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077).
Works with all DLC, safe to add or remove at any time.

## How it looks

With manual priorities enabled (the work tab shows numbers 1-4), the color
ladder changes:

| priority | vanilla color | with this mod |
|---|---|---|
| 1 | green | **cyan** |
| 2 | light yellow | green (vanilla 1) |
| 3 | tan | light yellow (vanilla 2) |
| 4 | grey | grey (default, configurable) |
| inactive | grey | grey (unchanged) |

All four colors are configurable, so the ladder can be tuned to any scheme -
the vanilla-to-cyan shift is just the default.

## The warning border

Vanilla draws a warning border around work boxes where the pawn is actively
assigned work but has an average relevant skill of **2 or below**, and adds a
"very bad skill" line to the priority tooltip. This mod moves that check one
tier up: the border appears from **skill 3 and below** by default, so
mediocre-but-not-hopeful assignments are flagged too.

The threshold is a slider (0-20, default 3); set it back to 2 for vanilla
behavior. The tooltip warning follows the border's threshold, and its text
keeps the yellow look it had before the color shift (it reads the color one
tier up from the shifted ladder).

## Mod settings

- **Enabled** - master switch. While off, the work tab keeps its vanilla
  colors and vanilla skill-2 border; changes apply immediately on re-enable.
- **Priority 1-4 colors** - click a swatch to open the vanilla color picker
  (full RGBA/HSV, preset palette includes the vanilla tier colors). Priority
  4 defaults to vanilla grey, so the lowest tier is unchanged unless you pick
  a color.
- **Reset colors to defaults** - back to cyan / green / yellow / grey.
- **Low-skill warning border** - the skill threshold described above.
- **Debug logging** - Off / Basic (loading, applied patches, transpiler
  outcomes) / Verbose (Work Tab compat details, reference counts).

## Compatibility

- **Vanilla work tab** - the primary target.
- **Fluffy's Work Tab** - detected automatically. That mod ships its own
  private color function (a green-white-grey gradient), so the mod replicates
  the gradient and shifts it the same way: 2 wears 1's gradient color, 3 wears
  2's, 1 wears the configured cyan. Its tier 4 is a gradient endpoint rather
  than vanilla grey, so it keeps that look until you customize the fourth
  color - then the setting applies there too. Its warning border
  is drawn by the (patched) vanilla background helper, so it follows the same
  threshold. Only its work *type* tooltip is tiered up; its detailed
  workgiver tooltips never colored their warning lines.
- **Other work tab / priority mods** - anything that draws priority numbers
  through vanilla's `WidgetsWork.ColorOfPriority` gets the shifted colors for
  free; anything with its own color code (like Fluffy's) is unaffected unless
  explicitly supported above.
- No def lists, no hardcoded work types - the patches target the game's
  drawing functions, not content.

## Technical notes

For modders and the curious - all patches are applied individually with
graceful degradation: if a game update renames a target, that one behavior
stays vanilla and a warning is logged, never a cascade of errors.

- `WidgetsWork.ColorOfPriority(int)` - postfix remaps 1-4 to the configured
  colors. Called per work box per frame, so the patch is allocation-free and
  logging-free; the master switch is a single bool read.
- `WidgetsWork.DrawWorkBoxBackground` (private) - transpiler replaces the
  single `skill <= 2f` constant with a load of the settings-driven threshold
  static field, so slider changes apply live. Fluffy's Work Tab invokes this
  method via reflection, which is why one transpiler covers both tabs. A
  match count of anything but exactly 1 aborts the edit and logs.
- `WidgetsWork.TipForPawnWorker(Pawn, WorkTypeDef, bool)` - same threshold
  edit, plus bumping the constant `2` passed to `ColorOfPriority` for the two
  warning lines up to `3`, keeping their pre-shift color.
- `WorkTab.DrawUtilities` (Fluffy's, conditional) - postfix on its private
  `ColorOfPriority` shifts tiers 1-3 using a replica of its gradient, and
  applies the fourth color once it is customized; its `maxPriority` setting
  is read via reflection once per frame (not per cell). Its work type tooltip
  gets the same two transpilers. Missing internals at patch time or runtime
  degrade to "Work Tab stays unpatched".
- Settings live in `RecolorWorkPrioritiesSettings`; the threshold is mirrored
  into a static field the patched IL reads (`SyncStatics`), which is what
  makes slider changes immediate.

## Build from source

Requires the .NET SDK and a RimWorld 1.6 install. Build the Release
configuration for the dll you ship - a plain `dotnet build` defaults to Debug:

```
cd Source/RecolorWorkPriorities
dotnet build -c Release -p:RimWorldDir="C:\Path\To\RimWorld"
```

The output lands in `Assemblies/RecolorWorkPriorities.dll`. Copy or symlink
the whole `RecolorWorkPriorities` folder into the game's `Mods` directory to
try it (with the Harmony mod loaded before it).
