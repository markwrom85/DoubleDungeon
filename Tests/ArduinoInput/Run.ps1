$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$testRoot = Join-Path $projectRoot 'Temp/ArduinoValidation'
$versionLine = Get-Content (Join-Path $projectRoot 'ProjectSettings/ProjectVersion.txt') | Select-Object -First 1
$version = ($versionLine -split ': ')[1]
$unityExe = "C:/Program Files/Unity/Hub/Editor/$version/Editor/Unity.exe"
if (!(Test-Path -LiteralPath $unityExe)) { throw "Unity $version not found at $unityExe" }
New-Item -ItemType Directory -Force "$testRoot/Assets/Scripts","$testRoot/Assets/Editor","$testRoot/Assets/Settings","$testRoot/Packages","$testRoot/ProjectSettings" | Out-Null
Copy-Item "$projectRoot/Assets/Scripts/PlayerScripts/*" "$testRoot/Assets/Scripts" -Force
Copy-Item "$projectRoot/Assets/Settings/InputSystem_Actions.inputactions*" "$testRoot/Assets/Settings" -Force
Copy-Item "$projectRoot/ProjectSettings/ProjectVersion.txt" "$testRoot/ProjectSettings" -Force
Copy-Item "$PSScriptRoot/ArduinoPlayChecks.cs" "$testRoot/Assets" -Force
Copy-Item "$PSScriptRoot/ArduinoTestBootstrap.cs" "$testRoot/Assets/Editor" -Force
'{"dependencies":{"com.unity.inputsystem":"1.20.0","com.unity.modules.physics2d":"1.0.0","com.unity.modules.imgui":"1.0.0","com.unity.modules.imageconversion":"1.0.0"}}' | Set-Content "$testRoot/Packages/manifest.json"
$resultPath = Join-Path $testRoot 'arduino-play-checks-passed.txt'
if (Test-Path -LiteralPath $resultPath) { Remove-Item -LiteralPath $resultPath }
$arguments = @('-batchmode','-nographics','-projectPath',('"' + $testRoot + '"'),'-executeMethod','ArduinoTestBootstrap.Run','-logFile',('"' + $testRoot + '/checks.log"'))
$process = Start-Process -FilePath $unityExe -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
if ($process.ExitCode -ne 0 -or !(Test-Path -LiteralPath $resultPath)) { throw "Arduino tests failed; read $testRoot/checks.log" }
Get-Content -LiteralPath $resultPath
