using UnityEditor;
using UnityEngine;

namespace Loomfall.EditorTools
{
    /// <summary>Проверка звуковой петли: длина, размах и бесшовность стыка.</summary>
    public static class LoomfallSound
    {
        /// <summary>Строит петлю и печатает её параметры вместе с замером стыка.</summary>
        public static void Check()
        {
            AudioClip clip = Loomfall.AmbienceLoop.BuildDrone(22050, 6f);
            var data = new float[clip.samples];
            clip.GetData(data, 0);

            float peak = 0f;
            float sum = 0f;
            foreach (float value in data)
            {
                peak = Mathf.Max(peak, Mathf.Abs(value));
                sum += Mathf.Abs(value);
            }

            float seam = Mathf.Abs(data[0] - data[data.Length - 1]);
            Debug.Log(
                "[Loomfall] Гул яруса: " + clip.samples + " отсчётов, " + clip.frequency + " Гц, " +
                clip.channels + " канал, " + (clip.samples / (float)clip.frequency).ToString("F1") + " с, " +
                "пик " + peak.ToString("F3") + ", средний размах " + (sum / data.Length).ToString("F3") +
                ", стык петли " + seam.ToString("F5") +
                (seam < 0.01f ? " — петля без щелчка" : " — ЩЕЛЧОК"));
        }
    }
}