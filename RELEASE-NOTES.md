# Blue Prince VR 0.1.96 - October 25, 2026

ok this is the big update since the October 10 first public build. all the new interactions and assets are in here, plus the latest map, cart, boat and CPU fixes. not just a small patch you need five other downloads for.

## New Since The First Public Build

- Shoulder backpack inventory with camera, photos, notebook, recorder and blueprint slots. Darker green bag, ownership-based 3D tool/key attachments, fixed straps, stronger key/can/compass swing, and draw/stow sounds and haptics.
- Native installed-game tool visuals when available, in hand and on the bag. All existing mod/fallback models are retained. No extracted game models or original game binaries are distributed.
- Right-back bag and left-back rolled paper blueprint. Single-X complete front/back flip, native menu on the back, automatic left-back roll/store, optional B roll/unroll, corrected front orientation and recovery from failed native page transitions. Drafting opens its map automatically again.
- Physical right-hand door knob twists and lever-handle pushes, with separate grips and attached hand follow-through during drafting/opening. The old automatic door-pointing route is retired.
- One-contact whole-mound digging, larger dirt effect and persistent held shovel during native rewards. Longer shovel, top-handle grip and optional shaft support. Power Shovel/Jack Hammer use the same loop; larger Jack Hammer replaces shovel and keeps native upgraded loot, not wall breaking.
- Head-only hammer strikes, broader supported chest/vase contact, impact rumble and restrained solid resistance. No heavy weapon lag. Power Hammer eligible-wall strikes cover a wider/higher area with cel dust; Entrance Hall vases get short debris. Morning Star uses normal hammer interactions.
- Pinched physical chest key/latch poke, preserving native confirmation/cost. Parlor Wind-up Key contact, white-box fixes, back-of-key pinch and unused-key keychain display.
- Safe keypad touch including Enter/Clear; terminal fingertip typing and Enter recovery; touchable utility/color/garage buttons; pinch switches and sliders; grip/move pump controls; boiler handle/wheel/valve grips. Legacy room controls remain selectable for troubleshooting.
- Physical wall/Lab levers, Orchard/matching gas knobs, secret-passage book-spine pulls, Room 8 statue handling and large Observatory telescope control grips. West Gate intended-side interaction guard.
- Owned watering-can auto-equip in supported green rooms/Aquarium, rear-handle grip, spout-aligned cel water and pouring haptics through the native use action. Owned portable telescope auto-equip in Observatory, four-second eye hold, recessed circular stars and eye/hand alignment fixes.
- Emerald Bracelet on right wrist; supported owned Battle Axe, repellent, broken lever, battery, detector, compass and other key/tool mounts. Green battery/silver ends, darker matched mod tool palette.
- Physical Polaroid camera, flash/shutter, ejected print, shake development, repeated photos, locomotion while held and a clearer see-through viewfinder. Camera stays in hand until stored; print stores independently.
- Larger high-quality photos, opaque white borders and brighter notebook paper. X switches pencil/recorder, recorder stays left, hold/release left trigger for local voice transcription. Photo captions sit on its white bottom border; drawings remain editable.
- Photograph open documents/books: Press X to use camera, right-trigger screenshot-style Polaroid with flash/sound, X back to reading. Automatic left-stick magnifier while reading, with normal page controls outside photo mode.
- Local VR Photos/VR Journal libraries with custom folder icons, full-quality images, readable HTML prints/pages, drawings/transcripts and editable data copies. Save-triggered background updates preserve original UserData.
- Physical Y sends E at supported prompts, X stays X in ordinary play, tap B backs out and hold B opens pause/day-ending menu. Selectable title-screen VR OPTIONS, refreshed controls help and clean developer view for F/I pickers.
- Casino roulette panel centering and Commissary labels on their wooden boards. Optional arcade uses the actual cabinet controls, B/A order and hand motion; the normal cabinet is untouched without a usable user-supplied setup.

## Latest Fixes And Performance Work

- Railroad cart releases through its native cleanup route on trigger release/Back/tracking or focus loss, with delayed-input handling and regrab protection. Forklift behavior remains separate.
- Safehouse boat retains either hand's hover during native pointer entry/update and uses its existing available-dock travel event once per press. No bypassed unlock, forced state or teleport.
- Late startup check repairs only missing live input bindings, preserving custom bindings and opt-out. Added diagnostics for the intermittent wrong-stick report; its exact cause remains unconfirmed without the tester's failing log.
- Cached hover hierarchy lookups and relevant screen-object discovery replace repeated full-scene searches. Smaller paced native-model discovery, earlier idle exits, save-driven exports and bounded CPU profiling cut background work.
- Performance/Balanced/Quality choices adjust eye resolution/fixed foveation; resolution needs restart. Compatible NVIDIA D3D11 hardware is needed for hardware foveation. No automatic Spacewarp/refresh-rate changes or native 90 FPS promise.
- Retired the render-thread submission and closed-panel budgeting experiments after headset regressions. Existing synchronized submission, working hand rendering and normal main-camera far distance are retained.

## What's In The Download

Full Package ZIP: Setup EXE, complete Manual ZIP, complete Source ZIP, README, controls, first-run guide, these release notes, source/asset notices, verification and checksums. Optional developer mode is off by default. No ROMs, emulator core, personal data, game binaries or generated game bindings.

CPU checks passed for the included 0.1.96 binary; new packaging also runs installer preservation/restore and archive checks. The newest boat/cart paths still need player headset confirmation. This is an experimental release, not a claim every room or clean PC has been tested. Keep all credits/notices with redistribution.

The quiet arcade setup instructions are at the very bottom of the README.
