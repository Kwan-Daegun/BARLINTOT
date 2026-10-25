    using UnityEngine;

    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        public float speed = 4f;
        public float gravity = -9.81f;
        public AudioClip footstepSound;
        public float walkPitch = 1f;
        public float runPitch = 1.5f;

        private bool isSprinting = false;
        private CharacterController controller;
        private Vector3 velocity;
        private AudioSource audioSource;
        public Animator animator;
        private static readonly int SpeedHash = Animator.StringToHash("Speed");

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            audioSource = GetComponent<AudioSource>();
            audioSource.clip = footstepSound;
            audioSource.loop = true;
        }

        private void Update()
{
    if (controller.isGrounded && velocity.y < 0)
        velocity.y = -2f;

    isSprinting = Input.GetKey(KeyCode.LeftShift);
    speed = isSprinting ? 10f : 4f;

    float x = Input.GetAxis("Horizontal");
    float z = Input.GetAxis("Vertical");
    bool isMoving = (x != 0 || z != 0) && controller.isGrounded;

    // 0 = stopped, 1 = walking, 1.5 = sprinting (playback speed multiplier)
    float targetAnimSpeed = !isMoving ? 0f : (isSprinting ? 1.5f : 1f);
    animator.SetFloat(SpeedHash, targetAnimSpeed, 0.1f, Time.deltaTime);

    if (isMoving)
    {
        audioSource.pitch = isSprinting ? runPitch : walkPitch;
        if (!audioSource.isPlaying)
            audioSource.Play();
    }
    else
    {
        audioSource.Stop();
    }

    Vector3 move = transform.right * x + transform.forward * z;
    controller.Move(move * (speed * Time.deltaTime));

    velocity.y += gravity * Time.deltaTime;
    controller.Move(velocity * Time.deltaTime);
}
    }