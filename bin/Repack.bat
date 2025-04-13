@ECHO OFF
ILRepack /verbose /out:MonoStereoMod.Dependencies.dll "Debug\net8.0\MonoStereoMod.dll" ..\lib\MonoStereo.Dependencies.dll
timeout /t -1
