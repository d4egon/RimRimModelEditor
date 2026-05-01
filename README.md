# Rim Rim Model Editor

A powerful in-game model/texture editor for modding—live texture editing within RimWorld with full XML export capabilities.

## Overview

Ever wondered how an item would look placed differently, or what motes or flecks look like with different settings? Rim Rim Model Editor lets you edit any texture live, in-game, and export your settings as XML. 

**Status**: This is an ALPHA release. Some features work, some don't. All feedback is welcome!

A tribute to the amazing RimWorld workshop and community. We've had so much fun over the years.

## Quick Links
- [Full Feature List](#current-features)
- [Getting Started](#getting-started)
- [Keyboard Shortcuts](#keyboard-shortcuts)
- [Exporting Your Work](#exporting-guide)

---

## Current Features

### Studio & Scene Management
- Multi-scene support — open multiple studio projects simultaneously as named tabs
- Rename scenes with custom names (falls back to first def name or "Empty Scene")
- Close individual scenes; always keeps at least one open
- Undo / Redo with configurable stack depth (tied to settings slider)
- Named version history (History tab) with restore points
- Deep-clone scene state for safe undo snapshots

### Def Browser (Left Panel)
- Browse ThingDefs, FleckDefs, EffecterDefs, PawnKindDefs
- Search/filter by defName
- ThingDefs with an active layer → assign texture to that layer directly
- FleckDefs, EffecterDefs, PawnKindDefs → always open as their own scene object in a new tab
- Animated fleck path resolution (tries `_0`, `_1`, `_a` frame suffixes)

### Preview Panel
- Real-time composited layer rendering
- Zoom (scroll wheel) and pan (middle mouse drag)
- Directional preview — rotate between South / East / West / North
- West direction auto-mirrors the East texture when no explicit `_west` variant exists
- Animation frame scrubbing (horizontal UV strip support)
- Checkerboard transparency background
- Custom background — solid colour presets (Black, Dark Grey, Mid Grey, White, Sky Blue, Warm Parchment, Forest Green, Deep Space) or load from RimWorld content path or external file
- Background opacity slider
- Collision overlay
- Placement overlay

### Layer System
- Add, delete, move up/down layers
- Per-layer: position (X/Y), scale (X/Y with linked-axis toggle), rotation, opacity, colour tint (RGB sliders)
- Layer visibility toggle
- Load custom texture from disk (PNG/JPG)
- Directional texture resolution (`_south`, `_east`, `_north`, `_west`)
- Body-type variant chaining for apparel (`Duster_Male_south`, `Duster_Fat_south`, etc.)
- Draw order by priority (Body → Apparel → Head → Beard → Hair → HeadAttachment → Hat → Weapon)

### Pawn Compositing
- Automatic body + head + hair + apparel + weapon layer stack from PawnKindDef
- Runtime texture path discovery via PawnTextureScanner (handles RimWorld 1.4 / 1.5 / 1.6 path differences)
- 1.6 head subfolder support (`Heads/Male/`, `Heads/Female/`, `HeavyJaw` skull type, gender-neutral heads)
- Skin tone picker (Light / Medium / Brown / Dark)
- Body type picker (Thin / Fat / Hulk / Male / Female)
- Head type picker (all discovered paths + manual entry fallback)
- Hair colour tint picker
- Apparel body-type variant picker
- Quick-add Beard layer (auto-positioned at head, priority 3)
- Quick-add Head Attachment layer (eye patch, horns, scars — priority 5)

### Inspector Panel
- Per-object: Draw Size, Draw Offset, Root Rotation
- Per-layer: Position, Scale (with linked axes), Rotation, Opacity, RGB colour channels
- Double-click any label to reset to neutral value
- Numeric input field next to every slider for exact values
- ThingDef-specific: Muzzle Flash Scale, Equipped Angle Offset
- FleckDef-specific: Growth Rate

### XML Export
- ThingDef XML generation (graphicData, graphicClass, drawSize, drawOffset, colour tint, weapon angle)
- FleckDef XML (fade in/solid/fade out timing, growth rate)
- EffecterDef XML
- PawnKindDef XML (body + head graphicData in lifeStages)
- All-layers export — every layer as its own ThingDef block with full position/scale/colour annotations, sorted by priority
- Live XML preview tab
- Batch export

### Screenshot & Portrait Export
- Full-window screenshot → `Desktop/RimRimScreenshots/` (timing-correct, no white box)
- Preview panel snapshot — captures only the preview area
- Export dialog with three modes:
  - **Entire Scene** — snapshot → wall-mounted portrait painting mod
  - **Current Layer** — active layer texture → portrait painting mod
  - **Scene as XML** — all layers as annotated ThingDef XML file
- Single shared portrait mod — all portraits accumulate in one mod folder
- Auto-numbered defNames when a portrait already exists (`Portrait_Bob` → `Portrait_Bob_2`)
- `About.xml` written once, never overwritten (preserves user edits)
- Correct `BuildingOnTop` altitude layer, `PlaceWorker_WallAttachment`, Beauty 15, Furniture architect tab

### Toolbar
- Category selector (switches active def type)
- Undo / Redo buttons
- Save Preset
- Export XML
- Background picker (colour presets + RimWorld path + file + opacity + clear)
- Screenshot
- Snapshot (opens export dialog)
- Hot-Reload (force-reloads selected def textures without restarting)
- Share

### Stability & Bug Fixes
- Null-safe layer names throughout (`def.label ?? def.defName ?? "Base"`)
- BeginScrollView/EndScrollView always balanced (null guards prevent mid-draw exceptions)
- `ThingDef.graphic` never accessed at runtime (uses `graphicData.texPath` only)
- FleckDef/EffecterDef always routed to own scene, never smashed onto pawn layers
- Animated fleck textures handled gracefully (no crash on folder paths or null graphicData)

### Tooltips
- Every toolbar button (with keyboard shortcuts where applicable)
- Every layer panel button and pawn picker row
- Every layer row (shows full texture path, priority, opacity, position, scale on hover)
- Every inspector slider (shows range, current value, double-click hint)
- Vector2 link button
- Load Custom Texture button

---

## Getting Started

1. **Launch the editor** and select a category (ThingDefs, FleckDefs, EffecterDefs, or PawnKindDefs)
2. **Browse and select** a def from the left panel
3. **Edit layers** — add, remove, reorder, and customize layers in the preview
4. **Adjust properties** — use the inspector panel to fine-tune position, scale, rotation, and colours
5. **Preview in real-time** — see your changes immediately with zoom and rotation

---

## Keyboard Shortcuts

| Action                          | Shortcut              |
|---------------------------------|-----------------------|
| Undo / Redo                     | Ctrl+Z / Ctrl+Y       |
| Save Preset                     | Ctrl+S                |
| Export XML                      | Ctrl+E                |
| Screenshot                      | Ctrl+F12              |
| Hot-Reload                      | Ctrl+R                |
| Add Layer                        | Ctrl+L                |
| Delete Layer                     | Ctrl+Delete           |
| Zoom In / Out / Reset View      | Scroll / Middle-Drag  |
| Toggle Collision / Placement    | (Toggle buttons)      |
| Next / Previous Animation Frame | (Arrow keys)          |

---

## Exporting Guide

### Best Practice: Export to Your Mod Folder

When exporting snapshots, export directly to your mod folder. The editor recognizes previous snapshots and creates new ones, allowing you to build a growing repository of custom snapshots.

### Step-by-Step Export

1. **Select export mode**:
   - **Entire Scene** — full snapshot for wall-mounted portrait mod
   - **Current Layer** — individual layer texture
   - **Scene as XML** — all layers as annotated ThingDef XML

2. **Choose destination**: Point to your mod folder

3. **Activate the mod**: In RimWorld, activate `Portrait_MyColonist` (or your chosen name)

4. **Enjoy!** Your custom portrait is now part of your game

**Auto-numbering**: If a portrait already exists, the editor automatically numbers it (`Portrait_Bob_2`, etc.)

---

## Feedback & Support

**Status**: This is an ALPHA release. Some features are still being developed.

Any and all feedback is welcome! This editor is a tribute to the amazing RimWorld community.

- Found a bug? Have a suggestion? Open an issue here on GitHub
- Connect on Discord or the community page
- Check the Steam Workshop page for additional resources

Thank you for your support! 🎉