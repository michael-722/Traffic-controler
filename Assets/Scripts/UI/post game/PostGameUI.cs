using UnityEngine;

public class  PostGameUI : MonoBehaviour
{
    public void Leavepressed()
    {
        Relay.Instance.Dissconect();
        GameManager.Instance.RequestReturnToLobby();
    }

    public void PlayAgainPressed()
    {
        PlayAgain();
        LeaderboardManager.Instance.ResetLeaderboardSession();
    }

    private async void PlayAgain()
    {
        await Awaitable.MainThreadAsync();

        StatTracker.Instance.ResetAllPlayerStats();
        GameManager.Instance.RequestRestGame();
    }
}