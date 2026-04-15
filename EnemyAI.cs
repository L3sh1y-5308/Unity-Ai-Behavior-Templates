using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    [Header("AI Settings")]
    public float moveSpeed = 3.5f;
    public float rotationSpeed = 2f;

    private Transform target;
    private NavMeshAgent navAgent;
    private bool hasTarget = false;

    void Start()
    {
        navAgent = GetComponent<NavMeshAgent>();
        if (navAgent != null)
        {
            navAgent.speed = moveSpeed;
        }
    }

    void Update()
    {
        if (hasTarget && target != null)
        {
            // Идем к цели
            if (navAgent != null)
            {
                navAgent.SetDestination(target.position);
            }
            else
            {
                // Простое движение без NavMesh
                Vector3 direction = (target.position - transform.position).normalized;
                transform.position += direction * moveSpeed * Time.deltaTime;

                // Поворачиваемся к цели
                if (direction != Vector3.zero)
                {
                    Quaternion lookRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
                }
            }
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        hasTarget = true;
        Debug.Log($"{gameObject.name} is now chasing {target.name}");
    }

    public void LoseTarget()
    {
        hasTarget = false;
        if (navAgent != null)
        {
            navAgent.ResetPath();
        }
        Debug.Log($"{gameObject.name} lost target");
    }
}