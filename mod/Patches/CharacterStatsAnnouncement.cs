using System;
using System.Text;
using MelonLoader;
using Il2CppSunshine.Metric;
using Il2CppSunshine.Dialogue;
using Il2Cpp;

namespace AccessibilityMod.Patches
{
    /// <summary>
    /// Handles character stats announcements (time, money, experience)
    /// </summary>
    public static class CharacterStatsAnnouncement
    {
        /// <summary>
        /// Announce current character stats including time, money, and experience
        /// </summary>
        public static void AnnounceCharacterStats()
        {
            try
            {
                var sb = new StringBuilder();

                // 1. Time of day
                string timeInfo = GetTimeInfo();
                if (!string.IsNullOrEmpty(timeInfo))
                {
                    sb.Append(timeInfo);
                    sb.Append(". ");
                }
                else
                {
                    MelonLogger.Warning("[CharStats] GetTimeInfo returned null or empty");
                }

                // 2. Money
                string moneyInfo = GetMoneyInfo();
                if (!string.IsNullOrEmpty(moneyInfo))
                {
                    sb.Append(moneyInfo);
                    sb.Append(". ");
                }

                // 3. Experience and level info
                string experienceInfo = GetExperienceInfo();
                if (!string.IsNullOrEmpty(experienceInfo))
                {
                    sb.Append(experienceInfo);
                }

                string announcement = sb.ToString().Trim();
                if (string.IsNullOrEmpty(announcement))
                {
                    announcement = "Unable to retrieve character stats";
                }

                TolkScreenReader.Instance.Speak(announcement, true);
                MelonLogger.Msg($"[CharStats] {announcement}");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error announcing character stats: {ex}");
                TolkScreenReader.Instance.Speak("Error retrieving character stats", true);
            }
        }

        /// <summary>
        /// Get current time information
        /// </summary>
        private static string GetTimeInfo()
        {
            try
            {
                var sb = new StringBuilder();

                // Use the DaytimeLuaFunctions static methods to get time info
                double totalMinutes = DaytimeLuaFunctions.TotalMinutesCount();
                double dayCount = DaytimeLuaFunctions.DayCount();

                // Calculate hours and minutes from total minutes
                int dayMinutes = (int)(totalMinutes % 1440); // Minutes in current day (1440 = 24*60)
                int hours = dayMinutes / 60;
                int minutes = dayMinutes % 60;

                // Calculate day of week (game starts on Monday = 1)
                int dayNumber = (int)dayCount;
                string[] daysOfWeek = { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
                string dayOfWeek = daysOfWeek[(dayNumber - 1) % 7];

                sb.Append(dayOfWeek);
                sb.Append(", ");

                // Format as 12-hour time with AM/PM
                string period = hours >= 12 ? "PM" : "AM";
                int displayHours = hours % 12;
                if (displayHours == 0) displayHours = 12;

                sb.Append($"{displayHours}:{minutes:D2} {period}");

                return sb.ToString();
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error getting time info: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Get money information
        /// </summary>
        private static string GetMoneyInfo()
        {
            try
            {
                var playerChar = PlayerCharacter.Singleton;
                if (playerChar == null)
                {
                    MelonLogger.Warning("PlayerCharacter instance is null");
                    return null;
                }

                int totalCents = playerChar.Money;

                // Convert cents to reál and cents (100 cents = 1 reál)
                int real = totalCents / 100;
                int cents = totalCents % 100;

                var sb = new StringBuilder();

                if (real > 0)
                {
                    sb.Append($"{real} reál");
                    if (real == 1)
                    {
                        sb.Replace("reál", "reál"); // Singular form
                    }

                    if (cents > 0)
                    {
                        sb.Append($" and {cents} cent");
                        if (cents != 1)
                        {
                            sb.Append("s");
                        }
                    }
                }
                else if (cents > 0)
                {
                    sb.Append($"{cents} cent");
                    if (cents != 1)
                    {
                        sb.Append("s");
                    }
                }
                else
                {
                    sb.Append("No money");
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error getting money info: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Get experience and skill point information
        /// </summary>
        private static string GetExperienceInfo()
        {
            try
            {
                var playerChar = PlayerCharacter.Singleton;
                if (playerChar == null)
                {
                    MelonLogger.Warning("PlayerCharacter instance is null");
                    return null;
                }

                var sb = new StringBuilder();

                // Level
                int level = playerChar.Level;
                sb.Append($"Level {level}");

                // Current XP
                int currentXP = playerChar.XpAmount;
                int totalXP = playerChar.TotalXpAmount;

                // Calculate XP to next level (using common RPG formula)
                int xpForNextLevel = CalculateXPForLevel(level + 1);
                int xpForCurrentLevel = CalculateXPForLevel(level);
                int xpToNextLevel = xpForNextLevel - totalXP;

                if (xpToNextLevel > 0)
                {
                    sb.Append($", {totalXP} experience");
                    sb.Append($", {xpToNextLevel} to next level");
                }
                else
                {
                    sb.Append($", {totalXP} experience");
                }

                // Skill points
                int skillPoints = playerChar.SkillPoints;
                if (skillPoints > 0)
                {
                    sb.Append($", {skillPoints} skill point");
                    if (skillPoints != 1)
                    {
                        sb.Append("s");
                    }
                    sb.Append(" available");
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error getting experience info: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Calculate XP required for a given level
        /// Using a simple formula: level * 100
        /// This may need adjustment based on game's actual progression
        /// </summary>
        private static int CalculateXPForLevel(int level)
        {
            // Simple progression formula
            // This is a guess - the actual game may use a different formula
            if (level <= 1) return 0;

            // Each level requires 100 more XP than the previous
            // Level 1: 0 XP
            // Level 2: 100 XP
            // Level 3: 200 XP
            // Level 4: 300 XP
            // etc.
            return (level - 1) * 100;
        }
    }
}