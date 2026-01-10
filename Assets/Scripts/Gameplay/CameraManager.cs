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

    private float minX, maxX, minZ, maxZ;

    private Vector3 dragOrigin;

    //old input sys - update to new input sys needed=============================

    private void Start()
    {
        Instance = this;

        minX = areaCenter.x - areaSize.x / 2f;
        maxX = areaCenter.x + areaSize.x / 2f;
        minZ = areaCenter.y - areaSize.y / 2f;
        maxZ = areaCenter.y + areaSize.y / 2f;
    }

    void Update()
    {
        HandleMovement();
        HandleZoom();
    }

    void HandleMovement()
    {
        if (!Input.GetMouseButton(0)) return;
        if (Input.GetMouseButtonDown(0))
        {
            dragOrigin = Input.mousePosition;
            return;
        }

        Vector3 delta = Input.mousePosition - dragOrigin;
        float scaledDragSpeed = dragSpeed * (transform.position.y / 50f);
        Vector3 move = new Vector3(-delta.x * scaledDragSpeed * Time.deltaTime, 0, -delta.y * scaledDragSpeed * Time.deltaTime);

        transform.Translate(move, Space.World);
        dragOrigin = Input.mousePosition;
    }

    void HandleZoom()
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
}