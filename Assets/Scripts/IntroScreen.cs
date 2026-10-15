using UnityEngine;
using UnityEngine.InputSystem;

namespace Loomfall
{
    /// <summary>
    /// Экран начала: завязка, управление и ожидание первого касания.
    /// Пока он виден, игра не принимает ввод — иначе Ткач уходил бы со старта сам.
    /// </summary>
    public class IntroScreen : MonoBehaviour
    {
        public static IntroScreen Instance { get; private set; }

        /// <summary>Заставка видна — ввод игры заблокирован.</summary>
        public static bool BlocksInput
        {
            get { return Instance != null && Instance.Visible; }
        }

        public bool Visible { get; private set; } = true;

        /// <summary>Пробное нажатие для проверок: в игре не используется.</summary>
        public bool testPress;

        private float shown;

        /// <summary>
        /// Закрываем не раньше 0,35 с: иначе случайное касание при загрузке
        /// проматывало бы заставку, и игрок не успевал прочитать, что происходит.
        /// </summary>
        public static bool ShouldClose(bool pressed, float shown)
        {
            return pressed && shown >= 0.35f;
        }

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            if (!Visible) return;
            shown += Time.deltaTime;

            bool pressed = testPress;
            testPress = false;

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.anyKey.wasPressedThisFrame) pressed = true;

            Touchscreen screen = Touchscreen.current;
            if (screen != null && screen.primaryTouch.press.wasPressedThisFrame) pressed = true;

            if (ShouldClose(pressed, shown)) Dismiss();
        }

        /// <summary>Закрыть заставку сразу: так её снимает проверка и кнопка «продолжить».</summary>
        public void Dismiss()
        {
            if (!Visible) return;
            Visible = false;
            Debug.Log("[Loomfall] Заставка закрыта на " + shown.ToString("F2") + " с — игра началась");
        }

        private void OnGUI()
        {
            if (!Visible) return;

            bool touch = TouchControls.IsTouchDevice();
            float width = Mathf.Min(Screen.width * 0.86f, 720f);
            float height = Mathf.Min(Screen.height * 0.86f, 460f);
            var panel = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);

            Color previous = GUI.color;
            GUI.color = new Color(0.04f, 0.05f, 0.07f, 0.94f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = previous;

            var title = new GUIStyle(GUI.skin.label);
            title.alignment = TextAnchor.UpperCenter;
            title.fontSize = Mathf.RoundToInt(Mathf.Min(width * 0.072f, 40f));
            title.normal.textColor = new Color(0.97f, 0.93f, 0.84f);
            GUI.Label(new Rect(panel.x, panel.y + panel.height * 0.07f, panel.width, 46f), "Ткач Ярусов", title);

            var body = new GUIStyle(GUI.skin.label);
            body.alignment = TextAnchor.UpperCenter;
            body.fontSize = Mathf.RoundToInt(Mathf.Min(width * 0.032f, 19f));
            body.wordWrap = true;
            body.normal.textColor = new Color(0.86f, 0.87f, 0.9f);
            GUI.Label(new Rect(panel.x + panel.width * 0.08f, panel.y + panel.height * 0.24f, panel.width * 0.84f, panel.height * 0.34f),
                "Из семи ярусов осталось пять. Забвение съедает город сверху вниз: где кончилась "
                + "память, там не осталось и камня.\n\n"
                + "Ты — Ткач. Собирай осколки памяти, зажигай светильники, натягивай нити "
                + "и поднимайся, пока ярусы ещё помнят себя.", body);

            var hint = new GUIStyle(GUI.skin.label);
            hint.alignment = TextAnchor.MiddleCenter;
            hint.fontSize = Mathf.RoundToInt(Mathf.Min(width * 0.028f, 17f));
            hint.normal.textColor = new Color(0.78f, 0.82f, 0.88f);
            GUI.Label(new Rect(panel.x, panel.y + panel.height * 0.63f, panel.width, 24f),
                touch
                    ? "Слева веди пальцем — идти, справа кнопки: прыжок, нить, слово, разрыв"
                    : "WASD — идти, Shift — бежать, пробел — прыжок, E — нить, Q — слово, F — разрыв, Tab — кодекс", hint);

            var prompt = new GUIStyle(GUI.skin.label);
            prompt.alignment = TextAnchor.MiddleCenter;
            prompt.fontSize = Mathf.RoundToInt(Mathf.Min(width * 0.036f, 22f));
            float pulse = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 3f);
            prompt.normal.textColor = new Color(1f, 0.9f, 0.72f, Mathf.Clamp01(pulse));
            GUI.Label(new Rect(panel.x, panel.y + panel.height * 0.82f, panel.width, 30f),
                touch ? "коснитесь экрана, чтобы начать" : "нажмите любую клавишу, чтобы начать", prompt);
        }
    }
}
