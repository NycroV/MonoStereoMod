@ECHO OFF
ILRepack /wildcards /verbose /out:MonoStereoMod.Dependencies.dll "Debug\net8.0\MonoStereoMod.dll" ..\lib\*.dll
timeout /t -1