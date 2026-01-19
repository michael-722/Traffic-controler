using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;
using Unity.VisualScripting;
using UnityEngine;

/*
 * - leaderboard needs to show player performnces 
 * |- avg car wait times per area
 * |- crashers per area
 * |- total player socre 
 * |- player rankings 
 * - then needs to show total score and ranking against others on leaderboard
 * |- automatically submit score under hostuser name
 */

public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance;

    [SerializeField] private GameObject leaderboardUI;
    [SerializeField] private Transform scrollviewContent;
    [SerializeField] private Transform entryPrefab;
    [SerializeField] private GameObject failedToLoadbj;

    [SerializeField] private string leaderboardID;

    private bool isSigningIn = false;
    private bool isSubmitting = false;

    private string lastSubmittedPlayerId;
    private int lastSubmittedScore;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private async void Start()
    {
        await UnityServices.InitializeAsync();
        await Task.Delay(1000);

        ResetLeaderboardSession();
    }

    private async Task TrySignIn()
    {
        if (AuthenticationService.Instance.IsSignedIn || isSigningIn) return;

        isSigningIn = true;
        try
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        catch (AuthenticationException e)
        {
            Debug.Log("sign in failed:" + e);
        }
        finally
        {
            isSigningIn = false;
        }
    }

    private async void refreshLeaderboard()
    {
        try
        {
            var leaderboardScorePage = await LeaderboardsService.Instance.GetScoresAsync(leaderboardID, new GetScoresOptions() { Limit = 100});
            int resultsCount = leaderboardScorePage.Results.Count;
            int childCount = scrollviewContent.childCount;

            for (int i = 0; i < resultsCount; i++)
            {
                LeaderboardEntry e = leaderboardScorePage.Results[i];
                Transform entry;

                if (i < childCount)//reuse existing entry
                    entry = scrollviewContent.GetChild(i);
                else//instantiate new entry
                    entry = Instantiate(entryPrefab, scrollviewContent);

                string displayName = e.PlayerName.Length > 5 ? e.PlayerName.Substring(0, e.PlayerName.Length - 5) : e.PlayerName;

                entry.GetChild(1).GetComponent<TMP_Text>().text = (e.Rank + 1).ToString();
                entry.GetChild(2).GetComponent<TMP_Text>().text = displayName;
                entry.GetChild(3).GetComponent<TMP_Text>().text = e.Score.ToString();

                var image = entry.GetChild(0).GetComponent<UnityEngine.UI.Image>();
                if (image != null)
                {
                    bool isCurrentSessionEntry = (e.PlayerId == lastSubmittedPlayerId && (int)e.Score == lastSubmittedScore);

                    if (isCurrentSessionEntry)
                        image.color = Color.green;
                    else
                        image.color = Color.white;
                }
                else Debug.LogWarning("leaderboard entry missing img component");
            }

            failedToLoadbj.SetActive(false);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("Failed to load leaderboard: " + ex);
            failedToLoadbj.SetActive(true);
        }
    }

    public async void SubmitScore()
    {
        if (isSubmitting) return;
        isSubmitting = true;

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await TrySignIn();
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                Debug.LogWarning("unable to sign in to leaderboards - cannot submit score");
                failedToLoadbj.SetActive(true);
                return;
            }
        }
        else failedToLoadbj.SetActive(false);

        if (!NetworkManager.Singleton.IsServer)
        {
            refreshLeaderboard();
            return;
        }

        //get initals for all players - leaderabord name
        Dictionary<ulong, string> usernames = PlayerManager.Instance.GetAllPlayerUsernames();
        List<string> initialsList = new List<string>();

        foreach (string username in usernames.Values)
        {
            if (!string.IsNullOrEmpty(username))
            {
                string initials = username.Length >= 4 ? username.Substring(0, 4) : username;
                initialsList.Add(initials);
            }
        }
        string leaderboardName = string.Join("+", initialsList);

        //calculate total score
        Dictionary<ulong, int> scores = PlayerManager.Instance.GetAllPlayerScores();
        int totalScore = 0;

        foreach (int score in scores.Values)
            totalScore += score;

        //Debug.Log($"leaderboard username: {leaderboardName} total score: {totalScore}");

        await AuthenticationService.Instance.UpdatePlayerNameAsync(leaderboardName);
        await LeaderboardsService.Instance.AddPlayerScoreAsync(leaderboardID, totalScore);
        //Debug.Log("score submitted to leaderboard");

        PlayerManager.Instance.NotifyLeaderboardUpdateClientRpc(AuthenticationService.Instance.PlayerId, totalScore);

        isSubmitting = false;
    }

    public void UpdateLocalHighlight(string playerId, int score)
    {
        lastSubmittedPlayerId = playerId;
        lastSubmittedScore = score;
        refreshLeaderboard();
    }

    public async void ResetLeaderboardSession()
    {
        //reset session so can resubmit again
        AuthenticationService.Instance.SignOut();
        AuthenticationService.Instance.ClearSessionToken();
        await TrySignIn();
    }

    public void SetLeaderboardState(bool state)
    {
        leaderboardUI.SetActive(state);
        if (!state)
        {
            for (int i = scrollviewContent.childCount - 1; i >= 0; i--)
                Destroy(scrollviewContent.GetChild(i).gameObject);
        }
    }

    private void OnApplicationQuit()
    {
        if (UnityServices.State == ServicesInitializationState.Initialized && AuthenticationService.Instance.IsSignedIn)
            ResetLeaderboardSession();
    }
}