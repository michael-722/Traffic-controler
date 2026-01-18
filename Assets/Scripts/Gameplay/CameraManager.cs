using System;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance;

    [SerializeField] private float dragSpeed = 20f;
    [SerializeField] private float zoomSpeed = 10f;
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 50f;

    [SerializeField] private Vector2 areaCenter = Vector2.zero;
    [SerializeField] private Vector2 areaSize = new Vector2(200, 200);

    [SerializeField] private float returnSpeed = 5f;

    private float minX, maxX, minZ, maxZ;

    private Vector3 dragOrigin;
    private bool isDragging = false;

    private void Awake()
    {
        if (Instance != null)
            Destroy(gameObject);
        else
            Instance = this;
    }

    private void Start()
    {
        minX = areaCenter.x - areaSize.x / 2f;
        maxX = areaCenter.x + areaSize.x / 2f;
        minZ = areaCenter.y - areaSize.y / 2f;
        maxZ = areaCenter.y + areaSize.y / 2f;
    }

    void Update()
    {
        if (!IsCameraMovementAllowed())
            return;

        HandleMovement();
        HandleZoom();

        if (!isDragging)
            ApplyBoundarySnap();
    }

    private void ApplyBoundarySnap()
    {
        Vector3 pos = transform.position;

        float targetX = Mathf.Clamp(pos.x, minX, maxX);
        float targetZ = Mathf.Clamp(pos.z, minZ, maxZ);

        Vector3 targetPos = new Vector3(targetX, pos.y, targetZ);

        if (Vector3.Distance(pos, targetPos) > 0.01f)
            transform.position = Vector3.Lerp(transform.position, targetPos, returnSpeed * Time.deltaTime);
    }

    private bool IsCameraMovementAllowed()
    {
        GameState state = GameManager.Instance.GetState();
        return state == GameState.Tutorial || state == GameState.Gameplay;
    }

    private void HandleMovement()
    {
        //end drag
        if (Input.GetMouseButtonUp(0))
            isDragging = false;

        //start drag
        if (!Input.GetMouseButton(0)) return;
        if (Input.GetMouseButtonDown(0))
        {
            dragOrigin = Input.mousePosition;
            isDragging = true;
            return;
        }

        //during drag
        if (Input.GetMouseButton(0) && isDragging)
        {
            Vector3 delta = Input.mousePosition - dragOrigin;

            //adjust movement speed based on height
            float heightFactor = transform.position.y / 50f;
            Vector3 move = new Vector3(-delta.x * dragSpeed * heightFactor * Time.deltaTime, 0, -delta.y * dragSpeed * heightFactor * Time.deltaTime);

            transform.position += move;
            dragOrigin = Input.mousePosition;
        }
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scroll) > 0.01f)
        {
            Vector3 position = transform.position;
            position.y -= scroll * zoomSpeed * 100f * Time.deltaTime;
            position.y = Mathf.Clamp(position.y, minZoom, maxZoom);
            transform.position = position;
        }
    }

    public bool IsOutsideArea()
    {
        Vector3 pos = transform.position;
        return pos.x < minX || pos.x > maxX || pos.z < minZ || pos.z > maxZ;
    }

    public void SetAreaCenter(Vector3 pos)
    {
        areaCenter = new Vector2(pos.x, pos.z);

        minX = areaCenter.x - areaSize.x / 2f;
        maxX = areaCenter.x + areaSize.x / 2f;
        minZ = areaCenter.y - areaSize.y / 2f;
        maxZ = areaCenter.y + areaSize.y / 2f;
    }

    public float GetDistanceFromCenter()
    {
        Vector2 camPosFlat = new Vector2(transform.position.x, transform.position.z);
        return Vector2.Distance(camPosFlat, areaCenter);
    }

    public void ResetCameraPosition()
    {
        SetAreaCenter(Vector3.zero);
        transform.position = new Vector3(0f, 80f, 0f);
        isDragging = false;
    }
}