$ErrorActionPreference='Stop'
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if(-not (Test-Path -LiteralPath $compiler)){throw '.NET Framework C# compiler not found. Nothing installed automatically.'}
$sources=@('AssemblyInfo.cs','CameraSession.cs','CameraPanel.cs','Program.cs','Localization.cs') | ForEach-Object {Join-Path $PSScriptRoot $_}
$references=@('/reference:System.Windows.Forms.dll','/reference:System.Drawing.dll','/reference:System.Runtime.Serialization.dll')
$common=@('/nologo','/codepage:65001','/platform:x64','/optimize+','/debug-')
$testDirectory=Join-Path $PSScriptRoot 'obj'
New-Item -ItemType Directory -Path $testDirectory -Force | Out-Null
$testBinary=Join-Path $testDirectory 'CameraExperiment.exe'
& $compiler @common @references /define:RESEARCH /target:exe "/out:$testBinary" @sources
if($LASTEXITCODE -ne 0){throw 'Test build failed'}
& $testBinary --self-test
if($LASTEXITCODE -ne 0){throw 'Self-tests failed'}
$binary=Join-Path (Split-Path -Parent $PSScriptRoot) 'ShoulderCamera.exe'
& $compiler @common @references /target:winexe "/out:$binary" @sources
if($LASTEXITCODE -ne 0){throw 'Panel build failed'}
Get-FileHash -LiteralPath $binary -Algorithm SHA256

