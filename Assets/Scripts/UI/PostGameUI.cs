using UnityEngine;

public class  PostGameUI : MonoBehaviour
{
    public void Leavepressed()
    {
        GameManager.Instance.RequestReturnToLobby();
    }

    public void PlayAgainPressed()
    {
        PlayAgain();
    }

    private async void PlayAgain()
    {
        await Awaitable.MainThreadAsync();
        GameManager.Instance.RequestRestGame();
    }
}