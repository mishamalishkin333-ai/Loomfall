using System.IO;
using UnityEditor;
using UnityEngine;

namespace Loomfall.EditorTools
{
    /// <summary>Правка собранной страницы: подключает SDK площадок к WebGL-архиву.</summary>
    public static class LoomfallWeb
    {
        // Версия VK Bridge закреплена: unpkg по умолчанию отдаёт свежую сборку,
        // и однажды выйдет версия, которая сломает вызовы.
        private const string VkBridge = "https://unpkg.com/@vkontakte/vk-bridge@3.0.2/dist/browser.min.js";
        private const string YandexSdk = "https://yandex.ru/games/sdk/v2";
        private const string Marker = "loomfall-platforms";

        /// <summary>Вставляет скрипты VK Bridge и Яндекс Игр перед закрытием head.</summary>
        public static void PatchTemplate(string indexPath)
        {
            if (!File.Exists(indexPath))
            {
                Debug.LogError("[Loomfall] Нет страницы сборки: " + indexPath);
                return;
            }

            string html = File.ReadAllText(indexPath);
            if (html.Contains(Marker))
            {
                Debug.Log("[Loomfall] Страница уже содержит SDK площадок");
                return;
            }

            string tags =
                "  <!-- " + Marker + " -->\n" +
                "  <script src=\"" + VkBridge + "\"></script>\n" +
                "  <script src=\"" + YandexSdk + "\"></script>\n";

            int head = html.IndexOf("</head>");
            if (head < 0)
            {
                Debug.LogError("[Loomfall] В странице нет </head>");
                return;
            }

            File.WriteAllText(indexPath, html.Insert(head, tags));
            Debug.Log("[Loomfall] SDK площадок подключены к странице");
        }
    }
}