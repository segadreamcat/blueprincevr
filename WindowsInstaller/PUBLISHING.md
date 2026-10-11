# Upload Guide

Only upload the finished files from this `release` folder. Do not upload the
original Ready ZIP or the full local build/work folder: the original input
contains generated game-specific loader files that this release excludes.
Nothing has been uploaded or published automatically.

## GitHub

Create/open your repository, choose **Releases**, then **Draft a new release**.
Use a version tag such as `windows-2026-10-10`. Attach the Windows EXE, the
Manual ZIP and `SHA256SUMS.txt` as release assets. Use `RELEASE-NOTES.md` as
the starting description and review it before publishing.

Do not commit the large downloads as ordinary repository files. GitHub
supports downloadable binaries as release assets; each must be under 2 GiB.
Both downloads in this package are below that limit.
https://docs.github.com/en/repositories/releasing-projects-on-github/about-releases

## Nexus Mods

Use `BluePrinceVR-Windows-Manual-2026-10-10.zip` as the main **manual download**.
Do not present it as a tested Vortex/mod-manager installer. It contains loose
files, not a self-extracting installer or another mod archive inside a ZIP.
Declare Windows PC, Blue Prince, SteamVR and the Microsoft runtime requirements.
Copy the relevant release notes and manual installation directions into the
description. Review the site's current game/category options yourself.

The custom EXE may require moderation: Nexus identifies self-extracting files
as potentially risky and restricts network-connected executables except for
essential functionality. The guided installer only downloads required
Microsoft runtimes when missing, but this is not a promise of Nexus approval.
If you also want to host it there, disclose the behavior and contact staff;
do not rename it or hide it inside another archive to evade scanning.
https://help.nexusmods.com/article/117-why-has-my-mod-been-quarantined
https://help.nexusmods.com/article/28-file-submission-guidelines

## Final Checks

Keep the third-party licenses and credits. Confirm you may distribute the
VR components and artwork; an assembled ZIP does not establish permission.
Review the site's required AI-content disclosure/tags for AI-assisted code
and any applicable assets. State the experimental status and known issues.
https://help.nexusmods.com/article/28-file-submission-guidelines

The EXE is unsigned and is not claimed to be certified or pre-approved by
Microsoft, GitHub, Nexus or the game publisher. Windows/antivirus warnings
remain possible. Review the local verification report, test a fresh game
installation and test actual VR gameplay before announcing a stable release.
Do not tell users to disable antivirus protection.
