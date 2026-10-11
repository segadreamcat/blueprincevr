using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace BluePrinceVRSetup
{
    internal static class SelfTest
    {
        private static readonly List<string> Results = new List<string>();
        private static void Check(bool condition, string name)
        {
            if (!condition) throw new Exception(name);
            Results.Add("PASS: " + name);
        }
        private static void Reject(Action action, string name)
        {
            bool rejected = false;
            try { action(); } catch (IOException) { rejected = true; }
            Check(rejected, name);
        }
        internal static int Run(string report)
        {
            string root = Path.Combine(Path.GetTempPath(), "BPVR-Installer-Test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root); int result = 0;
            try
            {
                var engine = new Engine(delegate { }, CancellationToken.None);
                string stage = engine.Stage(root);
                Check(engine.Package.Files.Count > 20 && File.Exists(Paths.Within(stage, "Mods/BluePrinceVR-Codex/Whisper/ggml-small.en.bin")), "Embedded payload, per-file checksums and offline Whisper model verified");
                Check(engine.Package.Files.All(f => Paths.ModFile(f.Path)), "Only VR mod assets and OpenVR are included");
                Check(!engine.Package.Files.Any(f => f.Path.Contains("Trailer") || f.Path.Contains("MoreResourceful") || f.Path.Contains("KeepDevObjects") || f.Path.Contains("DevHudSwitch") || f.Path.Contains("BPVR_shot") || f.Path.Contains("GameAssembly") || f.Path.Contains("Il2CppAssemblies") || f.Path.Contains("PhotoJournal") || f.Path.Contains("BPVR_notes.png") || f.Path.EndsWith(".log")), "No game caches, private notebook image, captures, logs, developer or Trailer mods bundled");
                Check(engine.Package.SourceSha256 == "f89ba64e9eb9274e841867241647dbed83939f5933d0a0c2715caadd39aa56cd" && engine.Package.SourceArchive == "BluePrinceVR-Ready-2026-10-10.zip", "Exact October 10 Ready source archive recorded");
                Check(engine.Package.Defaults.Count == 3 && !engine.Package.Defaults.ContainsKey("DevHudSwitch"), "Only three VR preference sections imported");
                Check(engine.Package.Defaults.Values.SelectMany(v => v.Values).All(v => !v.Contains(@"C:\Users\") && !v.Contains("/home/") && !v.Contains("/Users/")), "Release preference defaults contain no private user paths");
                Check(!Paths.ModFile("UserData/BPVR_notes.png") && !Paths.ModFile("UserData/PhotoJournal/private.png") && !Paths.ModFile("Mods/KeepDevObjects.dll") && !Paths.ModFile("Mods/BluePrinceVR.Trailer.dll"), "Private images and developer or Trailer DLLs rejected by payload allowlist");
                string fresh = Engine.MergePreferences("", engine.Package.Defaults);
                Check(fresh.Contains("ActionMoveX = \"Move Horizontal\"") && fresh.Contains("ActionMoveY = \"Move Vertical\"") && fresh.Contains("ActionMenu = \"system menu\""), "Fresh installation includes working movement and menu mappings");
                Check(fresh.Contains("SkipIntro = false"), "Game intro restored on fresh installs");
                string originalPreferences = "# keep this comment\r\n[UnrelatedMod]\r\nEnabled = false\r\n[BluePrinceVR_Codex]\r\nResolutionScale = 0.65\r\nSkipIntro = true # old filming setup\r\nActionMoveX = \"\" # needs repair\r\nActionMoveY = 'Custom Vertical'\r\nActionMenu = ''\r\n[ClaudeFixes]\r\nNotebookBrightness = 0.42\r\n";
                string merged = Engine.MergePreferences(originalPreferences, engine.Package.Defaults);
                Check(merged.Contains("ActionMoveX = \"Move Horizontal\" # needs repair") && merged.Contains("ActionMenu = \"system menu\""), "Blank quoted controller mappings repaired");
                Check(merged.Contains("SkipIntro = false # old filming setup"), "Update restores intro while preserving the setting comment");
                Check(merged.Contains("ResolutionScale = 0.65\r\n") && merged.Contains("ActionMoveY = 'Custom Vertical'") && merged.Contains("NotebookBrightness = 0.42") && merged.Contains("# keep this comment\r\n[UnrelatedMod]\r\nEnabled = false"), "Custom VR values, comments, line endings and unrelated preferences preserved");
                Check(Engine.MergePreferences(merged, engine.Package.Defaults) == merged, "Preference merge is idempotent");
                Reject(delegate { Engine.MergePreferences("[BluePrinceVR_Codex]\n[BluePrinceVR_Codex]\n", engine.Package.Defaults); }, "Duplicate VR sections rejected before installation");
                Reject(delegate { Engine.MergePreferences("[BluePrinceVR_Codex]\nActionMoveX = \"a\"\nActionMoveX = \"b\"\n", engine.Package.Defaults); }, "Duplicate VR keys rejected before installation");
                Reject(delegate { Engine.MergePreferences("[UnrelatedMod]\nText = \"\"\"complex\"\"\"", engine.Package.Defaults); }, "Unsupported multiline preferences fail closed");
                Reject(delegate { Paths.Within(root, "../outside.dll"); }, "Archive path traversal rejected");
                Reject(delegate { Paths.Within(root, "C:/outside.dll"); }, "Absolute archive paths rejected");
                Reject(delegate { Paths.Within(root, "Mods/file.dll:stream"); }, "Alternate data streams rejected");
                Reject(delegate { Paths.ValidateGame(root); }, "Non-game target rejected");
                string game = Path.Combine(root, "Game"); Directory.CreateDirectory(game); Directory.CreateDirectory(Path.Combine(game, "BLUE PRINCE_Data"));
                // A synthetic x64 PE header is used only to exercise directory
                // validation. This is never launched and contains no game code.
                byte[] pe = new byte[256]; pe[0] = 0x4d; pe[1] = 0x5a; pe[0x3c] = 0x80; pe[0x80] = 0x50; pe[0x81] = 0x45; pe[0x84] = 0x64; pe[0x85] = 0x86;
                File.WriteAllBytes(Path.Combine(game, "BLUE PRINCE.exe"), pe); File.WriteAllText(Path.Combine(game, "GameAssembly.dll"), "untouched game marker");
                Check(Paths.ValidateGame(game) == game, "Valid x64 installation folder accepted");
                Directory.CreateDirectory(Path.Combine(game, "Mods")); Directory.CreateDirectory(Path.Combine(game, "UserData", "Codex-Notebook"));
                File.WriteAllText(Path.Combine(game, "Mods", "Unrelated.dll"), "keep unrelated mod");
                File.WriteAllText(Path.Combine(game, "Mods", "BluePrinceVR.dll"), "previous VR mod");
                File.WriteAllText(Path.Combine(game, "UserData", "Codex-Notebook", "private-note.txt"), "private notes");
                string small = Path.Combine(root, "Small"); Directory.CreateDirectory(Path.Combine(small, "Mods"));
                File.WriteAllText(Path.Combine(small, "Mods", "BluePrinceVR.dll"), "new VR mod");
                File.WriteAllText(Path.Combine(small, "openvr_api.dll"), "new OpenVR library");
                Reject(delegate { engine.Commit(game, new[] { small }, 1); }, "Simulated mid-install write failure handled");
                Check(File.ReadAllText(Path.Combine(game, "Mods", "BluePrinceVR.dll")) == "previous VR mod" && !File.Exists(Path.Combine(game, "openvr_api.dll")), "Failed installation rolls back existing and newly added files");
                engine.Commit(game, new[] { small }, 0);
                Check(File.ReadAllText(Path.Combine(game, "Mods", "BluePrinceVR.dll")) == "new VR mod", "Install replaces only managed files");
                Check(File.ReadAllText(Path.Combine(game, "Mods", "Unrelated.dll")) == "keep unrelated mod" && File.ReadAllText(Path.Combine(game, "UserData", "Codex-Notebook", "private-note.txt")) == "private notes", "Unrelated mods and private notes preserved");
                File.WriteAllText(Path.Combine(game, "Mods", "BluePrinceVR.dll"), "modified by user");
                Reject(delegate { Engine.Restore(game, delegate { }); }, "Restore refuses to overwrite files changed after installation");
                File.WriteAllText(Path.Combine(game, "Mods", "BluePrinceVR.dll"), "new VR mod");
                File.WriteAllText(Path.Combine(small, "Mods", "BluePrinceVR.dll"), "updated VR mod"); engine.Commit(game, new[] { small }, 0);
                Engine.Restore(game, delegate { });
                Check(File.ReadAllText(Path.Combine(game, "Mods", "BluePrinceVR.dll")) == "new VR mod", "Update restore returns to previous installer version");
                Engine.Restore(game, delegate { });
                Check(File.ReadAllText(Path.Combine(game, "Mods", "BluePrinceVR.dll")) == "previous VR mod" && !File.Exists(Path.Combine(game, "openvr_api.dll")), "Second restore returns to original installation");
                Check(File.ReadAllText(Path.Combine(game, "GameAssembly.dll")) == "untouched game marker", "Game binaries never modified");
                string seedRoot = Path.Combine(root, "SeedOnly"); Directory.CreateDirectory(Path.Combine(seedRoot, "UserData"));
                string calibration = "UserData/ClaudeFixes-keyboard-shared.txt", comfort = "UserData/Codex-comfort.json";
                File.Copy(Paths.Within(stage, calibration), Paths.Within(seedRoot, calibration));
                File.Copy(Paths.Within(stage, comfort), Paths.Within(seedRoot, comfort));
                File.WriteAllText(Paths.Within(game, calibration), "keep my calibrated keys");
                Reject(delegate { engine.Commit(game, new[] { seedRoot }, 1); }, "Interrupted fresh comfort setup handled");
                Check(File.ReadAllText(Paths.Within(game, calibration)) == "keep my calibrated keys" && !File.Exists(Paths.Within(game, comfort)), "Interrupted seed setup preserves calibration and rolls back fresh defaults");
                Receipt seeded = engine.Commit(game, new[] { seedRoot }, 0);
                Check(File.ReadAllText(Paths.Within(game, calibration)) == "keep my calibrated keys" && seeded.Changes.All(c => c.Path != calibration), "Existing keyboard calibration is never overwritten or added to restore plan");
                Check(File.ReadAllText(Paths.Within(game, comfort)).Contains("1.8"), "Fresh comfort uses generic hand span, not author's measurement");
                Engine.Restore(game, delegate { });
                Check(!File.Exists(Paths.Within(game, comfort)) && File.ReadAllText(Paths.Within(game, calibration)) == "keep my calibrated keys", "Restore removes only newly seeded defaults and preserves user calibration");
                var cancelled = new Engine(delegate { }, new CancellationToken(true));
                bool cancellation = false;
                try { cancelled.Commit(game, new[] { small }, 0); } catch (OperationCanceledException) { cancellation = true; }
                Check(cancellation && File.ReadAllText(Path.Combine(game, "Mods", "BluePrinceVR.dll")) == "previous VR mod", "Cancellation preserves original files");
                File.WriteAllText(Path.Combine(small, "BLUE PRINCE.exe"), "invalid payload");
                Reject(delegate { engine.Commit(game, new[] { small }, 0); }, "Unrelated executable rejected by installation allowlist");
                string steam = Path.Combine(root, "Steam"), library = Path.Combine(root, "Second Steam Library");
                Directory.CreateDirectory(Path.Combine(steam, "steamapps")); Directory.CreateDirectory(Path.Combine(library, "steamapps", "common", "Blue Prince"));
                File.WriteAllText(Path.Combine(steam, "steamapps", "libraryfolders.vdf"), "\"libraryfolders\" { \"1\" { \"path\" \"" + library.Replace("\\", "\\\\") + "\" } }");
                File.WriteAllText(Path.Combine(library, "steamapps", "appmanifest_1569580.acf"), "\"AppState\" { \"appid\" \"1569580\" \"installdir\" \"Blue Prince\" }");
                Check(SteamDetection.Games(steam, "1569580").Single() == Path.Combine(library, "steamapps", "common", "Blue Prince"), "Steam detection follows additional libraries, spaces and app manifests");
                File.WriteAllText(Path.Combine(steam, "steamapps", "libraryfolders.vdf"), "\"LibraryFolders\" { \"1\" \"" + library.Replace("\\", "\\\\") + "\" }");
                Check(SteamDetection.Games(steam, "1569580").Count == 1, "Legacy Steam library format detected");
                string corruption = Path.Combine(root, "corrupt.zip"); File.WriteAllText(corruption, "not a verified dependency");
                Reject(delegate { engine.StageLoader(corruption, root); }, "Corrupt or substituted loader downloads rejected before extraction");
                string loader = engine.BundledLoader(root);
                Check(File.Exists(Path.Combine(loader, "version.dll")) && File.Exists(Path.Combine(loader, "MelonLoader", "net6", "MelonLoader.dll")), "Embedded official MelonLoader 0.7.3 works without a download or adjacent files");
                Check(engine.Package.LoaderFiles.All(f => File.Exists(Paths.Within(loader, f.Path)) && Paths.Hash(Paths.Within(loader, f.Path), "SHA256") == f.Sha256), "All bundled loader files match official archive manifest");
                Check(!Directory.Exists(Path.Combine(loader, "MelonLoader", "Il2CppAssemblies")) && File.Exists(Path.Combine(loader, "MelonLoader", "Documentation", "LICENSE.md")) && File.Exists(Path.Combine(loader, "MelonLoader", "Documentation", "NOTICE.txt")), "Clean loader excludes generated game caches and retains official license and notice");
                Engine.CheckProxy(game, loader); File.WriteAllText(Path.Combine(game, "version.dll"), "some other proxy");
                Reject(delegate { Engine.CheckProxy(game, loader); }, "Unknown competing root loader is preserved");
                string config = Paths.Within(game, "UserData/MelonPreferences.cfg");
                string configStage = Path.Combine(root, "ConfigStage"); Directory.CreateDirectory(configStage);
                var preferencesEngine = new Engine(delegate { }, CancellationToken.None);
                preferencesEngine.PreparePreferences(game, configStage);
                Reject(delegate { preferencesEngine.Commit(game, new[] { configStage }, 1); }, "Simulated preference write failure handled");
                Check(!File.Exists(config), "Failed fresh installation rolls back generated preferences");
                preferencesEngine.Commit(game, new[] { configStage }, 0);
                Check(File.ReadAllText(config).Contains("ActionMoveX = \"Move Horizontal\""), "Fresh preference defaults installed transactionally");
                Engine.Restore(game, delegate { });
                Check(!File.Exists(config), "Restore removes preferences created by installer");
                File.WriteAllText(config, originalPreferences, new UTF8Encoding(false));
                string originalHash = Paths.Hash(config, "SHA256");
                preferencesEngine.PreparePreferences(game, configStage);
                preferencesEngine.Commit(game, new[] { configStage }, 0);
                Check(File.ReadAllText(config) == merged, "Existing preferences merged during update");
                File.AppendAllText(config, "# edited after installation\n");
                Reject(delegate { Engine.Restore(game, delegate { }); }, "Restore protects preferences edited after installation");
                File.WriteAllText(config, merged, new UTF8Encoding(false));
                Engine.Restore(game, delegate { });
                Check(Paths.Hash(config, "SHA256") == originalHash, "Restore recovers original preference bytes and custom mappings");
                preferencesEngine.PreparePreferences(game, configStage);
                File.AppendAllText(config, "# concurrent change\n");
                Reject(delegate { preferencesEngine.Commit(game, new[] { configStage }, 0); }, "Concurrent preference changes abort installation before mutation");
                Results.Add("All tests passed. No real game files, Microsoft runtime installers or microphones were touched.");
            }
            catch (Exception ex) { Results.Add("FAIL: " + ex); result = 1; }
            finally
            {
                // Root was created here with a fixed prefix and random GUID.
                if (Path.GetDirectoryName(root) == Path.GetTempPath().TrimEnd('\\') && Path.GetFileName(root).StartsWith("BPVR-Installer-Test-")) Directory.Delete(root, true);
                File.WriteAllLines(Path.GetFullPath(report), Results, Encoding.UTF8);
            }
            return result;
        }
    }
}
