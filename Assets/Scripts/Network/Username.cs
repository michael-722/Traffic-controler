using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Username : MonoBehaviour
{
    public static Username Instance;
    public static string SavedUsername = "Player";

    [SerializeField] private TMP_InputField usernameInputField;
    [SerializeField] private Button HostBtn;
    [SerializeField] private Button JoinBtn;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        usernameInputField.onValueChanged.AddListener(OnUsernameInputChanged);
        UpdateButtonInteractability(usernameInputField.text);
    }

    private void OnDisable()
    {
        usernameInputField.onValueChanged.RemoveListener(OnUsernameInputChanged);
    }

    private void OnUsernameInputChanged(string newUsername)
    {
        if (!string.IsNullOrWhiteSpace(newUsername))
            SavedUsername = newUsername;

        UpdateButtonInteractability(newUsername);
    }

    private void UpdateButtonInteractability(string username)
    {
        bool isValid = !string.IsNullOrWhiteSpace(username);
        HostBtn.interactable = isValid;
        JoinBtn.interactable = isValid;
    }
}