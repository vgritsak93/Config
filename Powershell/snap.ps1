# Solution snapshot -> <SolutionName>.solution_snapshot.log next to the .sln
# One prompt in, one log out. Overwrites. No ACL tweaks. PowerShell 7/5.1 compatible.

$ErrorActionPreference = 'Stop'
$Dotnet = Join-Path ${env:ProgramFiles} "dotnet\dotnet.exe"

# ---- input ---------------------------------------------------------------
$SolutionPath = Read-Host "Paste FULL path to the .sln file"
$SolutionPath = $SolutionPath.Trim('"')  # removes wrapping quotes if present
# strip wrapping quotes if present
$SolutionPath = $SolutionPath.Trim('"')
if (-not (Test-Path -LiteralPath $SolutionPath -PathType Leaf)) { Write-Host "Not a file: $SolutionPath"; exit 1 }
if ($SolutionPath.ToLower() -notlike '*.sln') { Write-Host "Not a .sln: $SolutionPath"; exit 1 }

$sln     = Get-Item -LiteralPath $SolutionPath
$slnDir  = Split-Path $sln.FullName -Parent
$base    = $sln.BaseName
$logDir  = "C:\Users\Vadim\OneDrive\Documents\PowerShell\Logs"
if (!(Test-Path $logDir)) { New-Item -ItemType Directory -Path $logDir -Force | Out-Null }
$logPath = Join-Path $logDir ($base + ".solution_snapshot.log")

# ---- helpers -------------------------------------------------------------
if (Test-Path $logPath) { Remove-Item $logPath -Force -ErrorAction SilentlyContinue }
function W([string]$s){ $s | Out-File -FilePath $script:logPath -Append -Encoding utf8 }
function S([string]$name){ W ("=== " + $name + " ===") }
function DN([string[]]$ArgsArray){
  W ("> dotnet " + ($ArgsArray -join ' '))
  & $Dotnet @ArgsArray 2>&1 | Out-File $script:logPath -Append -Encoding utf8
  W ("ExitCode: " + $LASTEXITCODE)
}

# ---- header --------------------------------------------------------------
"TimestampUTC: $([DateTime]::UtcNow)" | Out-File $logPath -Encoding utf8
W ("Solution: " + $sln.FullName)
W ("Root: " + $slnDir)
W "ToolVersion: 2.2"

# ---- environment ---------------------------------------------------------
S "ENVIRONMENT"
W ("OS: " + [System.Environment]::OSVersion.VersionString)
W ("PowerShell: " + $PSVersionTable.PSVersion)
DN @("--list-sdks")
DN @("--info")

# ---- solution map + raw sln ----------------------------------------------
S "SOLUTION MAP"
DN @("sln", $sln.FullName, "list")

S "SLN CONTENT (RAW)"
Get-Content -LiteralPath $sln.FullName -Encoding UTF8 | Out-File $logPath -Append -Encoding utf8

# ---- discover projects (safe) --------------------------------------------
S "PROJECTS"
$projExts = @('.csproj','.vbproj','.fsproj')
$projs = Get-ChildItem -LiteralPath $slnDir -Recurse -File -Force -ErrorAction SilentlyContinue |
         Where-Object { $projExts -contains $_.Extension.ToLower() }
if (-not $projs) { W "Projects: <none found>" }

foreach($p in $projs){
  W ("Project: " + $p.FullName)
  try {
    [xml]$x = Get-Content -LiteralPath $p.FullName -Encoding UTF8
    $pg  = @($x.Project.PropertyGroup) | Select-Object -First 1
    $tfm = @($pg.TargetFramework, $pg.TargetFrameworks) -join ';'
    W ("  TargetFrameworks: " + $tfm)
    W ("  OutputType: " + $pg.OutputType)
    W ("  LangVersion: " + $pg.LangVersion)
    ($x.Project.ItemGroup.ProjectReference.Include) | Where-Object { $_ } | ForEach-Object { W ("  ProjectRef: " + $_) }
    ($x.Project.ItemGroup.PackageReference)         | Where-Object { $_ } | ForEach-Object { W ("  PackageRef: " + $_.Include + " " + $_.Version) }
    ($x.Project.ItemGroup.Analyzer.Include)         | Where-Object { $_ } | ForEach-Object { W ("  Analyzer: " + $_) }
  } catch { W ("  XML parse error: " + $_.Exception.Message) }

  DN @("restore", $p.FullName, "--nologo", "--verbosity", "minimal")
  DN @("list", $p.FullName, "package")
}

# ---- inventory: included/missing/unreferenced + hint paths ----------------
S "PROJECT CONTENT INVENTORY"
foreach($p in $projs){
  $projDir = Split-Path $p.FullName -Parent
  W ("Project: " + $p.FullName)
  try {
    [xml]$x = Get-Content -LiteralPath $p.FullName -Encoding UTF8

    $includes = @()
    $includes += $x.Project.ItemGroup.Compile.Include
    $includes += $x.Project.ItemGroup.None.Include
    $includes += $x.Project.ItemGroup.Content.Include
    $includes = $includes | Where-Object { $_ } | Select-Object -Unique

    foreach($inc in $includes){
      $abs = Join-Path $projDir $inc
      if (Test-Path -LiteralPath $abs) { W ("  Included: " + $inc) } else { W ("  MISSING:  " + $inc) }
    }

    $exts = @('*.cs','*.vb','*.fs','*.xaml','*.json','*.xml','*.config','*.targets','*.props')
    $onDisk = foreach($e in $exts){ Get-ChildItem -LiteralPath $projDir -Recurse -File -Force -ErrorAction SilentlyContinue -Filter $e }
    $onDiskRel = $onDisk | ForEach-Object { $_.FullName.Substring($projDir.Length + 1) }
    $notInProject = $onDiskRel | Where-Object { $includes -notcontains $_ }
    $Unref = @()   # <— ADD: collector
    foreach($f in ($notInProject | Sort-Object -Unique)){
      W ("  UNREFERENCED_ON_DISK: " + $f)
      # <— ADD: record for summary/dump
      $abs = Join-Path $projDir $f
      $Unref += [pscustomobject]@{ Project=$p.FullName; RelPath=$f; FullPath=$abs }
    }

    $refs = $x.Project.ItemGroup.Reference | Where-Object { $_ }
    foreach($r in $refs){
      $hint = $r.HintPath
      if ($hint){
        $abs = Join-Path $projDir $hint
        if (Test-Path -LiteralPath $abs) { W ("  AssemblyRef: " + $r.Include + " | HintPath OK") }
        else { W ("  AssemblyRef: " + $r.Include + " | HintPath MISSING: " + $hint) }
      } else { W ("  AssemblyRef: " + $r.Include) }
    }
  } catch { W ("  Inventory error: " + $_.Exception.Message) }
}

# ---- authoritative resolved references -----------------------------------
S "REFERENCE RESOLUTION"
foreach($p in $projs){ DN @("msbuild", $p.FullName, "-t:ResolveReferences", "-nologo", "-clp:NoSummary", "-v:m") }

# ---- build summary (always; concise) -------------------------------------
S "BUILD SUMMARY"
DN @("build", $sln.FullName, "-nologo", "-clp:Summary", "-v:m")

# ---- config surface (safe) -----------------------------------------------
S "CONFIG SURFACE"
$cfgExact = @('app.config','web.config','Directory.Build.props','Directory.Build.targets','NuGet.Config')
Get-ChildItem -LiteralPath $slnDir -Recurse -File -Force -ErrorAction SilentlyContinue |
  Where-Object { $cfgExact -contains $_.Name -or $_.Name -like 'appsettings*.json' } |
  ForEach-Object { W ("ConfigFile: " + $_.FullName) }

# ---- folder tree ---------------------------------------------------------
S "FOLDER TREE"
$excludeNames = @('bin','obj','.vs','.git','node_modules','packages')
function Skip([string]$p){ foreach($e in $excludeNames){ if ($p -like "*\$e*") { return $true } } return $false }
$queue = New-Object System.Collections.Generic.Queue[object]
$queue.Enqueue([pscustomobject]@{Path=$slnDir})
while($queue.Count -gt 0){
  $curr = $queue.Dequeue()
  if (Skip $curr.Path) { continue }
  $item = Get-Item -LiteralPath $curr.Path -Force
  if ($item.PSIsContainer){
    W ("[D] " + $item.FullName)
    Get-ChildItem -LiteralPath $item.FullName -Force -ErrorAction SilentlyContinue |
      ForEach-Object { $queue.Enqueue([pscustomobject]@{Path=$_.FullName}) }
  } else {
    W ("[F] " + $item.FullName + " | " + $item.Length + " bytes | " + ($item.LastWriteTimeUtc.ToString('u')))
  }
}

# ---- unreferenced files summary ------------------------------------------
S "UNREFERENCED FILES (SUMMARY)"
if (-not $Unref -or $Unref.Count -eq 0) {
  W "None"
} else {
  $Unref | Sort-Object Project, RelPath | ForEach-Object {
    W ("Project: " + $_.Project)
    W ("  " + $_.RelPath)
  }
}

# ---- unreferenced files content (text-only) -------------------------------
S "UNREFERENCED FILES (CONTENT)"
$txtExt = @('.cs','.xaml','.json','.xml','.config','.props','.targets','.csproj','.sln','.md','.txt','.yml','.yaml','.ini','.cmd','.ps1')
if (-not $Unref -or $Unref.Count -eq 0) {
  W "None"
} else {
  $Unref |
    Where-Object { $txtExt -contains ([System.IO.Path]::GetExtension($_.FullPath).ToLower()) } |
    Sort-Object FullPath |
    ForEach-Object {
      $p = $_.FullPath
      W ("--- UNREFERENCED FILE: " + $p + " ---")
      try { Get-Content -LiteralPath $p -Encoding UTF8 | Out-File $logPath -Append -Encoding utf8 }
      catch { W ("Read error: " + $_.Exception.Message) }
      W ("--- END UNREFERENCED FILE: " + $p + " ---")
    }
}

# ---- source snapshot: text files with line count + SHA256 -----------------
S "SOURCE SNAPSHOT (TEXT FILES)"
$txtExt = @('.cs','.xaml','.json','.xml','.config','.props','.targets',
    '.csproj','.sln','.md','.txt','.yml','.yaml','.ini','.cmd','.ps1')
Get-ChildItem -LiteralPath $slnDir -Recurse -File -Force -ErrorAction SilentlyContinue |
  Where-Object { $txtExt -contains $_.Extension.ToLower() } |
  Sort-Object FullName |
  ForEach-Object {
    $hash = ''
    try { $hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $_.FullName).Hash } catch {}
    $lines = 0
    try { $lines = (Get-Content -LiteralPath $_.FullName -Encoding UTF8).Count } catch {}
    W ("--- FILE: " + $_.FullName + " | " + $_.Length + " bytes | " + $lines + " lines | SHA256 " + $hash + " ---")
    try { Get-Content -LiteralPath $_.FullName -Encoding UTF8 | Out-File $logPath -Append -Encoding utf8 } catch { W ("Read error: " + $_.Exception.Message) }
    W ("--- END FILE: " + $_.FullName + " ---")
  }

S "END"
W ("CompletedUTC: " + [DateTime]::UtcNow)

# ---- summary (pre-scan) ---------------------------------------------------
S "SUMMARY"
# projects count
$projExts = @('.csproj','.vbproj','.fsproj')
$projFiles = Get-ChildItem -LiteralPath $slnDir -Recurse -File -Force -ErrorAction SilentlyContinue |
    Where-Object { $projExts -contains $_.Extension.ToLower() }
$projCount = ($projFiles | Measure-Object).Count

# file inventory
$allFiles = Get-ChildItem -LiteralPath $slnDir -Recurse -File -Force -ErrorAction SilentlyContinue
$allCount = ($allFiles | Measure-Object).Count
$txtFiles = $allFiles | Where-Object { $txtExt -contains $_.Extension.ToLower() }
$txtCount = ($txtFiles | Measure-Object).Count
$txtBytes = ($txtFiles | Measure-Object -Property Length -Sum).Sum

# total line count across text files
$txtLines = 0
foreach($f in $txtFiles){
    try { $txtLines += (Get-Content -LiteralPath $f.FullName -Encoding UTF8).Count } catch {} 
}

W ("Projects: "   + $projCount)
W ("AllFiles: "   + $allCount)
W ("TextFiles: "  + $txtCount)
W ("TextBytes: "  + $txtBytes)
W ("TotalTextLines: "+ $txtLines)


