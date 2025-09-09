<# gsync.ps1 — one button to sync Mirror + Archive #>

param(
  [ValidateSet('Mirror','Archive','Both')]
  [string]$Mode = 'Both',

  # paths and remotes
  [string]$WorkTree      = 'G:\Shared drives\GSADUs Projects\Our Models\0 - CATALOG\Config',
  [string]$MirrorGitDir  = 'C:\Repos\Config.git',
  [string]$ArchiveGitDir = 'C:\Repos\Config_Archive.git',
  [string]$MirrorRemote  = 'https://github.com/vgritsak93/Config.git',
  [string]$ArchiveRemote = 'https://github.com/vgritsak93/Config-Archive.git',

  [switch]$NoPause
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ----- helpers -----

function Invoke-Git {
  [CmdletBinding()]
  param([Parameter(Mandatory=$true)][string[]]$GitArgs)
  & git $GitArgs
  if ($LASTEXITCODE -ne 0) { throw "git failed: exit $LASTEXITCODE" }
}

function Test-GitAvailable {
  [CmdletBinding()]
  param()
  $git = Get-Command git -ErrorAction SilentlyContinue
  if (-not $git) { throw "git.exe not found on PATH" }
}

function Initialize-GitRepository {
  [CmdletBinding()]
  param(
    [Parameter(Mandatory=$true)][string]$GitDir,
    [Parameter(Mandatory=$true)][string]$Remote
  )

  if (-not (Test-Path $GitDir)) { New-Item -ItemType Directory -Path $GitDir | Out-Null }

  # init bare repo if new
  if (-not (Test-Path (Join-Path $GitDir 'HEAD'))) {
    Invoke-Git @('init','--bare',$GitDir)
  }

  # origin remote
  $hasOrigin = $true
  try { Invoke-Git @("--git-dir=$GitDir",'remote','get-url','origin') | Out-Null } catch { $hasOrigin = $false }
  if ($hasOrigin) { Invoke-Git @("--git-dir=$GitDir",'remote','set-url','origin',$Remote) }
  else { Invoke-Git @("--git-dir=$GitDir",'remote','add','origin',$Remote) }

  # minimal identity for commits
  try { Invoke-Git @("--git-dir=$GitDir",'config','user.name') | Out-Null } catch {
    Invoke-Git @("--git-dir=$GitDir",'config','user.name','gsync')
    Invoke-Git @("--git-dir=$GitDir",'config','user.email','gsync@local')
  }
}

function Publish-GitChanges {
  [CmdletBinding()]
  param(
    [Parameter(Mandatory=$true)][string]$GitDir,
    [Parameter(Mandatory=$true)][string]$WT,
    [string]$RemoteName = 'origin'
  )

  # ensure default branch is main BEFORE first commit
  try { Invoke-Git @("--git-dir=$GitDir",'rev-parse','--verify','main') | Out-Null }
  catch { Invoke-Git @("--git-dir=$GitDir",'symbolic-ref','HEAD','refs/heads/main') }

  # stage and commit
  Invoke-Git @("--git-dir=$GitDir","--work-tree=$WT",'add','-A')

  $msg = "gsync: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
  try {
    Invoke-Git @("--git-dir=$GitDir","--work-tree=$WT",'commit','-m', $msg)
  } catch {
    # likely "nothing to commit"; continue to push anyway
  }

  # push HEAD to main
  Invoke-Git @("--git-dir=$GitDir",'push',$RemoteName,'HEAD:main')
}

# ----- run -----

Write-Host "Mode: $Mode"
Write-Host "WorkTree: $WorkTree"

try {
  Test-GitAvailable

  if ($Mode -in @('Mirror','Both')) {
    Write-Host "`n[Mirror] Ensuring repo..."
    Initialize-GitRepository -GitDir $MirrorGitDir -Remote $MirrorRemote
    Write-Host "[Mirror] Committing and pushing..."
    Publish-GitChanges -GitDir $MirrorGitDir -WT $WorkTree
  }

  if ($Mode -in @('Archive','Both')) {
    Write-Host "`n[Archive] Ensuring repo..."
    Initialize-GitRepository -GitDir $ArchiveGitDir -Remote $ArchiveRemote
    Write-Host "[Archive] Committing and pushing..."
    Publish-GitChanges -GitDir $ArchiveGitDir -WT $WorkTree
  }

  Write-Host "`nSync complete."
}
catch {
  Write-Host "[ERROR] $($_.Exception.Message)" -ForegroundColor Red
}
finally {
  if (-not $NoPause) { Read-Host "Press Enter to close" | Out-Null }
}
