using UnityEngine;
using UnityEngine.InputSystem;

namespace Loomfall
{
    /// <summary>Управление Ткачом: WASD, бег на Shift, прыжок на пробел, анимация по скорости.</summary>
    public class WeaverController : MonoBehaviour
    {
        public float walkSpeed = 3.2f;
        public float runSpeed = 6.4f;
        public float jumpHeight = 1.1f;
        public float turnSpeed = 12f;
        public float gravity = 9.81f;

        /// <summary>Пробный ввод для проверок без клавиатуры: в игре не используется.</summary>
        public Vector2 testMove;
        public bool testJump;
        public bool testSprint;

        /// <summary>Сам Ткач: доступ без поиска объектов по сцене.</summary>
        public static WeaverController Instance { get; private set; }

        private Animator animator;
        private float verticalSpeed;

        private void Awake()
        {
            Instance = this;
            animator = GetComponent<Animator>();
            // Аниматор живёт на модели внутри узла, поэтому ищем и в детях.
            if (animator == null) animator = GetComponentInChildren<Animator>();
            // Анимация не должна двигать Ткача: позицию держит только этот контроллер.
            if (animator != null) animator.applyRootMotion = false;
        }

        private void Start()
        {
            // Опираемся на пол под героем: сцена могла сохранить позицию от прежней постройки.
            Vector3 start = transform.position;
            if (Physics.Raycast(start + Vector3.up * 8f, Vector3.down, out RaycastHit ground, 40f))
            {
                transform.position = new Vector3(start.x, ground.point.y, start.z);
                Debug.Log("[Loomfall] Ткач встал на пол: " + transform.position.ToString("F2")
                    + " (было " + start.ToString("F2") + ")");
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;

            Vector2 move = testMove;
            bool sprint = testSprint;
            bool jump = testJump;
            testJump = false;

            if (keyboard != null)
            {
                move = Vector2.zero;
                if (keyboard.aKey.isPressed) move.x -= 1f;
                if (keyboard.dKey.isPressed) move.x += 1f;
                if (keyboard.sKey.isPressed) move.y -= 1f;
                if (keyboard.wKey.isPressed) move.y += 1f;
                sprint = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
                jump = keyboard.spaceKey.wasPressedThisFrame;
            }

            if (move.sqrMagnitude > 1f) move.Normalize();
            float speed = move.magnitude * (sprint ? runSpeed : walkSpeed);

            if (move.sqrMagnitude > 0.001f)
            {
                Vector3 direction = (Vector3.forward * move.y + Vector3.right * move.x).normalized;
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(direction),
                    turnSpeed * Time.deltaTime);
                transform.position += direction * speed * Time.deltaTime;
            }

            bool onGround = Mathf.Abs(verticalSpeed) < 0.001f;
            if (jump && onGround)
            {
                verticalSpeed = Mathf.Sqrt(2f * gravity * jumpHeight);
                if (animator != null) animator.SetTrigger("jump");
            }

            verticalSpeed -= gravity * Time.deltaTime;
            Vector3 position = transform.position;
            position.y += verticalSpeed * Time.deltaTime;

            // Земля ищется лучом вниз: так Ткач сам поднимается по лестнице на верхний ярус.
            float floorHeight = 0f;
            if (Physics.Raycast(position + Vector3.up * 1.4f, Vector3.down, out RaycastHit hit, 14f))
            {
                floorHeight = hit.point.y;
            }

            if (position.y <= floorHeight)
            {
                position.y = floorHeight;
                verticalSpeed = 0f;
            }

            transform.position = position;
            if (animator != null) animator.SetFloat("speed", speed);
        }
    }
}