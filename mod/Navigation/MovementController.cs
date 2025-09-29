using System;
using UnityEngine;
using Il2CppFortressOccident;
using Il2CppSunshine;
using AccessibilityMod.Utils;
using MelonLoader;

namespace AccessibilityMod.Navigation
{
    public class MovementController
    {
        private bool isMonitoringMovement = false;
        private Character monitoredCharacter = null;
        private Vector3 movementDestination;
        private string movementTargetName = "";
        private float lastDistanceAnnouncement = 0f;

        public bool IsMoving => isMonitoringMovement;
        public string CurrentTarget => movementTargetName;

        public event Action<string> OnMovementCompleted;
        public event Action<string> OnMovementFailed;
        public event Action<string, float> OnMovementProgress;

        public bool TryNavigateToPosition(Vector3 destination, string objectName)
        {
            try
            {
                MelonLogger.Msg("[MOVEMENT] Attempting to navigate to position...");
                
                var character = GameObjectUtils.GetPlayerCharacter();
                if (character == null)
                {
                    MelonLogger.Error("[MOVEMENT] Could not find valid Character for automated movement");
                    return false;
                }

                Vector3 playerPos = character.transform.position;
                float distance = Vector3.Distance(playerPos, destination);
                
                // Check current movement status
                try
                {
                    var currentStatus = character.movementStatus;
                    MelonLogger.Msg($"[MOVEMENT] Current movement status: {currentStatus}");
                    
                    if (currentStatus == Character.MovementStatus.MOVING)
                    {
                        TolkScreenReader.Instance.Speak("Character is already moving. Please wait.", true);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"[MOVEMENT] Error checking movement status: {ex}");
                    return false;
                }

                // Try pathfinding with robust fallbacks
                if (TrySetDestination(character, destination, objectName, distance))
                {
                    return true;
                }

                // Fallback to directional guidance
                GiveDirectionalGuidance(playerPos, destination, objectName);
                return false;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[MOVEMENT] Navigation error: {ex}");
                OnMovementFailed?.Invoke($"Navigation failed: {ex.Message}");
                return false;
            }
        }

        private bool TrySetDestination(Character character, Vector3 destination, string objectName, float distance)
        {
            try
            {
                MelonLogger.Msg($"[MOVEMENT] Calling SetDestination for {objectName} at {destination}");
                
                // Create proper Il2Cpp nullable float
                var nullableHeading = new Il2CppSystem.Nullable<float>();
                
                // Use RUN mode for faster travel to targets
                character.SetDestination(destination, nullableHeading, MovementMode.RUN, false);
                
                MelonLogger.Msg($"[MOVEMENT] SetDestination succeeded for {objectName}");
                TolkScreenReader.Instance.Speak(
                    $"Running to {objectName}, {distance:F1} meters away. Character will move automatically.", true);
                
                // Start monitoring movement progress
                StartMovementMonitoring(character, destination, objectName);
                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[MOVEMENT] SetDestination failed: {ex}");
                
                // Try alternative approach with explicit heading
                try
                {
                    MelonLogger.Msg("[MOVEMENT] Trying SetDestination with explicit heading...");
                    var heading = new Il2CppSystem.Nullable<float>(0f); // Face north
                    character.SetDestination(destination, heading, MovementMode.RUN, false);
                    
                    MelonLogger.Msg($"[MOVEMENT] SetDestination with heading succeeded for {objectName}");
                    TolkScreenReader.Instance.Speak(
                        $"Running to {objectName}, {distance:F1} meters away. Character will move automatically.", true);
                    
                    StartMovementMonitoring(character, destination, objectName);
                    return true;
                }
                catch (Exception ex2)
                {
                    MelonLogger.Error($"[MOVEMENT] SetDestination with heading also failed: {ex2}");
                    return false;
                }
            }
        }

        private void StartMovementMonitoring(Character character, Vector3 destination, string objectName)
        {
            isMonitoringMovement = true;
            monitoredCharacter = character;
            movementDestination = destination;
            movementTargetName = objectName;
            lastDistanceAnnouncement = Time.time;
            
            MelonLogger.Msg($"[MOVEMENT] Started monitoring movement to {objectName}");
        }

        public void UpdateMovementProgress()
        {
            if (!isMonitoringMovement || monitoredCharacter == null)
                return;

            try
            {
                var status = monitoredCharacter.movementStatus;
                var currentPos = monitoredCharacter.transform.position;
                float currentDistance = Vector3.Distance(currentPos, movementDestination);
                
                // Check if movement completed
                if (status == Character.MovementStatus.IDLE || status == Character.MovementStatus.COMPLETED)
                {
                    string completionMessage;
                    if (currentDistance < 2.0f) // Successfully reached destination
                    {
                        completionMessage = $"Arrived at {movementTargetName}";
                        MelonLogger.Msg($"[MOVEMENT] Successfully arrived at {movementTargetName}");
                    }
                    else
                    {
                        completionMessage = $"Movement stopped. {currentDistance:F1} meters from {movementTargetName}";
                        MelonLogger.Msg($"[MOVEMENT] Movement stopped at {currentDistance:F1}m from target");
                    }
                    
                    OnMovementCompleted?.Invoke(completionMessage);
                    TolkScreenReader.Instance.Speak(completionMessage, true);
                    isMonitoringMovement = false;
                    return;
                }
                
                // Check if movement failed or got stuck
                if (status == Character.MovementStatus.BROKEN)
                {
                    string failureMessage = $"Movement failed. Unable to reach {movementTargetName}";
                    OnMovementFailed?.Invoke(failureMessage);
                    TolkScreenReader.Instance.Speak(failureMessage, true);
                    MelonLogger.Msg("[MOVEMENT] Movement status is BROKEN");
                    isMonitoringMovement = false;
                    return;
                }
                
                // Provide periodic distance updates (every 3 seconds while moving)
                if (status == Character.MovementStatus.MOVING && Time.time - lastDistanceAnnouncement > 3.0f)
                {
                    if (currentDistance > 5.0f) // Only announce if still far away
                    {
                        string progressMessage = $"{currentDistance:F0} meters to {movementTargetName}";
                        OnMovementProgress?.Invoke(movementTargetName, currentDistance);
                        TolkScreenReader.Instance.Speak(progressMessage, true);
                        lastDistanceAnnouncement = Time.time;
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[MOVEMENT] Movement monitoring error: {ex}");
                isMonitoringMovement = false;
            }
        }

        public void StopMovement()
        {
            if (!isMonitoringMovement)
            {
                TolkScreenReader.Instance.Speak("No active movement to stop", true);
                return;
            }
            
            try
            {
                if (monitoredCharacter != null)
                {
                    // Stop character movement by setting destination to current position
                    monitoredCharacter.SetDestination(monitoredCharacter.transform.position, null, MovementMode.AUTOMATIC, false);
                    MelonLogger.Msg("[MOVEMENT] Automated movement stopped by user");
                }
                
                TolkScreenReader.Instance.Speak("Movement stopped", true);
                isMonitoringMovement = false;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[MOVEMENT] Error stopping movement: {ex}");
                TolkScreenReader.Instance.Speak("Error stopping movement", true);
            }
        }

        private void GiveDirectionalGuidance(Vector3 start, Vector3 destination, string objectName)
        {
            float distance = Vector3.Distance(start, destination);
            string direction = DirectionCalculator.GetCardinalDirection(start, destination);
            
            string guidance = $"Direct path to {objectName}: {distance:F1} meters {direction}. " +
                            $"Face {direction} and walk forward.";
            
            MelonLogger.Msg($"[MOVEMENT] Providing directional guidance: {guidance}");
            TolkScreenReader.Instance.Speak(guidance, true);
        }
    }
}
