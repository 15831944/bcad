@echo off

set configuration=Debug
set runtests=true

:parseargs
if "%1" == "" goto argsdone
if /i "%1" == "-c" goto set_configuration
if /i "%1" == "--configuration" goto set_configuration
if /i "%1" == "-notest" goto set_notest
if /i "%1" == "--notest" goto set_notest

echo Unsupported argument: %1
goto error

:set_configuration
set configuration=%2
shift
shift
goto parseargs

:set_notest
set runtests=false
shift
goto parseargs

:argsdone

:: IxMilia.Dxf needs a custom invocation
call %~dp0src\IxMilia.Dxf\build-and-test.cmd -c %configuration% -notest -portable
if errorlevel 1 echo Error pre-building IxMilia.Dxf && goto error

:: restore packages
dotnet restore
if errorlevel 1 echo Error restoring packages && goto error

:: build .NET
dotnet build -c %configuration%
if errorlevel 1 echo Error building solution && goto error

:: test
if /i "%runtests%" == "true" (
    dotnet test --no-restore --no-build -c %configuration%
    if errorlevel 1 echo Error running tests && goto error
)

:: build electron
pushd %~dp0src\bcad
call npm i
if errorlevel 1 echo Error restoring npm packages && goto error
call npm config set bcad:configuration %configuration% && call npm run pack
if errorlevel 1 echo Error packing electron && goto error
popd

:: create deployment file
mkdir "%~dp0artifacts\publish"
set suffix=
if /i "%configuration%" == "debug" set suffix=-debug
set filename=bcad-win32-x64%suffix%.zip
powershell -Command "Compress-Archive -Path '%~dp0artifacts\pack\bcad-win32-x64\' -DestinationPath '%~dp0artifacts\publish\%filename%' -Force"
if errorlevel 1 echo Error creating deployment file && goto error

:: report final artifact name for GitHub Actions
echo ::set-env name=artifact_file_name::%filename%

exit /b 0

:error
echo Build exited with failures.
cd /d %~dp0
exit /b 1
