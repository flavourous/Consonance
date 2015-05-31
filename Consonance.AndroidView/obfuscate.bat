pushd %ProgramFiles(x86)%\Reference Assemblies\Microsoft\Framework\MonoAndroid
copy v1.0\mscorlib.dll %1\bin\Release
copy v4.0.3\Mono.Android.dll %1\bin\Release
popd
Confuser.CLI.exe -n %1\%2.crproj
pushd %1\bin\Release
del mscorlib.dll Mono.Android.dll
popd
