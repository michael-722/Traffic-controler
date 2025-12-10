using System;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public event Action<string> OnStartClientPressed;
    public event Action OnStartHostPressed;
    public event Action OnDissconnectPressed;
    public event Action OnHostDissconnectPressed;
    public event Action OnStartLANPressed;
    public event Action<string> OnJoinLANPressed;
    public event Action OnStartGamePressed;

    [SerializeField] private GameObject hostUI;
    [SerializeField] private GameObject inGameUI;
    [SerializeField] private GameObject postGameUI;

    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private TMP_Text joinCodeDisplay;
    [SerializeField] private TMP_Text playersConnectedCount;
    [SerializeField] private TMP_Text playersConnectedCountLAN;
    [SerializeField] private TMP_Text joinedWaiting;
    [SerializeField] private TMP_Text ipDisplay;
    [SerializeField] private TMP_InputField ipInput;
    public bool hostUIActive = false;

    public void StartHostBtn()
    {
        OnStartHostPressed?.Invoke();
    }

    public void StartLANBtn()
    {
        OnStartLANPressed?.Invoke();
    }

    public void JoinLANBtn()
    {
        ResetUIText();
        OnJoinLANPressed?.Invoke(ipInput.text);
    }

    public void StartGameBtn()
    {
        OnStartGamePressed?.Invoke();
    }

    public void StartClientBtn()
    {
        ResetUIText();
        OnStartClientPressed?.Invoke(joinCodeInput.text.Trim());
        //need to wait till connection is set bc rn client is server so runs locally
    }

    public void DissconnectBtn()
    {
        OnDissconnectPressed?.Invoke();
        ResetUIText();
    }

    private void ResetUIText()
    {
        //reset that may have been changed
        joinCodeDisplay.text = "Generating code...";
        playersConnectedCount.text = "Connected players: 0";
        playersConnectedCountLAN.text = "Connected players: 0";
        joinedWaiting.text = "Waiting for host to start game...";

        joinedWaiting.color = Color.white;
    }

    public void DisplayJoinCode(string code)
    {
        joinCodeDisplay.text = "Join code:" + code;
    }

    public void DisplayIpAdress(string ip)
    {
        ipDisplay.text = "Host IP: " + ip;
    }

    public void UpdateConnectedPlayerCount(int num)
    {
        playersConnectedCount.text = "Players connected: " + (num - 1);
        playersConnectedCountLAN.text = "Players connected: " + (num - 1);
    }

    public void UpdateJoinedWaitingText(string msg)
    {
        joinedWaiting.text = msg;
        joinedWaiting.color = Color.red;
    }

    public void ToggleUIBasedOnGameState(GameState state)
    {
        hostUI.SetActive(false);
        inGameUI.SetActive(false);
        postGameUI.SetActive(false);

        switch (state)
        {
            case GameState.Lobby:
                hostUIActive = true;
                break;
            case GameState.Gameplay:
                hostUIActive = false;
                break;
            case GameState.PostGame:
                hostUIActive = true;
                break;
        }
    }
}