param(
    [string] $Configuration = "Release"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$OutputEncoding = [System.Text.UTF8Encoding]::new()
[Console]::OutputEncoding = $OutputEncoding

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$appRoot = Resolve-Path (Join-Path $scriptRoot "..")
$projectPath = Join-Path $appRoot "CBreaseWebApp1.csproj"
$publishRoot = Join-Path $appRoot "publish-output"
$baseHref = "/cbreasefield/"

function Assert-PathUnderRoot {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path,

        [Parameter(Mandatory = $true)]
        [string] $Root
    )

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $fullRoot = [System.IO.Path]::GetFullPath($Root)

    if (-not $fullRoot.EndsWith([System.IO.Path]::DirectorySeparatorChar)) {
        $fullRoot = $fullRoot + [System.IO.Path]::DirectorySeparatorChar
    }

    if (-not $fullPath.StartsWith($fullRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to clean path outside app root: $fullPath"
    }
}

function Get-GeneratedAssemblyValue {
    param(
        [Parameter(Mandatory = $true)]
        [string] $FilePath,

        [Parameter(Mandatory = $true)]
        [string] $Pattern
    )

    if (-not (Test-Path -LiteralPath $FilePath)) {
        return $null
    }

    $content = Get-Content -LiteralPath $FilePath -Raw
    $match = [regex]::Match($content, $Pattern)
    if ($match.Success) {
        return $match.Groups[1].Value
    }

    return $null
}

function Format-BuildDisplay {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Version,

        [Parameter(Mandatory = $true)]
        [string] $UtcTimestamp
    )

    try {
        $timestamp = [System.DateTimeOffset]::Parse(
            $UtcTimestamp,
            [System.Globalization.CultureInfo]::InvariantCulture,
            [System.Globalization.DateTimeStyles]::AssumeUniversal)

        $localTimestamp = $timestamp.ToLocalTime().ToString(
            "MMM d h:mm tt",
            [System.Globalization.CultureInfo]::GetCultureInfo("en-US"))

        $separator = [char]0x2022
        return "v$Version $separator $localTimestamp"
    }
    catch {
        $separator = [char]0x2022
        return "v$Version $separator $UtcTimestamp"
    }
}

if (-not (Test-Path -LiteralPath $projectPath)) {
    throw "Project file not found: $projectPath"
}

Assert-PathUnderRoot -Path $publishRoot -Root $appRoot

Write-Host "Cleaning publish output..."
if (Test-Path -LiteralPath $publishRoot) {
    Remove-Item -LiteralPath $publishRoot -Recurse -Force
}

Write-Host "Publishing CBreaseWebApp1 ($Configuration)..."
dotnet publish $projectPath -c $Configuration -o $publishRoot

if (-not (Test-Path -LiteralPath $publishRoot)) {
    throw "Publish output folder was not created: $publishRoot"
}

$indexFile = Get-ChildItem -LiteralPath $publishRoot -Recurse -Filter "index.html" |
    Where-Object { $_.FullName -notmatch '\\obj\\|\\bin\\' } |
    Select-Object -First 1

if ($null -eq $indexFile) {
    throw "Could not find published index.html under: $publishRoot"
}

$wwwroot = $indexFile.Directory.FullName
$frameworkFolder = Join-Path $wwwroot "_framework"

if (-not (Test-Path -LiteralPath $frameworkFolder)) {
    throw "_framework folder not found under: $wwwroot"
}

$indexContent = Get-Content -LiteralPath $indexFile.FullName -Raw
$indexContent = $indexContent -replace '<base href="/" ?/?>', "<base href=`"$baseHref`" />"

$blazorFile = Get-ChildItem -LiteralPath $frameworkFolder -Filter "blazor.webassembly*.js" |
    Where-Object { $_.Name -ne "blazor.webassembly.js" } |
    Select-Object -First 1

if ($null -eq $blazorFile) {
    throw "Could not find hashed blazor.webassembly file in: $frameworkFolder"
}

$actualBlazor = "_framework/" + $blazorFile.Name
$indexContent = $indexContent -replace '_framework/blazor\.webassembly#\[\.\{fingerprint\}\]\.js', $actualBlazor
$indexContent = $indexContent -replace '_framework/blazor\.webassembly\.js', $actualBlazor
Set-Content -LiteralPath $indexFile.FullName -Value $indexContent

$breaseCoreWasm = Get-ChildItem -LiteralPath $frameworkFolder -Filter "Brease.Core*.wasm" |
    Sort-Object Name |
    Select-Object -First 1

$serviceWorkerAssets = Get-ChildItem -LiteralPath $wwwroot -Filter "service-worker-assets.js" |
    Select-Object -First 1

$assemblyInfo = Join-Path $appRoot "obj\$Configuration\net10.0\CBreaseWebApp1.AssemblyInfo.cs"
$buildInfo = Join-Path $appRoot "obj\$Configuration\net10.0\GeneratedBuildInfo.g.cs"
$informationalVersion = Get-GeneratedAssemblyValue -FilePath $assemblyInfo -Pattern 'AssemblyInformationalVersionAttribute\("([^"]+)"\)'
$assemblyVersion = Get-GeneratedAssemblyValue -FilePath $assemblyInfo -Pattern 'AssemblyVersionAttribute\("([^"]+)"\)'
$buildTimestampUtc = Get-GeneratedAssemblyValue -FilePath $buildInfo -Pattern 'BuildTimestampUtc", "([^"]+)"'

if ([string]::IsNullOrWhiteSpace($informationalVersion)) {
    $informationalVersion = $assemblyVersion
}

$displayString = $null
if (-not [string]::IsNullOrWhiteSpace($informationalVersion) -and
    -not [string]::IsNullOrWhiteSpace($buildTimestampUtc)) {
    $displayString = Format-BuildDisplay -Version $informationalVersion -UtcTimestamp $buildTimestampUtc
}

Write-Host ""
Write-Host "Publish complete."
Write-Host "Publish folder: $wwwroot"
Write-Host "Brease.Core WASM: $(if ($breaseCoreWasm) { $breaseCoreWasm.FullName } else { 'not found' })"
Write-Host "Service worker assets: $(if ($serviceWorkerAssets) { $serviceWorkerAssets.FullName } else { 'not found' })"
Write-Host "App version/build display: $(if ($displayString) { $displayString } else { 'not available' })"
