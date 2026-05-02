using UnityEngine;
using System.Collections;
using UnityEngine.AI;


public class HostileAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NavMeshAgent navAgent;
    [SerializeField] private Transform playerTransform;



    [Header("Layers")]
    [SerializeField] private LayerMask terrainLayer;
    [SerializeField] private LayerMask playerLayerMask;


    [Header("Patrol Settings")]
    [SerializeField] private float patrolRadius = 10f;
    private Vector3 currentPatrolPoint;
    private bool hasPatrolPoint;


    [Header("Combat Settings")]
    [SerializeField] private float attackCooldown = 1f;
    private bool isOnAttackCooldown;
    [SerializeField] private Transform target;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private Animator animator;

    [Header("Detection Ranges")]
    [SerializeField] private float visionRange = 20f;
    [SerializeField] private float engagementRange = 10f;


    private bool isPlayerVisible;
    private bool isPlayerInRange;


    private void Awake()
    {
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.Find("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
        }


        if (navAgent == null)
        {
            navAgent = GetComponent<NavMeshAgent>();
        }
    }


    private void Update()
    {
        DetectPlayer();
        UpdateBehaviourState();
    }


    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, engagementRange);


        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRange);
    }


    private void DetectPlayer()
    {
        isPlayerVisible = Physics.CheckSphere(transform.position, visionRange, playerLayerMask);
        isPlayerInRange = Physics.CheckSphere(transform.position, engagementRange, playerLayerMask);
    }


    private void Attack()
    {
        if (isOnAttackCooldown) return;

        StartCoroutine(ApproachAndAttack());


    }

    private IEnumerator ApproachAndAttack()
    {
        // 1. Подход к игроку пока не войдём в attackRange
        while (Vector3.Distance(transform.position, playerTransform.position) > attackRange)
        {
            // NavMeshAgent сам движет врага, значит просто задаём цель
            navAgent.SetDestination(playerTransform.position);

            // Поворачиваем модель к игроку
            Vector3 dir = (playerTransform.position - transform.position).normalized;
            dir.y = 0;
            transform.forward = Vector3.Lerp(transform.forward, dir, 10f * Time.deltaTime);

            yield return null;
        }

        // 2. Когда подошли — стоп
        navAgent.SetDestination(transform.position);

        // 3. Анимация атаки
        if (animator != null)
            animator.SetTrigger("Attack");

        // Вся остальная логика cooldown уже есть у тебя в PerformAttack()
    }




    private void FindPatrolPoint()
    {
        float randomX = Random.Range(-patrolRadius, patrolRadius);
        float randomZ = Random.Range(-patrolRadius, patrolRadius);


        Vector3 potentialPoint = new Vector3(transform.position.x + randomX, transform.position.y, transform.position.z + randomZ);


        if (Physics.Raycast(potentialPoint, -transform.up, 2f, terrainLayer))
        {
            currentPatrolPoint = potentialPoint;
            hasPatrolPoint = true;
        }
    }


    private IEnumerator AttackCooldownRoutine()
    {
        isOnAttackCooldown = true;
        yield return new WaitForSeconds(attackCooldown);
        isOnAttackCooldown = false;
    }




    private void PerformPatrol()
    {
        if (!hasPatrolPoint)
            FindPatrolPoint();


        if (hasPatrolPoint)
            navAgent.SetDestination(currentPatrolPoint);


        if (Vector3.Distance(transform.position, currentPatrolPoint) < 1f)
            hasPatrolPoint = false;
    }


    private void PerformChase()
    {
        if (playerTransform != null)
        {
            navAgent.SetDestination(playerTransform.position);
        }
    }


    private void PerformAttack()
    {
        navAgent.SetDestination(transform.position);


        if (playerTransform != null)
        {
            transform.LookAt(playerTransform);
        }


        if (!isOnAttackCooldown)
        {
            Attack();
            StartCoroutine(AttackCooldownRoutine());
        }
    }


    private void UpdateBehaviourState()
    {
        if (!isPlayerVisible && !isPlayerInRange)
        {
            PerformPatrol();
        }
        else if (isPlayerVisible && !isPlayerInRange)
        {
            PerformChase();
        }
        else if (isPlayerVisible && isPlayerInRange)
        {
            PerformAttack();
        }
    }
}
