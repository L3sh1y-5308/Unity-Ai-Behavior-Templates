using UnityEngine;
using UnityEngine.AI;
using Unity.Behavior;

public class AIController : MonoBehaviour
{
    public NavMeshAgent agent;
    public Animator animator;

    // Эту переменную будет менять Behavior Tree (вместо Set animation trigger)
    public bool isAttacking = false;

    private bool isOnCooldown = false;

    void Update()
    {
        if (isAttacking)
        {
            // 1. ОСТАНАВЛИВАЕМ НОГИ (решает проблему удара на лету)
            agent.isStopped = true;
            agent.velocity = Vector3.zero; // Гасим инерцию мгновенно

            // 2. СКОРОСТЬ В АНИМАТОР = 0 (чтобы включился Idle или стойка)
            animator.SetFloat("Speed", 0f);

            // 3. БЬЕМ (если не на кулдауне)
            if (!isOnCooldown)
            {
                animator.SetTrigger("Attack");
                StartCoroutine(AttackCooldownRoutine());
            }
        }
        else
        {
            // ЕСЛИ НЕ АТАКУЕМ (игрока нет рядом, бегаем или патрулируем)

            // 1. РАЗРЕШАЕМ ИДТИ
            agent.isStopped = false;

            // 2. ПЕРЕДАЕМ РЕАЛЬНУЮ СКОРОСТЬ В АНИМАТОР
            // Если агент стоит - будет 0 (Idle). Если идет - будет Walk/Run.
            animator.SetFloat("Speed", agent.velocity.magnitude);
        }
    }

    private System.Collections.IEnumerator AttackCooldownRoutine()
    {
        isOnCooldown = true;
        yield return new WaitForSeconds(1.5f); // Время твоего удара/кулдауна
        isOnCooldown = false;
    }
}