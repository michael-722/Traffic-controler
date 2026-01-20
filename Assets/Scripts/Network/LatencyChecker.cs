using UnityEngine;
using Unity.Netcode;

public class LatencyChecker : MonoBehaviour
{
    void Update()
    {
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer)
        {
            var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport;

            ulong serverId = NetworkManager.ServerClientId;
            long rttMillis = (long)transport.GetCurrentRtt(serverId);

            Debug.Log($"Current RTT: {rttMillis}ms");
        }
    }
}
