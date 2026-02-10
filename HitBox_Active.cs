using UnityEngine;

public class HitBox_Active : MonoBehaviour
{
    [Header("BoxActive Settings")]
    [SerializeField] protected LayerMask detectionMask = ~0;
    [SerializeField] protected Vector3 boxSize = Vector3.one;

    public struct HitBoxData
    {
        public bool hasHit;
        public GameObject hitObject;
        public string hitTag;
        public Vector3 hitPoint;
        public Vector3 hitNormal;
        public float distance;
    }
    protected HitBoxData hitBoxData;

    protected virtual void Start()
    {
        hitBoxData = new HitBoxData();
    }

    public abstract bool IsHitBoxColides();

    protected HitBoxData hitBoxData()
    {
        if (autoUpdateRaycasts)
        {
            PerformHitBoxDetection();
        }
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        HitBox_Active otherHitBox = other.GetComponent<HitBox_Active>();
        if (otherHitBox != null)
        {
            hitBoxData.hasHit = true;
            hitBoxData.hitObject = other.gameObject;
            hitBoxData.hitTag = other.tag;
            hitBoxData.hitPoint = other.ClosestPoint(transform.position);
            hitBoxData.hitNormal = (transform.position - hitBoxData.hitPoint).normalized;
            hitBoxData.distance = Vector3.Distance(transform.position, hitBoxData.hitPoint);
        }
    }

    protected virtual void OnTriggerStay(Collider other)
    {
        HitBox_Active otherHitBox = other.GetComponent<HitBox_Active>();
        if (otherHitBox != null && !hitBoxData.hasHit)
        {
            OnTriggerEnter(other);
        }
    }

    protected virtual void OnTriggerExit(Collider other)
    {
        HitBox_Active otherHitBox = other.GetComponent<HitBox_Active>();
        if (otherHitBox != null)
        {
            hitBoxData.hasHit = false;
            hitBoxData.hitObject = null;
        }
    }

    public virtual void RegisterRaycastHit(HitBoxData rayHitData)
    {
        if (rayHitData.hasHit)
        {
            hitBoxData = rayHitData;
            hitBoxData.hasHit = true;
        }
    }
}
