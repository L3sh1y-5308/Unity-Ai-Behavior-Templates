using System.Collections.Generic;
using UnityEngine;

public class HemisphereGenerator : MonoBehaviour
{
    [Header("Hemisphere Collider Settings")]
    public float maxRadius = 5f;
    public float minRadius = 0.5f;
    public int latitudeSegments = 10;
    public int longitudeSegments = 20;

    [Header("Adaptation Settings")]
    public LayerMask obstacleLayer = -1;
    public float adaptationSpeed = 5f;
    public float shrinkMultiplier = 0.8f;
    public bool smoothAdaptation = true;

    [Header("Player Protection")]
    public Transform player;
    public float playerBuffer = 0.2f;

    [Header("Enemy Detection")]
    public LayerMask enemyLayer = 1 << 8;
    public List<Transform> detectedEnemies = new List<Transform>();
    public float detectionCheckRadius = 0.5f; // Радиус проверки врагов в каждой точке

    [Header("Debug Visualization")]
    public bool showRays = true;
    public bool showAdaptedShape = true;
    public bool showEnemyConnections = true;

    private List<Vector3> hemisphereDirections = new List<Vector3>();
    private List<float> adaptedRadii = new List<float>();
    private List<float> targetRadii = new List<float>();
    private List<RaycastHit> hitInfos = new List<RaycastHit>();

    void Start()
    {
        GenerateHemisphereDirections();
        InitializeRadii();

        if (player == null)
            player = transform;

        // НЕ СОЗДАЕМ ОБЫЧНЫЙ КОЛЛАЙДЕР!
        // SetupTriggerCollider(); // УДАЛИ ЭТУ СТРОКУ
    }

    void Update()
    {
        AdaptToEnvironment();
        if (smoothAdaptation)
            SmoothRadiusTransition();

        // Проверяем врагов в адаптивной зоне
        CheckEnemiesInAdaptiveZone();
    }

    // Новый метод: проверка врагов только в видимых областях
    void CheckEnemiesInAdaptiveZone()
    {
        Vector3 origin = player.position;
        List<Transform> currentlyDetected = new List<Transform>();

        // Проверяем каждую точку адаптивной полусферы
        for (int i = 0; i < hemisphereDirections.Count; i++)
        {
            if (i >= adaptedRadii.Count) continue;

            Vector3 direction = hemisphereDirections[i];
            float radius = adaptedRadii[i];

            // Создаем точку на поверхности адаптивной полусферы
            Vector3 checkPoint = origin + direction * radius;

            // Ищем врагов вокруг этой точки
            Collider[] nearbyEnemies = Physics.OverlapSphere(checkPoint, detectionCheckRadius, enemyLayer);

            foreach (Collider enemyCollider in nearbyEnemies)
            {
                Transform enemy = enemyCollider.transform;

                // Проверяем прямую видимость до врага
                if (IsEnemyVisible(enemy) && !currentlyDetected.Contains(enemy))
                {
                    currentlyDetected.Add(enemy);

                    // Если враг новый - добавляем в список
                    if (!detectedEnemies.Contains(enemy))
                    {
                        detectedEnemies.Add(enemy);

                        EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
                        if (enemyAI != null)
                        {
                            enemyAI.SetTarget(player);
                        }

                        Debug.Log($"Enemy detected: {enemy.name}");
                    }
                }
            }
        }

        // Убираем врагов, которые больше не обнаружены
        List<Transform> enemiesToRemove = new List<Transform>();
        foreach (Transform enemy in detectedEnemies)
        {
            if (enemy == null || !currentlyDetected.Contains(enemy))
            {
                enemiesToRemove.Add(enemy);
            }
        }

        foreach (Transform enemy in enemiesToRemove)
        {
            detectedEnemies.Remove(enemy);

            if (enemy != null)
            {
                EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
                if (enemyAI != null)
                {
                    enemyAI.LoseTarget();
                }

                Debug.Log($"Enemy lost: {enemy.name}");
            }
        }
    }

    // Остальные методы остаются такими же...
    void GenerateHemisphereDirections() 
    {
        hemisphereDirections.Clear();

        for (int lat = 0; lat <= latitudeSegments; lat++)
        {
            float phi = Mathf.PI * 0.5f * lat / latitudeSegments;

            for (int lon = 0; lon <= longitudeSegments; lon++)
            {
                float theta = 2f * Mathf.PI * lon / longitudeSegments;

                float x = Mathf.Sin(phi) * Mathf.Cos(theta);
                float y = Mathf.Cos(phi);
                float z = Mathf.Sin(phi) * Mathf.Sin(theta);

                hemisphereDirections.Add(new Vector3(x, y, z).normalized);
            }
        }
    }
    void InitializeRadii()
    {
        adaptedRadii.Clear();
        targetRadii.Clear();

        for (int i = 0; i < hemisphereDirections.Count; i++)
        {
            adaptedRadii.Add(maxRadius);
            targetRadii.Add(maxRadius);
        }
    }
    void AdaptToEnvironment()
    {
        hitInfos.Clear();
        Vector3 origin = player.position;

        for (int i = 0; i < hemisphereDirections.Count; i++)
        {
            Vector3 direction = hemisphereDirections[i];
            RaycastHit hitInfo;

            // Проверяем на препятствия
            bool hit = Physics.Raycast(origin, direction, out hitInfo, maxRadius, obstacleLayer);
            hitInfos.Add(hitInfo);

            if (hit)
            {
                // Если есть препятствие, сжимаем радиус
                float obstacleDistance = hitInfo.distance - playerBuffer;
                float newRadius = Mathf.Max(obstacleDistance * shrinkMultiplier, minRadius);
                targetRadii[i] = newRadius;
            }
            else
            {
                // Если препятствий нет, возвращаем к максимальному радиусу
                targetRadii[i] = maxRadius;
            }
        }

        SmoothNeighborInfluence();
}
    void SmoothNeighborInfluence()
    {
    List<float> smoothedRadii = new List<float>(targetRadii);

    for (int i = 0; i < targetRadii.Count; i++)
    {
        float sum = targetRadii[i];
        int count = 1;

        for (int j = 0; j < targetRadii.Count; j++)
        {
            if (i != j)
            {
                Vector3 dir1 = hemisphereDirections[i];
                Vector3 dir2 = hemisphereDirections[j];

                if (Vector3.Dot(dir1, dir2) > 0.8f)
                {
                    sum += targetRadii[j];
                    count++;
                }
            }

        smoothedRadii[i] = Mathf.Min(sum / count, targetRadii[i]);
    }

    targetRadii = smoothedRadii;
}
    }
    void SmoothRadiusTransition()
    {
        for (int i = 0; i < adaptedRadii.Count; i++)
        {
            adaptedRadii[i] = Mathf.Lerp(adaptedRadii[i], targetRadii[i],
                                       adaptationSpeed * Time.deltaTime);
        }
    }

    public bool IsEnemyVisible(Transform enemy)
    {
        Vector3 directionToEnemy = (enemy.position - player.position).normalized;
        float distanceToEnemy = Vector3.Distance(player.position, enemy.position);

        float maxAllowedDistance = GetMaxDistanceInDirection(directionToEnemy);

        if (distanceToEnemy > maxAllowedDistance)
            return false;

        RaycastHit hit;
        if (Physics.Raycast(player.position, directionToEnemy, out hit, distanceToEnemy, obstacleLayer))
        {
            return false;
        }

        return true;
    }

    public float GetMaxDistanceInDirection(Vector3 direction)
    {
        Vector3 normalizedDir = direction.normalized;

        int closestRayIndex = 0;
        float maxDot = -1f;

        for (int i = 0; i < hemisphereDirections.Count; i++)
        {
            float dot = Vector3.Dot(normalizedDir, hemisphereDirections[i]);
            if (dot > maxDot)
            {
                maxDot = dot;
                closestRayIndex = i;
            }
        }

        return adaptedRadii[closestRayIndex];
    }

    void OnDrawGizmos()
    {
        if (hemisphereDirections.Count == 0)
        {
            GenerateHemisphereDirections();
            InitializeRadii();
        }

        Vector3 origin = player != null ? player.position : transform.position;

        // Рисуем адаптированные лучи
        for (int i = 0; i < hemisphereDirections.Count; i++)
        {
            if (i >= adaptedRadii.Count) continue;

            Vector3 direction = hemisphereDirections[i];
            float currentRadius = adaptedRadii[i];
            bool hasObstacle = i < hitInfos.Count && hitInfos[i].collider != null;

            if (showRays)
            {
                if (hasObstacle)
                {
                    Gizmos.color = Color.red;
                    if (hitInfos[i].collider != null)
                    {
                        Gizmos.DrawLine(origin, hitInfos[i].point);
                        Gizmos.DrawSphere(hitInfos[i].point, 0.05f);
                    }
                }
                else if (currentRadius < maxRadius * 0.9f)
                {
                    Gizmos.color = Color.yellow;
                }
                else
                {
                    Gizmos.color = Color.green;
                }

                Vector3 endPoint = origin + direction * currentRadius;
                Gizmos.DrawLine(origin, endPoint);
                Gizmos.DrawSphere(endPoint, 0.02f);

                // Рисуем сферы обнаружения на концах лучей
                Gizmos.color = new Color(1, 1, 0, 0.2f);
                Gizmos.DrawSphere(endPoint, detectionCheckRadius);
            }
        }

        // Рисуем соединения с обнаруженными врагами
        if (showEnemyConnections)
        {
            Gizmos.color = Color.magenta;
            foreach (Transform enemy in detectedEnemies)
            {
                if (enemy != null)
                {
                    Gizmos.DrawLine(origin, enemy.position);
                    Gizmos.DrawSphere(enemy.position, 0.1f);
                }
            }
        }

        // Центр
        Gizmos.color = Color.white;
        Gizmos.DrawSphere(origin, 0.1f);
    }

    // УДАЛИ ВСЕ МЕТОДЫ OnTriggerEnter, OnTriggerStay, OnTriggerExit
    // Они больше не нужны, так как мы не используем обычные коллайдеры
}