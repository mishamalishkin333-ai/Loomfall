using UnityEngine;
using UnityEngine.InputSystem;

namespace Loomfall
{
    /// <summary>Нить в Сердце Ткани: шаг к одной из трёх решает, чем кончится ярус.</summary>
    public class HeartChoice : MonoBehaviour
    {
        public Transform player;
        public int index;
        public float radius = 2.6f;

        private static readonly string[] ChoiceEndings = { "Сброс", "Разрыв", "Третий узор" };

        private void Awake()
        {
            var skin = GetComponent<Renderer>();
            if (skin == null) return;
            Color tint = index == 0
                ? new Color(0.85f, 0.32f, 0.3f)
                : index == 1
                    ? new Color(0.35f, 0.55f, 0.95f)
                    : new Color(0.95f, 0.93f, 0.84f);
            skin.material.color = tint;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || player == null) return;
            GameSession session = GameSession.Instance;
            if (session == null || !string.IsNullOrEmpty(session.Ending)) return;
            if (Vector3.Distance(player.position, transform.position) > radius) return;
            if (!keyboard.eKey.wasPressedThisFrame) return;

            int safe = Mathf.Clamp(index, 0, ChoiceEndings.Length - 1);
            if (!session.ChooseEnding(ChoiceEndings[safe]))
            {
                session.Shout("Отказ");
                return;
            }

            Debug.Log("[Loomfall] Сердце Ткани: выбран «" + ChoiceEndings[safe] + "»");
        }
    }
}