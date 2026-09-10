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
the vanilla-to-cyan shift is just the default. In practice the shift is aimed
at colonies that treat priority 2 as the everyday default and reserve tier 1
for extreme urgencies: the everyday crowd keeps the familiar green the old
top tier wore, so there is nothing to re-learn.

## The warning border

Vanilla draws a warning border around work boxes where the pawn is actively
assigned work but has an average relevant skill of **2 or below**, and adds a
"very bad skill" line to the priority tooltip. This mod adds the same border
and line for the next band up: skill 3 and below by default (the threshold
is a slider, 0-20). Warning text keeps vanilla's convention of wearing
priority 2's color - under the shifted color ladder that is simply green.

## Priority labels

While **Shift priority labels** is on, the drawn numbers follow the shifted
ladder: work boxes read **A / 1 / 2 / 3** instead of 1 / 2 / 3 / 4, and the
priority tooltip matches ("Priority A", "Priority 1", ...).

- The top tier shows a letter label - default **"A"**, as in afterburner
  (the scheme reads like gears: A is one step above 1st). It is a setting,
  so any 1-2 character label works ("!" also reads nicely, but visually
  collides with the low-skill warning border).
- Only the glyphs change: the real priority values stay 0-4 underneath, so
  saves, job assignment, shift click-cycling, copy/paste and other mods all
  keep seeing vanilla priorities.
- Fluffy's Work Tab cells are relabelled too; its tooltips spell priorities
  out in words ("Top", "High", ...) which stay as-is.

## Mod settings

- **Enabled** - master switch. While off, the work tab keeps its vanilla
  colors and vanilla skill-2 border; changes apply immediately on re-enable.
- **Priority 1-4 colors** - click a swatch to open the vanilla color picker
  (full RGBA/HSV, preset palette includes the vanilla tier colors). Priority
  4 defaults to vanilla grey, so the lowest tier is unchanged unless you pick
  a color.
- **Reset colors to defaults** - back to cyan / green / yellow / grey.
- **Shift priority labels** - draws A / 1 / 2 / 3 instead of 1 / 2 / 3 / 4.
- **Top tier label** - the letter shown on priority 1 boxes (default "A",
  up to 2 characters, empty falls back to "A").
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

For modders and the curious - every patch is a plain Harmony prefix/postfix
with graceful degradation, except two small IL edits (the drawn number swap
and the tooltip label block) that have no postfix equivalent which would not
double the per-cell drawing cost, copy vanilla rendering, or do fragile
string surgery on localized rich text. If a game update renames a target,
that one behavior stays vanilla and a warning is logged, never a cascade of
errors.

- `WidgetsWork.ColorOfPriority(int)` - postfix remaps 1-4 to the configured
  colors. Called per work box per frame, so the patch is allocation-free and
  logging-free; the master switch is a single bool read.
- `WidgetsWork.DrawWorkBoxFor` - the one transpiler: replaces the drawn
  priority's `ToStringCached` call with `DisplayLabelOf`, so cells read
  TopLabel / 1 / 2 / 3 while real values stay 0-4. A named-call swap with an
  occurrence-count check - any game change falls back to vanilla numbers
  with a logged warning. The top label is read from a static mirror synced
  by `SyncStatics`, never re-trimmed per cell, and the function returns
  literals or vanilla's cached ints (allocation-free hot path).
- `WidgetsWork.DrawWorkBoxBackground` (private) - plain postfix: draws the
  vanilla warning overlay texture for the 2..threshold skill band, exactly
  the way vanilla draws it for its own band. Additive only - if vanilla
  reworks its condition the worst case is a cosmetically off extra border.
  Fluffy's Work Tab invokes this method via reflection, which is why one
  postfix covers both tabs.
- `WidgetsWork.TipForPawnWorker(Pawn, WorkTypeDef, bool)` - one transpiler
  rewrites the `("Priority" + n).Translate()` block so tooltips use the same
  shifted glyphs as the cells in every language; the block shape (constant,
  int load, box, Concat, Translate, string cast) is validated and falls back
  to vanilla text on mismatch. A postfix appends the same "very bad skill"
  warning line vanilla adds - for the 2..threshold band the border extension
  covers - with the same key and the same tier-2 color convention.
- `WorkTab.DrawUtilities` (Fluffy's, conditional) - postfix on its private
  `ColorOfPriority` shifts tiers 1-3 using a replica of its gradient, and
  applies the fourth color once it is customized; its `maxPriority` setting
  is read via reflection once per frame (not per cell). A postfix appends
  the extended warning line to its work type tooltip (uncolored, like its
  own); its cells are relabelled through its `DrawPriority` via the shared
  transpiler. Missing internals at patch time or runtime degrade to
  "Work Tab stays unpatched".
- Settings live in `RecolorWorkPrioritiesSettings`; the warning threshold
  and the top label are mirrored into static fields (`SyncStatics`) so
  setting changes apply to the open work tab immediately.

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
