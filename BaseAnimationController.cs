using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class AnimationMapping
{
    public string commandName;       // Название команды для скриптов (например, "IsDead")
    public string animatorParameter;  // Точное имя в окне Animator (например, "Rat=Dead")
    
    [HideInInspector] 
    public int parameterHash;        // Числовой ID для скорости
}

public class BaseAnimationController : MonoBehaviour
{
    public List<AnimationMapping> animations = new List<AnimationMapping>();
    private Animator _animator;
    private Dictionary<string, int> _commandMap = new Dictionary<string, int>();

    void Awake()
    {
        _animator = GetComponent<Animator>();
        
        foreach (var mapping in animations)
        {
            mapping.parameterHash = Animator.StringToHash(mapping.animatorParameter);
            _commandMap[mapping.commandName] = mapping.parameterHash;
        }
    }

    // --- ДЛЯ ПАРАМЕТРОВ BOOL (True/False) ---
    // Вызов в коде: controller.SetBoolState("Running", true);
    public void SetBoolState(string command, bool value)
    {
        if (_animator != null && _commandMap.TryGetValue(command, out int hash))
        {
            _animator.SetBool(hash, value);
        }
    }

    // --- ДЛЯ ТРИГГЕРОВ (Одноразовое действие) ---
    // Вызов в коде: controller.ActivateTrigger("Hit");
    public void ActivateTrigger(string command)
    {
        if (_animator != null && _commandMap.TryGetValue(command, out int hash))
        {
            _animator.SetTrigger(hash);
        }
    }

    // ПРИМЕРЫ ДЛЯ ВАШЕЙ КРЫСЫ (можно вызывать из кнопок или BT):
    public void CmdRunStart() => SetBoolState("Run", true);
    public void CmdRunStop() => SetBoolState("Run", false);
    public void CmdGetHit() => ActivateTrigger("Hit");
    public void CmdDie() => SetBoolState("Dead", true); // Если в аниматоре это Bool
}