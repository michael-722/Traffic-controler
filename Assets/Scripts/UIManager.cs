using UnityEngine;
using System;
using TMPro;
using Unity.Netcode;

public class UIManager : MonoBehaviour
{
    public event Action<string> OnStartClientPressed;
    public event Action OnStartHostPressed;
    public event Action OnDissconnectPressed;
    public event Action OnHostDissconnectPressed;
    //public event Action //get players connected from relay================================================

    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private TMP_Text joinCodeDisplay;
    [SerializeField] private TMP_Text playersConnectedCount;
    [SerializeField] private TMP_Text joinedWaiting;

    public bool hostUIActive = false;

    public void StartHostBtn()
    {
        OnStartHostPressed?.Invoke();
    }

    public void StartGamePressed()
    {
    
    }

    public void StartClientBtn()
    {
        OnStartClientPressed?.Invoke(joinCodeInput.text.Trim());
        //need to wait till connection is set bc rn client is server so runs locally
    }

    public void DissconnectBtn()
    {
        OnDissconnectPressed?.Invoke();

        //reset that may have been changed
        joinCodeDisplay.text = "Generating code...";
        playersConnectedCount.text = "Connected players: 0";
        joinedWaiting.text = "Waiting for host to start game...";

        joinedWaiting.color = Color.white;
    }

    public void DisplayJoinCode(string code)
    {
        joinCodeDisplay.text = "Join code:" + code;
    }

    public void UpdateConnectedPlayerCount(int num)
    {
        playersConnectedCount.text = "Players connected: " + (num - 1);
    }

    public void UpdateJoinedWaitingTextClientRpc(string msg)
    {
        joinedWaiting.text = msg;
        joinedWaiting.color = Color.red;
    }
}