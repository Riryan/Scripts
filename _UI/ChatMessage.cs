using System;
using UnityEngine;

[Serializable]
public struct ChatMessage
{
    public string sender;
    public string identifier;
    public string text;
    public string replyPrefix;
    public GameObject textPrefab;

    public ChatMessage(string sender, string identifier, string text,
                       string replyPrefix, GameObject textPrefab)
    {
        this.sender      = sender;
        this.identifier  = identifier;
        this.text        = text;
        this.replyPrefix = replyPrefix;
        this.textPrefab  = textPrefab;
    }

    public string Construct()
    {
        string s = "";
        if (!string.IsNullOrWhiteSpace(sender))     s += sender + " ";
        if (!string.IsNullOrWhiteSpace(identifier)) s += identifier + " ";
        s += text;
        return s;
    }
}
