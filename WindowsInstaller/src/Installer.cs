using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle("Blue Prince VR Windows Installer")]
[assembly: AssemblyDescription("Install Blue Prince VR into an existing Windows Steam game")]
[assembly: AssemblyVersion("1.3.0.0")]
[assembly: AssemblyFileVersion("1.3.0.0")]

namespace BluePrinceVRSetup
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            if (args.Length > 0 && args[0] == "--self-test")
                return SelfTest.Run(args.Length > 1 ? args[1] : "Installer-tests.txt");
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (args.Length > 1 && args[0] == "--preview")
            {
                using (var preview = new SetupForm(null, false))
                {
                    preview.Show(); Application.DoEvents();
                    using (var bitmap = new Bitmap(preview.Width, preview.Height))
                    { preview.DrawToBitmap(bitmap, new Rectangle(Point.Empty, preview.Size)); bitmap.Save(Path.GetFullPath(args[1])); }
                    preview.Close();
                }
                return 0;
            }
            string selected = null;
            for (int i = 0; i + 1 < args.Length; i++) if (args[i] == "--game") selected = args[i + 1];
            Application.Run(new SetupForm(selected, args.Contains("--begin")));
            return 0;
        }
        internal static bool IsAdmin()
        {
            using (var identity = WindowsIdentity.GetCurrent())
                return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }
    }

    public sealed class PackageFile { public string Path; public string Sha256; public long Size; }
    public sealed class Dependency { public string Name; public string Url; public string Hash; public string Algorithm; }
    public sealed class PackageInfo
    {
        public string Build; public string PayloadHash; public List<PackageFile> Files; public List<PackageFile> LoaderFiles;
        public string SourceArchive; public string SourceSha256;
        public Dictionary<string, Dictionary<string, string>> Defaults;
        public Dependency Loader; public Dependency DotNet; public Dependency VisualCpp;
    }
    public sealed class Change
    {
        public string Path; public bool Existed; public string OriginalHash; public string InstalledHash;
    }
    public sealed class Receipt
    {
        public string Game; public string Backup; public string Build; public List<Change> Changes;
    }

    internal static class Paths
    {
        // All writes must stay beneath a validated root, with no junctions in
        // the path. This also applies to archive names and saved receipts.
        internal static string Within(string root, string relative)
        {
            if (String.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative) || relative.IndexOf(':') >= 0)
                throw new IOException("Invalid relative file path: " + relative);
            string[] parts = relative.Replace('/', '\\').Split('\\');
            if (parts.Any(p => p == ".." || p == "." || p.Length == 0 || p.EndsWith(".") || p.EndsWith(" ")))
                throw new IOException("Unsafe file path: " + relative);
            string absoluteRoot = Path.GetFullPath(root).TrimEnd('\\') + "\\";
            string full = Path.GetFullPath(Path.Combine(absoluteRoot, relative));
            if (!full.StartsWith(absoluteRoot, StringComparison.OrdinalIgnoreCase)) throw new IOException("Path leaves its folder.");
            RejectLinks(absoluteRoot.TrimEnd('\\'));
            string current = absoluteRoot.TrimEnd('\\');
            foreach (string part in parts) { current = Path.Combine(current, part); RejectLinks(current); }
            return full;
        }
        private static void RejectLinks(string path)
        {
            if ((File.Exists(path) || Directory.Exists(path)) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Choose a real folder instead of a junction or symbolic link: " + path);
        }
        internal static string ValidateGame(string root)
        {
            root = Path.GetFullPath(root.Trim().Trim('"')).TrimEnd('\\');
            if (File.Exists(root) && String.Equals(Path.GetFileName(root), "BLUE PRINCE.exe", StringComparison.OrdinalIgnoreCase)) root = Path.GetDirectoryName(root);
            if (!File.Exists(Within(root, "BLUE PRINCE.exe")) || !File.Exists(Within(root, "GameAssembly.dll")) || !Directory.Exists(Within(root, "BLUE PRINCE_Data")))
                throw new IOException("Select the Blue Prince folder containing BLUE PRINCE.exe, GameAssembly.dll and BLUE PRINCE_Data.");
            using (var reader = new BinaryReader(File.OpenRead(Within(root, "BLUE PRINCE.exe"))))
            {
                if (reader.ReadUInt16() != 0x5a4d) throw new IOException("The selected game executable is invalid.");
                reader.BaseStream.Position = 0x3c; int offset = reader.ReadInt32();
                if (offset < 64 || offset > reader.BaseStream.Length - 6) throw new IOException("Invalid executable header.");
                reader.BaseStream.Position = offset;
                if (reader.ReadUInt32() != 0x4550 || reader.ReadUInt16() != 0x8664) throw new IOException("This installer supports the 64-bit Windows game.");
            }
            return root;
        }
        internal static void EnsureClosed(string root)
        {
            foreach (var process in Process.GetProcessesByName("BLUE PRINCE"))
            {
                using (process)
                {
                    try { if (!process.HasExited) throw new IOException("Close Blue Prince before installing or restoring."); }
                    catch (InvalidOperationException) { }
                }
            }
        }
        internal static string Hash(string path, string algorithm)
        {
            using (var stream = File.OpenRead(path))
            using (HashAlgorithm hash = algorithm == "SHA512" ? (HashAlgorithm)SHA512.Create() : SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
        internal static void RequireHash(string path, string expected, string algorithm)
        {
            if (!String.Equals(Hash(path, algorithm), expected, StringComparison.OrdinalIgnoreCase))
                throw new IOException("Verification failed for " + Path.GetFileName(path) + ". No unverified download will be installed.");
        }
        internal static bool ModFile(string relative)
        {
            string p = relative.Replace('\\', '/');
            return p.StartsWith("Mods/BluePrinceVR-Codex/", StringComparison.OrdinalIgnoreCase) ||
                SeedFile(p) || new[] { "Mods/BluePrinceVR.dll", "Mods/BluePrinceVR.ClaudeFixes.dll", "Mods/BluePrinceVR.Fountain.dll", "Mods/BluePrinceVR.JournalArt.dll", "Mods/BluePrinceVR.RecorderPolish.dll", "openvr_api.dll", "OPENVR-LICENSE.txt", "BluePrinceVR-Installer/CONTROLS.txt", "BluePrinceVR-Installer/FIRST-RUN.txt", "BluePrinceVR-Installer/THIRD-PARTY-NOTICES.md", "UserData/BPVR_VR.png", "UserData/BPVR_smoke.png", "UserData/BPVR_puff.png", "UserData/BPVR_select.png", "UserData/arms.bpvr" }.Contains(p, StringComparer.OrdinalIgnoreCase);
        }
        internal static bool SeedFile(string relative)
        {
            return new[] { "UserData/Codex-keyboard-Laboratory-Computer.json", "UserData/Codex-keyboard-Office-Computer.json", "UserData/Codex-keyboard-Standalone_Rooms-Computer.json", "UserData/Codex-keyboard-Security-Click_Trigger.json", "UserData/Codex-keyboard-points.json", "UserData/ClaudeFixes-keyboard-Office-Computer.txt", "UserData/ClaudeFixes-keyboard-shared.txt", "UserData/BluePrinceVR-RecorderPolish.cfg", "UserData/Codex-comfort.json" }.Contains(relative.Replace('\\', '/'), StringComparer.OrdinalIgnoreCase);
        }
        internal static bool Managed(string relative)
        {
            string p = relative.Replace('\\', '/');
            return ModFile(p) || p.StartsWith("MelonLoader/", StringComparison.OrdinalIgnoreCase) || p == "version.dll" || p == "dobby.dll" || p == "UserData/MelonPreferences.cfg";
        }
    }

    internal static class SteamDetection
    {
        internal static List<string> Libraries(string explicitSteam)
        {
            var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!String.IsNullOrEmpty(explicitSteam)) roots.Add(explicitSteam);
            else
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
                    if (key != null && key.GetValue("SteamPath") is string) roots.Add((string)key.GetValue("SteamPath"));
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam"))
                    if (key != null && key.GetValue("InstallPath") is string) roots.Add((string)key.GetValue("InstallPath"));
                roots.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam"));
            }
            foreach (string root in roots.ToArray())
            {
                string vdf = Path.Combine(root, "steamapps", "libraryfolders.vdf");
                if (!File.Exists(vdf)) continue;
                string text = File.ReadAllText(vdf);
                foreach (Match match in Regex.Matches(text, "\"path\"\\s*\"((?:\\\\.|[^\"])*)\"")) roots.Add(Unescape(match.Groups[1].Value));
                // Older Steam library format used numeric keys and plain paths.
                foreach (Match match in Regex.Matches(text, "\"[0-9]+\"\\s*\"([A-Za-z]:[^\"]*)\"")) roots.Add(Unescape(match.Groups[1].Value));
            }
            return roots.Where(Directory.Exists).ToList();
        }
        private static string Unescape(string text) { return text.Replace("\\\\", "\\").Replace("\\\"", "\""); }
        internal static List<string> Games(string explicitSteam, string appId)
        {
            var result = new List<string>();
            foreach (string root in Libraries(explicitSteam))
            {
                string acf = Path.Combine(root, "steamapps", "appmanifest_" + appId + ".acf");
                if (!File.Exists(acf)) continue;
                Match match = Regex.Match(File.ReadAllText(acf), "\"installdir\"\\s*\"((?:\\\\.|[^\"])*)\"");
                if (!match.Success) continue;
                string path = Paths.Within(Path.Combine(root, "steamapps", "common"), Unescape(match.Groups[1].Value));
                if (Directory.Exists(path) && !result.Contains(path)) result.Add(path);
            }
            return result;
        }
        internal static bool DotNetReady()
        {
            using (var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(@"SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App"))
                if (key != null && key.GetValueNames().Any(v => v.StartsWith("6.0."))) return true;
            string root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet", "shared", "Microsoft.WindowsDesktop.App");
            return Directory.Exists(root) && Directory.GetDirectories(root, "6.0.*").Any();
        }
        internal static bool CppReady()
        {
            string system = Environment.GetFolderPath(Environment.SpecialFolder.System);
            return File.Exists(Path.Combine(system, "vcruntime140.dll")) && File.Exists(Path.Combine(system, "vcruntime140_1.dll")) && File.Exists(Path.Combine(system, "msvcp140.dll"));
        }
    }

    internal sealed class Engine
    {
        internal readonly PackageInfo Package;
        private readonly Action<string, int> progress;
        private readonly CancellationToken token;
        private bool preferencesPrepared;
        private string preferencesOriginalHash;
        internal Engine(Action<string, int> update, CancellationToken cancellation)
        {
            progress = update; token = cancellation;
            using (var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("package.json")))
                Package = new JavaScriptSerializer().Deserialize<PackageInfo>(reader.ReadToEnd());
        }
        internal string Stage(string temporary)
        {
            progress("Checking the bundled VR mod and offline voice model…", 5);
            string archive = Path.Combine(temporary, "payload.zip");
            using (var input = Assembly.GetExecutingAssembly().GetManifestResourceStream("payload.zip"))
            using (var output = File.Create(archive)) Copy(input, output);
            Paths.RequireHash(archive, Package.PayloadHash, "SHA256");
            string staging = Path.Combine(temporary, "mod"); Directory.CreateDirectory(staging);
            var expected = Package.Files.ToDictionary(f => f.Path, StringComparer.OrdinalIgnoreCase);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var zip = ZipFile.OpenRead(archive))
            {
                foreach (var entry in zip.Entries)
                {
                    token.ThrowIfCancellationRequested();
                    if (entry.FullName.EndsWith("/")) continue;
                    PackageFile file;
                    if (!Paths.ModFile(entry.FullName) || !expected.TryGetValue(entry.FullName, out file) || !seen.Add(entry.FullName) || entry.Length != file.Size)
                        throw new IOException("Unexpected file in installer payload: " + entry.FullName);
                    string target = Paths.Within(staging, entry.FullName);
                    Directory.CreateDirectory(Path.GetDirectoryName(target));
                    using (var input = entry.Open()) using (var output = File.Create(target)) Copy(input, output);
                    Paths.RequireHash(target, file.Sha256, "SHA256");
                    progress("Verified " + Path.GetFileName(target), 5 + (int)(20.0 * seen.Count / expected.Count));
                }
            }
            if (seen.Count != expected.Count) throw new IOException("The bundled mod is incomplete.");
            return staging;
        }
        internal static string MergePreferences(string current, Dictionary<string, Dictionary<string, string>> defaults)
        {
            // This is a surgical edit of MelonLoader's scalar preference format,
            // not a general TOML reserializer. Preserve unrelated text verbatim.
            if (current.Contains("\"\"\"") || current.Contains("'''"))
                throw new IOException("Multiline preference strings cannot be merged safely. Back up MelonPreferences.cfg and use a standard MelonLoader-generated config.");
            string newline = current.Contains("\r\n") ? "\r\n" : "\n";
            string result = current;
            var actions = new HashSet<string>(new[] { "ActionMoveX", "ActionMoveY", "ActionLookX", "ActionLookY", "ActionInteract", "ActionCancel", "ActionMenu", "ActionNavUp", "ActionNavDown", "ActionNavLeft", "ActionNavRight", "ActionSelect" });
            foreach (var section in defaults)
            {
                if (!new[] { "BluePrinceVR_Codex", "BluePrinceVR_Fountain", "ClaudeFixes" }.Contains(section.Key)) throw new IOException("Unexpected preference defaults section.");
                string header = @"(?m)^[ \t]*\[" + Regex.Escape(section.Key) + @"\][ \t]*(?:#[^\r\n]*)?\r?$";
                var matches = Regex.Matches(result, header);
                if (matches.Count > 1) throw new IOException("Duplicate VR preference section: " + section.Key);
                if (matches.Count == 0)
                {
                    result += (result.EndsWith(newline) || result.Length == 0 ? "" : newline) + newline + "[" + section.Key + "]" + newline;
                    foreach (var pair in section.Value) result += pair.Key + " = " + pair.Value + newline;
                    continue;
                }
                int start = matches[0].Index + matches[0].Length;
                var next = Regex.Match(result.Substring(start), @"(?m)^[ \t]*\[");
                int end = next.Success ? start + next.Index : result.Length;
                string body = result.Substring(start, end - start);
                foreach (var pair in section.Value)
                {
                    string key = @"(?m)^[ \t]*" + Regex.Escape(pair.Key) + @"[ \t]*=";
                    var keys = Regex.Matches(body, key);
                    if (keys.Count > 1) throw new IOException("Duplicate VR preference key: " + pair.Key);
                    if (keys.Count == 0)
                        body += (body.EndsWith(newline) ? "" : newline) + pair.Key + " = " + pair.Value + newline;
                    else if (section.Key == "BluePrinceVR_Codex" && pair.Key == "SkipIntro")
                    {
                        body = Regex.Replace(body, key + @"[ \t]*true[ \t]*(#[^\r\n]*)?\r?$", delegate(Match m) { return "SkipIntro = false" + (m.Groups[1].Success ? " " + m.Groups[1].Value : "") + (m.Value.EndsWith("\r") ? "\r" : ""); });
                    }
                    else if (section.Key == "BluePrinceVR_Codex" && actions.Contains(pair.Key))
                    {
                        string empty = key + "[ \\t]*(?:\"\"|'')[ \\t]*(#[^\\r\\n]*)?\\r?$";
                        body = Regex.Replace(body, empty, delegate(Match m) { return pair.Key + " = " + pair.Value + (m.Groups[1].Success ? " " + m.Groups[1].Value : "") + (m.Value.EndsWith("\r") ? "\r" : ""); });
                    }
                }
                result = result.Substring(0, start) + body + result.Substring(end);
            }
            return result;
        }
        internal void PreparePreferences(string game, string staging)
        {
            string target = Paths.Within(game, "UserData/MelonPreferences.cfg");
            preferencesOriginalHash = File.Exists(target) ? Paths.Hash(target, "SHA256") : null;
            string current = File.Exists(target) ? File.ReadAllText(target) : "";
            string merged = MergePreferences(current, Package.Defaults);
            preferencesPrepared = true;
            if (merged == current) return;
            string staged = Paths.Within(staging, "UserData/MelonPreferences.cfg");
            Directory.CreateDirectory(Path.GetDirectoryName(staged)); File.WriteAllText(staged, merged, new UTF8Encoding(false));
        }
        private void Copy(Stream input, Stream output)
        {
            byte[] buffer = new byte[131072]; int read;
            while ((read = input.Read(buffer, 0, buffer.Length)) > 0) { token.ThrowIfCancellationRequested(); output.Write(buffer, 0, read); }
        }
        internal async Task<string> Download(Dependency dependency, string temporary)
        {
            token.ThrowIfCancellationRequested();
            string target = Path.Combine(temporary, dependency.Name);
            var request = (HttpWebRequest)WebRequest.Create(dependency.Url);
            request.UserAgent = "BluePrinceVR-Installer/1.3"; request.Timeout = 30000; request.ReadWriteTimeout = 30000;
            using (token.Register(delegate { request.Abort(); }))
            using (var response = (HttpWebResponse)await request.GetResponseAsync())
            using (var stream = response.GetResponseStream())
            using (var output = File.Create(target))
            {
                if (response.ResponseUri.Scheme != "https") throw new IOException("Download was redirected to an insecure address.");
                long downloaded = 0; byte[] buffer = new byte[131072]; int read;
                while ((read = await stream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
                {
                    await output.WriteAsync(buffer, 0, read, token); downloaded += read;
                    progress("Downloading " + dependency.Name + " — " + (downloaded / 1048576) + " MB", 30);
                }
            }
            Paths.RequireHash(target, dependency.Hash, dependency.Algorithm);
            progress("Verified official download: " + dependency.Name, 35);
            return target;
        }
        internal string StageLoader(string zipPath, string temporary)
        {
            Paths.RequireHash(zipPath, Package.Loader.Hash, Package.Loader.Algorithm);
            string root = Path.Combine(temporary, "loader"); Directory.CreateDirectory(root);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var zip = ZipFile.OpenRead(zipPath))
            {
                foreach (var entry in zip.Entries)
                {
                    if (entry.FullName.EndsWith("/")) continue;
                    string name = entry.FullName.Replace('\\', '/');
                    if (!(name.StartsWith("MelonLoader/", StringComparison.OrdinalIgnoreCase) || name == "version.dll" || name == "dobby.dll")) continue;
                    if (!seen.Add(name)) throw new IOException("Duplicate MelonLoader file.");
                    string target = Paths.Within(root, name); Directory.CreateDirectory(Path.GetDirectoryName(target));
                    using (var input = entry.Open()) using (var output = File.Create(target)) Copy(input, output);
                }
            }
            if (!File.Exists(Path.Combine(root, "version.dll")) || !File.Exists(Path.Combine(root, "MelonLoader", "net6", "MelonLoader.dll")))
                throw new IOException("Official MelonLoader archive is incomplete.");
            return root;
        }
        internal string BundledLoader(string temporary)
        {
            string archive = Path.Combine(temporary, "MelonLoader.x64.zip");
            using (var input = Assembly.GetExecutingAssembly().GetManifestResourceStream("loader.zip"))
            using (var output = File.Create(archive))
            {
                if (input == null) throw new IOException("Bundled MelonLoader is missing.");
                Copy(input, output);
            }
            return StageLoader(archive, temporary);
        }
        internal static void CheckProxy(string game, string stagedLoader)
        {
            string proxy = Paths.Within(game, "version.dll");
            if (!File.Exists(proxy) || Paths.Hash(proxy, "SHA256") == Paths.Hash(Paths.Within(stagedLoader, "version.dll"), "SHA256")) return;
            string description = FileVersionInfo.GetVersionInfo(proxy).FileDescription ?? "";
            string product = FileVersionInfo.GetVersionInfo(proxy).ProductName ?? "";
            if (description.IndexOf("MelonLoader", StringComparison.OrdinalIgnoreCase) < 0 && product.IndexOf("MelonLoader", StringComparison.OrdinalIgnoreCase) < 0)
                throw new IOException("This game already has a different version.dll loader. Remove or back up that other loader first; this installer will not overwrite it.");
        }
        internal static bool RunDependency(string exe, Action<string, int> update)
        {
            update("Installing " + Path.GetFileName(exe) + "…", 45);
            using (var process = Process.Start(new ProcessStartInfo(exe, "/install /quiet /norestart") { UseShellExecute = false, CreateNoWindow = true }))
            {
                process.WaitForExit();
                if (process.ExitCode == 0) return false;
                if (process.ExitCode == 3010 || process.ExitCode == 1641) return true;
                throw new IOException("Dependency installer returned " + process.ExitCode + ". See the Microsoft installer log in your temporary folder.");
            }
        }
        internal Receipt Commit(string game, IList<string> roots, int failAfter)
        {
            game = Paths.ValidateGame(game); Paths.EnsureClosed(game);
            var files = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string root in roots) foreach (string file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
            {
                string relative = file.Substring(root.TrimEnd('\\').Length + 1).Replace('\\', '/');
                if (!Paths.Managed(relative)) throw new IOException("Refusing to install unrelated file: " + relative);
                string target = Paths.Within(game, relative);
                if (Paths.SeedFile(relative) && File.Exists(target)) continue;
                files[relative] = file;
            }
            if (preferencesPrepared)
            {
                string target = Paths.Within(game, "UserData/MelonPreferences.cfg");
                string actual = File.Exists(target) ? Paths.Hash(target, "SHA256") : null;
                if (actual != preferencesOriginalHash) throw new IOException("Preferences changed while setup was preparing. Close the game and run setup again.");
            }
            string backupName = "BluePrinceVR-Backups/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            var receipt = new Receipt { Game = game, Backup = backupName, Build = Package.Build, Changes = new List<Change>() };
            string backup = Paths.Within(game, backupName); Directory.CreateDirectory(backup);
            string state = Paths.Within(game, "BluePrinceVR-Installer/installed.json");
            string previous = File.Exists(state) ? File.ReadAllText(state) : null;
            if (previous != null) File.WriteAllText(Paths.Within(backup, "previous-receipt.json"), previous);
            bool completed = false;
            try
            {
                // Stage every old file and the full recovery plan before the
                // first game-folder mutation. Only managed files are touched.
                int n = 0;
                foreach (var pair in files)
                {
                    token.ThrowIfCancellationRequested(); string target = Paths.Within(game, pair.Key);
                    var change = new Change { Path = pair.Key, Existed = File.Exists(target), InstalledHash = Paths.Hash(pair.Value, "SHA256") };
                    if (change.Existed)
                    {
                        change.OriginalHash = Paths.Hash(target, "SHA256");
                        string saved = Paths.Within(backup, "files/" + pair.Key); Directory.CreateDirectory(Path.GetDirectoryName(saved)); File.Copy(target, saved, false);
                        Paths.RequireHash(saved, change.OriginalHash, "SHA256");
                    }
                    receipt.Changes.Add(change);
                }
                SaveReceipt(Paths.Within(backup, "recovery.json"), receipt);
                foreach (var pair in files)
                {
                    token.ThrowIfCancellationRequested(); Paths.EnsureClosed(game);
                    Change change = receipt.Changes[n]; string target = Paths.Within(game, pair.Key);
                    if (change.Existed) Paths.RequireHash(target, change.OriginalHash, "SHA256");
                    else if (File.Exists(target)) throw new IOException("A file appeared during installation: " + pair.Key);
                    Directory.CreateDirectory(Path.GetDirectoryName(target));
                    // A complete sibling temporary file is atomically moved or
                    // replaced, so interrupted copies never leave half a DLL.
                    string temporary = target + ".bpvr-" + Guid.NewGuid().ToString("N");
                    try
                    {
                        File.Copy(pair.Value, temporary, false); Paths.RequireHash(temporary, change.InstalledHash, "SHA256");
                        if (change.Existed) File.Replace(temporary, target, null); else File.Move(temporary, target);
                    }
                    finally { if (File.Exists(temporary)) File.Delete(temporary); }
                    n++; progress("Installed " + pair.Key, 55 + (int)(40.0 * n / files.Count));
                    if (failAfter > 0 && n >= failAfter) throw new IOException("Simulated interrupted installation.");
                }
                Directory.CreateDirectory(Path.GetDirectoryName(state)); SaveReceipt(state, receipt);
                completed = true; progress("Installation complete. Your saves and notes are untouched.", 100); return receipt;
            }
            finally
            {
                if (!completed)
                {
                    // Restore only files which this transaction actually wrote.
                    foreach (var change in receipt.Changes.AsEnumerable().Reverse())
                    {
                        string target = Paths.Within(game, change.Path);
                        if (File.Exists(target) && Paths.Hash(target, "SHA256") == change.InstalledHash)
                        {
                            if (change.Existed) File.Copy(Paths.Within(backup, "files/" + change.Path), target, true);
                            else File.Delete(target);
                        }
                    }
                    progress("Installation stopped; previous files restored. Recovery folder: " + backup, 0);
                }
            }
        }
        private static void SaveReceipt(string path, Receipt receipt)
        {
            string temporary = path + ".bpvr-" + Guid.NewGuid().ToString("N");
            try
            {
                File.WriteAllText(temporary, new JavaScriptSerializer().Serialize(receipt), Encoding.UTF8);
                if (File.Exists(path)) File.Replace(temporary, path, null); else File.Move(temporary, path);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
        internal static Receipt ReadReceipt(string game)
        {
            string path = Paths.Within(game, "BluePrinceVR-Installer/installed.json");
            if (!File.Exists(path)) throw new IOException("No installation made by this installer was found. Manual installs are left untouched.");
            return new JavaScriptSerializer().Deserialize<Receipt>(File.ReadAllText(path));
        }
        internal static void Restore(string game, Action<string, int> update)
        {
            game = Paths.ValidateGame(game); Paths.EnsureClosed(game); Receipt receipt = ReadReceipt(game);
            if (!String.Equals(game, receipt.Game, StringComparison.OrdinalIgnoreCase) || receipt.Changes == null || receipt.Changes.Count == 0 || !Regex.IsMatch(receipt.Backup ?? "", @"^BluePrinceVR-Backups/[0-9]{8}-[0-9]{6}-[a-f0-9]{8}$"))
                throw new IOException("Invalid installation receipt.");
            string backup = Paths.Within(game, receipt.Backup);
            // Validate everything before reverting anything. User changes stop
            // restoration rather than silently being overwritten or removed.
            foreach (var change in receipt.Changes)
            {
                if (!Paths.Managed(change.Path)) throw new IOException("Invalid recovery file.");
                string target = Paths.Within(game, change.Path);
                if (File.Exists(target)) Paths.RequireHash(target, change.InstalledHash, "SHA256");
                if (change.Existed) Paths.RequireHash(Paths.Within(backup, "files/" + change.Path), change.OriginalHash, "SHA256");
            }
            // Restoring is itself transactional: a failure rolls back files
            // already restored, including files newly added by this installer.
            string recovery = Paths.Within(backup, "restore-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(recovery);
            var changed = new List<Change>();
            try
            {
                int n = 0;
                foreach (var change in receipt.Changes.AsEnumerable().Reverse())
                {
                    string target = Paths.Within(game, change.Path); string saved = Paths.Within(recovery, change.Path);
                    if (File.Exists(target)) { Directory.CreateDirectory(Path.GetDirectoryName(saved)); File.Copy(target, saved, false); }
                    changed.Add(change);
                    if (change.Existed) { Directory.CreateDirectory(Path.GetDirectoryName(target)); File.Copy(Paths.Within(backup, "files/" + change.Path), target, true); }
                    else if (File.Exists(target)) File.Delete(target);
                    n++; update("Restored " + change.Path, (int)(95.0 * n / receipt.Changes.Count));
                }
                string state = Paths.Within(game, "BluePrinceVR-Installer/installed.json");
                string previous = Paths.Within(backup, "previous-receipt.json");
                if (File.Exists(previous)) File.Copy(previous, state, true); else File.Delete(state);
            }
            catch
            {
                foreach (var change in changed)
                {
                    string saved = Paths.Within(recovery, change.Path), target = Paths.Within(game, change.Path);
                    if (File.Exists(saved)) File.Copy(saved, target, true); else if (File.Exists(target)) File.Delete(target);
                }
                throw;
            }
            update("Previous installation restored. Personal saves, notes and other mods were kept.", 100);
        }
    }

    internal sealed class SetupForm : Form
    {
        private readonly ComboBox folder = new ComboBox();
        private readonly Button install = new Button(), browse = new Button(), restore = new Button(), finish = new Button();
        private readonly Label status = new Label(), dependencies = new Label();
        private readonly ProgressBar bar = new ProgressBar();
        private readonly TextBox log = new TextBox();
        private readonly CheckBox setupSteamVR = new CheckBox();
        private CancellationTokenSource cancellation;
        private bool busy, installed;
        private string game, logPath;
        internal SetupForm(string selected, bool begin)
        {
            Text = "Blue Prince VR - Windows Installer"; ClientSize = new Size(880, 760); MinimumSize = new Size(850, 800);
            StartPosition = FormStartPosition.CenterScreen; BackColor = Color.FromArgb(13, 20, 27); ForeColor = Color.FromArgb(225, 225, 216);
            Font = new Font("Segoe UI", 10); AutoScaleMode = AutoScaleMode.Dpi;
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("banner.png"))
            {
                var picture = new PictureBox { Image = new Bitmap(stream), SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.Black, Bounds = new Rectangle(0, 0, 880, 248), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
                Controls.Add(picture);
            }
            AddLabel("Install Blue Prince VR", 28, 258, 820, 36, 20);
            AddLabel("October 10, 2026 | Windows PC VR | bundled MelonLoader | offline voice notes", 30, 303, 815, 30, 10);
            AddLabel("Blue Prince game folder", 30, 346, 700, 24, 10);
            folder.SetBounds(30, 376, 696, 32); folder.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; folder.DropDownStyle = ComboBoxStyle.DropDown;
            Controls.Add(folder); AddButton(browse, "Browse…", 740, 374, 110, delegate { Browse(); });
            dependencies.SetBounds(30, 419, 820, 50); dependencies.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; Controls.Add(dependencies);
            setupSteamVR.Text = "Open SteamVR installation in Steam if it is missing"; setupSteamVR.Checked = true; setupSteamVR.SetBounds(30, 477, 810, 28); Controls.Add(setupSteamVR);
            status.Text = "Choose your game folder, then install. Existing replaced files are backed up."; status.SetBounds(30, 512, 815, 40); status.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; Controls.Add(status);
            bar.SetBounds(30, 558, 820, 12); bar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; Controls.Add(bar);
            log.Multiline = true; log.ReadOnly = true; log.ScrollBars = ScrollBars.Vertical; log.BorderStyle = BorderStyle.None; log.BackColor = Color.FromArgb(21, 31, 40); log.ForeColor = ForeColor;
            log.SetBounds(30, 584, 820, 96); log.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right; Controls.Add(log);
            AddButton(restore, "Restore previous files", 30, 704, 205, async delegate { await Restore(); });
            AddButton(finish, "Close", 586, 704, 100, delegate { if (busy) cancellation.Cancel(); else Close(); });
            AddButton(install, "Install / update VR", 700, 704, 150, async delegate { if (installed) Open("steam://rungameid/1569580"); else await Install(); });
            foreach (Button button in new[] { restore, finish, install }) button.Anchor = AnchorStyles.Bottom | (button == restore ? AnchorStyles.Left : AnchorStyles.Right);
            install.BackColor = Color.FromArgb(50, 82, 102); install.ForeColor = Color.White;
            foreach (string path in SteamDetection.Games(null, "1569580")) folder.Items.Add(path);
            if (selected != null) folder.Text = selected; else if (folder.Items.Count > 0) folder.SelectedIndex = 0;
            dependencies.Text = ".NET 6 Desktop: " + (SteamDetection.DotNetReady() ? "ready" : "will be installed") + "   •   Visual C++: " + (SteamDetection.CppReady() ? "ready" : "will be installed") + "\r\nMelonLoader 0.7.3 will be installed. SteamVR must be running before you play.";
            FormClosing += delegate(object sender, FormClosingEventArgs e) { if (busy) { e.Cancel = true; cancellation.Cancel(); status.Text = "Stopping safely; please wait for file restoration…"; } };
            Shown += async delegate { if (begin) await Install(); };
        }
        private void AddLabel(string text, int x, int y, int w, int h, int size) { Controls.Add(new Label { Text = text, Bounds = new Rectangle(x, y, w, h), Font = new Font("Segoe UI", size), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right }); }
        private void AddButton(Button button, string text, int x, int y, int w, EventHandler click)
        {
            button.Text = text; button.SetBounds(x, y, w, 36); button.FlatStyle = FlatStyle.Flat; button.FlatAppearance.BorderColor = Color.FromArgb(80, 98, 110); button.Click += click; Controls.Add(button);
        }
        private void Browse()
        {
            using (var dialog = new OpenFileDialog { Title = "Select BLUE PRINCE.exe", Filter = "Blue Prince|BLUE PRINCE.exe", CheckFileExists = true })
                if (dialog.ShowDialog(this) == DialogResult.OK) folder.Text = Path.GetDirectoryName(dialog.FileName);
        }
        private static void Open(string path) { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
        private void Report(string text, int percent)
        {
            if (InvokeRequired) { BeginInvoke((Action)(delegate { Report(text, percent); })); return; }
            status.Text = text; bar.Value = Math.Max(0, Math.Min(100, percent));
            if (text.StartsWith("Downloading ") && log.Lines.LastOrDefault() == text) return;
            log.AppendText(text + "\r\n");
            if (logPath != null) File.AppendAllText(logPath, DateTime.Now.ToString("HH:mm:ss ") + text + Environment.NewLine);
        }
        private bool Elevate(bool restoreAction)
        {
            if (Program.IsAdmin()) return false;
            string arguments = "--game \"" + game + "\"" + (restoreAction ? "" : " --begin");
            try { Process.Start(new ProcessStartInfo(Application.ExecutablePath, arguments) { UseShellExecute = true, Verb = "runas" }); Close(); return true; }
            catch (System.ComponentModel.Win32Exception) { throw new IOException("Installation needs Windows permission to write the game folder. Click Install again and accept the Windows prompt."); }
        }
        private void SetBusy(bool value)
        {
            busy = value; browse.Enabled = folder.Enabled = restore.Enabled = setupSteamVR.Enabled = install.Enabled = !value;
            finish.Text = value ? "Cancel" : "Close";
        }
        private async Task Install()
        {
            string temporary = null;
            try
            {
                game = Paths.ValidateGame(folder.Text); Paths.EnsureClosed(game);
                if (!Environment.Is64BitOperatingSystem) throw new IOException("64-bit Windows is required.");
                if (Elevate(false)) return;
                installed = false; SetBusy(true); cancellation = new CancellationTokenSource();
                string logs = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BluePrinceVR", "InstallerLogs"); Directory.CreateDirectory(logs);
                logPath = Path.Combine(logs, DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".log");
                temporary = Path.Combine(Path.GetTempPath(), "BPVR-Setup-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(temporary);
                string temp = temporary; bool steam = setupSteamVR.Checked;
                await Task.Run(async delegate
                {
                    var engine = new Engine(Report, cancellation.Token);
                    Report("Preparing build " + engine.Package.Build, 1);
                    var drive = new DriveInfo(Path.GetPathRoot(game));
                    if (drive.AvailableFreeSpace < 2200000000L) throw new IOException("Keep at least 2.2 GB free on the game drive for voice notes and backups.");
                    string mod = engine.Stage(temp);
                    engine.PreparePreferences(game, mod);
                    string loader = engine.BundledLoader(temp); Engine.CheckProxy(game, loader);
                    string dotnet = null, cpp = null;
                    if (!SteamDetection.DotNetReady()) dotnet = await engine.Download(engine.Package.DotNet, temp);
                    if (!SteamDetection.CppReady()) cpp = await engine.Download(engine.Package.VisualCpp, temp);
                    cancellation.Token.ThrowIfCancellationRequested();
                    bool reboot = false;
                    if (cpp != null) reboot |= Engine.RunDependency(cpp, Report);
                    if (dotnet != null) reboot |= Engine.RunDependency(dotnet, Report);
                    if (!SteamDetection.DotNetReady() || !SteamDetection.CppReady()) throw new IOException("Runtime setup did not complete. Restart Windows if requested, then run this installer again.");
                    engine.Commit(game, new[] { loader, mod }, 0);
                    Report(reboot ? "Installed. Restart Windows before playing." : "Installed! Start SteamVR, then play Blue Prince from Steam. First launch prepares MelonLoader; give it a few minutes.", 100);
                });
                installed = true; install.Text = "Play Blue Prince";
                if (steam && SteamDetection.Games(null, "250820").Count == 0) { Report("SteamVR is missing. Finish its installation in the Steam window, then start SteamVR before playing.", 100); Open("steam://install/250820"); }
                var folderButton = new Button(); AddButton(folderButton, "Game folder", 260, 704, 130, delegate { Open(game); }); folderButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
                var vrButton = new Button(); AddButton(vrButton, "Start SteamVR", 404, 704, 160, delegate { Open("steam://rungameid/250820"); }); vrButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            }
            catch (Exception ex) { Report((cancellation != null && cancellation.IsCancellationRequested ? "Cancelled safely." : ex.GetBaseException().Message) + (logPath == null ? "" : " Log: " + logPath), 0); }
            finally
            {
                SetBusy(false);
                // Only our unique temporary directory is ever cleaned up.
                if (temporary != null && Path.GetFileName(temporary).StartsWith("BPVR-Setup-") && Directory.Exists(temporary)) { try { Directory.Delete(temporary, true); } catch { } }
            }
        }
        private async Task Restore()
        {
            try
            {
                game = Paths.ValidateGame(folder.Text); Paths.EnsureClosed(game); Engine.ReadReceipt(game);
                if (MessageBox.Show(this, "Restore the files from before your last installation? Saves, voice notes and unrelated mods are kept. Microsoft runtimes stay installed.", "Restore Blue Prince VR", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                if (Elevate(true)) return;
                SetBusy(true); cancellation = new CancellationTokenSource();
                await Task.Run(delegate { Engine.Restore(game, Report); }); installed = false; install.Text = "Install / update VR";
            }
            catch (Exception ex) { Report(ex.GetBaseException().Message, 0); }
            finally { SetBusy(false); }
        }
    }
}
