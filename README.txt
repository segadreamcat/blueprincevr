# Blue Prince VR 0.1.96

ok here's the big update. if you played the first October 10 version, there is a lot more you can actually reach out and use now. grab the backpack, pull out your tools, twist a door knob, dig something up, take a picture and write on it. less pointing at stuff and pressing a button for everything.

This covers the changes since the [first public build](https://github.com/segadreamcat/blueprincevr/releases/tag/windows-2026-10-10), not just the last couple fixes. It is still an experimental mod, so please tell me when something feels weird.

**Windows PC VR. You need your own copy of Blue Prince, SteamVR and a headset connected to your PC. This is not a standalone Quest game.**

## Which Download?

Get **BluePrinceVR-Tester-0.1.96-Full-Package.zip** from Releases. It has all three:

- **BluePrinceVR-Tester-0.1.96-Setup.exe:** the easy install, with an optional developer-mode checkbox and a restore option.
- **BluePrinceVR-Manual-0.1.96.zip:** the do-it-yourself install. Same runtime and assets as the EXE.
- **BluePrinceVR-Source-0.1.96.zip:** source for the core mods, backpack/interactions, native helper, installer, scripts and tests. You don't need this to play.

Install with the EXE OR the manual version, not both. GitHub's automatic "Source code" download is not the playable mod package.

## Installing / Updating

1. Close Blue Prince. Back up your save before testing a mod update.
2. Extract the full package and run the Setup EXE.
3. Choose the game folder containing `BLUE PRINCE.exe`. Steam > Properties > Installed Files > Browse gets you there.
4. Leave developer mode off for normal play. Turn it on only if you want the room/item pickers for testing.
5. Install, connect your headset, start SteamVR, then launch Blue Prince normally from Steam.

MelonLoader 0.7.3 is included. First launch can take a few minutes to prepare the game's bindings. Missing Microsoft runtimes may need an internet download. Setup is unsigned; check the download source and the included SHA256 checksums before deciding to run it.

The installer backs up replaced mod files, preserves existing calibration and personal notes/photos, and merges missing input defaults. It restores the normal intro setting. It doesn't edit your native save. Keep the installer backup folder if you want to use Restore previous files later.

**Manual install:** extract the Manual ZIP, then copy the contents of `GameFiles` into the game folder. Don't copy a folder called GameFiles into the game. Fresh installs also need the seed files from `Defaults-Only`; existing users should copy only missing seed files and keep their own preferences/calibration. `Optional-Developer` stays separate. See `FIRST-RUN.txt` for runtime links and the longer instructions.

## The Backpack And Blueprint

Reach behind your **right** side and grip for the bag. Reach behind your **left** side and grip for the rolled blueprint. Either hand can reach either location.

The bag has your camera, photos, notebook, recorder and blueprint slots. Owned tools and keys hang on it, with straps and stronger movement when you shake the bag. The bag is darker green to fit the tools. Camera and recorder slot images stay easy to recognize; the hanging equipment is 3D.

The blueprint rolls open like an old piece of paper instead of just floating a screen in front of you. **One X press flips it all the way over.** The back has the normal native menu, including the call-it-a-day options. A/stick controls work on the ready page; nothing ends your day automatically. Bring it back behind your left side to roll it up and store it. B can also roll/unroll it manually.

There are fixes for the wrong side facing you, the stock map flashing in front, the half-finished flip and getting stuck on the Mt. Holly screen. The normal map also comes up automatically while drafting rooms.

## Doors, Tools And Room Stuff

- **Doors:** reach with the right hand, grip a knob and twist, or grip a lever handle and push down. Different grips for knobs and handles. The hand stays attached through room selection and the door opening follow-through.
- **Digging:** touch the dirt mound with the shovel blade. One dig gets the native reward, with more dirt flying up. The whole mound is easier to hit, and the shovel stays in hand while the reward appears. Longer shovel, right hand on the top grip and optional left-hand support on the shaft.
- **Upgraded digging tools:** Power Shovel and Jack Hammer use the same contact loop. Jack Hammer is bigger and replaces the shovel on the bag instead of showing both. It uses the game's upgraded digging rewards. It does NOT break walls.
- **Hammers:** swing the head into supported chests/trunks, not just one tiny pixel on the lock. There is impact rumble and contact resistance, without the heavy weapon lag experiment. The grip is adjusted so the hand isn't buried in the handle.
- **Power Hammer:** also hits the game's eligible weak walls/planks/ice and cracked structures. Wider and higher strike areas, with cel-style dust covering the reveal. Ordinary hammers don't get free Power Hammer abilities.
- **Entrance Hall vases:** supported breakable vases can be hit across more of the body and throw a little debris. This doesn't make every decorative vase breakable or reset rewards.
- **Chest keys:** with keys available and an empty tool hand, a pinched key appears near the chest. Poke its tip at the front latch, then confirm the normal key-use prompt. The game still spends your keys; no free unlock.
- **Parlor boxes:** approach with the Wind-up Key and poke the box/key area. The hand pinches the back of the key. The white-box interaction and key area have been adjusted; an unused key can hang on the keychain.
- **Safes and computers:** fingertip number/keyboard presses, including safe Enter/Clear and terminal Enter. There are fixes for keys not typing and getting stuck at a computer.
- **Pump / Boiler / Utility rooms:** fingertip buttons, color buttons, pinchable small switches, sliders, pump-handle movement and grip/twist controls for supported boiler wheels and handles. The garage button is touchable too. Legacy Pump/Boiler/Utility controls can still be restored independently in the backpack config.
- **More grips:** wall and Lab levers, matching Orchard gas valves, secret-passage book spines, Room 8 statues and the large Observatory telescope's controls. The West Gate also has a front-side reach-through guard so you can't just put your hand through the fence to bypass its intended route.
- **Watering can:** auto-equips when owned in supported green rooms, including the Aquarium check. Grip placement uses the rear handle. Tilt to pour from the spout, with cel-styled water and little pouring rumbles. It uses the game's normal watering action, not a separate loot system, and shouldn't appear just because you approached the arcade.
- **Handheld telescope:** auto-equips in the Observatory when owned. Bring the eyepiece to your eye and hold alignment for four seconds. Pulling away resets the timer. Stars sit inside the small eyepiece instead of a huge floating picture, with close-eye clearance and hand-grip fixes.
- **Other gear:** owned Morning Star uses regular hammer interactions; Battle Axe, repellent, broken lever, battery, compass, detector and keys have bag attachments. The battery has green housing/silver ends, and the Emerald Bracelet goes on the right wrist.

Native tool models are borrowed visually from each player's installed game when available. No extracted Blue Prince meshes, textures or game binaries are in the download. The mod's existing model/texture bundles and fallbacks are included too, so missing native discovery doesn't just leave an empty hand.

## Camera, Photos And Notebook

The camera is a physical Polaroid now. Use the holding-hand trigger, get the flash/shutter sound, watch the print come out and shake it to develop. You can take multiple pictures without putting the camera away, walk while holding it and look through the improved see-through viewfinder. Releasing grip doesn't leave the camera floating.

Put the camera, recorder or photo behind your back to store it. Photos and camera can be stored separately. There are new tool/bag draw and stow sounds, a zipper sound for those gadgets, and little storage rumbles.

Open Photos from the bag for a larger, easier-to-read print. The borders are solid white again and capture quality is higher. **X switches between pencil and recorder** in Photos and Notebook. In recorder mode it's in the **left hand**: hold **left trigger** to talk, release to transcribe. Your words go on the photo's bottom white area, not off beside it. Drawing still works, and notebook paper is brighter.

Books/documents have **Press X to use camera**. X enters photo mode, **right trigger** takes a screenshot-style Polaroid with flash and shutter sound, and X returns to reading. You don't have to close the book to photograph it. With the Magnifying Glass owned, left stick moves it automatically while reading; right trigger still handles pages outside photo mode.

Voice transcription stays local/offline after setup. Use the headset microphone as the Windows input and enable microphone forwarding in your PC VR connection app if needed.

## Saved Copies Outside VR

Look inside the Blue Prince game folder for **VR Photos** and **VR Journal**. They have their own Polaroid and yellow-journal folder icons.

Photos include the original-quality PNG plus an HTML print with your words/drawings, TXT transcript and editable note data. The journal has `Journal.html`, plain-text notes and the notebook data. Existing saves get copied too; new saves update the copies in the background. Nothing is uploaded and the originals remain authoritative. Don't overwrite the generated copies under the same names if you want to keep separate edits.

## Buttons You Actually Need

Default right-handed setup. Menu/tool roles can change with handedness; X/Y shortcuts, editor voice and physical B keep the sides noted here.

| Action | Control |
| --- | --- |
| Move / slow walk | Left stick / click left stick |
| Turn / crouch | Right stick / click right stick |
| Click or ordinary interact / hold | Trigger / grip |
| Bag / rolled blueprint | Grip behind right side / left side |
| Blueprint | X flips; left-back return stores; B rolls/unrolls |
| Door | Right grip, then twist knob or push handle down |
| Native E / use-item prompt | Physical Y |
| Normal room X action | Physical X |
| Confirm / back | A / tap B |
| Pause / day-ending menu | Hold physical right B for one second in normal play |
| Camera shutter | Holding-hand trigger |
| Photos / Notebook | X pencil/voice; hold left trigger for voice; Y closes |
| Reading photo | X enters/exits; physical right trigger shoots |
| Magnifier while reading | Left stick, automatically when owned |
| VR settings | Select VR OPTIONS at title, or hold both stick clicks in play |

B still backs out immediately in menus/documents/terminals. X has different jobs depending on whether you're reading, editing, using the camera or holding the blueprint. `CONTROLS.txt` has the longer list, including the cart and other room interactions.

## Performance And Fixes

There is less repeated CPU searching for hovered objects and native menu screens, smaller background model-discovery chunks and save-triggered photo/journal exports. Idle physical controls return sooner instead of doing all their checks constantly. The developer grip-screenshot shortcuts are off by default in a fresh install.

There are **Performance / Balanced / Quality** choices in VR options. They adjust eye resolution and fixed foveation, not the headset refresh rate or Spacewarp. Restart after changing resolution. Hardware foveation needs compatible NVIDIA D3D11 support; this is not eye tracking. Opening the settings doesn't silently apply a preset.

The earlier experimental render-thread submission and closed-panel budgeting paths are disabled/ignored in this version after headset regressions. The working synchronized submission and hand rendering are retained. No Windows power tweaks or shorter main draw distance were added for this release.

Also included: the railroad cart release fix, safehouse boat hover/click fix, casino roulette panel centering, shop names placed on their wooden boards, safer keyboard/input startup bindings and the selectable main-menu VR OPTIONS row.

**I'm not promising native 90 FPS on every PC.** These changes passed CPU tests, but that isn't the same as testing every room in a headset. The newest cart/boat paths still need player feedback. If something feels off, tell me the room, item, headset, mod version and what you were doing. A screenshot or `MelonLoader/Latest.log` helps; check logs for personal info before sharing.

## Developer Mode, Source And Credits

Developer mode is optional and off by default in Setup. F opens the room picker and I the item picker on the PC keyboard. The bars in front of your face are hidden by default, so you can film without them. Use a separate save because the pickers can affect progression. Leaving the checkbox off doesn't remove a developer mod you installed earlier.

Source is in the included Source ZIP. Read its build guide: you need your own game references, SDKs and the matching toolchain. Generated game bindings and original game files are deliberately excluded. The pinned core source snapshot has its existing source-to-binary limitations documented; the current add-on and installer source are included alongside it.

Keep `THIRD-PARTY-NOTICES.md` and `MODEL-SOURCES.md` with the package. They retain component credits and the origins of bundled models/audio, including any outstanding attribution notes. This is an unofficial mod, not endorsed by the game's developers or publisher. Blue Prince itself is not included.

## one little secret at the bottom

The Commissary arcade can do a little more, but only if you set it up yourself. Close the game and create this folder inside your Blue Prince game folder:

```text
UserData\BluePrinceVR-Arcade\
```

Put exactly **one uncompressed `.nes` ROM you legally own and are authorized to use** there. It also needs a separately obtained, trusted Windows x64 Nestopia libretro core named `nestopia_libretro.dll` in that folder. Get the core from the official RetroArch core downloader or [the official project](https://github.com/libretro/nestopia). There are **no ROMs or emulator core in this download**.

Restart. Grip the cabinet's actual left stick and move your hand; use your other fingertip on its buttons. Middle buttons are **B then A**, left to right. Start is upper left, Select below. The hand shifts with the stick direction, and the touch targets match the existing cabinet controls. Release the stick to walk away. Audio uses your Windows default output, so point that at your VR audio.

No usable core/ROM setup? The normal arcade is left alone. `NESArcade = false` in `UserData\BluePrinceVR-Backpack.cfg` also disables it.
