using System.Globalization;
using UnityEngine;
using UnityEngine.Rendering;

namespace Loomfall
{
    /// <summary>Забвение: число от 0 до 1, растёт само и откатывается зажжёнными Светильниками.</summary>
    public class OblivionClock : MonoBehaviour
    {
        public static OblivionClock Instance { get; private set; }

        public float growthPerSecond = 0.012f;
        public Light sky;
        public float baseSkyIntensity = 1.5f;
        public float baseFogDensity = 0.02f;
        public float maxFogDensity = 0.07f;

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
            ApplyLook(Oblivion, sky, baseSkyIntensity, baseFogDensity, maxFogDensity);
        }

        private void Update()
        {
            // Каждый зажжённый Светильник держит сектор: рост Забвения падает на четверть.
            float hold = Mathf.Pow(0.75f, LitLamps);
            Oblivion = Mathf.Clamp01(Oblivion + growthPerSecond * hold * Time.deltaTime);
            ApplyLook(Oblivion, sky, baseSkyIntensity, baseFogDensity, maxFogDensity);

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
            ApplyLook(Oblivion, sky, baseSkyIntensity, baseFogDensity, maxFogDensity);
            PlatformBridge.Save("oblivion", Oblivion.ToString("F3", CultureInfo.InvariantCulture));
            Debug.Log("[Loomfall] Светильник держит сектор: Забвение " + Oblivion.ToString("F2", CultureInfo.InvariantCulture));
        }

        /// <summary>Чем выше Забвение, тем гуще и темнее туман, тусклее луч и серее окружение.</summary>
        public static void ApplyLook(float oblivion, Light sky, float baseSkyIntensity, float baseFogDensity, float maxFogDensity)
        {
            float t = Mathf.Clamp01(oblivion);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = Color.Lerp(
                new Color(0.075f, 0.085f, 0.115f),
                new Color(0.028f, 0.03f, 0.038f),
                t);
            RenderSettings.fogDensity = Mathf.Lerp(baseFogDensity, maxFogDensity, t);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Color.Lerp(
                new Color(0.13f, 0.15f, 0.19f),
                new Color(0.07f, 0.074f, 0.082f),
                t);
            RenderSettings.ambientEquatorColor = Color.Lerp(
                new Color(0.07f, 0.075f, 0.09f),
                new Color(0.035f, 0.037f, 0.042f),
                t);
            RenderSettings.ambientGroundColor = Color.Lerp(
                new Color(0.025f, 0.025f, 0.03f),
                new Color(0.012f, 0.012f, 0.015f),
                t);

            if (sky != null) sky.intensity = Mathf.Lerp(baseSkyIntensity, baseSkyIntensity * 0.35f, t);
        }
    }
}