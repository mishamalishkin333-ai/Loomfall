using System.Globalization;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Loomfall
{
    /// <summary>Забвение: число 0..1, растёт само, откатывается Светильниками и гасит свет, фон и цвет мира.</summary>
    public class OblivionClock : MonoBehaviour
    {
        public static OblivionClock Instance { get; private set; }

        public float growthPerSecond = 0.012f;
        public Light[] lights;
        public float[] baseIntensities;
        public Camera mainCamera;
        public Volume volume;
        public float baseFogDensity = 0.02f;
        public float maxFogDensity = 0.07f;
        public float baseExposure = 0.45f;
        public float darkExposure = -1.3f;

        public float Oblivion { get; private set; }
        public int LitLamps { get; private set; }

        private float saveTimer;

        private void Awake()
        {
            Instance = this;
            float.TryParse(
                PlatformBridge.Load("oblivion", "0.15"),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float loaded);
            Oblivion = Mathf.Clamp01(loaded);
            ApplyLook(Oblivion, lights, baseIntensities, mainCamera, volume, baseFogDensity, maxFogDensity, baseExposure, darkExposure);
        }

        private void Update()
        {
            // Каждый зажжённый Светильник держит сектор: рост Забвения падает на четверть.
            float hold = Mathf.Pow(0.75f, LitLamps);
            Oblivion = Mathf.Clamp01(Oblivion + growthPerSecond * hold * Time.deltaTime);
            ApplyLook(Oblivion, lights, baseIntensities, mainCamera, volume, baseFogDensity, maxFogDensity, baseExposure, darkExposure);

            saveTimer += Time.deltaTime;
            if (saveTimer > 3f)
            {
                saveTimer = 0f;
                PlatformBridge.Save("oblivion", Oblivion.ToString("F3", CultureInfo.InvariantCulture));
            }
        }

        /// <summary>Зажжённый Светильник откатывает Забвение в своём секторе и держит его.</summary>
        public void Ignite()
        {
            LitLamps++;
            Oblivion = Mathf.Clamp01(Oblivion - 0.35f);
            ApplyLook(Oblivion, lights, baseIntensities, mainCamera, volume, baseFogDensity, maxFogDensity, baseExposure, darkExposure);
            PlatformBridge.Save("oblivion", Oblivion.ToString("F3", CultureInfo.InvariantCulture));
            Debug.Log("[Loomfall] Светильник держит сектор: Забвение " + Oblivion.ToString("F2", CultureInfo.InvariantCulture));
        }

        /// <summary>Магия сжигает Эхо: Забвение подскакивает, и вид мира сразу пересчитывается.</summary>
        public void Burn(float amount)
        {
            Oblivion = Mathf.Clamp01(Oblivion + amount);
            ApplyLook(Oblivion, lights, baseIntensities, mainCamera, volume, baseFogDensity, maxFogDensity, baseExposure, darkExposure);
        }

        /// <summary>Вид мира по числу Забвения: свет, туман, фон камеры, экспозиция и цвет.</summary>
        public static void ApplyLook(
            float oblivion,
            Light[] lights,
            float[] baseIntensities,
            Camera camera,
            Volume volume,
            float baseFogDensity,
            float maxFogDensity,
            float baseExposure,
            float darkExposure)
        {
            float t = Mathf.Clamp01(oblivion);

            // Самый заметный слой: фон камеры занимает большую часть кадра.
            if (camera != null)
            {
                camera.backgroundColor = Color.Lerp(
                    new Color(0.035f, 0.04f, 0.055f),
                    new Color(0.006f, 0.007f, 0.011f),
                    t);
            }

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = Color.Lerp(
                new Color(0.075f, 0.085f, 0.115f),
                new Color(0.012f, 0.013f, 0.017f),
                t);
            RenderSettings.fogDensity = Mathf.Lerp(baseFogDensity, maxFogDensity, t);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Color.Lerp(
                new Color(0.13f, 0.15f, 0.19f),
                new Color(0.03f, 0.032f, 0.038f),
                t);
            RenderSettings.ambientEquatorColor = Color.Lerp(
                new Color(0.07f, 0.075f, 0.09f),
                new Color(0.016f, 0.017f, 0.02f),
                t);
            RenderSettings.ambientGroundColor = Color.Lerp(
                new Color(0.025f, 0.025f, 0.03f),
                new Color(0.006f, 0.006f, 0.008f),
                t);

            if (lights != null)
            {
                for (int i = 0; i < lights.Length; i++)
                {
                    if (lights[i] == null) continue;
                    float basis = baseIntensities != null && i < baseIntensities.Length
                        ? baseIntensities[i]
                        : lights[i].intensity;
                    lights[i].intensity = Mathf.Lerp(basis, basis * 0.25f, t);
                }
            }

            // Постобработка: свет гаснет, цвет уходит — мир бледнеет целиком.
            if (volume != null && volume.profile != null && volume.profile.TryGet(out ColorAdjustments adjust))
            {
                adjust.postExposure.value = Mathf.Lerp(baseExposure, darkExposure, t);
                adjust.saturation.value = Mathf.Lerp(-12f, -60f, t);
                adjust.contrast.value = Mathf.Lerp(14f, 4f, t);
            }
        }
    }
}