using UnityEngine;

namespace Loomfall
{
    /// <summary>
    /// Экранный слой игры: панель состояния слева, цель и компас справа,
    /// контекстная подсказка снизу, кодекс и концовка по центру.
    /// Рисуется всегда, в том числе под заставкой, — иначе игрок не видит интерфейса.
    /// </summary>
    public class WeaverHud : MonoBehaviour
    {
        public Transform player;
        public Transform loom;
        public float loomRadius = 4.5f;

        private GUIStyle title;
        private GUIStyle body;
        private GUIStyle small;
        private GUIStyle accent;
        private GUIStyle number;
        private Texture2D plate;
        private Texture2D vignette;

        // Кэш ближайшей цели: перебирать всю сцену в OnGUI слишком дорого.
        private Vector3 targetPosition;
        private string targetLabel = "двор";
        private float targetDistance;
        private float refresh;

        private void Update()
        {
            refresh -= Time.deltaTime;
            if (refresh > 0f) return;
            refresh = 0.3f;
            FindTarget();
        }

        private void FindTarget()
        {
            Vector3 from = player != null ? player.position : transform.position;
            targetPosition = from;
            targetDistance = float.MaxValue;
            targetLabel = "узла не видно";

            if (loom != null)
            {
                targetPosition = loom.position;
                targetLabel = "Ткацкий узел";
                targetDistance = Vector3.Distance(from, loom.position);
            }

            foreach (LampAnchor lamp in Object.FindObjectsByType<LampAnchor>(FindObjectsSortMode.None))
            {
                if (lamp.IsLit) continue;
                float distance = Vector3.Distance(from, lamp.transform.position);
                if (distance >= targetDistance) continue;
                targetDistance = distance;
                targetPosition = lamp.transform.position;
                targetLabel = "Светильник";
            }

            foreach (MemoryShard shard in Object.FindObjectsByType<MemoryShard>(FindObjectsSortMode.None))
            {
                if (shard.Taken) continue;
                float distance = Vector3.Distance(from, shard.transform.position);
                if (distance >= targetDistance) continue;
                targetDistance = distance;
                targetPosition = shard.transform.position;
                targetLabel = "Осколок памяти";
            }

            foreach (HeartChoice thread in Object.FindObjectsByType<HeartChoice>(FindObjectsSortMode.None))
            {
                float distance = Vector3.Distance(from, thread.transform.position);
                if (distance >= targetDistance) continue;
                targetDistance = distance;
                targetPosition = thread.transform.position;
                targetLabel = "Нить «" + thread.name + "»";
            }

            if (targetDistance == float.MaxValue) targetDistance = 0f;
        }

        private void OnGUI()
        {
            BuildStyles();

            GameSession session = GameSession.Instance;
            OblivionClock clock = OblivionClock.Instance;
            WeaverWord word = WeaverWord.Instance;

            float oblivion = clock != null ? clock.Oblivion : 0f;
            int litLamps = clock != null ? clock.LitLamps : 0;
            int tier = session != null ? session.Tier : 1;
            int threads = session != null ? session.Threads : 0;
            int need = session != null ? session.threadsPerTier : 3;
            int shards = session != null ? session.Shards : 0;

            // Виньетка забвения: чем ближе конец, тем сильнее края давят на игрока.
            if (oblivion > 0.35f && vignette != null)
            {
                Color tint = GUI.color;
                GUI.color = new Color(0.5f, 0.08f, 0.12f, Mathf.Clamp01((oblivion - 0.35f) * 1.6f));
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), vignette);
                GUI.color = tint;
            }

            PanelState(tier, threads, need, shards, litLamps, oblivion, word);
            PanelTarget();
            PanelHint(session, word);

            if (session != null && !string.IsNullOrEmpty(session.Ending))
            {
                Banner("Концовка: «" + session.Ending + "»", "двор пройден, нить выбрана");
            }
            else if (session != null && Time.time - session.CodexTime < 3f && !string.IsNullOrEmpty(session.Codex))
            {
                Banner("Запись кодекса " + session.Shards, session.Codex);
            }

            if (session != null && session.CodexOpen)
            {
                Codex(session);
            }
        }

        private void BuildStyles()
        {
            if (title != null) return;

            title = new GUIStyle(GUI.skin.label);
            title.fontSize = 21;
            title.fontStyle = FontStyle.Bold;
            title.normal.textColor = new Color(0.96f, 0.9f, 0.72f);

            body = new GUIStyle(GUI.skin.label);
            body.fontSize = 15;
            body.normal.textColor = new Color(0.88f, 0.89f, 0.93f);

            small = new GUIStyle(GUI.skin.label);
            small.fontSize = 12;
            small.normal.textColor = new Color(0.7f, 0.73f, 0.8f);

            accent = new GUIStyle(GUI.skin.label);
            accent.fontSize = 14;
            accent.normal.textColor = new Color(0.66f, 0.79f, 0.95f);

            number = new GUIStyle(GUI.skin.label);
            number.fontSize = 15;
            number.fontStyle = FontStyle.Bold;
            number.alignment = TextAnchor.MiddleRight;
            number.normal.textColor = new Color(0.96f, 0.9f, 0.72f);

            plate = MakePlateTexture();
            vignette = MakeVignetteTexture();
        }

        /// <summary>Левая панель: ярус, забвение, эхо и счётчики добычи.</summary>
        private void PanelState(int tier, int threads, int need, int shards, int lamps, float oblivion, WeaverWord word)
        {
            float width = Mathf.Clamp(Screen.width * 0.24f, 250f, 330f);
            Rect panel = new Rect(14f, 14f, width, 168f);
            DrawPanel(panel);

            float x = panel.x + 12f;
            float y = panel.y + 8f;
            float inner = panel.width - 24f;

            GUI.Label(new Rect(x, y, inner, 26f), "Ткач Ярусов", title);
            y += 26f;
            GUI.Label(new Rect(x, y, inner, 20f), "Ярус " + tier + " из 5 · нитей " + threads + " из " + need, accent);
            y += 24f;

            Bar(new Rect(x, y, inner, 16f), oblivion, new Color(0.72f, 0.16f, 0.24f), "Забвение");
            GUI.Label(new Rect(x + inner - 60f, y - 2f, 60f, 20f), Mathf.RoundToInt(oblivion * 100f) + "%", number);
            y += 24f;

            float echo = word != null ? word.Echo / Mathf.Max(1f, word.maxEcho) : 0f;
            Bar(new Rect(x, y, inner, 16f), echo, new Color(0.24f, 0.5f, 0.78f), "Эхо");
            GUI.Label(new Rect(x + inner - 60f, y - 2f, 60f, 20f), word != null ? Mathf.RoundToInt(word.Echo) + "/100" : "—", number);
            y += 28f;

            Pips(new Rect(x, y, inner, 14f), shards, 8, new Color(0.86f, 0.72f, 0.3f));
            GUI.Label(new Rect(x + inner - 70f, y - 3f, 70f, 20f), "осколки " + shards + "/8", small);
            y += 20f;

            Pips(new Rect(x, y, inner, 14f), lamps, 7, new Color(0.95f, 0.6f, 0.2f));
            GUI.Label(new Rect(x + inner - 78f, y - 3f, 78f, 20f), "светильники " + lamps + "/7", small);
            y += 20f;

            Pips(new Rect(x, y, inner, 14f), threads, need, new Color(0.55f, 0.85f, 0.7f));
            GUI.Label(new Rect(x + inner - 60f, y - 3f, 60f, 20f), "нити " + threads + "/" + need, small);
        }

        /// <summary>Правая панель: ближайшая цель и стрелка-компас.</summary>
        private void PanelTarget()
        {
            float width = Mathf.Clamp(Screen.width * 0.19f, 190f, 250f);
            Rect panel = new Rect(Screen.width - width - 14f, 14f, width, 86f);
            DrawPanel(panel);

            GUI.Label(new Rect(panel.x + 12f, panel.y + 8f, panel.width - 24f, 20f), "Ближайшая цель", small);
            GUI.Label(new Rect(panel.x + 12f, panel.y + 26f, panel.width - 24f, 24f), targetLabel, body);
            GUI.Label(new Rect(panel.x + 12f, panel.y + 50f, panel.width - 24f, 24f),
                targetDistance > 0f ? targetDistance.ToString("F0") + " м" : "рядом", accent);

            // Компас: поворот относительно взгляда камеры, чтобы стрелка вела игрока, а не карту.
            Camera cam = Object.FindFirstObjectByType<Camera>();
            if (cam == null || player == null) return;

            Vector3 flat = targetPosition - player.position;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.04f) return;

            float angle = Vector3.SignedAngle(cam.transform.forward.normalized, flat.normalized, Vector3.up);
            Vector2 center = new Vector2(panel.x + panel.width - 34f, panel.y + 46f);
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(center, Quaternion.AngleAxis(-angle, Vector3.forward), Vector3.one);
            Color tint = GUI.color;
            GUI.color = new Color(0.95f, 0.85f, 0.55f, 0.95f);
            GUI.DrawTexture(new Rect(-7f, -9f, 14f, 18f), plate);
            GUI.color = tint;
            GUI.matrix = previous;
        }

        /// <summary>Нижняя подсказка: что делать прямо сейчас и как управлять.</summary>
        private void PanelHint(GameSession session, WeaverWord word)
        {
            bool touch = Loomfall.TouchControls.IsTouchDevice();
            bool nearLoom = player != null && loom != null
                && Vector3.Distance(player.position, loom.position) <= loomRadius;

            string action;
            if (nearLoom) action = touch ? "коснитесь узла — натянуть нить" : "E — натянуть нить";
            else if (word != null && word.Echo < word.wordCost) action = "мало эха: собирайте осколки памяти";
            else action = touch ? "ведите пальцем слева" : "WASD — идти, Shift — бежать, пробел — прыжок";

            string secondary = touch
                ? "справа: прыжок, нить, слово (Q), разрыв (F)"
                : "Q — Слово, F — Разрыв, Tab — кодекс, R — реклама за награду";

            float width = Mathf.Clamp(Screen.width * 0.42f, 380f, 620f);
            Rect panel = new Rect((Screen.width - width) * 0.5f, Screen.height - 92f, width, 78f);
            DrawPanel(panel);

            GUI.Label(new Rect(panel.x, panel.y + 10f, panel.width, 26f), action, title);
            GUIStyle hint = new GUIStyle(small);
            hint.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(panel.x, panel.y + 40f, panel.width, 20f), secondary, hint);
        }

        private void Banner(string headline, string detail)
        {
            float width = Mathf.Clamp(Screen.width * 0.5f, 420f, 760f);
            Rect panel = new Rect((Screen.width - width) * 0.5f, Screen.height * 0.16f, width, 92f);
            DrawPanel(panel);
            GUIStyle head = new GUIStyle(title);
            head.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(panel.x, panel.y + 12f, panel.width, 28f), headline, head);
            GUIStyle text = new GUIStyle(body);
            text.alignment = TextAnchor.MiddleCenter;
            text.wordWrap = true;
            GUI.Label(new Rect(panel.x + 16f, panel.y + 44f, panel.width - 32f, 44f), detail, text);
        }

        private void Codex(GameSession session)
        {
            float width = Mathf.Clamp(Screen.width * 0.62f, 520f, 900f);
            float height = Mathf.Clamp(Screen.height * 0.66f, 320f, 560f);
            Rect panel = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
            DrawPanel(panel);

            GUI.Label(new Rect(panel.x + 18f, panel.y + 12f, width - 36f, 28f), "Кодекс — собранные записи", title);
            GUIStyle text = new GUIStyle(body);
            text.wordWrap = true;
            text.alignment = TextAnchor.UpperLeft;
            GUI.Label(new Rect(panel.x + 18f, panel.y + 46f, width - 36f, height - 92f),
                string.IsNullOrEmpty(session.CodexLog) ? "Записей пока нет: подходите к осколкам памяти." : session.CodexLog, text);
            GUI.Label(new Rect(panel.x + 18f, panel.y + height - 34f, width - 36f, 22f), "Tab — закрыть", small);
        }

        private void DrawPanel(Rect panel)
        {
            Color previous = GUI.color;
            GUI.color = new Color(0.05f, 0.06f, 0.09f, 0.82f);
            GUI.DrawTexture(panel, plate);
            GUI.color = new Color(0.55f, 0.6f, 0.72f, 0.5f);
            GUI.DrawTexture(new Rect(panel.x, panel.y, panel.width, 1f), plate);
            GUI.DrawTexture(new Rect(panel.x, panel.yMax - 1f, panel.width, 1f), plate);
            GUI.DrawTexture(new Rect(panel.x, panel.y, 1f, panel.height), plate);
            GUI.DrawTexture(new Rect(panel.xMax - 1f, panel.y, 1f, panel.height), plate);
            GUI.color = previous;
        }

        private void Bar(Rect area, float value, Color fill, string label)
        {
            Color previous = GUI.color;
            GUI.color = new Color(0.12f, 0.14f, 0.19f, 0.95f);
            GUI.DrawTexture(area, plate);
            GUI.color = fill;
            Rect inner = new Rect(area.x + 1f, area.y + 1f, Mathf.Max(0f, (area.width - 2f) * Mathf.Clamp01(value)), area.height - 2f);
            GUI.DrawTexture(inner, plate);
            GUI.color = previous;
            GUI.Label(new Rect(area.x + 4f, area.y - 2f, area.width - 8f, area.height + 2f), label, small);
        }

        private void Pips(Rect area, int filled, int total, Color color)
        {
            float size = 11f;
            float gap = 3f;
            Color previous = GUI.color;
            for (int i = 0; i < total; i++)
            {
                Rect cell = new Rect(area.x + i * (size + gap), area.y, size, size);
                GUI.color = i < filled ? color : new Color(0.16f, 0.18f, 0.23f, 0.95f);
                GUI.DrawTexture(cell, plate);
            }
            GUI.color = previous;
        }

        private static Texture2D MakePlateTexture()
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            texture.hideFlags = HideFlags.HideAndDontSave;
            return texture;
        }

        /// <summary>Круговая виньетка: края темнее центра, используется для давления забвения.</summary>
        private static Texture2D MakeVignetteTexture()
        {
            const int Size = 96;
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            float half = Size * 0.5f;
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float dx = (x - half) / half;
                    float dy = (y - half) / half;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01((distance - 0.45f) / 0.55f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha));
                }
            }
            texture.Apply();
            texture.hideFlags = HideFlags.HideAndDontSave;
            return texture;
        }
    }
}
