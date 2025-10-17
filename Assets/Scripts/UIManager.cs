using UnityEngine;
using System;
using TMPro;

public class UIManager : MonoBehaviour
{
    public event Action<string> OnStartClientPressed;
    public event Action OnStartHostPressed;
    public event Action OnDissconnectPressed;

    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private TMP_Text joinCodeDisplay;

    public void StartHostBtn()
    {
        OnStartHostPressed?.Invoke();
    }

    public void StartClientBtn()
    {
        OnStartClientPressed?.Invoke(joinCodeInput.text.Trim());
    }

    public void DissconnectBtn()
    {
        OnDissconnectPressed?.Invoke();
    }

    public void DisplayJoinCode(string code)
    {
        joinCodeDisplay.text = "Join code:" + code;
    }
}
