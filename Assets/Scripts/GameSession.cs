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
            Shards = ReadInt("shards", 0);
            Ending = PlatformBridge.Load("ending", string.Empty);
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
            if (Threads >= RequiredThreads(Tier))
            {
                Threads = 0;
                if (Tier < MaxTier)
                {
                    Tier++;
                    Debug.Log("[Loomfall] Ярус пройден: нить ведёт выше, открыт ярус " + Tier);
                }
                else
                {
                    Ending = DecideEnding(
                        OblivionClock.Instance != null ? OblivionClock.Instance.Oblivion : 0f,
                        Shards,
                        OblivionClock.Instance != null ? OblivionClock.Instance.LitLamps : 0);
                    PlatformBridge.Save("ending", Ending);
                    Debug.Log("[Loomfall] Ярусов больше нет. Концовка: " + Ending);
                }
            }

            PlatformBridge.Save("tier", Tier.ToString());
            PlatformBridge.Save("threads", Threads.ToString());
            Debug.Log("[Loomfall] Нить натянута: ярус " + Tier + ", нитей " + Threads);
        }

        public int Shards { get; private set; }
        public string Codex { get; private set; } = string.Empty;
        public float CodexTime { get; private set; }

        /// <summary>Осколок памяти: чужая жизнь, запись в Кодексе и никакой пользы в бою.</summary>
        public void CollectShard(string record)
        {
            Shards++;
            Codex = record;
            CodexTime = Time.time;
            PlatformBridge.Save("shards", Shards.ToString());
            Debug.Log("[Loomfall] Записей в Кодексе: " + Shards);
        }

        /// <summary>Реплика мира на действие Ткача: каждая трата Эха стирает чью-то секунду.</summary>
        public void Shout(string word)
        {
            Codex = word == "Слово"
                ? "Слово сказано вслух. Кто-то чужой забыл эту секунду."
                : "Разрыв прошёл по камню. Память яруса стала тоньше.";
            CodexTime = Time.time;
        }

        public const int MaxTier = 5;

        public string Ending { get; private set; } = string.Empty;

        /// <summary>У каждого яруса своё условие: чем выше, тем больше нитей нужно натянуть.</summary>
        public static int RequiredThreads(int tier)
        {
            return Mathf.Clamp(tier, 1, MaxTier) + 2;
        }

        /// <summary>Три концовки по библии: Слово, Разрыв и тихое Забвение.</summary>
        public static string DecideEnding(float oblivion, int shards, int lamps)
        {
            if (shards >= 4 && lamps >= 2 && oblivion < 0.4f) return "Слово";
            if (oblivion >= 0.6f) return "Разрыв";
            return "Забвение";
        }

        private static int ReadInt(string key, int fallback)
        {
            return int.TryParse(PlatformBridge.Load(key, fallback.ToString()), out int value) ? value : fallback;
        }
    }
}