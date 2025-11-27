// NetworkName.cs
// Bandwidth-optimized: names replicate only on spawn and when they change.
// Back-compat goals: keep same component name; provide simple getters/setters;
// do NOT push any UI every frame. that saves about 40kb per client
// so at 1k CCU, that's a conservative ballpark 40mbps. 99% of that is wasted. using System;
using UnityEngine;
using Mirror;

[DisallowMultipleComponent]
public sealed class NetworkName : NetworkBehaviour
{
    [SerializeField] int maxLength = 32;
    [SerializeField] bool trimWhitespace = true;

    public delegate void NameChangedHandler(string newName);
    public event NameChangedHandler NameChanged;

    [SyncVar(hook = nameof(OnDisplayNameChanged))]
    string displayName = string.Empty;

    public string DisplayName => displayName;
    public string GetName() => displayName;

#if UNITY_SERVER || UNITY_EDITOR
    static long s_updatesSentThisSession = 0;
    public static long UpdatesSentThisSession => s_updatesSentThisSession;
#endif

    [Server]
    public void SetDisplayNameServer(string newName)
    {
        string s = Sanitize(newName);
        if (string.Equals(displayName, s, System.StringComparison.Ordinal)) return;
        displayName = s;
#if UNITY_SERVER || UNITY_EDITOR
        s_updatesSentThisSession++;
#endif
        gameObject.name = displayName;
    }

    [Server] public void InitializeDisplayName(string initialName) => SetDisplayNameServer(initialName);

#if !UNITY_SERVER || UNITY_EDITOR
    [Client]
    public void RequestRename(string requestedName)
    {
        if (!isLocalPlayer) return;
        CmdRequestRename(requestedName);
    }
#endif

    [Command] void CmdRequestRename(string requestedName) => SetDisplayNameServer(requestedName);

    void OnDisplayNameChanged(string oldValue, string newValue)
    {
        gameObject.name = newValue;
        var h = NameChanged; if (h != null) h(newValue);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!string.IsNullOrEmpty(displayName)) gameObject.name = displayName;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (string.IsNullOrEmpty(displayName))
            SetDisplayNameServer(gameObject.name);   // adopt name set during DB spawn flow
        else
            gameObject.name = displayName;
    }

    [Server] public void SetNameServer(string newName) => SetDisplayNameServer(newName);

    string Sanitize(string raw)
    {
        string s = raw ?? string.Empty;
        if (trimWhitespace) s = s.Trim();
        if (s.Length > maxLength) s = s.Substring(0, maxLength);
        if (string.IsNullOrWhiteSpace(s)) s = "Unknown";
        return s;
    }
}
