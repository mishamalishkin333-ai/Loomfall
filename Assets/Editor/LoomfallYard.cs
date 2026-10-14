using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Loomfall.EditorTools
{
    /// <summary>Двор первого яруса из модульного кита: плитка пола, кладка стен, ворота и Ткач.</summary>
    public static class LoomfallYard
    {
        private const string ScenePath = "Assets/Scenes/LowerYardModels.unity";
        private const string Kit = "Assets/Art/Models/CaveKit";
        private const string Hero = "Assets/Art/Models/Protagonist";
        private const int TilesX = 14;
        private const int TilesZ = 22;
        private const int Stack = 3;

        /// <summary>Собирает двор и рендерит кадр.</summary>
        public static void Build()
        {
            LoomfallCore.ConfigureProject();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            LoomfallCore.BuildLighting();

            Material cave = LoomfallCore.MakeTextureMaterial("CaveMap", Kit + "/colormap.png");
            Material skin = LoomfallCore.MakeTextureMaterial("HumanSkin", Hero + "/skaterMaleA.png");

            GameObject floor = LoomfallStudio.LoadModel(Kit + "/template-floor.fbx");
            GameObject wall = LoomfallStudio.LoadModel(Kit + "/template-wall.fbx");
            GameObject corner = LoomfallStudio.LoadModel(Kit + "/template-corner.fbx");
            GameObject stairs = LoomfallStudio.LoadModel(Kit + "/stairs.fbx");
            GameObject gate = LoomfallStudio.LoadModel(Kit + "/gate.fbx");
            GameObject hero = LoomfallStudio.LoadModel(Hero + "/characterMedium.fbx");
            if (floor == null || wall == null || hero == null)
            {
                Debug.LogError("[Loomfall] Нет модулей кита или персонажа");
                EditorApplication.Exit(1);
                return;
            }

            var root = new GameObject("ДворПервогоЯруса");
            float tile = Mathf.Max(0.2f, LoomfallStudio.MaxSize(floor));
            float level = LoomfallStudio.Size(wall).size.y;
            float startX = (TilesX - 1) * 0.5f * tile;
            float startZ = (TilesZ - 1) * 0.5f * tile;

            for (int x = 0; x < TilesX; x++)
            {
                for (int z = 0; z < TilesZ; z++)
                {
                    LoomfallStudio.Place(root, floor, cave, new Vector3(x * tile - startX, 0f, z * tile - startZ), 0f);
                }
            }

            for (int z = 0; z < TilesZ; z++)
            {
                for (int s = 0; s < Stack; s++)
                {
                    LoomfallStudio.Place(root, wall, cave, new Vector3(-startX - tile, s * level, z * tile - startZ), 90f);
                    LoomfallStudio.Place(root, wall, cave, new Vector3(startX + tile, s * level, z * tile - startZ), 270f);
                }
            }

            for (int x = 0; x < TilesX; x++)
            {
                for (int s = 0; s < Stack; s++)
                {
                    LoomfallStudio.Place(root, wall, cave, new Vector3(x * tile - startX, s * level, startZ + tile), 0f);
                }
            }

            if (corner != null)
            {
                LoomfallStudio.Place(root, corner, cave, new Vector3(-startX - tile, 0f, startZ + tile), 0f);
                LoomfallStudio.Place(root, corner, cave, new Vector3(startX + tile, 0f, startZ + tile), 270f);
            }

            if (stairs != null) LoomfallStudio.Place(root, stairs, cave, new Vector3(-2.6f, 0f, 6f), 0f);
            if (gate != null) LoomfallStudio.Place(root, gate, cave, new Vector3(0f, 0f, -startZ - tile), 0f);

            GameObject weaver = LoomfallStudio.Place(root, hero, skin, new Vector3(0f, 0f, -4.4f), 180f);
            weaver.transform.localScale = Vector3.one * (1.75f / Mathf.Max(0.05f, LoomfallStudio.MaxSize(hero)));
            Animator animator = weaver.GetComponent<Animator>();
            if (animator == null) animator = weaver.AddComponent<Animator>();
            animator.runtimeAnimatorController = LoomfallHeroAnim.EnsureController();

            Camera camera = LoomfallStudio.MakeCamera(
                "КамераИгрока",
                new Vector3(0.8f, 3f, -startZ - 5f),
                new Vector3(0f, 1.7f, 8f),
                52f);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();

            var stats = LoomfallCore.RenderShot(camera, "C:/Unity/shots/yard-models.png", 1600, 900);
            Debug.Log("[Loomfall] Двор из моделей: " + stats);
        }
    }
}