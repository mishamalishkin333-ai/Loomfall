using UnityEditor;
using UnityEngine;

namespace Loomfall.EditorTools
{
    /// <summary>Проверка сенсорного управления без телефона: считается отклонение пальца и попадание в кнопки.</summary>
    public static class LoomfallTouch
    {
        private static int passed;
        private static int failed;

        public static void Run()
        {
            passed = 0;
            failed = 0;

            Vector2 center = new Vector2(200f, 500f);
            const float radius = 100f;

            Check("палец в центре — движения нет", Loomfall.TouchControls.Stick(center, center, radius) == Vector2.zero);

            Vector2 half = Loomfall.TouchControls.Stick(center, center + new Vector2(radius * 0.5f, 0f), radius);
            Check("половина хода вправо — 0,5 по X", Mathf.Abs(half.x - 0.5f) < 0.01f && Mathf.Abs(half.y) < 0.01f,
                half.ToString("F3"));

            Vector2 up = Loomfall.TouchControls.Stick(center, center + new Vector2(0f, radius), radius);
            Check("палец вверх — идём вперёд", Mathf.Abs(up.y - 1f) < 0.01f && Mathf.Abs(up.x) < 0.01f, up.ToString("F3"));

            Vector2 down = Loomfall.TouchControls.Stick(center, center + new Vector2(0f, -radius), radius);
            Check("палец вниз — идём назад", Mathf.Abs(down.y + 1f) < 0.01f, down.ToString("F3"));

            Vector2 far = Loomfall.TouchControls.Stick(center, center + new Vector2(radius * 3f, 0f), radius);
            Check("палец за краем — ход всё равно 1", Mathf.Abs(far.magnitude - 1f) < 0.01f, far.ToString("F3"));

            Vector2 tinytouch = Loomfall.TouchControls.Stick(center, center + new Vector2(radius * 0.04f, 0f), radius);
            Check("дрожание пальца — не движение", tinytouch == Vector2.zero, tinytouch.ToString("F3"));

            Vector2 diagonal = Loomfall.TouchControls.Stick(center, center + new Vector2(radius, radius), radius);
            Check("по диагонали длина не больше 1", diagonal.magnitude <= 1.001f && diagonal.magnitude > 0.99f,
                diagonal.ToString("F3"));

            Check("нулевой радиус — не делим на ноль", Loomfall.TouchControls.Stick(center, center + Vector2.one, 0f) == Vector2.zero);

            Check("попадание в кнопку", Loomfall.TouchControls.InCircle(new Vector2(210f, 500f), center, radius));
            Check("промах мимо кнопки", !Loomfall.TouchControls.InCircle(new Vector2(400f, 500f), center, radius));
            Check("на самой границе кнопки — попадание", Loomfall.TouchControls.InCircle(center + new Vector2(radius, 0f), center, radius));

            string[] levels = { "Mobile", "PC" };
            Check("телефон — профиль качества Mobile", Loomfall.GameSession.ChooseLevel(true, levels) == 0);
            Check("компьютер — профиль качества PC", Loomfall.GameSession.ChooseLevel(false, levels) == 1);
            Check("телефон без мобильного профиля — берём PC", Loomfall.GameSession.ChooseLevel(true, new[] { "PC" }) == 0);
            Check("компьютер без профиля PC — берём мобильный", Loomfall.GameSession.ChooseLevel(false, new[] { "Mobile" }) == 0);
            Check("пустой список профилей — ничего не выбираем", Loomfall.GameSession.ChooseLevel(true, new string[0]) == -1);

            Debug.Log("[Loomfall] Сенсорное управление: успешно " + passed + ", сбоев " + failed);
            EditorApplication.Exit(failed == 0 ? 0 : 1);
        }

        private static void Check(string name, bool ok, string detail = "")
        {
            if (ok)
            {
                passed++;
                Debug.Log("[Loomfall]   ок   " + name);
            }
            else
            {
                failed++;
                Debug.Log("[Loomfall]   СБОЙ " + name + (string.IsNullOrEmpty(detail) ? "" : " — " + detail));
            }
        }
    }
}
