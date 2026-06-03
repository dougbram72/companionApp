param(
  [string]$Runtime = "win-x64",
  [string]$Configuration = "Release",
  [string]$OutputPath,
  [switch]$FrameworkDependent,
  [switch]$NoSingleFile
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$projectPath = Join-Path $repoRoot "src\KeypadCompanion\KeypadCompanion.csproj"

if (-not $OutputPath) {
  $OutputPath = Join-Path $repoRoot ("artifacts\KeypadCompanion\" + $Runtime)
}

$publishSingleFile = -not $NoSingleFile
$selfContained = -not $FrameworkDependent

New-Item -ItemType Directory -Force -Path $OutputPath | Out-Null

$publishArgs = @(
  "publish",
  $projectPath,
  "-c", $Configuration,
  "-r", $Runtime,
  "--output", $OutputPath,
  "--self-contained", $selfContained.ToString().ToLowerInvariant(),
  "/p:PublishSingleFile=$($publishSingleFile.ToString().ToLowerInvariant())",
  "/p:IncludeNativeLibrariesForSelfExtract=$($publishSingleFile.ToString().ToLowerInvariant())",
  "/p:DebugSymbols=false",
  "/p:DebugType=None",
  "--nologo"
)

& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) {
  throw "Companion app publish failed."
}

$exePath = Join-Path $OutputPath "KeypadCompanion.exe"
if (-not (Test-Path $exePath)) {
  throw "Publish completed, but '$exePath' was not created."
}

# The app runs without the native package PDBs, so drop them from release output.
Get-ChildItem -Path $OutputPath -Filter "*.pdb" -File -ErrorAction SilentlyContinue |
  Remove-Item -Force

Write-Host "Companion executable created: $exePath"
