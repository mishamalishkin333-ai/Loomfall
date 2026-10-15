using UnityEngine;
using UnityEngine.InputSystem;

namespace Loomfall
{
    /// <summary>
    /// Сенсорное управление для телефонов: палец слева ведёт Ткача, справа — прыжок,
    /// нить, Слово и Разрыв. На устройствах без сенсора экран не рисуется.
    /// </summary>
    public class TouchControls : MonoBehaviour
    {
        public WeaverController weaver;
        public float stickRadius = 0.15f;
        public float buttonRadius = 0.075f;
        public float runAt = 0.85f;

        private int stickTouch = -1;
        private Vector2 stickCenter;
        private Texture2D circle;

        /// <summary>Отклонение пальца от центра: длина не больше 1, вверх — вперёд.</summary>
        public static Vector2 Stick(Vector2 center, Vector2 point, float radius)
        {
            if (radius <= 0.0001f) return Vector2.zero;
            Vector2 offset = point - center;
            float length = offset.magnitude / radius;
            if (length < 0.08f) return Vector2.zero;
            float clamped = Mathf.Min(1f, length);
            Vector2 direction = offset.normalized;
            return new Vector2(direction.x, direction.y) * clamped;
        }

        /// <summary>Попадание пальца в круг кнопки.</summary>
        public static bool InCircle(Vector2 point, Vector2 center, float radius)
        {
            return (point - center).sqrMagnitude <= radius * radius;
        }

        /// <summary>Есть ли на устройстве сенсор: на компьютере управление с клавиатуры.</summary>
        public static bool IsTouchDevice()
        {
            return Touchscreen.current != null;
        }

        private void Update()
        {
            if (weaver == null) weaver = WeaverController.Instance;
            Touchscreen screen = Touchscreen.current;

            // Без сенсорного экрана ввод не трогаем: на компьютере им управляет клавиатура,
            // а в проверках — пробный ввод. Иначе чужие значения затирались бы каждый кадр.
            if (weaver == null || screen == null) return;

            // На заставке кнопки не работают: касание только закрывает её.
            if (IntroScreen.BlocksInput)
            {
                weaver.touchMove = Vector2.zero;
                weaver.touchJump = false;
                weaver.touchSprint = false;
                return;
            }

            Vector2 jumpCenter;
            Vector2 threadCenter;
            Vector2 wordCenter;
            Vector2 riftCenter;
            float buttonR = Layout(out jumpCenter, out threadCenter, out wordCenter, out riftCenter);

            Vector2 move = Vector2.zero;
            bool jump = false;
            bool thread = false;
            bool word = false;
            bool rift = false;

            foreach (var touch in screen.touches)
            {
                int id = touch.touchId.ReadValue();
                if (!touch.press.isPressed)
                {
                    if (id == stickTouch) stickTouch = -1;
                    continue;
                }

                Vector2 raw = touch.position.ReadValue();
                Vector2 point = new Vector2(raw.x, Screen.height - raw.y);

                if (point.x < Screen.width * 0.5f)
                {
                    if (stickTouch == -1)
                    {
                        stickTouch = id;
                        stickCenter = point;
                    }

                    if (stickTouch == id)
                    {
                        float unit = Mathf.Min(Screen.width, Screen.height);
                        move = Stick(stickCenter, point, unit * stickRadius);
                    }

                    continue;
                }

                if (InCircle(point, jumpCenter, buttonR)) jump = true;
                else if (InCircle(point, threadCenter, buttonR)) thread = true;
                else if (InCircle(point, wordCenter, buttonR)) word = true;
                else if (InCircle(point, riftCenter, buttonR)) rift = true;
            }

            weaver.touchMove = move;
            weaver.touchSprint = move.magnitude >= runAt;
            weaver.touchJump = jump;

            if (word && WeaverWord.Instance != null) WeaverWord.Instance.testPress = true;
            if (rift && WeaverWord.Instance != null) WeaverWord.Instance.testRift = true;
            if (thread || word) PressThreads();
        }

        /// <summary>
        /// Круги кнопок на экране. Считается от размеров экрана, а не от Screen напрямую,
        /// чтобы раскладку можно было проверить тестом на разных телефонах.
        /// </summary>
        public static float Layout(float width, float height, float buttonRadius, out Vector2 jump, out Vector2 thread, out Vector2 word, out Vector2 rift)
        {
            float unit = Mathf.Min(width, height);
            float buttonR = unit * buttonRadius;
            float right = width - buttonR * 1.6f;
            float bottom = height - buttonR * 1.6f;

            jump = new Vector2(right, bottom);
            thread = new Vector2(right - buttonR * 2.6f, bottom);
            word = new Vector2(right, bottom - buttonR * 2.6f);
            rift = new Vector2(right - buttonR * 2.6f, bottom - buttonR * 2.6f);
            return buttonR;
        }

        /// <summary>Круги кнопок для текущего экрана.</summary>
        private float Layout(out Vector2 jump, out Vector2 thread, out Vector2 word, out Vector2 rift)
        {
            return Layout(Screen.width, Screen.height, buttonRadius, out jump, out thread, out word, out rift);
        }

        private static void PressThreads()
        {
            foreach (HeartChoice choice in Object.FindObjectsByType<HeartChoice>(FindObjectsSortMode.None))
            {
                if (choice == null || choice.player == null) continue;
                if (Vector3.Distance(choice.player.position, choice.transform.position) <= choice.radius)
                {
                    choice.testPress = true;
                }
            }
        }

        private void OnGUI()
        {
            if (!IsTouchDevice() || weaver == null || IntroScreen.BlocksInput) return;

            Vector2 jump;
            Vector2 thread;
            Vector2 word;
            Vector2 rift;
            float buttonR = Layout(out jump, out thread, out word, out rift);
            float unit = Mathf.Min(Screen.width, Screen.height);
            float stickR = unit * stickRadius;
            float left = stickR * 1.3f;
            float bottomY = Screen.height - stickR * 1.3f;

            Circle();
            Draw(new Rect(left - stickR, bottomY - stickR, stickR * 2f, stickR * 2f), new Color(1f, 1f, 1f, 0.16f));

            var style = new GUIStyle(GUI.skin.label);
            style.alignment = TextAnchor.MiddleCenter;
            style.normal.textColor = new Color(1f, 1f, 1f, 0.75f);

            Vector2 thumb = stickCenter;
            if (stickTouch == -1) thumb = new Vector2(left, bottomY);
            Draw(new Rect(thumb.x - stickR * 0.32f, thumb.y - stickR * 0.32f, stickR * 0.64f, stickR * 0.64f),
                new Color(1f, 0.94f, 0.82f, 0.4f));

            DrawButton(jump, buttonR, "прыжок");
            DrawButton(thread, buttonR, "нить");
            DrawButton(word, buttonR, "слово");
            DrawButton(rift, buttonR, "разрыв");

            GUI.Label(new Rect(left - stickR, bottomY + stickR + 6f, stickR * 2f, 26f), "идти", style);
        }

        private void DrawButton(Vector2 center, float radius, string label)
        {
            Draw(new Rect(center.x - radius, center.y - radius, radius * 2f, radius * 2f), new Color(1f, 0.92f, 0.8f, 0.22f));
            var style = new GUIStyle(GUI.skin.label);
            style.alignment = TextAnchor.MiddleCenter;
            style.fontSize = Mathf.RoundToInt(radius * 0.42f);
            style.normal.textColor = new Color(1f, 1f, 1f, 0.8f);
            GUI.Label(new Rect(center.x - radius, center.y - 12f, radius * 2f, 24f), label, style);
        }

        private void Draw(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, circle, ScaleMode.StretchToFill, true);
            GUI.color = previous;
        }

        /// <summary>Мягкий круг без картинок: рисуется один раз и растягивается.</summary>
        private void Circle()
        {
            if (circle != null) return;
            const int size = 64;
            circle = new Texture2D(size, size, TextureFormat.RGBA32, false);
            circle.wrapMode = TextureWrapMode.Clamp;
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(half, half)) / half;
                    float alpha = Mathf.Clamp01(1f - Mathf.Max(0f, distance - 0.75f) * 4f);
                    if (distance > 1f) alpha = 0f;
                    circle.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            circle.Apply();
        }
    }
}
