using UnityEngine;
using System;
using TMPro;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    public static event Action OnStartHostPressed;
    public static event Action OnStartLANPressed;
    public static event Action<string> OnJoinLANPressed;
    public static event Action OnStartGamePressed;
    public static event Action<string> OnStartClientPressed;
    public static event Action OnDissconnectPressed;

    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private TMP_InputField ipInput;
    [SerializeField] private TMP_Text joinCodeDisplay;
    [SerializeField] private TMP_Text ipDisplay;
    [SerializeField] private TMP_Text playersConnectedCount;
    [SerializeField] private TMP_Text playersConnectedCountLAN;
    [SerializeField] private TMP_Text joinedWaiting;

    [SerializeField] private Button hostStartGameBtn;
    [SerializeField] private Button hostBackBtn;

    private void Start()
    {
        OnStartHostPressed += Relay.Instance.CreateHost;
        OnStartClientPressed += Relay.Instance.JoinGame;
        OnDissconnectPressed += Relay.Instance.Dissconect;
        OnStartLANPressed += Relay.Instance.CreateLAN;
        OnJoinLANPressed += Relay.Instance.JoinLAN;
        OnStartGamePressed += () => GameManager.Instance.RequestStartGame();
    }

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

        hostStartGameBtn.interactable = false;
        hostBackBtn.interactable = false;

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
        joinCodeDisplay.text = code;

        hostBackBtn.interactable = true;
    }

    public void DisplayIpAdress(string ip)
    {
        ipDisplay.text = ip;
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

    public void ExitBtn()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    public void CopyCodeBtn()
    {
        GUIUtility.systemCopyBuffer = joinCodeDisplay.text;
    }

    public void CopyIpBtn()
    {
        GUIUtility.systemCopyBuffer = ipDisplay.text;
    }

    public void SetHostStart(bool state)
    {
        hostStartGameBtn.interactable = state;
    }

    public void StartTutorial()
    {
        GameManager.Instance.SetState(GameState.Tutorial);
        TutorialManager.Instance.StartTutorial();
    }
}