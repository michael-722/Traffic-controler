using System;
using Unity.Netcode;
using UnityEngine;

public class PlayerReadyState : NetworkBehaviour
{
    public NetworkVariable<bool> isReady = new NetworkVariable<bool>(false);

    public void ToggleReady()
    {
        if (IsOwner)
            SetReadyStateServerRpc(!isReady.Value);
    }

    [ServerRpc]
    private void SetReadyStateServerRpc(bool state)
    {
        isReady.Value = state;
    }

    public bool IsReady()
    {
        return isReady.Value;
    }
}
