# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a MelonLoader mod for Disco Elysium that provides comprehensive accessibility features for visually impaired players. The mod transforms the game's navigation from visual-dependent interactions into audio-guided, automated systems. Core features include smart object categorization, automated character movement using the game's pathfinding, scene-wide object registry access, and screen reader integration via Tolk.

The mod bypasses the game's limited visual selection system (5 nearby objects) by accessing the global MouseOverHighlight registry (300+ scene objects) and provides intelligent categorization, filtering, and automated navigation.

## Architecture

### Core Components

**MelonLoader Integration**: Uses MelonLoader framework with correct game identifiers (ZAUM Studio / Disco Elysium).

**Smart Navigation System**: The primary accessibility feature providing categorized object selection and automated movement:

**Object Registry Access**:
- `MouseOverHighlight.registry` - Global scene object registry (300+ objects vs 5 from InteractableSelectionManager)
- Bypasses game's limited visual selection range for scene-wide awareness
- Provides foundation for virtual exploration without character movement

**Categorized Object Selection**:
- **NPCs** (`[` key): Interactive characters excluding Kim (who follows player)  
- **Locations** (`]` key): Doors, exits, vehicles, story objects
- **Loot** (`\` key): Containers, money, searchable items with clutter filtering
- **Everything** (`=` key): Fallback category with distance limiting

**Automated Movement System**:
- Uses `Character.SetDestination()` with proper Il2Cpp parameter marshalling
- Real-time movement monitoring with distance announcements
- Handles pathfinding failures gracefully with directional guidance fallback
- Emergency stop functionality (`/` key)

**Legacy Systems** (still functional):
- UI navigation tracking via `INavigationReceiver` patches
- Object detection for `OrbUiElement` skill checks and `MouseOverHighlight` objects
- Menu navigation monitoring for screen reader integration

### Key Game Classes

**Critical for Navigation System**:
- `MouseOverHighlight` - Contains global registry of all interactable objects (`MouseOverHighlight.registry`)
- `Character` - Player character with `SetDestination()` method for automated movement and `movementStatus` tracking
- `GameEntity` - Provides richer object names via `GetFirstActive()` method
- `MovementMode` (Il2CppSunshine) - Enum for movement types (WALK, AUTOMATIC, etc.)

**Core Interaction Classes**:
- `InteractableSelectionManager` - Limited-range selection system (legacy, still used for tracking)
- `CommonPadInteractable` - Wrapper for interactable objects
- `CharacterAnalogueControl` - Contains movement validation and pathfinding utilities

**UI Navigation Classes** (for future development):
- `INavigationReceiver` - Interface for custom menu navigation  
- `NavigationInputHandler` - Base class for controller input handling
- `UIScrollToSelection` - Unity extension for tracking UI selection
- Dialog-specific handlers: `DialogueNavigationInputHandler`, `OperationsNavigationInputHandler`

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

**Smart Navigation System**:
1. Launch Disco Elysium (MelonLoader loads the mod automatically)
2. Press categorization hotkeys to browse objects:
   - `[` - NPCs: Should show 1-3 characters excluding Kim
   - `]` - Locations: Should show doors, exits, vehicles (typically 2-6 objects)  
   - `\` - Loot: Should show containers, money with clutter filtering (typically 2-8 objects)
   - `=` - Everything: Should show nearest 10 objects with distance limiting
3. Press hotkey again to navigate to selected object with automated movement
4. Watch console for movement progress and pathfinding status
5. Use `/` for emergency stop if needed

**World Object Detection** (Legacy):
1. Move cursor over objects in-game to see detection logs in console
2. Verify both Orb and MouseOverHighlight object types are detected

**Menu Navigation Testing** (Legacy):
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

### Smart Navigation System Architecture

**Object Categorization Logic**:
```csharp
public enum ObjectCategory
{
    NPCs = 1,           // Interactive NPCs (excluding Kim)
    Locations = 2,      // Doors, exits, vehicles, story objects
    Loot = 3,          // Containers, money, pickuppable items
    Everything = 4      // Fallback category with distance limiting
}
```

**Registry Access Pattern**:
- Access global scene objects via `MouseOverHighlight.registry` (Il2CppSystem.Collections.Generic.List)
- Filter by distance, category, and interaction relevance
- Enhanced object naming via `GameEntity.GetFirstActive()` method
- Clutter filtering excludes "Trash", generic containers, and environmental decorations

**Automated Movement Implementation**:
```csharp
// Character detection with robust error handling
private Character GetPlayerCharacter()
{
    return Object.FindObjectOfType<Character>();
}

// Il2Cpp parameter marshalling for SetDestination
var nullableHeading = new Il2CppSystem.Nullable<float>();
character.SetDestination(destination, nullableHeading, MovementMode.WALK, false);
```

**Movement Monitoring System**:
- Real-time distance tracking with progress announcements
- Movement state detection via `character.movementStatus` enum
- Pathfinding failure detection and fallback guidance
- Emergency stop functionality with movement interruption

**Hotkey Safety**:
- Uses punctuation keys (`[`, `]`, `\`, `=`, `/`) to avoid game function conflicts
- Avoids letter keys that may conflict with dialogue choices or game commands
- Safe key selection verified through game testing

### Assembly References
The mod requires specific Il2Cpp assemblies. Key dependencies include:
- `Unity.TextMeshPro.dll` - Required for TextMeshPro component access in UI elements
- `Il2CppInControl.dll` - Required for controller input types
- `Il2CppSunshine.dll` - Required for MovementMode enum and character control types
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

With the smart navigation system now fully implemented, potential areas for accessibility expansion include:

**Dialogue System Accessibility**:
- Automated navigation through dialogue trees using Tolk announcements
- Integration with `DialogueNavigationInputHandler` and `CollageDialogue` classes
- Speech-to-text for dialogue choices or audio cue navigation
- Core to gameplay since most of Disco Elysium consists of dialogue interactions

**Character Sheet and Inventory Systems**:
- Voice navigation for skill point allocation and character progression
- Automated inventory organization and item identification
- Integration with `InventoryItemsPage` and character stat panels
- Audio feedback for item properties and character development choices

**Page System Navigation**:
- Enhanced navigation for the game's "page" system (journal, thoughts, tasks)
- Screen reader integration for thought cabinet and case progression
- Audio cues for completed vs. incomplete tasks and investigation progress

**Advanced Features**:
- **Configuration system** for user-customizable hotkeys and preferences
- **Spatial audio cues** using world position coordinates for enhanced environmental awareness  
- **Voice command integration** for hands-free navigation and interaction
- **Save/load state management** with audio feedback for game state awareness