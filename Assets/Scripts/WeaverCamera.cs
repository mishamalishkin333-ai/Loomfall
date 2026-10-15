using UnityEngine;

namespace Loomfall
{
    /// <summary>Камера от третьего лица: держится за спиной Ткача и плавно догоняет его.</summary>
    public class WeaverCamera : MonoBehaviour
    {
        public Transform target;
        public float distance = 5.1f;
        public float height = 2.35f;
        public float lookHeight = 1.25f;
        public float followSpeed = 6f;

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 behind = target.position - target.forward * distance + Vector3.up * height;
            transform.position = Vector3.Lerp(transform.position, behind, followSpeed * Time.deltaTime);
            transform.LookAt(target.position + Vector3.up * lookHeight);
        }
    }
}