using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Loomfall
{
    /// <summary>Звук двора: шаги Ткача, ткачество у узла, отклик интерфейса и колокол нового яруса.</summary>
    public class YardAudio : MonoBehaviour
    {
        public Transform player;
        public float stepDistance = 2.2f;

        private readonly List<AudioClip> steps = new List<AudioClip>();
        private readonly List<AudioClip> weave = new List<AudioClip>();
        private AudioClip click;
        private AudioClip bell;

        private AudioSource oneShot;
        private Vector3 lastStepPosition;
        private int stepIndex;
        private int weaveIndex;
        private int lastTier = 1;

        private void Awake()
        {
            foreach (AudioClip clip in Resources.LoadAll<AudioClip>("Audio"))
            {
                string id = clip.name.ToLowerInvariant();
                if (id.StartsWith("step")) steps.Add(clip);
                else if (id.StartsWith("weave")) weave.Add(clip);
                else if (id.StartsWith("bell")) bell = clip;
                else if (id.Contains("click")) click = clip;
            }

            oneShot = gameObject.AddComponent<AudioSource>();
            oneShot.playOnAwake = false;
            oneShot.spatialBlend = 0f;
            lastStepPosition = player != null ? player.position : transform.position;
            Debug.Log("[Loomfall] Звук: шагов " + steps.Count + ", ткачества " + weave.Count + ", колокол " + (bell != null));
        }

        private void Update()
        {
            if (player == null || oneShot == null) return;

            if (Vector3.Distance(player.position, lastStepPosition) > stepDistance)
            {
                lastStepPosition = player.position;
                Play(steps, ref stepIndex, 0.35f, 0.95f);
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
            {
                Play(weave, ref weaveIndex, 0.6f, 1f);
                if (click != null) oneShot.PlayOneShot(click, 0.35f);
            }

            GameSession session = GameSession.Instance;
            if (session != null && session.Tier != lastTier)
            {
                lastTier = session.Tier;
                if (bell != null) oneShot.PlayOneShot(bell, 0.7f);
            }
        }

        private void Play(List<AudioClip> clips, ref int index, float volume, float pitch)
        {
            if (clips.Count == 0) return;
            AudioClip clip = clips[index % clips.Count];
            index++;
            oneShot.pitch = pitch;
            oneShot.PlayOneShot(clip, volume);
        }
    }
}