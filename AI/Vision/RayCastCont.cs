using UnityEngine;
using System.Collections.Generic;

public abstract class RayCastCont : MonoBehaviour
{
    [Header("Raycast Settings")]
    [SerializeField] protected LayerMask detectionMask = ~0;
    [SerializeField, Range(1, 360)] protected int raysCount = 36;
    [SerializeField] protected float rayLength = 1f;
    [SerializeField] protected bool autoUpdateRaycasts = true;

    [Header("Debug Visualization")]
    [SerializeField] protected bool drawRays = true;
    [SerializeField] protected bool drawInPlayMode = true;
    [SerializeField] protected Color rayColorBase = Color.red;
    [SerializeField] protected Color hitColor = Color.green;
    [SerializeField] protected float hitPointSize = 0.1f;
    [SerializeField] protected bool drawHitPoints = true;

    public struct RaycastHitData
    {
        public bool hasHit;
        public GameObject hitObject;
        public string hitTag;
        public Vector3 hitPoint;
        public Vector3 hitNormal;
        public float distance;
        public float angle;
    }

    protected RaycastHitData[] rayHits;

    protected virtual void Start()
    {
        rayHits = new RaycastHitData[raysCount];
    }

    protected virtual void Update()
    {
        if (autoUpdateRaycasts)
        {
            PerformCircularRaycast();
        }
    }

    protected virtual void PerformCircularRaycast()
    {
        float step = 360f / Mathf.Max(1, raysCount);
        Vector3 origin = transform.position;

        for (int i = 0; i < raysCount; i++)
        {
            float angle = step * i;
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * transform.forward;

            if (Physics.Raycast(origin, direction, out RaycastHit hit, rayLength, detectionMask))
            {
                // Луч попал в объект - рисуем до точки столкновения
                if (drawRays && drawInPlayMode && Application.isPlaying)
                {
                    Debug.DrawLine(origin, hit.point, hitColor);

                    if (drawHitPoints)
                    {
                        // Визуализация точки попадания
                        Debug.DrawRay(hit.point, hit.normal * hitPointSize, Color.yellow);
                    }
                }

                rayHits[i] = new RaycastHitData
                {
                    hasHit = true,
                    hitObject = hit.collider.gameObject,
                    hitTag = hit.collider.tag,
                    hitPoint = hit.point,
                    hitNormal = hit.normal,
                    distance = hit.distance,
                    angle = angle
                };

                OnRayHit(hit, angle, i);
            }
            else
            {
                // Луч не попал - рисуем полную длину
                if (drawRays && drawInPlayMode && Application.isPlaying)
                {
                    Debug.DrawLine(origin, origin + direction * rayLength, rayColorBase);
                }

                rayHits[i] = new RaycastHitData
                {
                    hasHit = false,
                    angle = angle
                };
            }
        }
    }

    protected bool TryDetect(out RaycastHit hitInfo)
    {
        float step = 360f / Mathf.Max(1, raysCount);

        for (int i = 0; i < raysCount; i++)
        {
            float angle = step * i;
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * transform.forward;

            if (Physics.Raycast(transform.position, direction, out hitInfo, rayLength, detectionMask))
            {
                if (IsTarget(hitInfo))
                    return true;
            }
        }

        hitInfo = default;
        return false;
    }

    protected virtual void OnRayHit(RaycastHit hit, float angle, int rayIndex)
    {
        // Переопределите для своей логики
    }

    public abstract bool IsTarget(RaycastHit hitInfo);

    // Визуализация в редакторе (когда объект выбран)
    protected virtual void OnDrawGizmosSelected()
    {
        if (!drawRays)
            return;

        float step = 360f / Mathf.Max(1, raysCount);
        Vector3 origin = transform.position;

        for (int i = 0; i < raysCount; i++)
        {
            float angle = step * i;
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * transform.forward;

            if (Physics.Raycast(origin, direction, out RaycastHit hit, rayLength, detectionMask))
            {
                // Попадание - зеленая линия до точки столкновения
                Gizmos.color = hitColor;
                Gizmos.DrawLine(origin, hit.point);

                if (drawHitPoints)
                {
                    // Точка столкновения
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawSphere(hit.point, hitPointSize);

                    // Нормаль поверхности
                    Gizmos.color = Color.blue;
                    Gizmos.DrawRay(hit.point, hit.normal * hitPointSize * 2);
                }
            }
            else
            {
                // Промах - красная линия полной длины
                Gizmos.color = rayColorBase;
                Gizmos.DrawLine(origin, origin + direction * rayLength);
            }
        }
    }

    // Получить все попадания
    public RaycastHitData[] GetAllHits()
    {
        return rayHits;
    }

    // Получить ближайшее попадание
    public bool GetClosestHit(out RaycastHitData closestHit)
    {
        closestHit = default;
        float minDistance = float.MaxValue;
        bool foundHit = false;

        if (rayHits == null) return false;

        foreach (var hit in rayHits)
        {
            if (hit.hasHit && hit.distance < minDistance)
            {
                minDistance = hit.distance;
                closestHit = hit;
                foundHit = true;
            }
        }

        return foundHit;
    }
}
