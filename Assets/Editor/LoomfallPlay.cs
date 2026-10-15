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
        private static Vector3 previousPosition;
        private static float speedSum;
        private static int speedCount;
        private static HeartChoice thirdThread;
        private static Vector3 introStart;
        private static bool started;

        public static void Run()
        {
            // Прогон начинается с чистого сохранения: иначе концовка и осколки подтянутся
            // из прошлого запуска, и проверка финала станет бессмысленной.
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            EditorSceneManager.OpenScene("Assets/Scenes/LowerYardModels.unity");
            frames = 0;
            started = false;
            maxY = 0f;
            Debug.Log("[Loomfall] Прогон: сохранение очищено перед запуском");
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
                // Три фазы: ходьба с клавиатуры, бег с телефона, затем подъём по ярусам.
                driver.testMove = Vector2.zero;
                driver.testSprint = false;
                driver.touchMove = Vector2.zero;
                driver.touchSprint = false;

                // Фаза заставки: ввод подаётся, но Ткач обязан стоять на месте.
                if (frames >= 1 && frames < 15)
                {
                    driver.testMove = new Vector2(0f, 1f);
                    if (frames == 1)
                    {
                        introStart = driver.transform.position;
                        var introClock = Loomfall.OblivionClock.Instance;
                        Debug.Log("[Loomfall] Заставка: видна — " + Loomfall.IntroScreen.BlocksInput
                            + ", ввод игры заблокирован, Забвение "
                            + (introClock != null ? introClock.Oblivion.ToString("F6") : "нет часов"));
                    }
                }
                else if (frames == 15)
                {
                    float underIntro = Vector3.Distance(introStart, driver.transform.position);
                    var frozen = Loomfall.OblivionClock.Instance;
                    Debug.Log("[Loomfall] Заставка: сдвиг Ткача под заставкой " + underIntro.ToString("F4")
                        + " м за 14 кадров подачи движения, Забвение "
                        + (frozen != null ? frozen.Oblivion.ToString("F6") : "нет часов")
                        + " (должно совпасть с кадром 1 — под заставкой часы стоят)");

                    var intro = Loomfall.IntroScreen.Instance;
                    if (intro != null) intro.Dismiss();
                    previousPosition = driver.transform.position;
                    speedSum = 0f;
                    speedCount = 0;
                }
                else if (frames == 16)
                {
                    Debug.Log("[Loomfall] Заставка: после закрытия ввод принимается — "
                        + (Loomfall.IntroScreen.BlocksInput ? "нет" : "да"));
                }
                else if (Time.deltaTime > 0.000001f)
                {
                    speedSum += Vector3.Distance(previousPosition, driver.transform.position) / Time.deltaTime;
                    speedCount++;
                    previousPosition = driver.transform.position;
                }

                if (frames >= 20 && frames < 100)
                {
                    driver.testMove = new Vector2(0f, 1f);
                }
                else if (frames == 100)
                {
                    Debug.Log("[Loomfall] Скорость ходьбы: "
                        + (speedCount > 0 ? (speedSum / speedCount).ToString("F2") : "?")
                        + " м/с из " + speedCount + " замеров при заданной " + driver.walkSpeed.ToString("F2"));
                    speedSum = 0f;
                    speedCount = 0;
                }
                else if (frames >= 100 && frames < 180)
                {
                    // Бег идёт через сенсорный ввод: так проверяется и путь с телефона.
                    driver.touchMove = new Vector2(0f, 1f);
                    driver.touchSprint = true;
                }
                else if (frames == 180)
                {
                    Debug.Log("[Loomfall] Скорость бега с телефона: "
                        + (speedCount > 0 ? (speedSum / speedCount).ToString("F2") : "?")
                        + " м/с из " + speedCount + " замеров при заданной " + driver.runSpeed.ToString("F2"));
                    speedSum = 0f;
                    speedCount = 0;
                    Debug.Log("[Loomfall] Подъём: шагаем от двора к пятому ярусу, высоту решает опора на пол");
                }
                else if (frames >= 200 && frames < 560)
                {
                    // Только вперёд: высоту подъёма на пандусах решает опора на пол.
                    driver.transform.position += new Vector3(0f, 0f, 0.5f);
                }

                maxY = Mathf.Max(maxY, driver.transform.position.y);

                // Финал: подходим к третьей нити и пробуем шаг до и после выполнения условий.
                var currentSession = GameSession.Instance;
                var currentClock = OblivionClock.Instance;

                if (frames == 560)
                {
                    foreach (HeartChoice choice in Object.FindObjectsByType<HeartChoice>(FindObjectsSortMode.None))
                    {
                        if (choice.index == 2) thirdThread = choice;
                    }

                    if (thirdThread != null)
                    {
                        driver.transform.position = thirdThread.transform.position - new Vector3(0f, 1.5f, 1.2f);
                        thirdThread.player = driver.transform;
                        Debug.Log("[Loomfall] Финал: подошли к третьей нити, условия до шага — осколков "
                            + (currentSession != null ? currentSession.Shards : -1)
                            + ", светильников " + (currentClock != null ? currentClock.LitLamps : -1)
                            + ", Забвение " + (currentClock != null ? currentClock.Oblivion.ToString("F2") : "?"));
                    }
                    else
                    {
                        Debug.Log("[Loomfall] Финал: третья нить не найдена");
                    }
                }

                if (frames == 565 && thirdThread != null)
                {
                    thirdThread.testPress = true;
                }

                if (frames == 570)
                {
                    Debug.Log("[Loomfall] Финал: после шага без условий концовка «"
                        + (currentSession != null && currentSession.Ending.Length > 0 ? currentSession.Ending : "нет")
                        + "», последняя строка кодекса: "
                        + (currentSession != null && currentSession.CodexLog.Length > 0 ? currentSession.CodexLog : "пусто"));
                }

                if (frames == 575)
                {
                    if (currentSession != null)
                    {
                        for (int i = 0; i < 4; i++) currentSession.CollectShard("Проверка финала " + (i + 1));
                    }

                    if (currentClock != null)
                    {
                        for (int i = 0; i < 2; i++) currentClock.Ignite();
                    }

                    Debug.Log("[Loomfall] Финал: условия выполнены — осколков "
                        + (currentSession != null ? currentSession.Shards : -1)
                        + ", светильников " + (currentClock != null ? currentClock.LitLamps : -1)
                        + ", Забвение " + (currentClock != null ? currentClock.Oblivion.ToString("F2") : "?"));
                }

                if (frames == 580 && thirdThread != null)
                {
                    thirdThread.testPress = true;
                }

                if (frames == 585)
                {
                    Debug.Log("[Loomfall] Финал: после выполнения условий концовка «"
                        + (currentSession != null && currentSession.Ending.Length > 0 ? currentSession.Ending : "нет") + "»");
                }

                if (frames == 200)
                {
                    float previous = -1f;
                    int ledges = 0;
                    for (float z = -10f; z <= 180f; z += 1f)
                    {
                        float height = 0f;
                        if (Physics.Raycast(new Vector3(0f, 40f, z), Vector3.down, out RaycastHit probe, 80f))
                        {
                            height = probe.point.y;
                        }

                        if (previous >= 0f && Mathf.Abs(height - previous) > 0.6f)
                        {
                            ledges++;
                            Debug.Log("[Loomfall] Ступень: z " + z.ToString("F0")
                                + ", высота " + previous.ToString("F2") + " -> " + height.ToString("F2")
                                + " (перепад " + (height - previous).ToString("F2") + ")");
                        }

                        previous = height;
                    }

                    Debug.Log("[Loomfall] Свип высоты: z от -10 до 180 пройден, перепадов " + ledges);
                }

                if (frames >= 40 && frames % 40 == 0)
                {
                    Debug.Log("[Loomfall] Подъём: кадр " + frames
                        + ", позиция " + driver.transform.position.ToString("F2")
                        + ", выше всего " + maxY.ToString("F2"));
                }
            }

            if (frames < 600) return;

            var player = Loomfall.WeaverController.Instance;
            var session = GameSession.Instance;
            var clock = OblivionClock.Instance;
            int shards = Object.FindObjectsOfType<Loomfall.MemoryShard>().Length;
            int lamps = Object.FindObjectsOfType<Loomfall.LampAnchor>().Length;
            int threads = Object.FindObjectsOfType<Loomfall.HeartChoice>().Length;

            Debug.Log("[Loomfall] Прогон: кадров " + frames
                + ", Ткач на " + (player != null ? player.transform.position.ToString("F2") : "не найден")
                + ", сдвиг " + (player != null ? (player.transform.position - startPosition).magnitude.ToString("F3") : "?")
                + ", выше всего " + maxY.ToString("F2")
                + (maxY >= 17f ? " — вершина достигнута" : " — до вершины не дошёл"));
            Debug.Log("[Loomfall] Прогон: ход игры — " + (session != null)
                + ", часов Забвения " + (clock != null ? clock.Oblivion.ToString("F3") : "нет")
                + ", осколков в мире " + shards + ", светильников " + lamps + ", нитей " + threads
                + ", концовка «" + (session != null && session.Ending.Length > 0 ? session.Ending : "не выбрана") + "»");
            Debug.Log("[Loomfall] Прогон: завершён без остановки игры");

            EditorApplication.Exit(0);
        }
    }
}