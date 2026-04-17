// Copyright 2025. All Rights Reserved.

using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VisualDebug
{
    [ExecuteInEditMode]
    public class RaycastVisualizer : MonoBehaviour
    {
        [SerializeField]
        private CastType castType = CastType.Ray;
        [SerializeField]
        private float castDistance = 10f;
        [SerializeField]
        private Vector3 castDirection = Vector3.forward;
        [SerializeField]
        private float radius = 0.5f;
        [SerializeField]
        private Vector3 boxSize = Vector3.one;
        [SerializeField]
        private bool showInPlayMode = true;
        [SerializeField]
        private bool continuousCast = true;
        [SerializeField]
        private Color rayColor = Color.green;
        [SerializeField]
        private Color hitColor = Color.red;
        [SerializeField]
        private LayerMask layerMask = -1;

        public enum CastType
        {
            Ray,
            Sphere,
            Box,
            Capsule
        }

        public List<RaycastHit> hits = new List<RaycastHit>();
        private bool hasHit = false;
        private RaycastHit closestHit;

        private void FixedUpdate()
        {
            if (!continuousCast && !Application.isPlaying)
                return;

            PerformCast();
        }

        private void PerformCast()
        {
            hits.Clear();
            hasHit = false;

            Vector3 origin = transform.position;
            Vector3 direction = castDirection.normalized;

            switch (castType)
            {
                case CastType.Ray:
                    hasHit = Physics.Raycast(origin, direction, out closestHit, castDistance, layerMask);
                    if (hasHit)
                    {
                        hits.Add(closestHit);
                    }
                    break;

                case CastType.Sphere:
                    hasHit = Physics.SphereCast(origin, radius, direction, out closestHit, castDistance, layerMask);
                    if (hasHit)
                    {
                        hits.Add(closestHit);
                    }
                    break;

                case CastType.Box:
                    hasHit = Physics.BoxCast(origin, boxSize * 0.5f, direction, out closestHit, transform.rotation, castDistance, layerMask);
                    if (hasHit)
                    {
                        hits.Add(closestHit);
                    }
                    break;

                case CastType.Capsule:
                    Vector3 point1 = origin;
                    Vector3 point2 = origin + transform.up * radius * 2f;
                    hasHit = Physics.CapsuleCast(point1, point2, radius, direction, out closestHit, castDistance, layerMask);
                    if (hasHit)
                    {
                        hits.Add(closestHit);
                    }
                    break;
            }
        }

        [ContextMenu("Perform Cast")]
        public void ManualCast()
        {
            PerformCast();
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!showInPlayMode && Application.isPlaying)
                return;

            DrawCastVisualization();
        }

        private void DrawCastVisualization()
        {
            Vector3 origin = transform.position;
            Vector3 direction = castDirection.normalized;
            float distance = hasHit ? closestHit.distance : castDistance;
            Vector3 endPoint = origin + direction * distance;

            // Рисуем основную линию
            Gizmos.color = hasHit ? hitColor : rayColor;
            Gizmos.DrawLine(origin, endPoint);

            // Рисуем стрелку направления
            Handles.color = rayColor;
            Handles.ArrowHandleCap(0, origin, Quaternion.LookRotation(direction), 1f, EventType.Repaint);

            switch (castType)
            {
                case CastType.Ray:
                    DrawRayVisualization(origin, endPoint);
                    break;

                case CastType.Sphere:
                    DrawSphereVisualization(origin, endPoint, direction);
                    break;

                case CastType.Box:
                    DrawBoxVisualization(origin, endPoint, direction);
                    break;

                case CastType.Capsule:
                    DrawCapsuleVisualization(origin, endPoint, direction);
                    break;
            }

            // Рисуем точку попадания
            if (hasHit)
            {
                Gizmos.color = hitColor;
                Gizmos.DrawSphere(closestHit.point, 0.1f);
                
                // Рисуем нормаль
                Handles.color = Color.yellow;
                Handles.DrawLine(closestHit.point, closestHit.point + closestHit.normal);
                
                // Метка с информацией
                Handles.Label(closestHit.point + Vector3.up * 0.2f, 
                    $"Hit: {closestHit.collider.name}\nDist: {closestHit.distance:F2}");
            }

            // Метка у origin
            Handles.Label(origin + Vector3.up * 0.5f, 
                $"{castType} Cast\nDist: {castDistance:F2}");
        }

        private void DrawRayVisualization(Vector3 origin, Vector3 endPoint)
        {
            // Уже нарисована основная линия
            Gizmos.color = rayColor;
            Gizmos.DrawWireSphere(origin, 0.05f);
        }

        private void DrawSphereVisualization(Vector3 origin, Vector3 endPoint, Vector3 direction)
        {
            Gizmos.color = new Color(rayColor.r, rayColor.g, rayColor.b, 0.3f);
            
            // Сфера в начале
            Gizmos.DrawWireSphere(origin, radius);
            
            // Сфера в конце
            Gizmos.DrawWireSphere(endPoint, radius);
            
            // Промежуточные сферы для показа траектории
            int steps = 5;
            for (int i = 1; i < steps; i++)
            {
                float t = i / (float)steps;
                Vector3 pos = Vector3.Lerp(origin, endPoint, t);
                Gizmos.DrawWireSphere(pos, radius);
            }
        }

        private void DrawBoxVisualization(Vector3 origin, Vector3 endPoint, Vector3 direction)
        {
            Gizmos.color = new Color(rayColor.r, rayColor.g, rayColor.b, 0.3f);
            
            // Бокс в начале
            Gizmos.matrix = Matrix4x4.TRS(origin, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, boxSize);
            
            // Бокс в конце
            Gizmos.matrix = Matrix4x4.TRS(endPoint, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, boxSize);
            
            // Промежуточные боксы
            int steps = 3;
            for (int i = 1; i < steps; i++)
            {
                float t = i / (float)steps;
                Vector3 pos = Vector3.Lerp(origin, endPoint, t);
                Gizmos.matrix = Matrix4x4.TRS(pos, transform.rotation, Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, boxSize);
            }
            
            Gizmos.matrix = Matrix4x4.identity;
        }

        private void DrawCapsuleVisualization(Vector3 origin, Vector3 endPoint, Vector3 direction)
        {
            Gizmos.color = new Color(rayColor.r, rayColor.g, rayColor.b, 0.3f);
            
            Vector3 point1 = origin;
            Vector3 point2 = origin + transform.up * radius * 2f;
            
            // Капсула в начале
            DrawWireCapsule(point1, point2, radius);
            
            // Капсула в конце
            Vector3 endPoint1 = endPoint;
            Vector3 endPoint2 = endPoint + transform.up * radius * 2f;
            DrawWireCapsule(endPoint1, endPoint2, radius);
            
            // Промежуточные капсулы
            int steps = 3;
            for (int i = 1; i < steps; i++)
            {
                float t = i / (float)steps;
                Vector3 p1 = Vector3.Lerp(point1, endPoint1, t);
                Vector3 p2 = Vector3.Lerp(point2, endPoint2, t);
                DrawWireCapsule(p1, p2, radius);
            }
        }

        private void DrawWireCapsule(Vector3 p1, Vector3 p2, float radius)
        {
            // Сферы на концах
            Gizmos.DrawWireSphere(p1, radius);
            Gizmos.DrawWireSphere(p2, radius);
            
            // Линии соединяющие сферы
            Vector3 forward = (p2 - p1).normalized;
            Vector3 right = Vector3.Cross(forward, Vector3.up).normalized;
            if (right == Vector3.zero) right = Vector3.right;
            Vector3 up = Vector3.Cross(right, forward).normalized;
            
            Gizmos.DrawLine(p1 + right * radius, p2 + right * radius);
            Gizmos.DrawLine(p1 - right * radius, p2 - right * radius);
            Gizmos.DrawLine(p1 + up * radius, p2 + up * radius);
            Gizmos.DrawLine(p1 - up * radius, p2 - up * radius);
        }

        [CustomEditor(typeof(RaycastVisualizer))]
        public class RaycastVisualizerEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                DrawDefaultInspector();
                
                EditorGUILayout.Space();
                
                RaycastVisualizer visualizer = (RaycastVisualizer)target;
                
                if (GUILayout.Button("Perform Cast", GUILayout.Height(30)))
                {
                    visualizer.ManualCast();
                }
                
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Results", EditorStyles.boldLabel);
                
                if (visualizer.hits.Count > 0)
                {
                    foreach (var hit in visualizer.hits)
                    {
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        EditorGUILayout.LabelField("Object:", hit.collider.name);
                        EditorGUILayout.LabelField("Distance:", hit.distance.ToString("F2"));
                        EditorGUILayout.LabelField("Point:", hit.point.ToString());
                        EditorGUILayout.LabelField("Normal:", hit.normal.ToString());
                        EditorGUILayout.EndVertical();
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("No hits detected", MessageType.Info);
                }
            }
        }
#endif
    }
}