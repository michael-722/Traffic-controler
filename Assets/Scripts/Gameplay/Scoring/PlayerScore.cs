using System;
using Unity.Netcode;
using UnityEngine;

public class PlayerScore : NetworkBehaviour
{
    public NetworkVariable<int> Score = new NetworkVariable<int>(0);

    public static event Action<int> OnAnyScoreChanged;

    private void Start()
    {
        Score.OnValueChanged += OnScoreChanged;
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

    //-- collisions
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