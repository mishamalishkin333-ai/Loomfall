using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Loomfall.EditorTools
{
    /// <summary>Ставит на сцену двора управление Ткачом, камеру от третьего лица, узел и интерфейс.</summary>
    public static class LoomfallRuntime
    {
        private const string ScenePath = "Assets/Scenes/LowerYardModels.unity";
        private const string Kit = "Assets/Art/Models/CaveKit";

        /// <summary>Пересобирает двор и оживляет его рантайм-компонентами.</summary>
        public static void Setup()
        {
            LoomfallYard.Build();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            LoomfallCore.BuildVolume();

            GameObject weaver = GameObject.Find("ТкачУзел");
            if (weaver == null)
            {
                foreach (Animator animator in Object.FindObjectsByType<Animator>(FindObjectsSortMode.None))
                {
                    weaver = animator.gameObject;
                    break;
                }
            }

            if (weaver == null)
            {
                Debug.LogError("[Loomfall] Ткач не найден на сцене");
                EditorApplication.Exit(1);
                return;
            }

            if (weaver.GetComponent<Loomfall.WeaverController>() == null)
            {
                weaver.AddComponent<Loomfall.WeaverController>();
            }

            // Точка старта задаётся здесь: при постройке двора позиция до модели не доехала.
            weaver.transform.position = new Vector3(0f, 0.05f, -4.4f);
            Debug.Log("[Loomfall] Ткач на старте: " + weaver.transform.position.ToString("F2")
                + ", родитель " + (weaver.transform.parent != null ? weaver.transform.parent.name : "нет"));

            if (weaver.GetComponent<Loomfall.WeaverWord>() == null)
            {
                weaver.AddComponent<Loomfall.WeaverWord>();
            }

            if (weaver.GetComponent<Loomfall.AmbienceLoop>() == null)
            {
                weaver.AddComponent<Loomfall.AmbienceLoop>();
            }

            Camera camera = Object.FindFirstObjectByType<Camera>();
            if (camera != null)
            {
                var follow = camera.GetComponent<Loomfall.WeaverCamera>();
                if (follow == null) follow = camera.gameObject.AddComponent<Loomfall.WeaverCamera>();
                follow.target = weaver.transform;
            }

            var loom = new GameObject("ТкацкийУзел");
            loom.transform.position = new Vector3(0f, 0f, 7f);
            Material cave = LoomfallCore.MakeTextureMaterial("CaveMap", Kit + "/colormap.png");
            GameObject detail = LoomfallStudio.LoadModel(Kit + "/template-detail.fbx");
            if (detail != null) LoomfallStudio.Place(loom, detail, cave, loom.transform.position, 0f);

            var hudObject = new GameObject("Интерфейс");
            var hud = hudObject.AddComponent<Loomfall.WeaverHud>();
            hud.player = weaver.transform;
            hud.loom = loom.transform;

            var sessionObject = new GameObject("Сессия");
            var session = sessionObject.AddComponent<Loomfall.GameSession>();
            session.player = weaver.transform;
            session.loom = loom.transform;

            var audioObject = new GameObject("Звук");
            var audio = audioObject.AddComponent<Loomfall.YardAudio>();
            audio.player = weaver.transform;

            var clockObject = new GameObject("Забвение");
            var clock = clockObject.AddComponent<Loomfall.OblivionClock>();
            var skyLights = new System.Collections.Generic.List<Light>();
            foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type == LightType.Directional) skyLights.Add(light);
            }

            clock.lights = skyLights.ToArray();
            clock.baseIntensities = new float[clock.lights.Length];
            for (int i = 0; i < clock.lights.Length; i++)
            {
                clock.baseIntensities[i] = clock.lights[i].intensity;
            }

            clock.mainCamera = camera;
            clock.volume = Object.FindFirstObjectByType<UnityEngine.Rendering.Volume>();

            GameObject lampModel = LoomfallStudio.LoadModel(Kit + "/template-wall-detail-a.fbx");
            Vector3[] lampPlaces =
            {
                new Vector3(-5.5f, 1.8f, 2f),
                new Vector3(5.5f, 1.8f, 5f),
                new Vector3(-5.5f, 1.8f, 13f),
            };

            for (int i = 0; i < lampPlaces.Length; i++)
            {
                var lamp = new GameObject("Светильник-" + (i + 1));
                lamp.transform.position = lampPlaces[i];
                if (lampModel != null) LoomfallStudio.Place(lamp, lampModel, cave, lampPlaces[i], 0f);
                var anchor = lamp.AddComponent<Loomfall.LampAnchor>();
                anchor.player = weaver.transform;
            }

            Vector3[] shardPlaces =
            {
                new Vector3(-3.2f, 1.1f, 4.5f),
                new Vector3(3.4f, 1.1f, 9f),
                new Vector3(-4.2f, 1.1f, 15f),
                new Vector3(2.2f, 1.1f, 18f),
            };

            for (int i = 0; i < shardPlaces.Length; i++)
            {
                var shardObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                shardObject.name = "Осколок-" + (i + 1);
                shardObject.transform.position = shardPlaces[i];
                shardObject.transform.localScale = Vector3.one * 0.45f;
                var shard = shardObject.AddComponent<Loomfall.MemoryShard>();
                shard.player = weaver.transform;
                shard.index = i;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Loomfall] Рантайм поставлен: управление, камера, ткацкий узел, интерфейс");
        }
    }
}