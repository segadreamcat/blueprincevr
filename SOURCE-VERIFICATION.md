# Source Verification

Checked October 11, 2026, on Windows with .NET SDK 6.0.428 and the user's existing game-generated MelonLoader references. Verification used a separate working copy; no rebuilt DLLs were installed into the live game.

## Passed

- Core BluePrinceVR 2.10.57 compiles: zero errors, 11 existing unused/unassigned-field warnings.
- ClaudeFixes c96 source compiles: zero errors, three existing unassigned-field warnings.
- Fountain, JournalArt and RecorderPolish compile: zero errors or warnings.
- Main logic tests pass, including notebook persistence, input gates, crouch, video state, handwriting, fingertip input, arm mesh validation/refinement, crop math and pointer behavior. The arm asset was read from the user's installed mod, not included in this snapshot.
- Tap/hold tests pass.
- Copied source files are hash-checked against their local originals. The four ClaudeFixes embedded UI images are hash-checked against the public manual ZIP.
- Prepared upload tree contains no compiled DLLs/EXEs, dependency archives, generated reference assemblies, build caches, original game assets, saves, recordings or logs.
- Text scan found no personal author filesystem paths or common API-token/private-key patterns. This is a limited scan, not an independent security audit.

## Limits

- Rebuilt DLL hashes differ from the release. No byte-for-byte source-to-release reproduction is claimed. Build paths, debug information, compiler/reference versions and build inputs can affect hashes; the differences have not been fully analyzed.
- The supplied ClaudeFixes source archive's UI images differ from the public release's images. This snapshot uses the public release's images instead; C# source and project files remain unchanged.
- Native helper, Unity shader bundle and Windows installer were not rebuilt from this source snapshot. Installer packaging still needs the inputs documented in `BUILDING.md`.
- No headset gameplay, frame-rate, antivirus or external security audit was performed for this source preparation.
