using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Echo.UI
{
    /// <summary>
    /// Standalone Combination Lock UI mockup and component.
    /// Provides 3-digit display slots, on-screen keypad buttons (0-9, Clear, Backspace, Submit),
    /// full keyboard and Numpad bindings, error feedback animations, and responsive screen-safe layout.
    /// Does not re-implement code validation logic; invokes OnSubmitCode callback with entered string.
    /// </summary>
    [DisallowMultipleComponent]
    public class CombinationLockUI : MonoBehaviour
    {
        [Header("Root & Backdrop")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Image modalBackdrop;

        [Header("Headers & Feedback")]
        [SerializeField] private Text titleText;
        [SerializeField] private Text instructionText;
        [SerializeField] private Text feedbackText;

        [Header("Digit Display Slots")]
        [Tooltip("Displays for the entered code digits (e.g. 3 slots)")]
        [SerializeField] private Text[] digitSlotTexts = new Text[3];
        [SerializeField] private Image[] digitSlotBorders = new Image[3];

        [Header("Keypad Buttons")]
        [Tooltip("Direct keypad buttons for 0 through 9")]
        [SerializeField] private Button[] numberButtons = new Button[10];
        [SerializeField] private Button clearButton;
        [SerializeField] private Button backspaceButton;
        [SerializeField] private Button submitButton;
        [SerializeField] private Button closeButton;

        [Header("Styling Colors")]
        [SerializeField] private Color normalBorderColor = new Color(0.3f, 0.38f, 0.35f, 0.8f);
        [SerializeField] private Color activeBorderColor = new Color(0.85f, 0.75f, 0.55f, 1f);
        [SerializeField] private Color errorBorderColor = new Color(0.85f, 0.25f, 0.22f, 1f);
        [SerializeField] private Color successBorderColor = new Color(0.35f, 0.85f, 0.55f, 1f);
        [SerializeField] private Color normalTextColor = new Color(0.9f, 0.88f, 0.82f, 1f);
        [SerializeField] private Color emptyTextColor = new Color(0.45f, 0.48f, 0.46f, 0.7f);

        [Header("Animation & Feedback Settings")]
        [SerializeField] private float errorShakeDuration = 0.4f;
        [SerializeField] private float errorShakeMagnitude = 10f;

        public event Action<string> OnSubmitCode;
        public event Action OnCancelled;

        public bool IsVisible => panelRoot != null ? panelRoot.activeSelf : gameObject.activeSelf;
        public int MaxDigits => digitSlotTexts != null && digitSlotTexts.Length > 0 ? digitSlotTexts.Length : 3;
        public string CurrentCode => string.Concat(digits);

        private readonly List<int> digits = new List<int>();
        private Action<string> submitCallback;
        private Action cancelCallback;
        private float shakeTimer;
        private Vector3 originalPanelPosition;
        private bool isShaking;

        private void Awake()
        {
            AutoBindComponents();
            BindButtonEvents();
            if (panelRoot != null)
            {
                originalPanelPosition = panelRoot.transform.localPosition;
            }
        }

        private void Update()
        {
            if (!IsVisible) return;

            HandleKeyboardInput();
            UpdateErrorShake();
        }

        /// <summary>
        /// Open the lock interface and register optional submit/cancel callbacks.
        /// </summary>
        public void OpenLock(Action<string> onSubmit = null, Action onCancel = null)
        {
            AutoBindComponents();
            submitCallback = onSubmit;
            cancelCallback = onCancel;
            digits.Clear();

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
                originalPanelPosition = panelRoot.transform.localPosition;
            }
            else
            {
                gameObject.SetActive(true);
            }

            if (modalBackdrop != null)
            {
                modalBackdrop.gameObject.SetActive(true);
            }

            SetFeedback("Введіть 3 цифри коду", false);
            UpdateDisplay();
        }

        /// <summary>
        /// Close the lock interface.
        /// </summary>
        public void CloseLock()
        {
            if (panelRoot != null)
            {
                panelRoot.transform.localPosition = originalPanelPosition;
                panelRoot.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }

            if (modalBackdrop != null)
            {
                modalBackdrop.gameObject.SetActive(false);
            }

            isShaking = false;
            var callback = cancelCallback;
            cancelCallback = null;
            callback?.Invoke();
            OnCancelled?.Invoke();
        }

        public void AppendDigit(int digit)
        {
            if (digits.Count >= MaxDigits) return;
            digits.Add(Mathf.Clamp(digit, 0, 9));
            SetFeedback("Введено " + digits.Count + " з " + MaxDigits + " цифр", false);
            UpdateDisplay();
        }

        public void RemoveLastDigit()
        {
            if (digits.Count > 0)
            {
                digits.RemoveAt(digits.Count - 1);
                SetFeedback(digits.Count == 0 ? "Введіть 3 цифри коду" : "Введено " + digits.Count + " з " + MaxDigits + " цифр", false);
                UpdateDisplay();
            }
        }

        public void ClearDigits()
        {
            digits.Clear();
            SetFeedback("Код очищено", false);
            UpdateDisplay();
        }

        public void Submit()
        {
            if (digits.Count == 0)
            {
                SetFeedback("Введіть хоча б одну цифру", true);
                TriggerErrorFeedback();
                return;
            }

            string code = CurrentCode;
            submitCallback?.Invoke(code);
            OnSubmitCode?.Invoke(code);
        }

        public void SetFeedback(string message, bool isError = false)
        {
            if (feedbackText != null)
            {
                feedbackText.text = message ?? string.Empty;
                feedbackText.color = isError ? errorBorderColor : normalTextColor;
            }
        }

        public void ShowError(string message = "Код не підходить")
        {
            SetFeedback(message, true);
            TriggerErrorFeedback();
            HighlightSlots(errorBorderColor);
        }

        public void ShowSuccess(string message = "Замок відкрито")
        {
            SetFeedback(message, false);
            if (feedbackText != null)
            {
                feedbackText.color = successBorderColor;
            }
            HighlightSlots(successBorderColor);
        }

        private void TriggerErrorFeedback()
        {
            shakeTimer = errorShakeDuration;
            isShaking = true;
        }

        private void UpdateErrorShake()
        {
            if (!isShaking || panelRoot == null) return;

            if (shakeTimer > 0)
            {
                shakeTimer -= Time.unscaledDeltaTime;
                float offset = Mathf.Sin(shakeTimer * 35f) * errorShakeMagnitude * (shakeTimer / errorShakeDuration);
                panelRoot.transform.localPosition = originalPanelPosition + new Vector3(offset, 0, 0);
            }
            else
            {
                isShaking = false;
                panelRoot.transform.localPosition = originalPanelPosition;
                UpdateDisplay();
            }
        }

        private void HighlightSlots(Color color)
        {
            if (digitSlotBorders == null) return;
            for (int i = 0; i < digitSlotBorders.Length; i++)
            {
                if (digitSlotBorders[i] != null)
                {
                    digitSlotBorders[i].color = color;
                }
            }
        }

        private void UpdateDisplay()
        {
            if (digitSlotTexts == null) return;

            for (int i = 0; i < digitSlotTexts.Length; i++)
            {
                if (digitSlotTexts[i] == null) continue;

                if (i < digits.Count)
                {
                    digitSlotTexts[i].text = digits[i].ToString();
                    digitSlotTexts[i].color = normalTextColor;
                }
                else
                {
                    digitSlotTexts[i].text = "—";
                    digitSlotTexts[i].color = emptyTextColor;
                }
            }

            if (digitSlotBorders != null)
            {
                for (int i = 0; i < digitSlotBorders.Length; i++)
                {
                    if (digitSlotBorders[i] == null) continue;
                    bool isActive = (i == digits.Count) || (i == MaxDigits - 1 && digits.Count == MaxDigits);
                    digitSlotBorders[i].color = isActive ? activeBorderColor : normalBorderColor;
                }
            }

            if (submitButton != null)
            {
                submitButton.interactable = digits.Count > 0;
            }
        }

        private void HandleKeyboardInput()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.digit0Key.wasPressedThisFrame || kb.numpad0Key.wasPressedThisFrame) AppendDigit(0);
                else if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) AppendDigit(1);
                else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) AppendDigit(2);
                else if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) AppendDigit(3);
                else if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame) AppendDigit(4);
                else if (kb.digit5Key.wasPressedThisFrame || kb.numpad5Key.wasPressedThisFrame) AppendDigit(5);
                else if (kb.digit6Key.wasPressedThisFrame || kb.numpad6Key.wasPressedThisFrame) AppendDigit(6);
                else if (kb.digit7Key.wasPressedThisFrame || kb.numpad7Key.wasPressedThisFrame) AppendDigit(7);
                else if (kb.digit8Key.wasPressedThisFrame || kb.numpad8Key.wasPressedThisFrame) AppendDigit(8);
                else if (kb.digit9Key.wasPressedThisFrame || kb.numpad9Key.wasPressedThisFrame) AppendDigit(9);
                else if (kb.backspaceKey.wasPressedThisFrame || kb.deleteKey.wasPressedThisFrame) RemoveLastDigit();
                else if (kb.cKey.wasPressedThisFrame) ClearDigits();
                else if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) Submit();
                else if (kb.escapeKey.wasPressedThisFrame) CloseLock();
                return;
            }
#endif
            if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0)) AppendDigit(0);
            else if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) AppendDigit(1);
            else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) AppendDigit(2);
            else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) AppendDigit(3);
            else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) AppendDigit(4);
            else if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5)) AppendDigit(5);
            else if (Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6)) AppendDigit(6);
            else if (Input.GetKeyDown(KeyCode.Alpha7) || Input.GetKeyDown(KeyCode.Keypad7)) AppendDigit(7);
            else if (Input.GetKeyDown(KeyCode.Alpha8) || Input.GetKeyDown(KeyCode.Keypad8)) AppendDigit(8);
            else if (Input.GetKeyDown(KeyCode.Alpha9) || Input.GetKeyDown(KeyCode.Keypad9)) AppendDigit(9);
            else if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Delete)) RemoveLastDigit();
            else if (Input.GetKeyDown(KeyCode.C)) ClearDigits();
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) Submit();
            else if (Input.GetKeyDown(KeyCode.Escape)) CloseLock();
        }

        private void BindButtonEvents()
        {
            if (numberButtons != null)
            {
                for (int i = 0; i < numberButtons.Length; i++)
                {
                    int digit = i;
                    if (numberButtons[i] != null)
                    {
                        numberButtons[i].onClick.RemoveAllListeners();
                        numberButtons[i].onClick.AddListener(() => AppendDigit(digit));
                    }
                }
            }

            if (clearButton != null)
            {
                clearButton.onClick.RemoveAllListeners();
                clearButton.onClick.AddListener(ClearDigits);
            }

            if (backspaceButton != null)
            {
                backspaceButton.onClick.RemoveAllListeners();
                backspaceButton.onClick.AddListener(RemoveLastDigit);
            }

            if (submitButton != null)
            {
                submitButton.onClick.RemoveAllListeners();
                submitButton.onClick.AddListener(Submit);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(CloseLock);
            }
        }

        private void AutoBindComponents()
        {
            if (panelRoot == null) panelRoot = gameObject;
        }
    }
}
