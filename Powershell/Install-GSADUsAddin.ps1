# GSADUs Revit Add-in installer with auto-detect (net8.0-windows aware)

$ErrorActionPreference = "Stop"

# --- Config ---
$RevitYear      = 2026
$DeployDir      = "C:\GSADUs\RevitAddin"
$BackupDir      = Join-Path $DeployDir "Backups"
$AddinFileName  = "GSADUs.BatchExport.addin"
$AppFullClass   = "GSADUs.Revit.Addin.Startup"
$CmdFullClass   = "GSADUs.Revit.Addin.BatchExportCommand"
$VendorId       = "GSADUs"
$VendorDesc     = "GSADUs Tools"
$AppAddInId     = "8F0A0000-0000-4000-9000-000000000002"
$CmdAddInId     = "E5B48D43-DD13-4A93-BD12-5D3A523C53FD"

# Your project root (adjust if different)
$ProjectRoot = "G:\Shared drives\GSADUs Projects\Our Models\0 - CATALOG\Config\GSADUs Tools\src\GSADUs.Revit.Addin"

# --- Find newest build DLL under bin\x64 (covers net8.0-windows, Debug/Release) ---
$binRoot = Join-Path $ProjectRoot "bin\x64"
$sourceDll = $null
if (Test-Path $binRoot) {
  $dlls = Get-ChildItem -Path $binRoot -Recurse -Filter "GSADUs.Revit.Addin.dll" -File -ErrorAction SilentlyContinue
  if ($dlls) {
    $sourceDll = $dlls | Sort-Object LastWriteTime -Descending | Select-Object -First 1 -ExpandProperty FullName
  }
}
if (-not $sourceDll) {
  $sourceDll = Read-Host "DLL not found automatically. Paste full path to GSADUs.Revit.Addin.dll"
  if (-not (Test-Path $sourceDll)) { throw "Path not found: $sourceDll" }
}

# --- Create deploy + backup dirs ---
New-Item -ItemType Directory -Force -Path $DeployDir  | Out-Null
New-Item -ItemType Directory -Force -Path $BackupDir  | Out-Null

# --- Copy DLL (+PDB if present) ---
$dstDll = Join-Path $DeployDir "GSADUs.Revit.Addin.dll"
Copy-Item $sourceDll $dstDll -Force
$pdbSrc = [IO.Path]::ChangeExtension($sourceDll, ".pdb")
if (Test-Path $pdbSrc) { Copy-Item $pdbSrc (Join-Path $DeployDir "GSADUs.Revit.Addin.pdb") -Force }

Unblock-File -Path $dstDll -ErrorAction SilentlyContinue

# --- Write .addin into ProgramData, backup old into BackupDir ---
$addinDir  = Join-Path $env:ProgramData "Autodesk\Revit\Addins\$RevitYear"
New-Item -ItemType Directory -Force -Path $addinDir | Out-Null
$addinPath = Join-Path $addinDir $AddinFileName
if (Test-Path $addinPath) {
  $stamp = Get-Date -Format yyyyMMdd_HHmmss
  Copy-Item $addinPath (Join-Path $BackupDir "$AddinFileName.bak_$stamp") -Force
}

$xml = @"
<?xml version="1.0" encoding="utf-8" standalone="no"?>
<RevitAddIns>
  <AddIn Type="Application">
    <Name>GSADUs Tools</Name>
    <Assembly>$dstDll</Assembly>
    <AddInId>$AppAddInId</AddInId>
    <FullClassName>$AppFullClass</FullClassName>
    <VendorId>$VendorId</VendorId>
    <VendorDescription>$VendorDesc</VendorDescription>
  </AddIn>
  <AddIn Type="Command">
    <Name>GSADUs Batch Export</Name>
    <Assembly>$dstDll</Assembly>
    <AddInId>$CmdAddInId</AddInId>
    <FullClassName>$CmdFullClass</FullClassName>
    <VendorId>$VendorId</VendorId>
    <VendorDescription>$VendorDesc</VendorDescription>
  </AddIn>
</RevitAddIns>
"@
$xml | Set-Content -Path $addinPath -Encoding UTF8

# --- Summary ---
$hash = (Get-FileHash $dstDll -Algorithm SHA256).Hash
Write-Host "`nDeployed DLL: $dstDll" -ForegroundColor Green
Write-Host "Source DLL:   $sourceDll"
Write-Host "SHA256:       $hash"
Write-Host "Add-in file:  $addinPath"
Write-Host "Backups:      $BackupDir"
Write-Host "Done."
