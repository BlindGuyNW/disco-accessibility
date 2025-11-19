# Disco Elysium Accessibility Mod

An accessibility mod for the RPG game **Disco Elysium: The Final Cut**, adding screen reader support and keyboard navigation to the game.

## About

This mod uses MelonLoader and Harmony to inject accessibility features into Disco Elysium, making the game playable for blind and visually impaired players. It provides screen reader announcements, keyboard-based navigation, and enhanced UI accessibility.

You can find the game on [Steam](https://store.steampowered.com/app/632470/Disco_Elysium__The_Final_Cut/) or [Good Old Games](https://www.gog.com/game/disco_elysium_the_final_cut).

## Installation

1. **Install Disco Elysium** (tested with the Steam version)
2. **Install MelonLoader** (included with this mod package)
   - It should prompt you to install in the Disco Elysium folder
   - You might need to use NVDA object navigation
3. **Copy the mod DLL**
   - Once MelonLoader is installed, you should have a `Mods` folder inside your Disco Elysium folder
   - Copy `Accessibility Mod.DLL` to this folder
4. **Copy required DLLs**
   - Copy `tolk.dll` and the `NVDA controller client` into the main Disco Elysium folder (above the Mods folder)

## How to Use

Launch Disco Elysium and the game will, after a few moments, start talking. It should read menu items as you navigate up and down the main menu. A controller is recommended for movement, though arrow keys will work.

### Keyboard Commands

Once in-game, the following keyboard commands are available:

#### Navigation & Object Selection

| Key | Function |
|-----|----------|
| `[` | Select NPCs category |
| `]` | Select locations category |
| `\` | Select containers/loot category |
| `=` | Select everything category |
| `.` | Cycle forward within current category |
| `Shift + .` | Cycle backward within current category |
| `,` | Start auto-walking to selected object |
| `/` | Stop auto-walking |
| `;` | Toggle sorting mode (distance vs directional) |
| `'` | Distance-based scene scanner |
| `` ` `` | Announce current UI selection |

#### Waypoints

| Key | Function |
|-----|----------|
| `Ctrl + [` | Focus waypoint list |
| `Alt + [` | Create waypoint at current position |
| `Alt + ]` | Delete current waypoint |
| `Enter` | Confirm waypoint name (during creation) |
| `Escape` | Cancel waypoint creation |

*Note: When waypoints are focused, `.`, `Shift + .`, and `,` work with waypoints instead of objects.*

#### Character Information

| Key | Function |
|-----|----------|
| `H` | Announce health and morale |
| `X` | Announce experience, time of day, and money |
| `O` | Announce officer profile |
| `N` | Read skill description in character sheet |

#### Dialog & Speech

| Key | Function |
|-----|----------|
| `-` | Toggle dialog reading mode (off/on/speaker-only) |
| `R` | Repeat last dialogue line |
| `0` | Toggle orb announcements on/off |
| `8` | Toggle speech interrupt mode |

#### Thought Cabinet (only when in Thought Cabinet view)

| Key | Function |
|-----|----------|
| `Tab` | Read full thought description |
| `F2` | List all available thoughts |
| `F3` | List equipped thoughts |

## Features

- **Screen Reader Support**: Works with NVDA, JAWS, and includes SAPI fallback
- **Braille Display Support**: Automatically outputs to braille displays
- **Smart Object Categorization**: NPCs, locations, containers, and more
- **Waypoint System**: Create, name, and navigate to custom waypoints with persistent storage
- **Dialog Reading**: Multiple reading modes with language support
- **Inventory Navigation**: Full keyboard navigation of inventory screens
- **UI Announcements**: Automatic reading of menus and UI elements
- **Skill Check Announcements**: Hear skill checks and results
- **Notification Vocalization**: Important game notifications are spoken

## Usage Notes

- The system works in tandem with the right stick for interaction
- Once you get close to an object using auto-walk (`,`), use the game's built-in controls to highlight and select what you want to interact with
- The mod prioritizes objects on the same level as your character (you may need to look for stairs)
- Object names are cleaned up from internal game names, but some may still seem unusual
- Dialog reading respects the game's language settings

## Current Limitations

- Character sheet reading is not 100% reliable, though gameplay-essential features work
- Some internal game object names may appear in announcements
- Auto-walking brings you close to objects but won't interact with them automatically

## Feedback

Please provide feedback! I'll do my best to make improvements, though I can't promise to fix everything.

## Technical Details

Built with:
- **MelonLoader** - Unity modding framework for .NET 6.0
- **Harmony** - Runtime method patching
- **Tolk** - Screen reader integration library
- **Il2Cpp interop** - Unity IL2CPP compatibility

For developers, see [CLAUDE.md](CLAUDE.md) for detailed architecture and build instructions.
