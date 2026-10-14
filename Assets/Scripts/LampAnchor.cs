using UnityEngine;
using UnityEngine.InputSystem;

namespace Loomfall
{
    /// <summary>Светильник: якорь памяти. Зажжённый, он откатывает Забвение и держит свой сектор.</summary>
    public class LampAnchor : MonoBehaviour
    {
        public Transform player;
        public float radius = 3.5f;
        public float flameIntensity = 2.6f;
        public float flameRange = 9f;

        public bool IsLit { get; private set; }

        private Light flame;
        private float current;

        private void Awake()
        {
            flame = gameObject.AddComponent<Light>();
            flame.type = LightType.Point;
            flame.range = flameRange;
            flame.intensity = 0f;
            flame.color = new Color(1f, 0.82f, 0.55f);
            flame.shadows = LightShadows.Soft;
            flame.shadowStrength = 0.7f;
        }

        private void Update()
        {
            float target = IsLit ? flameIntensity : 0f;
            current = Mathf.MoveTowards(current, target, 2.5f * Time.deltaTime);
            flame.intensity = current;

            if (IsLit) return;
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || player == null) return;
            if (!keyboard.eKey.wasPressedThisFrame) return;
            if (Vector3.Distance(player.position, transform.position) > radius) return;

            IsLit = true;
            if (OblivionClock.Instance != null) OblivionClock.Instance.Ignite();
            Debug.Log("[Loomfall] Светильник зажжён: " + name);
        }
    }
}