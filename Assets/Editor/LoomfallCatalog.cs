using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Loomfall.EditorTools
{
    /// <summary>Витрина ассетов: все модели проекта в сетке при ровном свете.</summary>
    public static class LoomfallCatalog
    {
        private const string ScenePath = "Assets/Scenes/Catalog.unity";
        private const string CaveKit = "Assets/Art/Models/CaveKit";
        private const string Protagonist = "Assets/Art/Models/Protagonist";

        /// <summary>Точка входа: собирает витрину и рендерит кадр.</summary>
        public static void Build()
        {
            LoomfallCore.ConfigureProject();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            LoomfallStudio.BuildStudioLighting();

            Material cave = LoomfallCore.MakeTextureMaterial("CaveMap", CaveKit + "/colormap.png");
            Material human = LoomfallCore.MakeTextureMaterial("HumanSkin", Protagonist + "/skaterMaleA.png");
            var items = new List<GameObject>();

            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { "Assets/Art/Models" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".fbx")) continue;
                // Скелеты анимаций (idle, jump, run) в витрине не нужны.
                if (path.Contains("/Protagonist/") && !path.Contains("characterMedium")) continue;

                GameObject item = LoomfallStudio.LoadModel(path);
                if (item == null) continue;
                Material material = path.Contains("/Protagonist/") ? human : cave;
                foreach (Renderer renderer in item.GetComponentsInChildren<Renderer>())
                {
                    renderer.sharedMaterial = material;
                }

                item.name = Path.GetFileNameWithoutExtension(path);
                items.Add(item);
            }

            const int columns = 10;
            const float cell = 4.2f;
            for (int i = 0; i < items.Count; i++)
            {
                GameObject item = items[i];
                item.transform.localScale = Vector3.one * (2.4f / Mathf.Max(0.05f, LoomfallStudio.MaxSize(item)));
                item.transform.position = new Vector3(
                    (i % columns - (columns - 1) / 2f) * cell,
                    0f,
                    i / columns * cell);
            }

            int rows = Mathf.Max(1, Mathf.CeilToInt(items.Count / (float)columns));
            float extent = Mathf.Max(columns * cell, rows * cell);
            var center = new Vector3(0f, 1.1f, (rows - 1) * cell * 0.5f);
            Camera camera = LoomfallStudio.MakeCamera(
                "КамераВитрины",
                center + new Vector3(extent * 0.55f, extent * 0.75f, -extent * 0.8f),
                center,
                40f);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();

            LoomfallCore.FrameStats stats = LoomfallCore.RenderShot(camera, "C:/Unity/shots/catalog.png", 1600, 900);
            Debug.Log("[Loomfall] Витрина моделей: " + items.Count + " шт., " + stats);
        }
    }
}