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
    public LayerMask obstacleLayer = -1; // Слои препятствий (стены, потолки и т.д.)
    public float adaptationSpeed = 5f;
    public float shrinkMultiplier = 0.8f;
    public bool smoothAdaptation = true;

    [Header("Player Protection")]
    public Transform player;
    public float playerBuffer = 0.2f;

    [Header("Enemy Detection")]
    public LayerMask enemyLayer = 1 << 8; // Слой врагов
    public List<Transform> detectedEnemies = new List<Transform>();

    [Header("Debug Visualization")]
    public bool showRays = true;
    public bool showAdaptedShape = true;
    public bool showEnemyConnections = true;

    private List<Vector3> hemisphereDirections = new List<Vector3>();
    private List<float> adaptedRadii = new List<float>();
    private List<float> targetRadii = new List<float>();
    private List<RaycastHit> hitInfos = new List<RaycastHit>();
    private SphereCollider triggerCollider;

    void Start()
    {
        GenerateHemisphereDirections();
        InitializeRadii();

        if (player == null)
            player = transform;

        // Создаем или настраиваем триггер-коллайдер
        //SetupTriggerCollider();
    }

    void Update()
    {
        AdaptToEnvironment();
        if (smoothAdaptation)
            SmoothRadiusTransition();

        // Проверяем видимость обнаруженных врагов
        ValidateEnemyVisibility();
    }
    /*
    void SetupTriggerCollider()
    {
        triggerCollider = GetComponent<SphereCollider>();
        if (triggerCollider == null)
        {
            triggerCollider = gameObject.AddComponent<SphereCollider>();
        }

        triggerCollider.isTrigger = true;
        triggerCollider.radius = maxRadius;
    }
    */
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
            }

            smoothedRadii[i] = Mathf.Min(sum / count, targetRadii[i]);
        }

        targetRadii = smoothedRadii;
    }

    void SmoothRadiusTransition()
    {
        for (int i = 0; i < adaptedRadii.Count; i++)
        {
            adaptedRadii[i] = Mathf.Lerp(adaptedRadii[i], targetRadii[i],
                                       adaptationSpeed * Time.deltaTime);
        }
    }

    // Проверка видимости врага (не заблокирован ли стеной)
    public bool IsEnemyVisible(Transform enemy)
    {
        Vector3 directionToEnemy = (enemy.position - player.position).normalized;
        float distanceToEnemy = Vector3.Distance(player.position, enemy.position);

        // Проверяем, находится ли враг в пределах адаптированной формы
        float maxAllowedDistance = GetMaxDistanceInDirection(directionToEnemy);

        if (distanceToEnemy > maxAllowedDistance)
            return false;

        // Дополнительная проверка прямой видимости
        RaycastHit hit;
        if (Physics.Raycast(player.position, directionToEnemy, out hit, distanceToEnemy, obstacleLayer))
        {
            return false; // Есть препятствие между игроком и врагом
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

    void ValidateEnemyVisibility()
    {
        // Удаляем врагов, которые больше не видны
        detectedEnemies.RemoveAll(enemy => enemy == null || !IsEnemyVisible(enemy));
    }

    // События триггера
    void OnTriggerEnter(Collider other)
    {
        // Проверяем, является ли объект врагом
        if (((1 << other.gameObject.layer) & enemyLayer) != 0)
        {
            Transform enemy = other.transform;

            // Проверяем видимость врага
            if (IsEnemyVisible(enemy) && !detectedEnemies.Contains(enemy))
            {
                detectedEnemies.Add(enemy);

                // Уведомляем врага о обнаружении игрока
                EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
                if (enemyAI != null)
                {
                    enemyAI.SetTarget(player);
                }

                Debug.Log($"Enemy detected and can see player: {enemy.name}");
            }
        }
    }

    void OnTriggerStay(Collider other)
    {
        // Постоянно проверяем видимость врагов в триггере
        if (((1 << other.gameObject.layer) & enemyLayer) != 0)
        {
            Transform enemy = other.transform;

            if (IsEnemyVisible(enemy))
            {
                if (!detectedEnemies.Contains(enemy))
                {
                    detectedEnemies.Add(enemy);

                    EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
                    if (enemyAI != null)
                    {
                        enemyAI.SetTarget(player);
                    }
                }
            }
            else
            {
                // Враг больше не видим - убираем из списка
                if (detectedEnemies.Contains(enemy))
                {
                    detectedEnemies.Remove(enemy);

                    EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
                    if (enemyAI != null)
                    {
                        enemyAI.LoseTarget();
                    }

                    Debug.Log($"Enemy lost sight of player: {enemy.name}");
                }
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (((1 << other.gameObject.layer) & enemyLayer) != 0)
        {
            Transform enemy = other.transform;

            if (detectedEnemies.Contains(enemy))
            {
                detectedEnemies.Remove(enemy);

                EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
                if (enemyAI != null)
                {
                    enemyAI.LoseTarget();
                }

                Debug.Log($"Enemy left detection zone: {enemy.name}");
            }
        }
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
                /*
                Gizmos.DrawLine(origin, endPoint);
                Gizmos.DrawSphere(endPoint, 0.02f);
            */
                }
        }

        // Рисуем адаптированную форму коллайдера
        if (showAdaptedShape)
        {
            Gizmos.color = new Color(0, 1, 1, 0.3f);
            DrawAdaptedHemisphere(origin);
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

    void DrawAdaptedHemisphere(Vector3 center)
    {
        for (int i = 0; i < hemisphereDirections.Count - 1; i++)
        {
            if (i >= adaptedRadii.Count) continue;

            Vector3 point1 = center + hemisphereDirections[i] * adaptedRadii[i];

            for (int j = i + 1; j < hemisphereDirections.Count; j++)
            {
                if (j >= adaptedRadii.Count) continue;

                Vector3 dir1 = hemisphereDirections[i];
                Vector3 dir2 = hemisphereDirections[j];

                if (Vector3.Dot(dir1, dir2) > 0.85f)
                {
                    Vector3 point2 = center + hemisphereDirections[j] * adaptedRadii[j];
                    Gizmos.DrawLine(point1, point2);
                }
            }
        }
    }

    // Публичные методы для других скриптов
    public List<Transform> GetDetectedEnemies()
    {
        return new List<Transform>(detectedEnemies);
    }

    public bool IsEnemyDetected(Transform enemy)
    {
        return detectedEnemies.Contains(enemy);
    }
}