using MelonLoader;
using AccessibilityMod.UI;

namespace AccessibilityMod.Settings
{
    public static class AccessibilityPreferences
    {
        private static MelonPreferences_Category category;
        private static MelonPreferences_Entry<int> dialogModeEntry;
        private static MelonPreferences_Entry<bool> orbAnnouncementsEntry;
        private static MelonPreferences_Entry<bool> speechInterruptEntry;

        public static void Initialize()
        {
            category = MelonPreferences.CreateCategory("AccessibilityMod");
            category.SetFilePath("UserData/AccessibilityMod.cfg");

            dialogModeEntry = category.CreateEntry<int>("DialogReadingMode", 0,
                "Dialog Reading Mode (0=Disabled, 1=Full, 2=SpeakerOnly)");

            orbAnnouncementsEntry = category.CreateEntry<bool>("OrbAnnouncements", true,
                "Enable orb text announcements");

            speechInterruptEntry = category.CreateEntry<bool>("SpeechInterrupt", false,
                "Enable global speech interrupt");

            MelonLogger.Msg($"[PREFERENCES] Initialized - Dialog: {GetDialogMode()}, Orbs: {GetOrbAnnouncements()}, Interrupt: {GetSpeechInterrupt()}");
        }

        public static DialogReadingMode GetDialogMode()
        {
            return (DialogReadingMode)dialogModeEntry.Value;
        }

        public static void SetDialogMode(DialogReadingMode mode)
        {
            dialogModeEntry.Value = (int)mode;
            category.SaveToFile();
        }

        public static bool GetOrbAnnouncements()
        {
            return orbAnnouncementsEntry.Value;
        }

        public static void SetOrbAnnouncements(bool enabled)
        {
            orbAnnouncementsEntry.Value = enabled;
            category.SaveToFile();
        }

        public static bool GetSpeechInterrupt()
        {
            return speechInterruptEntry.Value;
        }

        public static void SetSpeechInterrupt(bool enabled)
        {
            speechInterruptEntry.Value = enabled;
            category.SaveToFile();
        }
    }
}