using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Loomfall.EditorTools
{
    /// <summary>Инструменты сцен из моделей: камера витрины, её свет и замеры моделей.</summary>
    public static class LoomfallStudio
    {
        /// <summary>Камера смотрит в центр кадра и сразу включает постобработку.</summary>
        public static Camera MakeCamera(string name, Vector3 position, Vector3 lookAt, float fieldOfView)
        {
            var cameraObject = new GameObject(name);
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = fieldOfView;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.05f, 0.055f, 0.07f);
            cameraObject.transform.position = position;
            cameraObject.transform.LookAt(lookAt);

            var data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.High;
            return camera;
        }

        /// <summary>Свет витрины: ровный и светлый, чтобы модели было видно целиком.</summary>
        public static void BuildStudioLighting()
        {
            var keyObject = new GameObject("СветВитрины");
            var key = keyObject.AddComponent<Light>();
            key.type = LightType.Directional;
            key.color = new Color(0.97f, 0.96f, 0.94f);
            key.intensity = 1.7f;
            key.shadows = LightShadows.Soft;
            key.shadowStrength = 0.9f;
            keyObject.transform.rotation = Quaternion.Euler(46f, -28f, 0f);

            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.34f, 0.36f, 0.42f);
            RenderSettings.ambientEquatorColor = new Color(0.2f, 0.21f, 0.24f);
            RenderSettings.ambientGroundColor = new Color(0.12f, 0.12f, 0.13f);
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
        }

        /// <summary>Загружает модель как объект сцены.</summary>
        public static GameObject LoadModel(string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null)
            {
                Debug.LogError("[Loomfall] Модель не найдена: " + path);
                return null;
            }

            return PrefabUtility.InstantiatePrefab(asset) as GameObject;
        }

        /// <summary>Границы модели: по ним считаются шаг плитки и масштаб.</summary>
        public static Bounds Size(GameObject item)
        {
            Renderer[] renderers = item.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(Vector3.zero, Vector3.one);
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            return bounds;
        }

        /// <summary>Наибольший размер модели по осям.</summary>
        public static float MaxSize(GameObject item)
        {
            Bounds bounds = Size(item);
            return Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
        }

        /// <summary>
        /// Вырезает кривые масштаба из клипов, импортированных из модели. FBX несёт
        /// в клипах родной масштаб корня (100x), и аниматор каждый кадр выписывает
        /// его обратно, перекрывая любую нормализацию при постройке сцены. После
        /// вырезания масштаб модели управляется одним местом — импортом.
        /// </summary>
        public static void StripScaleCurves(string assetPath)
        {
            var clips = new System.Collections.Generic.List<AnimationClip>();
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
            {
                if (asset is AnimationClip clip && !clip.name.Contains("Skeleton")) clips.Add(clip);
            }

            if (clips.Count == 0)
            {
                Debug.Log("[Loomfall] Клипы «" + assetPath + "»: анимации не найдены");
                return;
            }

            int removed = 0;
            foreach (AnimationClip clip in clips)
            {
                foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
                {
                    if (!binding.propertyName.StartsWith("m_LocalScale")) continue;
                    AnimationUtility.SetEditorCurve(clip, binding, null);
                    removed++;
                }
            }

            Debug.Log("[Loomfall] Клипы «" + assetPath + "»: клипов " + clips.Count
                + ", кривых масштаба вырезано " + removed);
        }

        /// <summary>
        /// Нормализует модель НА УРОВНЕ ИМПОРТА FBX. Масштабить инстанс бесполезно:
        /// клипы анимации записывают локальный масштаб корневого узла каждый кадр
        /// и возвращают модели её родные 100× — в игре Ткач вырастал до 105 метров,
        /// камера оказывалась внутри меша, и вместо игры был экран цвета кожи.
        /// </summary>
        public static void EnsureModelHeight(string assetPath, float meters)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null)
            {
                Debug.Log("[Loomfall] Импорт «" + assetPath + "»: это не модель, пропускаем");
                return;
            }

            var probe = UnityEngine.Object.Instantiate(
                AssetDatabase.LoadAssetAtPath<GameObject>(assetPath));
            Renderer[] renderers = probe.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                UnityEngine.Object.DestroyImmediate(probe);
                Debug.Log("[Loomfall] Импорт «" + assetPath + "»: рендереров нет, пропускаем");
                return;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            float current = bounds.size.y;
            UnityEngine.Object.DestroyImmediate(probe);

            if (current < 0.001f)
            {
                Debug.Log("[Loomfall] Импорт «" + assetPath + "»: высота " + current + " почти нулевая, пропускаем");
                return;
            }

            float wanted = importer.globalScale * (meters / current);
            if (Mathf.Abs(wanted - importer.globalScale) < 0.000001f)
            {
                Debug.Log("[Loomfall] Импорт «" + assetPath + "»: уже " + current.ToString("F2") + " м, не трогаем");
                return;
            }

            importer.globalScale = wanted;
            importer.SaveAndReimport();

            var recheck = UnityEngine.Object.Instantiate(
                AssetDatabase.LoadAssetAtPath<GameObject>(assetPath));
            Renderer[] again = recheck.GetComponentsInChildren<Renderer>(true);
            Bounds reb = again[0].bounds;
            for (int i = 1; i < again.Length; i++) reb.Encapsulate(again[i].bounds);
            UnityEngine.Object.DestroyImmediate(recheck);

            Debug.Log("[Loomfall] Импорт «" + assetPath + "»: глобальный масштаб "
                + importer.globalScale.ToString("F5") + ", высота стала " + reb.size.y.ToString("F2") + " м");
        }

        /// <summary>
        /// Подгоняет модель под нужный рост по её ФАКТИЧЕСКИМ мировым габаритам.
        /// Считать масштаб по ассету нельзя: FBX несёт собственный 100×, и двойная
        /// компенсация делала Ткача гигантом на 105 метров — камера оказывалась
        /// внутри меша, и игрок видел стену цвета кожи вместо игры.
        /// </summary>
        public static void NormalizeHeight(GameObject instance, float meters)
        {
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                Debug.Log("[Loomfall] Нормировка «" + instance.name + "»: рендереров нет, пропускаем");
                return;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

            float current = bounds.size.y;
            if (current < 0.0001f)
            {
                Debug.Log("[Loomfall] Нормировка «" + instance.name + "»: высота " + current + " почти нулевая, пропускаем");
                return;
            }

            float support = instance.transform.position.y;
            float before = instance.transform.localScale.y;
            instance.transform.localScale *= meters / current;

            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            float lift = support - bounds.min.y;
            instance.transform.position = new Vector3(
                instance.transform.position.x,
                instance.transform.position.y + lift,
                instance.transform.position.z);

            Debug.Log("[Loomfall] Нормировка «" + instance.name + "»: было " + current.ToString("F2") + " м (масштаб "
                + before.ToString("F2") + "), стало " + bounds.size.y.ToString("F2") + " м (масштаб "
                + instance.transform.localScale.y.ToString("F4") + "), множитель " + (meters / current).ToString("F5"));
        }

        /// <summary>Копия модели в точке с поворотом и общим материалом.</summary>
        public static GameObject Place(GameObject root, GameObject template, Material material, Vector3 position, float turn)
        {
            var copy = UnityEngine.Object.Instantiate(template);
            copy.transform.SetParent(root.transform);
            copy.transform.position = position;
            copy.transform.rotation = Quaternion.Euler(0f, turn, 0f);
            foreach (Renderer renderer in copy.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterial = material;
            }

            return copy;
        }
    }
}