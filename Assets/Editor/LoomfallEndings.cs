using UnityEditor;
using UnityEngine;

namespace Loomfall.EditorTools
{
    /// <summary>Проверка правил ярусов и трёх концовок без запуска игры.</summary>
    public static class LoomfallEndings
    {
        /// <summary>Печатает условия ярусов и полную матрицу концовок.</summary>
        public static void Compare()
        {
            for (int tier = 1; tier <= Loomfall.GameSession.MaxTier; tier++)
            {
                Debug.Log("[Loomfall] Ярус " + tier + ": нужно нитей " + Loomfall.GameSession.RequiredThreads(tier));
            }

            float[] oblivions = { 0f, 0.3f, 0.7f };
            int[] shards = { 0, 5 };
            int[] lamps = { 0, 3 };

            foreach (float oblivion in oblivions)
            {
                foreach (int shardCount in shards)
                {
                    foreach (int lampCount in lamps)
                    {
                        string ending = Loomfall.GameSession.DecideEnding(oblivion, shardCount, lampCount);
                        Debug.Log(
                            "[Loomfall] Забвение " + oblivion.ToString("F1") +
                            ", осколков " + shardCount +
                            ", светильников " + lampCount +
                            " -> концовка «" + ending + "»");
                    }
                }
            }
        }
    }
}