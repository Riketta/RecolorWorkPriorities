# Steam Workshop description

Paste the text below into the Workshop item's description field when publishing
(the BBCode renders on Steam, but not in-game - `About/About.xml` carries its own
plain-text description).

```
[h3]Recolor Work Priorities[/h3]
Moves the work tab's priority color ladder one tier up: priority 2 wears priority 1's green, priority 3 wears priority 2's yellow, and top priority gets a new cyan that stands out from both at a glance. Priority 4 stays vanilla grey by default.

[h3]What it does[/h3]
[list][*]Recolors the manual work priority numbers: 1 = cyan, 2 = green, 3 = light yellow - the vanilla ladder shifted one tier down, so the top tier no longer looks like every other green checkbox in the game. Priority 4 stays grey by default.
[*]All four priority colors are configurable in mod settings, with a full color picker and a one-click reset to the default scheme.
[*]Moves the low-skill warning border one tier up too: work boxes of active work at skill 3 and below are highlighted (vanilla: 2 and below). The threshold is a slider; the tooltip warning follows it, and warning text keeps its familiar yellow.
[*]Works with the vanilla work tab and with Fluffy's Work Tab - when present, its detailed priority colors are shifted the same way and its warning border follows the same threshold.[/list]

[h3]Settings[/h3]
Master switch, four configurable priority colors (with vanilla presets in the picker), the low-skill warning threshold, and debug logging (Off / Basic / Verbose).

[h3]Compatibility[/h3]
Requires RimWorld 1.6 and the [url=https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077]Harmony[/url] mod. All DLCs are optional.
Nothing is hardcoded to specific work types; patches target the game's drawing functions and degrade gracefully - if an update moves something, that piece stays vanilla instead of erroring. Safe to add or remove at any time.

Source code and details: [url]https://github.com/Riketta/RecolorWorkPriorities[/url]
```
