using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace GFFAddons
{
    public enum CustomizationType { byMaterials, byObjects, byTint, byBlendShape }
    public enum EquipmentItemType
    {
        Skin, Hair, Face, Beard, Helmet, Jackets, Pants, Boots, Gloves, Shoulders, Waist,
        [InspectorName("Hair Color")] HairColor,
        [InspectorName("Skin Color")] SkinColor,
        [InspectorName("Eye Color")] EyeColor,
        Ears, EyeBrows,
        Details, Back, Arms, Hips, Jewelry,LeftHand,RightHand
    }

    [Serializable] public class CustomizationObject
    {
        public string name;
        public GameObject[] parts;
        public EquipmentItem item;
    }

    [Serializable] public class CustomizationMaterials
    {
        public SkinnedMeshRenderer[] meshes;
        public int submeshIndex = 0;
        public Material[] materials;

        [HideInInspector, FormerlySerializedAs("mesh")] public SkinnedMeshRenderer legacyMesh;
        [HideInInspector, FormerlySerializedAs("positionInMesh")] public int legacyPositionInMesh;

        public void MigrateLegacy()
        {
            if ((meshes == null || meshes.Length == 0) && legacyMesh != null)
            {
                meshes = new SkinnedMeshRenderer[] { legacyMesh };
                if (submeshIndex == 0 && legacyPositionInMesh > 0) submeshIndex = legacyPositionInMesh;
            }
        }
    }
    [Serializable]
    public class CustomizationBlendshape
    {
        public SkinnedMeshRenderer[] meshes;
        public string shapeName;
        public float[] weightsPalette = new float[] { 0, 25, 50, 75, 100 };
        [System.NonSerialized] private Dictionary<SkinnedMeshRenderer, int> _indexCache;

        public int ResolveIndex(SkinnedMeshRenderer r)
        {
            if (!r || !r.sharedMesh) return -1;
            _indexCache ??= new Dictionary<SkinnedMeshRenderer, int>();
            if (_indexCache.TryGetValue(r, out var idx)) return idx;
            idx = r.sharedMesh.GetBlendShapeIndex(shapeName);
            _indexCache[r] = idx;
            return idx;
        }
    }

    [Serializable] public class CustomizationTint
    {
        [Tooltip("All renderers that should receive the per-player tint.")]
        public SkinnedMeshRenderer[] meshes;

        [Tooltip("Shader color property to set (e.g., _Color, _BaseColor, _Color_Skin).")]
        public string propertyName = "_Color";

        [Tooltip("Palette of colors the slider will cycle through.")]
        public Color[] colors = Array.Empty<Color>();

        [Tooltip("If true, apply only to material slots that actually have this property. If false, apply to submesh 0 (or first matching slot when filters are set).")]
        public bool detectPerSlot = true;

        [Tooltip("When detectPerSlot is OFF, this is the submesh slot used if no matching filtered slot is found.")]
        public int fallbackSubmeshIndex = 0;

        // ===== NEW: optional material filters =====
        [Header("Optional Material Filters")] 
        [Tooltip("If enabled, only these material asset(s) may receive the tint (drag body.mat etc.).")]
        public bool restrictToMaterialAssets = false;

        [Tooltip("Whitelisted material assets for this tint.")]
        public Material[] allowedMaterials = Array.Empty<Material>();

        [Tooltip("Additionally restrict by material name (case-insensitive). Useful if materials vary across models.")]
        public bool restrictToMaterialNames = false;

        [Tooltip("Any of these substrings in the material name will be accepted (e.g., 'body', 'skin').")]
        public string[] allowedNameSubstrings = Array.Empty<string>();
    }

    [Serializable] public class Customization
    {
        [HideInInspector] public string name;
        public EquipmentItemType type;
        public CustomizationType customizationBy;
        public bool showWhenCharacterCreate = false;

        [Header("By Objects")] public CustomizationObject[] objects;
        [Header("By Materials")] public CustomizationMaterials materials;
        [Header("By Tint (per-player MPB)")] public CustomizationTint tint;
        [Header("By BlendShape")] public CustomizationBlendshape blendshape;  
    }

    [DisallowMultipleComponent]
    public class PlayerCustomization : NetworkBehaviour
    {
        // Categories that should revert to index 0 when no item overrides them
        public List<EquipmentItemType> typesRevertToZero = new List<EquipmentItemType>();

        public Customization[] customization;
        public bool rescaling;
        public float scaleMin = 0.5f;
        public float scaleMax = 1.5f;

        public SyncList<int> values = new SyncList<int>();
        [SyncVar(hook = nameof(OnScaleChanged))] public float scale = 1;

        // === Helpers ===
        [Server]
        private void EnsureValuesLength()
        {
            if (customization == null) return;
            if (values == null) return;
            while (values.Count < customization.Length)
                values.Add(0);
        }

        private int ClampForCategory(int catIndex, int candidate)
        {
            if (customization == null || catIndex < 0 || catIndex >= customization.Length) return 0;
            var c = customization[catIndex];
            if (c == null) return 0;

            switch (c.customizationBy)
            {
                case CustomizationType.byObjects:
                {
                    int count = (c.objects != null) ? c.objects.Length : 0;
                    if (count <= 0) return 0;
                    return Mathf.Clamp(candidate, 0, count - 1);
                }
                case CustomizationType.byMaterials:
                {
                    if (c.materials == null || c.materials.materials == null) return 0;
                    int count = c.materials.materials.Length;
                    if (count <= 0) return 0;
                    return Mathf.Clamp(candidate, 0, count - 1);
                }
                case CustomizationType.byTint:
                {
                    if (c.tint == null || c.tint.colors == null) return 0;
                    int count = c.tint.colors.Length;
                    if (count <= 0) return 0;
                    return Mathf.Clamp(candidate, 0, count - 1);
                }
            }
            return 0;
        }

        void Awake()
        {
            // only at runtime (built player, including headless)
            if (!Application.isPlaying) return;

    #if UNITY_EDITOR
            // In the Editor, OnValidate wires listeners via EventsPartial.* once-helpers.
            // Avoid double-subscription at runtime.
            return;
    #endif

            // Wire NetworkManagerMMO event
            var manager = FindAnyObjectByType<NetworkManagerMMO>();
            if (manager != null)
                manager.onServerCharacterCreate.AddListener(manager.OnServerCharacterCreate_Customization);

            // Wire Database events
            var database = FindAnyObjectByType<Database>();
            if (database != null)
            {
                database.onConnected.AddListener(database.Connect_Customization);
                database.onCharacterLoad.AddListener(database.CharacterLoad_Customization);
                database.onCharacterSave.AddListener(database.CharacterSave_Customization);
            }
        }

        private void Start()
        {
    #if UNITY_SERVER
            // Headless/server build: skip any visual customization work.
            return;
    #else
            if (!isClient) return; // Only apply visuals on the client (host or remote)
            SetCustomization();
            // Client-only: reset local equip overrides and request equipment replay at login
            ResetEquipOverrideStateAndApplyBase();
            RequestEquipmentRebuildFromProvider();
            didInitialEquipRebuild = true;
    #endif
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
    #if UNITY_SERVER
            return;
    #else
            // Re-apply once initial SyncVars/SyncLists are ready
            try { values.Callback += OnValuesChanged; } catch { }
            ApplyCustomizationFromValues();
            // Ask equipment to replay once more after sync is fully ready
            RequestEquipmentRebuildFromProvider();
    #endif
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
    #if !UNITY_SERVER || UNITY_EDITOR
            try { values.Callback -= OnValuesChanged; } catch {}
    #endif
        }

        void OnValuesChanged(SyncList<int>.Operation op, int index, int oldItem, int newItem)
        {
    #if UNITY_SERVER
            return;
    #else
            ApplyCustomizationFromValues();
    #endif
        }

        private int FindIndex(EquipmentItemType type)
        {
            if (customization == null) return -1;
            for (int i = 0; i < customization.Length; i++)
                if (customization[i] != null && customization[i].type == type) return i;
            return -1;
        }

        public Customization[] GetItemTypesForCharacterCreate()
        {
            List<Customization> list = new List<Customization>();
            if (customization != null)
            {
                for (int i = 0; i < customization.Length; i++)
                    if (customization[i] != null && customization[i].showWhenCharacterCreate) list.Add(customization[i]);
            }
            return list.ToArray();
        }

        public void SetCustomizationLocalByType(EquipmentItemType type, int value, bool persist = true)
        {
            if (value < 0) return;

            int index = FindIndex(type);
            if (index == -1) return;

            var cat = customization[index];
            if (cat == null) return;

            switch (cat.customizationBy)
            {
                case CustomizationType.byObjects:
                {
                    int count = (cat.objects != null) ? cat.objects.Length : 0;
                    if (count <= 0) return;
                    int clamped = Mathf.Clamp(value, 0, count - 1);

                    for (int i = 0; i < count; i++)
                    {
                        bool active = (i == clamped);
                        var obj = cat.objects[i];
                        if (obj == null || obj.parts == null) continue;
                        for (int x = 0; x < obj.parts.Length; x++)
                            if (obj.parts[x] != null) obj.parts[x].SetActive(active);
                    }

    #if UNITY_SERVER
                    EnsureValuesLength();
                    if (index < values.Count) values[index] = clamped; else values.Add(clamped);
    #endif
                    if (persist) CmdSetCustomizationByType(type, clamped);
                    break;
                }
                case CustomizationType.byMaterials:
                {
                    if (cat.materials == null || cat.materials.materials == null) return;
                    int count = cat.materials.materials.Length;
                    if (count <= 0) return;

                    cat.materials.MigrateLegacy();
                    var arr = cat.materials.meshes;
                    if (arr == null || arr.Length == 0) return;

                    int sub = Mathf.Max(0, cat.materials.submeshIndex);
                    int clamped = Mathf.Clamp(value, 0, count - 1);
                    var chosen = cat.materials.materials[clamped];

                    for (int t = 0; t < arr.Length; ++t)
                    {
                        var r = arr[t];
                        if (!r) continue;
                        var rmats = r.materials; // per-instance
                        if (rmats == null || sub < 0 || sub >= rmats.Length) continue;
                        rmats[sub] = chosen;
                        r.materials = rmats;
                    }

    #if UNITY_SERVER
                    EnsureValuesLength();
                    if (index < values.Count) values[index] = clamped; else values.Add(clamped);
    #endif
                    if (persist) CmdSetCustomizationByType(type, clamped);
                    break;
                }
                case CustomizationType.byTint:
                {
                    if (cat.tint == null || cat.tint.colors == null) return;
                    int count = cat.tint.colors.Length;
                    if (count <= 0) return;

                    int clamped = Mathf.Clamp(value, 0, count - 1);
                    ApplyTint(cat.tint, clamped);
    #if UNITY_SERVER
                    EnsureValuesLength();
                    if (index < values.Count) values[index] = clamped; else values.Add(clamped);
    #endif
                    if (persist) CmdSetCustomizationByType(type, clamped);
                    break;
                }
            }

            if (rescaling)
            {
                float s = Mathf.Clamp(scale, scaleMin, scaleMax);
                transform.localScale = new Vector3(s, s, s);
            }
        }

        public void SetCustomization() { ApplyCustomizationFromValues(); }

        [Client]
        private void ApplyCustomizationFromValues()
        {
    #if UNITY_SERVER
            return;
    #else
            if (customization == null || values == null || customization.Length == 0) return;

            for (int i = 0; i < customization.Length; i++)
            {
                var cat = customization[i];
                if (cat == null) continue;

                int idx = ClampForCategory(i, (i < values.Count ? values[i] : 0));

                switch (cat.customizationBy)
                {
                    case CustomizationType.byObjects:
                    {
                        int count = (cat.objects != null) ? cat.objects.Length : 0;
                        if (count <= 0) break;
                        for (int x = 0; x < count; x++)
                        {
                            bool active = (x == idx);
                            var obj = cat.objects[x];
                            if (obj == null || obj.parts == null) continue;
                            for (int y = 0; y < obj.parts.Length; y++)
                                if (obj.parts[y] != null) obj.parts[y].SetActive(active);
                        }
                        break;
                    }
                    case CustomizationType.byMaterials:
                    {
                        if (cat.materials == null || cat.materials.materials == null) break;
                        int matOptions = cat.materials.materials.Length;
                        if (matOptions <= 0) break;

                        cat.materials.MigrateLegacy();
                        var arr = cat.materials.meshes;
                        if (arr == null || arr.Length == 0) break;

                        if (idx < 0 || idx >= matOptions) idx = 0;

                        int sub = Mathf.Max(0, cat.materials.submeshIndex);
                        var chosen = cat.materials.materials[idx];

                        for (int t = 0; t < arr.Length; ++t)
                        {
                            var r = arr[t];
                            if (!r) continue;
                            var rmats = r.materials;
                            if (rmats == null || sub < 0 || sub >= rmats.Length) continue;
                            rmats[sub] = chosen;
                            r.materials = rmats;
                        }
                        break;
                    }
                    case CustomizationType.byTint:
                    {
                        if (cat.tint == null || cat.tint.colors == null) break;
                        int max = cat.tint.colors.Length;
                        if (max <= 0) break;
                        if (idx < 0 || idx >= max) idx = 0;
                        ApplyTint(cat.tint, idx);
                        break;
                    }
                }
            }

            if (rescaling)
            {
                float s = Mathf.Clamp(scale, scaleMin, scaleMax);
                transform.localScale = new Vector3(s, s, s);
            }
    #endif
        }

        void OnScaleChanged(float oldValue, float newValue)
        {
    #if UNITY_SERVER
            return;
    #else
            if (rescaling)
            {
                float s = Mathf.Clamp(newValue, scaleMin, scaleMax);
                transform.localScale = new Vector3(s, s, s);
            }
    #endif
        }

        static readonly int _ColorID = Shader.PropertyToID("_Color"); // rarely used here; propertyName string will be used

        private void ApplyTint(CustomizationTint tint, int paletteIndex)
        {
            if (tint.meshes == null || tint.meshes.Length == 0) return;
            if (tint.colors == null || tint.colors.Length == 0) return;

            Color chosen = tint.colors[Mathf.Clamp(paletteIndex, 0, tint.colors.Length - 1)];
            string prop = string.IsNullOrEmpty(tint.propertyName) ? "_Color" : tint.propertyName;
            int propId = Shader.PropertyToID(prop);

            bool PassesFilters(Material mat)
            {
                if (!mat) return false;

                if (tint.restrictToMaterialAssets)
                {
                    bool any = false;
                    var list = tint.allowedMaterials;
                    for (int i = 0; i < (list != null ? list.Length : 0); ++i)
                    {
                        var allowed = list[i];
                        if (allowed && mat == allowed) { any = true; break; }
                    }
                    if (!any) return false;
                }

                if (tint.restrictToMaterialNames)
                {
                    string nm = mat.name;
                    const string inst = " (Instance)";
                    if (nm.EndsWith(inst, StringComparison.Ordinal))
                        nm = nm.Substring(0, nm.Length - inst.Length);

                    bool any = false;
                    var subs = tint.allowedNameSubstrings;
                    for (int i = 0; i < (subs != null ? subs.Length : 0); ++i)
                    {
                        string sub = subs[i];
                        if (!string.IsNullOrEmpty(sub) && nm.IndexOf(sub, StringComparison.OrdinalIgnoreCase) >= 0)
                        { any = true; break; }
                    }
                    if (!any) return false;
                }

                return true;
            }

            for (int m = 0; m < tint.meshes.Length; ++m)
            {
                var r = tint.meshes[m];
                if (!r) continue;

                if (tint.detectPerSlot)
                {
                    var shared = r.sharedMaterials;
                    int slotCount = shared != null ? shared.Length : 0;
                    for (int s = 0; s < slotCount; ++s)
                    {
                        var mat = shared[s];
                        if (!mat) continue;
                        if (!mat.HasProperty(propId)) continue;
                        if ((tint.restrictToMaterialAssets || tint.restrictToMaterialNames) && !PassesFilters(mat)) continue;

                        var block = new MaterialPropertyBlock();
                        r.GetPropertyBlock(block, s);
                        block.SetColor(propId, chosen);
                        r.SetPropertyBlock(block, s);
                    }
                }
                else
                {
                    // If filters are on, try to find the first matching slot; otherwise use fallbackSubmeshIndex
                    int sub = Mathf.Max(0, tint.fallbackSubmeshIndex);

                    if (tint.restrictToMaterialAssets || tint.restrictToMaterialNames)
                    {
                        var shared = r.sharedMaterials;
                        int slotCount = shared != null ? shared.Length : 0;
                        int found = -1;
                        for (int s = 0; s < slotCount; ++s)
                        {
                            var mat = shared[s];
                            if (!mat) continue;
                            if (!mat.HasProperty(propId)) continue;
                            if (!PassesFilters(mat)) continue;
                            found = s; break;
                        }
                        if (found >= 0) sub = found; else continue; // nothing matched
                    }

                    var block = new MaterialPropertyBlock();
                    r.GetPropertyBlock(block, sub);
                    block.SetColor(propId, chosen);
                    r.SetPropertyBlock(block, sub);
                }
            }
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            if (syncInterval == 0)
                syncInterval = 0.1f;

            if (customization != null)
            {
                for (int i = 0; i < customization.Length; i++)
                    if (customization[i] != null)
                        customization[i].name = customization[i].type.ToString();
            }

            var player = gameObject.GetComponent<Player>();
            if (player != null) player.customization = this;

    #if UNITY_EDITOR
            // Editor-only wiring to avoid duplicate subscriptions across domain reloads.
            NetworkManagerMMO manager = FindAnyObjectByType<NetworkManagerMMO>();
            if (manager)
            {
                UnityAction<CharacterCreateMsg, Player> unityAction = new UnityAction<CharacterCreateMsg, Player>(manager.OnServerCharacterCreate_Customization);
                EventsPartial.AddListenerOnceOnServerCharacterCreate(manager.onServerCharacterCreate, unityAction, manager);
            }

            Database database = FindAnyObjectByType<Database>();
            if (database)
            {
                UnityAction unityAction = new UnityAction(database.Connect_Customization);
                EventsPartial.AddListenerOnceOnConnected(database.onConnected, unityAction, database);

                UnityAction<Player> load = new UnityAction<Player>(database.CharacterLoad_Customization);
                EventsPartial.AddListenerOnceCharacterLoad(database.onCharacterLoad, load, database);

                UnityAction<Player> save = new UnityAction<Player>(database.CharacterSave_Customization);
                EventsPartial.AddListenerOnceCharacterSave(database.onCharacterSave, save, database);
            }
    #endif
        }

    #if !UNITY_SERVER
        // Equipment-driven overrides & hides (client-only)
        int slotCount = -1;
        int[] winnerSlotPerCat;
        int[] effectiveIndexPerCat;
        int[][] perCatSlotOverride;
        ushort[] hideRefCountPerCat;
        bool didInitialEquipRebuild = false;

        void EnsureEquipBuffers()
        {
            if (!isClient) return;
            if (slotCount < 0)
            {
                var eq = GetComponent<PlayerEquipment>();
                slotCount = (eq != null && eq.slotInfo != null) ? eq.slotInfo.Length : 8;
            }

            int catCount = customization != null ? customization.Length : 0;

            winnerSlotPerCat     ??= new int[catCount];
            effectiveIndexPerCat ??= new int[catCount];
            hideRefCountPerCat   ??= new ushort[catCount];
            perCatSlotOverride   ??= new int[catCount][];

            if (winnerSlotPerCat.Length != catCount)
            {
                winnerSlotPerCat     = new int[catCount];
                effectiveIndexPerCat = new int[catCount];
                hideRefCountPerCat   = new ushort[catCount];
                perCatSlotOverride   = new int[catCount][];
            }

            for (int c = 0; c < catCount; ++c)
            {
                if (perCatSlotOverride[c] == null || perCatSlotOverride[c].Length != slotCount)
                {
                    perCatSlotOverride[c] = new int[slotCount];
                    for (int s = 0; s < slotCount; ++s) perCatSlotOverride[c][s] = -1;
                }
                if (winnerSlotPerCat[c] == 0 && effectiveIndexPerCat[c] == 0)
                    winnerSlotPerCat[c] = -1;
            }
        }

        // Rebuild login-state: clear overrides/hides and apply base (or zero-fallback) locally.
        void ResetEquipOverrideStateAndApplyBase()
        {
            if (!isClient) return;
            EnsureEquipBuffers();

            int catCount = customization != null ? customization.Length : 0;

            // Full reset
            for (int c = 0; c < catCount; ++c)
            {
                winnerSlotPerCat[c]     = -1;
                effectiveIndexPerCat[c] = 0;
                hideRefCountPerCat[c]   = 0;

                int[] row = perCatSlotOverride[c];
                if (row != null)
                    for (int s = 0; s < row.Length; ++s)
                        row[s] = -1;
            }

            // Re-apply base (or zero) for all visible categories
            if (customization != null)
            {
                for (int c = 0; c < catCount; ++c)
                {
                    if (hideRefCountPerCat[c] > 0) continue;
                    var cat = customization[c];
                    if (cat == null) continue;

                    bool revertToZero = false;
                    if (typesRevertToZero != null)
                    {
                        var t = cat.type;
                        for (int i2 = 0; i2 < typesRevertToZero.Count; ++i2)
                            if (typesRevertToZero[i2] == t) { revertToZero = true; break; }
                    }

                    int baseIdx = (c < values.Count ? values[c] : 0);
                    int chosen  = ClampForCategory(c, revertToZero ? 0 : baseIdx);
                    SetCustomizationLocalByType(cat.type, chosen, false);
                }
            }
        }

        // Ask PlayerEquipment (if present) to replay current gear into this component.
        void RequestEquipmentRebuildFromProvider()
        {
            if (!isClient) return;
            var eq = GetComponent<PlayerEquipment>();
            if (eq != null)
                eq.SendMessage("Customization_OnRebuildRequested", this, SendMessageOptions.DontRequireReceiver);
        }

        void ApplyCategoryToCurrent(int catIndex)
        {
            int idx = (winnerSlotPerCat[catIndex] != -1)
                        ? effectiveIndexPerCat[catIndex]
                        : (catIndex < values.Count ? values[catIndex] : 0);

            if (hideRefCountPerCat[catIndex] > 0) return;

            var catType = customization[catIndex].type;
            SetCustomizationLocalByType(catType, idx, false);
        }

        void SetCategoryHidden(int catIndex, bool hidden)
        {
            if (!isClient) return;
            if (customization == null || catIndex < 0 || catIndex >= customization.Length) return;
            var cat = customization[catIndex];
            if (cat == null) return;

            if (!hidden)
            {
                ApplyCategoryToCurrent(catIndex);
                return;
            }

            if (cat.customizationBy == CustomizationType.byObjects && cat.objects != null)
            {
                for (int i = 0; i < cat.objects.Length; ++i)
                {
                    var obj = cat.objects[i];
                    if (obj == null || obj.parts == null) continue;
                    for (int p = 0; p < obj.parts.Length; ++p)
                        if (obj.parts[p] != null) obj.parts[p].SetActive(false);
                }
            }
        }

        public void OnSlotEquipped(int slotIndex, EquipmentItemType type, int overrideIndex, EquipmentItemType[] hides)
        {
            if (!isClient) return;
            EnsureEquipBuffers();

            if (overrideIndex >= 0)
            {
                int cat = FindIndex(type);
                if (cat != -1)
                {
                    if (perCatSlotOverride[cat] == null || slotIndex < 0 || slotIndex >= perCatSlotOverride[cat].Length)
                        return;

                    int clamped = ClampForCategory(cat, overrideIndex);
                    perCatSlotOverride[cat][slotIndex] = clamped;

                    if (winnerSlotPerCat[cat] == -1 || slotIndex < winnerSlotPerCat[cat])
                    {
                        winnerSlotPerCat[cat]     = slotIndex;
                        effectiveIndexPerCat[cat] = clamped;

                        if (hideRefCountPerCat[cat] == 0)
                            SetCustomizationLocalByType(type, clamped, false);
                    }
                }
            }

            if (hides != null)
            {
                for (int i = 0; i < hides.Length; ++i)
                {
                    int hcat = FindIndex(hides[i]);
                    if (hcat == -1) continue;

                    unchecked { hideRefCountPerCat[hcat]++; }
                    SetCategoryHidden(hcat, true);
                }
            }
        }

        public void OnSlotUnequipped(int slotIndex, EquipmentItemType type, int overrideIndex, EquipmentItemType[] hides)
        {
            if (!isClient) return;
            EnsureEquipBuffers();

            if (overrideIndex >= 0)
            {
                int cat = FindIndex(type);
                if (cat != -1)
                {
                    if (perCatSlotOverride[cat] == null || slotIndex < 0 || slotIndex >= perCatSlotOverride[cat].Length)
                        return;

                    perCatSlotOverride[cat][slotIndex] = -1;

                    if (winnerSlotPerCat[cat] == slotIndex)
                    {
                        int nextWinner = -1;
                        int nextIndex  = 0;
                        for (int s = 0; s < slotCount; ++s)
                        {
                            int cand = perCatSlotOverride[cat][s];
                            if (cand >= 0)
                            {
                                nextWinner = s; nextIndex = cand; break;
                            }
                        }
                        winnerSlotPerCat[cat]     = nextWinner;
                        effectiveIndexPerCat[cat] = nextIndex;

                        if (hideRefCountPerCat[cat] == 0)
                            ApplyCategoryToCurrent(cat);
                    }
                }
            }

            if (hides != null)
            {
                for (int i = 0; i < hides.Length; ++i)
                {
                    int hcat = FindIndex(hides[i]);
                    if (hcat == -1) continue;

                    if (hideRefCountPerCat[hcat] > 0)
                        hideRefCountPerCat[hcat]--;

                    if (hideRefCountPerCat[hcat] == 0)
                        ApplyCategoryToCurrent(hcat);
                }
            }
        }
    #endif // !UNITY_SERVER

        [Server]
        private void ServerInitCustomization()
        {
            EnsureValuesLength();
            int cats = (customization != null ? customization.Length : 0);
            for (int i = 0; i < cats; ++i)
            {
                int current = (i < values.Count ? values[i] : 0);
                int clamped = ClampForCategory(i, current);
                if (i < values.Count) values[i] = clamped; else values.Add(clamped);
            }
            if (rescaling)
            {
                float s = Mathf.Clamp(scale, scaleMin, scaleMax);
                scale = s; // server owns the SyncVar
            }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            ServerInitCustomization();
        }

        [Command]
        private void CmdSetCustomizationByType(EquipmentItemType type, int value)
        {
            int index = FindIndex(type);
            if (index == -1) return;
            EnsureValuesLength();
            int clamped = ClampForCategory(index, value);
            if (index < values.Count) values[index] = clamped; else values.Add(clamped);
        }

        [Command]
        private void CmdSetScale(float newScale)
        {
            float s = Mathf.Clamp(newScale, scaleMin, scaleMax);
            scale = s;
        }
    }
}
