using System;
using MelonLoader;
using HarmonyLib;
using UnityEngine;
using AccessibilityMod.Navigation;
using AccessibilityMod.Input;
using AccessibilityMod.UI;
using AccessibilityMod.Inventory;
using AccessibilityMod.Settings;
using AccessibilityMod.Audio;

[assembly: MelonInfo(typeof(AccessibilityMod.AccessibilityMod), "Disco Elysium Accessibility Mod", "1.0.0", "YourName")]
[assembly: MelonGame("ZAUM Studio", "Disco Elysium")]

namespace AccessibilityMod
{
    public class AccessibilityMod : MelonMod
    {
        private SmartNavigationSystem navigationSystem;
        private InputManager inputManager;
        private UINavigationHandler uiNavigationHandler;
        private InventoryNavigationHandler inventoryHandler;

        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Accessibility Mod initializing...");

            // Initialize preferences
            AccessibilityPreferences.Initialize();

            // Initialize Harmony patches
            try
            {
                var harmony = new HarmonyLib.Harmony("com.accessibility.discoelysium");
                harmony.PatchAll();
                LoggerInstance.Msg("Harmony patches applied successfully");
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Failed to apply Harmony patches: {ex}");
            }

            // Initialize Tolk screen reader
            if (TolkScreenReader.Instance.Initialize())
            {
                LoggerInstance.Msg("Tolk initialized successfully!");
                
                string detectedReader = TolkScreenReader.Instance.DetectScreenReader();
                if (!string.IsNullOrEmpty(detectedReader))
                {
                    LoggerInstance.Msg($"Detected screen reader: {detectedReader}");
                }
                else
                {
                    LoggerInstance.Msg("No screen reader detected, using SAPI fallback");
                }
                
                if (TolkScreenReader.Instance.HasSpeech())
                {
                    LoggerInstance.Msg("Speech output available");
                    TolkScreenReader.Instance.Speak("Disco Elysium Accessibility Mod loaded", true);
                }
                
                if (TolkScreenReader.Instance.HasBraille())
                {
                    LoggerInstance.Msg("Braille output available");
                }
            }
            else
            {
                LoggerInstance.Warning("Failed to initialize Tolk - falling back to console logging");
            }

            // Initialize audio-aware announcement manager
            try
            {
                var audioManager = AudioAwareAnnouncementManager.Instance;
                LoggerInstance.Msg("AudioAwareAnnouncementManager initialized successfully");
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Failed to initialize AudioAwareAnnouncementManager: {ex}");
            }

            // Initialize modular systems
            navigationSystem = new SmartNavigationSystem();
            inputManager = new InputManager(navigationSystem);
            uiNavigationHandler = new UINavigationHandler();
            inventoryHandler = InventoryNavigationHandler.Instance;
            inventoryHandler.Initialize();
            
            LoggerInstance.Msg("All accessibility systems initialized successfully");
        }
        
        public override void OnApplicationQuit()
        {
            // Clean up Tolk when the game exits
            TolkScreenReader.Instance.Cleanup();
            LoggerInstance.Msg("Tolk cleaned up");
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            LoggerInstance.Msg($"Scene loaded: {sceneName} (Index: {buildIndex})");
        }
        
        public override void OnUpdate()
        {
            try
            {
                // Update audio-aware announcement manager
                AudioAwareAnnouncementManager.Instance.Update();

                // Handle input through the centralized input manager
                inputManager.HandleInput();

                // Update movement monitoring
                navigationSystem.UpdateMovement();

                // Update UI navigation
                uiNavigationHandler.UpdateUINavigation();

                // Update inventory navigation
                inventoryHandler.Update();

                // Update thought cabinet cache
                ThoughtCabinetNavigationHandler.UpdateThoughtCache();
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error in OnUpdate: {ex}");
            }
        }
    }
}