using UnityEngine;
using Unity.Netcode;

public class TestBtn : MonoBehaviour
{
    public void TestScoreSystem()
    {
        if (NetworkManager.Singleton != null)
        {
            ulong localId = NetworkManager.Singleton.LocalClientId;
            PlayerScoreManager.Instance.increaseScoreServerRpc(localId, 10);
        }
    }
}