using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Renderer))]
public sealed class KawaiiFloor : MonoBehaviour
{
    [SerializeField] private Renderer floorRenderer;
    [SerializeField] private bool randomizeOnStart = true;
    [SerializeField, Min(0)] private int paletteIndex;

    private Material runtimeMaterial;

    private void Awake()
    {
        if (floorRenderer == null)
            floorRenderer = GetComponent<Renderer>();
    }

    private void Start()
    {
        ApplyFloorColor();
    }

    public void ApplyFloorColor()
    {
        if (floorRenderer == null ||
            floorRenderer.sharedMaterial == null ||
            KawaiiColors.Count == 0)
            return;

        Color baseColor = randomizeOnStart
            ? KawaiiColors.RandomColor()
            : KawaiiColors.Get(paletteIndex);

        Color visualColor =
            KawaiiColors.GetFloorColor(baseColor);

        if (runtimeMaterial == null)
        {
            runtimeMaterial =
                new Material(floorRenderer.sharedMaterial)
                {
                    name =
                        floorRenderer.sharedMaterial.name +
                        "_Runtime"
                };

            floorRenderer.sharedMaterial =
                runtimeMaterial;
        }

        if (runtimeMaterial.HasProperty("_BaseColor"))
            runtimeMaterial.SetColor("_BaseColor", visualColor);

        if (runtimeMaterial.HasProperty("_Color"))
            runtimeMaterial.SetColor("_Color", visualColor);
    }

    private void OnDestroy()
    {
        if (runtimeMaterial != null)
            Destroy(runtimeMaterial);
    }
}