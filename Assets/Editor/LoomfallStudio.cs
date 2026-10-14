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