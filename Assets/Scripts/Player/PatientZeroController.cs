using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[RequireComponent(typeof(SpriteAnimator))]
public class PatientZeroController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float speed = 3f;
    [SerializeField] private float arrivalThreshold = 0.05f;

    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    private Vector2 targetPosition;
    private bool isMoving = false;
    private SpriteAnimator spriteAnimator;
    private Rigidbody2D rb;

    private void Awake()
    {
        spriteAnimator = GetComponent<SpriteAnimator>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        currentHealth = maxHealth;
        targetPosition = transform.position;
    }

    private void Update()
    {
        // Handle touch / mouse input using the New Input System
        if (Pointer.current != null && Pointer.current.press.isPressed)
        {
            // Avoid moving when clicking on UI elements
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            // Convert screen position to world coordinates
            Vector2 screenPos = Pointer.current.position.ReadValue();
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
            targetPosition = new Vector2(worldPos.x, worldPos.y);
            isMoving = true;
        }

        // Determine direction and movement
        Vector2 currentPos = transform.position;
        float distance = Vector2.Distance(currentPos, targetPosition);

        if (isMoving && distance > arrivalThreshold)
        {
            Vector2 displacement = targetPosition - currentPos;
            Vector2 moveDirection = displacement.normalized;

            // Apply movement smoothly
            if (rb != null)
            {
                // Rigidbodies must be moved by setting velocity to prevent conflict with physics loops
                rb.linearVelocity = moveDirection * speed;
            }
            else
            {
                // If no physics, MoveTowards is perfectly smooth and handles overshooting natively
                transform.position = Vector2.MoveTowards(currentPos, targetPosition, speed * Time.deltaTime);
            }

            // Update animations
            spriteAnimator.PlayState(SpriteAnimator.AnimState.Move);

            // Flip only if there is a significant horizontal distance to prevent microscopic float noise flipping the sprite
            if (Mathf.Abs(displacement.x) > 0.05f)
            {
                spriteAnimator.SetFacingDirection(displacement);
            }
        }
        else
        {
            // Arrived or stationary
            isMoving = false;
            
            // Stop Rigidbody2D velocity if active to prevent sliding, and snap position for precision
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero; 
                if (distance > 0.01f && distance <= arrivalThreshold)
                {
                    rb.position = targetPosition;
                }
            }

            spriteAnimator.PlayState(SpriteAnimator.AnimState.Idle);
        }
    }

    /// <summary>
    /// Helper method to set movement target programmatically if needed.
    /// </summary>
    public void SetTargetPosition(Vector2 position)
    {
        targetPosition = position;
        isMoving = true;
    }

    public bool IsDead => currentHealth <= 0f;

    public void TakeDamage(float damage)
    {
        if (currentHealth <= 0f) return;

        currentHealth -= damage;
        Debug.Log($"[PatientZero] Took {damage} damage! Remaining Health: {currentHealth}/{maxHealth}");
        
        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("[PatientZero] Patient Zero died. Defeat!");
        isMoving = false;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
        spriteAnimator.PlayState(SpriteAnimator.AnimState.Idle);
        // We can reload scene or open GameOver UI here later
    }
}
