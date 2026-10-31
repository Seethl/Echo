using UnityEngine;

namespace Echo.Menu
{
    public static class GameSettings
    {
        private const string VolumeKey = "Echo.Settings.MasterVolume";
        private const string FullscreenKey = "Echo.Settings.Fullscreen";
        private const string SensitivityKey = "Echo.Settings.MouseSensitivityMultiplier";

        public static float MasterVolume { get; private set; } = 1f;
        public static float SensitivityMultiplier { get; private set; } = 1f;
        public static bool Fullscreen { get; private set; }

        public static void Load()
        {
            MasterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1f));
            SensitivityMultiplier = Mathf.Clamp(PlayerPrefs.GetFloat(SensitivityKey, 1f), 0.1f, 3f);
            Fullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) != 0;
            AudioListener.volume = MasterVolume;
            if (PlayerPrefs.HasKey(FullscreenKey))
                ApplyFullscreen();
        }

        public static void SetMasterVolume(float value)
        {
            MasterVolume = Mathf.Clamp01(value);
            AudioListener.volume = MasterVolume;
            PlayerPrefs.SetFloat(VolumeKey, MasterVolume);
            PlayerPrefs.Save();
        }

        public static void SetSensitivity(float value)
        {
            SensitivityMultiplier = Mathf.Clamp(value, 0.1f, 3f);
            PlayerPrefs.SetFloat(SensitivityKey, SensitivityMultiplier);
            PlayerPrefs.Save();
        }

        public static void SetFullscreen(bool value)
        {
            Fullscreen = value;
            ApplyFullscreen();
            PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }

        private static void ApplyFullscreen()
        {
            Screen.fullScreenMode = Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        }
    }
}
