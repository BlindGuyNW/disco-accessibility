using System;
using UnityEngine;
using Il2CppFortressOccident;
using MelonLoader;

namespace AccessibilityMod.Utils
{
    public static class GameObjectUtils
    {
        public static Character GetPlayerCharacter()
        {
            try
            {
                MelonLogger.Msg("[GAMEOBJECT] Searching for player character...");
                
                // Method 1: Try Character.Main static property
                try
                {
                    var mainChar = Character.Main;
                    if (mainChar != null)
                    {
                        MelonLogger.Msg($"[GAMEOBJECT] Found Character.Main: {mainChar.name}");
                        // Test that the object is valid for Il2Cpp calls
                        var testStatus = mainChar.movementStatus;
                        MelonLogger.Msg($"[GAMEOBJECT] Character.Main status test: {testStatus}");
                        return mainChar;
                    }
                    else
                    {
                        MelonLogger.Msg("[GAMEOBJECT] Character.Main is null");
                    }
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"[GAMEOBJECT] Error accessing Character.Main: {ex}");
                }
                
                // Method 2: Find by UnityEngine.Object
                try
                {
                    var foundChar = UnityEngine.Object.FindObjectOfType<Character>();
                    if (foundChar != null)
                    {
                        MelonLogger.Msg($"[GAMEOBJECT] Found Character by type: {foundChar.name}");
                        // Test that the object is valid for Il2Cpp calls
                        var testStatus = foundChar.movementStatus;
                        MelonLogger.Msg($"[GAMEOBJECT] Found Character status test: {testStatus}");
                        return foundChar;
                    }
                    else
                    {
                        MelonLogger.Msg("[GAMEOBJECT] FindObjectOfType<Character>() returned null");
                    }
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"[GAMEOBJECT] Error finding Character by type: {ex}");
                }
                
                MelonLogger.Error("[GAMEOBJECT] Failed to find valid Character object");
                return null;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[GAMEOBJECT] Error in GetPlayerCharacter: {ex}");
                return null;
            }
        }

        public static Vector3 GetPlayerPosition()
        {
            var character = GetPlayerCharacter();
            if (character == null) return Vector3.zero;
            return character.transform.position;
        }

        public static bool IsPlayerMoving()
        {
            var character = GetPlayerCharacter();
            if (character == null) return false;
            
            try
            {
                return character.movementStatus == Character.MovementStatus.MOVING;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[GAMEOBJECT] Error checking movement status: {ex}");
                return false;
            }
        }

        public static Character.MovementStatus GetPlayerMovementStatus()
        {
            var character = GetPlayerCharacter();
            if (character == null) return Character.MovementStatus.IDLE;
            
            try
            {
                return character.movementStatus;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[GAMEOBJECT] Error getting movement status: {ex}");
                return Character.MovementStatus.IDLE;
            }
        }
    }
}