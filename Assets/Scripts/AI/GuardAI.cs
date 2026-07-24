using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SoldierAnimator))]
public class GuardAI : MonoBehaviour
{
    public enum AlertState
    {
        Patrol,
        Attack
    }

    [Header("Patrol Settings")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float waitTimeAtWaypoint = 1.5f;

    [Header("Vision Settings")]
    [SerializeField] private float fovAngle = 75f;
    [SerializeField] private float fovRange = 6f;
    [SerializeField] private LayerMask obstacleMask; // Layer(s) for walls/obstacles

    [Header("Combat Settings")]
    [SerializeField] private float damagePerShot = 15f;
    [SerializeField] private float fireRate = 1f; // Shots per second

    private int currentWaypointIndex = 0;
    private float waitTimer = 0f;
    private bool isWaiting = false;
    private float shootTimer = 0f;

    private AlertState currentState = AlertState.Patrol;
    private Transform activeTarget;
    
    private SoldierAnimator animator;
    private Rigidbody2D rb;

    private void Awake()
    {
        animator = GetComponent<SoldierAnimator>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        currentState = AlertState.Patrol;
        
        // Initialize position to first waypoint if available
        if (waypoints != null && waypoints.Length > 0)
        {
            transform.position = waypoints[0].position;
        }
    }

    private void Update()
    {
        // 1. Scan for targets in line of sight
        activeTarget = FindTarget();

        if (activeTarget != null)
        {
            currentState = AlertState.Attack;
        }
        else
        {
            currentState = AlertState.Patrol;
        }

        // 2. Execute behaviors based on state
        switch (currentState)
        {
            case AlertState.Patrol:
                ExecutePatrol();
                break;
            case AlertState.Attack:
                ExecuteAttack();
                break;
        }
    }

    private void ExecutePatrol()
    {
        // Reset shooting timer when patrolling
        shootTimer = 0f;

        if (waypoints == null || waypoints.Length == 0)
        {
            if (rb != null) rb.linearVelocity = Vector2.zero;
            animator.PlayState(SoldierAnimator.AnimState.Idle);
            return;
        }

        if (isWaiting)
        {
            if (rb != null) rb.linearVelocity = Vector2.zero;
            waitTimer += Time.deltaTime;
            animator.PlayState(SoldierAnimator.AnimState.Idle);
            
            if (waitTimer >= waitTimeAtWaypoint)
            {
                isWaiting = false;
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            }
        }
        else
        {
            Vector2 currentPos = transform.position;
            Vector2 targetPos = waypoints[currentWaypointIndex].position;
            float distance = Vector2.Distance(currentPos, targetPos);

            if (distance > 0.05f)
            {
                Vector2 moveDirection = (targetPos - currentPos).normalized;
                
                if (rb != null)
                {
                    rb.linearVelocity = moveDirection * patrolSpeed;
                }
                else
                {
                    transform.position = Vector2.MoveTowards(currentPos, targetPos, patrolSpeed * Time.deltaTime);
                }

                // Rotate to face movement direction (X-axis forward)
                float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);

                animator.PlayState(SoldierAnimator.AnimState.Walk);
            }
            else
            {
                // Arrived at waypoint
                if (rb != null) rb.linearVelocity = Vector2.zero;
                isWaiting = true;
                waitTimer = 0f;
                animator.PlayState(SoldierAnimator.AnimState.Idle);
            }
        }
    }

    private void ExecuteAttack()
    {
        // Stop moving
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        if (activeTarget == null) return;

        // 1. Rotate to face target
        Vector2 targetPos = activeTarget.position;
        Vector2 direction = (targetPos - (Vector2)transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // 2. Handle Shooting
        shootTimer += Time.deltaTime;
        float fireCooldown = 1f / fireRate;

        if (shootTimer >= fireCooldown)
        {
            shootTimer = 0f;
            FireAtTarget();
        }
        else
        {
            // Keep animator in Idle/Aiming state if not actively playing the shoot frame trigger
            if (animator.GetCurrentState() != SoldierAnimator.AnimState.Shoot)
            {
                animator.PlayState(SoldierAnimator.AnimState.Idle);
            }
        }
    }

    private void FireAtTarget()
    {
        if (activeTarget == null) return;

        // Play shoot animation
        animator.PlayState(SoldierAnimator.AnimState.Shoot, true);

        // Apply damage if target has PatientZeroController
        PatientZeroController player = activeTarget.GetComponent<PatientZeroController>();
        if (player != null)
        {
            player.TakeDamage(damagePerShot);
        }

        // Spawn bullet tracer effect
        StartCoroutine(SpawnBulletTracer(transform.position, activeTarget.position));
    }

    private Transform FindTarget()
    {
        // 1. Find player
        PatientZeroController player = FindObjectOfType<PatientZeroController>();
        if (player != null && !player.IsDead)
        {
            if (IsTargetInVision(player.transform))
            {
                return player.transform;
            }
        }

        // 2. Find zombie minions (forward compatibility)
        ZombieMinion[] minions = FindObjectsOfType<ZombieMinion>();
        foreach (ZombieMinion minion in minions)
        {
            if (minion != null)
            {
                if (IsTargetInVision(minion.transform))
                {
                    return minion.transform;
                }
            }
        }

        return null;
    }

    private bool IsTargetInVision(Transform targetTrans)
    {
        Vector2 startPos = transform.position;
        Vector2 targetPos = targetTrans.position;
        Vector2 directionToTarget = targetPos - startPos;
        float distance = directionToTarget.magnitude;

        // Check distance
        if (distance > fovRange) return false;

        // Check angle relative to guard facing direction (local transform.right)
        float angleToTarget = Vector2.Angle(transform.right, directionToTarget);
        if (angleToTarget > fovAngle / 2f) return false;

        // Temporarily change own layer to Ignore Raycast so we don't hit our own collider
        int oldLayer = gameObject.layer;
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

        // Raycast to check if there is an obstacle blocking line of sight
        RaycastHit2D hit = Physics2D.Raycast(startPos, directionToTarget.normalized, distance, obstacleMask);

        gameObject.layer = oldLayer; // restore layer

        // If the ray hits nothing, the line of sight is clear!
        return hit.collider == null;
    }

    private IEnumerator SpawnBulletTracer(Vector3 start, Vector3 end)
    {
        GameObject tracer = new GameObject("BulletTracer");
        LineRenderer lr = tracer.AddComponent<LineRenderer>();
        lr.startWidth = 0.05f;
        lr.endWidth = 0.02f;
        
        // Use default Sprites shader
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = new Color(1f, 0.8f, 0.1f, 0.8f); // Gold yellow
        lr.endColor = new Color(1f, 0.4f, 0f, 0f);       // Fading orange-red

        lr.SetPosition(0, start);
        lr.SetPosition(1, end);

        float duration = 0.12f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            Color sColor = lr.startColor;
            sColor.a = Mathf.Lerp(0.8f, 0f, t);
            lr.startColor = sColor;
            yield return null;
        }

        Destroy(tracer);
    }

    private void OnDrawGizmosSelected()
    {
        // Draw Vision Range in Editor
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, fovRange);

        // Draw FOV limits
        Vector3 leftBoundary = Quaternion.AngleAxis(fovAngle / 2f, Vector3.forward) * transform.right;
        Vector3 rightBoundary = Quaternion.AngleAxis(-fovAngle / 2f, Vector3.forward) * transform.right;
        
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, leftBoundary * fovRange);
        Gizmos.DrawRay(transform.position, rightBoundary * fovRange);
    }
}
