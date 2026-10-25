# Full Release Verification - October 25, 2026

All 69 embedded payload files verified by size and SHA256. No ROMs, emulator core, saves, personal captures or generated game bindings.
Every current add-on runtime file matches the verified 0.1.96 ZIP, plus the optional inventory bundle: 13 bundles, recorder art and all four isolated arcade-worker files.
Native x64 helper matches the CPU-tested 0.1.96 input. 6,045 native mock assertions passed. The render-thread submission experiment is retired/ignored by this runtime; it retains the original synchronized submission. No D3D device or headset was initialized for these checks.
All 18 embedded new assets match their approved inputs: Morning Star/Battle Axe/Repellent meshes and textures, all ten tool/bag/gadget sounds, and both six-resolution custom folder icons.
89 temporary-folder installer checks passed: embedded payload, developer off/on, exact core/asset contracts, seed preservation, preferences, interruption/rollback, restore guards, safe paths and official loader hashes.
All 242 manual ZIP files verified. Runtime payload equals the EXE exactly; official loader hashes match. Generic UserData defaults and optional developer DLL are kept separate.
All 510 full-source files verified: core mods, current add-on/native/arcade/model code, source assets, installer and tests. No game DLLs, interop caches, private data, ROMs or emulator core. Original core source-to-binary limitations remain documented.
All 862 sampled live mod/assets/config files unchanged. No live installation, game/Unity/GPU preview, microphone or Microsoft-runtime setup was run.
Bundled 0.1.96 interaction build passed 660,228 CPU-only assertions, including actual managed Harmony hooks, blueprint flip/storage, native boat routes, cart release, startup binding preservation and previous interactions. Newest boat/cart headset behavior remains unverified. No GPU/compositor run, clean-PC first launch or native 90 FPS claim is made.
All current model/audio assets are retained with MODEL-SOURCES.md and third-party notices. Native game tool meshes are borrowed at runtime, never extracted into this release. No ROMs or emulator core redistributed. Existing attribution/provenance limitations remain documented.
Installer is unsigned. Verify its checksum and obtain it only from a source you trust.
