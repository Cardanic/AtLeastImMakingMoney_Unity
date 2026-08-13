using UnityEngine;

public class CameraFocus : MonoBehaviour
{
    public static CameraFocus Instance;

    [Header("Focus Settings")]
    public float moveSpeed = 5f;
    public float heightOffset = 10f;    // how high above the target the camera sits
    public float backOffset = 8f;       // how far back from the target the camera sits

    private Vector3 targetPosition;
    private bool isMoving = false;

    void Awake()
    {
        Instance = this;
    }

    public void FocusOn(Vector3 worldPosition)
    {
        targetPosition = worldPosition + new Vector3(0, heightOffset, -backOffset);
        isMoving = true;
    }

    void Update()
    {
        if (!isMoving) return;

        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * moveSpeed);

        if (Vector3.Distance(transform.position, targetPosition) < 0.05f)
        {
            transform.position = targetPosition;
            isMoving = false;
        }
    }
}