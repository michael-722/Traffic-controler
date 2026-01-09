using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CarWaitUI : MonoBehaviour
{
    [SerializeField] private Image waitUI;
    [SerializeField] private float countdownTime = 5f;
    [SerializeField] private float penaltyTime = 3f;

    [SerializeField] private GameObject scoreFX;
    [SerializeField] private float fxTime = 3f;//alpha fade out animation curve in fx graph

    [SerializeField] private GameObject waitTimerRootObj;
    [SerializeField] private float shakeMagnitude = 5f;
    [SerializeField] private float pulseScale = 1.2f;
    [SerializeField] private float duration = 0.5f;

    [SerializeField] private bool isActive = false;
    private float currentTime;
    private float penaltyTimer;
    private Coroutine shakeCoroutine;

    private void Start()
    {
        waitTimerRootObj.SetActive(isActive);
    }

    private void Update()
    {
        if (!isActive) return;

        if (currentTime < countdownTime)
        {
            currentTime += Time.deltaTime;
            updateUI();
        }
        else
        {
            shakePulse();
            handelPenalty();
        }
    }

    private void handelPenalty()
    {
        if (penaltyTimer < penaltyTime) penaltyTimer += Time.deltaTime;
        else
        {
            //spawn sore fx
            Vector3 offsetFX = new Vector3(0, 2f, 0);
            GameObject fx = Instantiate(scoreFX, waitTimerRootObj.transform.position, Quaternion.identity);

            fx.transform.position += offsetFX;
            Destroy(fx, fxTime);

            //score update
            int quad = PlayerSpawner.instance.GetQuadrantFromPosition(transform.position);
            ulong onwerId = PlayerSpawner.instance.GetPlayerInQuadrant(quad);
            PlayerManager.Instance.DecreaseScoreServerRpc(onwerId, 1);

            penaltyTimer = 0f;
        }
    }

    private void updateUI()
    {
        //calc 126(green) to 0(red) based on time left
        float t = 1 - (currentTime / countdownTime);
        float h = Mathf.Lerp(0f, 126f / 360f, t);

        Color currentColor = waitUI.color;
        //get hsv to fade color
        Color.RGBToHSV(currentColor, out float x, out float s, out float v);

        //set new color with updated hue
        currentColor = Color.HSVToRGB(h, s, v);
        waitUI.color = new Color(currentColor.r, currentColor.g, currentColor.b, 0.85f);
        waitUI.fillAmount = t;
    }

    public void UIstate(bool state)
    {
        isActive = state;

        if (isActive)
        {
            waitTimerRootObj.SetActive(true);
            resetUI();
        }
        else
        {
            shakeCoroutine = null;
            waitTimerRootObj.SetActive(false);
        }
    }

    private void resetUI()
    {
        currentTime = 0f;
        penaltyTimer = 0f;
    }

    public void shakePulse()
    {
        if (shakeCoroutine != null)
            return;
        shakeCoroutine = StartCoroutine(shakePulseCoroutine());
    }

    private IEnumerator shakePulseCoroutine()
    {
        waitUI.fillAmount = 1f;

        Color currentColor = waitUI.color;
        currentColor.a = 1f;
        waitUI.color = currentColor;

        Vector3 originalPos = waitTimerRootObj.transform.localPosition;
        Vector3 originalScale = waitTimerRootObj.transform.localScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float percent = Mathf.Clamp01(elapsed / duration);
            //shake
            float fade = 1 - percent;
            float x = UnityEngine.Random.Range(-1f, 1f) * shakeMagnitude * fade;
            float y = UnityEngine.Random.Range(-1f, 1f) * shakeMagnitude * fade;
            waitTimerRootObj.transform.localPosition = originalPos + new Vector3(x, y, 0f);

            //pulse
            float scale = Mathf.Lerp(pulseScale, 1f, percent);
            waitTimerRootObj.transform.localScale = originalScale * scale;

            elapsed += Time.deltaTime;
            yield return null;
        }

        waitTimerRootObj.transform.localPosition = originalPos;
        waitTimerRootObj.transform.localScale = originalScale;
        shakeCoroutine = null;
    }
}