using UnityEngine;
using UnityEngine.InputSystem;

namespace Loomfall
{
    /// <summary>Ход игры: подключение площадки, натяжение нитей у узла и сохранение прогресса.</summary>
    public class GameSession : MonoBehaviour
    {
        public Transform player;
        public Transform loom;
        public float loomRadius = 4.5f;
        public int threadsPerTier = 3;

        public static GameSession Instance { get; private set; }

        public int Tier { get; private set; } = 1;
        public int Threads { get; private set; }

        private void Awake()
        {
            Instance = this;
            PlatformBridge.Init();
            Tier = ReadInt("tier", 1);
            Threads = ReadInt("threads", 0);
            Debug.Log("[Loomfall] Ярус " + Tier + ", нитей натянуто " + Threads);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || player == null || loom == null) return;
            if (keyboard.rKey.wasPressedThisFrame)
            {
                // Награждаемая реклама площадки: в VK это VKWebAppShowNativeAds, в Яндексе — showFullscreenAdv.
                PlatformBridge.ShowRewardAd();
            }

            if (!keyboard.eKey.wasPressedThisFrame) return;
            if (Vector3.Distance(player.position, loom.position) > loomRadius) return;

            Threads++;
            if (Threads >= threadsPerTier)
            {
                Threads = 0;
                Tier++;
                Debug.Log("[Loomfall] Ярус пройден: нить ведёт выше");
            }

            PlatformBridge.Save("tier", Tier.ToString());
            PlatformBridge.Save("threads", Threads.ToString());
            Debug.Log("[Loomfall] Нить натянута: ярус " + Tier + ", нитей " + Threads);
        }

        private static int ReadInt(string key, int fallback)
        {
            return int.TryParse(PlatformBridge.Load(key, fallback.ToString()), out int value) ? value : fallback;
        }
    }
}