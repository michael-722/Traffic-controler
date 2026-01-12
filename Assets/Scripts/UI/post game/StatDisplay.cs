using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class StatDisplay : MonoBehaviour
{
    [SerializeField] private GameObject statsRootObj;
    [SerializeField] private List<Transform> playerStatDisplay = new List<Transform>();

    private bool isListenerAdded = false;

    private void Start()
    {
        TryAddGameStateListener();
    }

    private void OnEnable()
    {
        TryAddGameStateListener();
    }

    private void OnDisable()
    {
        if (isListenerAdded && GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            isListenerAdded = false;
        }
    }

    private void TryAddGameStateListener()
    {
        if (!isListenerAdded && GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            isListenerAdded = true;
        }
    }

    private void HandleGameStateChanged(GameState newState)
    {
        if (newState == GameState.PostGame) SetStatsState(true);
        else SetStatsState(false);
    }

    public void SetStatsState(bool state)
    {
        statsRootObj.SetActive(state);

        if (state)
        {
            CancelInvoke(nameof(UpdateStats));
            Invoke(nameof(UpdateStats), 0.5f);
        }
        else CancelInvoke(nameof(UpdateStats));
    }

    private void UpdateStats()
    {
        Dictionary<ulong, int> playerCrashes = StatTracker.Instance.GetAllPlayerCrashes();
        Dictionary<ulong, float> playerTotalWaitTime = StatTracker.Instance.GetAllPlayerTotalWaitTime();
        Dictionary<ulong, int> playerWaitTimeEntries = StatTracker.Instance.GetAllPlayerWaitTimeEnties();
        Dictionary<ulong, string> playernames = PlayerManager.Instance.GetAllPlayerUsernames();
        Dictionary<ulong, int> playerscores = PlayerManager.Instance.GetAllPlayerScores();

        List<(ulong id, float score, float totalWait, float avgWait, int crashes)> playerResults = new();

        foreach (var playerId in playerscores.Keys)
        {
            float score = playerscores.TryGetValue(playerId, out int s) ? s : 0;
            float totalWait = playerTotalWaitTime.TryGetValue(playerId, out float t) ? t : 0;
            int waitEntries = playerWaitTimeEntries.TryGetValue(playerId, out int w) ? w : 0;
            int crashes = playerCrashes.TryGetValue(playerId, out int c) ? c : 0;
            float avgWait = waitEntries == 0 ? 0 : totalWait / waitEntries;

            playerResults.Add((playerId, score, totalWait, avgWait, crashes));
        }

        //sort by - score desc, avg wait asc, crashes asc
        playerResults.Sort((a, b) =>
        {
            int scoreCompare = b.score.CompareTo(a.score);
            if (scoreCompare != 0) return scoreCompare;

            int avgWaitCompare = a.avgWait.CompareTo(b.avgWait);
            if (avgWaitCompare != 0) return avgWaitCompare;

            return a.crashes.CompareTo(b.crashes);
        });

        for (int i = 0; i < playerStatDisplay.Count; i++)
        {
            if (i < playerResults.Count)
            {
                var result = playerResults[i];
                playerStatDisplay[i].gameObject.SetActive(true);

                string username = playernames.ContainsKey(result.id) ? playernames[result.id] : $"Player {result.id}";

                //order dependant on hirarchy
                playerStatDisplay[i].GetChild(0).GetComponent<TMP_Text>().text = username;
                playerStatDisplay[i].GetChild(1).GetComponent<TMP_Text>().text = result.score.ToString();
                playerStatDisplay[i].GetChild(2).GetComponent<TMP_Text>().text = result.totalWait.ToString("F1") + "s";
                playerStatDisplay[i].GetChild(3).GetComponent<TMP_Text>().text = result.avgWait.ToString("F1") + "s";
                playerStatDisplay[i].GetChild(4).GetComponent<TMP_Text>().text = result.crashes.ToString();
            }
            else
            {
                playerStatDisplay[i].gameObject.SetActive(false);
            }
        }
    }

    public void ShowLeaderboard()
    {
        LeaderboardManager.Instance.SetLeaderboardState(true);
    }
}
