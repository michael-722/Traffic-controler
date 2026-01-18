using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerLobbyState : MonoBehaviour
{
    public static event Action<bool> AllPlayersReady;

    [SerializeField] private Button readyButton;
    [SerializeField] private TMP_Text playerText;
    [SerializeField] private RectTransform bgRect;
    [SerializeField] private float updateInterval = 0.5f;

    private float lastUpdateTime;
    private bool lastAllReadyState;

    private void OnEnable()
    {
        lastAllReadyState = false;
        HandleAllPlayersReady(false);
    }

    private void HandleAllPlayersReady(bool state)
    {
        var ui = UIManager.Instance.GetMenuUI();

        ui.SetHostStart(state);
        ui.SetHostStartLAN(state);
    }

    private void Update()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient)
        {
            playerText.text = "";
            return;
        }  

        if (Time.time - lastUpdateTime > updateInterval)
        {
            UpdatePlayerStates();
            lastUpdateTime = Time.time;
        }
    }

    private void UpdatePlayerStates()
    {
        if (playerText == null || PlayerManager.Instance == null)
        {
            Debug.LogWarning("player text ref or player score manager instance is null");
            return;
        }

        StringBuilder sb = new StringBuilder();
        Dictionary<ulong, string> usernames = PlayerManager.Instance.GetAllPlayerUsernames();
        Dictionary<ulong, bool> readyStates = PlayerManager.Instance.GetAllPlayerReadyStates();
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

            bgRect.sizeDelta = new Vector2(bgRect.sizeDelta.x, 50f + 43f * (usernames.Count - 1));
        }

        playerText.text = sb.ToString();

        float dynamicWidth = Mathf.Max(300f, playerText.preferredWidth + 40f);
        float dynamicHeight = 50f + (43f * Mathf.Max(0, usernames.Count - 1));
        bgRect.sizeDelta = new Vector2(dynamicWidth, dynamicHeight);

        if (lastAllReadyState != allReady)
        {
            AllPlayersReady?.Invoke(allReady);
            lastAllReadyState = allReady;
            HandleAllPlayersReady(allReady);
        }
    }

    public void OnReadyButtonClicked()
    {
        //if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer) return;

        var localPlayerObject = NetworkManager.Singleton.LocalClient.PlayerObject;

        if (localPlayerObject != null)
        {
            PlayerReadyState readyState = localPlayerObject.GetComponent<PlayerReadyState>();
            if (readyState != null)
                readyState.ToggleReady();
        }
        else Debug.LogWarning("local player obj not found - player probably not spawned yet");
    }
}