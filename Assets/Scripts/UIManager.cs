using UnityEngine;
using System;
using Unity.Netcode;
using TMPro;

public class UIManager : NetworkBehaviour
{
    public event Action<string> OnStartClientPressed;
    public event Action OnStartHostPressed;

    [SerializeField] private TMP_InputField joinCode;

    public void StartHostBtn()
    {
        OnStartHostPressed?.Invoke();
    }

    public void StartClientBtn()
    {
        OnStartClientPressed?.Invoke(joinCode.text.Trim());
    }
}
