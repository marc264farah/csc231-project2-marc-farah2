using UnityEngine;

namespace NeonVeil
{
    // FollowCamera2D applies its movement in LateUpdate first.
    [DefaultExecutionOrder(100)]
    public class NeonParallax : MonoBehaviour
    {
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Transform farLayer;
        [SerializeField] private Transform middleLayer;
        [SerializeField] private Transform nearLayer;
        [SerializeField] private Vector2 farFollow = new Vector2(0.84f, 0.64f);
        [SerializeField] private Vector2 middleFollow = new Vector2(0.55f, 0.40f);
        [SerializeField] private Vector2 nearFollow = new Vector2(0.27f, 0.22f);

        private Vector3 lastCameraPosition;

        public void Setup(Transform camera, Transform far, Transform middle, Transform near)
        {
            cameraTransform = camera;
            farLayer = far;
            middleLayer = middle;
            nearLayer = near;
        }

        private void Start()
        {
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;

            if (cameraTransform == null)
            {
                enabled = false;
                return;
            }

            lastCameraPosition = cameraTransform.position;
        }

        private void LateUpdate()
        {
            Vector3 delta = cameraTransform.position - lastCameraPosition;
            lastCameraPosition = cameraTransform.position;

            MoveLayer(farLayer, delta, farFollow);
            MoveLayer(middleLayer, delta, middleFollow);
            MoveLayer(nearLayer, delta, nearFollow);
        }

        private static void MoveLayer(Transform layer, Vector3 delta, Vector2 follow)
        {
            if (layer != null)
                layer.position += new Vector3(delta.x * follow.x, delta.y * follow.y, 0f);
        }
    }
}
