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
        private static float startTime;
        private static float maxY;
        private static bool started;

        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/LowerYardModels.unity");
            frames = 0;
            started = false;
            maxY = 0f;
            EditorApplication.update += Tick;
            EditorApplication.EnterPlaymode();
        }

        private static void Tick()
        {
            if (!Application.isPlaying) return;

            if (!started)
            {
                started = true;
                var first = Loomfall.WeaverController.Instance;
                if (first != null)
                {
                    startPosition = first.transform.position;
                    startTime = Time.time;
                }
                Debug.Log("[Loomfall] Прогон: игра запустилась, Ткач на " + startPosition.ToString("F2")
                    + ", контроллер " + (first != null ? "взят из самого компонента" : "пуст"));
            }

            frames++;

            if (frames == 60)
            {
                var hero = Object.FindObjectOfType<Loomfall.WeaverController>();
                if (hero == null)
                {
                    Debug.Log("[Loomfall] Разбор: WeaverController не найден");
                }
                else
                {
                    Transform t = hero.transform;
                    string parent = t.parent != null ? t.parent.name : "нет";
                    Debug.Log("[Loomfall] Разбор: объект «" + hero.name + "», родитель «" + parent
                        + "», мировые " + t.position.ToString("F2") + ", локальные " + t.localPosition.ToString("F2"));
                    Debug.Log("[Loomfall] Разбор: детей " + t.childCount + ", Animator "
                        + (hero.GetComponent<Animator>() != null ? "на себе" : "нет")
                        + ", root motion " + (hero.GetComponent<Animator>() != null
                            ? hero.GetComponent<Animator>().applyRootMotion.ToString()
                            : "нет аниматора"));

                    for (int i = 0; i < t.childCount && i < 4; i++)
                    {
                        Transform child = t.GetChild(i);
                        Debug.Log("[Loomfall] Разбор: ребёнок «" + child.name + "» локально "
                            + child.localPosition.ToString("F2") + ", мировые " + child.position.ToString("F2")
                            + ", видим " + (child.GetComponent<Renderer>() != null));
                    }
                }

                Camera cam = Camera.main;
                if (cam != null)
                {
                    var follow = cam.GetComponent<Loomfall.WeaverCamera>();
                    Debug.Log("[Loomfall] Разбор: камера на " + cam.transform.position.ToString("F2")
                        + ", слежение " + (follow != null ? "есть" : "нет")
                        + ", цель " + (follow != null && follow.target != null ? follow.target.name : "нет"));
                }

                var anim = Object.FindObjectOfType<Animator>();
                Debug.Log("[Loomfall] Разбор: первый Animator на «" + (anim != null ? anim.name : "нет") + "»");

                int heroCount = 0;
                foreach (GameObject candidate in Object.FindObjectsOfType<GameObject>())
                {
                    if (candidate.name.IndexOf("characterMedium", System.StringComparison.Ordinal) < 0) continue;
                    heroCount++;
                    Debug.Log("[Loomfall] Разбор: кандидат «" + candidate.name + "» родитель «"
                        + (candidate.transform.parent != null ? candidate.transform.parent.name : "нет")
                        + "» локально " + candidate.transform.localPosition.ToString("F2")
                        + " мир " + candidate.transform.position.ToString("F2")
                        + " аниматор " + (candidate.GetComponent<Animator>() != null)
                        + " контроллер " + (candidate.GetComponent<Loomfall.WeaverController>() != null));
                }

                Debug.Log("[Loomfall] Разбор: всего объектов с именем героя " + heroCount
                    + ", компонентов управления " + Object.FindObjectsOfType<Loomfall.WeaverController>().Length);

                var loaded = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                Debug.Log("[Loomfall] Разбор: активная сцена «" + loaded.name + "», путь «" + loaded.path
                    + "», корневых объектов " + loaded.GetRootGameObjects().Length
                    + ", сцена героя «" + Object.FindObjectOfType<Loomfall.WeaverController>().gameObject.scene.name + "»");

                var keys = UnityEngine.InputSystem.Keyboard.current;
                Debug.Log("[Loomfall] Разбор: клавиатура " + (keys != null ? "есть" : "нет")
                    + (keys != null
                        ? ", W " + keys.wKey.isPressed + ", S " + keys.sKey.isPressed
                            + ", A " + keys.aKey.isPressed + ", D " + keys.dKey.isPressed
                            + ", пробел " + keys.spaceKey.isPressed
                        : "")
                    + ", кадров " + Time.frameCount + ", шаг " + Time.deltaTime.ToString("F4"));
            }

            var driver = Loomfall.WeaverController.Instance;
            if (driver != null)
            {
                // Ведём Ткача вперёд, как если бы игрок держал «W».
                driver.testMove = frames >= 10 && frames < 110 ? new Vector2(0f, 1f) : Vector2.zero;
                maxY = Mathf.Max(maxY, driver.transform.position.y);
                if (frames == 60) driver.testJump = true;

                // Пробный сдвиг: если его тоже отменит, значит позицию возвращает кто-то другой.
                if (frames == 70) driver.transform.position += new Vector3(0f, 0f, 0.5f);
                if (frames == 80)
                {
                    Debug.Log("[Loomfall] Прогон: пробный сдвиг на 0,5 м по Z дал " + driver.transform.position.ToString("F3"));
                }

                if (frames == 110)
                {
                    float moved = (driver.transform.position - startPosition).magnitude;
                    float elapsed = Time.time - startTime;
                    Debug.Log("[Loomfall] Прогон: Ткач прошёл " + moved.ToString("F3") + " м за " + elapsed.ToString("F3")
                        + " с, скорость " + (elapsed > 0.0001f ? (moved / elapsed).ToString("F2") : "?")
                        + " м/с при заданной ходьбе " + driver.walkSpeed.ToString("F2")
                        + ", сдвиг по Z " + (driver.transform.position.z - startPosition.z).ToString("F3"));
                }
            }

            if (frames < 120) return;

            var player = Loomfall.WeaverController.Instance;
            var session = GameSession.Instance;
            var clock = OblivionClock.Instance;
            int shards = Object.FindObjectsOfType<Loomfall.MemoryShard>().Length;
            int lamps = Object.FindObjectsOfType<Loomfall.LampAnchor>().Length;
            int threads = Object.FindObjectsOfType<Loomfall.HeartChoice>().Length;

            Debug.Log("[Loomfall] Прогон: кадров " + frames
                + ", Ткач на " + (player != null ? player.transform.position.ToString("F2") : "не найден")
                + ", сдвиг " + (player != null ? (player.transform.position - startPosition).magnitude.ToString("F3") : "?")
                + ", выше всего " + maxY.ToString("F2"));
            Debug.Log("[Loomfall] Прогон: ход игры — " + (session != null)
                + ", часов Забвения " + (clock != null ? clock.Oblivion.ToString("F3") : "нет")
                + ", осколков в мире " + shards + ", светильников " + lamps + ", нитей " + threads);
            Debug.Log("[Loomfall] Прогон: завершён без остановки игры");

            EditorApplication.Exit(0);
        }
    }
}