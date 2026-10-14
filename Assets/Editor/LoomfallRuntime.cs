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

            GameObject weaver = null;
            foreach (Animator animator in Object.FindObjectsByType<Animator>(FindObjectsSortMode.None))
            {
                weaver = animator.gameObject;
                break;
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
            foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type == LightType.Directional)
                {
                    clock.sky = light;
                    break;
                }
            }

            if (clock.sky != null) clock.baseSkyIntensity = clock.sky.intensity;

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

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Loomfall] Рантайм поставлен: управление, камера, ткацкий узел, интерфейс");
        }
    }
}