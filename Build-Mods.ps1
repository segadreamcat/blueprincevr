param([Parameter(Mandatory=$true)][string]$GameDir)
$ErrorActionPreference = 'Stop'
$GameDir = (Resolve-Path -LiteralPath $GameDir).Path
$interop = Join-Path $GameDir 'MelonLoader\Il2CppAssemblies'
$loader = Join-Path $GameDir 'MelonLoader\net6'
foreach ($path in @($interop, $loader)) {
    if (!(Test-Path -LiteralPath $path -PathType Container)) {
        throw 'Install the public mod and launch your own game once so MelonLoader generates the reference assemblies.'
    }
}
$out = Join-Path $PSScriptRoot 'artifacts'
$core = Join-Path $PSScriptRoot 'source\BluePrinceVR\src\BluePrinceVR.csproj'
& dotnet build $core -c Release "-p:GameDir=$GameDir" -o "$out\BluePrinceVR" --configfile "$PSScriptRoot\source\BluePrinceVR\NuGet.Config"
if ($LASTEXITCODE -ne 0) { throw 'Core mod build failed.' }
# Companion projects resolve references locally, without installing any output.
$refs = Join-Path $PSScriptRoot 'source\BluePrinceVR.ClaudeFixes\refs'
New-Item -ItemType Directory -Path $refs -Force | Out-Null
foreach ($folder in @($interop, $loader)) {
    Get-ChildItem -LiteralPath $folder -Filter '*.dll' -File | Copy-Item -Destination $refs -Force
}
Copy-Item -LiteralPath "$out\BluePrinceVR\BluePrinceVR.dll" -Destination $refs -Force
& dotnet build "$PSScriptRoot\source\BluePrinceVR.ClaudeFixes\ClaudeFixes.csproj" -c Release -o "$out\BluePrinceVR.ClaudeFixes" --configfile "$PSScriptRoot\source\BluePrinceVR.ClaudeFixes\NuGet.config"
if ($LASTEXITCODE -ne 0) { throw 'ClaudeFixes build failed.' }
foreach ($name in @('BluePrinceVR.Fountain','BluePrinceVR.JournalArt','BluePrinceVR.RecorderPolish')) {
    & dotnet build "$PSScriptRoot\source\$name\$name.csproj" -c Release "-p:GameDir=$GameDir" -o "$out\$name" --configfile "$PSScriptRoot\source\BluePrinceVR\NuGet.Config"
    if ($LASTEXITCODE -ne 0) { throw "Build failed: $name" }
}
Write-Output 'Build complete in artifacts. Nothing was installed into your game.'
Write-Output 'Generated refs contain local game assemblies: do not upload them.'
