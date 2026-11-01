using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class TornadoRuntimeCollider : MonoBehaviour
{
    private const string ColliderObjectName =
        "__TORNADO_RUNTIME_BLOCKER__";

    private TornadoDestructible destructible;

    private GameObject blockerObject;
    private BoxCollider blocker;

    // =========================================================
    // AUTO INSTALL
    // =========================================================

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallSystem()
    {
        // Prevent duplicate managers.
        TornadoRuntimeColliderInstaller existing =
            Object.FindFirstObjectByType<
                TornadoRuntimeColliderInstaller>();

        if (existing != null)
        {
            return;
        }

        GameObject manager =
            new GameObject(
                "__TORNADO_COLLIDER_SYSTEM__"
            );

        Object.DontDestroyOnLoad(
            manager
        );

        manager.AddComponent<
            TornadoRuntimeColliderInstaller>();
    }

    // =========================================================
    // SETUP
    // =========================================================

    public void Initialize(
        TornadoDestructible target)
    {
        if (target == null)
        {
            return;
        }

        destructible =
            target;

        CreateBlocker();
    }

    private void CreateBlocker()
    {
        if (destructible == null)
        {
            return;
        }

        // ---------------------------------------------
        // ALREADY CREATED
        // ---------------------------------------------

        Transform existing =
            destructible.transform.Find(
                ColliderObjectName
            );

        if (existing != null)
        {
            blockerObject =
                existing.gameObject;

            blocker =
                blockerObject.GetComponent<
                    BoxCollider>();

            if (blocker == null)
            {
                blocker =
                    blockerObject.AddComponent<
                        BoxCollider>();
            }

            blocker.isTrigger =
                false;

            return;
        }

        // ---------------------------------------------
        // FIND ALL VISIBLE MESHES
        // ---------------------------------------------

        Renderer[] renderers =
            destructible.GetComponentsInChildren<
                Renderer>(true);

        if (renderers == null ||
            renderers.Length == 0)
        {
            return;
        }

        bool found =
            false;

        Bounds worldBounds =
            new Bounds();

        for (int i = 0;
             i < renderers.Length;
             i++)
        {
            Renderer renderer =
                renderers[i];

            if (renderer == null)
            {
                continue;
            }

            // Particle effects should not determine
            // the object's physical size.
            if (renderer is ParticleSystemRenderer)
            {
                continue;
            }

            if (!renderer.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (!renderer.enabled)
            {
                continue;
            }

            if (!found)
            {
                worldBounds =
                    renderer.bounds;

                found =
                    true;
            }
            else
            {
                worldBounds.Encapsulate(
                    renderer.bounds
                );
            }
        }

        if (!found)
        {
            return;
        }

        // ---------------------------------------------
        // CREATE CHEAP RUNTIME BOX
        // ---------------------------------------------

        blockerObject =
            new GameObject(
                ColliderObjectName
            );

        blockerObject.layer =
            destructible.gameObject.layer;

        Transform blockerTransform =
            blockerObject.transform;

        blockerTransform.SetParent(
            destructible.transform,
            false
        );

        blockerTransform.localPosition =
            Vector3.zero;

        blockerTransform.localRotation =
            Quaternion.identity;

        blockerTransform.localScale =
            Vector3.one;

        blocker =
            blockerObject.AddComponent<
                BoxCollider>();

        blocker.isTrigger =
            false;

        SetColliderFromWorldBounds(
            worldBounds
        );

        blocker.enabled =
            !destructible.IsActivated &&
            !destructible.IsWaitingForRespawn;
    }

    // =========================================================
    // SIZE COLLIDER TO THE OBJECT
    // =========================================================

    private void SetColliderFromWorldBounds(
        Bounds worldBounds)
    {
        if (blocker == null ||
            destructible == null)
        {
            return;
        }

        Transform root =
            destructible.transform;

        Vector3 min =
            worldBounds.min;

        Vector3 max =
            worldBounds.max;

        Vector3[] corners =
        {
            new Vector3(
                min.x,
                min.y,
                min.z
            ),

            new Vector3(
                min.x,
                min.y,
                max.z
            ),

            new Vector3(
                min.x,
                max.y,
                min.z
            ),

            new Vector3(
                min.x,
                max.y,
                max.z
            ),

            new Vector3(
                max.x,
                min.y,
                min.z
            ),

            new Vector3(
                max.x,
                min.y,
                max.z
            ),

            new Vector3(
                max.x,
                max.y,
                min.z
            ),

            new Vector3(
                max.x,
                max.y,
                max.z
            )
        };

        Vector3 first =
            root.InverseTransformPoint(
                corners[0]
            );

        Bounds localBounds =
            new Bounds(
                first,
                Vector3.zero
            );

        for (int i = 1;
             i < corners.Length;
             i++)
        {
            localBounds.Encapsulate(
                root.InverseTransformPoint(
                    corners[i]
                )
            );
        }

        blocker.center =
            localBounds.center;

        blocker.size =
            new Vector3(
                Mathf.Max(
                    0.05f,
                    localBounds.size.x
                ),

                Mathf.Max(
                    0.05f,
                    localBounds.size.y
                ),

                Mathf.Max(
                    0.05f,
                    localBounds.size.z
                )
            );
    }

    // =========================================================
    // ENABLE / DISABLE COLLIDER
    // =========================================================

    private void LateUpdate()
    {
        if (destructible == null)
        {
            return;
        }

        if (blocker == null)
        {
            CreateBlocker();

            if (blocker == null)
            {
                return;
            }
        }

        /*
         * INTACT OBJECT
         *     collider ON
         *
         * OBJECT ACTIVATED / PIECES FLYING
         *     collider OFF
         *
         * WAITING FOR RESPAWN
         *     collider OFF
         *
         * RESPAWN FINISHED
         *     collider ON
         */

        bool shouldBlock =
            destructible.isActiveAndEnabled &&
            !destructible.IsActivated &&
            !destructible.IsWaitingForRespawn;

        if (blocker.enabled !=
            shouldBlock)
        {
            blocker.enabled =
                shouldBlock;
        }
    }

    private void OnDestroy()
    {
        if (blockerObject != null)
        {
            Destroy(
                blockerObject
            );
        }
    }
}


// =============================================================
// AUTO INSTALLER
//
// YOU DO NOT PUT THIS ON ANYTHING.
// IT FINDS TORNADODESTRUCTIBLE OBJECTS AUTOMATICALLY.
// =============================================================

public sealed class TornadoRuntimeColliderInstaller :
    MonoBehaviour
{
    private const float ScanInterval =
        0.25f;

    private float nextScanTime;

    private readonly HashSet<
        TornadoDestructible> installed =
            new();

    private void Update()
    {
        if (Time.unscaledTime <
            nextScanTime)
        {
            return;
        }

        nextScanTime =
            Time.unscaledTime +
            ScanInterval;

        InstallMissingColliders();

        CleanupDestroyedObjects();
    }

    private void InstallMissingColliders()
    {
        IReadOnlyList<TornadoDestructible>
            destructibles =
                TornadoDestructible
                    .ActiveDestructibles;

        if (destructibles == null ||
            destructibles.Count == 0)
        {
            return;
        }

        for (int i = 0;
             i < destructibles.Count;
             i++)
        {
            TornadoDestructible destructible =
                destructibles[i];

            if (destructible == null)
            {
                continue;
            }

            if (installed.Contains(
                    destructible))
            {
                continue;
            }

            TornadoRuntimeCollider runtime =
                destructible.GetComponent<
                    TornadoRuntimeCollider>();

            if (runtime == null)
            {
                runtime =
                    destructible.gameObject
                        .AddComponent<
                            TornadoRuntimeCollider>();
            }

            runtime.Initialize(
                destructible
            );

            installed.Add(
                destructible
            );
        }
    }

    private void CleanupDestroyedObjects()
    {
        installed.RemoveWhere(
            destructible =>
                destructible == null
        );
    }
}