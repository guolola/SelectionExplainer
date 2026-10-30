param([switch]$Test, [string]$OutputName = 'SelectionExplainer.exe')
$ErrorActionPreference = 'Stop'
if ($OutputName -notmatch '^[A-Za-z0-9._-]+\.exe$') { throw 'OutputName must be a simple .exe filename.' }
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$compiler = Join-Path $framework 'csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw '.NET Framework 4.x x64 compiler is required.' }
$outputDir = Join-Path $PSScriptRoot 'dist'
New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
$sources = @('AssemblyInfo.cs','Config.cs','ModelClient.cs','SelectionMonitor.cs','UiComponents.cs','Views.cs','Program.cs') | ForEach-Object { Join-Path $PSScriptRoot "src\$_" }
$references = @('System.dll','System.Core.dll','System.Drawing.dll','System.Windows.Forms.dll','System.Web.Extensions.dll','System.Security.dll', (Join-Path $framework 'WPF\UIAutomationClient.dll'), (Join-Path $framework 'WPF\UIAutomationTypes.dll'), (Join-Path $framework 'WPF\WindowsBase.dll'))
$arguments = @('/nologo','/optimize+','/platform:x64','/target:winexe',"/win32manifest:$(Join-Path $PSScriptRoot 'app.manifest')", "/win32icon:$(Join-Path $PSScriptRoot 'docs\brand\app.ico')", "/out:$(Join-Path $outputDir $OutputName)")
foreach ($reference in $references) { $arguments += "/reference:$reference" }
& $compiler @arguments @sources
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
Get-Item -LiteralPath (Join-Path $outputDir $OutputName) | Select-Object Name,Length
if ($Test) {
    $testDir = Join-Path $PSScriptRoot 'work\tests'
    New-Item -ItemType Directory -Path $testDir -Force | Out-Null
    $testArgs = @('/nologo','/optimize+','/platform:x64','/target:exe','/main:SelectionExplainer.Tests', "/out:$(Join-Path $testDir 'Tests.exe')")
    foreach ($reference in $references) { $testArgs += "/reference:$reference" }
    & $compiler @testArgs @sources (Join-Path $PSScriptRoot 'tests\Tests.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Test build failed.' }
    & (Join-Path $testDir 'Tests.exe')
    if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
}
