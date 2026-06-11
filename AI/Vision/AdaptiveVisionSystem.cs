using System.Collections.Generic;
using UnityEngine;

public class AdaptiveVisionSystem : MonoBehaviour
{
    [Header("Eye Offset")]
    public float eyeHeight = 1.7f;

    [Header("Vision Settings")]
    [Range(0f, 360f)] public float horizontalAngle = 120f;
    [Range(0f, 180f)] public float verticalAngleUp = 45f;
    [Range(0f, 180f)] public float verticalAngleDown = 30f;
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
    public LayerMask obstacleLayer = 1 << 0;
    public float adaptationSpeed = 5f;
    public float shrinkMultiplier = 0.8f;
    public bool smoothAdaptation = true;
    public float visionBuffer = 0.2f;

    [Header("Target Memory (Anti-Flicker)")]
    [Tooltip("Сколько секунд NPC помнит игрока после потери видимости")]
    public float playerMemoryTime = 2.0f;

    [Header("Close Range Override")]
    [Tooltip("На этой дистанции цель считается видимой всегда (если нет стены)")]
    public float proximityOverrideRange = 2.0f;

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

    // --- Внутренние данные ---
    private List<Vector3> directions = new List<Vector3>();
    private List<float> directionMaxRanges = new List<float>();
    private List<float> adaptedRanges = new List<float>();
    private List<float> targetRanges = new List<float>();
    private List<RaycastHit> hitInfos = new List<RaycastHit>();
    private List<int>[] rayNeighbors;

    // Флаг: нужно ли пересчитать геометрию лучей (только при изменении параметров)
    private bool dirtyDirections = true;
    private int cachedLatSegs, cachedLonSegs;
    private float cachedHAngle, cachedVUp, cachedVDown, cachedHOffset;

    private float playerMemoryTimer = 0f;
    private Transform cachedTransform;

    // -----------------------------------------------------------------------

    void Awake()
    {
        cachedTransform = transform;
    }

    void Start()
    {
        RebuildDirections();
    }

    void Update()
    {
        // Пересчитываем геометрию лучей ТОЛЬКО если изменились параметры в инспекторе
        if (HasDirectionParamChanged())
            RebuildDirections();

        AdaptToEnvironment();

        if (smoothAdaptation)
            SmoothRangeTransition();

        CheckPlayerInVisionZone();
        CheckFoodInVisionZone();
        CheckDangerInVisionZone();
    }

    // -----------------------------------------------------------------------
    // BT INTEGRATION
    // -----------------------------------------------------------------------

    /// <summary>
    /// Возвращает true пока игрок виден ИЛИ таймер памяти не истёк.
    /// </summary>
    public bool CanSeePlayer() => isPlayerVisible && detectedPlayer != null;

    /// <summary>
    /// Возвращает цель, пока она в памяти (включая время после потери видимости).
    /// </summary>
    public Transform GetDetectedPlayer() => detectedPlayer;

    public bool CanSeeFood() => isFoodVisible && detectedFood != null;
    public bool CanSeeDanger() => isDangerVisible && detectedDanger != null;

    // -----------------------------------------------------------------------
    // НАПРАВЛЕНИЯ (пересчёт только при нужде)
    // -----------------------------------------------------------------------

    bool HasDirectionParamChanged()
    {
        if (cachedLatSegs != latitudeSegments || cachedLonSegs != longitudeSegments ||
            cachedHAngle != horizontalAngle || cachedVUp != verticalAngleUp ||
            cachedVDown != verticalAngleDown || cachedHOffset != horizontalOffset)
        {
            cachedLatSegs = latitudeSegments; cachedLonSegs = longitudeSegments;
            cachedHAngle = horizontalAngle; cachedVUp = verticalAngleUp;
            cachedVDown = verticalAngleDown; cachedHOffset = horizontalOffset;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Полный пересчёт геометрии лучей. Вызывается один раз на Start и при смене параметров.
    /// </summary>
    void RebuildDirections()
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
                Vector3 localDir = new Vector3(xz * Mathf.Sin(azimuthRad), y, xz * Mathf.Cos(azimuthRad)).normalized;

                directions.Add(localDir);
                directionMaxRanges.Add(CalculateDirectionalRange(localDir));
            }
        }

        // Инициализируем диапазоны с нуля
        int count = directions.Count;
        adaptedRanges = new List<float>(new float[count]);
        targetRanges = new List<float>(new float[count]);
        for (int i = 0; i < count; i++)
            adaptedRanges[i] = targetRanges[i] = directionMaxRanges[i];

        PrecalculateNeighbors();
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

        float scale = (fwd * forwardRangeScale +
                       bck * backwardRangeScale +
                       up * upRangeScale +
                       dwn * downRangeScale +
                       side * sideRangeScale) / total;
        return maxDetectionRange * scale;
    }

    void PrecalculateNeighbors()
    {
        int count = directions.Count;
        rayNeighbors = new List<int>[count];
        for (int i = 0; i < count; i++)
        {
            rayNeighbors[i] = new List<int>();
            for (int j = 0; j < count; j++)
                if (i != j && Vector3.Dot(directions[i], directions[j]) > 0.8f)
                    rayNeighbors[i].Add(j);
        }
    }

    // -----------------------------------------------------------------------
    // АДАПТАЦИЯ К СРЕДЕ
    // -----------------------------------------------------------------------

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

            targetRanges[i] = hit
                ? Mathf.Max((hitInfo.distance - visionBuffer) * shrinkMultiplier, minDetectionRange)
                : directionMaxRanges[i];
        }
        SmoothNeighborInfluence();
    }

    void SmoothNeighborInfluence()
    {
        // Используем временный массив вместо new List каждый кадр
        float[] smoothed = new float[targetRanges.Count];
        for (int i = 0; i < targetRanges.Count; i++)
        {
            float sum = targetRanges[i];
            int count = 1;
            foreach (int j in rayNeighbors[i]) { sum += targetRanges[j]; count++; }
            smoothed[i] = Mathf.Min(sum / count, targetRanges[i]);
        }
        for (int i = 0; i < targetRanges.Count; i++)
            targetRanges[i] = smoothed[i];
    }

    void SmoothRangeTransition()
    {
        float dt = adaptationSpeed * Time.deltaTime;
        for (int i = 0; i < adaptedRanges.Count; i++)
            adaptedRanges[i] = Mathf.Lerp(adaptedRanges[i], targetRanges[i], dt);
    }

    // -----------------------------------------------------------------------
    // ДЕТЕКЦИЯ ИГРОКА
    // -----------------------------------------------------------------------

    void CheckPlayerInVisionZone()
    {
        Transform foundThisFrame = FindPlayerInRays();

        if (foundThisFrame != null)
        {
            bool wasVisible = isPlayerVisible;
            detectedPlayer = foundThisFrame;
            isPlayerVisible = true;
            playerMemoryTimer = playerMemoryTime;
            if (!wasVisible) OnPlayerDetected();
        }
        else if (detectedPlayer != null)
        {
            // Прямая проверка текущей цели (не зависит от лучей)
            if (IsTargetVisible(detectedPlayer))
            {
                playerMemoryTimer = playerMemoryTime;   // ещё видим
            }
            else
            {
                playerMemoryTimer -= Time.deltaTime;    // не видим — тикает таймер
            }

            if (playerMemoryTimer <= 0f)
            {
                if (isPlayerVisible) OnPlayerLost();
                isPlayerVisible = false;
                detectedPlayer = null;
            }
            // Пока таймер > 0 — isPlayerVisible остаётся true, цель помним
        }
        else
        {
            isPlayerVisible = false;
        }
    }

    Transform FindPlayerInRays()
    {
        Vector3 origin = cachedTransform.position + Vector3.up * eyeHeight;

        for (int i = 0; i < directions.Count; i++)
        {
            Vector3 worldDir = cachedTransform.TransformDirection(directions[i]);
            float range = adaptedRanges[i];

            // Луч упёрся в стену — пропускаем
            if (range < directionMaxRanges[i] * 0.1f) continue;

            Vector3 checkPoint = origin + worldDir * range;
            Collider[] nearby = Physics.OverlapSphere(checkPoint, playerCheckRadius, playerLayer);

            foreach (Collider pc in nearby)
            {
                if (IsTargetVisible(pc.transform))
                    return pc.transform;
            }
        }
        return null;
    }

    // -----------------------------------------------------------------------
    // ПРОВЕРКА ВИДИМОСТИ (ключевой метод)
    // -----------------------------------------------------------------------

    public bool IsTargetVisible(Transform target)
    {
        if (target == null) return false;

        Vector3 origin = cachedTransform.position + Vector3.up * eyeHeight;
        Vector3 targetCenter = target.position + Vector3.up * 1.0f;
        float dist = Vector3.Distance(origin, targetCenter);

        // --- Proximity override ---
        // На близкой дистанции проверяем только препятствие, но НЕ угол обзора.
        // Это решает проблему «потери» цели при подходе вплотную,
        // когда она выходит за горизонтальный угол зрения.
        if (dist <= proximityOverrideRange)
        {
            RaycastHit hit;
            if (Physics.Raycast(origin, (targetCenter - origin).normalized, out hit, dist, obstacleLayer))
                return false;   // стена между нами — не видим
            return true;        // вплотную и без стены — видим
        }

        // --- Угловая проверка ---
        Vector3 dirToTarget = (targetCenter - origin).normalized;
        Vector3 localDir = cachedTransform.InverseTransformDirection(dirToTarget);

        float elevation = Mathf.Asin(Mathf.Clamp(localDir.y, -1f, 1f)) * Mathf.Rad2Deg;
        float azimuth = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;

        if (elevation > verticalAngleUp || elevation < -verticalAngleDown) return false;
        if (Mathf.Abs(Mathf.DeltaAngle(horizontalOffset, azimuth)) > horizontalAngle * 0.5f) return false;

        // --- Raycast на препятствия ---
        RaycastHit hitInfo;
        if (Physics.Raycast(origin, dirToTarget, out hitInfo, dist, obstacleLayer | playerLayer))
        {
            return hitInfo.transform == target || hitInfo.transform.IsChildOf(target);
        }
        return true;
    }

    // -----------------------------------------------------------------------
    // FOOD & DANGER
    // -----------------------------------------------------------------------

    void CheckFoodInVisionZone()
    {
        if (string.IsNullOrEmpty(foodTag)) return;
        isFoodVisible = false;
        Vector3 origin = cachedTransform.position + Vector3.up * eyeHeight;
        foreach (Collider col in Physics.OverlapSphere(origin, maxDetectionRange, -1))
        {
            if (col.CompareTag(foodTag) && IsTargetVisible(col.transform))
            {
                detectedFood = col.transform;
                isFoodVisible = true;
                return;
            }
        }
        if (!isFoodVisible) detectedFood = null;
    }

    void CheckDangerInVisionZone()
    {
        if (string.IsNullOrEmpty(dangerTag)) return;
        isDangerVisible = false;
        Vector3 origin = cachedTransform.position + Vector3.up * eyeHeight;
        foreach (Collider col in Physics.OverlapSphere(origin, maxDetectionRange, -1))
        {
            if (col.CompareTag(dangerTag) && IsTargetVisible(col.transform))
            {
                detectedDanger = col.transform;
                isDangerVisible = true;
                return;
            }
        }
        if (!isDangerVisible) detectedDanger = null;
    }

    // -----------------------------------------------------------------------
    // UNIVERSAL BT HELPERS
    // -----------------------------------------------------------------------

    public Transform FindVisibleByTag(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return null;
        Vector3 origin = cachedTransform.position + Vector3.up * eyeHeight;
        foreach (Collider col in Physics.OverlapSphere(origin, maxDetectionRange, -1))
            if (col.CompareTag(tag) && IsTargetVisible(col.transform))
                return col.transform;
        return null;
    }

    public Transform FindVisibleByLayer(LayerMask layer)
    {
        Vector3 origin = cachedTransform.position + Vector3.up * eyeHeight;
        foreach (Collider col in Physics.OverlapSphere(origin, maxDetectionRange, layer))
            if (IsTargetVisible(col.transform))
                return col.transform;
        return null;
    }

    // -----------------------------------------------------------------------
    // EVENTS
    // -----------------------------------------------------------------------

    public virtual void OnPlayerDetected() => Debug.Log($"<color=green>[{gameObject.name}] Вижу игрока!</color>");
    protected virtual void OnPlayerLost() => Debug.Log($"<color=red>[{gameObject.name}] Потерял игрока!</color>");

    // -----------------------------------------------------------------------
    // DEBUG GIZMOS
    // -----------------------------------------------------------------------

    void OnDrawGizmos()
    {
        if (!Application.isPlaying && (directions == null || directions.Count == 0))
            RebuildDirections();

        Vector3 origin = transform.position + Vector3.up * eyeHeight;

        if (showRays)
        {
            for (int i = 0; i < directions.Count; i++)
            {
                bool blocked = hitInfos != null && i < hitInfos.Count && hitInfos[i].collider != null;
                Gizmos.color = blocked ? obstructedRayColor : normalRayColor;
                Gizmos.DrawLine(origin, origin + transform.rotation * directions[i] * adaptedRanges[i]);
            }
        }

        // Рисуем сферу proximity-override
        Gizmos.color = new Color(1f, 1f, 0f, 0.15f);
        Gizmos.DrawWireSphere(origin, proximityOverrideRange);

        if (showPlayerConnection && detectedPlayer != null)
        {
            Gizmos.color = isPlayerVisible ? playerConnectionColor : new Color(1f, 0.5f, 0f);
            Gizmos.DrawLine(origin, detectedPlayer.position);
        }
    }
}