using System;
using Unity.Netcode;
using UnityEngine;
using Unity.Collections;

public class PlayerInfo : NetworkBehaviour
{
    public NetworkVariable<int> Score = new NetworkVariable<int>(0);
    //string cant be used - dynamic size allocation issue
    public NetworkVariable<FixedString32Bytes> PlayerName = new NetworkVariable<FixedString32Bytes>("");

    public static event Action<int> OnAnyScoreChanged;

    private void Start()
    {
        Score.OnValueChanged += OnScoreChanged;
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            if (!string.IsNullOrWhiteSpace(Username.SavedUsername))
                SetUsernameServerRpc(Username.SavedUsername);
            else
            {
                Debug.LogWarning("saved username was empty - using tempary");
                SetUsernameServerRpc("player");
            }
        }

        base.OnNetworkSpawn();
    }

    [ServerRpc]
    public void SetUsernameServerRpc(string newUsername)
    {
        PlayerName.Value = newUsername;
    }

    public string GetUsername()
    {
        return PlayerName.Value.ToString();
    }

    private void OnScoreChanged(int oldValue, int newValue)
    {
        //Debug.Log($"score changed from {oldValue} to {newValue} for client {NetworkObject.OwnerClientId}");
        OnAnyScoreChanged?.Invoke(newValue);
    }

    public int GetScore()
    {
        return Score.Value;
    }

    public void addScore(int amount)
    {
        if (IsServer)
            Score.Value += amount;
    }

    //--collisions
    public void subtractScore(int amount)
    {
        if (IsServer)
            Score.Value -= amount;
    }

    //resetsing game
    public void setScore(int newScore)
    {
        if (IsServer)
            Score.Value = newScore;
    }
}