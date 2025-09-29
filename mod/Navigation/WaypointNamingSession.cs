using System;
using UnityEngine;

namespace AccessibilityMod.Navigation
{
    internal class WaypointNamingSession
    {
        private readonly Vector3 position;
        private readonly string defaultName;
        private readonly string sceneName;
        private readonly Action<string, Vector3, string, bool> onCompleted;
        private readonly Action onCancelled;
        private string currentInput = string.Empty;
        private bool isActive = true;

        public WaypointNamingSession(
            Vector3 position,
            string defaultName,
            string sceneName,
            Action<string, Vector3, string, bool> onCompleted,
            Action onCancelled)
        {
            this.position = position;
            this.defaultName = defaultName;
            this.sceneName = sceneName;
            this.onCompleted = onCompleted;
            this.onCancelled = onCancelled;
        }

        public bool IsActive => isActive;
        public string CurrentInput => currentInput;
        public string DefaultName => defaultName;
        public string SceneName => sceneName;

        public void HandleInput(string inputCharacters)
        {
            if (!isActive || string.IsNullOrEmpty(inputCharacters))
                return;

            foreach (char character in inputCharacters)
            {
                if (character == '\b')
                {
                    if (currentInput.Length > 0)
                    {
                        currentInput = currentInput.Substring(0, currentInput.Length - 1);
                    }
                }
                else if (character == '\r' || character == '\n')
                {
                    Confirm();
                }
                else if (!char.IsControl(character))
                {
                    if (currentInput.Length < 64)
                    {
                        currentInput += character;
                    }
                }
            }
        }

        public void Confirm()
        {
            if (!isActive)
                return;

            isActive = false;
            bool usedDefault = string.IsNullOrWhiteSpace(currentInput);
            string finalName = usedDefault ? defaultName : currentInput.Trim();
            onCompleted?.Invoke(finalName, position, sceneName, usedDefault);
        }

        public void Cancel()
        {
            if (!isActive)
                return;

            isActive = false;
            onCancelled?.Invoke();
        }
    }
}
