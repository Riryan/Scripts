﻿using UnityEngine;
using UnityEngine.UI;

public partial class UIChat : MonoBehaviour
{
    public static UIChat singleton;
    public GameObject panel;
    public InputField messageInput;
    public Button sendButton;
    public Transform content;
    public ScrollRect scrollRect;
    public KeyCode[] activationKeys = { KeyCode.Return, KeyCode.KeypadEnter };
    public int keepHistory = 100;

    bool eatActivation;

    [Header("Hover / Highlight (optional)")]
    // Drag your UIImageMouseoverColor (chat background/hover) here in the inspector.
    public UIImageMouseoverColor hoverArea;

    [Header("Auto Hide")]
    public bool autoHide = true;
    public float visibleDuration = 6f;      // how long log stays visible after last activity
    public float fadeDuration = 0.5f;       // fade speed
    [Range(0f, 1f)]
    public float fadedAlpha = 0f;           // alpha when hidden
    [Range(0f, 1f)]
    public float visibleAlpha = 1f;         // alpha when visible

    CanvasGroup canvasGroup;                // lives on scrollRect GameObject
    float lastActivityTime;                 // unscaled time of last chat activity

    public UIChat()
    {
        if (singleton == null) singleton = this;
    }

    // Make sure we have a CanvasGroup on the log area (ScrollRect root)
    void EnsureCanvasGroup()
    {
        if (canvasGroup == null)
        {
            if (scrollRect == null)
                return;

            GameObject target = scrollRect.gameObject;

            canvasGroup = target.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = target.AddComponent<CanvasGroup>();

            canvasGroup.alpha = visibleAlpha;
            SetRaycastState(true);
        }
    }

    // Toggle whether the log area (and optional hover image) should block UI clicks
    void SetRaycastState(bool state)
    {
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = state;
            canvasGroup.interactable = state;
        }

        // Also toggle the hover image’s raycast so it doesn’t eat clicks when hidden
        if (hoverArea != null && hoverArea.image != null)
        {
            hoverArea.image.raycastTarget = state;
        }
    }

    // Any chat-related activity should call this to keep the log visible
    void TouchActivity()
    {
        lastActivityTime = Time.unscaledTime;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = visibleAlpha;
            SetRaycastState(true);
        }
    }

    void Update()
    {
        Player player = Player.localPlayer;
        if (player)
        {
            panel.SetActive(true);
            EnsureCanvasGroup();

            // init timer once
            if (lastActivityTime <= 0f)
                lastActivityTime = Time.unscaledTime;

            // only open chat if no other UI input is active (same as stock uMMORPG)
            if (!UIUtils.AnyInputActive())
            {
                if (Utils.AnyKeyDown(activationKeys) && !eatActivation)
                {
                    TouchActivity();
                    messageInput.Select();
                }
                eatActivation = false;
            }

            // limit characters based on player.chat settings
            messageInput.characterLimit = player.chat.maxLength;

            // submit on Enter
            messageInput.onEndEdit.SetListener((value) =>
            {
                if (Utils.AnyKeyDown(activationKeys))
                {
                    TouchActivity();

                    string newinput = player.chat.OnSubmit(value);
                    messageInput.text = newinput;
                    messageInput.MoveTextEnd(false);
                    eatActivation = true;
                }

                UIUtils.DeselectCarefully();
            });

            // submit on button click
            sendButton.onClick.SetListener(() =>
            {
                TouchActivity();

                string newinput = player.chat.OnSubmit(messageInput.text);
                messageInput.text = newinput;
                messageInput.MoveTextEnd(false);

                UIUtils.DeselectCarefully();
            });

            // keep it visible while typing
            if (messageInput.isFocused)
                TouchActivity();

            UpdateAutoHide();
        }
        else
        {
            panel.SetActive(false);
        }
    }

    void UpdateAutoHide()
    {
        if (!autoHide || canvasGroup == null)
            return;

        float now = Time.unscaledTime;

        bool inputFocused = messageInput != null && messageInput.isFocused;
        bool withinVisibleWindow = (now - lastActivityTime) < visibleDuration;

        float targetAlpha = (inputFocused || withinVisibleWindow)
                            ? visibleAlpha
                            : fadedAlpha;

        if (!Mathf.Approximately(canvasGroup.alpha, targetAlpha))
        {
            float step = (fadeDuration > 0f)
                         ? (Time.unscaledDeltaTime / fadeDuration)
                         : 1f;

            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, step);

            bool isVisible = canvasGroup.alpha > 0.001f;
            SetRaycastState(isVisible);     // when not visible, log becomes click-through
        }
    }

    void AutoScroll()
    {
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0;
    }

    public void AddMessage(ChatMessage message)
    {
        // trim history when it gets too long
        if (content.childCount >= keepHistory)
        {
            for (int i = 0; i < content.childCount / 2; ++i)
                Destroy(content.GetChild(i).gameObject);
        }

        GameObject go = Instantiate(message.textPrefab, content.transform, false);
        go.GetComponent<Text>().text = message.Construct();
        go.GetComponent<UIChatEntry>().message = message;

        TouchActivity(); // new message = show log again
        AutoScroll();
    }

    public void OnEntryClicked(UIChatEntry entry)
    {
        if (!string.IsNullOrWhiteSpace(entry.message.replyPrefix))
        {
            messageInput.text = entry.message.replyPrefix;
            messageInput.Select();

            Invoke(nameof(MoveTextEnd), 0.1f);
            TouchActivity();
        }
    }

    void MoveTextEnd()
    {
        messageInput.MoveTextEnd(false);
    }
}
