$project = ".\CBreaseWebApp1.csproj"
$publishRoot = ".\publish-output"
$baseHref = "/cbreasefield/"

Write-Host "Cleaning old publish output..."
if (Test-Path $publishRoot) {
    Remove-Item $publishRoot -Recurse -Force
}

Write-Host "Publishing app..."
dotnet publish $project -c Release -o $publishRoot

if (!(Test-Path $publishRoot)) {
    Write-Host "Publish output folder not found."
    exit 1
}

Write-Host "Locating published index.html..."
$indexFile = Get-ChildItem $publishRoot -Recurse -Filter "index.html" |
    Where-Object { $_.FullName -notmatch '\\obj\\|\\bin\\' } |
    Select-Object -First 1

if ($null -eq $indexFile) {
    Write-Host "Could not find published index.html."
    exit 1
}

$wwwroot = $indexFile.DirectoryName
Write-Host "Using published folder: $wwwroot"

$content = Get-Content $indexFile.FullName -Raw

Write-Host "Fixing base href..."
$content = $content -replace '<base href="/" ?/?>', "<base href=`"$baseHref`" />"

Write-Host "Looking for hashed blazor.webassembly file..."
$frameworkFolder = Join-Path $wwwroot "_framework"

if (!(Test-Path $frameworkFolder)) {
    Write-Host "_framework folder not found under: $wwwroot"
    exit 1
}

$blazorFile = Get-ChildItem $frameworkFolder -Filter "blazor.webassembly*.js" |
    Where-Object { $_.Name -ne "blazor.webassembly.js" } |
    Select-Object -First 1

if ($null -eq $blazorFile) {
    Write-Host "Could not find hashed blazor.webassembly file in _framework."
    exit 1
}

$actualBlazor = "_framework/" + $blazorFile.Name
Write-Host "Using script file: $actualBlazor"

$content = $content -replace '_framework/blazor\.webassembly#\[\.\{fingerprint\}\]\.js', $actualBlazor
$content = $content -replace '_framework/blazor\.webassembly\.js', $actualBlazor

Set-Content $indexFile.FullName $content

Write-Host ""
Write-Host "Done."
Write-Host "Zip the CONTENTS of this folder:"
Write-Host $wwwroot