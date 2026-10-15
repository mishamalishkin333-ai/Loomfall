using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Loomfall.EditorTools
{
    /// <summary>Живой прогон: входит в play-режим, крутит кадры и сообщает, что получилось.</summary>
    public static class LoomfallPlay
    {
        private static int frames;
        private static Vector3 startPosition;
        private static bool started;

        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/LowerYardModels.unity");
            frames = 0;
            started = false;
            EditorApplication.update += Tick;
            EditorApplication.EnterPlaymode();
        }

        private static void Tick()
        {
            if (!Application.isPlaying) return;

            if (!started)
            {
                started = true;
                var hero = Object.FindObjectOfType<Loomfall.WeaverController>();
                if (hero != null) startPosition = hero.transform.position;
                Debug.Log("[Loomfall] Прогон: игра запустилась, Ткач на " + startPosition.ToString("F2"));
            }

            frames++;

            if (frames == 60)
            {
                var hero = Object.FindObjectOfType<Loomfall.WeaverController>();
                Debug.Log("[Loomfall] Прогон: 60 кадров, Ткач на "
                    + (hero != null ? hero.transform.position.ToString("F2") : "не найден"));
            }

            if (frames < 120) return;

            var player = Object.FindObjectOfType<Loomfall.WeaverController>();
            var session = GameSession.Instance;
            var clock = OblivionClock.Instance;
            int shards = Object.FindObjectsOfType<Loomfall.MemoryShard>().Length;
            int lamps = Object.FindObjectsOfType<Loomfall.LampAnchor>().Length;
            int threads = Object.FindObjectsOfType<Loomfall.HeartChoice>().Length;

            Debug.Log("[Loomfall] Прогон: кадров " + frames
                + ", Ткач на " + (player != null ? player.transform.position.ToString("F2") : "не найден")
                + ", сдвиг " + (player != null ? (player.transform.position - startPosition).magnitude.ToString("F3") : "?"));
            Debug.Log("[Loomfall] Прогон: ход игры — " + (session != null)
                + ", часов Забвения " + (clock != null ? clock.Oblivion.ToString("F3") : "нет")
                + ", осколков в мире " + shards + ", светильников " + lamps + ", нитей " + threads);
            Debug.Log("[Loomfall] Прогон: завершён без остановки игры");

            EditorApplication.Exit(0);
        }
    }
}