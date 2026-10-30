param([Parameter(Mandatory=$true)][ValidatePattern('^\d+\.\d+\.\d+$')][string]$Version)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$expected = "AssemblyVersion(`"$Version.0`")"
if (-not (Get-Content -LiteralPath (Join-Path $root 'src\AssemblyInfo.cs') -Raw).Contains($expected)) {
    throw 'Release version must match src/AssemblyInfo.cs.'
}
Push-Location $root
try {
    & ./build.ps1 -Test
    $artifacts = Join-Path $root 'artifacts'
    New-Item -ItemType Directory -Path $artifacts -Force | Out-Null
    if ((Get-ChildItem -LiteralPath $artifacts -File).Count -gt 0) { throw 'Use an empty artifacts directory; existing release files will not be overwritten.' }
    $prefix = "SelectionExplainer-v$Version"
    $portable = Join-Path $artifacts "$prefix-windows-x64.zip"
    $portableFiles = @('dist\SelectionExplainer.exe','docs\release\README.txt','LICENSE','NOTICE') | ForEach-Object { Join-Path $root $_ }
    Compress-Archive -LiteralPath $portableFiles -DestinationPath $portable -CompressionLevel Optimal
    & git archive --format=zip "--prefix=$prefix/" "--output=$(Join-Path $artifacts "$prefix-source.zip")" HEAD
    if ($LASTEXITCODE -ne 0) { throw 'Source ZIP creation failed.' }
    & git archive --format=tar.gz "--prefix=$prefix/" "--output=$(Join-Path $artifacts "$prefix-source.tar.gz")" HEAD
    if ($LASTEXITCODE -ne 0) { throw 'Source tar.gz creation failed.' }
    $commit = & git rev-parse HEAD
    if ($LASTEXITCODE -ne 0) { throw 'Cannot identify release commit.' }
    [ordered]@{
        name = 'SelectionExplainer'
        version = $Version
        status = 'preview'
        source_commit = $commit.Trim()
        platform = 'Windows x64'
        runtime = '.NET Framework 4.8'
        build_and_local_provider_tests = 'passed'
        code_signed = $false
        real_provider_quality = 'not verified by this workflow'
    } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $artifacts 'release.json') -Encoding utf8
    $checksums = Get-ChildItem -LiteralPath $artifacts -File | Sort-Object Name | ForEach-Object {
        '{0}  {1}' -f (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant(), $_.Name
    }
    $checksums | Set-Content -LiteralPath (Join-Path $artifacts 'SHA256SUMS') -Encoding ascii
    Get-ChildItem -LiteralPath $artifacts -File | Select-Object Name,Length
} finally { Pop-Location }
