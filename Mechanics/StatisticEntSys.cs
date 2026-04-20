using UnityEngine;

public class StatisticEntSys : MonoBehaviour
{
    [Header("Hunger Settings")]
    [SerializeField] private float _maxHunger = 100f;
    [SerializeField] private float _hungerDepletionRate = 1.5f;
    [SerializeField] private float _currentHunger;

    public float CurrentHunger => _currentHunger;
    public float HungerPercent => _currentHunger / _maxHunger;

    void Start()
    {
        // Инициализация начального значения голода (полная сытость)
        _currentHunger = _maxHunger;
    }

    void Update()
    {
        ProcessHunger();
    }

    private void ProcessHunger()
    {
        if (_currentHunger > 0)
        {
            _currentHunger -= _hungerDepletionRate * Time.deltaTime;
            
            if (_currentHunger < 0)
            {
                _currentHunger = 0;
                OnStarving();
            }
        }
    }

    public void Eat(float amount)
    {
        _currentHunger = Mathf.Clamp(_currentHunger + amount, 0, _maxHunger);
    }

    private void OnStarving()
    {
        // Логика, когда персонаж полностью проголодался (например, уменьшение здоровья)
        Debug.Log($"{gameObject.name} is starving!");
    }
}
