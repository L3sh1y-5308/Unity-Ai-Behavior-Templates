using System.Collections.Generic;
using UnityEngine;

public class AdaptiveVisionSystem : MonoBehaviour
{
    // Настройки углов обзора
    [Header("Vision Settings")]
    [Tooltip("Горизонтальный угол обзора (360° = полная окружность)")]
    [Range(0f, 360f)] public float horizontalAngle = 120f;
    [Range(0f, 180f)] public float verticalAngleUp = 45f;
    [Range(0f, 180f)] public float verticalAngleDown = 30f;
    [Tooltip("Смещение направления взгляда по горизонтали (0 = вперёд)")]
    [Range(-180f, 180f)] public float horizontalOffset = 0f;

    [Header("Detection Range")]
    public float maxDetectionRange = 10f;
    public float minDetectionRange = 0.5f;
    [Tooltip("Множитель дальности обзора вперёд")]
    [Range(0.1f, 3f)] public float forwardRangeScale = 1f;
    [Tooltip("Множитель дальности обзора назад")]
    [Range(0.1f, 3f)] public float backwardRangeScale = 0.3f;
    [Tooltip("Множитель дальности обзора вверх")]
    [Range(0.1f, 3f)] public float upRangeScale = 0.8f;
    [Tooltip("Множитель дальности обзора вниз")]
    [Range(0.1f, 3f)] public float downRangeScale = 0.6f;
    [Tooltip("Множитель дальности обзора по сторонам")]
    [Range(0.1f, 3f)] public float sideRangeScale = 0.7f;

    [Header("Resolution")]
    public int latitudeSegments = 8;
    public int longitudeSegments = 16;

    [Header("Adaptation Settings")]
    public LayerMask obstacleLayer = -1;
    public float adaptationSpeed = 5f;
    public float shrinkMultiplier = 0.8f;
    public bool smoothAdaptation = true;
    public float visionBuffer = 0.2f;

    [Header("Player Detection")]
    public LayerMask playerLayer = 1 << 6;
    public Transform detectedPlayer = null;
    public float playerCheckRadius = 0.5f;
    public bool isPlayerVisible { get; private set; } = false;

    [Header("Debug Visualization")]
    public bool showRays = true;
    public bool showPlayerConnection = true;
    public Color normalRayColor = Color.green;
    public Color obstructedRayColor = Color.red;
    public Color adaptedRayColor = Color.yellow;
    public Color playerConnectionColor = Color.magenta;

    private List<Vector3> directions = new List<Vector3>();
    private List<float> directionMaxRanges = new List<float>();
    private List<float> adaptedRanges = new List<float>();
    private List<float> targetRanges = new List<float>();
    private List<RaycastHit> hitInfos = new List<RaycastHit>();

    private Transform cachedTransform;

    void Awake()
    {
        cachedTransform = transform;
    }

    void Start()
    {
        GenerateDirections();
        InitializeRanges();
    }

    void Update()
    {
        AdaptToEnvironment();
        
        if (smoothAdaptation)
            SmoothRangeTransition();

        CheckPlayerInVisionZone();
    }

    /// <summary>
    /// Генерирует направления лучей для системы обзора
    /// </summary>
    void GenerateDirections()
    {
        directions.Clear();
        directionMaxRanges.Clear();

        float phiMin = -verticalAngleDown;
        float phiMax = verticalAngleUp;

        float thetaMin = horizontalOffset - horizontalAngle * 0.5f;
        float thetaMax = horizontalOffset + horizontalAngle * 0.5f;

        for (int lat = 0; lat <= latitudeSegments; lat++)
        {
            float t = (float)lat / latitudeSegments;
            float elevationDeg = Mathf.Lerp(phiMax, phiMin, t);
            float elevationRad = elevationDeg * Mathf.Deg2Rad;

            for (int lon = 0; lon <= longitudeSegments; lon++)
            {
                float s = (float)lon / longitudeSegments;
                float azimuthDeg = Mathf.Lerp(thetaMin, thetaMax, s);
                float azimuthRad = azimuthDeg * Mathf.Deg2Rad;

                float y = Mathf.Sin(elevationRad);
                float xz = Mathf.Cos(elevationRad);
                float x = xz * Mathf.Sin(azimuthRad);
                float z = xz * Mathf.Cos(azimuthRad);

                Vector3 localDir = new Vector3(x, y, z).normalized;
                directions.Add(localDir);

                float dirMax = CalculateDirectionalRange(localDir);
                directionMaxRanges.Add(dirMax);
            }
        }
    }

    /// <summary>
    /// Вычисляет максимальную дальность обзора в заданном направлении
    /// </summary>
    float CalculateDirectionalRange(Vector3 localDir)
    {
        float fwd = Mathf.Max(0f, localDir.z);
        float bck = Mathf.Max(0f, -localDir.z);
        float up = Mathf.Max(0f, localDir.y);
        float dwn = Mathf.Max(0f, -localDir.y);
        float side = Mathf.Abs(localDir.x);

        float total = fwd + bck + up + dwn + side;
        if (total < 0.001f) return maxDetectionRange;

        float scale = (fwd * forwardRangeScale
                     + bck * backwardRangeScale
                     + up * upRangeScale
                     + dwn * downRangeScale
                     + side * sideRangeScale) / total;

        return maxDetectionRange * scale;
    }

    void InitializeRanges()
    {
        adaptedRanges.Clear();
        targetRanges.Clear();

        for (int i = 0; i < directions.Count; i++)
        {
            adaptedRanges.Add(directionMaxRanges[i]);
            targetRanges.Add(directionMaxRanges[i]);
        }
    }

    /// <summary>
    /// Перегенерирует систему обзора (вызывается при изменении параметров)
    /// </summary>
    public void RebuildVisionSystem()
    {
        GenerateDirections();
        InitializeRanges();
    }

    /// <summary>
    /// Адаптирует зону обзора к окружающим препятствиям
    /// </summary>
    void AdaptToEnvironment()
    {
        hitInfos.Clear();
        Vector3 origin = cachedTransform.position;

        for (int i = 0; i < directions.Count; i++)
        {
            Vector3 worldDir = cachedTransform.TransformDirection(directions[i]);
            float dirMax = directionMaxRanges[i];

            RaycastHit hitInfo;
            bool hit = Physics.Raycast(origin, worldDir, out hitInfo, dirMax, obstacleLayer);
            hitInfos.Add(hitInfo);

            if (hit)
            {
                float obstacleDistance = hitInfo.distance - visionBuffer;
                float newRange = Mathf.Max(obstacleDistance * shrinkMultiplier, minDetectionRange);
                targetRanges[i] = newRange;
            }
            else
            {
                targetRanges[i] = dirMax;
            }
        }

        SmoothNeighborInfluence();
    }

    void SmoothNeighborInfluence()
    {
        List<float> smoothedRanges = new List<float>(targetRanges);

        for (int i = 0; i < targetRanges.Count; i++)
        {
            float sum = targetRanges[i];
            int count = 1;

            for (int j = 0; j < targetRanges.Count; j++)
            {
                if (i != j && Vector3.Dot(directions[i], directions[j]) > 0.8f)
                {
                    sum += targetRanges[j];
                    count++;
                }
            }

            smoothedRanges[i] = Mathf.Min(sum / count, targetRanges[i]);
        }

        targetRanges = smoothedRanges;
    }

    void SmoothRangeTransition()
    {
        for (int i = 0; i < adaptedRanges.Count; i++)
        {
            adaptedRanges[i] = Mathf.Lerp(adaptedRanges[i], targetRanges[i],
                                          adaptationSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// Проверяет наличие игрока в зоне обзора
    /// </summary>
    void CheckPlayerInVisionZone()
    {
        Vector3 origin = cachedTransform.position;
        Transform foundPlayer = null;

        // Проверяем каждый луч на наличие игрока
        for (int i = 0; i < directions.Count; i++)
        {
            if (i >= adaptedRanges.Count) continue;

            Vector3 worldDir = cachedTransform.TransformDirection(directions[i]);
            float range = adaptedRanges[i];

            // Пропускаем сильно заблокированные лучи
            if (range < directionMaxRanges[i] * 0.5f)
                continue;

            // Проверяем отсутствие препятствия на пути луча
            if (Physics.Raycast(origin, worldDir, range, obstacleLayer))
                continue;

            Vector3 checkPoint = origin + worldDir * range;
            Collider[] nearbyPlayers = Physics.OverlapSphere(checkPoint, playerCheckRadius, playerLayer);

            foreach (Collider playerCollider in nearbyPlayers)
            {
                Transform player = playerCollider.transform;
                if (IsPlayerVisible(player))
                {
                    foundPlayer = player;
                    break;
                }
            }

            if (foundPlayer != null)
                break;
        }

        // Обновляем состояние обнаружения игрока
        bool wasVisible = isPlayerVisible;
        
        if (foundPlayer != null)
        {
            detectedPlayer = foundPlayer;
            isPlayerVisible = true;

            if (!wasVisible)
                OnPlayerDetected();
        }
        else
        {
            // Если игрок был обнаружен, проверяем всё ещё ли он виден
            if (detectedPlayer != null)
            {
                if (!IsPlayerVisible(detectedPlayer))
                {
                    isPlayerVisible = false;
                    OnPlayerLost();
                }
            }
            else
            {
                isPlayerVisible = false;
            }
        }
    }

    /// <summary>
    /// Проверяет, виден ли игрок из позиции врага
    /// </summary>
    public bool IsPlayerVisible(Transform player)
    {
        if (player == null)
            return false;

        Vector3 origin = cachedTransform.position;
        Vector3 directionToPlayer = (player.position - origin).normalized;
        float distanceToPlayer = Vector3.Distance(origin, player.position);

        // Проверка попадания в углы обзора
        Vector3 localDir = cachedTransform.InverseTransformDirection(directionToPlayer);
        float elevationDeg = Mathf.Asin(Mathf.Clamp(localDir.y, -1f, 1f)) * Mathf.Rad2Deg;
        float azimuthDeg = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;

        // Проверка вертикального угла
        if (elevationDeg > verticalAngleUp || elevationDeg < -verticalAngleDown)
            return false;

        // Проверка горизонтального угла
        float halfH = horizontalAngle * 0.5f;
        float relAzimuth = Mathf.DeltaAngle(horizontalOffset, azimuthDeg);
        if (relAzimuth < -halfH || relAzimuth > halfH)
            return false;

        // Проверка максимальной дистанции с учетом адаптивного радиуса
        float maxAllowedDistance = GetMaxRangeInDirection(directionToPlayer);
        if (distanceToPlayer > maxAllowedDistance)
            return false;

        // Проверка препятствий между врагом и игроком
        RaycastHit hit;
        Vector3 rayStart = origin + Vector3.up * 0.5f;
        Vector3 playerCenter = player.position + Vector3.up * 0.5f;
        Vector3 rayDirection = (playerCenter - rayStart).normalized;
        float rayDistance = Vector3.Distance(rayStart, playerCenter);

        if (Physics.Raycast(rayStart, rayDirection, out hit, rayDistance, obstacleLayer))
        {
            // Проверяем, не попали ли в самого игрока
            if (hit.collider.transform == player ||
                hit.collider.transform.IsChildOf(player) ||
                player.IsChildOf(hit.collider.transform))
            {
                return true;
            }

            // Попали в препятствие
            return false;
        }

        return true;
    }

    /// <summary>
    /// Возвращает максимальную дальность обзора в заданном направлении
    /// </summary>
    public float GetMaxRangeInDirection(Vector3 worldDirection)
    {
        Vector3 localDir = cachedTransform.InverseTransformDirection(worldDirection).normalized;

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

        return adaptedRanges[closestIndex];
    }

    /// <summary>
    /// Вызывается когда игрок обнаружен
    /// </summary>
    public virtual void OnPlayerDetected()
    {
        Debug.Log($"<color=green>[{gameObject.name}] Player detected!</color>");
        
        // Интеграция с EnemyAI
        EnemyAI enemyAI = GetComponent<EnemyAI>();
        if (enemyAI != null)
        {
            enemyAI.SetTarget(detectedPlayer);
        }
    }

    /// <summary>
    /// Вызывается когда игрок потерян из виду
    /// </summary>
    protected virtual void OnPlayerLost()
    {
        Debug.Log($"<color=yellow>[{gameObject.name}] Player lost!</color>");
        
        // Интеграция с EnemyAI
        EnemyAI enemyAI = GetComponent<EnemyAI>();
        if (enemyAI != null)
        {
            enemyAI.LoseTarget();
        }
    }

    void OnDrawGizmos()
    {
        if (directions.Count == 0)
        {
            GenerateDirections();
            InitializeRanges();
        }

        Vector3 origin = cachedTransform != null ? cachedTransform.position : transform.position;
        Quaternion rot = cachedTransform != null ? cachedTransform.rotation : transform.rotation;

        // Рисуем лучи обзора
        for (int i = 0; i < directions.Count; i++)
        {
            if (i >= adaptedRanges.Count) continue;

            Vector3 worldDir = rot * directions[i];
            float currentRange = adaptedRanges[i];
            bool hasObstacle = i < hitInfos.Count && hitInfos[i].collider != null;

            if (showRays)
            {
                if (hasObstacle)
                {
                    Gizmos.color = obstructedRayColor;
                    if (hitInfos[i].collider != null)
                    {
                        Gizmos.DrawLine(origin, hitInfos[i].point);
                        Gizmos.DrawSphere(hitInfos[i].point, 0.05f);
                    }
                    continue;
                }

                if (currentRange < directionMaxRanges[i] * 0.9f)
                    Gizmos.color = adaptedRayColor;
                else
                    Gizmos.color = normalRayColor;

                Vector3 endPoint = origin + worldDir * currentRange;
                Gizmos.DrawLine(origin, endPoint);
                Gizmos.DrawSphere(endPoint, 0.02f);

                Gizmos.color = new Color(1, 1, 0, 0.2f);
                Gizmos.DrawSphere(endPoint, playerCheckRadius);
            }
        }

        // Рисуем соединение с обнаруженным игроком
        if (showPlayerConnection && detectedPlayer != null && isPlayerVisible)
        {
            Gizmos.color = playerConnectionColor;
            Gizmos.DrawLine(origin, detectedPlayer.position);
            Gizmos.DrawSphere(detectedPlayer.position, 0.15f);
        }

        // Рисуем центр врага
        Gizmos.color = isPlayerVisible ? Color.red : Color.white;
        Gizmos.DrawSphere(origin, 0.1f);
    }
}