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

    [SerializeField] private Button readyUpBtn;
    [SerializeField] private Button hostStartGameBtn;
    [SerializeField] private Button hostBackBtn;

    [SerializeField] private Button readyUpLANBtn;
    [SerializeField] private Button hostStartGameLANBtn;
    [SerializeField] private Button hostBackLANBtn;

    [SerializeField] private GameObject hostingUIObj;
    [SerializeField] private GameObject joinedUIObj;
    [SerializeField] private GameObject LANHostingUIObj;
    [SerializeField] private GameObject LANJoinedUIObj;

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
        TutorialManager.Instance.DespawnTutorialCars();
    }

    public void StartLANBtn()
    {
        TutorialManager.Instance.DespawnTutorialCars();
        OnStartLANPressed?.Invoke();
    }

    public void JoinLANBtn()
    {
        OnJoinLANPressed?.Invoke(ipInput.text);
        TutorialManager.Instance.HideTutorial();
        ResetUIText();
    }

    public void StartGameBtn()
    {
        OnStartGamePressed?.Invoke();
    }

    public void StartClientBtn()
    {
        OnStartClientPressed?.Invoke(joinCodeInput.text.Trim());
        TutorialManager.Instance.HideTutorial();
        ResetUIText();
    }

    public void DissconnectBtn()
    {
        OnDissconnectPressed?.Invoke();

        readyUpBtn.interactable = false;
        hostStartGameBtn.interactable = false;
        hostBackBtn.interactable = false;

        readyUpLANBtn.interactable = false;
        hostStartGameLANBtn.interactable = false;
        hostBackLANBtn.interactable = false;

        TutorialManager.Instance.ShowTutorial();

        ResetUIText();
    }

    private void ResetUIText()
    {
        //reset that may have been changed
        joinCodeDisplay.text = "Generating code...";
        playersConnectedCount.text = "Connected players: 0";
        playersConnectedCountLAN.text = "Connected players: 0";
        joinedWaiting.text = "Waiting for host to start game...";
        joinCodeInput.text = "";
        ipInput.text = "";

        UpdateConnectedPlayerCount(0);
        joinedWaiting.color = Color.white;
    }

    public void DisplayJoinCode(string code)
    {
        joinCodeDisplay.text = code;

        hostBackBtn.interactable = true;
        readyUpBtn.interactable = true;
        TutorialManager.Instance.HideTutorial();
    }

    public void DisplayIpAdress(string ip)
    {
        ipDisplay.text = ip;
        hostBackLANBtn.interactable = true;
        readyUpLANBtn.interactable = true;

        TutorialManager.Instance.HideTutorial();
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

        TutorialManager.Instance.ShowTutorial();
        Camera.main.transform.position = new Vector3(0f, 80f, 0f);

        joinCodeInput.text = "";
        ipInput.text = "";
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

    public void SetHostStartLAN(bool state)
    {
        hostStartGameLANBtn.interactable = state;
    }

    public void StartTutorial()
    {
        GameManager.Instance.SetState(GameState.Tutorial);
        TutorialManager.Instance.StartTutorial();
    }

    public void showOnlineHostingUI()
    {
        if (joinCodeDisplay.text == "Generating code...") TutorialManager.Instance.ShowTutorial();
        else TutorialManager.Instance.HideTutorial();

        hostingUIObj.SetActive(true);
    }

    public void showOnlineJoinedUI()
    {
        joinedUIObj.SetActive(true);
    }

    public void showLANHostingUI()
    {
        LANHostingUIObj.SetActive(true);
    }

    public void showLANJoinedUI()
    {
        LANJoinedUIObj.SetActive(true);
    }
}