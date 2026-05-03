using UnityEngine;
using Unity.Behavior;

public class EnemyAI : MonoBehaviour
{
    [Header("Blackboard")]
    private BehaviorGraphAgent behaviorAgent;
    private BlackboardVariable m_targetDetect;
    private BlackboardVariable m_targetPlayer;
    private BlackboardVariable m_foodDetect;
    private BlackboardVariable m_detectedFood;
    private BlackboardVariable m_dangerDetect;
    private BlackboardVariable m_detectedDanger;
    private BlackboardVariable m_attaking;

    private Transform targetPlayer;
    private Transform detectedFood;
    private Transform detectedDanger;
    private bool hasTarget;

    [Header("Animator")]
    private Animator animator;
    [SerializeField] public string attackAnimatorParameterName = "Rat_Attaking";

    public Transform TargetPlayer => targetPlayer;
    public bool HasTarget => hasTarget;
    public Transform DetectedFood => detectedFood;
    public Transform DetectedDanger => detectedDanger;

    private AdaptiveVisionSystem visionSystem;

    void Awake()
    {
        behaviorAgent = GetComponent<BehaviorGraphAgent>();
        visionSystem = GetComponent<AdaptiveVisionSystem>();
        animator = GetComponent<Animator>();

        if (behaviorAgent == null)
        {
            Debug.LogError($"EnemyAI на {gameObject.name} требует BehaviorGraphAgent компонент!");
        }

        if (animator == null)
        {
            Debug.LogWarning($"EnemyAI на {gameObject.name} требует Animator компонент для синхронизации анимации!");
        }
    }

    void Update()
    {
        // Sync vision system state with AI
        if (visionSystem != null)
        {
            if (visionSystem.isPlayerVisible && visionSystem.detectedPlayer != null)
                SetTarget(visionSystem.detectedPlayer);
            else if (!visionSystem.isPlayerVisible)
                LoseTarget();

            if (visionSystem.isFoodVisible && visionSystem.detectedFood != null)
                SetDetectedFood(visionSystem.detectedFood);
            else if (!visionSystem.isFoodVisible)
                LoseFood();

            if (visionSystem.isDangerVisible && visionSystem.detectedDanger != null)
                SetDetectedDanger(visionSystem.detectedDanger);
            else if (!visionSystem.isDangerVisible)
                LoseDanger();
        }
    }

    /// <summary>
    /// Устанавливает цель (игрока) для преследования.
    /// BehaviorTree берёт эту информацию из Blackboard.
    /// </summary>
    public void SetTarget(Transform target)
    {
        targetPlayer = target;
        hasTarget = targetPlayer != null;

        UpdateBlackboard();

        if (hasTarget)
        {
            Debug.Log($"<color=cyan>[EnemyAI] Цель установлена: {targetPlayer.gameObject.name}</color>");
        }
    }

    /// <summary>
    /// Теряет цель (игрок вышел из зоны видимости).
    /// BehaviorTree прекратит преследование.
    /// </summary>
    public void LoseTarget()
    {
        targetPlayer = null;
        hasTarget = false;

        UpdateBlackboard();

        Debug.Log($"<color=yellow>[EnemyAI] Цель потеряна</color>");
    }

    /// <summary>
    /// Устанавливает обнаруженную еду.
    /// Вызывается из AdaptiveVisionSystem.
    /// </summary>
    public void SetDetectedFood(Transform food)
    {
        detectedFood = food;
        UpdateBlackboard();

        if (detectedFood != null)
        {
            Debug.Log($"<color=cyan>[EnemyAI] Еда обнаружена: {detectedFood.gameObject.name}</color>");
        }
    }

    /// <summary>
    /// Теряет еду из вида.
    /// </summary>
    public void LoseFood()
    {
        detectedFood = null;
        UpdateBlackboard();

        Debug.Log($"<color=cyan>[EnemyAI] Еда потеряна</color>");
    }

    /// <summary>
    /// Устанавливает обнаруженную опасность.
    /// Вызывается из AdaptiveVisionSystem.
    /// </summary>
    public void SetDetectedDanger(Transform danger)
    {
        detectedDanger = danger;
        UpdateBlackboard();

        if (detectedDanger != null)
        {
            Debug.Log($"<color=red>[EnemyAI] Опасность обнаружена: {detectedDanger.gameObject.name}</color>");
        }
    }

    /// <summary>
    /// Теряет опасность из вида.
    /// </summary>
    public void LoseDanger()
    {
        detectedDanger = null;
        UpdateBlackboard();

        Debug.Log($"<color=red>[EnemyAI] Опасность потеряна</color>");
    }

    /// <summary>
    /// Синхронизирует состояние Attaking из Blackboard с параметром Animator
    /// </summary>
    private void SyncAttakingWithAnimator()
    {
        if (animator == null || m_attaking == null)
            return;

        bool attackingState = (bool)m_attaking.ObjectValue;
        animator.SetBool(attackAnimatorParameterName, attackingState);

        Debug.Log($"<color=magenta>[EnemyAI] Animator синхронизирован: {attackAnimatorParameterName} = {attackingState}</color>");
    }

    /// <summary>
    /// Обновляет все переменные Blackboard для BehaviorTree
    /// </summary>
    void UpdateBlackboard()
    {
        if (behaviorAgent == null || behaviorAgent.BlackboardReference == null)
            return;

        // Игрок
        behaviorAgent.BlackboardReference.GetVariable("TargetDetect", out m_targetDetect);
        if (m_targetDetect != null)
        {
            m_targetDetect.ObjectValue = hasTarget;
        }

        behaviorAgent.BlackboardReference.GetVariable("TargetPlayer", out m_targetPlayer);
        if (m_targetPlayer != null)
        {
            m_targetPlayer.ObjectValue = targetPlayer;
        }

        // Анимация атаки
        behaviorAgent.BlackboardReference.GetVariable("Attaking", out m_attaking);
        if (m_attaking != null)
        {
            m_attaking.ObjectValue = hasTarget;
            SyncAttakingWithAnimator();
        }


        // Еда
        behaviorAgent.BlackboardReference.GetVariable("FoodDetect", out m_foodDetect);
        if (m_foodDetect != null)
        {
            m_foodDetect.ObjectValue = detectedFood != null;
        }

        behaviorAgent.BlackboardReference.GetVariable("DetectedFood", out m_detectedFood);
        if (m_detectedFood != null)
        {
            m_detectedFood.ObjectValue = detectedFood;
        }

        // Опасность
        behaviorAgent.BlackboardReference.GetVariable("DangerDetect", out m_dangerDetect);
        if (m_dangerDetect != null)
        {
            m_dangerDetect.ObjectValue = detectedDanger != null;
        }

        behaviorAgent.BlackboardReference.GetVariable("DetectedDanger", out m_detectedDanger);
        if (m_detectedDanger != null)
        {
            m_detectedDanger.ObjectValue = detectedDanger;
        }
    }

    /// <summary>
    /// Возвращает расстояние до цели (игрока)
    /// </summary>
    public float GetDistanceToTarget()
    {
        if (targetPlayer == null)
            return float.MaxValue;

        return Vector3.Distance(transform.position, targetPlayer.position);
    }
}