using UnityEngine;
using UnityEngine.InputSystem;

namespace Loomfall
{
    /// <summary>
    /// Стартовая карточка: завязка, управление и кнопка «ИГРАТЬ».
    /// Она не закрывает весь экран — мир и Ткач должны быть видны сразу,
    /// иначе игра выглядит мёртвой. Пока карточка видна, ввод игры заблокирован.
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

        /// <summary>Через сколько секунд карточка уходит сама.</summary>
        public const float AutoCloseAfter = 12f;

        /// <summary>
        /// Карточка обязана уйти сама: если игрок ничего не нажал и не кликнет
        /// (окно браузера не в фокусе), игра не превращается в вечный тёмный экран.
        /// </summary>
        public static bool ShouldAutoClose(float shown)
        {
            return shown >= AutoCloseAfter;
        }

        /// <summary>Сколько секунд осталось до автоматического закрытия.</summary>
        public static float AutoCloseIn(float shown)
        {
            return Mathf.Max(0f, AutoCloseAfter - shown);
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

            // Мышь — самый частый способ «нажать» в браузере на компьютере.
            // Без неё карточка не закрывалась ни кликом, и игра выглядела мёртвой.
            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame) pressed = true;

            Touchscreen screen = Touchscreen.current;
            if (screen != null && screen.primaryTouch.press.wasPressedThisFrame) pressed = true;

            if (ShouldClose(pressed, shown) || ShouldAutoClose(shown)) Dismiss();
        }

        /// <summary>Закрыть карточку сразу: так её снимает проверка и кнопка «ИГРАТЬ».</summary>
        public void Dismiss()
        {
            if (!Visible) return;
            Visible = false;
            Debug.Log("[Loomfall] Заставка закрыта на " + shown.ToString("F2") + " с — игра началась");
        }

        /// <summary>Геометрия карточки: единственный источник правды о её размере.</summary>
        public static Rect PanelRect(float screenWidth, float screenHeight)
        {
            float width = Mathf.Min(screenWidth * 0.92f, 640f);
            float height = Mathf.Min(screenHeight * 0.4f, 268f);
            return new Rect((screenWidth - width) * 0.5f, screenHeight - height - 14f, width, height);
        }

        private void OnGUI()
        {
            if (!Visible) return;

            bool touch = TouchControls.IsTouchDevice();

            // Карточка внизу экрана: верхние две трети остаются за миром и Ткачом.
            Rect panel = PanelRect(Screen.width, Screen.height);
            float width = panel.width;
            float height = panel.height;

            Color previous = GUI.color;
            GUI.color = new Color(0.04f, 0.05f, 0.07f, 0.9f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = new Color(0.62f, 0.68f, 0.84f, 0.7f);
            GUI.DrawTexture(new Rect(panel.x, panel.y, panel.width, 2f), Texture2D.whiteTexture);
            GUI.color = previous;

            float x = panel.x + 20f;
            float inner = panel.width - 40f;

            var title = new GUIStyle(GUI.skin.label);
            title.fontSize = Mathf.RoundToInt(Mathf.Min(inner * 0.075f, 34f));
            title.fontStyle = FontStyle.Bold;
            title.normal.textColor = new Color(0.97f, 0.92f, 0.78f);
            GUI.Label(new Rect(x, panel.y + 12f, inner, title.fontSize + 8f), "Ткач Ярусов", title);

            var subtitle = new GUIStyle(GUI.skin.label);
            subtitle.fontSize = Mathf.RoundToInt(Mathf.Min(inner * 0.028f, 15f));
            subtitle.normal.textColor = new Color(0.66f, 0.79f, 0.95f);
            GUI.Label(new Rect(x, panel.y + 12f + title.fontSize + 6f, inner, 22f),
                "Ярусы памяти. Из семи осталось пять.", subtitle);

            var body = new GUIStyle(GUI.skin.label);
            body.fontSize = Mathf.RoundToInt(Mathf.Min(inner * 0.03f, 16f));
            body.wordWrap = true;
            body.normal.textColor = new Color(0.87f, 0.88f, 0.92f);
            float storyTop = panel.y + 12f + title.fontSize + 30f;
            GUI.Label(new Rect(x, storyTop, inner, height * 0.3f),
                "Забвение съедает город сверху вниз: где кончилась память, там не осталось и камня. "
                + "Собирайте осколки, зажигайте светильники, натягивайте нити — и поднимайтесь, "
                + "пока ярусы ещё помнят себя.", body);

            var hint = new GUIStyle(GUI.skin.label);
            hint.fontSize = Mathf.RoundToInt(Mathf.Min(inner * 0.026f, 14f));
            hint.wordWrap = true;
            hint.normal.textColor = new Color(0.74f, 0.77f, 0.84f);
            GUI.Label(new Rect(x, panel.yMax - 92f, inner - 210f, 44f),
                touch
                    ? "Слева веди пальцем — идти. Справа кнопки: прыжок, нить, слово, разрыв."
                    : "WASD — идти, Shift — бежать, пробел — прыжок. E — нить, Q — Слово, F — Разрыв, Tab — кодекс.",
                hint);

            // Настоящая кнопка: игрок видит, куда нажимать, а не гадает.
            var button = new GUIStyle(GUI.skin.button);
            button.fontSize = Mathf.RoundToInt(Mathf.Min(inner * 0.035f, 20f));
            button.fontStyle = FontStyle.Bold;
            button.normal.textColor = new Color(0.06f, 0.07f, 0.1f);
            button.normal.background = Texture2D.whiteTexture;
            button.active.textColor = new Color(0.1f, 0.12f, 0.16f);
            button.hover.textColor = new Color(0.06f, 0.07f, 0.1f);
            Color tint = GUI.color;
            GUI.color = new Color(0.95f, 0.85f, 0.55f);
            Rect play = new Rect(panel.xMax - 20f - 190f, panel.yMax - 62f, 190f, 46f);
            if (GUI.Button(play, "ИГРАТЬ", button)) Dismiss();
            GUI.color = tint;

            var auto = new GUIStyle(GUI.skin.label);
            auto.fontSize = Mathf.RoundToInt(Mathf.Min(inner * 0.024f, 13f));
            auto.alignment = TextAnchor.MiddleRight;
            auto.normal.textColor = new Color(0.6f, 0.64f, 0.72f, 0.9f);
            GUI.Label(new Rect(x, panel.yMax - 40f, play.x - x - 12f, 22f),
                "или любая клавиша · сама через " + Mathf.CeilToInt(AutoCloseIn(shown)) + " с", auto);
        }
    }
}
