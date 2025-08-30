using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace CustomPiperTTS
{
    public static class eSpeakPhonemizer
    {
        [DllImport("espeak-ng", CallingConvention = CallingConvention.Cdecl)]
        private static extern void espeak_SetVoiceByName(string name);

        [DllImport("espeak-ng", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr espeak_TextToPhonemes(ref IntPtr textPtr, int textmode, int phonememode);

        public static string TextToPhonemes(string text, string voice = "ru")
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            try
            {
                espeak_SetVoiceByName(voice);
                IntPtr textPtr = Marshal.StringToHGlobalAnsi(text);
                IntPtr phonemesPtr = espeak_TextToPhonemes(ref textPtr, 1, 1); // textmode=1 (UTF8), phonememode=1 (IPA)
                string phonemes = Marshal.PtrToStringAnsi(phonemesPtr);
                Marshal.FreeHGlobal(textPtr);
                return phonemes ?? string.Empty;
            }
            catch (Exception e)
            {
                Debug.LogError($"eSpeak phonemization failed: {e.Message}");
                return string.Empty;
            }
        }
    }
}