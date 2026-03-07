using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    public float moveSpeed = 20f;
    public float zoomSpeed = 15f;
    public float rotationSpeed = 5f;

    public float minY = 8f;
    public float maxY = 40f;

    void Update()
    {
        Move();
        Zoom();
        Rotate();
    }

    void Move()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        forward.y = 0f;
        right.y = 0f;

        Vector3 dir = (forward.normalized * v + right.normalized * h);
        transform.position += dir * moveSpeed * Time.deltaTime;
    }

    void Zoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        Vector3 pos = transform.position;
        pos.y -= scroll * zoomSpeed * 100f * Time.deltaTime;
        pos.y = Mathf.Clamp(pos.y, minY, maxY);

        transform.position = pos;
    }

    void Rotate()
    {
        if (Input.GetMouseButton(1)) // Hold Right Mouse Button
        {
            float mouseX = Input.GetAxis("Mouse X");
            transform.Rotate(Vector3.up, mouseX * rotationSpeed * 100f * Time.deltaTime, Space.World);
        }
    }
}