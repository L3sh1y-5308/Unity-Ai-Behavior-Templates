using System.Collections.Generic;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;

public enum DetectionMode
{
    Normal,
    Research,
    Alert,
    Custom
}


[System.Serializable, Unity.Properties.GeneratePropertyBag]
[NodeDescription(name: "Set Detection Mode", story: "Set detection mode to [mode] for [agent]", category: "Action", id: "set_detection_mode")]
public partial class SetDetectionModeAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<DetectionMode> Mode = new(DetectionMode.Normal);

    // Настройки для разных режимов
    [Header("Normal Mode")]
    public float normalHorizontalFOV = 90f;
    public float normalVerticalFOV = 60f;
    public float normalRadius = 10f;

    [Header("Research Mode")]
    public float researchHorizontalFOV = 360f;
    public float researchVerticalFOV = 360f;
    public float researchRadius = 25f;

    [Header("Alert Mode")]
    public float alertHorizontalFOV = 120f;
    public float alertVerticalFOV = 90f;
    public float alertRadius = 15f;

    protected override Status OnStart()
    {
        if (Agent.Value == null) return Status.Failure;

        var fovComponent = Agent.Value.GetComponent<FieldOfViewCone>();
        if (fovComponent == null) return Status.Failure;

        switch (Mode.Value)
        {
            case DetectionMode.Normal:
                fovComponent.SetHorizontalFOV(normalHorizontalFOV);
                fovComponent.SetVerticalFOV(normalVerticalFOV);
                fovComponent.SetViewRadius(normalRadius);
                break;

            case DetectionMode.Research:
                fovComponent.SetHorizontalFOV(researchHorizontalFOV);
                fovComponent.SetVerticalFOV(researchVerticalFOV);
                fovComponent.SetViewRadius(researchRadius);
                break;

            case DetectionMode.Alert:
                fovComponent.SetHorizontalFOV(alertHorizontalFOV);
                fovComponent.SetVerticalFOV(alertVerticalFOV);
                fovComponent.SetViewRadius(alertRadius);
                break;
        }

        Debug.Log($"Detection mode changed to: {Mode.Value}");
        return Status.Success;
    }




    public class FieldOfViewCone : MonoBehaviour
    {
        [Header("3D Field of View Settings")]
        [Range(0f, 360f)]
        [SerializeField] float horizontalFOV = 90f; // Угол по горизонтали (Y-axis)
        [Range(0f, 360f)]
        [SerializeField] float verticalFOV = 90f;   // Угол по вертикали (X-axis)
        [Range(0f, 360f)]
        [SerializeField] float rollFOV = 360f;      // Поворот вокруг оси Z

        [Header("Detection Settings")]
        [SerializeField] float viewRadius = 10f;
        [SerializeField] int horizontalRays = 20;   // Количество лучей по горизонтали
        [SerializeField] int verticalRays = 10;     // Количество лучей по вертикали

        [Header("Target Detection")]
        [SerializeField] LayerMask targetMask;
        [SerializeField] LayerMask obstacleMask;
        [SerializeField] Transform target;

        [Header("Debug")]
        [SerializeField] bool showRays = true;
        [SerializeField] bool showDetectedTargets = true;

        public bool IsTargetVisible { get; private set; }
        private List<Vector3> rayDirections = new List<Vector3>();
        private List<RaycastHit> rayHits = new List<RaycastHit>();
        private List<bool> rayHasHit = new List<bool>();

        void Start()
        {
            GenerateRayDirections();
        }

        void Update()
        {
            GenerateRayDirections(); // Обновляем лучи при изменении углов
            PerformSphericalRaycast();
            CheckTargetVisibility();
        }

        void GenerateRayDirections()
        {
            rayDirections.Clear();

            // Для 360-градусного обзора используем сферические координаты
            int totalHorizontalRays = horizontalFOV >= 359f ? horizontalRays : Mathf.Max(1, (int)(horizontalRays * (horizontalFOV / 360f)));
            int totalVerticalRays = verticalFOV >= 359f ? verticalRays : Mathf.Max(1, (int)(verticalRays * (verticalFOV / 360f)));

            // Вычисляем углы начала и конца
            float horizontalStart = -horizontalFOV * 0.5f;
            float verticalStart = -verticalFOV * 0.5f;

            float horizontalStep = horizontalFOV / Mathf.Max(1, totalHorizontalRays - 1);
            float verticalStep = verticalFOV / Mathf.Max(1, totalVerticalRays - 1);

            // Генерируем направления лучей
            for (int h = 0; h < totalHorizontalRays; h++)
            {
                for (int v = 0; v < totalVerticalRays; v++)
                {
                    // Углы в сферических координатах
                    float horizontalAngle = horizontalStart + (h * horizontalStep);
                    float verticalAngle = verticalStart + (v * verticalStep);

                    // Применяем поворот вокруг Z (roll)
                    Vector3 direction = GetSphericalDirection(horizontalAngle, verticalAngle, rollFOV);
                    rayDirections.Add(direction);
                }
            }

            // Специальный случай для полного 360° обзора
            if (horizontalFOV >= 359f && verticalFOV >= 359f)
            {
                GenerateFullSphereRays();
            }
        }

        void GenerateFullSphereRays()
        {
            rayDirections.Clear();

            // Генерируем лучи равномерно по сфере (алгоритм Fibonacci sphere)
            int totalRays = horizontalRays * verticalRays;

            for (int i = 0; i < totalRays; i++)
            {
                float t = (float)i / (totalRays - 1);

                // Fibonacci sphere distribution
                float inclination = Mathf.Acos(1 - 2 * t); // от 0 до π
                float azimuth = Mathf.PI * (3 - Mathf.Sqrt(5)) * i; // золотой угол

                // Преобразуем в декартовы координаты
                float x = Mathf.Sin(inclination) * Mathf.Cos(azimuth);
                float y = Mathf.Sin(inclination) * Mathf.Sin(azimuth);
                float z = Mathf.Cos(inclination);

                Vector3 direction = new Vector3(x, y, z);

                // Применяем поворот объекта
                direction = transform.TransformDirection(direction);
                rayDirections.Add(direction);
            }
        }

        Vector3 GetSphericalDirection(float horizontalAngle, float verticalAngle, float rollAngle)
        {
            // Создаем направление относительно forward вектора объекта
            Quaternion horizontalRotation = Quaternion.AngleAxis(horizontalAngle, Vector3.up);
            Quaternion verticalRotation = Quaternion.AngleAxis(verticalAngle, Vector3.right);
            Quaternion rollRotation = Quaternion.AngleAxis(rollAngle * 0.5f, Vector3.forward);

            // Комбинируем повороты
            Quaternion totalRotation = horizontalRotation * verticalRotation * rollRotation;
            Vector3 localDirection = totalRotation * Vector3.forward;

            // Преобразуем в мировые координаты
            return transform.TransformDirection(localDirection);
        }

        void PerformSphericalRaycast()
        {
            rayHits.Clear();
            rayHasHit.Clear();

            Vector3 origin = transform.position;

            foreach (Vector3 direction in rayDirections)
            {
                RaycastHit hit;
                bool hasHit = Physics.Raycast(origin, direction, out hit, viewRadius, obstacleMask | targetMask);

                rayHits.Add(hit);
                rayHasHit.Add(hasHit);

                // Debug лучи
                if (showRays)
                {
                    Color rayColor = hasHit ?
                        (((1 << hit.collider.gameObject.layer) & targetMask) != 0 ? Color.red : Color.yellow) :
                        Color.green;

                    Vector3 endPoint = hasHit ? hit.point : origin + direction * viewRadius;
                    Debug.DrawLine(origin, endPoint, rayColor, 0.1f);
                }
            }
        }

        void CheckTargetVisibility()
        {
            IsTargetVisible = false;

            if (target == null) return;

            // Проверяем через прямой raycast к цели
            Vector3 directionToTarget = (target.position - transform.position).normalized;
            float distanceToTarget = Vector3.Distance(transform.position, target.position);

            if (distanceToTarget > viewRadius) return;

            // Проверяем, попадает ли цель в наш FOV
            if (!IsDirectionInFOV(directionToTarget)) return;

            // Проверяем препятствия
            RaycastHit hit;
            if (Physics.Raycast(transform.position, directionToTarget, out hit, distanceToTarget, obstacleMask))
            {
                return; // Препятствие блокирует обзор
            }

            // Проверяем слой цели
            if (((1 << target.gameObject.layer) & targetMask) != 0)
            {
                IsTargetVisible = true;
            }
        }

        bool IsDirectionInFOV(Vector3 worldDirection)
        {
            // Преобразуем направление в локальные координаты объекта
            Vector3 localDirection = transform.InverseTransformDirection(worldDirection);

            // Вычисляем углы в сферических координатах
            float horizontalAngle = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;
            float verticalAngle = Mathf.Asin(localDirection.y / localDirection.magnitude) * Mathf.Rad2Deg;

            // Нормализуем углы
            horizontalAngle = NormalizeAngle(horizontalAngle);

            // Проверяем попадание в FOV
            bool inHorizontalFOV = horizontalFOV >= 359f || Mathf.Abs(horizontalAngle) <= horizontalFOV * 0.5f;
            bool inVerticalFOV = verticalFOV >= 359f || Mathf.Abs(verticalAngle) <= verticalFOV * 0.5f;

            return inHorizontalFOV && inVerticalFOV;
        }

        float NormalizeAngle(float angle)
        {
            while (angle > 180f) angle -= 360f;
            while (angle < -180f) angle += 360f;
            return angle;
        }

        // Публичные методы для внешнего контроля
        public void SetHorizontalFOV(float angle)
        {
            horizontalFOV = Mathf.Clamp(angle, 0f, 360f);
        }

        public void SetVerticalFOV(float angle)
        {
            verticalFOV = Mathf.Clamp(angle, 0f, 360f);
        }

        public void SetRollFOV(float angle)
        {
            rollFOV = Mathf.Clamp(angle, 0f, 360f);
        }

        public void SetViewRadius(float radius)
        {
            viewRadius = Mathf.Max(0.1f, radius);
        }

        // Получить все обнаруженные объекты
        public List<GameObject> GetDetectedObjects()
        {
            List<GameObject> detected = new List<GameObject>();

            for (int i = 0; i < rayHits.Count; i++)
            {
                if (rayHasHit[i] && rayHits[i].collider != null)
                {
                    if (((1 << rayHits[i].collider.gameObject.layer) & targetMask) != 0)
                    {
                        GameObject obj = rayHits[i].collider.gameObject;
                        if (!detected.Contains(obj))
                        {
                            detected.Add(obj);
                        }
                    }
                }
            }

            return detected;
        }

        void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            Vector3 origin = transform.position;

            // Рисуем сферу обзора
            Gizmos.color = new Color(0, 1, 0, 0.1f);
            Gizmos.DrawSphere(origin, viewRadius);

            // Рисуем границы FOV
            if (horizontalFOV < 359f || verticalFOV < 359f)
            {
                DrawFOVBounds();
            }

            // Рисуем обнаруженные цели
            if (showDetectedTargets)
            {
                Gizmos.color = Color.red;
                List<GameObject> detected = GetDetectedObjects();
                foreach (GameObject obj in detected)
                {
                    Gizmos.DrawLine(origin, obj.transform.position);
                    Gizmos.DrawWireSphere(obj.transform.position, 0.5f);
                }
            }

            // Центр
            Gizmos.color = IsTargetVisible ? Color.red : Color.white;
            Gizmos.DrawWireSphere(origin, 0.2f);
        }

        void DrawFOVBounds()
        {
            Vector3 origin = transform.position;

            // Рисуем границы горизонтального FOV
            if (horizontalFOV < 359f)
            {
                Gizmos.color = Color.blue;
                float halfHorizontal = horizontalFOV * 0.5f;

                Vector3 leftBound = GetSphericalDirection(-halfHorizontal, 0, 0);
                Vector3 rightBound = GetSphericalDirection(halfHorizontal, 0, 0);

                Gizmos.DrawLine(origin, origin + leftBound * viewRadius);
                Gizmos.DrawLine(origin, origin + rightBound * viewRadius);
            }

            // Рисуем границы вертикального FOV
            if (verticalFOV < 359f)
            {
                Gizmos.color = Color.cyan;
                float halfVertical = verticalFOV * 0.5f;

                Vector3 upBound = GetSphericalDirection(0, halfVertical, 0);
                Vector3 downBound = GetSphericalDirection(0, -halfVertical, 0);

                Gizmos.DrawLine(origin, origin + upBound * viewRadius);
                Gizmos.DrawLine(origin, origin + downBound * viewRadius);
            }
        }
    }
}
