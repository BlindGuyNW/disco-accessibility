# Disco Elysium Accessibility Mod

A MelonLoader mod that adds accessibility features to Disco Elysium, starting with object detection and logging for screen reader integration.

## Features

- **Object Detection**: Automatically detects and logs information about interactable objects
- **Controller Support**: Monitors right stick camera movement for object detection
- **Detailed Logging**: Provides object names, types, positions, and other useful information
- **Foundation for Screen Reader Integration**: Designed to work with Tolk library for future screen reader support

## Prerequisites

1. **Disco Elysium - The Final Cut** installed
2. **MelonLoader** installed in your Disco Elysium directory
3. **.NET 6.0 SDK** for building the mod

## Installation

### Option 1: Build from Source

1. Set the environment variable for your Disco Elysium path:
   ```bash
   # Windows
   set DISCO_ELYSIUM_PATH=C:\Program Files (x86)\Steam\steamapps\common\Disco Elysium
   
   # Linux/Mac
   export DISCO_ELYSIUM_PATH="/path/to/your/disco/elysium"
   ```

2. Build the mod:
   ```bash
   # Windows
   build.bat
   
   # Linux/Mac
   ./build.sh
   ```

3. The mod will be automatically copied to your Mods folder if the environment variable is set correctly.

### Option 2: Manual Installation

1. Build the project:
   ```bash
   dotnet build AccessibilityMod.csproj --configuration Release
   ```

2. Copy `bin/Release/net6.0/AccessibilityMod.dll` to your `Disco Elysium/Mods/` folder

## Usage

1. Launch Disco Elysium
2. Open the MelonLoader console (usually F4 or check MelonLoader documentation)
3. Move around in the game - object information will be logged to the console
4. Use controller right stick for camera movement to trigger additional object detection

## What Gets Logged

The mod logs detailed information about objects you interact with:

- **Object Type**: Skill check, dialogue option, item, etc.
- **Position**: World coordinates for spatial awareness
- **Name**: GameObject name when available
- **Entity Information**: Game-specific entity data
- **Object Classification**: Distinguishes between Orbs (skill checks, thoughts) and regular interactables

## Example Output

```
Selected interactable: Type: SkillCheck, Position: (12.34, 5.67, 8.90), Object: Orb, GameObject: Logic_Check_Easy, 
Right stick input detected: (0.75, -0.23)
Looking for interactables from character position (10.0, 0.0, 8.5) in direction (0.75, -0.23)
```

## Development

This mod is designed as a foundation for more advanced accessibility features. Key areas for expansion:

1. **Screen Reader Integration**: Use Tolk library to announce object information
2. **Spatial Audio**: Add audio cues for object locations
3. **Enhanced Navigation**: Implement better controller-based object selection
4. **Menu Accessibility**: Add support for menu navigation

## Technical Details

The mod uses Harmony patches to hook into:

- `InteractableSelectionManager.OnUpdate()`: Detects selection changes
- `InteractableSelectionManager.set_CurrentSelected`: Monitors selection updates
- `DragInput.Update()`: Captures controller input

## Troubleshooting

**Build Issues:**
- Ensure .NET 6.0 SDK is installed
- Verify Disco Elysium path is correct
- Check that MelonLoader is properly installed

**Runtime Issues:**
- Check MelonLoader console for error messages
- Verify the mod DLL is in the correct Mods folder
- Ensure game version compatibility

## Contributing

This is an early-stage accessibility mod. Contributions welcome for:
- Screen reader integration
- Better object detection algorithms
- Menu and UI accessibility
- Audio cues and spatial feedback

## License

MIT License - Feel free to use and modify for accessibility improvements.