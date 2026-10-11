# Source Snapshot: October 11, 2026

Source for the Windows PC VR mod accompanying the October 10 first public build:

- BluePrinceVR Codex 2.10.57: C# project, helper classes and selected logic tests.
- BluePrinceVR.ClaudeFixes: source archive whose startup log identifies `c96-menunav`. The four UI images required by its project are supplied from the public manual ZIP and verified against that package's hashes; they are not the differing images found in the source archive.
- BluePrinceVR.Fountain, BluePrinceVR.JournalArt and BluePrinceVR.RecorderPolish: C# projects.
- Native CodexVrs helper, mod shaders, and offline memo capture/transcription wrappers.
- Windows installer source, built-in tests, and original packaging scripts.

The original code and project files are copied without rewriting them. `Build-Mods.ps1` is a new convenience script; `SOURCE-FILES.json` records the included files and SHA256 hashes. `docs/release-file-provenance.json` records the public release's component hashes, not source-to-binary reproducibility.

The locally retained build outputs for the core, Fountain, JournalArt and RecorderPolish match their October 10 public release DLL hashes. ClaudeFixes' source identifies the same c96 build label. This does not, by itself, prove every source file reconstructs the exact released binary. Compilation results and limits are recorded separately in `SOURCE-VERIFICATION.md`.

This snapshot does not contain the game's own source, original game binaries, extracted game assets, generated game/Unity interop assemblies, personal saves, notebook images, recordings, logs, private calibration or SDK/dependency binaries. The mod's existing source may refer to game types and asset names through the user's local installation.

Third-party software remains under its respective terms. See the included NVIDIA notice and `source/WindowsInstaller/THIRD-PARTY-NOTICES.md`. Publishing source does not automatically grant a new open-source license: no new blanket license has been selected for this snapshot. Any pre-existing rights and notices remain in place.

This source snapshot is separate from the existing EXE and manual ZIP. Preparing it does not change those release downloads or automatically publish to GitHub.
