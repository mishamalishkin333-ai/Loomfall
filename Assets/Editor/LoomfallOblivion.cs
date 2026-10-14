using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Loomfall.EditorTools
{
    /// <summary>Проверка механики Забвения: как мир тускнеет и бледнеет по мере роста числа.</summary>
    public static class LoomfallOblivion
    {
        private const string ScenePath = "Assets/Scenes/LowerYardModels.unity";

        /// <summary>Рендерит двор при Забвении 0, 0,5 и 1 и печатает замеры кадров.</summary>
        public static void Compare()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var clock = Object.FindFirstObjectByType<Loomfall.OblivionClock>();
            Camera camera = Object.FindFirstObjectByType<Camera>();
            if (camera == null)
            {
                Debug.LogError("[Loomfall] На сцене нет камеры");
                EditorApplication.Exit(1);
                return;
            }

            Light[] lights = clock != null && clock.lights != null ? clock.lights : new Light[0];
            float[] basis = clock != null ? clock.baseIntensities : null;
            if (lights.Length == 0)
            {
                var found = new List<Light>();
                foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                {
                    if (light.type == LightType.Directional) found.Add(light);
                }

                lights = found.ToArray();
                basis = new float[lights.Length];
                for (int i = 0; i < lights.Length; i++) basis[i] = lights[i].intensity;
            }

            float baseFog = clock != null ? clock.baseFogDensity : 0.02f;
            float maxFog = clock != null ? clock.maxFogDensity : 0.07f;

            float[] steps = { 0f, 0.5f, 1f };
            foreach (float value in steps)
            {
                Loomfall.OblivionClock.ApplyLook(value, lights, basis, baseFog, maxFog);

                string path = "C:/Unity/shots/oblivion-" + Mathf.RoundToInt(value * 100f) + ".png";
                LoomfallCore.FrameStats stats = LoomfallCore.RenderShot(camera, path, 1280, 720);
                Debug.Log("[Loomfall] Забвение " + value.ToString("F1") + ": " + stats);
            }
        }
    }
}