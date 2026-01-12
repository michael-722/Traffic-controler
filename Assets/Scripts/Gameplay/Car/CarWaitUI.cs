using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Unity.Netcode;

public class CarWaitUI : NetworkBehaviour
{
    [SerializeField] private Image waitUI;
    [SerializeField] private GameObject waitTimerRootObj;
    [SerializeField] private GameObject scoreFX;

    [SerializeField] private float countdownTime = 5f;
    [SerializeField] private float penaltyTime = 3f;
    [SerializeField] private float fxTime = 3f;
    [SerializeField] private float shakeMagnitude = 5f;
    [SerializeField] private float pulseScale = 1.2f;
    [SerializeField] private float duration = 0.5f;

    private Coroutine shakeCoroutine;

    private NetworkVariable<float> currentTime = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<float> penaltyTimer = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> isActive = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Start()
    {
        isActive.OnValueChanged += OnActiveChanged;
        waitTimerRootObj.SetActive(false);
    }

    public override void OnNetworkSpawn()
    {
        isActive.OnValueChanged += OnActiveChanged;
        waitTimerRootObj.SetActive(isActive.Value);
    }

    private void Update()
    {
        if (IsServer || !IsSpawned)
            UpdateServerLogic();

        if (isActive.Value)
            UpdateVisuals(currentTime.Value);
    }

    private void UpdateServerLogic()
    {
        if (!isActive.Value) return;

        if (currentTime.Value < countdownTime)
            currentTime.Value += Time.deltaTime;
        else
        {
            if (!IsSpawned)
                StartShakeLocal();
            else
                ShakePulseClientRpc();

            HandlePenaltyServer();
        }
    }

    private void HandlePenaltyServer()
    {
        if (penaltyTimer.Value < penaltyTime)
            penaltyTimer.Value += Time.deltaTime;
        else
        {
            penaltyTimer.Value = 0f;
            SpawnScoreFXClientRpc();

            //score penalty
            int quad = PlayerSpawner.instance.GetQuadrantFromPosition(transform.position);
            ulong ownerId = PlayerSpawner.instance.GetPlayerInQuadrant(quad);
            PlayerManager.Instance.DecreaseScoreServerRpc(ownerId, 1);
        }
    }

    private void UpdateVisuals(float time)
    {
        float t = Mathf.Clamp01(1 - (time / countdownTime));

        //color fade - red (0) to green (126)
        float h = Mathf.Lerp(0f, 126f / 360f, t);
        Color currentColor = waitUI.color;
        Color.RGBToHSV(currentColor, out _, out float s, out float v);
        Color newColor = Color.HSVToRGB(h, s, v);

        waitUI.color = new Color(newColor.r, newColor.g, newColor.b, 0.85f);
        waitUI.fillAmount = t;
    }

    public void UIstate(bool state)
    {
        if (IsSpawned)
            if (!IsServer) return;

        isActive.Value = state;
        if (state)
        {
            currentTime.Value = 0f;
            penaltyTimer.Value = 0f;
        }
    }

    private void OnActiveChanged(bool prev, bool curr)
    {
        waitTimerRootObj.SetActive(curr);

        if (!curr)
        {
            if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
            shakeCoroutine = null;
        }
    }

    private void StartShakeLocal()
    {
        if (shakeCoroutine == null)
            shakeCoroutine = StartCoroutine(ShakePulseCoroutine());
    }

    [ClientRpc]
    private void ShakePulseClientRpc()
    {
        if (shakeCoroutine == null)
            shakeCoroutine = StartCoroutine(ShakePulseCoroutine());
    }

    [ClientRpc]
    private void SpawnScoreFXClientRpc()
    {
        Vector3 offsetFX = new Vector3(0, 2f, 0);
        GameObject fx = Instantiate(scoreFX, waitTimerRootObj.transform.position + offsetFX, Quaternion.identity);
        Destroy(fx, fxTime);
    }

    private IEnumerator ShakePulseCoroutine()
    {
        Vector3 originalPos = waitTimerRootObj.transform.localPosition;
        Vector3 originalScale = waitTimerRootObj.transform.localScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float percent = elapsed / duration;
            float fade = 1 - percent;

            float x = UnityEngine.Random.Range(-1f, 1f) * shakeMagnitude * fade;
            float y = UnityEngine.Random.Range(-1f, 1f) * shakeMagnitude * fade;

            waitTimerRootObj.transform.localPosition = originalPos + new Vector3(x, y, 0f);
            waitTimerRootObj.transform.localScale = originalScale * Mathf.Lerp(pulseScale, 1f, percent);

            elapsed += Time.deltaTime;
            yield return null;
        }

        waitTimerRootObj.transform.localPosition = originalPos;
        waitTimerRootObj.transform.localScale = originalScale;
        shakeCoroutine = null;
    }
}