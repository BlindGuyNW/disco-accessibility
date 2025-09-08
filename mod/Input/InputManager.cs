using UnityEngine;
using AccessibilityMod.Navigation;

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
            // Test hotkey: Semicolon to scan entire scene registry
            if (UnityEngine.Input.GetKeyDown(KeyCode.Semicolon))
            {
                navigationSystem.TestRegistryAccess();
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
        }
    }
}