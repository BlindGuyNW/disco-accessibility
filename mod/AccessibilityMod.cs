using System;
using MelonLoader;
using HarmonyLib;
using UnityEngine;
using AccessibilityMod.Navigation;
using AccessibilityMod.Input;
using AccessibilityMod.UI;

[assembly: MelonInfo(typeof(AccessibilityMod.AccessibilityMod), "Disco Elysium Accessibility Mod", "1.0.0", "YourName")]
[assembly: MelonGame("ZAUM Studio", "Disco Elysium")]

namespace AccessibilityMod
{
    public class AccessibilityMod : MelonMod
    {
        private SmartNavigationSystem navigationSystem;
        private InputManager inputManager;
        private UINavigationHandler uiNavigationHandler;

        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Accessibility Mod initializing...");
            
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
            
            // Move mouse cursor to safe position to prevent UI interference
            try
            {
                // Move mouse to bottom-right corner where there's typically no UI
                Cursor.SetCursor(null, new Vector2(Screen.width - 10, Screen.height - 10), CursorMode.Auto);
                // Also try to set the actual mouse position if possible
                // Input.mousePosition is read-only, so we can't set it directly
                LoggerInstance.Msg("Mouse cursor repositioned to safe area");
            }
            catch (Exception ex)
            {
                LoggerInstance.Warning($"Could not reposition mouse cursor: {ex.Message}");
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

            // Initialize modular systems
            navigationSystem = new SmartNavigationSystem();
            inputManager = new InputManager(navigationSystem);
            uiNavigationHandler = new UINavigationHandler();
            
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
            
            // Reposition mouse cursor when scene loads to prevent UI interference
            try
            {
                // Move to corner where there's typically no UI
                Cursor.SetCursor(null, new Vector2(Screen.width - 10, Screen.height - 10), CursorMode.Auto);
                LoggerInstance.Msg("Mouse cursor repositioned after scene load");
            }
            catch (Exception ex)
            {
                LoggerInstance.Warning($"Could not reposition mouse after scene load: {ex.Message}");
            }
        }
        
        public override void OnUpdate()
        {
            try
            {
                // Handle input through the centralized input manager
                inputManager.HandleInput();
                
                // Update movement monitoring
                navigationSystem.UpdateMovement();
                
                // Update UI navigation
                uiNavigationHandler.UpdateUINavigation();
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error in OnUpdate: {ex}");
            }
        }
    }
}