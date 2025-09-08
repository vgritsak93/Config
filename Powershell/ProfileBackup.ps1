# Microsoft.PowerShell_profile.ps1  — PS7 rolling log

# Choose Documents (OneDrive if present)
$DocRoot = if ($env:OneDrive) { Join-Path $env:OneDrive 'Documents' }
           else { Join-Path $env:USERPROFILE 'Documents' }

$LogDir  = Join-Path $DocRoot 'PowerShell\Logs'
$LogFile = Join-Path $LogDir  'ps_current.log'

# Ensure folder
if (-not (Test-Path -LiteralPath $LogDir)) {
    New-Item -ItemType Directory -Path $LogDir -Force | Out-Null
}

# Stop any active transcript first, ignore errors
try { Stop-Transcript | Out-Null } catch {}

# Start a fresh transcript (overwrite on new window/tab)
Start-Transcript -Path $LogFile -Force | Out-Null

# Optional helper
function Show-Log { Get-Content $LogFile -Wait -Tail 200 }

#**********************
# Custom Git aliases for Drive work-tree
$GitDir = "C:\Repos\Config.git"
$WorkTree = "G:\Shared drives\GSADUs Projects\Our Models\0 - CATALOG\Config"

function gstatus { git --git-dir=$GitDir --work-tree=$WorkTree status }
function gadd { param($Path=".") git --git-dir=$GitDir --work-tree=$WorkTree add $Path }
function gcommit { param($Message) git --git-dir=$GitDir --work-tree=$WorkTree commit -m $Message }
function gpush { git --git-dir=$GitDir --work-tree=$WorkTree push origin master }
function gpull { git --git-dir=$GitDir --work-tree=$WorkTree pull origin master }
