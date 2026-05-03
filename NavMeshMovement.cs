using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
/*
[System.Serializable]
public class RunAnimPlayer
{
    public string commandName;       // Название команды для скриптов (например, "IsDead")
    public string animatorParameter;  // Точное имя в окне Animator (например, "Rat=Dead")

    [HideInInspector]
    public int parameterHash;        // Числовой ID для скорости
}
*/

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class NavMeshMovement : MonoBehaviour
{
    public NavMeshAgent _agent;
    public Animator _animator;

    [Header("Настройки движения")]
    public float walkSpeed = 1.5f;
    public float runSpeed = 5f;

    [Header("Цель (куда идти)")]
    public Transform target;

    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();
    }

    void Update()
    {
        // 1. Указываем агенту цель (если она есть)
        if (target != null)
        {
            _agent.SetDestination(target.position);
        }

        // 2. Логика переключения скорости (для примера)
        // Если до цели далеко — бежим, если близко — идем
        float distance = Vector3.Distance(transform.position, _agent.destination);

        if (distance > 5f)
        {
            _agent.speed = runSpeed;
        }
        else
        {
            _agent.speed = walkSpeed;
        }

        // 3. ПЕРЕДАЧА СКОРОСТИ В АНИМАТОР
        // magnitude — это фактическая скорость объекта в текущий момент
        float currentSpeed = _agent.velocity.magnitude;

        // "Speed" — это название параметра Float в вашем Аниматоре
        _animator.SetFloat("Speed", currentSpeed, 0.1f, Time.deltaTime);
    }
}