using UnityEngine;

namespace NeonVeil
{
    public class FollowCamera2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector2 offset = new Vector2(2f, 1.5f);
        [SerializeField] private float smoothTime = 0.15f;
        [SerializeField] private float minimumY = 2f;
        private Vector3 velocity;

        public void SetTarget(Transform player) { target = player; }

        private void Start()
        {
            SnapToTarget();
        }

        private void LateUpdate()
        {
            if (target == null) return;
            transform.position = Vector3.SmoothDamp(transform.position, TargetPosition(), ref velocity, smoothTime);
        }

        public void SnapToTarget()
        {
            if (target == null) return;
            velocity = Vector3.zero;
            transform.position = TargetPosition();
        }

        private Vector3 TargetPosition()
        {
            return new Vector3(target.position.x + offset.x,
                Mathf.Max(minimumY, target.position.y + offset.y), transform.position.z);
        }
    }
}
