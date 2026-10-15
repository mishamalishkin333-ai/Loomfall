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

            // Второй ярус: площадка выше двора за его дальним краем, пандус наверх, свой Светильник и Осколок.
            {
            const float upperLevel = 4.4f;
            const int upX = 6;
            const int upZ = 8;
            float upOffset = (upX - 1) * 0.5f * tile;
            float upStart = startZ + tile * 2f;
            var upper = new GameObject("ВторойЯрус");

            for (int x = 0; x < upX; x++)
            {
                for (int z = 0; z < upZ; z++)
                {
                    LoomfallStudio.Place(
                        upper,
                        floor,
                        cave,
                        new Vector3(x * tile - upOffset, upperLevel, upStart + z * tile),
                        0f);
                }
            }

            for (int z = 0; z < upZ; z++)
            {
                LoomfallStudio.Place(upper, wall, cave, new Vector3(-upOffset - tile, upperLevel, upStart + z * tile), 0f);
                LoomfallStudio.Place(upper, wall, cave, new Vector3(upOffset + tile, upperLevel, upStart + z * tile), 0f);
            }

            for (int x = 0; x < upX; x++)
            {
                LoomfallStudio.Place(upper, wall, cave, new Vector3(x * tile - upOffset, upperLevel, upStart + upZ * tile), 0f);
            }

            // Пандус: невидимый наклонный пол, по нему Ткач поднимается с нижнего двора.
            var ramp = new GameObject("ПандусНаверх");
            ramp.transform.position = new Vector3(0f, upperLevel * 0.5f, startZ + tile);
            ramp.transform.rotation = Quaternion.Euler(-26f, 0f, 0f);
            var rampCollider = ramp.AddComponent<BoxCollider>();
            rampCollider.size = new Vector3(tile * 2.5f, 0.4f, 11f);

            // Полы-коллайдеры: луч земли в контроллере Ткача опирается на них.
            var lowerFloor = new GameObject("ПолДвора");
            lowerFloor.transform.position = new Vector3(0f, -0.05f, 0f);
            lowerFloor.AddComponent<BoxCollider>().size = new Vector3(TilesX * tile + 2f, 0.1f, TilesZ * tile + 2f);

            var upperFloor = new GameObject("ПолВерхнегоЯруса");
            upperFloor.transform.position = new Vector3(0f, upperLevel - 0.05f, upStart + tile * 3.5f);
            upperFloor.AddComponent<BoxCollider>().size = new Vector3(upX * tile + 2f, 0.1f, upZ * tile + 4f);

            GameObject upperLamp = LoomfallStudio.Place(
                upper,
                LoomfallStudio.LoadModel("Assets/Art/Models/CaveKit/template-wall-detail-a.fbx"),
                cave,
                new Vector3(-upOffset, upperLevel + 1.8f, upStart + tile * 6f),
                0f);
            if (upperLamp != null)
            {
                var upperAnchor = upperLamp.AddComponent<Loomfall.LampAnchor>();
                upperAnchor.player = weaver.transform;
            }

            var upperShard = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            upperShard.name = "Осколок-5";
            upperShard.transform.position = new Vector3(upOffset - tile, upperLevel + 1.1f, upStart + tile * 5f);
            upperShard.transform.localScale = Vector3.one * 0.45f;
            var upperShardPart = upperShard.AddComponent<Loomfall.MemoryShard>();
            upperShardPart.player = weaver.transform;
            upperShardPart.index = 4;

            // Стены-коллайдеры: сквозь кладку Ткач больше не проходит, проход для пандуса открыт.
            var wallWest = new GameObject("СтенаЗапад");
            wallWest.transform.position = new Vector3(-TilesX * tile * 0.5f - 0.5f, 1.5f, 0f);
            wallWest.AddComponent<BoxCollider>().size = new Vector3(1f, 3f, TilesZ * tile + 2f);

            var wallEast = new GameObject("СтенаВосток");
            wallEast.transform.position = new Vector3(TilesX * tile * 0.5f + 0.5f, 1.5f, 0f);
            wallEast.AddComponent<BoxCollider>().size = new Vector3(1f, 3f, TilesZ * tile + 2f);

            var wallUpperWest = new GameObject("СтенаЯрусаЗапад");
            wallUpperWest.transform.position = new Vector3(-upOffset - tile - 0.5f, upperLevel + 1.5f, upStart + upZ * tile * 0.5f);
            wallUpperWest.AddComponent<BoxCollider>().size = new Vector3(1f, 3f, upZ * tile + 2f);

            var wallUpperEast = new GameObject("СтенаЯрусаВосток");
            wallUpperEast.transform.position = new Vector3(upOffset + tile + 0.5f, upperLevel + 1.5f, upStart + upZ * tile * 0.5f);
            wallUpperEast.AddComponent<BoxCollider>().size = new Vector3(1f, 3f, upZ * tile + 2f);

            var wallUpperFar = new GameObject("СтенаЯрусаТорец");
            wallUpperFar.transform.position = new Vector3(0f, upperLevel + 1.5f, upStart + upZ * tile + 0.5f);
            wallUpperFar.AddComponent<BoxCollider>().size = new Vector3(upX * tile + 2f, 3f, 1f);

            // Замер подъёма: высота земли вдоль пандуса обязана расти от двора к верхнему ярусу.
            float[] probes = { startZ - tile, startZ + tile * 0.5f, startZ + tile * 2f, startZ + tile * 3f, upStart + tile * 2f };
            foreach (float probe in probes)
            {
                float height = 0f;
                if (Physics.Raycast(new Vector3(0f, upperLevel + 6f, probe), Vector3.down, out RaycastHit probeHit, 20f))
                {
                    height = probeHit.point.y;
                }

                Debug.Log("[Loomfall] Подъём: z " + probe.ToString("F1") + " -> высота " + height.ToString("F2"));
            }

            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();

            var stats = LoomfallCore.RenderShot(camera, "C:/Unity/shots/yard-models.png", 1600, 900);
            Debug.Log("[Loomfall] Двор из моделей: " + stats);
        }
    }
}
