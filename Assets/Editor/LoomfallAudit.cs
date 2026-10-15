using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Loomfall.EditorTools
{
    /// <summary>
    /// Разбор картинки: что игрок реально видит на экране. Считает размер аватара
    /// в пикселях, число объектов в кадре, покрытие экрана заставкой и статистику
    /// по кадру, плюс сохраняет кадры для просмотра человеком.
    /// </summary>
    public static class LoomfallAudit
    {
        private const string Dir = "C:/Unity/shots/audit";
        private const int ShotWidth = 1280;
        private const int ShotHeight = 720;

        private static int frames;

        public static void Run()
        {
            Directory.CreateDirectory(Dir);
            // Сцену пересобираем здесь же: иначе разбор меряет старую сцену,
            // где Ткач ещё гигант, и выводы получаются про прошлую версию.
            LoomfallRuntime.Setup();
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            EditorSceneManager.OpenScene("Assets/Scenes/LowerYardModels.unity");
            frames = 0;
            Debug.Log("[Loomfall] Разбор: начинаем, кадры кладём в " + Dir);
            EditorApplication.update += Tick;
            EditorApplication.EnterPlaymode();
        }

        private static void Tick()
        {
            if (!Application.isPlaying) return;
            frames++;

            var weaver = Loomfall.WeaverController.Instance;
            if (weaver != null)
            {
                weaver.testMove = Vector2.zero;
                weaver.testJump = false;
                weaver.testSprint = false;
            }

            if (frames == 3) DescribeIntro();
            if (frames == 8)
            {
                // Снимок экрана позже описания: захват экрана готов не в первом кадре,
                // и иначе кадр заставки терял экранный слой (OnGUI камерой не пишется).
                Shoot("00-intro", weaver);
            }

            if (frames == 30)
            {
                var intro = Object.FindFirstObjectByType<Loomfall.IntroScreen>();
                if (intro != null) intro.Dismiss();
            }

            if (frames == 70) { Describe("старт", weaver); Shoot("01-start", weaver); }

            // Идём по двору перемещением трансформы: в живом редакторе настоящая
            // клавиатура перекрывает пробный ввод, а кадр нужен идущего героя.
            if (frames > 70 && frames <= 190 && weaver != null)
            {
                weaver.transform.position += new Vector3(0f, 0f, 0.35f);
            }

            if (frames == 190) { Describe("двор", weaver); Shoot("02-yard", weaver); }

            // Дальше разбираем настоящие цели: станок и вершину, а не выдуманные
            // координаты. Прежде разбор ставил героя в пустоту на высоте 17,6 м,
            // и кадр вершины выходил на 94% чёрным.
            if (frames == 200 && weaver != null) { StandBefore(FindLoom(), weaver, 4f); SnapCamera(weaver); }
            if (frames == 250) { Describe("узел", weaver); Shoot("03-loom", weaver); }

            if (frames == 260 && weaver != null) { StandBefore(HighestHeart(), weaver, 4.5f); SnapCamera(weaver); }

            // После телепорта камере нужно доехать до героя: снимали раньше — она
            // оставалась в 40 м и кадр вершины тонул в тумане.
            if (frames == 400) { Describe("вершина", weaver); Shoot("04-top", weaver); }

            if (frames >= 440)
            {
                Summarize();
                EditorApplication.Exit(0);
            }
        }

        /// <summary>Станок, к которому приводят игрока нити сердца.</summary>
        private static Transform FindLoom()
        {
            var loom = GameObject.Find("ТкацкийУзел");
            return loom != null ? loom.transform : null;
        }

        /// <summary>Самая высокая нить сердца: вокруг неё построен финал.</summary>
        private static Transform HighestHeart()
        {
            Transform best = null;
            foreach (Loomfall.HeartChoice choice in Object.FindObjectsByType<Loomfall.HeartChoice>(FindObjectsSortMode.None))
            {
                if (best == null || choice.transform.position.y > best.position.y) best = choice.transform;
            }

            return best;
        }

        /// <summary>Ставит героя в нескольких метрах от цели лицом к ней и на её высоту.</summary>
        private static void StandBefore(Transform target, WeaverController weaver, float offset)
        {
            if (target == null)
            {
                Debug.Log("[Loomfall] Разбор: цели для кадра нет на сцене");
                return;
            }

            Vector3 position = new Vector3(target.position.x, target.position.y, target.position.z - offset);

            // Пол под целью: иначе герой повиснет в воздухе и камера уедет вверх.
            if (Physics.Raycast(position + Vector3.up * 6f, Vector3.down, out RaycastHit hit, 24f))
            {
                position.y = hit.point.y;
            }

            weaver.transform.position = position;
            weaver.transform.rotation = Quaternion.LookRotation(target.position - position);
            Debug.Log("[Loomfall] Разбор: герой поставлен в " + position.ToString("F2")
                + " напротив «" + target.name + "» (" + target.position.ToString("F2") + ")");
        }

        /// <summary>
        /// Ставит камеру мгновенно туда, куда она доехала бы за сотню кадров.
        /// Без этого снимок после телепорта выходил издалека: герой в 40 м,
        /// туман съел кадр, и вершина казалась чёрной пустотой.
        /// </summary>
        private static void SnapCamera(WeaverController weaver)
        {
            var follow = Object.FindFirstObjectByType<Loomfall.WeaverCamera>();
            if (follow == null || weaver == null) return;

            Transform hero = weaver.transform;
            follow.transform.position = hero.position - hero.forward * follow.distance + Vector3.up * follow.height;
            follow.transform.LookAt(hero.position + Vector3.up * follow.lookHeight);
            Debug.Log("[Loomfall] Разбор: камера встала за спиной героя в " + follow.transform.position.ToString("F2"));
        }

        /// <summary>Сколько экрана занято заставкой и насколько она непрозрачна.</summary>
        private static void DescribeIntro()
        {
            Rect panel = Loomfall.IntroScreen.PanelRect(Screen.width, Screen.height);
            float cover = panel.width * panel.height / (Mathf.Max(1f, Screen.width) * Mathf.Max(1f, Screen.height));
            Debug.Log("[Loomfall] Заставка: карточка " + Mathf.RoundToInt(panel.width) + "x" + Mathf.RoundToInt(panel.height)
                + " внизу из " + Screen.width + "x" + Screen.height + ", закрыто " + Mathf.RoundToInt(cover * 100f)
                + "% экрана; мир и Ткач видны в оставшихся " + Mathf.RoundToInt((1f - cover) * 100f) + "%");
        }

        private static void Describe(string label, Loomfall.WeaverController weaver)
        {
            Camera cam = Object.FindFirstObjectByType<Camera>();
            if (cam == null)
            {
                Debug.Log("[Loomfall] Разбор «" + label + "»: камеры нет");
                return;
            }

            string hero = weaver != null ? HeroReport(cam, weaver) : "Ткача нет в сцене";
            string scene = SceneReport(cam);
            if (label == "старт" && weaver != null) Details(weaver);
            var intro = Object.FindFirstObjectByType<Loomfall.IntroScreen>();
            Debug.Log("[Loomfall] Разбор «" + label + "»: Ткач в " + (weaver != null ? weaver.transform.position.ToString("F2") : "?")
                + ", рост " + (weaver != null ? weaver.CurrentHeight.ToString("F2") : "?") + " м при цели 1,75 м"
                + ", ступни " + (weaver != null ? weaver.FeetOffset.ToString("+0.00;-0.00") : "?") + " м от опоры"
                + ", заставка видна — " + (intro != null && intro.Visible)
                + ", ввод заблокирован — " + Loomfall.IntroScreen.BlocksInput);
            Debug.Log("[Loomfall] Разбор «" + label + "»: " + hero + "; " + scene
                + "; камера в " + cam.transform.position.ToString("F1") + ", ГОО " + cam.fieldOfView.ToString("F0")
                + "°, Ткач в " + (weaver != null ? Vector3.Distance(cam.transform.position, weaver.transform.position).ToString("F1") : "?") + " м");
        }

        /// <summary>Разбор иерархии Ткача: масштаб, меш, материалы — откуда берётся размер.</summary>
        private static void Details(Loomfall.WeaverController weaver)
        {
            Debug.Log("[Loomfall] Ткач «" + weaver.name + "»: локальный масштаб "
                + weaver.transform.localScale.ToString("F3") + ", мировой " + weaver.transform.lossyScale.ToString("F3")
                + ", коллайдер " + (weaver.GetComponent<Collider>() != null
                    ? weaver.GetComponent<Collider>().bounds.size.ToString("F2") : "нет"));

            foreach (Transform child in weaver.transform.GetComponentsInChildren<Transform>(true))
            {
                if (child == weaver.transform) continue;
                Renderer renderer = child.GetComponent<Renderer>();
                if (renderer == null) continue;

                Mesh mesh = null;
                if (renderer is SkinnedMeshRenderer skin) mesh = skin.sharedMesh;
                else if (child.GetComponent<MeshFilter>() != null) mesh = child.GetComponent<MeshFilter>().sharedMesh;

                string meshInfo = mesh != null
                    ? "меш «" + mesh.name + "», локальные габариты " + mesh.bounds.size.ToString("F2")
                      + ", вершин " + mesh.vertexCount
                    : "меша нет";
                string materialInfo = "материалов 0";
                if (renderer.sharedMaterials.Length > 0)
                {
                    Material m = renderer.sharedMaterials[0];
                    Texture main = m != null ? m.mainTexture : null;
                    materialInfo = "материалов " + renderer.sharedMaterials.Length + ", первый «"
                        + (m != null ? m.name : "пусто") + "», текстура " + (main != null ? "«" + main.name + "»" : "НЕТ")
                        + ", цвет " + (m != null ? m.color.ToString("F2") : "?");
                }

                Debug.Log("[Loomfall]   «" + child.name + "»: тип " + renderer.GetType().Name
                    + ", локальный масштаб " + child.localScale.ToString("F3") + ", мировой " + child.lossyScale.ToString("F3")
                    + ", мировые габариты " + renderer.bounds.size.ToString("F2") + "; " + meshInfo + "; " + materialInfo);
            }
        }

        /// <summary>Насколько крупно аватар виден на экране.</summary>
        private static string HeroReport(Camera cam, Loomfall.WeaverController weaver)
        {
            Renderer[] renderers = weaver.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return "АВАТАР НЕ ВИДЕН: у Ткача нет ни одного рендерера";
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            int inFront = 0;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = new Vector3(
                    (i & 1) == 0 ? bounds.min.x : bounds.max.x,
                    (i & 2) == 0 ? bounds.min.y : bounds.max.y,
                    (i & 4) == 0 ? bounds.min.z : bounds.max.z);
                Vector3 screen = cam.WorldToScreenPoint(corner);
                if (screen.z <= 0f) continue;
                inFront++;
                if (screen.x < minX) minX = screen.x;
                if (screen.x > maxX) maxX = screen.x;
                if (screen.y < minY) minY = screen.y;
                if (screen.y > maxY) maxY = screen.y;
            }

            if (inFront == 0)
            {
                return "АВАТАР ЗА КАМЕРОЙ: рендереров " + renderers.Length + ", но все позади";
            }

            float width = maxX - minX;
            float height = maxY - minY;
            float share = width * height / (Screen.width * (float)Screen.height);
            float aim = Vector3.Dot(cam.transform.forward, (bounds.center - cam.transform.position).normalized);
            bool onScreen = minX >= 0f && maxX <= Screen.width && minY >= 0f && maxY <= Screen.height;

            return "аватар " + Mathf.RoundToInt(width) + "x" + Mathf.RoundToInt(height) + " px ("
                + Mathf.RoundToInt(share * 100f) + "% экрана), в кадре " + (onScreen ? "целиком" : "частично")
                + ", камера смотрит на него (" + aim.ToString("F2") + " из 1), рендереров " + renderers.Length
                + ", рост " + bounds.size.y.ToString("F2") + " м";
        }

        /// <summary>Сколько мира попадает в кадр.</summary>
        private static string SceneReport(Camera cam)
        {
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cam);
            int visible = 0;
            long triangles = 0;
            foreach (Renderer renderer in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                if (!GeometryUtility.TestPlanesAABB(planes, renderer.bounds)) continue;
                visible++;

                Mesh mesh = null;
                if (renderer is SkinnedMeshRenderer skin) mesh = skin.sharedMesh;
                else if (renderer.GetComponent<MeshFilter>() != null) mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                if (mesh != null) triangles += mesh.triangles.Length / 3;
            }

            int lights = 0;
            foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.enabled && light.gameObject.activeInHierarchy
                    && GeometryUtility.TestPlanesAABB(planes, light.gameObject.GetComponent<Renderer>() != null
                        ? light.gameObject.GetComponent<Renderer>().bounds
                        : new Bounds(light.transform.position, Vector3.one * 8f)))
                {
                    lights++;
                }
            }

            return "объектов в кадре " + visible + ", треугольников " + triangles + ", источников света рядом " + lights;
        }

        /// <summary>
        /// Кадр для просмотра человеком. Сначала пробуем снять весь экран —
        /// только так в кадр попадает интерфейc (OnGUI камерой не рендерится).
        /// Если экран недоступен, остаётся старый путь: рендер камеры.
        /// </summary>
        private static void Shoot(string name, Loomfall.WeaverController weaver)
        {
            Texture2D texture = null;
            string source = "экран";
            try
            {
                texture = ScreenCapture.CaptureScreenshotAsTexture();
                if (texture != null && (texture.width < 200 || texture.height < 200))
                {
                    Object.DestroyImmediate(texture);
                    texture = null;
                }
            }
            catch (System.Exception e)
            {
                Debug.Log("[Loomfall] Экран снять не удалось (" + e.Message + "), рендерю камерой");
                texture = null;
            }

            if (texture == null)
            {
                source = "камера (без интерфейса)";
                texture = RenderCamera();
            }

            if (texture == null)
            {
                Debug.Log("[Loomfall] Кадр «" + name + "»: получить нечего");
                return;
            }

            byte[] png = texture.EncodeToPNG();
            File.WriteAllBytes(Dir + "/" + name + ".png", png);
            int shotWidth = texture.width;
            int shotHeight = texture.height;

            Color32[] pixels = texture.GetPixels32();
            double sum = 0d;
            int dark = 0;
            int saturated = 0;
            var shades = new HashSet<int>();
            foreach (Color32 p in pixels)
            {
                int brightness = (p.r + p.g + p.b) / 3;
                sum += brightness;
                if (brightness < 24) dark++;
                if (p.r - p.b > 40 || p.b - p.r > 40 || p.g - Mathf.Min(Mathf.Min(p.r, p.g), p.b) > 40) saturated++;
                shades.Add((p.r >> 3) << 10 | (p.g >> 3) << 5 | (p.b >> 3));
            }

            int total = pixels.Length;
            int average = Mathf.RoundToInt((float)(sum / total));
            string ui = source == "экран"
                ? UiReport(pixels, shotWidth, shotHeight)
                : "интерфейс в кадр камеры не попадает (OnGUI камерой не рендерится)";
            Object.DestroyImmediate(texture);

            Debug.Log("[Loomfall] Кадр «" + name + "» [" + source + " " + shotWidth + "x" + shotHeight + "]: "
                + "яркость " + average + " из 255, теней " + Mathf.RoundToInt(dark * 100f / total) + "%, цветных "
                + Mathf.RoundToInt(saturated * 100f / total) + "%, оттенков " + shades.Count
                + ", файл " + Mathf.RoundToInt(png.Length / 1024f) + " КБ");
            Debug.Log("[Loomfall]   " + ui);
        }

        /// <summary>
        /// Доказательство, что экранный слой нарисован: в прямоугольниках интерфейса
        /// ищем яркие точки (текст и полосы) поверх тёмных панелей.
        /// </summary>
        private static string UiReport(Color32[] pixels, int width, int height)
        {
            Rect state = new Rect(14f, 14f, Mathf.Clamp(width * 0.24f, 250f, 330f), 168f);
            float targetWidth = Mathf.Clamp(width * 0.19f, 190f, 250f);
            Rect target = new Rect(width - targetWidth - 14f, 14f, targetWidth, 86f);
            float hintWidth = Mathf.Clamp(width * 0.42f, 380f, 620f);
            Rect hint = new Rect((width - hintWidth) * 0.5f, height - 92f, hintWidth, 78f);

            // Захват экрана может быть перевёрнут по вертикали: меряем и прямой,
            // и отражённый прямоугольник, чтобы не потерять интерфейс из-за ориентации.
            int stateDirect = CountBright(pixels, width, height, state);
            int stateFlipped = CountBright(pixels, width, height, Mirror(state, height));
            int targetDirect = CountBright(pixels, width, height, target);
            int targetFlipped = CountBright(pixels, width, height, Mirror(target, height));
            int hintDirect = CountBright(pixels, width, height, hint);
            int hintFlipped = CountBright(pixels, width, height, Mirror(hint, height));

            int best = Mathf.Max(Mathf.Max(stateDirect, stateFlipped),
                Mathf.Max(Mathf.Max(targetDirect, targetFlipped), Mathf.Max(hintDirect, hintFlipped)));

            return "интерфейс: панель состояния " + stateDirect + "/" + stateFlipped
                + ", панель цели " + targetDirect + "/" + targetFlipped
                + ", подсказка " + hintDirect + "/" + hintFlipped
                + " (прямой/перевёрнутый) — " + (best > 400 ? "слой рисуется" : "СЛОЙ НЕ ВИДЕН");
        }

        private static Rect Mirror(Rect rect, int height)
        {
            return new Rect(rect.x, height - rect.yMax, rect.width, rect.height);
        }

        private static int CountBright(Color32[] pixels, int width, int height, Rect screenRect)
        {
            int count = 0;
            int x0 = Mathf.Max(0, Mathf.CeilToInt(screenRect.xMin));
            int x1 = Mathf.Min(width - 1, Mathf.CeilToInt(screenRect.xMax));
            // Захват экрана сверху вниз: y текстуры равен y экрана.
            int y0 = Mathf.Max(0, Mathf.CeilToInt(screenRect.yMin));
            int y1 = Mathf.Min(height - 1, Mathf.CeilToInt(screenRect.yMax));

            for (int y = y0; y <= y1; y++)
            {
                int row = y * width;
                for (int x = x0; x <= x1; x++)
                {
                    Color32 p = pixels[row + x];
                    if ((p.r + p.g + p.b) / 3 > 150) count++;
                }
            }

            return count;
        }

        private static Texture2D RenderCamera()
        {
            Camera cam = Object.FindFirstObjectByType<Camera>();
            if (cam == null) return null;

            RenderTexture previousTarget = cam.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture rt = new RenderTexture(ShotWidth, ShotHeight, 24, RenderTextureFormat.ARGB32);
            rt.Create();
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;

            var texture = new Texture2D(ShotWidth, ShotHeight, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0f, 0f, ShotWidth, ShotHeight), 0, 0);
            texture.Apply();

            cam.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            rt.Release();
            Object.DestroyImmediate(rt);
            return texture;
        }

        private static void Summarize()
        {
            string[] files = Directory.Exists(Dir) ? Directory.GetFiles(Dir, "*.png") : new string[0];
            Debug.Log("[Loomfall] Разбор: кадров пройдено " + frames + ", снимков " + files.Length);
            foreach (string file in files)
            {
                Debug.Log("[Loomfall]   " + Path.GetFileName(file) + " — " + Mathf.RoundToInt(new FileInfo(file).Length / 1024f) + " КБ");
            }
        }
    }
}
