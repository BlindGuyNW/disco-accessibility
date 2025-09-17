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

                // Check if this is a skill check - if so, let SkillCheckTooltipPatches handle everything
                bool isSkillCheck = responseButton.whiteCheck || responseButton.redCheck;
                if (isSkillCheck)
                {
                    return null; // Skip entirely for skill checks
                }

                // Check if we should skip dialog text to avoid interrupting dialog reading
                bool skipDialogText = DialogStateManager.IsInConversation() && DialogStateManager.IsDialogReadingEnabled;

                // Return dialog text for non-skill checks
                if (!string.IsNullOrEmpty(dialogText) && !skipDialogText)
                {
                    return dialogText;
                }

                return null;
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
    }
}