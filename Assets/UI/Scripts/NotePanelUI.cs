using System;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Echo.UI
{
    /// <summary>
    /// Standalone, readable Note Panel UI component for lore documents, clues, and letters.
    /// Features a scrollable content area (ScrollRect), title, body text, close button,
    /// and full rendering support for Ukrainian characters (І, Ї, Є, Ґ), numbers, and line breaks.
    /// </summary>
    [DisallowMultipleComponent]
    public class NotePanelUI : MonoBehaviour
    {
        [Header("UI Element Bindings")]
        [Tooltip("The main container of the note panel (used for show/hide)")]
        [SerializeField] private GameObject panelRoot;

        [Tooltip("Header/Title text component")]
        [SerializeField] private Text titleText;

        [Tooltip("Body/Content text component inside the scrollable viewport")]
        [SerializeField] private Text bodyText;

        [Tooltip("ScrollRect for handling long text")]
        [SerializeField] private ScrollRect scrollRect;

        [Tooltip("Optional vertical scrollbar for the scroll rect")]
        [SerializeField] private Scrollbar verticalScrollbar;

        [Tooltip("Button used to dismiss/close the note")]
        [SerializeField] private Button closeButton;

        [Tooltip("Optional modal backdrop/dimmer image behind the note")]
        [SerializeField] private Image modalBackdrop;

        [Header("Behavior Settings")]
        [Tooltip("Close the note when the Escape key is pressed")]
        [SerializeField] private bool closeOnEscape = true;

        [Tooltip("Auto-select the close button on opening for keyboard/gamepad navigation")]
        [SerializeField] private bool selectCloseButtonOnOpen = true;

        public event Action OnClosed;

        public bool IsVisible => panelRoot != null ? panelRoot.activeSelf : gameObject.activeSelf;

        private Action closeCallback;

        private void Awake()
        {
            AutoBindComponents();
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(CloseNote);
                closeButton.onClick.AddListener(CloseNote);
            }
        }

        private void Update()
        {
            if (!IsVisible) return;

            if (closeOnEscape && WasEscapePressed())
            {
                CloseNote();
            }
        }

        /// <summary>
        /// Display a note with the given title and body content.
        /// Resets scroll position to the top.
        /// </summary>
        public void ShowNote(string title, string content, Action onClose = null)
        {
            AutoBindComponents();
            closeCallback = onClose;

            if (titleText != null)
            {
                titleText.text = string.IsNullOrEmpty(title) ? "ЗАПИСКА" : title;
            }

            if (bodyText != null)
            {
                bodyText.text = content ?? string.Empty;
            }

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }
            else
            {
                gameObject.SetActive(true);
            }

            if (modalBackdrop != null)
            {
                modalBackdrop.gameObject.SetActive(true);
            }

            // Reset scroll position to top
            if (scrollRect != null)
            {
                scrollRect.verticalNormalizedPosition = 1f;
                // Force layout update so the ContentSizeFitter recalculates
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 1f;
            }

            if (selectCloseButtonOnOpen && closeButton != null && UnityEngine.EventSystems.EventSystem.current != null)
            {
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
            }
        }

        /// <summary>
        /// Closes the note panel and invokes registered close callbacks.
        /// </summary>
        public void CloseNote()
        {
            if (panelRoot != null)
            {
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

            var callback = closeCallback;
            closeCallback = null;
            callback?.Invoke();
            OnClosed?.Invoke();
        }

        private bool WasEscapePressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                return true;
#endif
            return Input.GetKeyDown(KeyCode.Escape);
        }

        private void AutoBindComponents()
        {
            if (panelRoot == null)
            {
                panelRoot = gameObject;
            }

            if (titleText == null)
            {
                var titles = GetComponentsInChildren<Text>(true);
                foreach (var t in titles)
                {
                    if (t.name.IndexOf("title", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        titleText = t;
                        break;
                    }
                }
            }

            if (bodyText == null)
            {
                var texts = GetComponentsInChildren<Text>(true);
                foreach (var t in texts)
                {
                    if (t.name.IndexOf("body", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        t.name.IndexOf("content", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        bodyText = t;
                        break;
                    }
                }
            }

            if (scrollRect == null)
            {
                scrollRect = GetComponentInChildren<ScrollRect>(true);
            }

            if (verticalScrollbar == null && scrollRect != null)
            {
                verticalScrollbar = scrollRect.verticalScrollbar;
            }

            if (closeButton == null)
            {
                var buttons = GetComponentsInChildren<Button>(true);
                foreach (var b in buttons)
                {
                    if (b.name.IndexOf("close", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        b.name.IndexOf("закрити", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        closeButton = b;
                        break;
                    }
                }
            }
        }
    }
}

