using UnityEngine;

public class RayCastController : MonoBehaviour
{
    [Header("Raycast Settings")]
    [SerializeField] protected LayerMask collisionMask;
    [SerializeField] protected int rayCount = 16;
    [SerializeField] protected float rayLength = 1f;
    [SerializeField] protected bool autoUpdateRaycasts = true;

    [Header("Debug")]
    [SerializeField] protected bool showDebugRays = true;
    [SerializeField] protected Color rayColor = Color.red;
    [SerializeField] protected Color hitColor = Color.green;

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
        rayHits = new RaycastHitData[rayCount];
    }

    protected virtual void Update()
    {
        if (autoUpdateRaycasts)
        {
            PerformCircularRaycast();
        }
    }

    // Выстреливает лучи на 360 градусов вокруг объекта
    protected virtual void PerformCircularRaycast()
    {
        float angleStep = 360f / rayCount;
        Vector3 origin = transform.position;

        for (int i = 0; i < rayCount; i++)
        {
            float angle = angleStep * i;
            float angleRad = angle * Mathf.Deg2Rad;

            // Направление луча в плоскости XY
            Vector3 direction = new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0f);

            if (Physics.Raycast(origin, direction, out RaycastHit hit, rayLength, collisionMask))
            {
                if (showDebugRays)
                    Debug.DrawRay(origin, direction * hit.distance, hitColor);

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
                if (showDebugRays)
                    Debug.DrawRay(origin, direction * rayLength, rayColor);

                rayHits[i] = new RaycastHitData
                {
                    hasHit = false,
                    angle = angle
                };
            }
        }
    }

    // Виртуальный метод для обработки попадания луча
    protected virtual void OnRayHit(RaycastHit hit, float angle, int rayIndex)
    {
        // Переопределите этот метод в дочернем классе для кастомной логики
    }

    // Проверка попадания в конкретный тег
    public virtual bool CheckForTag(string tag, out RaycastHitData hitData)
    {
        for (int i = 0; i < rayHits.Length; i++)
        {
            if (rayHits[i].hasHit && rayHits[i].hitTag == tag)
            {
                hitData = rayHits[i];
                return true;
            }
        }
        hitData = default;
        return false;
    }

    // Проверка попадания в несколько тегов
    public virtual bool CheckForTags(string[] tags, out RaycastHitData hitData)
    {
        for (int i = 0; i < rayHits.Length; i++)
        {
            if (rayHits[i].hasHit)
            {
                foreach (string tag in tags)
                {
                    if (rayHits[i].hitTag == tag)
                    {
                        hitData = rayHits[i];
                        return true;
                    }
                }
            }
        }
        hitData = default;
        return false;
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

        for (int i = 0; i < rayHits.Length; i++)
        {
            if (rayHits[i].hasHit && rayHits[i].distance < minDistance)
            {
                minDistance = rayHits[i].distance;
                closestHit = rayHits[i];
                foundHit = true;
            }
        }

        return foundHit;
    }

    // Получить попадания в определенном диапазоне углов
    public RaycastHitData[] GetHitsInAngleRange(float minAngle, float maxAngle)
    {
        var hitsInRange = new System.Collections.Generic.List<RaycastHitData>();

        for (int i = 0; i < rayHits.Length; i++)
        {
            if (rayHits[i].hasHit && rayHits[i].angle >= minAngle && rayHits[i].angle <= maxAngle)
            {
                hitsInRange.Add(rayHits[i]);
            }
        }

        return hitsInRange.ToArray();
    }
}