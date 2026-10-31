using UnityEngine;
using UnityEngine.UI;

namespace Echo.UI
{
    /// <summary>
    /// Centralized style definitions and palette constants for Echo Horror Game UI.
    /// Provides standardized dark manor aesthetic palettes, button states, and font styling.
    /// </summary>
    public static class UIStyleConfig
    {
        // Dark manor horror palette
        public static readonly Color BackgroundDim = new Color(0.012f, 0.016f, 0.02f, 0.94f);
        public static readonly Color PanelSurface = new Color(0.045f, 0.06f, 0.065f, 0.96f);
        public static readonly Color PanelBorder = new Color(0.24f, 0.30f, 0.28f, 0.75f);
        public static readonly Color AccentAntiqueBronze = new Color(0.68f, 0.58f, 0.42f, 1f);

        // Text colors
        public static readonly Color TextPrimary = new Color(0.92f, 0.91f, 0.86f, 1f);
        public static readonly Color TextSecondary = new Color(0.68f, 0.72f, 0.70f, 1f);
        public static readonly Color TextMuted = new Color(0.44f, 0.48f, 0.46f, 1f);

        // Feedback / State colors
        public static readonly Color StateError = new Color(0.85f, 0.24f, 0.22f, 1f);
        public static readonly Color StateSuccess = new Color(0.35f, 0.85f, 0.55f, 1f);
        public static readonly Color StateWarning = new Color(0.95f, 0.72f, 0.35f, 1f);
        public static readonly Color StateInfo = new Color(0.35f, 0.78f, 0.80f, 1f);

        // Button interactive state colors
        public static readonly Color ButtonNormal = new Color(0.12f, 0.16f, 0.15f, 1f);
        public static readonly Color ButtonHover = new Color(0.22f, 0.28f, 0.26f, 1f);
        public static readonly Color ButtonPressed = new Color(0.30f, 0.38f, 0.35f, 1f);
        public static readonly Color ButtonSelected = new Color(0.25f, 0.32f, 0.29f, 1f);
        public static readonly Color ButtonDisabled = new Color(0.08f, 0.10f, 0.09f, 0.45f);

        /// <summary>
        /// Generates a standardized ColorBlock for Unity UI Buttons.
        /// </summary>
        public static ColorBlock GetStandardColorBlock()
        {
            var colors = ColorBlock.defaultColorBlock;
            colors.normalColor = ButtonNormal;
            colors.highlightedColor = ButtonHover;
            colors.pressedColor = ButtonPressed;
            colors.selectedColor = ButtonSelected;
            colors.disabledColor = ButtonDisabled;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.1f;
            return colors;
        }

        /// <summary>
        /// Applies standard colors to an existing Button.
        /// </summary>
        public static void ApplyStyle(Button button)
        {
            if (button == null) return;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = GetStandardColorBlock();
        }
    }
}
