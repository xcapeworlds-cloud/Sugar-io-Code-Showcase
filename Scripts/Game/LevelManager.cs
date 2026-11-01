using UnityEngine;

[DisallowMultipleComponent]
public sealed class LevelManager : MonoBehaviour
{
    [Header("Levels")]
    [Tooltip("Parent object containing Level_01, Level_02, etc.")]
    [SerializeField]
    private Transform levelsParent;

    [SerializeField, Min(0)]
    private int startingLevel = 0;


    public int CurrentLevel
    {
        get;
        private set;
    } = -1;


    public int LevelCount =>
        levelsParent != null
            ? levelsParent.childCount
            : 0;


    public GameObject CurrentLevelObject
    {
        get
        {
            if (levelsParent == null ||
                CurrentLevel < 0 ||
                CurrentLevel >= LevelCount)
            {
                return null;
            }


            return levelsParent
                .GetChild(CurrentLevel)
                .gameObject;
        }
    }


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (levelsParent == null)
        {
            Debug.LogError(
                "LevelManager needs a Levels Parent assigned.",
                this
            );

            enabled = false;
            return;
        }


        DisableAllLevels();
    }


    private void Start()
    {
        if (LevelCount <= 0)
        {
            Debug.LogError(
                "The Levels parent has no level objects.",
                this
            );

            enabled = false;
            return;
        }


        int level =
            Mathf.Clamp(
                startingLevel,
                0,
                LevelCount - 1
            );


        LoadLevel(level);
    }


    // =========================================================
    // LOAD
    // =========================================================

    public void LoadLevel(
        int index)
    {
        if (levelsParent == null ||
            LevelCount <= 0)
        {
            return;
        }


        if (index < 0 ||
            index >= LevelCount)
        {
            Debug.LogError(
                $"Level {index} does not exist.",
                this
            );

            return;
        }


        // Don't disable/re-enable the same
        // level for no reason.
        if (CurrentLevel == index)
        {
            GameObject current =
                levelsParent
                    .GetChild(index)
                    .gameObject;


            if (!current.activeSelf)
            {
                current.SetActive(true);
            }


            return;
        }


        // Only deactivate the previous level.
        // Cheaper than looping through every
        // level on every change.
        if (CurrentLevel >= 0 &&
            CurrentLevel < LevelCount)
        {
            levelsParent
                .GetChild(CurrentLevel)
                .gameObject
                .SetActive(false);
        }


        CurrentLevel =
            index;


        GameObject level =
            levelsParent
                .GetChild(CurrentLevel)
                .gameObject;


        level.SetActive(true);
    }


    // =========================================================
    // NAVIGATION
    // =========================================================

    public void NextLevel()
    {
        if (LevelCount <= 0)
        {
            return;
        }


        int next =
            CurrentLevel + 1;


        if (next >= LevelCount)
        {
            next = 0;
        }


        LoadLevel(next);
    }


    public void PreviousLevel()
    {
        if (LevelCount <= 0)
        {
            return;
        }


        int previous =
            CurrentLevel - 1;


        if (previous < 0)
        {
            previous =
                LevelCount - 1;
        }


        LoadLevel(previous);
    }


    public void ReloadLevel()
    {
        if (levelsParent == null ||
            CurrentLevel < 0 ||
            CurrentLevel >= LevelCount)
        {
            return;
        }


        GameObject level =
            levelsParent
                .GetChild(CurrentLevel)
                .gameObject;


        level.SetActive(false);
        level.SetActive(true);
    }


    // =========================================================
    // INITIAL CLEANUP
    // =========================================================

    private void DisableAllLevels()
    {
        if (levelsParent == null)
        {
            return;
        }


        for (int i = 0;
             i < levelsParent.childCount;
             i++)
        {
            GameObject level =
                levelsParent
                    .GetChild(i)
                    .gameObject;


            if (level.activeSelf)
            {
                level.SetActive(false);
            }
        }
    }


    // =========================================================
    // EDITOR
    // =========================================================

    private void OnValidate()
    {
        startingLevel =
            Mathf.Max(
                0,
                startingLevel
            );


        if (levelsParent != null &&
            levelsParent.childCount > 0)
        {
            startingLevel =
                Mathf.Min(
                    startingLevel,
                    levelsParent.childCount - 1
                );
        }
    }
}