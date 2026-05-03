$Dll = Join-Path -Path $PSScriptRoot -ChildPath "..\bin\Debug\net10.0\DragonBot.CoreModules.dll"
$Destination = Join-Path -Path $PSScriptRoot -ChildPath "..\..\bin\Debug\net10.0\modules\DragonBot.CoreModules.dll"

Copy-Item -Path $Dll -Destination $Destination