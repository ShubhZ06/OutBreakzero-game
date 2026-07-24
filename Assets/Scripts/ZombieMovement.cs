using UnityEngine;

public class ZombieMovement : MonoBehaviour
{
    public float speed = 3f;

    [Header("Camera Settings")]
    public float cameraSmoothSpeed = 5f; // Adjust this to make the camera follow snappier or looser

    private Vector2 targetPosition;
    private Animator anim;
    private Camera mainCamera;

    void Start()
    {
        // Set the initial target to the current position so he doesn't move on start
        targetPosition = transform.position;
        anim = GetComponent<Animator>();

        // Cache the main camera once at start for better performance
        mainCamera = Camera.main;
    }

    void Update()
    {
        // 1. Detect Touch or Mouse Click
        if (Input.GetMouseButtonDown(0))
        {
            // Convert the screen click into world coordinates
            targetPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        }

        // Calculate distance to avoid floating point math errors
        float distance = Vector2.Distance(transform.position, targetPosition);

        // 2. Check if we need to move using a tiny distance threshold 
        if (distance > 0.05f)
        {
            // Move the zombie toward the target
            transform.position = Vector2.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

            // 3. Rotate to face the target position
            Vector2 direction = targetPosition - (Vector2)transform.position;

            // THE ROTATION FIX: Only assign rotation if the direction vector is large enough
            if (direction.sqrMagnitude > 0.001f)
            {
                transform.right = direction; // Aligns the zombie's right side (its face) to the direction
            }

            // 4. Trigger the walking animation
            anim.SetBool("isWalking", true);
        }
        else
        {
            // We have arrived. Snap to the exact target and stop the animation.
            transform.position = targetPosition;
            anim.SetBool("isWalking", false);
        }
    }

    // 5. Implement Camera Follow
    // LateUpdate runs after all Update functions have finished, perfect for cameras!
    void LateUpdate()
    {
        if (mainCamera != null)
        {
            // Create a target position for the camera (matching the zombie's X and Y, keeping camera's Z)
            Vector3 desiredCameraPosition = new Vector3(transform.position.x, transform.position.y, mainCamera.transform.position.z);

            // Smoothly move the camera towards the desired position using Lerp
            mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, desiredCameraPosition, cameraSmoothSpeed * Time.deltaTime);
        }
    }
}