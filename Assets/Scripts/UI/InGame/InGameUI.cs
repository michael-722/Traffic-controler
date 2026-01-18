using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class InGameUI : MonoBehaviour
{
    [SerializeField] private Image timeLeftImg;

    private RectTransform timeFillRect;
    private float originalTimeFillWidth;

    private void Start()
    {
        timeFillRect = timeLeftImg.GetComponent<RectTransform>();
        originalTimeFillWidth = timeFillRect.sizeDelta.x;

        WinLoseConditions.Instance.timeToComplete.OnValueChanged += OnTimeChanged;
        WinLoseConditions.Instance.currentJournies.OnValueChanged += OnJourniesChanged;

        RefreshUI();
    }

    private void OnTimeChanged(float prev, float curr) => RefreshUI();
    private void OnJourniesChanged(int prev, int curr) => RefreshUI();

    private void RefreshUI()
    {
        WinLoseConditions state = WinLoseConditions.Instance;

        float fillAmount = Mathf.Clamp01(state.timeToComplete.Value / WinLoseConditions.Instance.maxTime);
        timeFillRect.sizeDelta = new Vector2(originalTimeFillWidth * fillAmount, timeFillRect.sizeDelta.y);
    }
}