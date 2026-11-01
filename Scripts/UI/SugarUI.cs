using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SugarUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private TMP_Text sugarText;

    private Tornado player;

    private int lastDisplayedSugar =
        int.MinValue;


    private void Awake()
    {
        FindText();
    }


    private void Start()
    {
        FindPlayer();

        RefreshImmediate();
    }


    private void Update()
    {
        if (player == null ||
            player.IsDefeated)
        {
            FindPlayer();
        }


        Refresh();
    }


    private void FindText()
    {
        if (sugarText != null)
        {
            return;
        }


        sugarText =
            GetComponent<TMP_Text>();


        if (sugarText == null)
        {
            sugarText =
                GetComponentInChildren<TMP_Text>(
                    true
                );
        }
    }


    private void FindPlayer()
    {
        player = null;


        var tornadoes =
            Tornado.ActiveTornadoes;


        for (int i = 0;
             i < tornadoes.Count;
             i++)
        {
            Tornado candidate =
                tornadoes[i];


            if (candidate == null ||
                candidate.IsDefeated ||
                candidate.IsAI)
            {
                continue;
            }


            player =
                candidate;

            break;
        }


        if (player != null)
        {
            return;
        }


        PlayerController controller =
            FindFirstObjectByType<PlayerController>();


        if (controller != null)
        {
            player =
                controller.GetComponent<Tornado>();
        }
    }


    private void Refresh()
    {
        if (sugarText == null)
        {
            FindText();


            if (sugarText == null)
            {
                return;
            }
        }


        int sugar =
            player != null &&
            !player.IsDefeated
                ? Mathf.FloorToInt(
                    Mathf.Max(
                        0f,
                        player.Sugar
                    )
                )
                : 0;


        if (sugar ==
            lastDisplayedSugar)
        {
            return;
        }


        lastDisplayedSugar =
            sugar;


        sugarText.text =
            "Sugar: " +
            sugar;
    }


    private void RefreshImmediate()
    {
        lastDisplayedSugar =
            int.MinValue;

        Refresh();
    }
}