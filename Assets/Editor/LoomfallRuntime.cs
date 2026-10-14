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

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Loomfall] Рантайм поставлен: управление, камера, ткацкий узел, интерфейс");
        }
    }
}