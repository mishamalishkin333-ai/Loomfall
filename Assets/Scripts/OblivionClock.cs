using System.Globalization;
using UnityEngine;

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
        public float maxFogDensity = 0.055f;

        public float Oblivion { get; private set; }
        public int LitLamps { get; private set; }

        private Color baseSkyColor = new Color(0.13f, 0.15f, 0.19f);
        private Color greySkyColor = new Color(0.17f, 0.17f, 0.175f);
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
            ApplyLook();
        }

        private void Update()
        {
            // Каждый зажжённый Светильник держит сектор: рост Забвения падает на четверть.
            float hold = Mathf.Pow(0.75f, LitLamps);
            Oblivion = Mathf.Clamp01(Oblivion + growthPerSecond * hold * Time.deltaTime);
            ApplyLook();

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
            ApplyLook();
            PlatformBridge.Save("oblivion", Oblivion.ToString("F3", CultureInfo.InvariantCulture));
            Debug.Log("[Loomfall] Светильник держит сектор: Забвение " + Oblivion.ToString("F2", CultureInfo.InvariantCulture));
        }

        /// <summary>Чем выше Забвение, тем гуще туман, тусклее луч и серее окружение.</summary>
        private void ApplyLook()
        {
            RenderSettings.fogDensity = Mathf.Lerp(baseFogDensity, maxFogDensity, Oblivion);
            RenderSettings.ambientSkyColor = Color.Lerp(baseSkyColor, greySkyColor, Oblivion * 0.7f);
            if (sky != null) sky.intensity = Mathf.Lerp(baseSkyIntensity, baseSkyIntensity * 0.45f, Oblivion);
        }
    }
}