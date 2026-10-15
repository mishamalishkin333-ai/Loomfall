using UnityEngine;

namespace Loomfall
{
    /// <summary>Экранный слой: название яруса, строка сюжета, счёт нитей, подсказки и зов узла.</summary>
    public class WeaverHud : MonoBehaviour
    {
        public Transform player;
        public Transform loom;
        public float loomRadius = 4.5f;

        private GUIStyle title;
        private GUIStyle line;
        private GUIStyle accent;

        private void OnGUI()
        {
            if (title == null)
            {
                title = new GUIStyle(GUI.skin.label);
                title.fontSize = 26;
                title.fontStyle = FontStyle.Bold;
                title.normal.textColor = new Color(0.95f, 0.89f, 0.7f);
                line = new GUIStyle(GUI.skin.label);
                line.fontSize = 17;
                line.normal.textColor = new Color(0.84f, 0.84f, 0.88f);
                accent = new GUIStyle(line);
                accent.normal.textColor = new Color(0.72f, 0.78f, 0.9f);
            }

            GameSession session = GameSession.Instance;
            int tier = session != null ? session.Tier : 1;
            int threads = session != null ? session.Threads : 0;
            int need = session != null ? session.threadsPerTier : 3;

            GUI.Label(new Rect(28f, 22f, 1100f, 40f), "Ткач Ярусов — нижний двор, ярус " + tier, title);
            GUI.Label(new Rect(28f, 62f, 1100f, 30f), "Мир держится на нитях памяти. Из семи ярусов осталось пять.", accent);
            GUI.Label(new Rect(28f, 94f, 1100f, 30f), "Нить яруса: " + threads + " из " + need + " — каждая возвращает кусок чужой памяти.", line);

            OblivionClock clock = OblivionClock.Instance;
            if (clock != null)
            {
                GUI.Label(
                    new Rect(28f, 126f, 1100f, 30f),
                    "Забвение: " + Mathf.RoundToInt(clock.Oblivion * 100f) + "%, Светильников зажжено: " + clock.LitLamps,
                    accent);
            }

            WeaverWord word = WeaverWord.Instance;
            if (word != null)
            {
                GUI.Label(
                    new Rect(28f, 158f, 1300f, 30f),
                    "Эхо: " + Mathf.RoundToInt(word.Echo) + " из 100 — «Q» Слово, «F» Разрыв (жгут память яруса)",
                    line);
            }

            if (session != null && Time.time - session.CodexTime < 4.5f)
            {
                GUI.Label(
                    new Rect(28f, 190f, 1300f, 30f),
                    "Запись Кодекса (" + session.Shards + "): " + session.Codex,
                    accent);
            }

            if (session != null && !string.IsNullOrEmpty(session.Ending))
            {
                GUI.Label(
                    new Rect(28f, 222f, 1300f, 34f),
                    "Концовка: «" + session.Ending + "» — двор пройден",
                    accent);
            }
            GUI.Label(new Rect(28f, Screen.height - 62f, 1100f, 26f), "WASD — идти, Shift — бежать, пробел — прыжок, E — ткать, R — реклама за награду", line);

            if (player == null || loom == null) return;
            if (Vector3.Distance(player.position, loom.position) > loomRadius) return;

            GUI.Label(new Rect(Screen.width * 0.5f - 170f, Screen.height - 130f, 440f, 32f), "E — натянуть нить", title);
        }
    }
}