using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Il2CppTMPro;
using Il2Cpp;
using MelonLoader;

namespace AccessibilityMod.UI
{
    /// <summary>
    /// Handles formatting for dialog system elements including responses and conversation text
    /// </summary>
    public static class DialogFormatter
    {
        /// <summary>
        /// Try to get dialog response text from UI object
        /// </summary>
        public static string GetDialogResponseText(GameObject uiObject)
        {
            try
            {
                if (uiObject == null) return null;

                // Check for SunshineResponseButton component (Disco Elysium's dialog choices)
                var responseButton = uiObject.GetComponent<Il2Cpp.SunshineResponseButton>();
                if (responseButton != null)
                {
                    return FormatDialogResponseText(responseButton);
                }

                // Also check if this might be a child of a response button
                var parentResponseButton = uiObject.GetComponentInParent<Il2Cpp.SunshineResponseButton>();
                if (parentResponseButton != null)
                {
                    return FormatDialogResponseText(parentResponseButton);
                }

                return null;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error getting dialog response text: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Format dialog response button text with skill check information
        /// </summary>
        public static string FormatDialogResponseText(Il2Cpp.SunshineResponseButton responseButton)
        {
            try
            {
                if (responseButton == null) return null;

                string dialogText = "";
                string skillCheckInfo = "";

                // Extract the main dialog text from optionText
                if (responseButton.optionText != null)
                {
                    // Try to get text from the textField component
                    if (responseButton.optionText.textField != null && !string.IsNullOrEmpty(responseButton.optionText.textField.text))
                    {
                        dialogText = responseButton.optionText.textField.text.Trim();
                    }
                    // Fallback to originalText property
                    else if (!string.IsNullOrEmpty(responseButton.optionText.originalText))
                    {
                        dialogText = responseButton.optionText.originalText.Trim();
                    }
                }

                // Check for skill check information
                bool isWhiteCheck = responseButton.whiteCheck;
                bool isRedCheck = responseButton.redCheck;

                if (isWhiteCheck || isRedCheck)
                {
                    // Extract skill check details
                    string checkType = isWhiteCheck ? "White Check" : "Red Check";
                    string skillDetails = ExtractSkillCheckDetails(responseButton);
                    
                    // Format skill check info
                    if (!string.IsNullOrEmpty(skillDetails))
                    {
                        skillCheckInfo = $"{checkType} ({skillDetails})";
                    }
                    else
                    {
                        skillCheckInfo = checkType;
                    }
                }

                // Check if we should skip dialog text to avoid interrupting dialog reading
                bool skipDialogText = DialogStateManager.IsInConversation() && DialogStateManager.IsDialogReadingEnabled;
                
                // Combine skill check info with dialog text
                return CombineDialogAndSkillCheck(skillCheckInfo, dialogText, skipDialogText, isWhiteCheck, isRedCheck);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error formatting dialog response text: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Check for confirmation dialog elements
        /// </summary>
        public static string GetConfirmationTextContext(GameObject uiObject)
        {
            try
            {
                var confirmationController = UnityEngine.Object.FindObjectOfType<ConfirmationController>();
                if (confirmationController == null || !confirmationController.IsVisible)
                {
                    return null;
                }
                
                // Check if this is the main text of the confirmation dialog
                var textComponent = uiObject.GetComponent<Text>();
                if (textComponent != null && confirmationController.Text == textComponent)
                {
                    return $"Confirmation: {textComponent.text}";
                }
                
                return null;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error getting confirmation text context: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Check for confirmation dialog button context
        /// </summary>
        public static string GetConfirmationButtonContext(Button button, GameObject uiObject)
        {
            try
            {
                var confirmationController = UnityEngine.Object.FindObjectOfType<ConfirmationController>();
                if (confirmationController == null || !confirmationController.IsVisible)
                {
                    return null;
                }
                
                string message = "";
                if (confirmationController.Text != null)
                {
                    message = confirmationController.Text.text;
                }
                
                // Check if this button is the Confirm button
                if (confirmationController.Confirm == button)
                {
                    return !string.IsNullOrEmpty(message) ? $"Confirm: {message}" : "Confirm Button";
                }
                
                // Check if this button is the Cancel button
                if (confirmationController.Cancel == button)
                {
                    return !string.IsNullOrEmpty(message) ? $"Cancel: {message}" : "Cancel Button";
                }
                
                return null;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error getting confirmation button context: {ex}");
                return null;
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Extract skill check details from response button
        /// </summary>
        private static string ExtractSkillCheckDetails(Il2Cpp.SunshineResponseButton responseButton)
        {
            try
            {
                // Look for skill check details in the response button
                var parent = responseButton.transform.parent;
                if (parent != null)
                {
                    // Look for text components that might contain percentage or difficulty
                    var textComponents = parent.GetComponentsInChildren<Il2CppTMPro.TextMeshProUGUI>();
                    foreach (var textComp in textComponents)
                    {
                        if (textComp != null && !string.IsNullOrEmpty(textComp.text))
                        {
                            string text = textComp.text.Trim();
                            // Look for percentage patterns like "75%" or difficulty like "Very Easy"
                            if (text.Contains("%") || 
                                text.Contains("Easy") || text.Contains("Medium") || text.Contains("Hard") ||
                                text.Contains("Impossible") || text.Contains("Trivial") ||
                                text.Contains("Challenging") || text.Contains("Legendary"))
                            {
                                return text;
                            }
                        }
                    }
                }
                
                return null;
            }
            catch
            {
                // Fallback to basic check type
                return null;
            }
        }

        /// <summary>
        /// Combine dialog text and skill check information intelligently
        /// </summary>
        private static string CombineDialogAndSkillCheck(string skillCheckInfo, string dialogText, bool skipDialogText, bool isWhiteCheck, bool isRedCheck)
        {
            if (!string.IsNullOrEmpty(skillCheckInfo) && !string.IsNullOrEmpty(dialogText))
            {
                if (skipDialogText)
                {
                    // Only announce skill check info, skip dialog text to avoid interruption
                    return skillCheckInfo;
                }
                else
                {
                    // Check if dialog text already contains skill check details to avoid duplication
                    if (dialogText.Contains("[") && dialogText.Contains("]") && dialogText.Contains("-"))
                    {
                        // Dialog text already has skill check details, just prefix with check type
                        string checkType = isWhiteCheck ? "White Check" : "Red Check";
                        return $"{checkType}: {dialogText}";
                    }
                    else
                    {
                        return $"{skillCheckInfo}: {dialogText}";
                    }
                }
            }
            else if (!string.IsNullOrEmpty(skillCheckInfo))
            {
                return skillCheckInfo;
            }
            else if (!string.IsNullOrEmpty(dialogText))
            {
                if (skipDialogText)
                {
                    // Skip dialog text entirely when dialog reading is active
                    return null;
                }
                else
                {
                    return $"Dialog: {dialogText}";
                }
            }

            return null;
        }

        #endregion
    }
}