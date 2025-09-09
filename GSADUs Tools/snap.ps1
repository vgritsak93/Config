# snap.ps1 — right-click friendly snapshot logger with pause
# ToolVersion: 2.7  (PS 5.1 + PS 7 compatible)

# Relaunch without profiles when invoked from Explorer
if (-not $env:SNAP_NOPROFILE) {
  $exe = ($PSVersionTable.PSEdition -eq 'Core') ? 'pwsh' : 'powershell'
  $snapArgs = @('-NoProfile','-ExecutionPolicy','Bypass','-File',"`$PSCommandPath`"")
  $env:SNAP_NOPROFILE = '1'
  Start-Process -FilePath $exe -ArgumentList $snapArgs -Wait
  exit
}




$ErrorActionPreference = 'Stop'
$script:LogPath = $null

function Wait-IfNeeded {
  [void](Read-Host 'Press Enter to close')
}

try {
  # Resolve locations
  $ScriptDir = Split-Path -LiteralPath $MyInvocation.MyCommand.Path -Parent
  if (-not $SolutionRoot) { $SolutionRoot = $ScriptDir }

  if (-not $SlnName) {
    $slns = Get-ChildItem -LiteralPath $SolutionRoot -Filter *.sln -File | Select-Object -Expand FullName
    if (-not $slns) { throw "No .sln found in $SolutionRoot. Put snap.ps1 next to your .sln." }
    $SlnPath = $slns[0]
    $SlnName = Split-Path $SlnPath -Leaf
  } else {
    $SlnPath = Join-Path $SolutionRoot $SlnName
    if (-not (Test-Path -LiteralPath $SlnPath)) { throw "Cannot find $SlnPath" }
  }

  $BaseName        = [IO.Path]::GetFileNameWithoutExtension($SlnName)
  $script:LogPath  = Join-Path $SolutionRoot "$BaseName.solution_snapshot.log"

  # Start transcript early so errors are captured
  try { Stop-Transcript | Out-Null } catch {}
  Start-Transcript -Path $script:LogPath -Append | Out-Null

  function Write-Section([string]$title) { "`n=== $title ===" }

  function Invoke-CommandLine {
    param([string]$Command, [int]$Expected = 0)
    $sw = [Diagnostics.Stopwatch]::StartNew()
    try { Invoke-Expression $Command 2>&1 | ForEach-Object { $_ } ; $code = $LASTEXITCODE } catch { $_ ; $code = 1 } finally { $sw.Stop() }
    "ExitCode: $code"
    if ($code -ne $Expected) { throw "Command failed: $Command" }
  }

  function Get-ProjectsFromSln {
    (dotnet sln $SlnPath list | Select-Object -Skip 2) |
      ForEach-Object { $_.Trim() } |
      Where-Object { $_ } |
      ForEach-Object { Join-Path $SolutionRoot $_ } |
      Where-Object { Test-Path $_ }
  }

  function Get-MSBuildItems {
    param([string]$CsprojPath)
    $projDir = Split-Path -LiteralPath $CsprojPath -Parent
    $targets = @"
<Project>
  <Target Name='DumpItems'>
    <WriteLinesToFile File='obj\Items-Compile.txt' Lines='@(Compile)' Overwrite='true' />
    <WriteLinesToFile File='obj\Items-Page.txt'    Lines='@(Page)'    Overwrite='true' />
    <WriteLinesToFile File='obj\Items-None.txt'    Lines='@(None)'    Overwrite='true' />
    <WriteLinesToFile File='obj\Items-EmbeddedResource.txt' Lines='@(EmbeddedResource)' Overwrite='true' />
    <WriteLinesToFile File='obj\Items-AdditionalFiles.txt' Lines='@(AdditionalFiles)' Overwrite='true' />
  </Target>
</Project>
"@
    $tempTargets = Join-Path $projDir "DumpItems.targets"
    Set-Content -Path $tempTargets -Value $targets -Encoding UTF8
    dotnet msbuild $CsprojPath -nologo -t:DumpItems | Out-Null
    Remove-Item $tempTargets -Force

    $obj = Join-Path $projDir "obj"
    $files = "Items-Compile.txt","Items-Page.txt","Items-None.txt","Items-EmbeddedResource.txt","Items-AdditionalFiles.txt" |
             ForEach-Object { Join-Path $obj $_ }

    $items = @()
    foreach ($f in $files) {
      if (Test-Path $f) {
        $items += Get-Content $f | Where-Object { $_ -and ($_ -notmatch '^\s*$') }
      }
    }
    if ($items) { $items = $items | Sort-Object -Unique }
    else { $items = @() }

    # Normalize to absolute paths
    $abs = @()
    foreach ($p in $items) {
      if ([IO.Path]::IsPathRooted($p)) { $abs += [IO.Path]::GetFullPath($p) }
      else { $abs += [IO.Path]::GetFullPath((Join-Path $projDir $p)) }
    }
    $abs | Sort-Object -Unique
  }

  function Get-Inventory {
    param([string]$CsprojPath)
    $projDir  = Split-Path -LiteralPath $CsprojPath -Parent
    $declared = @(Get-MSBuildItems $CsprojPath)

    $disk = @(Get-ChildItem -LiteralPath $projDir -Recurse -File -Include *.cs,*.xaml,*.resx,*.md,*.json,*.xml |
      Where-Object {
        $_.FullName -notmatch '\\(bin|obj|\.git|\.vs)\\' -and
        $_.Name -notlike '*.g.cs' -and
        $_.Name -notlike '*.g.i.cs' -and
        $_.Name -notlike '*.AssemblyAttributes.cs'
      } | Select-Object -Expand FullName)

    # Case-insensitive diff using Compare-Object
    $unref = Compare-Object -ReferenceObject $declared -DifferenceObject $disk -PassThru |
             Where-Object { $_.SideIndicator -eq '=>' } | Sort-Object -Unique
    $missing = Compare-Object -ReferenceObject $declared -DifferenceObject $disk -PassThru |
               Where-Object { $_.SideIndicator -eq '<=' } | Sort-Object -Unique

    [pscustomobject]@{
      Project              = $CsprojPath
      UNREFERENCED_ON_DISK = $unref
      MISSING_ON_DISK      = $missing
    }
  }

  # Header
  "TimestampUTC: $([DateTime]::UtcNow.ToString('MM/dd/yyyy HH:mm:ss'))"
  "Solution: $SlnPath"
  "Root: $SolutionRoot"
  "ToolVersion: 2.7"

  # Environment
  Write-Section "ENVIRONMENT"
  "OS: $([Environment]::OSVersion.VersionString)"
  "PowerShell: $($PSVersionTable.PSVersion)"
  "> dotnet --list-sdks"; Invoke-CommandLine "dotnet --list-sdks"
  "> dotnet --info";     Invoke-CommandLine "dotnet --info"

  # Solution map
  Write-Section "SOLUTION MAP"
  "> dotnet sln `"$SlnPath`" list"
  Invoke-CommandLine "dotnet sln `"$SlnPath`" list"

  # Raw sln
  Write-Section "SLN CONTENT (RAW)"
  Get-Content -LiteralPath $SlnPath

  # Projects
  Write-Section "PROJECTS"
  $projects = Get-ProjectsFromSln
  foreach ($proj in $projects) {
    "Project: $proj"
    try {
      $xml = [xml](Get-Content -LiteralPath $proj)
      $tfmNodes = $xml.Project.PropertyGroup.TargetFrameworks, $xml.Project.PropertyGroup.TargetFramework
      $tfms = ($tfmNodes | Where-Object { $_ } | ForEach-Object { $_.InnerText }) -join ";"
      "  TargetFrameworks: $tfms"
      "  OutputType: $($xml.Project.PropertyGroup.OutputType | ForEach-Object { $_.InnerText } | Select-Object -First 1)"
      "  LangVersion: $($xml.Project.PropertyGroup.LangVersion | ForEach-Object { $_.InnerText } | Select-Object -First 1)"
    } catch { "  <project file parse skipped>" }

    "> dotnet restore $proj --nologo --verbosity minimal"
  Invoke-CommandLine "dotnet restore `"$proj`" --nologo --verbosity minimal"

    "> dotnet list $proj package"
  Invoke-CommandLine "dotnet list `"$proj`" package"
  }

  # Inventory
  Write-Section "PROJECT CONTENT INVENTORY"
  foreach ($proj in $projects) {
    $inv = Get-Inventory $proj
    "Project: $($inv.Project)"
    foreach ($p in $inv.UNREFERENCED_ON_DISK) { "  UNREFERENCED_ON_DISK: $p" }
    foreach ($p in $inv.MISSING_ON_DISK)      { "  MISSING_ON_DISK: $p" }
  }

  # References
  Write-Section "REFERENCE RESOLUTION"
  foreach ($proj in $projects) {
  "> dotnet msbuild $proj -t:ResolveReferences -nologo -clp:NoSummary -v:m"
  Invoke-CommandLine "dotnet msbuild `"$proj`" -t:ResolveReferences -nologo -clp:NoSummary -v:m"
  }

  # Build summary
  Write-Section "BUILD SUMMARY"
  "> dotnet build $SlnPath -nologo -clp:Summary -v:m"
  Invoke-CommandLine "dotnet build `"$SlnPath`" -nologo -clp:Summary -v:m"

} catch {
  "ERROR: $($_.Exception.Message)"
}
finally {
  try { Stop-Transcript | Out-Null } catch {}
  # Headless: do not open Notepad, do not prompt
  Wait-IfNeeded
}
