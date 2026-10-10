# Blue Prince VR

An unofficial Windows PC VR mod for Blue Prince, with motion controls, interactive hands, journal/camera tools and offline English voice notes.

**This is the first public build, released October 10, 2026. It is early and experimental. Expect bugs and performance differences between PCs and rooms.**

## Download

[Get the Windows build from Releases](https://github.com/segadreamcat/blueprincevr/releases)

- **Windows Setup EXE:** recommended guided installation, with backups and restore.
- **Manual ZIP:** an alternative for manual installation. Follow the README inside it.
- **SHA256SUMS.txt:** checksums for both downloads.

Download the files attached to the release, not GitHub's automatically generated source-code ZIP.

## Requirements

- Your own installed Windows PC copy of Blue Prince.
- A VR headset connected to your PC, with motion controllers.
- SteamVR.
- Internet for the first launch while MelonLoader prepares the game assemblies, and for any missing Microsoft runtimes.

MelonLoader 0.7.3 is bundled. You do not need to install it separately. This is a PC VR mod, not a standalone Quest/Android version.

## Installation

1. Close Blue Prince.
2. Download and run `BluePrinceVR-Windows-Setup-2026-10-10.exe` from Releases.
3. Select the game folder containing `BLUE PRINCE.exe`, `GameAssembly.dll` and `BLUE PRINCE_Data`.
4. Install the mod. If required, the installer downloads and verifies Microsoft's .NET 6 Desktop and Visual C++ x64 runtimes before running their installers.
5. Connect your headset to your PC and start SteamVR.
6. Launch Blue Prince normally through Steam. The first launch may take several minutes.

The installer is unsigned. If Windows or your antivirus blocks it, review the warning and verify the download source before deciding whether to run it.

For a manual installation, use the Manual ZIP's included README. It includes root DLLs as well as folders, so copying only `Mods` is not enough.

## Controls

**You can return to this section at any time. After installation, the controls guide is also in `BluePrinceVR-Installer/CONTROLS.txt` inside your Blue Prince game folder.**

| Action | Control |
| --- | --- |
| Move | Left stick |
| Turn | Right stick |
| Look | Headset |
| Walk slowly | Click left stick |
| Crouch | Click right stick |
| Interact | Trigger |
| Pick up / hold | Grip |
| Use held item | Trigger + grip |
| Journal / camera / notebook | Tap X |
| Voice memo | Hold X |
| Floor plan | Tap Y |
| Pause | Hold Y |
| Confirm | A |
| Back | B |
| VR options during play | Hold both stick clicks |
| VR options at the title screen | X |

Trigger and grip replace the PC's **E key** interactions: use trigger to interact, grip to pick up or hold an item, and trigger + grip to use a held item.

Select smooth or snap turning in VR options. X and Y stay on the left controller when changing your dominant hand. SteamVR bindings may differ.

### Computers

Keyboard calibration is included with the mod, so you should not need to recalibrate before playing.

Touch terminal keys with your fingertips. A/B and the sticks also operate Enter, Escape and the arrow menu keys.

**If you have keyboard issues**, such as touches not lining up with the keys, try recalibrating at a computer:

1. Hold both grips for three seconds.
2. Touch Q with your right index fingertip, then press A.
3. Touch P with your right index fingertip, then press A.
4. Touch Z with your right index fingertip, then press A.

Calibration is shared between computers, so you do not need to repeat this at every terminal. The Security terminal still needs gameplay confirmation in this build.

### Other Interactions

- **Forklift / pallet jack:** hold trigger to keep hold; grips control lift.
- **Magnifier:** hold grip over a document; the left stick moves the glass.
- **Voice notes:** select your headset microphone as the Windows recording input and enable microphone forwarding in your PC connection app. English transcription runs locally, without an account or API key.

## Updating and Restore

Close the game before updating. The guided installer backs up the files it replaces and preserves existing comfort, keyboard calibration and recorder settings. The intro is enabled with `SkipIntro = false`.

Other mods are not automatically removed. If you previously used a filming or developer setup, move its extra developer, cheat or Trailer DLLs out of `Mods` before testing this public build.

To reverse the guided installation, run the same installer again and select **Restore previous files**. Keep the `BluePrinceVR-Backups` folder. Restore refuses to overwrite files you have edited since installation. Microsoft runtimes remain installed.

For manual updates or removal, follow the Manual ZIP's README. Do not delete the entire `UserData` folder: it can contain personal notes, photos and recordings.

## Known Issues and Reports

- This is an early public build. Bugs and untested interactions remain.
- Performance varies by PC and room. This release does not promise a particular frame rate or compatibility with every headset.
- The Security terminal still needs gameplay confirmation.
- **Avoid F6 unless troubleshooting.** It generates large diagnostic screenshots in `UserData`.

When reporting a problem, include your headset, the room, any held item, the input you used and what happened. If sharing `MelonLoader/Latest.log`, check it for personal information and local file paths first.

More setup information is installed in `BluePrinceVR-Installer/FIRST-RUN.txt`. Third-party notices are included with the download.

This is an unofficial community mod and is not affiliated with or endorsed by Blue Prince's developers or publisher. The original game is required and is not included.
