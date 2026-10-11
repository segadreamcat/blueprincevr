# BluePrinceVR.ClaudeFixes

A companion mod for the Blue Prince VR mod by Codex. It does not replace that
mod and does not modify its source: it Harmony-patches it from the outside, so
a rebuild of the VR mod cannot quietly wipe these fixes. Every patch is
resolved by name at startup and skipped with a log line if its target has
moved, so a refactor upstream costs one feature rather than crashing the game.

Requires: Blue Prince (Steam), MelonLoader 0.7.3 open-beta, and the Codex
BluePrinceVR mod. Built against IL2CPP, net6.

## What it does

**Rendering**
- Room culling. The game's `Culler` component switches a room's renderers off
  based on the flat camera, which in VR faces wherever the stick last turned
  you, so a room seen through a doorway vanishes when you turn only your head.
  Rooms within a radius are never switched off. (`KeepNearRoomsDrawn`)

**Terminals**
- A full fingertip keyboard. Codex wires six keys to the terminals: Enter,
  Escape and four arrows. This adds A-Z, 0-9, Space, Backspace, Delete and
  Shift, pressed by touching the terminal's own keys. Calibration is three
  taps, once, ever: touch Q, P and Z and the rest of the layout follows by
  arithmetic. Every computer in the game uses the same keyboard, so one frame
  serves all of them. (`FullFingertipKeyboard`)
- A computer opened by using an item on it, the upgrade disc for instance,
  goes through the game's own inspection zoom and never tells the VR mod a
  terminal opened. This catches that case. (`CatchItemOpenedComputer`)

**Interaction**
- The document magnifier moves on grip plus the left stick, and the pointer
  snaps onto the glass before pressing, so the drag takes hold first time
  rather than turning a page. (`MagnifierGripGate`)
- The forklift and pallet jack stay held while the trigger is held.
  (`ForkliftHoldToDrag`, `HoldDragNames`)
- The voice recorder is posed in the off hand with a worked grip, an index
  press animation and a button click.

**Menus and title**
- A VR options page on the title screen, in the game's own font and layout,
  with a controls list.
- The headset's own recenter moves the mod's playspace origin with it, so the
  hardware recenter button works in game. (`FollowHeadsetRecenter`)
- Assorted menu, journal, draft, laundry, casino and dig fixes.

Everything is a MelonPreferences entry and everything can be switched off.

## Building

1. Put the reference assemblies in `refs/`. See `refs/PUT-REFERENCES-HERE.txt`.
   They are not included here because they are the game's and MelonLoader's.
2. `dotnet build -c Release`
3. Copy `bin/Release/net6.0/BluePrinceVR.ClaudeFixes.dll` into the game's
   `Mods` folder, alongside `BluePrinceVR.dll`.

## A note on the source

This is one long file. That is deliberate rather than neglected: it is a
companion mod whose whole job is to reach into another assembly by reflection,
and keeping the patch, its reason and its fallback next to each other has been
worth more than tidy file boundaries. The comments say why each patch exists
and what broke without it, which is the part that would be expensive to
rediscover.

## Credit

The VR mod this patches is Codex's work. This is a companion to it, written
with Claude.
