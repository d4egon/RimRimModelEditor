# RimRimModelEditor
A sort of IDE for modders to use in-game to manipulate textures and view their changes live.


This is the beginning of... A powerful in-game model/texture editor for modding.

&#x20; I often found myself wondering how an item would look if placed slightly different, or what motes or flecks look like if they have different settings.

&#x20; 

&#x20; This mod does that - live. You can literally edit any texture while in the game in this "IDE". And you can export the settings as XML.

&#x20; 

&#x20; along with a whole lot of other things, some work and some dont. That's why I call it ALPHA. 

&#x20; 

&#x20; Any and all feedback is more than welcome. I hope this to be a tribute to the amazing workshop community and the RimWorld community.

&#x20; 

&#x20; We have had so much fun over the years.

&#x20; 

&#x20; Full list of current features is available in the readme.md and on the steam workshop page.

&#x20; 

&#x20; ## Current Features



\*\*Studio \& Scene Management\*\*

\- Multi-scene support — open multiple studio projects simultaneously as named tabs

\- Rename scenes with custom names (falls back to first def name or "Empty Scene")

\- Close individual scenes; always keeps at least one open

\- Undo / Redo with configurable stack depth (tied to settings slider)

\- Named version history (History tab) with restore points

\- Deep-clone scene state for safe undo snapshots



\*\*Def Browser (Left Panel)\*\*

\- Browse ThingDefs, FleckDefs, EffecterDefs, PawnKindDefs

\- Search/filter by defName

\- ThingDefs with an active layer → assign texture to that layer directly

\- FleckDefs, EffecterDefs, PawnKindDefs → always open as their own scene object in a new tab

\- Animated fleck path resolution (tries `\_0`, `\_1`, `\_a` frame suffixes)



\*\*Preview Panel\*\*

\- Real-time composited layer rendering

\- Zoom (scroll wheel) and pan (middle mouse drag)

\- Directional preview — rotate between South / East / West / North

\- West direction auto-mirrors the East texture when no explicit `\_west` variant exists

\- Animation frame scrubbing (horizontal UV strip support)

\- Checkerboard transparency background

\- Custom background — solid colour presets (Black, Dark Grey, Mid Grey, White, Sky Blue, Warm Parchment, Forest Green, Deep Space) or load from RimWorld content path or external file

\- Background opacity slider

\- Collision overlay

\- Placement overlay



\*\*Layer System\*\*

\- Add, delete, move up/down layers

\- Per-layer: position (X/Y), scale (X/Y with linked-axis toggle), rotation, opacity, colour tint (RGB sliders)

\- Layer visibility toggle

\- Load custom texture from disk (PNG/JPG)

\- Directional texture resolution (`\_south`, `\_east`, `\_north`, `\_west`)

\- Body-type variant chaining for apparel (`Duster\_Male\_south`, `Duster\_Fat\_south`, etc.)

\- Draw order by priority (Body → Apparel → Head → Beard → Hair → HeadAttachment → Hat → Weapon)



\*\*Pawn Compositing\*\*

\- Automatic body + head + hair + apparel + weapon layer stack from PawnKindDef

\- Runtime texture path discovery via PawnTextureScanner (handles RimWorld 1.4 / 1.5 / 1.6 path differences)

\- 1.6 head subfolder support (`Heads/Male/`, `Heads/Female/`, `HeavyJaw` skull type, gender-neutral heads)

\- Skin tone picker (Light / Medium / Brown / Dark)

\- Body type picker (Thin / Fat / Hulk / Male / Female)

\- Head type picker (all discovered paths + manual entry fallback)

\- Hair colour tint picker

\- Apparel body-type variant picker

\- Quick-add Beard layer (auto-positioned at head, priority 3)

\- Quick-add Head Attachment layer (eye patch, horns, scars — priority 5)



\*\*Inspector Panel\*\*

\- Per-object: Draw Size, Draw Offset, Root Rotation

\- Per-layer: Position, Scale (with linked axes), Rotation, Opacity, RGB colour channels

\- Double-click any label to reset to neutral value

\- Numeric input field next to every slider for exact values

\- ThingDef-specific: Muzzle Flash Scale, Equipped Angle Offset

\- FleckDef-specific: Growth Rate



\*\*XML Export\*\*

\- ThingDef XML generation (graphicData, graphicClass, drawSize, drawOffset, colour tint, weapon angle)

\- FleckDef XML (fade in/solid/fade out timing, growth rate)

\- EffecterDef XML

\- PawnKindDef XML (body + head graphicData in lifeStages)

\- All-layers export — every layer as its own ThingDef block with full position/scale/colour annotations, sorted by priority

\- Live XML preview tab

\- Batch export



\*\*Screenshot \& Portrait Export\*\*

\- Full-window screenshot → `Desktop/RimRimScreenshots/` (timing-correct, no white box)

\- Preview panel snapshot — captures only the preview area

\- Export dialog with three modes:

&#x20; - \*\*Entire Scene\*\* — snapshot → wall-mounted portrait painting mod

&#x20; - \*\*Current Layer\*\* — active layer texture → portrait painting mod

&#x20; - \*\*Scene as XML\*\* — all layers as annotated ThingDef XML file

\- Single shared portrait mod — all portraits accumulate in one mod folder

\- Auto-numbered defNames when a portrait already exists (`Portrait\_Bob` → `Portrait\_Bob\_2`)

\- `About.xml` written once, never overwritten (preserves user edits)

\- Correct `BuildingOnTop` altitude layer, `PlaceWorker\_WallAttachment`, Beauty 15, Furniture architect tab



\*\*Toolbar\*\*

\- Category selector (switches active def type)

\- Undo / Redo buttons

\- Save Preset

\- Export XML

\- Background picker (colour presets + RimWorld path + file + opacity + clear)

\- Screenshot

\- Snapshot (opens export dialog)

\- Hot-Reload (force-reloads selected def textures without restarting)

\- Share



\*\*Stability \& Bug Fixes\*\*

\- Null-safe layer names throughout (`def.label ?? def.defName ?? "Base"`)

\- BeginScrollView/EndScrollView always balanced (null guards prevent mid-draw exceptions)

\- `ThingDef.graphic` never accessed at runtime (uses `graphicData.texPath` only)

\- FleckDef/EffecterDef always routed to own scene, never smashed onto pawn layers

\- Animated fleck textures handled gracefully (no crash on folder paths or null graphicData)



\*\*Tooltips\*\*

\- Every toolbar button (with keyboard shortcuts where applicable)

\- Every layer panel button and pawn picker row

\- Every layer row (shows full texture path, priority, opacity, position, scale on hover)

\- Every inspector slider (shows range, current value, double-click hint)

\- Vector2 link button

\- Load Custom Texture button



\*\*Keyboard Shortcuts\*\*

\- Ctrl+Z / Ctrl+Y — Undo / Redo

\- Ctrl+S — Save Preset

\- Ctrl+E — Export XML

\- Ctrl+F12 — Screenshot

\- Ctrl+R — Hot-Reload

\- Ctrl+L — Add Layer

\- Ctrl+Delete — Delete Layer

\- Zoom In / Out / Reset View

\- Toggle Collision / Placement overlays

\- Next / Previous animation frame





\#############Exporting#############
When you use snapshot export the best idea is to export it to your mod folder. It will recognize previous snapshots and make new ones so you evolve your repository of custom snapshots you can mount on walls in game.

If 

(you exported to your mod folder)
then
(After export):
Activate the mod : Portrait\_MyColonist 

Finally: Enjoy and have fun.


Post Scriptum.

Thank you for feedback and support! Any and all is welcome. Tag me on discord or on 

