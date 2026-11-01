using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public enum TornadoMatchState
{
    Waiting,
    Countdown,
    Playing,
    Finished
}


[Serializable]
public sealed class TornadoMatchStats
{
    public float SugarCollected { get; internal set; }
    public int CandyCollected { get; internal set; }
    public int EnvironmentPiecesConsumed { get; internal set; }
    public int TornadoEliminations { get; internal set; }
    public float SurvivalTime { get; internal set; }
    public float FinalSize { get; internal set; }
    public float Score { get; internal set; }
    public int Placement { get; internal set; }
}


[DisallowMultipleComponent]
public sealed class TornadoMatchManager : MonoBehaviour
{
    private sealed class Competitor
    {
        public Tornado Tornado;

        public readonly TornadoMatchStats Stats =
            new TornadoMatchStats();

        public bool Alive = true;
    }


    public static TornadoMatchManager Instance
    {
        get;
        private set;
    }


    // ==================================================
    // MATCH BALANCE
    // ==================================================

    [Header("Match Balance")]

    [SerializeField]
    private MatchBalance balance =
        new MatchBalance();


    public MatchBalance Balance
    {
        get
        {
            if (balance == null)
            {
                balance =
                    new MatchBalance();

                balance.Validate();
            }

            return balance;
        }
    }


    // ==================================================
    // MATCH SETUP
    // ==================================================

    [Header("Match Setup")]

    [SerializeField, Min(0f)]
    private float countdownDuration = 3f;


    [SerializeField, Range(2, 12)]
    [Tooltip(
        "Total tornadoes in the match, INCLUDING the player."
    )]
    private int competitorCount = 6;


    [SerializeField, Min(0f)]
    [Tooltip(
        "Bonus score for eating another tornado."
    )]
    private float eliminationScore = 25f;


    // ==================================================
    // MATCH UI
    // ==================================================

    [Header("Match UI")]

    [SerializeField]
    [Tooltip(
        "TMP text showing how many tornadoes are still alive."
    )]
    private TMP_Text playersLeftText;


    [SerializeField]
    [Tooltip(
        "TMP text used for countdown and match status."
    )]
    private TMP_Text gameStatusText;

    [SerializeField, Min(0f)]
    [Tooltip("How long the GO! message remains visible after gameplay starts.")]
    private float goMessageDuration = 0.8f;


    [SerializeField]
    [Tooltip(
        "Restart button. Hidden during gameplay and shown after the match."
    )]
    private Button restartButton;


    // ==================================================
    // PLAYER DEATH / RESTART
    // ==================================================

    [Header("Player Death")]

    [SerializeField, Min(0f)]
    [Tooltip(
        "Small delay before restart becomes available."
    )]
    private float restartInputDelay = 0.35f;


    // ==================================================
    // REFERENCES
    // ==================================================

    [Header("References")]

    [SerializeField]
    private TornadoAISpawner aiSpawner;


    [Header("Sugar Popup")]

    [SerializeField]
    [Tooltip("World-space 3D TextMeshPro popup pool used for player sugar gains.")]
    private SugarPopup sugarPopup;

    [SerializeField, Min(0f)]
    [Tooltip("How far above the player's position the sugar popup appears.")]
    private float sugarPopupHeight = 2f;


    // ==================================================
    // RUNTIME
    // ==================================================

    private readonly List<Competitor> competitors =
        new List<Competitor>();


    private readonly Dictionary<Tornado, Competitor> lookup =
        new Dictionary<Tornado, Competitor>();


    private Tornado player;
    private Tornado matchWinner;


    private float countdownRemaining;
    private float matchStartedAt;
    private float nextRankUpdate;
    private float clearGoMessageAt = -1f;


    private int lastCountdownValue = -1;
    private int playerRank;


    private bool playerDead;
    private float restartAllowedAt;
    private bool restarting;


    // ==================================================
    // SIMPLE GAME RULES
    // ==================================================

    public bool RespawnCandy =>
        false;


    public bool RespawnDestructibles =>
        false;


    // ==================================================
    // PUBLIC MATCH INFO
    // ==================================================

    public TornadoMatchState MatchState
    {
        get;
        private set;
    } = TornadoMatchState.Waiting;


    public float PlayerScore =>
        GetScore(player);


    public int PlayerRank =>
        playerRank;


    public int AliveCount =>
        CountAlive();


    public int TotalCompetitors =>
        competitors.Count;


    public Tornado MatchWinner =>
        matchWinner;


    public int FinalPlacement =>
        GetStats(player)?.Placement ?? 0;


    public TornadoMatchStats MatchStats =>
        GetStats(player);


    public bool IsPlaying =>
        MatchState ==
        TornadoMatchState.Playing;


    public int CountdownRemaining =>
        Mathf.Max(
            0,
            Mathf.CeilToInt(
                countdownRemaining
            )
        );


    public bool PlayerIsDead =>
        playerDead;


    public bool IsPlayerWinner =>
        MatchState == TornadoMatchState.Finished &&
        matchWinner == player;


    // ==================================================
    // EVENTS
    // ==================================================

    public event Action MatchStarted;
    public event Action<int> CountdownChanged;
    public event Action<Tornado, float> ScoreChanged;
    public event Action<int> RankChanged;
    public event Action<int> PlayersLeftChanged;


    public event Action<Tornado, Tornado>
        CompetitorEliminated;


    public event Action MatchEnded;
    public event Action PlayerEliminated;
    public event Action GameWon;
    public event Action GameLost;


    // ==================================================
    // UNITY
    // ==================================================

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }


        Instance =
            this;


        if (balance == null)
        {
            balance =
                new MatchBalance();
        }


        balance.Validate();


        SetupUI();

        if (sugarPopup != null)
        {
            sugarPopup.gameObject.SetActive(false);
        }
    }


    private IEnumerator Start()
    {
        yield return null;

        SetupMatch();
    }


    private void Update()
    {
        // ----------------------------------------------
        // RESTART BUTTON DELAY
        // ----------------------------------------------

        if (restartButton != null &&
            restartButton.gameObject.activeSelf &&
            !restartButton.interactable &&
            Time.unscaledTime >= restartAllowedAt)
        {
            restartButton.interactable =
                true;
        }


        // ----------------------------------------------
        // COUNTDOWN
        // ----------------------------------------------

        if (MatchState ==
            TornadoMatchState.Countdown)
        {
            UpdateCountdown();
            return;
        }


        if (MatchState !=
            TornadoMatchState.Playing)
        {
            return;
        }

        if (clearGoMessageAt >= 0f &&
            Time.unscaledTime >= clearGoMessageAt)
        {
            clearGoMessageAt = -1f;

            SetGameStatus(
                string.Empty
            );
        }


        UpdateSurvivalStats();


        if (Time.time >=
            nextRankUpdate)
        {
            nextRankUpdate =
                Time.time +
                0.25f;

            RefreshPlayerRank();
        }
    }


    private void OnDestroy()
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(
                RestartFromUI
            );
        }


        if (Instance == this)
        {
            Instance =
                null;
        }
    }


    // ==================================================
    // UI
    // ==================================================

    private void SetupUI()
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(
                RestartFromUI
            );

            restartButton.onClick.AddListener(
                RestartFromUI
            );

            restartButton.interactable =
                false;

            restartButton.gameObject.SetActive(
                false
            );
        }


        if (playersLeftText != null)
        {
            playersLeftText.text =
                "PLAYERS LEFT: 0";
        }


        if (gameStatusText != null)
        {
            gameStatusText.text =
                "GET READY";
        }
    }


    private void UpdatePlayersLeftUI()
    {
        int alive =
            CountAlive();

        if (playersLeftText != null)
        {
            playersLeftText.text =
                "PLAYERS LEFT: " +
                alive;
        }

        PlayersLeftChanged?.Invoke(
            alive
        );
    }


    private void SetGameStatus(
        string message)
    {
        if (gameStatusText == null)
        {
            return;
        }


        gameStatusText.text =
            message;
    }


    private void ShowRestartButton()
    {
        if (restartButton == null)
        {
            return;
        }


        restartButton.gameObject.SetActive(
            true
        );


        restartButton.interactable =
            Time.unscaledTime >=
            restartAllowedAt;
    }


    private void HideRestartButton()
    {
        if (restartButton == null)
        {
            return;
        }


        restartButton.interactable =
            false;


        restartButton.gameObject.SetActive(
            false
        );
    }


    private void RestartFromUI()
    {
        if (restarting)
        {
            return;
        }


        if (Time.unscaledTime <
            restartAllowedAt)
        {
            return;
        }


        restarting =
            true;


        if (restartButton != null)
        {
            restartButton.interactable =
                false;
        }


        RestartMatch();
    }


    // ==================================================
    // SETUP
    // ==================================================

    public void SetupMatch()
    {
        MatchState =
            TornadoMatchState.Waiting;


        competitors.Clear();
        lookup.Clear();


        matchWinner =
            null;


        playerRank =
            0;


        playerDead =
            false;


        restarting =
            false;


        clearGoMessageAt =
            -1f;


        restartAllowedAt =
            0f;


        HideRestartButton();


        SetGameStatus(
            "GET READY"
        );


        player =
            FindPlayer();


        if (player == null)
        {
            Debug.LogError(
                "TornadoMatchManager could not find the human player.",
                this
            );

            return;
        }


        if (aiSpawner == null)
        {
            aiSpawner =
                FindFirstObjectByType<TornadoAISpawner>();
        }


        RemoveUnmanagedAI();


        RegisterCompetitor(
            player
        );


        int total =
            Mathf.Max(
                2,
                competitorCount
            );


        if (aiSpawner == null &&
            total > 1)
        {
            Debug.LogError(
                "TornadoMatchManager requires a TornadoAISpawner.",
                this
            );

            return;
        }


        if (aiSpawner != null)
        {
            aiSpawner.SpawnForMatch(
                total - 1,
                ai =>
                {
                    RegisterCompetitor(
                        ai
                    );
                }
            );
        }


        UpdatePlayersLeftUI();


        countdownRemaining =
            countdownDuration;


        lastCountdownValue =
            -1;


        MatchState =
            TornadoMatchState.Countdown;


        BroadcastCountdown();
    }


    // ==================================================
    // RESTART
    // ==================================================

    public void RestartMatch()
    {
        if (restarting == false)
        {
            restarting =
                true;
        }


        SceneManager.LoadScene(
            SceneManager.GetActiveScene()
                .buildIndex
        );
    }


    public void RestartGame()
    {
        RestartMatch();
    }


    // ==================================================
    // COMPETITORS
    // ==================================================

    public void GetCompetitors(
        List<Tornado> result)
    {
        if (result == null)
        {
            return;
        }


        result.Clear();


        for (int i = 0;
             i < competitors.Count;
             i++)
        {
            Tornado tornado =
                competitors[i].Tornado;


            if (tornado != null)
            {
                result.Add(
                    tornado
                );
            }
        }
    }


    // ==================================================
    // REPORT CANDY
    // ==================================================

    public void ReportCandyCollected(
        Tornado collector,
        float sugar)
    {
        ReportCandyCollected(
            collector,
            sugar,
            Color.white
        );
    }

    public void ReportCandyCollected(
        Tornado collector,
        float sugar,
        Color highlightColor)
    {
        Competitor data =
            Find(
                collector
            );


        if (data == null ||
            MatchState !=
            TornadoMatchState.Playing)
        {
            return;
        }


        float amount =
            Mathf.Max(
                0f,
                sugar
            );


        data.Stats.CandyCollected++;


        data.Stats.SugarCollected +=
            amount;


        AddScore(
            collector,
            amount
        );


        // ----------------------------------------------
        // PLAYER SUGAR POPUP
        // ----------------------------------------------

        if (collector == player &&
            sugarPopup != null &&
            amount > 0f)
        {
            Vector3 popupPosition =
                collector.transform.position +
                Vector3.up *
                sugarPopupHeight;

            sugarPopup.Show(
                amount,
                popupPosition,
                collector.VisualScale,
                highlightColor
            );
        }
    }


    // ==================================================
    // REPORT ENVIRONMENT
    // ==================================================

    public void ReportEnvironmentConsumed(
        Tornado collector,
        float sugar)
    {
        ReportEnvironmentConsumed(
            collector,
            sugar,
            Color.white
        );
    }

    public void ReportEnvironmentConsumed(
        Tornado collector,
        float sugar,
        Color highlightColor)
    {
        Competitor data =
            Find(
                collector
            );


        if (data == null ||
            MatchState !=
            TornadoMatchState.Playing)
        {
            return;
        }


        float amount =
            Mathf.Max(
                0f,
                sugar
            );


        data.Stats.EnvironmentPiecesConsumed++;


        data.Stats.SugarCollected +=
            amount;


        AddScore(
            collector,
            amount
        );

        if (collector == player &&
            sugarPopup != null &&
            amount > 0f)
        {
            Vector3 popupPosition =
                collector.transform.position +
                Vector3.up * sugarPopupHeight;

            sugarPopup.Show(
                amount,
                popupPosition,
                collector.VisualScale,
                highlightColor
            );
        }
    }


    // ==================================================
    // REPORT ELIMINATION
    // ==================================================

    public void ReportElimination(
        Tornado killer,
        Tornado victim,
        float transferredSugar)
    {
        if (MatchState !=
            TornadoMatchState.Playing)
        {
            return;
        }


        Competitor victimData =
            Find(
                victim
            );


        if (victimData == null ||
            !victimData.Alive)
        {
            return;
        }


        victimData.Alive =
            false;


        victimData.Stats.FinalSize =
            victim != null
                ? victim.VisualScale
                : 0f;


        victimData.Stats.Placement =
            CountAlive() +
            1;


        // ----------------------------------------------
        // PLAYER DIED
        // ----------------------------------------------

        if (victim ==
            player)
        {
            playerDead =
                true;


            restartAllowedAt =
                Time.unscaledTime +
                restartInputDelay;


            SetGameStatus(
                "YOU LOST!"
            );


            ShowRestartButton();


            PlayerEliminated?.Invoke();
        }


        // ----------------------------------------------
        // KILLER
        // ----------------------------------------------

        Competitor killerData =
            Find(
                killer
            );


        if (killerData != null)
        {
            killerData.Stats.TornadoEliminations++;


            float transferred =
                Mathf.Max(
                    0f,
                    transferredSugar
                );


            killerData.Stats.SugarCollected +=
                transferred;


            AddScore(
                killer,
                eliminationScore +
                transferred
            );
        }


        UpdatePlayersLeftUI();


        CompetitorEliminated?.Invoke(
            killer,
            victim
        );


        // ==============================================
        // AUDIO - PLAYER ATE ANOTHER TORNADO
        // ==============================================

        if (killer != null &&
            !killer.IsAI)
        {
            GameAudioManager.Instance
                ?.PlayElimination();
        }


        RefreshPlayerRank();


        if (playerDead)
        {
            matchWinner =
                null;

            FinishMatch();
            return;
        }

    }


    // ==================================================
    // COUNTDOWN
    // ==================================================

    private void UpdateCountdown()
    {
        countdownRemaining -=
            Time.deltaTime;


        BroadcastCountdown();


        if (countdownRemaining >
            0f)
        {
            return;
        }


        countdownRemaining =
            0f;


        MatchState =
            TornadoMatchState.Playing;


        matchStartedAt =
            Time.time;


        nextRankUpdate =
            Time.time;


        SetGameStatus(
            "GO!"
        );


        clearGoMessageAt =
            Time.unscaledTime +
            goMessageDuration;


        // ==============================================
        // AUDIO - RESET GROWTH MILESTONES
        // ==============================================

        if (player != null)
        {
            GameAudioManager.Instance
                ?.ResetGrowthMilestones(
                    player.VisualScale
                );
        }


        MatchStarted?.Invoke();
    }


    private void BroadcastCountdown()
    {
        int value =
            Mathf.Max(
                0,
                Mathf.CeilToInt(
                    countdownRemaining
                )
            );


        if (value ==
            lastCountdownValue)
        {
            return;
        }


        lastCountdownValue =
            value;


        if (value > 0)
        {
            SetGameStatus(
                value.ToString()
            );
        }


        CountdownChanged?.Invoke(
            value
        );
    }


    // ==================================================
    // REGISTER
    // ==================================================

    private void RegisterCompetitor(
        Tornado tornado)
    {
        if (tornado == null ||
            lookup.ContainsKey(
                tornado
            ))
        {
            return;
        }


        Competitor data =
            new Competitor
            {
                Tornado =
                    tornado
            };


        competitors.Add(
            data
        );


        lookup.Add(
            tornado,
            data
        );
    }


    // ==================================================
    // AI CLEANUP
    // ==================================================

    private void RemoveUnmanagedAI()
    {
        var active =
            Tornado.ActiveTornadoes;


        for (int i =
                 active.Count - 1;
             i >= 0;
             i--)
        {
            Tornado tornado =
                active[i];


            if (tornado == null ||
                !tornado.IsAI)
            {
                continue;
            }


            tornado.gameObject.SetActive(
                false
            );


            Destroy(
                tornado.gameObject
            );
        }


        if (aiSpawner != null)
        {
            aiSpawner.ClearSpawnedAI();
        }
    }


    // ==================================================
    // FIND PLAYER
    // ==================================================

    private Tornado FindPlayer()
    {
        var active =
            Tornado.ActiveTornadoes;


        for (int i = 0;
             i < active.Count;
             i++)
        {
            Tornado candidate =
                active[i];


            if (candidate != null &&
                !candidate.IsAI)
            {
                return candidate;
            }
        }


        return null;
    }


    // ==================================================
    // SCORE
    // ==================================================

    private void AddScore(
        Tornado tornado,
        float amount)
    {
        Competitor data =
            Find(
                tornado
            );


        if (data == null ||
            amount <= 0f)
        {
            return;
        }


        data.Stats.Score +=
            amount;


        ScoreChanged?.Invoke(
            tornado,
            data.Stats.Score
        );
    }


    // ==================================================
    // SURVIVAL
    // ==================================================

    private void UpdateSurvivalStats()
    {
        float survival =
            Time.time -
            matchStartedAt;


        for (int i = 0;
             i < competitors.Count;
             i++)
        {
            Competitor data =
                competitors[i];


            if (data.Alive)
            {
                data.Stats.SurvivalTime =
                    survival;
            }
        }
    }


    // ==================================================
    // FINISH
    // ==================================================

    private void FinishMatch()
    {
        if (MatchState ==
            TornadoMatchState.Finished)
        {
            return;
        }


        MatchState =
            TornadoMatchState.Finished;


        clearGoMessageAt =
            -1f;


        AssignFinalPlacements();


        for (int i = 0;
             i < competitors.Count;
             i++)
        {
            Competitor data =
                competitors[i];


            if (data.Tornado == null)
            {
                continue;
            }


            data.Stats.FinalSize =
                data.Tornado.VisualScale;


            data.Tornado.SetMoveIntent(
                Vector3.zero,
                0f
            );
        }


        UpdatePlayersLeftUI();


        RefreshPlayerRank();


        restartAllowedAt =
            Time.unscaledTime +
            restartInputDelay;


        if (matchWinner ==
            player)
        {
            SetGameStatus(
                "YOU WIN!"
            );


            GameWon?.Invoke();


            // ==========================================
            // AUDIO - VICTORY
            // ==========================================

            GameAudioManager.Instance
                ?.PlayVictory();
        }
        else
        {
            SetGameStatus(
                "YOU LOST!"
            );


            GameLost?.Invoke();


            // ==========================================
            // AUDIO - DEFEAT
            // ==========================================

            GameAudioManager.Instance
                ?.PlayDefeat();
        }


        ShowRestartButton();


        MatchEnded?.Invoke();
    }


    // ==================================================
    // PLACEMENT
    // ==================================================

    private void AssignFinalPlacements()
    {
        if (matchWinner != null)
        {
            Competitor winner =
                Find(
                    matchWinner
                );


            if (winner != null)
            {
                winner.Stats.Placement =
                    1;
            }
        }


        for (int i = 0;
             i < competitors.Count;
             i++)
        {
            Competitor data =
                competitors[i];


            if (data.Stats.Placement >
                0)
            {
                continue;
            }


            if (data.Tornado ==
                matchWinner)
            {
                data.Stats.Placement =
                    1;

                continue;
            }


            int placement =
                2;


            float score =
                data.Stats.Score;


            for (int j = 0;
                 j < competitors.Count;
                 j++)
            {
                if (i == j)
                {
                    continue;
                }


                Competitor other =
                    competitors[j];


                if (other.Tornado ==
                    matchWinner)
                {
                    continue;
                }


                if (other.Stats.Score >
                    score)
                {
                    placement++;
                }
            }


            data.Stats.Placement =
                Mathf.Clamp(
                    placement,
                    2,
                    competitors.Count
                );
        }
    }


    // ==================================================
    // PLAYER RANK
    // ==================================================

    private void RefreshPlayerRank()
    {
        if (player == null)
        {
            return;
        }


        Competitor playerData =
            Find(
                player
            );


        if (playerData == null)
        {
            return;
        }


        int rank;


        if (!playerData.Alive &&
            playerData.Stats.Placement > 0)
        {
            rank =
                playerData.Stats.Placement;
        }
        else
        {
            float score =
                playerData.Stats.Score;


            rank =
                1;


            for (int i = 0;
                 i < competitors.Count;
                 i++)
            {
                Competitor other =
                    competitors[i];


                if (other.Tornado ==
                    player)
                {
                    continue;
                }


                if (other.Stats.Score >
                    score)
                {
                    rank++;
                }
            }
        }


        if (rank ==
            playerRank)
        {
            return;
        }


        playerRank =
            rank;


        RankChanged?.Invoke(
            rank
        );
    }


    // ==================================================
    // ALIVE
    // ==================================================

    private int CountAlive()
    {
        int count =
            0;


        for (int i = 0;
             i < competitors.Count;
             i++)
        {
            Competitor data =
                competitors[i];


            if (data.Alive &&
                data.Tornado != null)
            {
                count++;
            }
        }


        return count;
    }


    private Tornado FindFirstAlive()
    {
        for (int i = 0;
             i < competitors.Count;
             i++)
        {
            Competitor data =
                competitors[i];


            if (data.Alive &&
                data.Tornado != null)
            {
                return data.Tornado;
            }
        }


        return null;
    }


    // ==================================================
    // LOOKUP
    // ==================================================

    private Competitor Find(
        Tornado tornado)
    {
        if (tornado == null)
        {
            return null;
        }


        return
            lookup.TryGetValue(
                tornado,
                out Competitor data
            )
                ? data
                : null;
    }


    public float GetScore(
        Tornado tornado)
    {
        Competitor data =
            Find(
                tornado
            );


        return
            data != null
                ? data.Stats.Score
                : 0f;
    }


    public TornadoMatchStats GetStats(
        Tornado tornado)
    {
        return
            Find(tornado)?.Stats;
    }


    // ==================================================
    // VALIDATION
    // ==================================================

    private void OnValidate()
    {
        if (balance == null)
        {
            balance =
                new MatchBalance();
        }


        balance.Validate();


        countdownDuration =
            Mathf.Max(
                0f,
                countdownDuration
            );


        competitorCount =
            Mathf.Clamp(
                competitorCount,
                2,
                12
            );


        eliminationScore =
            Mathf.Max(
                0f,
                eliminationScore
            );


        restartInputDelay =
            Mathf.Max(
                0f,
                restartInputDelay
            );


        goMessageDuration =
            Mathf.Max(
                0f,
                goMessageDuration
            );


        sugarPopupHeight =
            Mathf.Max(
                0f,
                sugarPopupHeight
            );
    }
}