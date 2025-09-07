@echo off
echo Building Disco Elysium Accessibility Mod...

REM Check if DISCO_ELYSIUM_PATH environment variable is set
if "%DISCO_ELYSIUM_PATH%"=="" (
    echo Warning: DISCO_ELYSIUM_PATH environment variable not set.
    echo Please set it to your Disco Elysium installation directory.
    echo Example: set DISCO_ELYSIUM_PATH=C:\Program Files ^(x86^)\Steam\steamapps\common\Disco Elysium
    echo.
    echo Trying to build with default Steam path...
)

REM Build the project
dotnet build AccessibilityMod.csproj --configuration Release

if %ERRORLEVEL% == 0 (
    echo.
    echo Build successful!
    if not "%DISCO_ELYSIUM_PATH%"=="" (
        echo Mod copied to: %DISCO_ELYSIUM_PATH%\Mods\
    ) else (
        echo Please manually copy AccessibilityMod.dll from bin\Release\net6.0\ to your Disco Elysium\Mods\ folder
    )
) else (
    echo.
    echo Build failed! Check the error messages above.
)

pause