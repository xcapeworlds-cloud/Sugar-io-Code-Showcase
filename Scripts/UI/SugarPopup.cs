using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
[RequireComponent(typeof(TextMeshPro))]
public sealed class SugarPopup : MonoBehaviour
{
    [Header("Pool")]
    [SerializeField, Range(1, 16)]
    private int poolSize = 8;

    [Header("Text Outline")]
    [SerializeField]
    private Color outlineColor = Color.black;

    [SerializeField, Range(0f, 0.5f)]
    private float outlineWidth = 0.12f;

    [Header("Animation")]
    [SerializeField, Range(0.5f, 1.5f)]
    private float lifetime = 0.85f;

    [SerializeField, Min(0f)]
    private float riseDistance = 1.35f;

    [SerializeField, Range(0.01f, 1f)]
    private float startScale = 0.55f;

    [SerializeField, Range(1f, 2f)]
    private float peakScale = 1.18f;

    [SerializeField, Range(0f, 0.95f)]
    private float fadeStart = 0.55f;

    [Header("Size With Tornado Growth")]
    [SerializeField, Min(0.01f)]
    [Tooltip("World-size multiplier while the tornado is at its starting scale.")]
    private float startingPopupSize = 0.42f;

    [SerializeField, Range(0f, 1f)]
    [Tooltip("How strongly popup size follows tornado growth. Lower values prevent extreme scaling.")]
    private float growthSizeResponse = 0.7f;

    [SerializeField, Min(0.1f)]
    private float maximumPopupSize = 3.2f;

    [Header("Popup Spacing")]
    [SerializeField, Min(0f)]
    [Tooltip("Horizontal separation between rapid pickup popups.")]
    private float horizontalSpacing = 1.15f;

    [SerializeField, Min(0f)]
    [Tooltip("Additional height variation between rapid pickup popups.")]
    private float verticalSpacing = 0.3f;

    private sealed class Entry
    {
        public GameObject gameObject;
        public Transform transform;
        public TextMeshPro text;
        public MeshRenderer renderer;
        public MaterialPropertyBlock materialProperties;
        public Vector3 startPosition;
        public float shownAt;
        public float hideAt;
        public float displaySize;
        public Color displayColor;
        public bool active;
    }

    private TextMeshPro template;
    private Entry[] entries;
    private Camera targetCamera;
    private Color baseColor;
    private int nextEntry;
    private int popupSequence;
    private Material runtimeMaterial;

    private static readonly int OutlineColorId =
        Shader.PropertyToID("_OutlineColor");

    private static readonly int OutlineWidthId =
        Shader.PropertyToID("_OutlineWidth");

    private void Awake()
    {
        template = GetComponent<TextMeshPro>();
        targetCamera = Camera.main;
        baseColor = template.color;

        CreateSharedRuntimeMaterial();

        // The scene object is a hidden template/controller, never a visible
        // test popup. Its shared TMP material is reused by every pool entry.
        template.text = string.Empty;
        template.enabled = false;

        BuildPool();
    }

    private void Update()
    {
        if (entries == null)
        {
            return;
        }

        for (int i = 0; i < entries.Length; i++)
        {
            Entry entry = entries[i];

            if (entry == null || !entry.active)
            {
                continue;
            }

            float now = Time.unscaledTime;

            if (now >= entry.hideAt)
            {
                Hide(entry);
                continue;
            }

            float progress = Mathf.Clamp01(
                (now - entry.shownAt) /
                Mathf.Max(0.05f, entry.hideAt - entry.shownAt)
            );
            float riseProgress = 1f - Mathf.Pow(1f - progress, 2f);

            entry.transform.position =
                entry.startPosition + Vector3.up * riseDistance * riseProgress;

            FaceCamera(entry.transform);

            float scale;

            if (progress < 0.18f)
            {
                scale = Mathf.Lerp(
                    startScale,
                    peakScale,
                    progress / 0.18f
                );
            }
            else
            {
                scale = Mathf.Lerp(
                    peakScale,
                    1f,
                    Mathf.InverseLerp(0.18f, 1f, progress)
                );
            }

            entry.transform.localScale =
                Vector3.one * scale * entry.displaySize;

            float alpha = progress < fadeStart
                ? 1f
                : 1f - Mathf.InverseLerp(fadeStart, 1f, progress);

            Color color = baseColor;
            color.a *= alpha;
            entry.text.color = color;

        }
    }

    public void Show(
        float sugarAmount,
        Vector3 worldPosition,
        float tornadoVisualScale,
        Color highlightColor)
    {
        if (sugarAmount <= 0f)
        {
            return;
        }

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        if (entries == null || entries.Length == 0)
        {
            return;
        }

        Entry entry = GetNextEntry();
        float now = Time.unscaledTime;
        entry.shownAt = now;
        entry.hideAt = now + Mathf.Max(0.05f, lifetime);
        entry.displaySize = GetDisplaySize(tornadoVisualScale);
        entry.displayColor = highlightColor;
        entry.displayColor.a = 1f;
        entry.active = true;
        entry.startPosition = GetSpreadPosition(
            worldPosition,
            entry.displaySize
        );
        entry.text.text = "+" + FormatAmount(sugarAmount);
        entry.text.color = baseColor;
        ApplyOutlineColor(entry);
        entry.transform.position = entry.startPosition;
        entry.transform.localScale =
            Vector3.one * startScale * entry.displaySize;

        FaceCamera(entry.transform);
        entry.gameObject.SetActive(true);
    }

    private void BuildPool()
    {
        entries = new Entry[Mathf.Max(1, poolSize)];

        for (int i = 0; i < entries.Length; i++)
        {
            GameObject popupObject = new GameObject(
                "SugarPopup_Pooled_" + (i + 1),
                typeof(RectTransform),
                typeof(MeshRenderer),
                typeof(TextMeshPro)
            );

            Transform popupTransform = popupObject.transform;
            popupTransform.SetParent(transform, false);
            popupTransform.localScale = Vector3.one;

            TextMeshPro popupText = popupObject.GetComponent<TextMeshPro>();
            CopyTemplateSettings(popupText);

            MeshRenderer popupRenderer = popupObject.GetComponent<MeshRenderer>();
            popupRenderer.shadowCastingMode = ShadowCastingMode.Off;
            popupRenderer.receiveShadows = false;
            popupRenderer.lightProbeUsage = LightProbeUsage.Off;
            popupRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            popupRenderer.motionVectorGenerationMode =
                MotionVectorGenerationMode.ForceNoMotion;

            popupObject.SetActive(false);

            entries[i] = new Entry
            {
                gameObject = popupObject,
                transform = popupTransform,
                text = popupText,
                renderer = popupRenderer,
                materialProperties = new MaterialPropertyBlock()
            };
        }
    }

    private void CopyTemplateSettings(TextMeshPro destination)
    {
        destination.font = template.font;
        destination.fontSharedMaterial = runtimeMaterial;
        destination.fontSize = template.fontSize;
        destination.fontStyle = template.fontStyle;
        destination.alignment = TextAlignmentOptions.Center;
        destination.color = baseColor;
        destination.textWrappingMode = TextWrappingModes.NoWrap;
        destination.richText = false;
        destination.raycastTarget = false;
        destination.sortingLayerID = template.sortingLayerID;
        destination.sortingOrder = template.sortingOrder;
        destination.text = string.Empty;
    }

    private Entry GetNextEntry()
    {
        for (int i = 0; i < entries.Length; i++)
        {
            int index = (nextEntry + i) % entries.Length;

            if (!entries[index].active)
            {
                nextEntry = (index + 1) % entries.Length;
                return entries[index];
            }
        }

        // If collection is faster than the pool duration, recycle the oldest
        // popup rather than allocating another object during gameplay.
        int oldestIndex = 0;
        float oldestAge = Time.unscaledTime - entries[0].shownAt;

        for (int i = 1; i < entries.Length; i++)
        {
            float age = Time.unscaledTime - entries[i].shownAt;

            if (age > oldestAge)
            {
                oldestAge = age;
                oldestIndex = i;
            }
        }

        nextEntry = (oldestIndex + 1) % entries.Length;
        return entries[oldestIndex];
    }

    private void FaceCamera(Transform popupTransform)
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera != null)
        {
            popupTransform.rotation = targetCamera.transform.rotation;
        }
    }

    private static string FormatAmount(float amount)
    {
        float whole = Mathf.Round(amount);

        return Mathf.Abs(amount - whole) < 0.001f
            ? whole.ToString("0")
            : amount.ToString("0.##");
    }

    private float GetDisplaySize(float tornadoVisualScale)
    {
        const float StartingTornadoScale = 0.2f;

        float relativeGrowth = Mathf.Max(
            1f,
            tornadoVisualScale / StartingTornadoScale
        );

        float growthMultiplier = Mathf.Pow(
            relativeGrowth,
            growthSizeResponse
        );

        return Mathf.Min(
            maximumPopupSize,
            startingPopupSize * growthMultiplier
        );
    }

    private static void ApplyOutlineColor(Entry entry)
    {
        if (entry == null || entry.renderer == null)
        {
            return;
        }

        MaterialPropertyBlock properties = entry.materialProperties;
        properties.Clear();
        entry.renderer.GetPropertyBlock(properties);
        properties.SetColor(OutlineColorId, entry.displayColor);
        entry.renderer.SetPropertyBlock(properties);
    }

    private Vector3 GetSpreadPosition(
        Vector3 worldPosition,
        float displaySize)
    {
        const int LaneCount = 5;

        int sequence = popupSequence++;
        int lane = sequence % LaneCount - LaneCount / 2;
        int row = sequence / LaneCount % 2;

        Vector3 right = targetCamera != null
            ? targetCamera.transform.right
            : Vector3.right;

        right.y = 0f;

        if (right.sqrMagnitude < 0.001f)
        {
            right = Vector3.right;
        }
        else
        {
            right.Normalize();
        }

        return worldPosition +
               right * lane * horizontalSpacing * displaySize +
               Vector3.up * row * verticalSpacing * displaySize;
    }

    private static void Hide(Entry entry)
    {
        entry.active = false;
        entry.shownAt = 0f;
        entry.hideAt = 0f;
        entry.displaySize = 1f;
        entry.displayColor = Color.white;
        entry.text.text = string.Empty;
        entry.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (entries == null)
        {
            return;
        }

        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i] != null && entries[i].active)
            {
                Hide(entries[i]);
            }
        }
    }

    private void OnDestroy()
    {
        if (entries == null)
        {
            return;
        }

        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i] != null && entries[i].gameObject != null)
            {
                Destroy(entries[i].gameObject);
            }
        }

        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
            runtimeMaterial = null;
        }
    }

    private void CreateSharedRuntimeMaterial()
    {
        Material sourceMaterial = template.fontSharedMaterial;

        if (sourceMaterial == null)
        {
            return;
        }

        runtimeMaterial = new Material(sourceMaterial)
        {
            name = sourceMaterial.name + " (Sugar Popup Runtime)"
        };

        if (runtimeMaterial.HasProperty(OutlineColorId))
        {
            runtimeMaterial.SetColor(OutlineColorId, outlineColor);
        }

        if (runtimeMaterial.HasProperty(OutlineWidthId))
        {
            runtimeMaterial.SetFloat(OutlineWidthId, outlineWidth);
        }

        template.fontSharedMaterial = runtimeMaterial;
    }

    private void OnValidate()
    {
        poolSize = Mathf.Clamp(poolSize, 1, 16);
        outlineWidth = Mathf.Clamp(outlineWidth, 0f, 0.5f);
        lifetime = Mathf.Clamp(lifetime, 0.5f, 1.5f);
        riseDistance = Mathf.Max(0f, riseDistance);
        startScale = Mathf.Clamp(startScale, 0.01f, 1f);
        peakScale = Mathf.Clamp(peakScale, 1f, 2f);
        fadeStart = Mathf.Clamp(fadeStart, 0f, 0.95f);
        startingPopupSize = Mathf.Max(0.01f, startingPopupSize);
        growthSizeResponse = Mathf.Clamp01(growthSizeResponse);
        maximumPopupSize = Mathf.Max(startingPopupSize, maximumPopupSize);
        horizontalSpacing = Mathf.Max(0f, horizontalSpacing);
        verticalSpacing = Mathf.Max(0f, verticalSpacing);
    }
}