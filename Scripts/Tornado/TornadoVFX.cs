using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
[RequireComponent(typeof(Tornado), typeof(TornadoColors))]
public sealed class TornadoVFX : MonoBehaviour
{
    // =========================================================
    // MOBILE GEOMETRY
    // =========================================================

    private const int StrandCount = 4;
    private const int SegmentsPerStrand = 20;

    /*
     * 6 sides instead of 4.
     *
     * Four sides makes the strands read almost like
     * rotating flat ribbons from some camera angles.
     * Six is still cheap but looks much rounder.
     */
    private const int TubeSides = 6;

    private const float Height = 8.08f;

    private const float BottomRadius = 0.15f;
    private const float TopRadius = 1.68f;

    private const float Turns = 2.4f;

    private const float BottomThickness = 0.11f;
    private const float TopThickness = 0.31f;


    // =========================================================
    // COTTON CANDY SURFACE
    // =========================================================

    [Header("Cotton Candy Surface")]
    [SerializeField]
    private Texture2D fiberTexture;

    private const float FiberTiling = 5f;
    private const float FiberStrength = 0.30f;
    private const float FiberContrast = 1.25f;


    // =========================================================
    // 3D LOOK
    // =========================================================

    /*
     * Tornado itself stays opaque.
     *
     * Depth comes from shading now, NOT transparency.
     */
    private const float BottomOpacity = 1f;
    private const float TopOpacity = 1f;
    private const float FadeStart = 1f;


    /*
     * Soft directional lighting gives the tube an
     * obvious round side and shadow side.
     */
    private static readonly Vector3 FakeLightDirection =
        new Vector3(
            -0.35f,
            0.75f,
            -0.55f
        ).normalized;


    private const float AmbientLight = 0.66f;
    private const float DirectLight = 0.34f;


    /*
     * Small white edge highlight keeps the cotton
     * candy fluffy without washing everything out.
     */
    private static readonly Color RimColor =
        Color.white;

    private const float RimStrength = 0.16f;
    private const float RimPower = 3.2f;


    // =========================================================
    // MOTION
    // =========================================================

    private const float FallbackDegreesPerSecond = 270f;
    private const float FallbackGrowthSmoothness = 7f;
    private const float ColorTransitionSpeed = 2f;


    // =========================================================
    // RUNTIME
    // =========================================================

    private Tornado tornado;
    private TornadoColors tornadoColors;

    private Transform visualRoot;

    private Mesh mesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    private Material[] materials;

    private readonly List<Color> knownColors =
        new List<Color>();

    private Color[] displayedColors;
    private Color[] targetColors;

    private int lastColorRevision = -1;
    private int nextStrand;

    private float displayedScale;

    private bool colorsAnimating;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        tornado =
            GetComponent<Tornado>();

        tornadoColors =
            GetComponent<TornadoColors>();
    }


    private void Start()
    {
        CreateVisual();


        if (!enabled ||
            visualRoot == null)
        {
            return;
        }


        InitializeColors();


        displayedScale =
            Mathf.Max(
                0.01f,
                tornado.VisualScale
            );


        visualRoot.localScale =
            Vector3.one *
            displayedScale;
    }


    private void Update()
    {
        if (visualRoot == null ||
            tornado == null)
        {
            return;
        }


        visualRoot.Rotate(
            0f,
            GetSpinSpeed() *
            Time.deltaTime,
            0f,
            Space.Self
        );


        UpdateGrowth();


        if (tornadoColors != null &&
            lastColorRevision !=
            tornadoColors.Revision)
        {
            RefreshColorTargets();
        }


        if (colorsAnimating)
        {
            AnimateColors();
        }
    }


    // =========================================================
    // GROWTH
    // =========================================================

    private void UpdateGrowth()
    {
        float targetScale =
            Mathf.Max(
                0.01f,
                tornado.VisualScale
            );


        if (Mathf.Abs(
                displayedScale -
                targetScale) <
            0.001f)
        {
            displayedScale =
                targetScale;

            visualRoot.localScale =
                Vector3.one *
                displayedScale;

            return;
        }


        float blend =
            1f -
            Mathf.Exp(
                -GetGrowthSmoothness() *
                Time.deltaTime
            );


        displayedScale =
            Mathf.Lerp(
                displayedScale,
                targetScale,
                blend
            );


        visualRoot.localScale =
            Vector3.one *
            displayedScale;
    }

    private float GetSpinSpeed()
    {
        TornadoMatchManager match =
            TornadoMatchManager.Instance;

        if (match == null ||
            match.Balance == null ||
            tornado == null)
        {
            return FallbackDegreesPerSecond;
        }

        MatchBalance balance = match.Balance;
        float relativeGrowth = Mathf.Max(
            0f,
            tornado.VisualScale /
            Mathf.Max(0.01f, balance.startingVisualScale) - 1f
        );

        float growthProgress =
            1f - Mathf.Exp(
                -relativeGrowth * balance.spinGrowthResponse
            );

        float multiplier = Mathf.Lerp(
            1f,
            Mathf.Max(1f, balance.maximumSpinMultiplier),
            growthProgress
        );

        return balance.tornadoSpinSpeed * multiplier;
    }

    private static float GetGrowthSmoothness()
    {
        TornadoMatchManager match =
            TornadoMatchManager.Instance;

        return match != null &&
               match.Balance != null
            ? match.Balance.visualGrowthSpeed
            : FallbackGrowthSmoothness;
    }


    // =========================================================
    // CREATE
    // =========================================================

    private void CreateVisual()
    {
        visualRoot =
            new GameObject(
                "TornadoVisual"
            ).transform;


        visualRoot.SetParent(
            transform,
            false
        );


        GameObject meshObject =
            new GameObject(
                "CombinedTornado"
            );


        meshObject.transform.SetParent(
            visualRoot,
            false
        );


        meshFilter =
            meshObject.AddComponent<MeshFilter>();


        meshRenderer =
            meshObject.AddComponent<MeshRenderer>();


        meshRenderer.shadowCastingMode =
            ShadowCastingMode.Off;

        meshRenderer.receiveShadows =
            false;

        meshRenderer.lightProbeUsage =
            LightProbeUsage.Off;

        meshRenderer.reflectionProbeUsage =
            ReflectionProbeUsage.Off;

        meshRenderer.motionVectorGenerationMode =
            MotionVectorGenerationMode.ForceNoMotion;


        Shader shader =
            Shader.Find(
                "Tornado/GradientUnlit"
            );


        if (shader == null)
        {
            Debug.LogError(
                "Tornado/GradientUnlit shader could not be found.",
                this
            );

            enabled = false;

            return;
        }


        materials =
            new Material[StrandCount];


        displayedColors =
            new Color[StrandCount];


        targetColors =
            new Color[StrandCount];


        for (int i = 0;
             i < StrandCount;
             i++)
        {
            Material material =
                new Material(
                    shader
                );


            material.name =
                "TornadoStrand_" +
                i;


            ConfigureMaterial(
                material
            );


            materials[i] =
                material;
        }


        meshRenderer.sharedMaterials =
            materials;


        BuildCombinedMesh();
    }


    // =========================================================
    // BUILD MESH
    // =========================================================

    private void BuildCombinedMesh()
    {
        int verticesPerStrand =
            SegmentsPerStrand *
            TubeSides;


        int totalVertices =
            verticesPerStrand *
            StrandCount;


        Vector3[] vertices =
            new Vector3[
                totalVertices
            ];


        Vector2[] uv =
            new Vector2[
                totalVertices
            ];


        int[][] triangles =
            new int[
                StrandCount
            ][];


        for (int strand = 0;
             strand < StrandCount;
             strand++)
        {
            int vertexOffset =
                strand *
                verticesPerStrand;


            int triangleCount =
                (SegmentsPerStrand - 1) *
                TubeSides *
                6;


            triangles[strand] =
                new int[
                    triangleCount
                ];


            float phase =
                strand *
                Mathf.PI *
                2f /
                StrandCount;


            phase +=
                strand *
                0.11f;


            for (int segment = 0;
                 segment <
                 SegmentsPerStrand;
                 segment++)
            {
                float t =
                    segment /
                    (float)(
                        SegmentsPerStrand -
                        1
                    );


                Vector3 center =
                    GetCenter(
                        t,
                        phase,
                        strand
                    );


                float previousT =
                    Mathf.Max(
                        0f,
                        t - 0.02f
                    );


                float nextT =
                    Mathf.Min(
                        1f,
                        t + 0.02f
                    );


                Vector3 previous =
                    GetCenter(
                        previousT,
                        phase,
                        strand
                    );


                Vector3 next =
                    GetCenter(
                        nextT,
                        phase,
                        strand
                    );


                Vector3 tangent =
                    (
                        next -
                        previous
                    ).normalized;


                Vector3 radial =
                    new Vector3(
                        center.x,
                        0f,
                        center.z
                    );


                if (radial.sqrMagnitude >
                    0.0001f)
                {
                    radial.Normalize();
                }
                else
                {
                    radial =
                        Vector3.right;
                }


                Vector3 binormal =
                    Vector3.Cross(
                        tangent,
                        radial
                    ).normalized;


                /*
                 * Recalculate radial so our little
                 * tube frame stays perpendicular.
                 *
                 * This helps the cross-section remain
                 * properly round while spiraling.
                 */
                radial =
                    Vector3.Cross(
                        binormal,
                        tangent
                    ).normalized;


                float smoothT =
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        t
                    );


                float requestedThickness =
                    Mathf.Lerp(
                        BottomThickness,
                        TopThickness,
                        smoothT
                    );


                float centerRadius =
                    new Vector2(
                        center.x,
                        center.z
                    ).magnitude;


                float spacing =
                    Mathf.PI *
                    2f *
                    centerRadius /
                    StrandCount;


                /*
                 * Slightly less overlap than before.
                 *
                 * The strands still look connected,
                 * but don't constantly intersect.
                 */
                float thickness =
                    Mathf.Min(
                        requestedThickness,
                        spacing *
                        0.28f
                    );


                if (strand == 0)
                {
                    thickness *=
                        1.03f;
                }


                for (int side = 0;
                     side < TubeSides;
                     side++)
                {
                    float angle =
                        side *
                        Mathf.PI *
                        2f /
                        TubeSides;


                    Vector3 normal =
                        (
                            radial *
                            Mathf.Cos(angle)
                            +
                            binormal *
                            Mathf.Sin(angle)
                        ).normalized;


                    Vector3 offset =
                        normal *
                        thickness;


                    int index =
                        vertexOffset +
                        segment *
                        TubeSides +
                        side;


                    vertices[index] =
                        center +
                        offset;


                    uv[index] =
                        new Vector2(
                            side /
                            (float)TubeSides,
                            t
                        );
                }
            }


            int triangleIndex = 0;


            for (int segment = 0;
                 segment <
                 SegmentsPerStrand - 1;
                 segment++)
            {
                for (int side = 0;
                     side < TubeSides;
                     side++)
                {
                    int nextSide =
                        (side + 1) %
                        TubeSides;


                    int a =
                        vertexOffset +
                        segment *
                        TubeSides +
                        side;


                    int b =
                        vertexOffset +
                        segment *
                        TubeSides +
                        nextSide;


                    int c =
                        vertexOffset +
                        (segment + 1) *
                        TubeSides +
                        side;


                    int d =
                        vertexOffset +
                        (segment + 1) *
                        TubeSides +
                        nextSide;


                    triangles[strand]
                        [triangleIndex++] = a;

                    triangles[strand]
                        [triangleIndex++] = b;

                    triangles[strand]
                        [triangleIndex++] = c;


                    triangles[strand]
                        [triangleIndex++] = b;

                    triangles[strand]
                        [triangleIndex++] = d;

                    triangles[strand]
                        [triangleIndex++] = c;
                }
            }
        }


        mesh =
            new Mesh();


        mesh.name =
            "CottonCandyTornadoMesh";


        mesh.vertices =
            vertices;


        mesh.uv =
            uv;


        mesh.subMeshCount =
            StrandCount;


        for (int i = 0;
             i < StrandCount;
             i++)
        {
            mesh.SetTriangles(
                triangles[i],
                i,
                false
            );
        }


        /*
         * IMPORTANT.
         *
         * Shader uses these normals to make the
         * strands actually look round/3D.
         */
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();


        mesh.UploadMeshData(
            true
        );


        meshFilter.sharedMesh =
            mesh;
    }


    // =========================================================
    // TORNADO PROFILE
    // =========================================================

    private static Vector3 GetCenter(
        float t,
        float phase,
        int strand)
    {
        float profile =
            Mathf.Pow(
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                ),
                1.35f
            );


        float crown =
            1f +
            0.10f *
            Mathf.SmoothStep(
                0.62f,
                0.88f,
                t
            )
            -
            0.06f *
            Mathf.SmoothStep(
                0.92f,
                1f,
                t
            );


        float radius =
            Mathf.Lerp(
                BottomRadius,
                TopRadius,
                profile
            ) *
            crown;


        float angle =
            phase +
            t *
            Turns *
            Mathf.PI *
            2f;


        float yOffset =
            (strand % 3) *
            0.018f;


        return new Vector3(
            Mathf.Cos(angle) *
            radius,

            t *
            Height +
            yOffset,

            Mathf.Sin(angle) *
            radius
        );
    }


    // =========================================================
    // COLORS
    // =========================================================

    private void InitializeColors()
    {
        if (tornadoColors == null)
        {
            return;
        }


        Color initial =
            tornadoColors.GetMainColor();


        knownColors.Clear();


        knownColors.Add(
            initial
        );


        Color visualColor =
            GetVisualTornadoColor(
                initial
            );


        for (int i = 0;
             i < StrandCount;
             i++)
        {
            displayedColors[i] =
                visualColor;


            targetColors[i] =
                visualColor;


            ApplyColor(
                materials[i],
                visualColor
            );
        }


        nextStrand =
            StrandCount > 1
                ? 1
                : 0;


        lastColorRevision =
            -1;


        RefreshColorTargets();
    }


    private void RefreshColorTargets()
    {
        if (tornadoColors == null)
        {
            return;
        }


        IReadOnlyList<
            TornadoColors.ColorAmount
        > palette =
            tornadoColors.Colors;


        Color main =
            tornadoColors
                .GetMainColor();


        for (int i =
                 knownColors.Count - 1;
             i >= 0;
             i--)
        {
            if (ContainsColor(
                    palette,
                    knownColors[i]))
            {
                continue;
            }


            ReplaceTargetColor(
                knownColors[i],
                main
            );


            knownColors.RemoveAt(
                i
            );
        }


        for (int i = 0;
             i < palette.Count;
             i++)
        {
            Color gameplayColor =
                palette[i].color;


            if (ContainsColor(
                    knownColors,
                    gameplayColor))
            {
                continue;
            }


            knownColors.Add(
                gameplayColor
            );


            targetColors[nextStrand] =
                GetVisualTornadoColor(
                    gameplayColor
                );


            nextStrand =
                (
                    nextStrand +
                    1
                ) %
                StrandCount;
        }


        lastColorRevision =
            tornadoColors.Revision;


        colorsAnimating =
            true;
    }


    private void AnimateColors()
    {
        float blend =
            1f -
            Mathf.Exp(
                -ColorTransitionSpeed *
                Time.deltaTime
            );


        bool stillAnimating =
            false;


        for (int i = 0;
             i < StrandCount;
             i++)
        {
            Color current =
                displayedColors[i];


            Color target =
                targetColors[i];


            if (!SameColorLoose(
                    current,
                    target))
            {
                current =
                    Color.Lerp(
                        current,
                        target,
                        blend
                    );


                stillAnimating =
                    true;
            }
            else
            {
                current =
                    target;
            }


            displayedColors[i] =
                current;


            ApplyColor(
                materials[i],
                current
            );
        }


        colorsAnimating =
            stillAnimating;
    }


    private static Color GetVisualTornadoColor(
        Color gameplayColor)
    {
        Color color =
            KawaiiColors.GetTornadoColor(
                gameplayColor
            );


        color.a = 1f;


        return color;
    }


    // =========================================================
    // MATERIAL
    // =========================================================

    private void ConfigureMaterial(
        Material material)
    {
        /*
         * OPAQUE DEPTH RENDERING.
         *
         * Candy / other tornadoes / the back side
         * of this tornado now sort using the depth
         * buffer rather than transparent sorting.
         */
        material.renderQueue =
            (int)RenderQueue.Geometry;


        if (fiberTexture != null)
        {
            material.SetTexture(
                "_FiberTex",
                fiberTexture
            );
        }


        material.SetFloat(
            "_FiberTiling",
            FiberTiling
        );


        material.SetFloat(
            "_FiberStrength",
            FiberStrength
        );


        material.SetFloat(
            "_FiberContrast",
            FiberContrast
        );


        material.SetFloat(
            "_BottomOpacity",
            BottomOpacity
        );


        material.SetFloat(
            "_TopOpacity",
            TopOpacity
        );


        material.SetFloat(
            "_FadeStart",
            FadeStart
        );


        material.SetColor(
            "_RimColor",
            RimColor
        );


        material.SetFloat(
            "_RimStrength",
            RimStrength
        );


        material.SetFloat(
            "_RimPower",
            RimPower
        );


        material.SetVector(
            "_FakeLightDirection",
            new Vector4(
                FakeLightDirection.x,
                FakeLightDirection.y,
                FakeLightDirection.z,
                0f
            )
        );


        material.SetFloat(
            "_AmbientLight",
            AmbientLight
        );


        material.SetFloat(
            "_DirectLight",
            DirectLight
        );
    }


    private static void ApplyColor(
        Material material,
        Color visualColor)
    {
        if (material == null)
        {
            return;
        }


        visualColor.a =
            1f;


        material.SetColor(
            "_BaseColor",
            visualColor
        );
    }


    private void ReplaceTargetColor(
        Color oldGameplayColor,
        Color replacementGameplayColor)
    {
        Color oldVisual =
            GetVisualTornadoColor(
                oldGameplayColor
            );


        Color replacementVisual =
            GetVisualTornadoColor(
                replacementGameplayColor
            );


        for (int i = 0;
             i < targetColors.Length;
             i++)
        {
            if (SameColor(
                    targetColors[i],
                    oldVisual))
            {
                targetColors[i] =
                    replacementVisual;
            }
        }
    }


    private static bool ContainsColor(
        IReadOnlyList<
            TornadoColors.ColorAmount
        > colors,
        Color target)
    {
        for (int i = 0;
             i < colors.Count;
             i++)
        {
            if (SameColor(
                    colors[i].color,
                    target))
            {
                return true;
            }
        }


        return false;
    }


    private static bool ContainsColor(
        IReadOnlyList<Color> colors,
        Color target)
    {
        for (int i = 0;
             i < colors.Count;
             i++)
        {
            if (SameColor(
                    colors[i],
                    target))
            {
                return true;
            }
        }


        return false;
    }


    private static bool SameColor(
        Color a,
        Color b)
    {
        float r =
            a.r - b.r;

        float g =
            a.g - b.g;

        float blue =
            a.b - b.b;


        return
            r * r +
            g * g +
            blue * blue <
            0.000001f;
    }


    private static bool SameColorLoose(
        Color a,
        Color b)
    {
        float r =
            a.r - b.r;

        float g =
            a.g - b.g;

        float blue =
            a.b - b.b;


        return
            r * r +
            g * g +
            blue * blue <
            0.0001f;
    }


    // =========================================================
    // PUBLIC
    // =========================================================

    public Vector3 GetVisibleTopWorldPosition()
    {
        if (visualRoot != null)
        {
            return
                visualRoot.TransformPoint(
                    new Vector3(
                        0f,
                        Height + 0.036f,
                        0f
                    )
                );
        }


        float scale =
            tornado != null
                ? Mathf.Max(
                    0.01f,
                    tornado.VisualScale
                )
                : 1f;


        return
            transform.TransformPoint(
                new Vector3(
                    0f,
                    Height *
                    scale,
                    0f
                )
            );
    }


    // =========================================================
    // CLEANUP
    // =========================================================

    private void OnDestroy()
    {
        if (mesh != null)
        {
            Destroy(
                mesh
            );

            mesh = null;
        }


        if (materials != null)
        {
            for (int i = 0;
                 i < materials.Length;
                 i++)
            {
                if (materials[i] != null)
                {
                    Destroy(
                        materials[i]
                    );
                }
            }
        }


        if (visualRoot != null)
        {
            Destroy(
                visualRoot.gameObject
            );


            visualRoot = null;
        }
    }
}