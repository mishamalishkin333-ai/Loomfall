using UnityEngine;

namespace Loomfall
{
    /// <summary>Гул яруса: петля без швов, громче по мере роста Забвения.</summary>
    public class AmbienceLoop : MonoBehaviour
    {
        public float quietVolume = 0.12f;
        public float loudVolume = 0.4f;

        private AudioSource source;

        private void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.clip = BuildDrone(22050, 6f);
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = quietVolume;
            source.Play();
            Debug.Log("[Loomfall] Гул яруса звучит: " + source.clip.samples + " отсчётов");
        }

        private void Update()
        {
            float oblivion = OblivionClock.Instance != null ? OblivionClock.Instance.Oblivion : 0f;
            if (source != null) source.volume = Mathf.Lerp(quietVolume, loudVolume, oblivion);
        }

        /// <summary>Гул: низкие синусы и медленный шум. Частоты кратны длине петли, поэтому стык сходится без щелчка.</summary>
        public static AudioClip BuildDrone(int sampleRate, float seconds)
        {
            int samples = Mathf.RoundToInt(sampleRate * seconds);
            var data = new float[samples];
            int cyclesA = Mathf.RoundToInt(55f * seconds);
            int cyclesB = Mathf.RoundToInt(82.5f * seconds);
            int cyclesC = Mathf.RoundToInt(27.5f * seconds);

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float low = Mathf.Sin(2f * Mathf.PI * cyclesA * t);
                float mid = Mathf.Sin(2f * Mathf.PI * cyclesB * t) * 0.5f;
                float deep = Mathf.Sin(2f * Mathf.PI * cyclesC * t) * 0.35f;
                data[i] = (low + mid + deep) / 3f * 0.5f;
            }

            var clip = AudioClip.Create("ГулЯруса", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}