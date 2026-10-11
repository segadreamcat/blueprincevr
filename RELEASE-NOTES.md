# Blue Prince VR - Windows - October 10, 2026

Experimental Windows PC VR release based on the October 10 Ready package
with ClaudeFixes c96. Requires an installed copy of Blue Prince, SteamVR,
a connected PC VR headset and motion controllers.

## Downloads

- `BluePrinceVR-Windows-Setup-2026-10-10.exe`: guided installation, updates,
  per-file verification, backups and restore. Unsigned Windows x64 EXE.
- `BluePrinceVR-Windows-Manual-2026-10-10.zip`: plain manual-install download,
  with the same VR components and clean MelonLoader files. No installer EXE
  or nested archives inside. See the ZIP's root README before installing.
- `SHA256SUMS.txt`: hashes for the two downloadable files.

## Included

Codex VR, ClaudeFixes c96, Fountain, JournalArt, RecorderPolish, the hand and
journal assets, English offline voice notes, keyboard calibration and
MelonLoader 0.7.3. OpenVR and the required root loader DLL are supplied.
Missing Microsoft runtimes are downloaded only by the guided installer,
from pinned official URLs with checksums verified before execution.

The game intro is restored with `SkipIntro = false`. Guided updates keep
other existing VR values, add missing settings, repair empty controller
mappings and preserve existing keyboard/comfort/recorder configuration.
The release uses a generic hand-span default rather than the author's fit.

## Excluded

No developer-mod option. KeepDevObjects, DevHudSwitch, MoreResourcefulPrince,
Trailer, the old Contact mod and backup DLLs are not shipped. The public
download also excludes saved photos/notebooks, the scratch notebook image,
logs/debug dumps and pre-generated game/Unity assembly caches. The loader
generates its required game-specific files on the user's own installation.

## Before Playing

Start SteamVR, then launch the game normally from Steam. The first launch
can take several minutes and requires internet. No account or API key is
needed for local English voice transcription. This is not a standalone
Quest/Android or Linux package. Other existing mods are not auto-deleted.

## Known Limitations

This packaging pass does not establish VR gameplay compatibility or claim
a frame-rate improvement. Reprojection may be needed. The Security terminal
still needs gameplay confirmation. F6 writes large diagnostic screenshot
sets to UserData; avoid it unless troubleshooting. Read the included
controls and calibration instructions before reporting a terminal issue.
