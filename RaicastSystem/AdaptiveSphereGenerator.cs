using System.Collections.Generic;
using UnityEngine;

public class AdaptiveSphereGenerator : MonoBehaviour
{
    [Header("Shape Settings")]
    [Tooltip("Полная сфера (360°) или частичная")]
    [Range(0f, 360f)] public float horizontalAngle = 360f;   // горизонтальный охват
    [Range(0f, 180f)] public float verticalAngleUp = 90f;     // вверх от горизонта
    [Range(0f, 180f)] public float verticalAngleDown = 90f;   // вниз от горизонта
    [Tooltip("Смещение центра обзора по горизонтали (0 = вперёд)")]
    [Range(-180f, 180f)] public float horizontalOffset = 0f;

    [Header("Radius Settings")]
    public float maxRadius = 5f;
    public float minRadius = 0.5f;
    [Tooltip("Множитель дальности вперёд")]
    [Range(0.1f, 3f)] public float forwardRadiusScale = 1f;
    [Tooltip("Множитель дальности назад")]
    [Range(0.1f, 3f)] public float backwardRadiusScale = 1f;
    [Tooltip("Множитель дальности вверх")]
    [Range(0.1f, 3f)] public float upRadiusScale = 1f;
    [Tooltip("Множитель дальности вниз")]
    [Range(0.1f, 3f)] public float downRadiusScale = 1f;
    [Tooltip("Множитель дальности влево/вправо")]
    [Range(0.1f, 3f)] public float sideRadiusScale = 1f;

    [Header("Resolution")]
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
    public float detectionCheckRadius = 0.5f;

    [Header("Debug Visualization")]
    public bool showRays = true;
    public bool showAdaptedShape = true;
    public bool showEnemyConnections = true;

    private List<Vector3> directions = new List<Vector3>();
    private List<float> directionMaxRadii = new List<float>();  // макс. радиус для каждого луча
    private List<float> adaptedRadii = new List<float>();
    private List<float> targetRadii = new List<float>();
    private List<RaycastHit> hitInfos = new List<RaycastHit>();

    /*
    void Start()
    {
        GenerateDirections();
        InitializeRadii();

        if (player == null)
            player = transform;
    }
    */
 

    void Update()
    {
        
        GenerateDirections();
        InitializeRadii();

        if (player == null)
            player = transform;
        
        AdaptToEnvironment();
        if (smoothAdaptation)
            SmoothRadiusTransition();

        CheckEnemiesInAdaptiveZone();
    }

    /// <summary>
    /// Генерирует лучи в пределах заданных углов обзора.
    /// Поддерживает от узкого конуса до полной сферы.
    /// </summary>
    void GenerateDirections()
    {
        directions.Clear();
        directionMaxRadii.Clear();

        // Вертикальный диапазон: от -verticalAngleDown до +verticalAngleUp
        // 0° = горизонт, +90° = зенит, -90° = надир
        float phiMin = -verticalAngleDown;
        float phiMax = verticalAngleUp;

        // Горизонтальный диапазон с учётом смещения
        float thetaMin = horizontalOffset - horizontalAngle * 0.5f;
        float thetaMax = horizontalOffset + horizontalAngle * 0.5f;

        for (int lat = 0; lat <= latitudeSegments; lat++)
        {
            // Интерполируем вертикальный угол
            float t = (float)lat / latitudeSegments;
            float elevationDeg = Mathf.Lerp(phiMax, phiMin, t);  // сверху вниз
            float elevationRad = elevationDeg * Mathf.Deg2Rad;

            for (int lon = 0; lon <= longitudeSegments; lon++)
            {
                float s = (float)lon / longitudeSegments;
                float azimuthDeg = Mathf.Lerp(thetaMin, thetaMax, s);
                float azimuthRad = azimuthDeg * Mathf.Deg2Rad;

                // Сферические → декартовы (Y — вверх)
                float y = Mathf.Sin(elevationRad);
                float xz = Mathf.Cos(elevationRad);
                float x = xz * Mathf.Sin(azimuthRad);
                float z = xz * Mathf.Cos(azimuthRad);

                Vector3 localDir = new Vector3(x, y, z).normalized;
                directions.Add(localDir);

                // Вычисляем максимальный радиус для этого направления
                float dirMax = CalculateDirectionalRadius(localDir);
                directionMaxRadii.Add(dirMax);
            }
        }
    }

    /// <summary>
    /// Вычисляет максимальный радиус в зав��симости от направления луча,
    /// плавно интерполируя между множителями сторон.
    /// </summary>
    float CalculateDirectionalRadius(Vector3 localDir)
    {
        // Разложим направление на компоненты и взвесим
        // forward = +Z, back = -Z, up = +Y, down = -Y, side = |X|
        float fwd = Mathf.Max(0f, localDir.z);     // вперёд
        float bck = Mathf.Max(0f, -localDir.z);    // назад
        float up = Mathf.Max(0f, localDir.y);     // вверх
        float dwn = Mathf.Max(0f, -localDir.y);    // вниз
        float side = Mathf.Abs(localDir.x);         // влево/вправо

        // Нормализуем веса
        float total = fwd + bck + up + dwn + side;
        if (total < 0.001f) return maxRadius;

        float scale = (fwd * forwardRadiusScale
                     + bck * backwardRadiusScale
                     + up * upRadiusScale
                     + dwn * downRadiusScale
                     + side * sideRadiusScale) / total;

        return maxRadius * scale;
    }

    void InitializeRadii()
    {
        adaptedRadii.Clear();
        targetRadii.Clear();

        for (int i = 0; i < directions.Count; i++)
        {
            adaptedRadii.Add(directionMaxRadii[i]);
            targetRadii.Add(directionMaxRadii[i]);
        }
    }

    /// <summary>
    /// Перегенерирует форму зоны. Вызывай из инспектора или при смене параметров.
    /// </summary>
    public void Rebuild()
    {
        GenerateDirections();
        InitializeRadii();
    }

    void AdaptToEnvironment()
    {
        hitInfos.Clear();
        Vector3 origin = player.position;

        for (int i = 0; i < directions.Count; i++)
        {
            // Преобразуем локальное направление в мировое (учитываем поворот игрока)
            Vector3 worldDir = player.TransformDirection(directions[i]);
            float dirMax = directionMaxRadii[i];

            RaycastHit hitInfo;
            bool hit = Physics.Raycast(origin, worldDir, out hitInfo, dirMax, obstacleLayer);
            hitInfos.Add(hitInfo);

            if (hit)
            {
                float obstacleDistance = hitInfo.distance - playerBuffer;
                float newRadius = Mathf.Max(obstacleDistance * shrinkMultiplier, minRadius);
                targetRadii[i] = newRadius;
            }
            else
            {
                targetRadii[i] = dirMax;
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
                if (i != j && Vector3.Dot(directions[i], directions[j]) > 0.8f)
                {
                    sum += targetRadii[j];
                    count++;
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

    void CheckEnemiesInAdaptiveZone()
    {
        Vector3 origin = player.position;
        List<Transform> currentlyDetected = new List<Transform>();

        for (int i = 0; i < directions.Count; i++)
        {
            if (i >= adaptedRadii.Count) continue;

            Vector3 worldDir = player.TransformDirection(directions[i]);
            float radius = adaptedRadii[i];
            Vector3 checkPoint = origin + worldDir * radius;

            Collider[] nearbyEnemies = Physics.OverlapSphere(checkPoint, detectionCheckRadius, enemyLayer);

            foreach (Collider enemyCollider in nearbyEnemies)
            {
                Transform enemy = enemyCollider.transform;

                if (IsEnemyVisible(enemy) && !currentlyDetected.Contains(enemy))
                {
                    currentlyDetected.Add(enemy);

                    if (!detectedEnemies.Contains(enemy))
                    {
                        detectedEnemies.Add(enemy);

                        EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
                        if (enemyAI != null)
                            enemyAI.SetTarget(player);

                        Debug.Log($"Enemy detected: {enemy.name}");
                    }
                }
            }
        }

        List<Transform> enemiesToRemove = new List<Transform>();
        foreach (Transform enemy in detectedEnemies)
        {
            if (enemy == null || !currentlyDetected.Contains(enemy))
                enemiesToRemove.Add(enemy);
        }

        foreach (Transform enemy in enemiesToRemove)
        {
            detectedEnemies.Remove(enemy);
            if (enemy != null)
            {
                EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
                if (enemyAI != null)
                    enemyAI.LoseTarget();
                Debug.Log($"Enemy lost: {enemy.name}");
            }
        }
    }

    public bool IsEnemyVisible(Transform enemy)
    {
        Vector3 directionToEnemy = (enemy.position - player.position).normalized;
        float distanceToEnemy = Vector3.Distance(player.position, enemy.position);

        // Проверяем, попадает ли враг в углы обзора
        Vector3 localDir = player.InverseTransformDirection(directionToEnemy);
        float elevationDeg = Mathf.Asin(Mathf.Clamp(localDir.y, -1f, 1f)) * Mathf.Rad2Deg;
        float azimuthDeg = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;

        if (elevationDeg > verticalAngleUp || elevationDeg < -verticalAngleDown)
            return false;

        float halfH = horizontalAngle * 0.5f;
        float relAzimuth = Mathf.DeltaAngle(horizontalOffset, azimuthDeg);
        if (relAzimuth < -halfH || relAzimuth > halfH)
            return false;

        float maxAllowedDistance = GetMaxDistanceInDirection(directionToEnemy);
        if (distanceToEnemy > maxAllowedDistance)
            return false;

        RaycastHit hit;
        if (Physics.Raycast(player.position, directionToEnemy, out hit, distanceToEnemy, obstacleLayer))
            return false;

        return true;
    }

    public float GetMaxDistanceInDirection(Vector3 worldDirection)
    {
        Vector3 localDir = player.InverseTransformDirection(worldDirection).normalized;

        int closestIndex = 0;
        float maxDot = -1f;

        for (int i = 0; i < directions.Count; i++)
        {
            float dot = Vector3.Dot(localDir, directions[i]);
            if (dot > maxDot)
            {
                maxDot = dot;
                closestIndex = i;
            }
        }

        return adaptedRadii[closestIndex];
    }

    void OnDrawGizmos()
    {
        if (directions.Count == 0)
        {
            GenerateDirections();
            InitializeRadii();
        }

        Vector3 origin = player != null ? player.position : transform.position;
        Quaternion rot = player != null ? player.rotation : transform.rotation;

        for (int i = 0; i < directions.Count; i++)
        {
            if (i >= adaptedRadii.Count) continue;

            Vector3 worldDir = rot * directions[i];
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
                    continue; // <--- НЕ рисуем дальше для этого луча
                }

                if (currentRadius < directionMaxRadii[i] * 0.9f)
                    Gizmos.color = Color.yellow;
                else
                    Gizmos.color = Color.green;

                Vector3 endPoint = origin + worldDir * currentRadius;
                Gizmos.DrawLine(origin, endPoint);
                Gizmos.DrawSphere(endPoint, 0.02f);

                Gizmos.color = new Color(1, 1, 0, 0.2f);
                Gizmos.DrawSphere(endPoint, detectionCheckRadius);
            }
        }

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

        Gizmos.color = Color.white;
        Gizmos.DrawSphere(origin, 0.1f);
    }
}