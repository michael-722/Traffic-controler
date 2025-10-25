using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [SerializeField] private float dragSpeed = 20f;
    [SerializeField] private float zoomSpeed = 10f;
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 50f;

    private Vector3 dragOrigin;

    //==========================
    //old input sys - update to new input sys needed
    //==========================

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
}
