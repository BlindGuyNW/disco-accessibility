using System;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using AccessibilityMod.Settings;
using MelonLoader;

namespace AccessibilityMod
{
    /// <summary>
    /// Wrapper around the Tolk screen reader integration library
    /// </summary>
    internal class TolkScreenReader
    {
        private static TolkScreenReader instance;
        private bool isInitialized = false;
        private bool suppressAnnouncements = false;
        private bool globalInterruptEnabled = false;

        public static TolkScreenReader Instance
        {
            get
            {
                if (instance == null)
                    instance = new TolkScreenReader();
                return instance;
            }
        }

        public bool IsInitialized => isInitialized;

        public bool Initialize()
        {
            try
            {
                Tolk.TrySAPI(true);  // Allow SAPI as fallback
                Tolk.PreferSAPI(false);  // Prefer real screen readers

                Tolk.Load();
                isInitialized = Tolk.IsLoaded();

                // Load settings after initialization
                if (isInitialized)
                {
                    globalInterruptEnabled = AccessibilityPreferences.GetSpeechInterrupt();
                    MelonLogger.Msg($"[TOLK] Speech interrupt loaded from preferences: {globalInterruptEnabled}");
                }

                return isInitialized;
            }
            catch (Exception)
            {
                isInitialized = false;
                return false;
            }
        }

        public string DetectScreenReader()
        {
            if (!isInitialized) return null;
            return Tolk.DetectScreenReader();
        }

        public bool HasSpeech()
        {
            if (!isInitialized) return false;
            return Tolk.HasSpeech();
        }

        public bool HasBraille()
        {
            if (!isInitialized) return false;
            return Tolk.HasBraille();
        }

        private string StripHtmlTags(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            
            // Remove Unity color tags while preserving content
            // This removes <color=...> and </color> tags but keeps the text inside
            text = Regex.Replace(text, @"<color[^>]*>", "", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"</color>", "", RegexOptions.IgnoreCase);
            
            // Remove other common Unity/HTML tags while preserving content
            text = Regex.Replace(text, @"</?[^>]+>", "");
            
            return text;
        }

        public bool Speak(string text, bool interrupt = false)
        {
            return SendSpeech(text, interrupt, true);
        }

        private bool SendSpeech(string text, bool interrupt, bool respectSuppression)
        {
            if (!isInitialized || string.IsNullOrEmpty(text)) return false;
            if (respectSuppression && suppressAnnouncements) return false;

            text = StripHtmlTags(text);

            bool effectiveInterrupt = interrupt || globalInterruptEnabled;

            if (effectiveInterrupt && Tolk.IsSpeaking())
            {
                Tolk.Silence();
            }

            return Tolk.Output(text, effectiveInterrupt);
        }

        public void ToggleGlobalInterrupt()
        {
            globalInterruptEnabled = !globalInterruptEnabled;
            string status = globalInterruptEnabled ? "enabled" : "disabled";
            // Always interrupt this announcement so user gets immediate feedback
            Speak($"Speech interrupt {status}", true);

            // Save the new setting
            AccessibilityPreferences.SetSpeechInterrupt(globalInterruptEnabled);
            MelonLogger.Msg($"[TOLK] Speech interrupt {status} and saved");
        }

        public bool IsGlobalInterruptEnabled()
        {
            return globalInterruptEnabled;
        }

        public void SuppressAnnouncements(bool suppress)
        {
            suppressAnnouncements = suppress;
        }

        public bool Output(string text, bool interrupt = false)
        {
            return SendSpeech(text, interrupt, false);
        }

        public bool Braille(string text)
        {
            if (!isInitialized || string.IsNullOrEmpty(text)) return false;
            text = StripHtmlTags(text);
            return Tolk.Braille(text);
        }

        public bool IsSpeaking()
        {
            if (!isInitialized) return false;
            return Tolk.IsSpeaking();
        }

        public bool Silence()
        {
            if (!isInitialized) return false;
            return Tolk.Silence();
        }

        public void Shutdown()
        {
            if (isInitialized)
            {
                Tolk.Unload();
                isInitialized = false;
            }
        }

        public void Cleanup()
        {
            Shutdown();
        }
    }

    // Official Tolk .NET wrapper class
    public sealed class Tolk 
    {
        [DllImport("Tolk.dll", CharSet=CharSet.Unicode, CallingConvention=CallingConvention.Cdecl)]
        private static extern void Tolk_Load();
        [DllImport("Tolk.dll", CharSet=CharSet.Unicode, CallingConvention=CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool Tolk_IsLoaded();
        [DllImport("Tolk.dll", CharSet=CharSet.Unicode, CallingConvention=CallingConvention.Cdecl)]
        private static extern void Tolk_Unload();
        [DllImport("Tolk.dll", CharSet=CharSet.Unicode, CallingConvention=CallingConvention.Cdecl)]
        private static extern void Tolk_TrySAPI(
            [MarshalAs(UnmanagedType.I1)]bool trySAPI);
        [DllImport("Tolk.dll", CharSet=CharSet.Unicode, CallingConvention=CallingConvention.Cdecl)]
        private static extern void Tolk_PreferSAPI(
            [MarshalAs(UnmanagedType.I1)]bool preferSAPI);
        [DllImport("Tolk.dll", CharSet=CharSet.Unicode, CallingConvention=CallingConvention.Cdecl)]
        private static extern IntPtr Tolk_DetectScreenReader();
        [DllImport("Tolk.dll", CharSet=CharSet.Unicode, CallingConvention=CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool Tolk_HasSpeech();
        [DllImport("Tolk.dll", CharSet=CharSet.Unicode, CallingConvention=CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool Tolk_HasBraille();
        [DllImport("Tolk.dll", CharSet=CharSet.Unicode, CallingConvention=CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool Tolk_Output(
            [MarshalAs(UnmanagedType.LPWStr)]String str,
            [MarshalAs(UnmanagedType.I1)]bool interrupt);
        [DllImport("Tolk.dll", CharSet=CharSet.Unicode, CallingConvention=CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool Tolk_Speak(
            [MarshalAs(UnmanagedType.LPWStr)]String str,
            [MarshalAs(UnmanagedType.I1)]bool interrupt);
        [DllImport("Tolk.dll", CharSet=CharSet.Unicode, CallingConvention=CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool Tolk_Braille(
            [MarshalAs(UnmanagedType.LPWStr)]String str);
        [DllImport("Tolk.dll", CharSet=CharSet.Unicode, CallingConvention=CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool Tolk_IsSpeaking();
        [DllImport("Tolk.dll", CharSet=CharSet.Unicode, CallingConvention=CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool Tolk_Silence();

        // Prevent construction
        private Tolk() {}

        public static void Load() { Tolk_Load(); }
        public static bool IsLoaded() { return Tolk_IsLoaded(); }
        public static void Unload() { Tolk_Unload(); }
        public static void TrySAPI(bool trySAPI) { Tolk_TrySAPI(trySAPI); }
        public static void PreferSAPI(bool preferSAPI) { Tolk_PreferSAPI(preferSAPI); }
        // Prevent the marshaller from freeing the unmanaged string
        public static String DetectScreenReader() { return Marshal.PtrToStringUni(Tolk_DetectScreenReader()); }
        public static bool HasSpeech() { return Tolk_HasSpeech(); }
        public static bool HasBraille() { return Tolk_HasBraille(); }
        public static bool Output(String str, bool interrupt = false) { return Tolk_Output(str, interrupt); }
        public static bool Speak(String str, bool interrupt = false) { return Tolk_Speak(str, interrupt); }
        public static bool Braille(String str) { return Tolk_Braille(str); }
        public static bool IsSpeaking() { return Tolk_IsSpeaking(); }
        public static bool Silence() { return Tolk_Silence(); }
    }
}