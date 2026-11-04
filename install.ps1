# QuickNote installer (latest GitHub release). ASCII only for PowerShell 5.1
# powershell -ExecutionPolicy Bypass -c "irm https://raw.githubusercontent.com/3uiseok/quicknote-tray/main/install.ps1 | iex"
$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$dir = if ($env:QUICKNOTE_DIR) { $env:QUICKNOTE_DIR } else { Join-Path $env:LOCALAPPDATA 'Programs\QuickNote' }
$url = 'https://github.com/3uiseok/quicknote-tray/releases/latest/download/QuickNote.zip'
$zip = Join-Path $env:TEMP 'QuickNote.zip'

# stop a running copy so the exe can be replaced
Get-Process QuickNote -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 300

Write-Host "Downloading $url"
Invoke-WebRequest $url -OutFile $zip -UseBasicParsing
New-Item -ItemType Directory -Force $dir | Out-Null
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::OpenRead($zip)
try {
    foreach ($e in $archive.Entries) {
        if ($e.Name -eq '') { continue }
        [IO.Compression.ZipFileExtensions]::ExtractToFile($e, (Join-Path $dir $e.Name), $true)
    }
} finally { $archive.Dispose() }
Remove-Item $zip -Force

# remove the "downloaded from the internet" mark (avoids the SmartScreen prompt)
Get-ChildItem $dir -Recurse | Unblock-File

$exe = Join-Path $dir 'QuickNote.exe'
if (-not $env:QUICKNOTE_NO_SHORTCUT) {
    $lnk = Join-Path ([Environment]::GetFolderPath('Programs')) 'QuickNote.lnk'
    $sc = (New-Object -ComObject WScript.Shell).CreateShortcut($lnk)
    $sc.TargetPath = $exe
    $sc.WorkingDirectory = $dir
    $sc.Save()
    Write-Host "Start menu shortcut: $lnk"
}

Write-Host "Installed: $exe"
if (-not $env:QUICKNOTE_NO_START) { Start-Process $exe }
