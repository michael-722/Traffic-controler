using Unity.Netcode;
using UnityEngine;

public class PlayerStats : NetworkBehaviour
{
    public NetworkVariable<int> TotalCrashes = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> TotalWaitTime = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> WaitTimeEntries = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public void increaseTotalCrashes()
    {
        if (IsServer)
            TotalCrashes.Value++;
    }

    public void AddWaitTime(float waitTime)
    {
        if (IsServer)
        {
            TotalWaitTime.Value += waitTime;
            WaitTimeEntries.Value++;
        }
    }

    public int getCrashes()
    {
        return TotalCrashes.Value;
    }

    public float getTotalWaitTime()
    {
        return TotalWaitTime.Value;
    }

    public int getWaitTimeEntries()
    {
        return WaitTimeEntries.Value;
    }
}