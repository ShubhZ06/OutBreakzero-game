using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;
    [SerializeField] private float smoothSpeed = 6f;
    [SerializeField] private Vector2 offset = Vector2.zero;
    [SerializeField] private float cameraZ = -10f;

    private void Start()
    {
        // Try to automatically find the player (Patient Zero) if not assigned in Inspector
        if (target == null)
        {
            PatientZeroController player = FindObjectOfType<PatientZeroController>();
            if (player != null)
            {
                target = player.transform;
            }
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Target position plus offsets
        Vector3 targetPosition = new Vector3(target.position.x + offset.x, target.position.y + offset.y, cameraZ);

        // Smoothly interpolate between camera's current position and target position
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
    }
}
