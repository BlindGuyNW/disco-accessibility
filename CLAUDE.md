# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a MelonLoader mod for Disco Elysium that provides accessibility features, specifically object detection and logging for screen reader integration. The mod uses Harmony patches to intercept the game's interaction system and extract information about objects the player is hovering over or selecting.

## Architecture

### Core Components

**MelonLoader Integration**: The mod uses MelonLoader's mod framework with the correct game identifiers:
- Developer: "ZAUM Studio" 
- Game: "Disco Elysium"

**Harmony Patching System**: The mod patches key game methods:
- `InteractableSelectionManager.OnUpdate()` - Detects when object selection changes
- `InteractableSelectionManager.set_CurrentSelected` - Alternative hook for selection changes  
- `InteractableSelectionManager.Add()` - Monitors when objects are added to selection

**Object Detection**: The mod extracts detailed information from two main object types:
- `OrbUiElement` - Skill checks, thoughts, dice rolls
- `MouseOverHighlight` - NPCs, environment objects, items

### Key Game Classes

The mod interfaces with Il2Cpp-generated classes from the game:
- `InteractableSelectionManager` - Main selection system
- `CommonPadInteractable` - Wrapper for all interactable objects
- `Character` - Player character reference
- `GameEntity` - Game-specific entity data

## Development Commands

### Building
```bash
cd mod
./build.sh    # Linux/WSL - auto-detects Disco Elysium path
./build.bat   # Windows
```

The build system:
- References game assemblies from `MelonLoader/Il2CppAssemblies/` and `MelonLoader/net6/`
- Uses fallback paths if `DISCO_ELYSIUM_PATH` environment variable not set
- Excludes `../disco/` source directory from compilation (kept for API reference)
- Auto-copies built DLL to `Disco Elysium/Mods/` folder

### Testing
1. Launch Disco Elysium (MelonLoader loads the mod automatically)
2. MelonLoader console window shows mod output
3. Move cursor over objects in-game to see detection logs

## Code Structure

### File Organization
```
mod/                           # Mod source files
├── AccessibilityMod.cs        # Main mod and Harmony patches
├── AccessibilityMod.csproj    # Build configuration
├── build.sh/.bat             # Build scripts
disco/                         # Game source code (read-only reference)
├── Il2Cpp/                    # Core game classes
├── Il2CppCollageMode/         # UI and interaction systems  
├── Il2CppFortressOccident/    # Game entities and characters
└── ...                        # Other game namespaces
```

### Harmony Patch Pattern
All patches follow this structure:
```csharp
[HarmonyPatch(typeof(TargetClass), "MethodName")]
public class PatchClassName
{
    static void Postfix(TargetClass __instance, /* method parameters */)
    {
        try
        {
            // Patch logic with error handling
        }
        catch (Exception ex)
        {
            MelonLogger.Error($"Error in patch: {ex}");
        }
    }
}
```

## Critical Implementation Details

### Assembly References
The mod requires specific Il2Cpp assemblies. Missing references will cause compile errors referencing types like `PlayerOneAxisAction` (requires `Il2CppInControl.dll`).

### Object Information Extraction
The `LogInteractableInfo()` method demonstrates how to safely extract object data:
- Use null-conditional operators (`?.`) for Il2Cpp objects
- Handle both Orb and MouseOverHighlight object types
- Access Unity Transform via `.transform` property, not `GetComponent<Transform>()`

### Type Compatibility
- `InteractableSelectionManager` inherits from `Il2CppSystem.Object`, not Unity's `MonoBehaviour`
- Use direct type checking rather than `is` pattern matching for Il2Cpp types
- GameObject finding requires Unity methods like `FindObjectOfType<Character>()`

## Future Development Paths

The current logging system can be replaced with:
- **Tolk integration** for screen reader announcements
- **Spatial audio cues** using world position coordinates  
- **Configuration system** for user preferences
- **UI accessibility patches** for menus and inventory