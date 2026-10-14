using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Loomfall
{
    /// <summary>Мост к площадкам: VK Bridge и Яндекс Игры, сохранение нити прогресса, реклама.</summary>
    public static class PlatformBridge
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void LoomfallInit();
        [DllImport("__Internal")] private static extern IntPtr LoomfallStorageGet(string key);
        [DllImport("__Internal")] private static extern void LoomfallStorageSet(string key, string value);
        [DllImport("__Internal")] private static extern void LoomfallFree(IntPtr pointer);
        [DllImport("__Internal")] private static extern void LoomfallShowAd();
#endif

        /// <summary>Подключается к площадке: в VK это VKWebAppInit, в Яндексе — YaGames.init.</summary>
        public static void Init()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            LoomfallInit();
#else
            Debug.Log("[Loomfall] Площадка: локальный запуск");
#endif
        }

        /// <summary>Читает сохранение: в браузере из localStorage и VK, в редакторе из PlayerPrefs.</summary>
        public static string Load(string key, string fallback)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            IntPtr pointer = LoomfallStorageGet(key);
            if (pointer == IntPtr.Zero) return fallback;
            string value = Marshal.PtrToStringUTF8(pointer);
            LoomfallFree(pointer);
            return string.IsNullOrEmpty(value) ? fallback : value;
#else
            return PlayerPrefs.GetString("loomfall." + key, fallback);
#endif
        }

        /// <summary>Пишет сохранение в браузера и в VK.</summary>
        public static void Save(string key, string value)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            LoomfallStorageSet(key, value);
#else
            PlayerPrefs.SetString("loomfall." + key, value);
            PlayerPrefs.Save();
#endif
        }

        /// <summary>Показывает вознаграждаемую рекламу площадки.</summary>
        public static void ShowRewardAd()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            LoomfallShowAd();
#else
            Debug.Log("[Loomfall] Реклама: в редакторе не показывается");
#endif
        }
    }
}