using UnityEngine;
using UnityEngine.EventSystems;
using AccessibilityMod.Navigation;
using AccessibilityMod.UI;
using AccessibilityMod.Patches;
using MelonLoader;

namespace AccessibilityMod.Input
{
    public class InputManager
    {
        private readonly SmartNavigationSystem navigationSystem;

        public InputManager(SmartNavigationSystem navigationSystem)
        {
            this.navigationSystem = navigationSystem;
        }

        public void HandleInput()
        {
            // On-demand current selection announcement: Grave/Tilde key (`)
            if (UnityEngine.Input.GetKeyDown(KeyCode.BackQuote))
            {
                AnnounceCurrentSelection();
            }
            
            // Toggle sorting mode: Semicolon (;) - toggles between distance and directional sorting
            if (UnityEngine.Input.GetKeyDown(KeyCode.Semicolon))
            {
                navigationSystem.ToggleSortingMode();
            }
            
            // Distance-based scene scanner: Quote (')
            if (UnityEngine.Input.GetKeyDown(KeyCode.Quote))
            {
                navigationSystem.ScanSceneByDistance();
            }
            
            // Category selection keys (safe punctuation)
            if (UnityEngine.Input.GetKeyDown(KeyCode.LeftBracket))  // [
            {
                navigationSystem.SelectCategory(ObjectCategory.NPCs);
            }
            else if (UnityEngine.Input.GetKeyDown(KeyCode.RightBracket))  // ]
            {
                navigationSystem.SelectCategory(ObjectCategory.Locations);
            }
            else if (UnityEngine.Input.GetKeyDown(KeyCode.Backslash))  // \
            {
                navigationSystem.SelectCategory(ObjectCategory.Loot);
            }
            else if (UnityEngine.Input.GetKeyDown(KeyCode.Equals))  // =
            {
                navigationSystem.SelectCategory(ObjectCategory.Everything);
            }
            
            // Cycle within current category: Period (.)
            if (UnityEngine.Input.GetKeyDown(KeyCode.Period))
            {
                navigationSystem.CycleWithinCategory();
            }
            
            // Navigate to selected object: Comma (,)
            if (UnityEngine.Input.GetKeyDown(KeyCode.Comma))
            {
                navigationSystem.NavigateToSelectedObject();
            }
            
            // Stop automated movement: Slash (/)
            if (UnityEngine.Input.GetKeyDown(KeyCode.Slash))
            {
                navigationSystem.StopMovement();
            }
            
            // Toggle dialog reading mode: Minus/Hyphen (-)
            if (UnityEngine.Input.GetKeyDown(KeyCode.Minus))
            {
                DialogStateManager.ToggleDialogReading();
            }

            // Toggle orb announcements: Zero (0)
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha0))
            {
                OrbTextVocalizationPatches.ToggleOrbAnnouncements();
            }

            // Character status announcement: H key
            if (UnityEngine.Input.GetKeyDown(KeyCode.H))
            {
                Patches.CharacterStatusAnnouncement.AnnounceFullStatus();
            }

            // Character stats announcement (time, money, experience): X key
            if (UnityEngine.Input.GetKeyDown(KeyCode.X))
            {
                Patches.CharacterStatsAnnouncement.AnnounceCharacterStats();
            }

            // Handle Thought Cabinet specific input
            ThoughtCabinetNavigationHandler.HandleThoughtCabinetInput();
        }

        private void AnnounceCurrentSelection()
        {
            try
            {
                var eventSystem = EventSystem.current;
                if (eventSystem != null)
                {
                    var currentSelection = eventSystem.currentSelectedGameObject;
                    if (currentSelection != null)
                    {
                        string speechText = UIElementFormatter.FormatUIElementForSpeech(currentSelection);
                        if (!string.IsNullOrEmpty(speechText))
                        {
                            TolkScreenReader.Instance.Speak(speechText, true); // Interrupt for on-demand announcements
                            MelonLogger.Msg($"[ON-DEMAND] Current selection: {speechText}");
                        }
                        else
                        {
                            TolkScreenReader.Instance.Speak("Current selection has no text", true);
                            MelonLogger.Msg($"[ON-DEMAND] Current selection: {currentSelection.name} (no formatted text)");
                        }
                    }
                    else
                    {
                        TolkScreenReader.Instance.Speak("No UI element selected", true);
                        MelonLogger.Msg("[ON-DEMAND] No UI element currently selected");
                    }
                }
                else
                {
                    TolkScreenReader.Instance.Speak("No event system active", true);
                    MelonLogger.Msg("[ON-DEMAND] No EventSystem found");
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"Error announcing current selection: {ex}");
                TolkScreenReader.Instance.Speak("Error getting current selection", true);
            }
        }
    }
}