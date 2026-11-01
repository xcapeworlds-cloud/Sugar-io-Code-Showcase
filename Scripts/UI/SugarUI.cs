using TMPro;
using UnityEngine;

/// <summary>
/// Displays the local player's sugar without formatting text every frame.
/// Rebinds periodically to support late player spawning and respawning.
/// </summary>
[DisallowMultipleComponent]
public sealed class SugarUI : MonoBehaviour
{
    private const float PlayerSearchInterval = 0.5f;

    [Header("UI")]
    [SerializeField] private TMP_Text sugarText;

    private Tornado player;
    private int lastDisplayedSugar = int.MinValue;
    private float nextPlayerSearchTime;

    private void Awake()
    {
        if (sugarText == null)
        {
            sugarText = GetComponent<TMP_Text>();
            if (sugarText == null)
                sugarText = GetComponentInChildren<TMP_Text>(true);
        }
    }

    private void OnEnable()
    {
        lastDisplayedSugar = int.MinValue;
        nextPlayerSearchTime = 0f;
        FindAndBindPlayer();
        Refresh(player != null ? player.Sugar : 0f);
    }

    private void Update()
    {
        // A defeated or destroyed player may be replaced during the match.
        // Only perform discovery at a bounded interval; sugar changes are event-driven.
        if (Time.unscaledTime < nextPlayerSearchTime)
            return;

        nextPlayerSearchTime = Time.unscaledTime + PlayerSearchInterval;

        // Re-check the active player even when the old component still exists.
        // This covers scene transitions that swap player objects without defeating them.
        FindAndBindPlayer();
        if (player == null || player.IsDefeated)
            Refresh(0f);
    }

    private void OnDisable()
    {
        UnbindPlayer();
    }

    private void OnDestroy()
    {
        UnbindPlayer();
    }

    private void FindAndBindPlayer()
    {
        Tornado candidate = null;
        var tornadoes = Tornado.ActiveTornadoes;

        for (int i = 0; i < tornadoes.Count; i++)
        {
            Tornado current = tornadoes[i];
            if (current != null && !current.IsAI && !current.IsDefeated)
            {
                candidate = current;
                break;
            }
        }

        if (candidate == null)
        {
            PlayerController controller = FindFirstObjectByType<PlayerController>();
            if (controller != null)
            {
                Tornado controlled = controller.GetComponent<Tornado>();
                if (controlled != null && !controlled.IsDefeated)
                    candidate = controlled;
            }
        }

        if (candidate == player)
            return;

        UnbindPlayer();
        player = candidate;

        if (player != null)
            player.SugarChanged += Refresh;

        // A newly selected player may have a different sugar value even if
        // no SugarChanged event has fired since this HUD was enabled.
        Refresh(player != null ? player.Sugar : 0f);
    }

    private void UnbindPlayer()
    {
        if (player != null)
            player.SugarChanged -= Refresh;

        player = null;
    }

    private void Refresh(float sugar)
    {
        if (sugarText == null)
            return;

        int displayedSugar = Mathf.FloorToInt(Mathf.Max(0f, sugar));
        if (displayedSugar == lastDisplayedSugar)
            return;

        lastDisplayedSugar = displayedSugar;
        sugarText.text = "Sugar: " + displayedSugar;
    }
}
