using System.Globalization;
using UnityEngine;
using UnityEngine.Rendering;

namespace Loomfall
{
    /// <summary>Забвение: число 0..1, растёт само, откатывается Светильниками и гасит свет мира.</summary>
    public class OblivionClock : MonoBehaviour
    {
        public static OblivionClock Instance { get; private set; }

        public float growthPerSecond = 0.012f;
        public Light[] lights;
        public float[] baseIntensities;
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
            ApplyLook(Oblivion, lights, baseIntensities, baseFogDensity, maxFogDensity);
        }

        private void Update()
        {
            // Каждый зажжённый Светильник держит сектор: рост Забвения падает на четверть.
            float hold = Mathf.Pow(0.75f, LitLamps);
            Oblivion = Mathf.Clamp01(Oblivion + growthPerSecond * hold * Time.deltaTime);
            ApplyLook(Oblivion, lights, baseIntensities, baseFogDensity, maxFogDensity);

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
            ApplyLook(Oblivion, lights, baseIntensities, baseFogDensity, maxFogDensity);
            PlatformBridge.Save("oblivion", Oblivion.ToString("F3", CultureInfo.InvariantCulture));
            Debug.Log("[Loomfall] Светильник держит сектор: Забвение " + Oblivion.ToString("F2", CultureInfo.InvariantCulture));
        }

        /// <summary>Чем выше Забвение, тем гуще и темнее туман, тусклее весь свет и серее окружение.</summary>
        public static void ApplyLook(float oblivion, Light[] lights, float[] baseIntensities, float baseFogDensity, float maxFogDensity)
        {
            float t = Mathf.Clamp01(oblivion);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = Color.Lerp(
                new Color(0.075f, 0.085f, 0.115f),
                new Color(0.026f, 0.028f, 0.035f),
                t);
            RenderSettings.fogDensity = Mathf.Lerp(baseFogDensity, maxFogDensity, t);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Color.Lerp(
                new Color(0.13f, 0.15f, 0.19f),
                new Color(0.05f, 0.053f, 0.06f),
                t);
            RenderSettings.ambientEquatorColor = Color.Lerp(
                new Color(0.07f, 0.075f, 0.09f),
                new Color(0.025f, 0.026f, 0.03f),
                t);
            RenderSettings.ambientGroundColor = Color.Lerp(
                new Color(0.025f, 0.025f, 0.03f),
                new Color(0.008f, 0.008f, 0.01f),
                t);

            if (lights == null) return;
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] == null) continue;
                float basis = baseIntensities != null && i < baseIntensities.Length
                    ? baseIntensities[i]
                    : lights[i].intensity;
                lights[i].intensity = Mathf.Lerp(basis, basis * 0.25f, t);
            }
        }
    }
}