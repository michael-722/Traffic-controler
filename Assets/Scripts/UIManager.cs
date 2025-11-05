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
    }

    public void HostDissconectBtn()
    {
        OnHostDissconnectPressed?.Invoke();
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
        Debug.Log(NetworkManager.Singleton.DisconnectReason);
        joinedWaiting.text = msg;
    }
}