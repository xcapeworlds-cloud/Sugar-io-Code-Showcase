using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class TornadoAISpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Tornado aiTornadoPrefab;

    [SerializeField]
    private PlayingField playingField;

    [Header("Spawn Safety")]
    [SerializeField]
    private float spawnY;

    [SerializeField, Min(0f)]
    private float playerSafeDistance = 10f;

    [SerializeField, Min(0f)]
    private float tornadoSpacing = 4f;

    [SerializeField, Range(1, 100)]
    private int spawnAttempts = 40;

    private readonly List<Tornado> spawnedAI =
        new();

    private Tornado player;

    private int nextColorIndex;
    private float distributionRotation;

    public IReadOnlyList<Tornado> SpawnedAI =>
        spawnedAI;


    // ==================================================
    // SPAWN MATCH AI
    // ==================================================

    public void SpawnForMatch(
        int requestedCount,
        Action<Tornado> onSpawned)
    {
        ClearSpawnedAI();
        ResolveReferences();

        if (aiTornadoPrefab == null ||
            playingField == null)
        {
            Debug.LogError(
                "TornadoAISpawner requires an AI prefab and PlayingField.",
                this
            );

            return;
        }

        int count =
            Mathf.Max(
                0,
                requestedCount
            );

        distributionRotation =
            UnityEngine.Random.Range(
                0f,
                Mathf.PI * 2f
            );

        nextColorIndex =
            0;

        for (int i = 0;
             i < count;
             i++)
        {
            Tornado tornado =
                TrySpawn(
                    i,
                    count
                );

            if (tornado != null)
            {
                onSpawned?.Invoke(
                    tornado
                );
            }
        }
    }


    // ==================================================
    // CLEAR AI
    // ==================================================

    public void ClearSpawnedAI()
    {
        for (int i =
                 spawnedAI.Count - 1;
             i >= 0;
             i--)
        {
            Tornado tornado =
                spawnedAI[i];

            if (tornado == null)
                continue;

            tornado.gameObject.SetActive(
                false
            );

            Destroy(
                tornado.gameObject
            );
        }

        spawnedAI.Clear();
    }


    // ==================================================
    // REFERENCES
    // ==================================================

    private void ResolveReferences()
    {
        if (playingField == null)
        {
            playingField =
                PlayingField.Instance != null
                    ? PlayingField.Instance
                    : FindFirstObjectByType<PlayingField>();
        }

        player =
            null;

        var tornadoes =
            Tornado.ActiveTornadoes;

        for (int i = 0;
             i < tornadoes.Count;
             i++)
        {
            Tornado candidate =
                tornadoes[i];

            if (candidate != null &&
                !candidate.IsAI &&
                !candidate.IsDefeated)
            {
                player =
                    candidate;

                break;
            }
        }
    }


    // ==================================================
    // SPAWN
    // ==================================================

    private Tornado TrySpawn(
        int spawnIndex,
        int total)
    {
        if (!TryGetSpawnPosition(
                spawnIndex,
                total,
                out Vector3 position))
        {
            Debug.LogWarning(
                $"Could not find a safe spawn for AI {spawnIndex + 1}.",
                this
            );

            return null;
        }

        Tornado ai =
            Instantiate(
                aiTornadoPrefab,
                position,
                Quaternion.identity,
                transform
            );

        if (ai == null)
            return null;

        TornadoAI controller =
            ai.GetComponent<TornadoAI>();

        if (controller != null)
        {
            controller.ConfigurePersonality(
                spawnIndex
            );
        }

        TornadoColors colors =
            ai.Colors;

        if (colors != null &&
            KawaiiColors.Count > 0)
        {
            colors.SetStartingColor(
                KawaiiColors.Get(
                    nextColorIndex
                )
            );

            nextColorIndex =
                (nextColorIndex + 1) %
                KawaiiColors.Count;
        }

        spawnedAI.Add(
            ai
        );

        return ai;
    }


    // ==================================================
    // SPAWN POSITION
    // ==================================================

    private bool TryGetSpawnPosition(
        int index,
        int total,
        out Vector3 position)
    {
        for (int attempt = 0;
             attempt < spawnAttempts;
             attempt++)
        {
            Vector3 candidate =
                GetDistributedPoint(
                    index,
                    total,
                    attempt
                );

            if (!IsSafe(
                    candidate
                ))
            {
                continue;
            }

            position =
                candidate;

            return true;
        }

        position =
            playingField.GetRandomPoint();

        position.y =
            spawnY;

        return true;
    }


    private Vector3 GetDistributedPoint(
        int index,
        int total,
        int attempt)
    {
        Bounds bounds =
            playingField.PlayableBounds;

        float fraction =
            (index + 0.65f) /
            Mathf.Max(
                1f,
                total
            );

        float radius =
            Mathf.Sqrt(
                fraction
            ) *
            0.88f;

        float angle =
            distributionRotation +
            index *
            2.399963f;

        Vector2 jitter =
            UnityEngine.Random
                .insideUnitCircle *
            (0.04f +
             attempt * 0.012f);

        float x =
            Mathf.Clamp(
                Mathf.Cos(angle) *
                radius +
                jitter.x,
                -0.92f,
                0.92f
            );

        float z =
            Mathf.Clamp(
                Mathf.Sin(angle) *
                radius +
                jitter.y,
                -0.92f,
                0.92f
            );

        return new Vector3(
            bounds.center.x +
            x *
            bounds.extents.x,

            spawnY,

            bounds.center.z +
            z *
            bounds.extents.z
        );
    }


    // ==================================================
    // SPAWN SAFETY
    // ==================================================

    private bool IsSafe(
        Vector3 position)
    {
        if (player != null &&
            !player.IsDefeated &&
            FlatDistanceSquared(
                position,
                player.transform.position
            ) <
            playerSafeDistance *
            playerSafeDistance)
        {
            return false;
        }

        var tornadoes =
            Tornado.ActiveTornadoes;

        for (int i = 0;
             i < tornadoes.Count;
             i++)
        {
            Tornado other =
                tornadoes[i];

            if (other != null &&
                !other.IsDefeated &&
                FlatDistanceSquared(
                    position,
                    other.transform.position
                ) <
                tornadoSpacing *
                tornadoSpacing)
            {
                return false;
            }
        }

        var destructibles =
            TornadoDestructible
                .ActiveDestructibles;

        for (int i = 0;
             i < destructibles.Count;
             i++)
        {
            TornadoDestructible destructible =
                destructibles[i];

            if (destructible != null &&
                !destructible.IsWaitingForRespawn &&
                destructible.BlocksTornadoPosition(
                    position,
                    0.5f
                ))
            {
                return false;
            }
        }

        return true;
    }


    // ==================================================
    // DISTANCE
    // ==================================================

    private static float FlatDistanceSquared(
        Vector3 a,
        Vector3 b)
    {
        float x =
            a.x - b.x;

        float z =
            a.z - b.z;

        return
            x * x +
            z * z;
    }


    // ==================================================
    // VALIDATION
    // ==================================================

    private void OnValidate()
    {
        playerSafeDistance =
            Mathf.Max(
                0f,
                playerSafeDistance
            );

        tornadoSpacing =
            Mathf.Max(
                0f,
                tornadoSpacing
            );

        spawnAttempts =
            Mathf.Clamp(
                spawnAttempts,
                1,
                100
            );
    }
}