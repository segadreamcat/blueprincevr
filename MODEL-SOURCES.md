# Prototype Model Sources

## October 25 Full Package (0.1.96)

All existing mod models, fallback bundles, textures and sounds are retained.
Native tool visuals load from the player's own installed game at runtime;
no extracted Blue Prince mesh, texture or original game binary is distributed.
The 0.1.96 cart/boat/input/map/CPU updates add no imported model or sound.
The native render-thread submission experiment below is archived code,
not the active rendering path: 0.1.96 retains original synchronized submission.
The sections below preserve the actual historical supplied-asset origins and
attribution limitations rather than relabeling imported work as original.

## Render Queue Bridge (0.1.84)

`BPVRSubmit.dll` is original project C++ code compiled with the Windows SDK and static MSVC runtime. It does not bundle OpenVR headers, a new runtime, NVIDIA proprietary code, game assets or ROMs. It receives the working core's compositor entry points and uses Windows D3D11 COM resource interfaces. Source and CPU-only mock tests are in the add-on source archive under `native-submit/SubmitBridge.cpp`; build script is `BuildSubmitBridge.ps1`.

## Physical Chest Key (0.1.84)

The chest key borrows the native Key A pickup mesh/materials at runtime using the same bounded render-only discovery. No extracted game data is distributed. If it is not available, the mod builds a small low-poly brass key with a real bow opening, shaft and teeth using its existing cel surface shader. This fallback is mod-owned procedural geometry, not an imported game mesh or supplied model. The native Trunk Code key-use prompt, cost, spending and loot remain game-owned; the visual does not grant an inventory item.

## Native Tool Visuals (0.1.77-0.1.84)

The runtime borrows installed-game MeshFilter meshes and shared materials for owned world tools and displays cosmetic copies on the backpack/in hand. No extracted Blue Prince tool meshes, textures, prefabs, scripts, bindings or game binaries are included in this package. Telescope optical clipping creates only a small runtime-owned eyepiece opening; the mod's static circular star image remains recessed inside it. Shovel/hammer contact anchors and mounting hardware remain mod-owned. The camera, recorder, notebooks/paper and unavailable native models use the existing custom assets below, whose original rights/credits still apply. Disabling UseNativeToolModels restores the fallback artwork. Runtime native borrowing is not a claim of publisher permission or a grant to redistribute native game assets.

## Gadget Stow Audio (0.1.74)

User-supplied `365376__rdpipo7777__cremallera.wav` is embedded unchanged as `BPVRBackpack.Audio.gadget-stow.wav` for successful camera, recorder and photo back stows. Mono PCM16, 48 kHz, approximately 0.271 seconds. Source SHA256: `5A67FF959EA90C039CF02C515D31A64DA7576A38A06AFD7FBAFCF5D02967D9A7`. Attribution and redistribution permission must be confirmed with the other supplied audio before public upload.

## Morning Star, Battle Axe And Repellent (0.1.63)

User-supplied archives: `simple-morning-star.zip` (nested RAR / `MorningStar.obj` and `Diffuse.jpeg`), `double-sided-battle-axe.zip` (nested ZIP / `model.dae` and `02___Default_albedo.jpeg`), and `dichlorvos-spray.zip` (`Dichlorvos.fbx` and `Banka_Base_color.jpg`). Only model/color data is read. No archive code is executed.

The single-worker .NET importer reads OBJ faces, structured Collada XML and binary FBX arrays/model transforms. Spray includes its main can and cap; the offset `Dichlorvos.002` duplicate is omitted. Prepared models are bounded binary vertex/UV/triangle data embedded in the add-on, with 1024x1024 JPEG albedos and the existing stepped cel tool shader. Approximate heights: Morning Star 80cm, Battle Axe 95cm, Repellent 23cm. Attribution and redistribution licenses were not supplied and must be confirmed before public distribution. Original archives are retained in Downloads.

## Tool Audio

User-supplied audio: `30248__streety__sword7.flac` for shovel draw, `417035__funwithsound__quick-sword-draw.mp3` for hammer draw, `776203__hushless__sword-sheath(1).wav` for shovel stow, and `180838__32cheeseman32__swordsheath03.wav` for hammer stow. Converted to mono PCM16 WAV at their original sample rates (44.1 kHz for the first three, 96 kHz for hammer stow), preserving duration and pitch. The converted clips are embedded in the private add-on DLL and included in the source archive.

Backpack draw uses the supplied `320144__owlstorm__blanket-movement-6.wav`; backpack stow uses `529563__drmrsir__roll.wav`. These are also converted to mono PCM16 at their original sample rates and embedded in the DLL.

Watering-can draw and behind-back stow both use `516257__wyronroberth__grab-_-can-watering.mp3`, converted to mono PCM16 at its original sample rate and embedded once as `watering-can-handle.wav`. Its decoded peaks exceed full scale, so uniform attenuation provides 0.98 peak headroom without clipping, changing duration or changing pitch.

Telescope draw uses `863340__alexarje__telescope-stand-soundaction-361.wav`, trimmed to the audible movement (1.51-1.93 seconds of the original recording), with short boundary fades and peak headroom. `telescope-draw.wav` retains the original 48 kHz sample rate; `telescope-stow.wav` is an exact sample reversal of that trimmed PCM clip. Leading/trailing dead time is excluded from both.

## 3D Backpack Inventory (0.1.28)

`binder-notebook.zip` supplies `source/binder_notebook.fbx` and its three JPG textures. The notebook is imported into a cosmetic no-collider bundle and shaded with the existing private backpack cel surface. The recorder is self-authored procedural geometry based on the project's existing recorder design; no new recorder source model was supplied. The camera uses the already-built `vintage-camera.bundle`. These inventory meshes are display-only: native launch, drawing, voice-note and photo behavior remains in the existing runtime paths.

## Held Hammers (0.1.25)

`BuildHeldHammers.ps1` builds an isolated `held-hammers.bundle` from the existing private `side-tools.bundle` and `power-hammer.bundle`, omitting bag mounting straps. No older bundle or supplied source model is edited. The original model provenance and redistribution cautions still apply. Native chest FSM information is inspected locally to route an actual tracked impact through the installed game's existing hammer-click path; no exported game assets or game scripts are packaged. Unity previews use the installed hand GLB read-only; neither that GLB nor generated hand renders are redistributed.

## Combined Power Shovel (0.1.21)

`model-preview/Assets/Editor/BuildCrafted.cs` combines the previously supplied shovel and metal-detector geometry from `side-tools.bundle`: shovel blade/shaft/grip and bag-matched straps, complete detector coil and control-box components. Their existing known albedo inputs are reused; the shovel is darkened and the detector retains green paint. The wrapped cable and segmented brass coil detail are prototype additions. The supplied Power Shovel reference guides the arrangement but is not embedded as a texture. No native game mesh, texture or crafting logic is included. The original source-model provenance/redistribution caveats still apply; this remains a private prototype.

## Treasure Roll And Revised Mounts (0.1.19)

- `treasure-map-sea-of-thieves-inspired.zip`: nested `source/model.zip` / `model/model.dae` and outer `textures/lambert1_albedo.jpeg`. The supplied rolled parchment is used as the owned TREASURE MAP attachment, with bag-matched leather retaining belts. No Blue Prince map texture is imported into this model.
- The existing supplied watering-can mesh/albedo and carabiner are reused for a teal-blue cosmetic override, retaining the separate swing pivot. The old side-tool bundle is unchanged.
- Battery end triangles receive a silver material; housing, lockpick sleeve and mounting geometry are recolored/repositioned without editing supplied source meshes/textures.

The treasure model's author, license and original download page are unconfirmed, as with the supplied models below. Its title indicates inspiration from Sea of Thieves; that does not establish redistribution permission. Keep the prototype private until provenance, credits and permissions are checked.

## Supplied Extra Tools (0.1.17)

Six additional private prototype archives supplied by the user:

- `lockpick-set.zip`: nested `source/model.zip` / `model/model.dae`, plus `textures/lambert1_albedo.jpeg`. The ten supplied pick meshes are retained; their connected case/closed flap and buttons are replaced by a self-authored open leather sleeve.
- `vintage-magnifying-glass-lp.zip`: nested `source/model.zip` / `model/model.dae`, with `DefaultMaterial_albedo.jpeg` and `DefaultMaterial_opacity.jpeg`. The supplied opacity mask separates the lens into a tinted transparent material; no real zoom is added.
- `battery-pack-by-thisteam.zip`: nested Collada model and Worn/scratched metal, Light grey brushed metal and Blue Glow albedo textures. The housing uses the bag's estate-green cel palette; source files remain unchanged.
- `compass.zip`: nested Collada model and `compassMain_albedo.jpeg`. The separate glass shell is omitted to expose the supplied dial/needle.
- `carabiner.zip`: `source/simple carabiner var.fbx`, used as a brass bottom hanger for the compass.
- `keys.zip`: `source/keys.obj`, replaces only the car-key geometry on the existing owned-key pivot. Other special keys retain their existing models.

Leather belts, buckles, open sleeve, transparent lens shader and ink/cel materials are prototype additions. Only known model/color data is extracted. All attachments are cosmetic; inventory is read from the installed game and is never granted or changed by these models.

Authors/download pages/redistribution licenses have not been established from these archives. They are not claimed as original mod models. Keep this build private until provenance, required credits and redistribution permissions for each supplied model are confirmed. No public upload was performed as part of this update.

## Special Keys

The 0.1.20 extension adds modeled Key 8, Basement, Master, Aries, Prism, numbered cabinet (1/2/5), Diary and Lucky Rabbit's Foot attachments. The eight supplied images include a duplicate rabbit-foot image and an existing Sanctum reference. Master/cabinet designs are provisional; the leaf reference is used for Aries and the purple bow/cord reference for Diary. Each is newly constructed volumetric geometry, not a flat reference-image card. Supplied pictures are not copied into bundles or source ZIPs. Inventory names were checked read-only against local native pickup names; no exported game meshes/materials/textures were imported. Existing Sanctum geometry remains unchanged rather than duplicated. Derivative design permissions remain unconfirmed; keep this prototype private.

The 0.1.15 keychain models are newly constructed volumetric geometry in `model-preview/Assets/Editor/BuildKeychain.cs`, visually based on the five key reference pictures supplied by the user. Those pictures depict Blue Prince artwork and are not claimed as original mod designs or imported as textures. The numbered vault variants share a modeled silhouette with individually modeled 149/233/304/370 tags. The ring, link geometry, cel materials and cosmetic pendulum are prototype additions. This is a private test build; permission to distribute derivative key designs is not established by modeling new geometry.

Rebuild with `BuildKeys.ps1` using the same Unity version as `BuildTools.ps1`. It creates the self-contained `keychain.bundle` and verifies its volumetric meshes, materials and individual rendered visibility. Native inventory, game files, reference images, developer DLLs and saves are not packaged in the keychain bundle.

## Supplied Side Tools

Private prototype inputs supplied by the user on October 11, 2026:

- `shovel.zip`: `source/shovel.obj` and the supplied albedo texture.
- `sledge-hammer.zip`: the nested `source/model.zip`, containing `model/model.dae` and its supplied albedo texture.
- `metal-detector.zip`: `source/metal_detector.fbx` and `textures/diffuse.jpg`.
- `watering-can.zip`: `source/LP2.fbx` and `textures/LP2_Material_BaseColor.tga.png`.
- `refracting-telescope.zip`: nested `source/4012_10_V_Luneta.zip` / `4012_10_V_Luneta.obj`, with outer `textures/4012_Luneta_Diffuse.jpeg`.
- `adjustable-spannerwrench.zip`: nested `source/Adjustable_Spanner_01.zip` / `Adjustable_Spanner_01.obj`. No color texture is supplied; the prototype uses a solid cel-shaded steel material.

The model authors, download pages and redistribution licenses were not included in these archives. These models are not claimed as original mod artwork. Confirm their provenance, required credits and redistribution permissions before including them in any public GitHub or Nexus release.

Only the supplied model/color data is imported, not archive scripts or executables. The side straps, chain/hook and cel shader are prototype additions. Blue/teal paint is changed to estate green by a material parameter; original source textures remain unmodified. No Blue Prince model or texture is included in this side-tool bundle. Game inventory is read locally from the player's installation.
# Physical Polaroid (0.1.22)

`src/PolaroidModel.cs` is original procedural 3D geometry based on the blue/white folding-camera design of the already-installed `camera-choice-flat.png` illustration. No matching imported 3D camera file was found. The new camera, print stock, aperture and bag photo-stack icon are modeled in code; the original illustration is neither modified nor copied into this add-on. New self-authored surface/development shaders are in `polaroid.bundle`. Saved player screenshots are generated at runtime and never included in release archives.

## Broken Lever And Supplied Camera (0.1.23)

The lever is original procedural geometry modeled from the user's reference image. No game model, texture or reference bitmap is embedded. It uses the existing self-authored cel surface shader and matching bag leather/brass/thread colors in an isolated `broken-lever.bundle`.

`vintage-polaroid-camera.zip` supplies `source/model.zip` / `Modelv2.obj` and the three `Polaroid`, `Polaroid_CH` and `Polaroid_inside` albedo JPEGs. Only model/color data is imported; no archive scripts or executables are run. The supplied camera replaces the procedural visual through an optional `vintage-camera.bundle`; the old model is retained as a fallback. Attribution, author and redistribution license were not provided with the upload and must be confirmed before public redistribution.

Pinch previews read the already-installed `hands_fp_cel.glb` for private verification only. Neither that hand asset, generated hand previews nor the user's reference photograph is included in the source/runtime archives. The runtime adjusts the existing local rig without replacing core files or hand assets.

## Power Hammer And Camera Support Grip (0.1.24)

The Power Hammer combines only existing private prototype assets: the supplied sledge-hammer head, supplied battery geometry, self-authored lever/linkage and hammer mounting straps. Added brackets and the cosmetic composite layout are modeled in code from the user's reference, which is not embedded. It is self-contained in `power-hammer.bundle`; none of the older bundles are rebuilt. The original supplied-asset license/provenance cautions still apply.

`CameraGripSolver.cs` supplies bounded finger rotations and a wrist-relative side/bottom hold for the existing vintage camera. Private Unity checks read the already-installed hand pack without copying it into the project or either ZIP. `BuildCameraGrip.ps1` renders/verifies without rebuilding any bundle; `BuildPowerHammer.ps1` builds only the new crafted attachment.

