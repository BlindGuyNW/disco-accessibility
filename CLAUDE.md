# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a MelonLoader mod for Disco Elysium that provides accessibility features, including object detection in the game world and menu navigation tracking for screen reader integration. The mod uses Harmony patches to intercept both the game's interaction system and custom UI navigation system to extract information about objects and UI elements the player is interacting with.

## Architecture

### Core Components

**MelonLoader Integration**: The mod uses MelonLoader's mod framework with the correct game identifiers:
- Developer: "ZAUM Studio" 
- Game: "Disco Elysium"

**Dual Patching System**: The mod patches both world interaction and UI navigation:

**World Object Detection**:
- `InteractableSelectionManager.OnUpdate()` - Detects when object selection changes
- `InteractableSelectionManager.set_CurrentSelected` - Alternative hook for selection changes  
- `InteractableSelectionManager.Add()` - Monitors when objects are added to selection

**Menu Navigation Tracking**:
- `INavigationReceiver.SelectNext()` - Detects controller navigation (right/down)
- `INavigationReceiver.SelectPrevious()` - Detects controller navigation (left/up)
- `UIScrollToSelection.Update()` - Tracks current UI selection state

**Object Detection**: The mod extracts detailed information from multiple object types:
- `OrbUiElement` - Skill checks, thoughts, dice rolls
- `MouseOverHighlight` - NPCs, environment objects, items
- UI Elements - Buttons, toggles, sliders, dropdowns with text content

### Key Game Classes

**World Interaction Classes**:
- `InteractableSelectionManager` - Main selection system
- `CommonPadInteractable` - Wrapper for all interactable objects
- `Character` - Player character reference
- `GameEntity` - Game-specific entity data

**UI Navigation Classes**:
- `INavigationReceiver` - Interface for custom menu navigation
- `NavigationInputHandler` - Base class for controller input handling
- `UIScrollToSelection` - Unity extension for tracking UI selection
- `DialogueNavigationInputHandler`, `OperationsNavigationInputHandler` - Specific navigation handlers

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

**World Object Detection**:
1. Launch Disco Elysium (MelonLoader loads the mod automatically)
2. MelonLoader console window shows mod output
3. Move cursor over objects in-game to see detection logs

**Menu Navigation Testing**:
1. Use controller D-pad/stick to navigate menus (main menu, options, inventory, etc.)
2. Watch console for `[NAVIGATION]` and `[UIScrollToSelection]` messages
3. Each navigation action should log the selected UI element's details

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
The mod requires specific Il2Cpp assemblies. Key dependencies include:
- `Unity.TextMeshPro.dll` - Required for TextMeshPro component access in UI elements
- `Il2CppInControl.dll` - Required for controller input types
- Missing references will cause compile errors for specific component types

### Information Extraction Patterns

**World Objects** (`LogInteractableInfo()` method):
- Use null-conditional operators (`?.`) for Il2Cpp objects
- Handle both Orb and MouseOverHighlight object types
- Access Unity Transform via `.transform` property, not `GetComponent<Transform>()`

**UI Elements** (`NavigationHelper.LogUISelectionInfo()` method):
- Extract text from multiple component types: `UnityEngine.UI.Text`, `TextMeshProUGUI`, `TextMeshPro`
- Check for UI component types: Button, Toggle, Slider, Dropdown
- Get text from child components for buttons using `GetComponentInChildren<>()`
- Build hierarchical context with parent/grandparent GameObject names

### Type Compatibility and Navigation System Architecture

**Il2Cpp Compatibility**:
- `InteractableSelectionManager` inherits from `Il2CppSystem.Object`, not Unity's `MonoBehaviour`
- Use direct type checking rather than `is` pattern matching for Il2Cpp types
- GameObject finding requires Unity methods like `FindObjectOfType<Character>()`

**Navigation System Design**:
- Disco Elysium uses custom navigation, NOT Unity's EventSystem for menus
- `INavigationReceiver` interface defines `SelectNext()`/`SelectPrevious()` methods
- Multiple implementations: `CollageDialogue`, `SavesPanel`, `InventoryItemsPage`, etc.
- `UIScrollToSelection` tracks actual current selection via `CurrentSelectedGameObject` property

## Navigation System Investigation Guide

When working on menu navigation features, key areas to investigate in the `disco/` decompiled code:

**Core Navigation Classes** (in `Il2CppCollageMode/`):
- `NavigationInputHandler.cs` - Base input handler with `NextAction`/`PreviousAction` properties
- `INavigationReceiver.cs` - Interface defining `SelectNext()`/`SelectPrevious()` methods
- `DialogueNavigationInputHandler.cs` - Dialogue-specific navigation
- `OperationsNavigationInputHandler.cs` - Operations menu navigation

**UI Implementation Classes**:
- `CollageDialogue.cs`, `SavesPanel.cs`, `OperationsPanel.cs` - Implement `INavigationReceiver`
- `InventoryItemsPage.cs`, `THCDetailsPage.cs` - Game-specific navigation implementations

**Selection Tracking**:
- `UIScrollToSelection.cs` (in `UnityEngine.UI.Extensions/`) - Tracks current UI selection
- Look for `CurrentSelectedGameObject` and `LastCheckedGameObject` properties

## Future Development Paths

The current logging system provides foundation for:
- **Tolk integration** for screen reader announcements of both world objects and UI elements
- **Spatial audio cues** using world position coordinates for game objects  
- **Configuration system** for user preferences
- **Enhanced UI navigation** with better selection feedback and alternative input methods