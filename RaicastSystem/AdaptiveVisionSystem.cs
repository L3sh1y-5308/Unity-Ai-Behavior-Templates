using System.Collections.Generic;
using UnityEngine;

public class AdaptiveVisionSystem : MonoBehaviour
{
    [Header("Eye Offset")]
    public float eyeHeight = 1.7f; // Поднял до уровня глаз человека

    [Header("Vision Settings")]
    [Tooltip("Горизонтальный угол обзора (360° = полная окружность)")]
    [Range(0f, 360f)] public float horizontalAngle = 120f;
    [Range(0f, 180f)] public float verticalAngleUp = 45f;
    [Range(0f, 180f)] public float verticalAngleDown = 30f;
    [Tooltip("Смещение направления взгляда по горизонтали (0 = вперёд)")]
    [Range(-180f, 180f)] public float horizontalOffset = 0f;

    [Header("Detection Range")]
    public float maxDetectionRange = 15f;
    public float minDetectionRange = 0.5f;
    [Range(0.1f, 3f)] public float forwardRangeScale = 1.2f;
    [Range(0.1f, 3f)] public float backwardRangeScale = 0.2f;
    [Range(0.1f, 3f)] public float upRangeScale = 0.8f;
    [Range(0.1f, 3f)] public float downRangeScale = 0.6f;
    [Range(0.1f, 3f)] public float sideRangeScale = 0.7f;

    [Header("Resolution")]
    public int latitudeSegments = 8;
    public int longitudeSegments = 16;

    [Header("Adaptation (Obstacles)")]
    public LayerMask obstacleLayer = 1 << 0; // Обычно Default (слой земли и стен)
    public float adaptationSpeed = 5f;
    public float shrinkMultiplier = 0.8f;
    public bool smoothAdaptation = true;
    public float visionBuffer = 0.2f;

    [Header("Target Memory (Anti-Flicker)")]
    [Tooltip("Сколько секунд NPC помнит игрока после того, как он скрылся из виду")]
    public float playerMemoryTime = 2.0f;
    private float playerMemoryTimer = 0f;

    [Header("Player Detection")]
    public LayerMask playerLayer = 1 << 6;
    public Transform detectedPlayer = null;
    public float playerCheckRadius = 0.8f;
    public bool isPlayerVisible { get; private set; } = false;

    [Header("Food Detection")]
    public string foodTag = "Food";
    public Transform detectedFood = null;
    public float foodCheckRadius = 0.5f;
    public bool isFoodVisible { get; private set; } = false;

    [Header("Danger Detection")]
    public string dangerTag = "Danger";
    public Transform detectedDanger = null;
    public float dangerCheckRadius = 0.5f;
    public bool isDangerVisible { get; private set; } = false;

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
    private List<int>[] rayNeighbors;
    private Transform cachedTransform;

    void Awake()
    {
        cachedTransform = transform;
    }

    void Start()
    {
        GenerateDirections();
        InitializeRanges();
        PrecalculateNeighbors();
    }

    void Update()
    {
        AdaptToEnvironment();

        if (smoothAdaptation)
            SmoothRangeTransition();

        CheckPlayerInVisionZone();
        CheckFoodInVisionZone();
        CheckDangerInVisionZone();
    }

    // --- BT INTEGRATION ---

    public bool CanSeePlayer()
    {
        // Теперь возвращает true, пока не истек таймер памяти
        return isPlayerVisible && detectedPlayer != null;
    }

    public Transform GetDetectedPlayer()
    {
        return isPlayerVisible ? detectedPlayer : null;
    }

    // --- VISION LOGIC ---

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
            float elevationRad = Mathf.Lerp(phiMax, phiMin, t) * Mathf.Deg2Rad;

            for (int lon = 0; lon <= longitudeSegments; lon++)
            {
                float s = (float)lon / longitudeSegments;
                float azimuthRad = Mathf.Lerp(thetaMin, thetaMax, s) * Mathf.Deg2Rad;

                float y = Mathf.Sin(elevationRad);
                float xz = Mathf.Cos(elevationRad);
                float x = xz * Mathf.Sin(azimuthRad);
                float z = xz * Mathf.Cos(azimuthRad);

                Vector3 localDir = new Vector3(x, y, z).normalized;
                directions.Add(localDir);
                directionMaxRanges.Add(CalculateDirectionalRange(localDir));
            }
        }
    }

    float CalculateDirectionalRange(Vector3 localDir)
    {
        float fwd = Mathf.Max(0f, localDir.z);
        float bck = Mathf.Max(0f, -localDir.z);
        float up = Mathf.Max(0f, localDir.y);
        float dwn = Mathf.Max(0f, -localDir.y);
        float side = Mathf.Abs(localDir.x);

        float total = fwd + bck + up + dwn + side;
        if (total < 0.001f) return maxDetectionRange;

        float scale = (fwd * forwardRangeScale + bck * backwardRangeScale + up * upRangeScale + dwn * downRangeScale + side * sideRangeScale) / total;
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

    void PrecalculateNeighbors()
    {
        rayNeighbors = new List<int>[directions.Count];
        for (int i = 0; i < directions.Count; i++)
        {
            rayNeighbors[i] = new List<int>();
            for (int j = 0; j < directions.Count; j++)
            {
                if (i != j && Vector3.Dot(directions[i], directions[j]) > 0.8f)
                    rayNeighbors[i].Add(j);
            }
        }
    }

    void AdaptToEnvironment()
    {
        hitInfos.Clear();
        Vector3 origin = cachedTransform.position + Vector3.up * eyeHeight;

        for (int i = 0; i < directions.Count; i++)
        {
            Vector3 worldDir = cachedTransform.TransformDirection(directions[i]);
            RaycastHit hitInfo;
            bool hit = Physics.Raycast(origin, worldDir, out hitInfo, directionMaxRanges[i], obstacleLayer);
            hitInfos.Add(hitInfo);

            if (hit)
                targetRanges[i] = Mathf.Max((hitInfo.distance - visionBuffer) * shrinkMultiplier, minDetectionRange);
            else
                targetRanges[i] = directionMaxRanges[i];
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
            foreach (int j in rayNeighbors[i]) { sum += targetRanges[j]; count++; }
            smoothedRanges[i] = Mathf.Min(sum / count, targetRanges[i]);
        }
        targetRanges = smoothedRanges;
    }

    void SmoothRangeTransition()
    {
        for (int i = 0; i < adaptedRanges.Count; i++)
            adaptedRanges[i] = Mathf.Lerp(adaptedRanges[i], targetRanges[i], adaptationSpeed * Time.deltaTime);
    }

    // --- DETECTION CORE ---

    void CheckPlayerInVisionZone()
    {
        Vector3 origin = cachedTransform.position + Vector3.up * eyeHeight;
        Transform foundInThisFrame = null;

        // 1. Проверяем попадание игрока в лучи
        for (int i = 0; i < directions.Count; i++)
        {
            Vector3 worldDir = cachedTransform.TransformDirection(directions[i]);
            float range = adaptedRanges[i];

            // Если луч слишком короткий (уткнулся в стену), через него не смотрим
            if (range < directionMaxRanges[i] * 0.1f) continue;

            Vector3 checkPoint = origin + worldDir * range;
            Collider[] nearbyPlayers = Physics.OverlapSphere(checkPoint, playerCheckRadius, playerLayer);

            foreach (Collider pc in nearbyPlayers)
            {
                if (IsTargetVisible(pc.transform))
                {
                    foundInThisFrame = pc.transform;
                    break;
                }
            }
            if (foundInThisFrame != null) break;
        }

        // 2. Логика памяти и состояний
        if (foundInThisFrame != null)
        {
            bool wasVisible = isPlayerVisible;
            detectedPlayer = foundInThisFrame;
            isPlayerVisible = true;
            playerMemoryTimer = playerMemoryTime; // Сброс таймера памяти

            if (!wasVisible) OnPlayerDetected();
        }
        else
        {
            // Если в этом кадре не видим, но цель была — проверяем видимость цели напрямую
            if (detectedPlayer != null)
            {
                if (IsTargetVisible(detectedPlayer))
                {
                    playerMemoryTimer = playerMemoryTime; // Еще видим напрямую
                }
                else
                {
                    playerMemoryTimer -= Time.deltaTime; // Теряем из виду, тикает таймер
                }

                if (playerMemoryTimer <= 0)
                {
                    if (isPlayerVisible)
                    {
                        isPlayerVisible = false;
                        OnPlayerLost();
                    }
                    detectedPlayer = null;
                }
            }
            else
            {
                isPlayerVisible = false;
            }
        }
    }

    public bool IsTargetVisible(Transform target)
    {
        if (target == null) return false;

        Vector3 origin = cachedTransform.position + Vector3.up * eyeHeight;
        Vector3 targetCenter = target.position + Vector3.up * 1.0f; // Луч в грудь/голову
        Vector3 dirToTarget = (targetCenter - origin).normalized;
        float distToTarget = Vector3.Distance(origin, targetCenter);

        // Проверка углов
        Vector3 localDir = cachedTransform.InverseTransformDirection(dirToTarget);
        float elevation = Mathf.Asin(Mathf.Clamp(localDir.y, -1f, 1f)) * Mathf.Rad2Deg;
        float azimuth = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;

        if (elevation > verticalAngleUp || elevation < -verticalAngleDown) return false;
        if (Mathf.Abs(Mathf.DeltaAngle(horizontalOffset, azimuth)) > horizontalAngle * 0.5f) return false;

        // Проверка препятствий (Raycast)
        RaycastHit hit;
        if (Physics.Raycast(origin, dirToTarget, out hit, distToTarget, obstacleLayer | playerLayer))
        {
            // Если попали в игрока или его дочерний объект — видим
            if (hit.transform == target || hit.transform.IsChildOf(target)) return true;
            return false;
        }
        return true;
    }

    // --- FOOD & DANGER (Simple) ---

    void CheckFoodInVisionZone()
    {
        Vector3 origin = cachedTransform.position + Vector3.up * eyeHeight;
        Collider[] hitColliders = Physics.OverlapSphere(origin, maxDetectionRange, -1);
        isFoodVisible = false;
        foreach (Collider col in hitColliders)
        {
            if (col.CompareTag(foodTag) && IsTargetVisible(col.transform))
            {
                detectedFood = col.transform;
                isFoodVisible = true;
                break;
            }
        }
    }

    void CheckDangerInVisionZone()
    {
        Vector3 origin = cachedTransform.position + Vector3.up * eyeHeight;
        Collider[] hitColliders = Physics.OverlapSphere(origin, maxDetectionRange, -1);
        isDangerVisible = false;
        foreach (Collider col in hitColliders)
        {
            if (col.CompareTag(dangerTag) && IsTargetVisible(col.transform))
            {
                detectedDanger = col.transform;
                isDangerVisible = true;
                break;
            }
        }
    }

    // --- EVENTS ---

    public virtual void OnPlayerDetected() { Debug.Log($"<color=green>[{gameObject.name}] Вижу игрока!</color>"); }
    protected virtual void OnPlayerLost() { Debug.Log($"<color=red>[{gameObject.name}] Потерял игрока!</color>"); }

    // --- DEBUG ---

    void OnDrawGizmos()
    {
        if (!Application.isPlaying && (directions == null || directions.Count == 0)) { GenerateDirections(); InitializeRanges(); }
        Vector3 origin = transform.position + Vector3.up * eyeHeight;
        if (showRays)
        {
            for (int i = 0; i < directions.Count; i++)
            {
                Gizmos.color = (hitInfos != null && i < hitInfos.Count && hitInfos[i].collider != null) ? obstructedRayColor : normalRayColor;
                Gizmos.DrawLine(origin, origin + transform.rotation * directions[i] * adaptedRanges[i]);
            }
        }
        if (showPlayerConnection && isPlayerVisible && detectedPlayer != null)
        {
            Gizmos.color = playerConnectionColor;
            Gizmos.DrawLine(origin, detectedPlayer.position);
        }
    }
}