using System.Collections.Generic;
using UnityEngine;

public class AdaptiveVSystem_SpiderEdit : MonoBehaviour
{
    [Header("Vision Settings")]
    [Tooltip("Горизонтальный угол обзора")]
    [Range(0f, 360f)] public float horizontalAngle = 120f;
    [Range(0f, 180f)] public float verticalAngleUp = 45f;
    [Range(0f, 180f)] public float verticalAngleDown = 30f;

    [Tooltip("Офсет осей модели. Если синяя стрелка(Z) смотрит назад, поставьте 180")]
    [Range(-180f, 180f)] public float horizontalOffset = 180f;

    [Header("Detection Range")]
    public float maxDetectionRange = 10f;
    public float minDetectionRange = 0.5f;
    [Tooltip("Множитель дальности перед головой паука")]
    [Range(0.1f, 3f)] public float forwardRangeScale = 1f;
    [Tooltip("Множитель дальности сзади (у хвоста)")]
    [Range(0.1f, 3f)] public float backwardRangeScale = 0.3f;
    [Tooltip("Множитель дальности вверх")]
    [Range(0.1f, 3f)] public float upRangeScale = 0.8f;
    [Tooltip("Множитель дальности вниз")]
    [Range(0.1f, 3f)] public float downRangeScale = 0.6f;
    [Tooltip("Множитель дальности по бокам")]
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

    void Awake() => cachedTransform = transform;

    void Update()
    {
        GenerateDirections();
        InitializeRanges();
        AdaptToEnvironment();
        if (smoothAdaptation) SmoothRangeTransition();
        CheckPlayerInVisionZone();
    }

    /// <summary>
    /// Генерирует лучи, учитывая разворот осей модели
    /// </summary>
    void GenerateDirections()
    {
        directions.Clear();
        directionMaxRanges.Clear();

        float phiMin = -verticalAngleDown;
        float phiMax = verticalAngleUp;

        // Определяем сектор обзора вокруг головы (horizontalOffset)
        float thetaMin = horizontalOffset - horizontalAngle * 0.5f;
        float thetaMax = horizontalOffset + horizontalAngle * 0.5f;

        // Кватернион смещения для правильного расчета весов дальности
        Quaternion offsetRotation = Quaternion.Euler(0, horizontalOffset, 0);

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

                // Корректируем веса: теперь "настоящий перед" это horizontalOffset
                Vector3 weightDir = Quaternion.Inverse(offsetRotation) * localDir;
                float dirMax = CalculateDirectionalRange(weightDir);
                directionMaxRanges.Add(dirMax);
            }
        }
    }

    float CalculateDirectionalRange(Vector3 weightDir)
    {
        float fwd = Mathf.Max(0f, weightDir.z);
        float bck = Mathf.Max(0f, -weightDir.z);
        float up = Mathf.Max(0f, weightDir.y);
        float dwn = Mathf.Max(0f, -weightDir.y);
        float side = Mathf.Abs(weightDir.x);

        float total = fwd + bck + up + dwn + side;
        if (total < 0.001f) return maxDetectionRange;

        float scale = (fwd * forwardRangeScale + bck * backwardRangeScale + up * upRangeScale + dwn * downRangeScale + side * sideRangeScale) / total;
        return maxDetectionRange * scale;
    }

    void InitializeRanges()
    {
        while (adaptedRanges.Count < directions.Count)
        {
            adaptedRanges.Add(maxDetectionRange);
            targetRanges.Add(maxDetectionRange);
        }
    }

    void AdaptToEnvironment()
    {
        hitInfos.Clear();
        Vector3 origin = cachedTransform.position;

        for (int i = 0; i < directions.Count; i++)
        {
            Vector3 worldDir = cachedTransform.TransformDirection(directions[i]);
            RaycastHit hit;
            if (Physics.Raycast(origin, worldDir, out hit, directionMaxRanges[i], obstacleLayer))
            {
                hitInfos.Add(hit);
                targetRanges[i] = Mathf.Max((hit.distance - visionBuffer) * shrinkMultiplier, minDetectionRange);
            }
            else
            {
                hitInfos.Add(new RaycastHit());
                targetRanges[i] = directionMaxRanges[i];
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
                if (i != j && Vector3.Dot(directions[i], directions[j]) > 0.8f) { sum += targetRanges[j]; count++; }
            smoothedRanges[i] = Mathf.Min(sum / count, targetRanges[i]);
        }
        targetRanges = smoothedRanges;
    }

    void SmoothRangeTransition()
    {
        for (int i = 0; i < adaptedRanges.Count; i++)
            adaptedRanges[i] = Mathf.Lerp(adaptedRanges[i], targetRanges[i], adaptationSpeed * Time.deltaTime);
    }

    void CheckPlayerInVisionZone()
    {
        Vector3 origin = cachedTransform.position;
        Transform foundPlayer = null;
        for (int i = 0; i < directions.Count; i++)
        {
            if (i >= adaptedRanges.Count || adaptedRanges[i] < directionMaxRanges[i] * 0.5f) continue;
            Vector3 worldDir = cachedTransform.TransformDirection(directions[i]);
            if (Physics.Raycast(origin, worldDir, adaptedRanges[i], obstacleLayer)) continue;

            Collider[] hits = Physics.OverlapSphere(origin + worldDir * adaptedRanges[i], playerCheckRadius, playerLayer);
            foreach (var h in hits) if (IsPlayerVisible(h.transform)) { foundPlayer = h.transform; break; }
            if (foundPlayer != null) break;
        }

        if (foundPlayer != null) { detectedPlayer = foundPlayer; if (!isPlayerVisible) OnPlayerDetected(); isPlayerVisible = true; }
        else if (isPlayerVisible) { if (detectedPlayer == null || !IsPlayerVisible(detectedPlayer)) { isPlayerVisible = false; OnPlayerLost(); } }
    }

    public bool IsPlayerVisible(Transform player)
    {
        if (!player) return false;
        Vector3 origin = cachedTransform.position;
        Vector3 toPlayer = (player.position - origin).normalized;
        Vector3 local = cachedTransform.InverseTransformDirection(toPlayer);

        float elevationDeg = Mathf.Asin(Mathf.Clamp(local.y, -1f, 1f)) * Mathf.Rad2Deg;
        if (elevationDeg > verticalAngleUp || elevationDeg < -verticalAngleDown) return false;

        float azimuthDeg = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
        if (Mathf.Abs(Mathf.DeltaAngle(horizontalOffset, azimuthDeg)) > horizontalAngle * 0.5f) return false;

        RaycastHit hit;
        Vector3 rayOrigin = origin + Vector3.up * 0.5f;
        Vector3 playerPos = player.position + Vector3.up * 0.5f;
        if (Physics.Raycast(rayOrigin, (playerPos - rayOrigin).normalized, out hit, Vector3.Distance(rayOrigin, playerPos), obstacleLayer))
            return hit.transform.IsChildOf(player) || player.IsChildOf(hit.transform);

        return Vector3.Distance(origin, player.position) <= GetMaxRangeInDirection(toPlayer);
    }

    public float GetMaxRangeInDirection(Vector3 worldDirection)
    {
        Vector3 localDir = cachedTransform.InverseTransformDirection(worldDirection).normalized;
        int best = 0; float maxDot = -1f;
        for (int i = 0; i < directions.Count; i++)
        {
            float dot = Vector3.Dot(localDir, directions[i]);
            if (dot > maxDot) { maxDot = dot; best = i; }
        }
        return adaptedRanges[best];
    }

    public virtual void OnPlayerDetected() => GetComponent<EnemyAI>()?.SetTarget(detectedPlayer);
    protected virtual void OnPlayerLost() => GetComponent<EnemyAI>()?.LoseTarget();

    void OnDrawGizmos()
    {
        if (directions.Count == 0 || !Application.isPlaying) { GenerateDirections(); InitializeRanges(); }
        Vector3 origin = cachedTransform ? cachedTransform.position : transform.position;
        Quaternion rot = cachedTransform ? cachedTransform.rotation : transform.rotation;

        for (int i = 0; i < directions.Count; i++)
        {
            if (i >= adaptedRanges.Count) continue;
            Vector3 worldDir = rot * directions[i];
            bool hitObstacle = i < hitInfos.Count && hitInfos[i].collider != null;

            Gizmos.color = hitObstacle ? obstructedRayColor : (adaptedRanges[i] < directionMaxRanges[i] * 0.9f ? adaptedRayColor : normalRayColor);
            Vector3 target = origin + worldDir * (hitObstacle ? hitInfos[i].distance : adaptedRanges[i]);
            Gizmos.DrawLine(origin, target);
            if (hitObstacle) Gizmos.DrawSphere(target, 0.05f);
        }
    }
}