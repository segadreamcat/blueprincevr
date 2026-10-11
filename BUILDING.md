# Building the Mod

This is the mod's source, not Blue Prince's source. You need your own installed Windows copy of the game. Game assemblies, game assets, generated interop assemblies, model weights and dependency binaries are not included.

## Managed Mod DLLs

1. Install the public Windows mod in your own game and launch once so MelonLoader generates `MelonLoader/Il2CppAssemblies`.
2. Install a .NET SDK capable of building `net6.0` projects and the .NET 6 runtime for running the tests.
3. Open PowerShell in this repository and run, adjusting the game path:

```powershell
.\Build-Mods.ps1 -GameDir 'C:\Program Files (x86)\Steam\steamapps\common\Blue Prince'
```

The five mod DLLs are built into separate folders under `artifacts`. Nothing is copied into your game. This builds code only; keep the public release's runtime assets and dependencies when testing a rebuilt DLL.

The script creates local reference DLLs in `source/BluePrinceVR.ClaudeFixes/refs`. These include assemblies from your game and must not be uploaded. The repository's `.gitignore` excludes them and other generated build files.

The Fountain, JournalArt and RecorderPolish projects reference the core DLL in your installed game's `Mods` folder. To develop against a changed core API, update that local reference deliberately before building those projects.

## Small Logic Tests

```powershell
dotnet build source/BluePrinceVR/tests/Tests.csproj -c Release
dotnet source/BluePrinceVR/tests/bin/Release/net6.0/Tests.dll 'C:\Program Files (x86)\Steam\steamapps\common\Blue Prince\Mods\BluePrinceVR-Codex\arms.bpvr'
dotnet build source/BluePrinceVR/tests/TapHoldTests.csproj -c Release
dotnet source/BluePrinceVR/tests/bin/Release/net6.0/TapHoldTests.dll
```

Adjust the arm-asset path to your own installed public mod. The asset is not included in this code snapshot. Invoke the test DLLs explicitly as above because these two projects share an output directory. These check selected logic without a headset. They are not full VR gameplay tests.

## Native Foveation Helper

`source/BluePrinceVR/native/CodexVrs.cpp` is the native helper source. Use an x64 Visual Studio developer command prompt, the Windows SDK and your own NVIDIA NVAPI SDK checkout. Its license notice is included. For example:

```bat
set "NVAPI_DIR=C:\path\to\nvapi"
cl /nologo /std:c++17 /EHsc /O2 /MT /LD /I"%NVAPI_DIR%" source\BluePrinceVR\native\CodexVrs.cpp /Fe:CodexVrs.dll /link "%NVAPI_DIR%\amd64\nvapi64.lib" d3d11.lib user32.lib
```

The original native SDK and static library are not vendored here. Native recompilation has not been verified as part of preparing this source snapshot.

## Shader Sources and Voice Notes

The shader project contains the two mod shaders and its Unity editor build script. Open it with the Unity editor version recorded in `ProjectSettings/ProjectVersion.txt`. Shader-bundle recompilation has not been verified in this snapshot.

The offline memo capture and transcription wrapper sources are in `source/BluePrinceVR/memo`. The public release supplies Whisper's runtime and English model separately; they are not copied into this source tree.

## Windows Installer

`source/WindowsInstaller/src` contains the WinForms installer and its built-in tests. The original packaging and verification scripts are also included. They require local packaging inputs that are intentionally not in this code repository:

- The pinned October 10 Ready archive, SHA256 `f89ba64e9eb9274e841867241647dbed83939f5933d0a0c2715caadd39aa56cd`.
- Official MelonLoader 0.7.3 `MelonLoader.x64.zip` in the installer's `build` directory. The pinned URL and hash are in `build/dependencies.json`.
- The release's `openvr_api.dll` and `OPENVR-LICENSE.txt` under `build/openvr`.
- A local `build/banner.png` for the installer artwork.

The original packaging script validates its pinned input hashes. Rebuilding a package with changed components requires reviewing and deliberately updating those inputs, not bypassing validation. The original private Ready archive is not provided by this source snapshot, so this is not a self-contained, one-command reproduction of the distributed installer.

With the required inputs supplied locally, run `Build.ps1 -SourceArchive <path>` from `source/WindowsInstaller`, then the resulting EXE with `--self-test <report-path>` and `Verify-Release.ps1 -SourceArchive <path>`. These commands create local output files. Do not publish cached game assemblies or your generated build directories.
