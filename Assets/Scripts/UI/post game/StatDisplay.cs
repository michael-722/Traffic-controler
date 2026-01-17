using UnityEngine;
using TMPro;
using System.Collections.Generic;
using Unity.Netcode;

public class StatDisplay : MonoBehaviour
{
    [SerializeField] private GameObject statsRootObj;
    [SerializeField] private GameObject waitingRootObj;
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
            if (GameManager.Instance.GetState() == GameState.PostGame) SetStatsState(true);
        }
    }

    private void HandleGameStateChanged(GameState newState)
    {
        SetStatsState(newState == GameState.PostGame);
    }

    public void SetStatsState(bool state)
    {
        statsRootObj.SetActive(state);

        if (state)
        {
            waitingRootObj.SetActive(false);
            CancelInvoke(nameof(UpdateStats));
            Invoke(nameof(UpdateStats), 0.16f);//3x 50ms allow network data to sync
        }
        else CancelInvoke(nameof(UpdateStats));
    }

    private void UpdateStats()
    {
        Dictionary<ulong, string> playernames = PlayerManager.Instance.GetAllPlayerUsernames();
        Dictionary<ulong, int> playerscores = PlayerManager.Instance.GetAllPlayerScores();
        Dictionary<ulong, int> playerCrashes = StatTracker.Instance.GetAllPlayerCrashes();
        Dictionary<ulong, float> playerTotalWait = StatTracker.Instance.GetAllPlayerTotalWaitTime();
        Dictionary<ulong, int> playerWaitEntries = StatTracker.Instance.GetAllPlayerWaitTimeEnties();

        if (playerscores.Count == 0)
        {
            //delayed reattempt
            Invoke(nameof(UpdateStats), 1.0f);
            return;
        }

        List<(ulong id, float score, float totalWait, float avgWait, int crashes)> playerResults = new();

        foreach (var playerId in playerscores.Keys)
        {
            float score = playerscores[playerId];
            float totalWait = playerTotalWait.TryGetValue(playerId, out float t) ? t : 0;
            int waitEntries = playerWaitEntries.TryGetValue(playerId, out int w) ? w : 0;
            int crashes = playerCrashes.TryGetValue(playerId, out int c) ? c : 0;
            float avgWait = waitEntries == 0 ? 0 : totalWait / waitEntries;

            playerResults.Add((playerId, score, totalWait, avgWait, crashes));
        }

        //sort by score desc, avg wait asc
        playerResults.Sort((a, b) =>
        {
            int scoreCompare = b.score.CompareTo(a.score);
            if (scoreCompare != 0) return scoreCompare;
            return a.avgWait.CompareTo(b.avgWait);
        });

        //display
        for (int i = 0; i < playerStatDisplay.Count; i++)
        {
            if (i < playerResults.Count)
            {
                var res = playerResults[i];
                playerStatDisplay[i].gameObject.SetActive(true);

                string name = playernames.ContainsKey(res.id) ? playernames[res.id] : $"Player {res.id}";

                playerStatDisplay[i].GetChild(0).GetComponent<TMP_Text>().text = name;
                playerStatDisplay[i].GetChild(1).GetComponent<TMP_Text>().text = res.score.ToString();
                playerStatDisplay[i].GetChild(2).GetComponent<TMP_Text>().text = res.totalWait.ToString("F1") + "s";
                playerStatDisplay[i].GetChild(3).GetComponent<TMP_Text>().text = res.avgWait.ToString("F1") + "s";
                playerStatDisplay[i].GetChild(4).GetComponent<TMP_Text>().text = res.crashes.ToString();
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
