using System;
using HarmonyLib;
using Il2Cpp;
using Il2CppSunshine.Metric;
using Il2CppSunshine.Dialogue;
using MelonLoader;
using AccessibilityMod;

namespace AccessibilityMod.Patches
{
    /// <summary>
    /// Utility class for announcing character status information
    /// </summary>
    public static class CharacterStatusAnnouncement
    {
        /// <summary>
        /// Announce comprehensive character status including health, morale, and healing items
        /// </summary>
        public static void AnnounceFullStatus()
        {
            try
            {
                // Get current health and morale using game's own calculation methods
                double currentHealth = CharacterLuaFunctions.CurrentEndurance();
                double currentMorale = CharacterLuaFunctions.CurrentVolition();

                // Get player character for max values and healing items
                var world = World.Singleton;
                if (world?.you == null)
                {
                    TolkScreenReader.Instance.Speak($"Health: {currentHealth:F0}, Morale: {currentMorale:F0}");
                    return;
                }

                var characterSheet = world.you;
                var playerCharacter = PlayerCharacter.Singleton;

                // Get max health and morale from skills
                var endurance = characterSheet.GetSkill(SkillType.ENDURANCE);
                var volition = characterSheet.GetSkill(SkillType.VOLITION);

                string announcement = $"Health: {currentHealth:F0}";
                if (endurance != null)
                {
                    announcement += $" of {endurance.value}";
                }

                announcement += $", Morale: {currentMorale:F0}";
                if (volition != null)
                {
                    announcement += $" of {volition.value}";
                }

                // Add healing items information
                if (playerCharacter?.healingPools != null)
                {
                    try
                    {
                        // Get healing charges for health (Endurance) and morale (Volition)
                        int healthCharges = playerCharacter.healingPools.GetHealingChargetsForSkill(SkillType.ENDURANCE);
                        int moraleCharges = playerCharacter.healingPools.GetHealingChargetsForSkill(SkillType.VOLITION);

                        if (healthCharges > 0 || moraleCharges > 0)
                        {
                            announcement += ". Healing items: ";
                            if (healthCharges > 0)
                            {
                                announcement += $"{healthCharges} health";
                                if (moraleCharges > 0)
                                {
                                    announcement += $", {moraleCharges} morale";
                                }
                            }
                            else if (moraleCharges > 0)
                            {
                                announcement += $"{moraleCharges} morale";
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MelonLogger.Warning($"Could not access healing items: {ex.Message}");
                    }
                }

                TolkScreenReader.Instance.Speak(announcement);
                MelonLogger.Msg($"Character status: {announcement}");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error announcing character status: {ex}");
                TolkScreenReader.Instance.Speak("Could not get character status");
            }
        }
    }
}