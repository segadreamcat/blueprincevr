param(
    [Parameter(Mandatory=$true)][string]$SourceArchive,
    [string]$ExpectedSourceHash = 'f89ba64e9eb9274e841867241647dbed83939f5933d0a0c2715caadd39aa56cd'
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$build = Join-Path $PSScriptRoot 'build'
$release = Join-Path $PSScriptRoot 'release'
New-Item -ItemType Directory -Path $build,$release -Force | Out-Null
$dependencies = Get-Content -Raw -LiteralPath (Join-Path $build 'dependencies.json') | ConvertFrom-Json
$actual = (Get-FileHash -LiteralPath $SourceArchive -Algorithm SHA256).Hash.ToLowerInvariant()
if ($actual -ne $ExpectedSourceHash) { throw 'Source archive changed; inspect and pin the new release before building.' }
$loaderPath = Join-Path $build 'MelonLoader.x64.zip'
if ((Get-FileHash -LiteralPath $loaderPath -Algorithm SHA256).Hash.ToLowerInvariant() -ne $dependencies.Loader.Hash) { throw 'Official loader checksum mismatch.' }
$openVrHashes = @{
    'openvr_api.dll'='bab8ac6ef64e68a9ca53315b0014d131088584b2efdfa6db511d67ec03cfcb4a'
    'OPENVR-LICENSE.txt'='e0f0ba2fda3fc45f4ca889b78c8c507818a11d11aaf4411eb10eb2cccb46557c'
}
foreach ($name in $openVrHashes.Keys) {
    if ((Get-FileHash -LiteralPath (Join-Path $build "openvr\$name") -Algorithm SHA256).Hash.ToLowerInvariant() -ne $openVrHashes[$name]) { throw "OpenVR support checksum mismatch: $name" }
}
function Add-ArchiveFile($archive, [string]$name, [string]$origin) {
    $entry = $archive.GetEntry('BluePrinceVR/' + $name)
    if ($null -eq $entry) { throw "Missing release file: $name" }
    $script:files[$name] = @{ Entry=$entry; Origin=$origin }
}
function Write-ZipFile($destination, [string]$name, $spec) {
    if ($spec.ContainsKey('Entry')) { $input=$spec.Entry.Open(); $size=$spec.Entry.Length }
    else { $input=[IO.File]::OpenRead($spec.File); $size=$input.Length }
    $sha=[Security.Cryptography.SHA256]::Create()
    try {
        $entry=$destination.CreateEntry($name,[IO.Compression.CompressionLevel]::Optimal)
        $output=$entry.Open()
        try {
            $buffer=New-Object byte[] 131072
            while (($count=$input.Read($buffer,0,$buffer.Length)) -gt 0) {
                $output.Write($buffer,0,$count)
                $null=$sha.TransformBlock($buffer,0,$count,$buffer,0)
            }
            $null=$sha.TransformFinalBlock([byte[]]@(),0,0)
            $hash=[BitConverter]::ToString($sha.Hash).Replace('-','').ToLowerInvariant()
        } finally { $output.Dispose() }
    } finally { $input.Dispose(); $sha.Dispose() }
    return @{ Path=$name; Sha256=$hash; Size=$size }
}
$source=[IO.Compression.ZipFile]::OpenRead($SourceArchive)
$loader=[IO.Compression.ZipFile]::OpenRead($loaderPath)
$payloadPath=Join-Path $build 'payload.zip'
try {
    $script:files=[ordered]@{}
    foreach ($name in @('BluePrinceVR.dll','BluePrinceVR.ClaudeFixes.dll','BluePrinceVR.Fountain.dll','BluePrinceVR.JournalArt.dll','BluePrinceVR.RecorderPolish.dll')) {
        Add-ArchiveFile $source "Mods/$name" 'Claude release ZIP'
    }
    $assets=@('arms.bpvr','Check-OfflineMemo.cmd','CodexVrs.dll','FPS-ARMS-CREDITS.md','fps-arms-rigged.bpvr','hands_fp_cel.glb','MemoCapture.cs','NVIDIA-LICENSE.txt','OfflineMemo.ps1')
    foreach ($name in $assets) { Add-ArchiveFile $source "Mods/BluePrinceVR-Codex/$name" 'Claude release ZIP' }
    foreach ($name in @('camera-choice-flat.png','notebook-choice-flat.png','camera-choice.png','notebook-choice.png')) { Add-ArchiveFile $source "Mods/BluePrinceVR-Codex/JournalArt/$name" 'Claude release ZIP' }
    foreach ($name in @('ggml-base.dll','ggml-cpu.dll','ggml-small.en.bin','ggml.dll','whisper-cli.exe','WHISPER-CPP-LICENSE.txt','WHISPER-MODEL-LICENSE.txt','whisper.dll')) { Add-ArchiveFile $source "Mods/BluePrinceVR-Codex/Whisper/$name" 'Claude release ZIP' }
    foreach ($name in @('BPVR_VR.png','BPVR_smoke.png','BPVR_puff.png','BPVR_select.png','arms.bpvr','Codex-keyboard-Laboratory-Computer.json','Codex-keyboard-Office-Computer.json','Codex-keyboard-Standalone_Rooms-Computer.json','Codex-keyboard-Security-Click_Trigger.json','Codex-keyboard-points.json','ClaudeFixes-keyboard-Office-Computer.txt','ClaudeFixes-keyboard-shared.txt','BluePrinceVR-RecorderPolish.cfg')) { Add-ArchiveFile $source "UserData/$name" 'Claude release ZIP' }
    foreach ($name in $openVrHashes.Keys) { $files[$name]=@{ File=(Join-Path $build "openvr\$name"); Origin='Pinned OpenVR support runtime' } }
    foreach ($name in @('CONTROLS.txt','FIRST-RUN.txt','THIRD-PARTY-NOTICES.md')) { $files["BluePrinceVR-Installer/$name"]=@{ File=(Join-Path $PSScriptRoot $name); Origin='Release documentation' } }
    # Import only the VR mod's scalar settings, never developer settings.
    $reader=[IO.StreamReader]::new($source.GetEntry('BluePrinceVR/UserData/MelonPreferences.cfg').Open())
    try { $preferences=$reader.ReadToEnd() } finally { $reader.Dispose() }
    $defaults=[ordered]@{}; $section=''
    foreach ($line in ($preferences -split '\r?\n')) {
        if ($line -match '^\[([A-Za-z0-9_]+)\]\s*$') {
            $section=$Matches[1]
            if ($section -in @('BluePrinceVR_Codex','BluePrinceVR_Fountain','ClaudeFixes')) { $defaults[$section]=[ordered]@{} }
        } elseif ($defaults.Contains($section) -and $line -notmatch '^\s*(#|$)') {
            if ($line -notmatch '^([A-Za-z0-9_]+)\s*=\s*(.+?)\s*$') { throw "Unsupported preference: $line" }
            $key=$Matches[1]; $value=$Matches[2]
            if ($value -notmatch '^(true|false|[-+0-9.eE]+|"[^"\r\n]*")$' -or $value -match '[A-Za-z]:\\|/home/|/Users/') { throw "Nonportable scalar preference: $key" }
            $defaults[$section][$key]=$value
        }
    }
    if ($defaults.Count -ne 3 -or $defaults['BluePrinceVR_Codex']['ActionMoveX'] -ne '"Move Horizontal"' -or $defaults['BluePrinceVR_Codex']['SkipIntro'] -ne 'false') { throw 'Required controller mappings or restored intro missing.' }
    $fresh=@()
    foreach ($section in $defaults.Keys) {
        $fresh += "[$section]"
        foreach ($key in $defaults[$section].Keys) { $fresh += "$key = $($defaults[$section][$key])" }
        $fresh += ''
    }
    $freshPath=Join-Path $build 'fresh-preferences.cfg'
    [IO.File]::WriteAllText($freshPath,($fresh -join "`r`n"),[Text.UTF8Encoding]::new($false))
    # Generic release comfort defaults, not the author's measured arm span.
    $comfortPath=Join-Path $build 'fresh-comfort.json'
    [IO.File]::WriteAllText($comfortPath,(@{ LeftHanded=$false; Snap=$false; SnapAngle=60; Span=1.8; Forearms=$true } | ConvertTo-Json -Compress),[Text.UTF8Encoding]::new($false))
    $files['UserData/Codex-comfort.json']=@{ File=$comfortPath; Origin='Generic release comfort defaults' }
    $manifest=@(); $provenance=@()
    $stream=[IO.File]::Open($payloadPath,[IO.FileMode]::Create)
    $zip=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Create,$false)
    try {
        foreach ($pair in $files.GetEnumerator()) {
            $record=Write-ZipFile $zip $pair.Key $pair.Value
            $manifest += $record
            $provenance += @{ Path=$record.Path; Sha256=$record.Sha256; Origin=$pair.Value.Origin }
        }
    } finally { $zip.Dispose(); $stream.Dispose() }
    $loaderManifest=@()
    $manualPath=Join-Path $release 'BluePrinceVR-Windows-Manual-2026-10-10.zip'
    $stream=[IO.File]::Open($manualPath,[IO.FileMode]::Create)
    $manual=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Create,$false)
    try {
        foreach ($pair in $files.GetEnumerator()) { $null=Write-ZipFile $manual $pair.Key $pair.Value }
        foreach ($entry in $loader.Entries | Where-Object {$_.Name}) {
            $name=$entry.FullName
            if ($name -notmatch '^(MelonLoader/|version\.dll$|dobby\.dll$)' -or $name -match '(^|/)\.\.?(/|$)|:|\\|\.zip$|Il2CppAssemblies|Latest\.log') { throw "Unexpected official loader entry: $name" }
            $loaderManifest += Write-ZipFile $manual $name @{Entry=$entry}
        }
        $null=Write-ZipFile $manual 'UserData/MelonPreferences.cfg' @{File=$freshPath}
        $null=Write-ZipFile $manual 'README.txt' @{File=(Join-Path $PSScriptRoot 'MANUAL-INSTALL.txt')}
    } finally { $manual.Dispose(); $stream.Dispose() }
} finally { $source.Dispose(); $loader.Dispose() }
$package=@{
    Build='2026-10-10 Windows release / ClaudeFixes c96 / installer 1.3'
    SourceArchive=[IO.Path]::GetFileName($SourceArchive); SourceSha256=$actual
    PayloadHash=(Get-FileHash -LiteralPath $payloadPath -Algorithm SHA256).Hash.ToLowerInvariant()
    Files=$manifest; LoaderFiles=$loaderManifest; Defaults=$defaults
    Loader=$dependencies.Loader; DotNet=$dependencies.DotNet; VisualCpp=$dependencies.VisualCpp
}
$config=Join-Path $build 'package.json'
$package | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $config -Encoding utf8
$provenance | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $build 'provenance.json') -Encoding utf8
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$exe=Join-Path $release 'BluePrinceVR-Windows-Setup-2026-10-10.exe'
$arguments=@('/nologo','/target:winexe','/platform:x64','/optimize+','/langversion:5',"/out:$exe",'/r:System.dll','/r:System.Core.dll','/r:System.Drawing.dll','/r:System.Windows.Forms.dll','/r:System.Web.Extensions.dll','/r:System.IO.Compression.dll','/r:System.IO.Compression.FileSystem.dll',"/win32manifest:$PSScriptRoot\src\Installer.manifest", "/resource:$build\banner.png,banner.png", "/resource:$payloadPath,payload.zip", "/resource:$loaderPath,loader.zip", "/resource:$config,package.json", "$PSScriptRoot\src\Installer.cs", "$PSScriptRoot\src\SelfTest.cs")
& $compiler @arguments
if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' }
foreach ($name in @('RELEASE-NOTES.md','PUBLISHING.md','THIRD-PARTY-NOTICES.md')) { Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) -Destination (Join-Path $release $name) -Force }
Get-ChildItem -LiteralPath $release -File | Where-Object {$_.Extension -in @('.exe','.zip')} | Select-Object Name,Length
