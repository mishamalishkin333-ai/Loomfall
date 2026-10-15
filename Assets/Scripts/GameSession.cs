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
            ApplyDeviceQuality();
            PlatformBridge.Init();
            Tier = ReadInt("tier", 1);
            Threads = ReadInt("threads", 0);
            Shards = ReadInt("shards", 0);
            Ending = PlatformBridge.Load("ending", string.Empty);
            Debug.Log("[Loomfall] Ярус " + Tier + ", нитей натянуто " + Threads);
        }

        /// <summary>
        /// Профиль качества по устройству: на телефоне легче (без сглаживания, ближе тени),
        /// на компьютере полный. Названия профилей задаются в настройках качества проекта.
        /// </summary>
        public static int ChooseLevel(bool touch, string[] names)
        {
            int mobile = -1;
            int desktop = -1;
            for (int i = 0; i < names.Length; i++)
            {
                if (names[i] == "Mobile") mobile = i;
                else if (names[i] == "PC") desktop = i;
            }

            if (touch) return mobile >= 0 ? mobile : desktop;
            return desktop >= 0 ? desktop : mobile;
        }

        private static void ApplyDeviceQuality()
        {
            bool touch = Touchscreen.current != null;
            string[] names = QualitySettings.names;
            int level = ChooseLevel(touch, names);
            string name = level >= 0 ? names[level] : "не найден";
            if (level >= 0) QualitySettings.SetQualityLevel(level, true);
            Debug.Log("[Loomfall] Качество: " + name + ", сенсорный экран: " + (touch ? "да" : "нет")
                + ", уровней в проекте: " + names.Length);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.tabKey.wasPressedThisFrame) CodexOpen = !CodexOpen;
            if (player == null || loom == null) return;
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
        public string CodexLog { get; private set; } = string.Empty;
        public bool CodexOpen { get; private set; }

        /// <summary>Осколок памяти: чужая жизнь, запись в Кодексе и никакой пользы в бою.</summary>
        public void CollectShard(string record)
        {
            Shards++;
            Codex = record;
            if (CodexLog.Length == 0) CodexLog = record;
            else if (!CodexLog.Contains(record)) CodexLog = CodexLog + "\n" + record;
            CodexTime = Time.time;
            PlatformBridge.Save("shards", Shards.ToString());
            Debug.Log("[Loomfall] Записей в Кодексе: " + Shards);
        }

        /// <summary>Шаг к нити в Сердце Ткани. «Третий узор» держится, только если собрано и зажжено.</summary>
        public bool ChooseEnding(string ending)
        {
            if (ending == "Третий узор")
            {
                int lamps = OblivionClock.Instance != null ? OblivionClock.Instance.LitLamps : 0;
                float oblivion = OblivionClock.Instance != null ? OblivionClock.Instance.Oblivion : 1f;
                if (Shards < 4 || lamps < 2 || oblivion >= 0.4f) return false;
            }

            Ending = ending;
            PlatformBridge.Save("ending", Ending);
            Debug.Log("[Loomfall] Концовка выбрана шагом: " + Ending);
            return true;
        }

        /// <summary>Реплика мира на действие Ткача: каждая трата Эха стирает чью-то секунду.</summary>
        public void Shout(string word)
        {
            if (word == "Отказ") Codex = "Третий узел не держится: в нём нет ни собранного, ни зажжённого.";
            else if (word == "Слово") Codex = "Слово сказано вслух. Кто-то чужой забыл эту секунду.";
            else Codex = "Разрыв прошёл по камню. Память яруса стала тоньше.";
            CodexTime = Time.time;
        }

        public const int MaxTier = 5;

        public string Ending { get; private set; } = string.Empty;

        /// <summary>У каждого яруса своё условие: чем выше, тем больше нитей нужно натянуть.</summary>
        public static int RequiredThreads(int tier)
        {
            return Mathf.Clamp(tier, 1, MaxTier) + 2;
        }

        /// <summary>Три концовки по библии: сброс, разрыв и тайный «Третий узор».</summary>
        public static string DecideEnding(float oblivion, int shards, int lamps)
        {
            if (shards >= 4 && lamps >= 2 && oblivion < 0.4f) return "Третий узор";
            if (oblivion >= 0.6f) return "Разрыв";
            return "Сброс";
        }

        private static int ReadInt(string key, int fallback)
        {
            return int.TryParse(PlatformBridge.Load(key, fallback.ToString()), out int value) ? value : fallback;
        }
    }
}