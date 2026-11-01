using UnityEngine;

[DisallowMultipleComponent]
public sealed class CandySwirlVFX : MonoBehaviour
{
    // ==================================================
    // VISUAL SETTINGS
    // ==================================================

    private const float MinimumLifetime = 0.65f;
    private const float MaximumLifetime = 1.05f;
    private const float LifetimePerWorldUnit = 0.035f;

    private const float TopHeight = 1.03f;

    private const float EndRadius = 0.05f;
    private const float TopOrbitRadiusMultiplier = 1.15f;

    private const float Turns = 1.70f;

    private const float StartScaleMultiplier = 0.92f;
    private const float EndScaleMultiplier = 0.03f;


    // ==================================================
    // STATE
    // ==================================================

    private Tornado tornado;

    private float age;
    private float startingAngle;
    private float startingRadius;
    private float startingHeight;
    private float lifetime;

    private Vector3 originalScale;


    // ==================================================
    // SPAWN
    // ==================================================

    public static void Spawn(
        Candy source,
        Tornado targetTornado)
    {
        if (source == null ||
            targetTornado == null)
        {
            return;
        }


        Renderer[] sourceRenderers =
            source.GetComponentsInChildren<Renderer>(
                true
            );

        if (sourceRenderers == null ||
            sourceRenderers.Length == 0)
        {
            return;
        }


        GameObject root =
            new GameObject(
                "__EATEN_CANDY_SWIRL__"
            );

        root.transform.position =
            source.transform.position;

        root.transform.rotation =
            source.transform.rotation;

        root.transform.localScale =
            source.transform.lossyScale;


        // ==================================================
        // COPY VISUALS ONLY
        // ==================================================

        for (int i = 0;
             i < sourceRenderers.Length;
             i++)
        {
            Renderer sourceRenderer =
                sourceRenderers[i];

            if (sourceRenderer == null ||
                !sourceRenderer.enabled)
            {
                continue;
            }


            MeshRenderer sourceMeshRenderer =
                sourceRenderer as MeshRenderer;

            SkinnedMeshRenderer sourceSkinnedRenderer =
                sourceRenderer as SkinnedMeshRenderer;


            if (sourceMeshRenderer != null)
            {
                CopyMeshRenderer(
                    source.transform,
                    sourceMeshRenderer,
                    root.transform
                );
            }
            else if (sourceSkinnedRenderer != null)
            {
                CopySkinnedRenderer(
                    source.transform,
                    sourceSkinnedRenderer,
                    root.transform
                );
            }
        }


        CandySwirlVFX swirl =
            root.AddComponent<CandySwirlVFX>();

        swirl.Initialize(
            targetTornado
        );
    }


    // ==================================================
    // COPY MESH RENDERER
    // ==================================================

    private static void CopyMeshRenderer(
        Transform sourceRoot,
        MeshRenderer sourceRenderer,
        Transform destinationRoot)
    {
        MeshFilter sourceFilter =
            sourceRenderer.GetComponent<MeshFilter>();

        if (sourceFilter == null ||
            sourceFilter.sharedMesh == null)
        {
            return;
        }


        GameObject visual =
            new GameObject(
                sourceRenderer.gameObject.name
            );

        Transform visualTransform =
            visual.transform;

        visualTransform.SetParent(
            destinationRoot,
            false
        );


        CopyRelativeTransform(
            sourceRoot,
            sourceRenderer.transform,
            visualTransform
        );


        MeshFilter filter =
            visual.AddComponent<MeshFilter>();

        filter.sharedMesh =
            sourceFilter.sharedMesh;


        MeshRenderer renderer =
            visual.AddComponent<MeshRenderer>();

        renderer.sharedMaterials =
            sourceRenderer.sharedMaterials;

        CopyMaterialProperties(
            sourceRenderer,
            renderer
        );

        renderer.shadowCastingMode =
            sourceRenderer.shadowCastingMode;

        renderer.receiveShadows =
            sourceRenderer.receiveShadows;

        renderer.lightProbeUsage =
            sourceRenderer.lightProbeUsage;

        renderer.reflectionProbeUsage =
            sourceRenderer.reflectionProbeUsage;
    }


    // ==================================================
    // COPY SKINNED RENDERER
    // ==================================================

    private static void CopySkinnedRenderer(
        Transform sourceRoot,
        SkinnedMeshRenderer sourceRenderer,
        Transform destinationRoot)
    {
        if (sourceRenderer.sharedMesh == null)
        {
            return;
        }


        GameObject visual =
            new GameObject(
                sourceRenderer.gameObject.name
            );

        Transform visualTransform =
            visual.transform;

        visualTransform.SetParent(
            destinationRoot,
            false
        );


        CopyRelativeTransform(
            sourceRoot,
            sourceRenderer.transform,
            visualTransform
        );


        MeshFilter filter =
            visual.AddComponent<MeshFilter>();

        Mesh bakedMesh =
            new Mesh
            {
                name =
                    sourceRenderer.sharedMesh.name +
                    "_Swirl"
            };

        sourceRenderer.BakeMesh(
            bakedMesh
        );

        filter.sharedMesh =
            bakedMesh;


        MeshRenderer renderer =
            visual.AddComponent<MeshRenderer>();

        renderer.sharedMaterials =
            sourceRenderer.sharedMaterials;

        CopyMaterialProperties(
            sourceRenderer,
            renderer
        );

        renderer.shadowCastingMode =
            sourceRenderer.shadowCastingMode;

        renderer.receiveShadows =
            sourceRenderer.receiveShadows;
    }


    // ==================================================
    // COPY PER-CANDY COLOR
    // ==================================================

    private static void CopyMaterialProperties(
        Renderer source,
        Renderer destination)
    {
        if (source == null || destination == null)
        {
            return;
        }

        Material[] materials = source.sharedMaterials;
        MaterialPropertyBlock properties =
            new MaterialPropertyBlock();

        for (int i = 0; i < materials.Length; i++)
        {
            source.GetPropertyBlock(properties, i);
            destination.SetPropertyBlock(properties, i);
            properties.Clear();
        }
    }


    // ==================================================
    // RELATIVE TRANSFORM
    // ==================================================

    private static void CopyRelativeTransform(
        Transform sourceRoot,
        Transform source,
        Transform destination)
    {
        destination.localPosition =
            sourceRoot.InverseTransformPoint(
                source.position
            );

        destination.localRotation =
            Quaternion.Inverse(
                sourceRoot.rotation
            ) *
            source.rotation;


        Vector3 rootScale =
            sourceRoot.lossyScale;

        Vector3 sourceScale =
            source.lossyScale;


        destination.localScale =
            new Vector3(
                SafeDivide(
                    sourceScale.x,
                    rootScale.x
                ),

                SafeDivide(
                    sourceScale.y,
                    rootScale.y
                ),

                SafeDivide(
                    sourceScale.z,
                    rootScale.z
                )
            );
    }


    private static float SafeDivide(
        float value,
        float divisor)
    {
        if (Mathf.Abs(divisor) <
            0.0001f)
        {
            return value;
        }

        return value /
               divisor;
    }


    // ==================================================
    // INITIALIZE
    // ==================================================

    private void Initialize(
        Tornado targetTornado)
    {
        tornado =
            targetTornado;

        age =
            0f;

        Vector3 offset =
            transform.position -
            tornado.BottomTipWorldPosition;

        startingHeight =
            offset.y;

        offset.y =
            0f;

        startingRadius =
            Mathf.Max(
                0.25f,
                offset.magnitude
            );

        lifetime = Mathf.Clamp(
            MinimumLifetime +
            startingRadius * LifetimePerWorldUnit,
            MinimumLifetime,
            MaximumLifetime
        );

        startingAngle =
            Mathf.Atan2(
                offset.z,
                offset.x
            ) * Mathf.Rad2Deg;

        originalScale =
            transform.localScale;
    }


    // ==================================================
    // UPDATE
    // ==================================================

    private void Update()
    {
        if (tornado == null ||
            tornado.IsDefeated)
        {
            Destroy(
                gameObject
            );

            return;
        }


        age +=
            Time.deltaTime;


        float progress =
            Mathf.Clamp01(
                age /
                lifetime
            );


        // ==================================================
        // HEIGHT
        // ==================================================

        float heightProgress =
            Mathf.SmoothStep(
                0f,
                1f,
                progress
            );

        float targetHeight =
            tornado.GetVisibleHeight() *
            TopHeight;

        float height =
            Mathf.Lerp(
                startingHeight,
                targetHeight,
                heightProgress
            );


        // ==================================================
        // RADIUS
        // ==================================================

        float topOrbitRadius = Mathf.Max(
            0.18f,
            tornado.GetVisibleRadius() * TopOrbitRadiusMultiplier
        );

        float radius;

        if (progress < 0.82f)
        {
            float funnelProgress = Mathf.SmoothStep(
                0f,
                1f,
                progress / 0.82f
            );

            radius = Mathf.Lerp(
                startingRadius,
                topOrbitRadius,
                funnelProgress
            );
        }
        else
        {
            float finishProgress = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.InverseLerp(0.82f, 1f, progress)
            );

            radius = Mathf.Lerp(
                topOrbitRadius,
                Mathf.Max(0.02f, tornado.VisualScale * EndRadius),
                finishProgress
            );
        }


        // ==================================================
        // ORBIT
        // ==================================================

        float angle =
            startingAngle +
            progress *
            Turns *
            360f;

        float radians =
            angle *
            Mathf.Deg2Rad;


        Vector3 orbitOffset =
            new Vector3(
                Mathf.Cos(radians) *
                radius,

                height,

                Mathf.Sin(radians) *
                radius
            );


        transform.position =
            tornado.BottomTipWorldPosition +
            orbitOffset;


        // ==================================================
        // ROTATION
        // ==================================================

        transform.Rotate(
            180f *
            Time.deltaTime,

            420f *
            Time.deltaTime,

            120f *
            Time.deltaTime,

            Space.Self
        );


        // ==================================================
        // SCALE
        // ==================================================

        float scaleMultiplier = StartScaleMultiplier;

        if (progress > 0.72f)
        {
            float shrinkProgress = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.InverseLerp(0.72f, 1f, progress)
            );

            scaleMultiplier = Mathf.Lerp(
                StartScaleMultiplier,
                EndScaleMultiplier,
                shrinkProgress
            );
        }

        transform.localScale =
            originalScale *
            scaleMultiplier;


        // ==================================================
        // FINISH
        // ==================================================

        if (progress >= 1f)
        {
            Destroy(
                gameObject
            );
        }
    }


    // ==================================================
    // CLEANUP
    // ==================================================

    private void OnDestroy()
    {
        MeshFilter[] filters =
            GetComponentsInChildren<MeshFilter>(
                true
            );

        for (int i = 0;
             i < filters.Length;
             i++)
        {
            MeshFilter filter =
                filters[i];

            if (filter == null ||
                filter.sharedMesh == null)
            {
                continue;
            }


            if (filter.sharedMesh.name.EndsWith(
                    "_Swirl"))
            {
                Destroy(
                    filter.sharedMesh
                );
            }
        }
    }
}