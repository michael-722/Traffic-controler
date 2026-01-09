using UnityEngine;
using Unity.Netcode;

public class TestBtn : MonoBehaviour
{
    public void TestScoreSystem()
    {
        if (NetworkManager.Singleton != null)
        {
            ulong localId = NetworkManager.Singleton.LocalClientId;
            PlayerManager.Instance.IncreaseScoreServerRpc(localId, 10);
        }
    }
}