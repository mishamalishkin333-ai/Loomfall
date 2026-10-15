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

            var wallUpperFarWest = new GameObject("СтенаЯрусаТорецЗапад");
            wallUpperFarWest.transform.position = new Vector3(-upOffset - 2.5f, upperLevel + 1.5f, upStart + upZ * tile + 0.5f);
            wallUpperFarWest.AddComponent<BoxCollider>().size = new Vector3(upX * tile * 0.5f - 4f, 3f, 1f);

            var wallUpperFarEast = new GameObject("СтенаЯрусаТорецВосток");
            wallUpperFarEast.transform.position = new Vector3(upOffset + 2.5f, upperLevel + 1.5f, upStart + upZ * tile + 0.5f);
            wallUpperFarEast.AddComponent<BoxCollider>().size = new Vector3(upX * tile * 0.5f - 4f, 3f, 1f);

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

            // Третий ярус: ещё выше, второй пандус, свой Светильник и Осколок.
            const float level3 = 8.8f;
            float up3Start = upStart + upZ * tile + tile * 2f;
            var upper3 = new GameObject("ТретийЯрус");

            for (int x = 0; x < 5; x++)
            {
                for (int z = 0; z < 6; z++)
                {
                    LoomfallStudio.Place(upper3, floor, cave, new Vector3(x * tile - tile * 2f, level3, up3Start + z * tile), 0f);
                }
            }

            for (int z = 0; z < 6; z++)
            {
                LoomfallStudio.Place(upper3, wall, cave, new Vector3(-tile * 3f, level3, up3Start + z * tile), 0f);
                LoomfallStudio.Place(upper3, wall, cave, new Vector3(tile * 3f, level3, up3Start + z * tile), 0f);
            }

            var ramp3 = new GameObject("ПандусНаТретийЯрус");
            ramp3.transform.position = new Vector3(0f, (upperLevel + level3) * 0.5f, upStart + upZ * tile + tile);
            ramp3.transform.rotation = Quaternion.Euler(-26f, 0f, 0f);
            ramp3.AddComponent<BoxCollider>().size = new Vector3(tile * 2.5f, 0.4f, 11f);

            var floor3 = new GameObject("ПолТретьегоЯруса");
            floor3.transform.position = new Vector3(0f, level3 - 0.05f, up3Start + tile * 2.5f);
            floor3.AddComponent<BoxCollider>().size = new Vector3(5f * tile + 2f, 0.1f, 6f * tile + 4f);

            var wall3West = new GameObject("СтенаТретьегоЗапад");
            wall3West.transform.position = new Vector3(-tile * 3f - 0.5f, level3 + 1.5f, up3Start + tile * 2.5f);
            wall3West.AddComponent<BoxCollider>().size = new Vector3(1f, 3f, 6f * tile + 2f);

            var wall3East = new GameObject("СтенаТретьегоВосток");
            wall3East.transform.position = new Vector3(tile * 3f + 0.5f, level3 + 1.5f, up3Start + tile * 2.5f);
            wall3East.AddComponent<BoxCollider>().size = new Vector3(1f, 3f, 6f * tile + 2f);

            GameObject lamp3 = LoomfallStudio.Place(
                upper3,
                LoomfallStudio.LoadModel("Assets/Art/Models/CaveKit/template-wall-detail-a.fbx"),
                cave,
                new Vector3(tile, level3 + 1.8f, up3Start + tile * 4f),
                0f);
            if (lamp3 != null)
            {
                var anchor3 = lamp3.AddComponent<Loomfall.LampAnchor>();
                anchor3.player = weaver.transform;
            }

            var shard3 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shard3.name = "Осколок-6";
            shard3.transform.position = new Vector3(-tile, level3 + 1.1f, up3Start + tile * 3f);
            shard3.transform.localScale = Vector3.one * 0.45f;
            var shard3Part = shard3.AddComponent<Loomfall.MemoryShard>();
            shard3Part.player = weaver.transform;
            shard3Part.index = 5;

            // Замер второго подъёма: от верхнего яруса к третьему.
            float[] probes3 = { upStart + upZ * tile + tile * 0.5f, upStart + upZ * tile + tile * 2f, up3Start + tile * 2.5f };
            foreach (float probe in probes3)
            {
                float height = 0f;
                if (Physics.Raycast(new Vector3(0f, level3 + 6f, probe), Vector3.down, out RaycastHit probe3Hit, 24f))
                {
                    height = probe3Hit.point.y;
                }

                Debug.Log("[Loomfall] Подъём выше: z " + probe.ToString("F1") + " -> высота " + height.ToString("F2"));
            }

            // Четвёртый ярус: третий пандус, свой Светильник и Осколок.
            const float level4 = 13.2f;
            float up4Start = up3Start + tile * 8f;
            var upper4 = new GameObject("ЧетвёртыйЯрус");

            for (int x = 0; x < 4; x++)
            {
                for (int z = 0; z < 5; z++)
                {
                    LoomfallStudio.Place(upper4, floor, cave, new Vector3(x * tile - tile * 1.5f, level4, up4Start + z * tile), 0f);
                }
            }

            for (int z = 0; z < 5; z++)
            {
                LoomfallStudio.Place(upper4, wall, cave, new Vector3(-tile * 2.5f, level4, up4Start + z * tile), 0f);
                LoomfallStudio.Place(upper4, wall, cave, new Vector3(tile * 2.5f, level4, up4Start + z * tile), 0f);
            }

            var ramp4 = new GameObject("ПандусНаЧетвёртыйЯрус");
            ramp4.transform.position = new Vector3(0f, (level3 + level4) * 0.5f, up3Start + tile * 6f + tile);
            ramp4.transform.rotation = Quaternion.Euler(-26f, 0f, 0f);
            ramp4.AddComponent<BoxCollider>().size = new Vector3(tile * 2.5f, 0.4f, 11f);

            var floor4 = new GameObject("ПолЧетвёртогоЯруса");
            floor4.transform.position = new Vector3(0f, level4 - 0.05f, up4Start + tile * 2f);
            floor4.AddComponent<BoxCollider>().size = new Vector3(4f * tile + 2f, 0.1f, 5f * tile + 4f);

            GameObject lamp4 = LoomfallStudio.Place(
                upper4,
                LoomfallStudio.LoadModel("Assets/Art/Models/CaveKit/template-wall-detail-a.fbx"),
                cave,
                new Vector3(tile * 0.5f, level4 + 1.8f, up4Start + tile * 3.5f),
                0f);
            if (lamp4 != null)
            {
                var anchor4 = lamp4.AddComponent<Loomfall.LampAnchor>();
                anchor4.player = weaver.transform;
            }

            var shard4 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shard4.name = "Осколок-7";
            shard4.transform.position = new Vector3(-tile * 0.5f, level4 + 1.1f, up4Start + tile * 2.5f);
            shard4.transform.localScale = Vector3.one * 0.45f;
            var shard4Part = shard4.AddComponent<Loomfall.MemoryShard>();
            shard4Part.player = weaver.transform;
            shard4Part.index = 6;

            // Замер третьего подъёма.
            float[] probes4 = { up3Start + tile * 6f + tile * 0.5f, up4Start + tile * 2f };
            foreach (float probe in probes4)
            {
                float height = 0f;
                if (Physics.Raycast(new Vector3(0f, level4 + 6f, probe), Vector3.down, out RaycastHit probe4Hit, 28f))
                {
                    height = probe4Hit.point.y;
                }

                Debug.Log("[Loomfall] Подъём ещё выше: z " + probe.ToString("F1") + " -> высота " + height.ToString("F2"));
            }

            // Пятый ярус: последний подъём, вершина мира.
            const float level5 = 17.6f;
            float up5Start = up4Start + tile * 7f;
            var upper5 = new GameObject("ПятыйЯрус");

            for (int x = 0; x < 3; x++)
            {
                for (int z = 0; z < 4; z++)
                {
                    LoomfallStudio.Place(upper5, floor, cave, new Vector3(x * tile - tile, level5, up5Start + z * tile), 0f);
                }
            }

            for (int z = 0; z < 4; z++)
            {
                LoomfallStudio.Place(upper5, wall, cave, new Vector3(-tile * 2f, level5, up5Start + z * tile), 0f);
                LoomfallStudio.Place(upper5, wall, cave, new Vector3(tile * 2f, level5, up5Start + z * tile), 0f);
            }

            var ramp5 = new GameObject("ПандусНаПятыйЯрус");
            ramp5.transform.position = new Vector3(0f, (level4 + level5) * 0.5f, up4Start + tile * 5f + tile);
            ramp5.transform.rotation = Quaternion.Euler(-26f, 0f, 0f);
            ramp5.AddComponent<BoxCollider>().size = new Vector3(tile * 2.5f, 0.4f, 11f);

            var floor5 = new GameObject("ПолПятогоЯруса");
            floor5.transform.position = new Vector3(0f, level5 - 0.05f, up5Start + tile * 1.5f);
            floor5.AddComponent<BoxCollider>().size = new Vector3(3f * tile + 2f, 0.1f, 4f * tile + 4f);

            GameObject lamp5 = LoomfallStudio.Place(
                upper5,
                LoomfallStudio.LoadModel("Assets/Art/Models/CaveKit/template-wall-detail-a.fbx"),
                cave,
                new Vector3(0f, level5 + 1.8f, up5Start + tile * 2.5f),
                0f);
            if (lamp5 != null)
            {
                var anchor5 = lamp5.AddComponent<Loomfall.LampAnchor>();
                anchor5.player = weaver.transform;
            }

            var shard5 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shard5.name = "Осколок-8";
            shard5.transform.position = new Vector3(0f, level5 + 1.1f, up5Start + tile * 1.5f);
            shard5.transform.localScale = Vector3.one * 0.45f;
            var shard5Part = shard5.AddComponent<Loomfall.MemoryShard>();
            shard5Part.player = weaver.transform;
            shard5Part.index = 7;

            float[] probes5 = { up4Start + tile * 5f + tile * 0.5f, up5Start + tile * 1.5f };
            foreach (float probe in probes5)
            {
                float height = 0f;
                if (Physics.Raycast(new Vector3(0f, level5 + 6f, probe), Vector3.down, out RaycastHit probe5Hit, 32f))
                {
                    height = probe5Hit.point.y;
                }

                Debug.Log("[Loomfall] Подъём на вершину: z " + probe.ToString("F1") + " -> высота " + height.ToString("F2"));
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
