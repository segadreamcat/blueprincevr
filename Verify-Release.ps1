param([Parameter(Mandatory=$true)][string]$SourceArchive)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$package=Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'build/package.json') | ConvertFrom-Json
$provenance=Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'build/provenance.json') | ConvertFrom-Json
$release=Join-Path $PSScriptRoot 'release'
$manualPath=Join-Path $release 'BluePrinceVR-Windows-Manual-2026-10-10.zip'
$exePath=Join-Path $release 'BluePrinceVR-Windows-Setup-2026-10-10.exe'
function Entry-Hash($entry) {
    $input=$entry.Open(); $sha=[Security.Cryptography.SHA256]::Create()
    try { return [BitConverter]::ToString($sha.ComputeHash($input)).Replace('-','').ToLowerInvariant() }
    finally { $input.Dispose(); $sha.Dispose() }
}
if ((Get-FileHash -LiteralPath $SourceArchive -Algorithm SHA256).Hash.ToLowerInvariant() -ne $package.SourceSha256) { throw 'Source release changed during packaging.' }
$source=[IO.Compression.ZipFile]::OpenRead($SourceArchive)
$payload=[IO.Compression.ZipFile]::OpenRead((Join-Path $PSScriptRoot 'build/payload.zip'))
$manual=[IO.Compression.ZipFile]::OpenRead($manualPath)
$checks=[Collections.Generic.List[string]]::new()
try {
    if ($manual.Entries.Count -ne $package.Files.Count+$package.LoaderFiles.Count+2) { throw 'Unexpected manual ZIP entry count.' }
    $seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($entry in $manual.Entries) {
        if (!$seen.Add($entry.FullName) -or $entry.FullName -match '(^|/)\.\.?(/|$)|:|\\|^/|\.zip$|\.7z$|Il2CppAssemblies/|Assembly-CSharp|GameAssembly\.dll|PhotoJournal/|Codex-Notebook/|BPVR_notes\.png|\.log$|MoreResourceful|KeepDevObjects|DevHudSwitch|Trailer|Contact') { throw "Nonpublic or unsafe archive path: $($entry.FullName)" }
        if ($entry.FullName -match '\.(txt|md|cfg|json|ps1|cs|cmd)$' -and $entry.Length -lt 200000) {
            $reader=[IO.StreamReader]::new($entry.Open())
            try { $text=$reader.ReadToEnd() } finally { $reader.Dispose() }
            if ($text -match '(?i)C:\\Users\\BLACK|/home/[^\s]+|/Users/[^\s]+') { throw "Private path in public file: $($entry.FullName)" }
        }
    }
    foreach ($file in $package.Files) {
        $a=$payload.GetEntry($file.Path); $b=$manual.GetEntry($file.Path)
        if (!$a -or !$b -or $a.Length -ne $file.Size -or $b.Length -ne $file.Size -or (Entry-Hash $a) -ne $file.Sha256 -or (Entry-Hash $b) -ne $file.Sha256) { throw "Payload/manual mismatch: $($file.Path)" }
    }
    $checks.Add("Verified all $($package.Files.Count) shared installer/manual payload files by size and SHA-256.")
    foreach ($file in $package.LoaderFiles) {
        $entry=$manual.GetEntry($file.Path)
        if (!$entry -or $entry.Length -ne $file.Size -or (Entry-Hash $entry) -ne $file.Sha256) { throw "Official loader mismatch: $($file.Path)" }
    }
    $checks.Add("Verified $($package.LoaderFiles.Count) clean official MelonLoader files, including root version.dll and licenses.")
    foreach ($record in $provenance | Where-Object Origin -eq 'Claude release ZIP') {
        $entry=$source.GetEntry('BluePrinceVR/'+$record.Path)
        if (!$entry -or (Entry-Hash $entry) -ne $record.Sha256) { throw "Claude source mismatch: $($record.Path)" }
    }
    $checks.Add('Every Claude-derived mod, asset and keyboard calibration file matches the source ZIP exactly.')
    $reader=[IO.StreamReader]::new($manual.GetEntry('UserData/MelonPreferences.cfg').Open())
    try { $preferences=$reader.ReadToEnd() } finally { $reader.Dispose() }
    if ($preferences -notmatch '(?m)^SkipIntro = false\r?$' -or $preferences -notmatch '(?m)^ActionMoveX = "Move Horizontal"\r?$' -or $preferences -match 'DevHudSwitch|Trailer') { throw 'Public fresh-install preferences are incorrect.' }
    $checks.Add('Intro restored; required movement mappings included; developer/Trailer settings excluded.')
    foreach ($name in @('version.dll','openvr_api.dll','OPENVR-LICENSE.txt','Mods/BluePrinceVR-Codex/Whisper/ggml-small.en.bin','UserData/ClaudeFixes-keyboard-shared.txt','MelonLoader/Documentation/LICENSE.md','MelonLoader/Documentation/NOTICE.txt')) { if (!$manual.GetEntry($name)) { throw "Missing clean-install requirement: $name" } }
    $checks.Add('Manual ZIP contains no nested archives, game assembly caches, personal notebook/capture files, or excluded mods.')
} finally { $source.Dispose(); $payload.Dispose(); $manual.Dispose() }
$reader=[IO.BinaryReader]::new([IO.File]::OpenRead($exePath))
try {
    if ($reader.ReadUInt16() -ne 0x5a4d) { throw 'Installer is not a Windows PE.' }
    $reader.BaseStream.Position=0x3c; $offset=$reader.ReadInt32(); $reader.BaseStream.Position=$offset
    if ($reader.ReadUInt32() -ne 0x4550 -or $reader.ReadUInt16() -ne 0x8664) { throw 'Installer is not x64 Windows PE.' }
} finally { $reader.Dispose() }
$testReport=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'Installer-tests.txt')
if ($testReport -match '^FAIL:' -or $testReport[-1] -notlike 'All tests passed.*') { throw 'Installer self-test report is missing or failed.' }
$testCount=@($testReport | Where-Object { $_ -like 'PASS:*' }).Count
$checks.Add("Passed $testCount installer checks using synthetic game files, with no live game changes or runtime installs.")
$checks.Add('Installer preview reviewed separately. No headset/gameplay, antivirus certification, or site approval is claimed.')
$sums=@()
foreach ($path in @($exePath,$manualPath)) {
    $file=Get-Item -LiteralPath $path
    if ($file.Length -ge 2GB) { throw 'File exceeds the GitHub release asset limit.' }
    $hash=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
    $sums += "$hash  $($file.Name)"
    $checks.Add("$($file.Name): $($file.Length) bytes; SHA-256 $hash")
}
$sums | Set-Content -LiteralPath (Join-Path $release 'SHA256SUMS.txt') -Encoding ascii
@('# Verification Report - October 10, 2026','',"Source: $($package.SourceArchive)","Source SHA-256: $($package.SourceSha256)",'') + $checks | Set-Content -LiteralPath (Join-Path $release 'VERIFICATION.md') -Encoding utf8
$checks
