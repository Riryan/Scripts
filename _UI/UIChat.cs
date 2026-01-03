﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace uMMORPG
{
    public partial class UIChat : MonoBehaviour
    {
        public static UIChat singleton;
        public GameObject panel;
        public InputField messageInput;
        public Button sendButton;
        public Transform content;
        public ScrollRect scrollRect;
        public KeyCode[] activationKeys = { KeyCode.Return, KeyCode.KeypadEnter };
        public int keepHistory = 100; // only keep 'n' messages

        // pooling (GC fix)
        readonly Stack<UIChatEntry> pooledEntries = new Stack<UIChatEntry>(128);

        bool eatActivation;

        void Awake()
        {
            singleton = this;
        }

        void Update()
        {
            Player player = Player.localPlayer;

            if (player != null)
            {
                panel.SetActive(true);

                // activation keys
                if (Utils.AnyKeyDown(activationKeys) && !eatActivation)
                {
                    messageInput.Select();
                    Invoke(nameof(MoveTextEnd), 0.1f);
                }

                eatActivation = false;

                // end edit listener
                messageInput.onEndEdit.SetListener((value) =>
                {
                    if (Utils.AnyKeyDown(activationKeys))
                    {
                        string newinput = player.chat.OnSubmit(value);
                        messageInput.text = newinput;
                        messageInput.MoveTextEnd(false);
                        eatActivation = true;
                    }

                    UIUtils.DeselectCarefully();
                });

                // send button
                sendButton.onClick.SetListener(() =>
                {
                    string newinput = player.chat.OnSubmit(messageInput.text);
                    messageInput.text = newinput;
                    messageInput.MoveTextEnd(false);
                    UIUtils.DeselectCarefully();
                });
            }
            else panel.SetActive(false);
        }

        public void AddMessage(ChatMessage message)
        {
            // reuse or create entry
            UIChatEntry entry = GetEntry(message.textPrefab);
            entry.message = message;
            entry.text.text = message.Construct();
            entry.gameObject.SetActive(true);

            AutoScroll();

            // trim history (no Destroy)
            int excess = content.childCount - keepHistory;
            for (int i = 0; i < excess; ++i)
            {
                UIChatEntry oldEntry = content.GetChild(0).GetComponent<UIChatEntry>();
                oldEntry.gameObject.SetActive(false);
                pooledEntries.Push(oldEntry);
            }
        }

        UIChatEntry GetEntry(GameObject prefab)
        {
            if (pooledEntries.Count > 0)
            {
                UIChatEntry entry = pooledEntries.Pop();
                entry.transform.SetParent(content, false);
                return entry;
            }

            GameObject go = Instantiate(prefab, content, false);
            return go.GetComponent<UIChatEntry>();
        }

        void AutoScroll()
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }

        // called by chat entries when clicked
        public void OnEntryClicked(UIChatEntry entry)
        {
            if (!string.IsNullOrWhiteSpace(entry.message.replyPrefix))
            {
                messageInput.text = entry.message.replyPrefix;
                messageInput.Select();
                Invoke(nameof(MoveTextEnd), 0.1f);
            }
        }

        void MoveTextEnd()
        {
            messageInput.MoveTextEnd(false);
        }
    }
}
