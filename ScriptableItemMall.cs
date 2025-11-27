// ScriptableItemMall.cs — DISABLED STUB
// Keeps the class/type so existing references compile, but exposes no runtime data.

using UnityEngine;

#if UNITY_EDITOR
[CreateAssetMenu(menuName="uMMORPG/Item Mall (Disabled)", fileName="ItemMallConfig_Disabled", order=999)]
#endif
public class ScriptableItemMall : ScriptableObject
{
    // Kept for API compatibility; empty in disabled build.
    //[Tooltip("Unused while Item Mall is disabled.")]
   // public ItemMallCategory[] categories = System.Array.Empty<ItemMallCategory>();

    // For UI guards: if (config && config.IsEnabled) ...
    public bool IsEnabled => false;

    // Convenience for legacy loops/UI.
    public int Count => 0;
}
