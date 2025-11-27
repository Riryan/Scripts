using Mirror;
using UnityEngine;

public partial class Player
{
#if !UNITY_SERVER || UNITY_EDITOR
    UI_InvisibleHint _uiInvisibleHint;

    UI_InvisibleHint GetInvisibleHintUI()
    {
        if (_uiInvisibleHint == null)
            _uiInvisibleHint = Object.FindObjectOfType<UI_InvisibleHint>(true); // include inactive
        return _uiInvisibleHint;
    }

    [Client]
    public void InvisibleHint_Show(string message, float hideAfter)
    {
        var ui = GetInvisibleHintUI();
        if (ui == null) return;

        CancelInvoke(nameof(InvisibleHint_Hide)); // prevent double timers
        ui.Show(message);

        if (hideAfter > 0f)
            Invoke(nameof(InvisibleHint_Hide), hideAfter);
    }

    [Client]
    public void InvisibleHint_Hide()
    {
        var ui = GetInvisibleHintUI();
        if (ui == null) return;

        CancelInvoke(nameof(InvisibleHint_Hide));
        ui.Hide();
    }
#endif
}
