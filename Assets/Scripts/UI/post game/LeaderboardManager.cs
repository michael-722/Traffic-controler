using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;
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

    [SerializeField] private string leaderboardID;

    private bool isSigningIn = false;
    private float refreshTimer = 0f;
    private float refreshInterval = 1f;

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
        await TrySignIn();
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

    void Update()
    {
        if (leaderboardUI.activeSelf)
        {
            if (!AuthenticationService.Instance.IsSignedIn) return;
            refreshTimer += Time.deltaTime;
            if (refreshTimer >= refreshInterval)
            {
                refreshTimer = 0f;
                refreshLeaderboard();
            }
        }
        else refreshTimer = 0f;
    }

    private async void refreshLeaderboard()
    {
        LeaderboardScoresPage leaderboardScorePage = await LeaderboardsService.Instance.GetScoresAsync(leaderboardID, new GetScoresOptions() { Limit = 100 });

        //playerid as key to stop dupes
        List<string> existingEntries = new List<string>();
        foreach (Transform child in scrollviewContent)
        {
            TMP_Text[] texts = child.GetComponentsInChildren<TMP_Text>();
            if (texts.Length > 2)
            {
                string playerName = texts[1].text;
                existingEntries.Add(playerName);
            }
        }

        foreach (LeaderboardEntry e in leaderboardScorePage.Results)
        {
            //cut #xxxx from usernames
            string displayName = e.PlayerName.Substring(0, e.PlayerName.Length - 5);

            if (existingEntries.Contains(displayName))
                continue;

            Transform entryTransform = Instantiate(entryPrefab, scrollviewContent);
            entryTransform.GetChild(1).GetComponent<TMPro.TextMeshProUGUI>().text = (e.Rank + 1).ToString();
            entryTransform.GetChild(2).GetComponent<TMPro.TextMeshProUGUI>().text = displayName;
            entryTransform.GetChild(3).GetComponent<TMPro.TextMeshProUGUI>().text = e.Score.ToString();
        }
        await Task.Delay(1000);
    }

    public async void SubmitScore()
    {
        //get initals for all players - leaderabord name
        Dictionary<ulong, string> usernames = PlayerManager.Instance.GetAllPlayerUsernames();
        List<string> initialsList = new List<string>();

        foreach (string username in usernames.Values)
        {
            if (!string.IsNullOrEmpty(username))
            {
                string initials = username.Length >= 2 ? username.Substring(0, 2) : username;
                initialsList.Add(initials);
            }
        }
        string leaderboardName = string.Join("+", initialsList);

        //calculate total score
        Dictionary<ulong, int> scores = PlayerManager.Instance.GetAllPlayerScores();
        int totalScore = 0;

        foreach (int score in scores.Values)
            totalScore += score;

        //need to get host user name and total score here <==============================
        await AuthenticationService.Instance.UpdatePlayerNameAsync(leaderboardName);
        await LeaderboardsService.Instance.AddPlayerScoreAsync(leaderboardID, totalScore);
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
    }
}