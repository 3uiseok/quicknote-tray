# Build QuickNote.exe with the csc bundled in .NET Framework 4.x (no extra install)
$csc = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$here = Split-Path -Parent $MyInvocation.MyCommand.Path

# 1) sheep icon generator -> sheep.ico
& $csc /nologo /codepage:65001 /out:"$here\MakeIcon.exe" /r:System.Drawing.dll "$here\MakeIcon.cs"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& "$here\MakeIcon.exe" "$here"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# 2) app
& $csc /nologo /target:winexe /codepage:65001 /out:"$here\QuickNote.exe" /win32icon:"$here\sheep.ico" `
    /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:Microsoft.CSharp.dll /r:System.Core.dll `
    "$here\QuickNote.cs"
exit $LASTEXITCODE
