using UnityEngine;

namespace Loomfall
{
    /// <summary>Экранный слой: задача яруса, подсказки управления и зов у ткацкого узла.</summary>
    public class WeaverHud : MonoBehaviour
    {
        public Transform player;
        public Transform loom;
        public float loomRadius = 4.5f;

        private GUIStyle title;
        private GUIStyle line;

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
            }

            GUI.Label(new Rect(28f, 22f, 1000f, 40f), "Ткач Ярусов — нижний двор", title);
            GUI.Label(new Rect(28f, 62f, 1000f, 30f), "Найдите ткацкий узел и натяните нить между ярусами", line);
            GUI.Label(new Rect(28f, Screen.height - 62f, 1000f, 26f), "WASD — идти, Shift — бежать, пробел — прыжок", line);

            if (player == null || loom == null) return;
            if (Vector3.Distance(player.position, loom.position) > loomRadius) return;

            GUI.Label(new Rect(Screen.width * 0.5f - 160f, Screen.height - 124f, 420f, 32f), "E — натянуть нить", title);
        }
    }
}