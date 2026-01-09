using System;
using System.Text;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerLobbyState : MonoBehaviour
{
    [SerializeField] private Button readyButton;
    [SerializeField] private TMP_Text playerText;
    [SerializeField] private float updateInterval = 0.5f;

    private float lastUpdateTime;

    public static event Action<bool> AllPlayersReady;

    private void Start()
    {
        if (readyButton != null)
        {
            readyButton.onClick.AddListener(OnReadyButtonClicked);
        }

        var ui = UIManager.Instance.GetMenuUI();
        AllPlayersReady += (state) => ui.SetHostStart(state);
    }

    private void Update()
    {
        if (Time.time - lastUpdateTime > updateInterval)
        {
            UpdatePlayerStates();
            lastUpdateTime = Time.time;
        }
    }

    private bool lastAllReadyState;

    private void UpdatePlayerStates()
    {
        if (playerText == null || PlayerManager.Instance == null)
        {
            Debug.LogWarning("player text ref or player score manager instance is null");
            return;
        }

        StringBuilder sb = new StringBuilder();
        var usernames = PlayerManager.Instance.GetAllPlayerUsernames();
        var readyStates = PlayerManager.Instance.GetAllPlayerReadyStates();

        bool allReady = usernames.Count > 0;
        foreach (var player in usernames)
        {
            ulong playerId = player.Key;
            string playerName = player.Value;
            bool isReady = false;

            if (readyStates.TryGetValue(playerId, out isReady))
            {
                string readyText = isReady ? "<color=green>Ready</color>" : "<color=red>Not Ready</color>";
                sb.AppendLine($"{playerName} - {readyText}");
                if (!isReady)
                    allReady = false;
            }
            else
            {
                Debug.LogWarning($"ready state for player {playerName} not found");
                sb.AppendLine($"{playerName} - <color=red>Not Ready</color>");
                allReady = false;
            }
        }

        playerText.text = sb.ToString();

        if (lastAllReadyState != allReady)
        {
            AllPlayersReady?.Invoke(allReady);
            lastAllReadyState = allReady;
        }
    }

    private void OnReadyButtonClicked()
    {
        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer) return;

        var localPlayerObject = NetworkManager.Singleton.LocalClient.PlayerObject;

        if (localPlayerObject != null)
        {
            PlayerReadyState readyState = localPlayerObject.GetComponent<PlayerReadyState>();
            if (readyState != null)
                readyState.ToggleReady();
        }
        else
        {
            Debug.LogError("local player obj not found");
        }
    }
}