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

            Check("заставка: без нажатия не закрывается", !Loomfall.IntroScreen.ShouldClose(false, 5f));
            Check("заставка: случайное касание сразу после загрузки не закрывает", !Loomfall.IntroScreen.ShouldClose(true, 0.1f));
            Check("заставка: нажатие через полсекунды закрывает", Loomfall.IntroScreen.ShouldClose(true, 0.5f));
            Check("заставка: ровно на пороге закрывает", Loomfall.IntroScreen.ShouldClose(true, 0.35f));

            // Раскладка кнопок на телефоне: не вылезать за экран, не пересекаться, быть крупными.
            var sizes = new[]
            {
                new Vector2(360f, 640f), new Vector2(640f, 360f), new Vector2(320f, 568f),
                new Vector2(1280f, 720f), new Vector2(2400f, 1080f)
            };
            bool inside = true;
            bool apart = true;
            bool big = true;
            bool clear = true;
            string worst = "нет";
            foreach (Vector2 size in sizes)
            {
                Vector2 jump;
                Vector2 thread;
                Vector2 word;
                Vector2 rift;
                float r = Loomfall.TouchControls.Layout(size.x, size.y, 0.075f, out jump, out thread, out word, out rift);
                Vector2[] points = { jump, thread, word, rift };

                foreach (Vector2 point in points)
                {
                    if (point.x - r < 0f || point.y - r < 0f || point.x + r > size.x || point.y + r > size.y)
                    {
                        inside = false;
                        worst = size.ToString("F0") + " кнопка " + point.ToString("F0");
                    }
                }

                for (int i = 0; i < points.Length; i++)
                {
                    for (int j = i + 1; j < points.Length; j++)
                    {
                        if (Vector2.Distance(points[i], points[j]) < r * 2f)
                        {
                            apart = false;
                            worst = size.ToString("F0") + " пара " + i + "/" + j;
                        }
                    }
                }

                if (r * 2f < 44f)
                {
                    big = false;
                    worst = size.ToString("F0") + " поперечник " + (r * 2f).ToString("F1");
                }

                // Круг хождения под большим пальцем не должен налезать на кнопки.
                float stickR = Mathf.Min(size.x, size.y) * 0.15f;
                float stickEdge = stickR * 1.3f + stickR;
                float nearestButton = Mathf.Min(Mathf.Min(points[0].x, points[1].x), Mathf.Min(points[2].x, points[3].x)) - r;
                if (stickEdge > nearestButton)
                {
                    clear = false;
                    worst = size.ToString("F0") + " круг хождения " + stickEdge.ToString("F0")
                        + " против кнопок " + nearestButton.ToString("F0");
                }
            }

            Check("кнопки не вылезают за экран на всех размерах", inside, worst);
            Check("кнопки не перекрывают друг друга", apart, worst);
            Check("кнопки не мельче 44 точек в поперечнике", big, worst);
            Check("круг хождения не налезает на кнопки", clear, worst);

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
