#!/bin/bash
echo "Building Disco Elysium Accessibility Mod..."

# Check if DISCO_ELYSIUM_PATH environment variable is set
if [ -z "$DISCO_ELYSIUM_PATH" ]; then
    echo "Warning: DISCO_ELYSIUM_PATH environment variable not set."
    echo "Setting to detected WSL Steam path..."
    export DISCO_ELYSIUM_PATH="/mnt/c/Program Files (x86)/Steam/steamapps/common/Disco Elysium"
    echo "Using: $DISCO_ELYSIUM_PATH"
fi

# Build the project
dotnet build AccessibilityMod.csproj --configuration Release

if [ $? -eq 0 ]; then
    echo ""
    echo "Build successful!"
    if [ -n "$DISCO_ELYSIUM_PATH" ]; then
        echo "Mod copied to: $DISCO_ELYSIUM_PATH/Mods/"
    else
        echo "Please manually copy AccessibilityMod.dll from bin/Release/net6.0/ to your Disco Elysium/Mods/ folder"
    fi
else
    echo ""
    echo "Build failed! Check the error messages above."
fi