using UnityEngine;
using System;
using System.Collections;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
    public enum PlayerActivityState
    {
        Free,
        PickingUp,
        Dashing,
    }

    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonController : MonoBehaviour
    {
        [SerializeField] private StatController _stats;

        [Header("Input Settings")]
        [Tooltip("If true, rotation is controlled by the Mouse cursor (Raycast). If false, it uses the Gamepad/Stick input.")]
        public bool UseMouseRotation = true;

        [Header("Player")]
        public float MoveSpeed = 2.0f;
        public float SprintSpeed = 5.335f;
        [Tooltip("How fast the character turns to face the mouse/stick")]
        public float RotationSmoothTime = 0.05f;
        public float SpeedChangeRate = 10.0f;

        [Header("Audio")]
        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;
        [SerializeField] private SoundDef dashSound;

        [Space(10)]
        public float Gravity = -15.0f;
        public float FallTimeout = 0.15f;

        [Header("Player Dash")]
        public float DashSpeed = 20.0f;
        public float DashDuration = 0.2f;
        public float DashCooldown = 1.0f;
        public float InvincibilityDuration = 0.2f;

        [Header("Player Grounded")]
        public bool Grounded = true;
        public float GroundedOffset = -0.14f;
        public float GroundedRadius = 0.28f;
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        public GameObject CinemachineCameraTarget;
        public float TopClamp = 70.0f;
        public float BottomClamp = -30.0f;
        public float CameraAngleOverride = 0.0f;
        public bool LockCameraPosition = false;

        public event Action OnPickupAnimationComplete;
        public event Action<Vector3> OnDashStart;

        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;
        private PlayerStamina _playerStamina;
        private PlayerGarbageHandler _playerGarbageHandler;

        private float _fallTimeoutDelta;
        private float _dashCooldownTimer;

        private int _playerLayer;
        private int _phasingPlayerLayer;
        [SerializeField] private TrailRenderer _trailRenderer;

        private PlayerActivityState _currentState = PlayerActivityState.Free;
        private bool _isPickUpCancelable = false;

        // Input tracking for gamepad vs mouse persistence
        private bool _isUsingGamepad = false;

        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;
        private int _animIDPickUp;

#if ENABLE_INPUT_SYSTEM
        private PlayerInput _playerInput;
#endif
        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;
        private bool _hasAnimator;

        private void Awake()
        {
            if (_mainCamera == null) _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        }

        private void Start()
        {
            if (_stats != null)
            {
                _stats.InitializeStat(StatType.MoveSpeed, MoveSpeed);
                _stats.InitializeStat(StatType.SprintSpeed, SprintSpeed);
                _stats.InitializeStat(StatType.DashDuration, DashDuration);
            }

            _playerLayer = gameObject.layer;
            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
            _playerStamina = GetComponent<PlayerStamina>();

#if ENABLE_INPUT_SYSTEM
            _playerInput = GetComponent<PlayerInput>();
            _playerGarbageHandler = GetComponent<PlayerGarbageHandler>();
            _phasingPlayerLayer = LayerMask.NameToLayer("PhasingPlayer");
#endif
            AssignAnimationIDs();
            _fallTimeoutDelta = FallTimeout;
        }

        private void Update()
        {
            if (_dashCooldownTimer > 0.0f) _dashCooldownTimer -= Time.deltaTime;

            GroundedCheck();
            HandleState();
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
            _animIDPickUp = Animator.StringToHash("PickUp");
        }

        private void HandleState()
        {
            switch (_currentState)
            {
                case PlayerActivityState.Free:
                    ApplyGravity();
                    HandleRotation();
                    Move();
                    HandleDashInput();
                    HandleBoostInput();
                    break;
                case PlayerActivityState.PickingUp:
                    HandlePickingUpState();
                    break;
                case PlayerActivityState.Dashing:
                    ApplyGravity();
                    break;
            }
        }

        private void HandleRotation()
        {
#if ENABLE_INPUT_SYSTEM
            // GAMEPAD / CONTROLLER MODE
            if (!UseMouseRotation)
            {
                // Force cursor hidden and locked when in Controller mode
                if (Cursor.visible)
                {
                    Cursor.visible = false;
                    Cursor.lockState = CursorLockMode.Locked;
                }

                // Use the Input System 'look' vector (usually Right Stick)
                Vector2 lookInput = _input.look;

                // Only rotate if the stick is actually pushed (deadzone check)
                if (lookInput.sqrMagnitude > 0.1f)
                {
                    float targetAngle = Mathf.Atan2(lookInput.x, lookInput.y) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;
                    Quaternion targetRotation = Quaternion.Euler(0, targetAngle, 0);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 20f);
                }
            }
            // MOUSE MODE
            else
            {
                // Force cursor visible and free when in Mouse mode
                if (Cursor.lockState != CursorLockMode.None)
                {
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                }

                // Perform the Raycast logic (Isometric/Top-down style)
                Plane playerPlane = new Plane(Vector3.up, transform.position);
                Ray ray = _mainCamera.GetComponent<Camera>().ScreenPointToRay(Mouse.current.position.ReadValue());

                if (playerPlane.Raycast(ray, out float hitDist))
                {
                    Vector3 targetPoint = ray.GetPoint(hitDist);
                    Vector3 lookDirection = (targetPoint - transform.position).normalized;
                    lookDirection.y = 0; // Keep rotation flat on the ground

                    if (lookDirection != Vector3.zero)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 20f);
                    }
                }
            }
#endif
        }

        private void Move()
        {
            float currentMoveSpeed = _stats ? _stats.GetStat(StatType.MoveSpeed) : MoveSpeed;
            float sprintMultiplier = SprintSpeed / MoveSpeed;
            float currentSprintSpeed = currentMoveSpeed * sprintMultiplier;

            float targetSpeed = (_playerStamina != null && _playerStamina.IsBoostActive()) ? currentSprintSpeed : currentMoveSpeed;
            if (_playerGarbageHandler != null && _playerGarbageHandler.IsOverencumbered) targetSpeed /= 2f;
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;
            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            if (_input.move != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;
            }

            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }

        private void HandleDashInput()
        {
            if (_input.jump)
            {
                _input.jump = false;
                if (_dashCooldownTimer <= 0.0f)
                {
                    SoundManager.Instance.Play(dashSound, transform.position);
                    StartCoroutine(Dash());
                }
            }
        }

        private IEnumerator Dash()
        {
            _currentState = PlayerActivityState.Dashing;
            _dashCooldownTimer = DashCooldown;

            if (_trailRenderer != null) _trailRenderer.emitting = true;
            StartCoroutine(BecomeInvincible(InvincibilityDuration));

            float startTime = Time.time;

            Vector3 dashDirection;
            if (_input.move != Vector2.zero)
            {
                Vector3 inputDir = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;
                float dashAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;
                dashDirection = Quaternion.Euler(0.0f, dashAngle, 0.0f) * Vector3.forward;
            }
            else
            {
                dashDirection = transform.forward;
            }

            OnDashStart?.Invoke(dashDirection);

            float currentDuration = _stats ? _stats.GetStat(StatType.DashDuration) : DashDuration;

            while (Time.time < startTime + currentDuration)
            {
                Vector3 horizontalMovement = dashDirection.normalized * (DashSpeed * Time.deltaTime);
                Vector3 verticalMovement = new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime;
                _controller.Move(horizontalMovement + verticalMovement);
                yield return null;
            }

            if (_trailRenderer != null) _trailRenderer.emitting = false;
            _currentState = PlayerActivityState.Free;
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (FootstepAudioClips.Length > 0)
                {
                    var index = UnityEngine.Random.Range(0, FootstepAudioClips.Length);
                    AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
                }
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }

        public void UpgradePlayerSpeed(float increaseAmount)
        {
            MoveSpeed += increaseAmount;
            SprintSpeed += increaseAmount;
        }

        private void GroundedCheck()
        {
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
            if (_hasAnimator) _animator.SetBool(_animIDGrounded, Grounded);
        }

        private void ApplyGravity()
        {
            if (Grounded)
            {
                _fallTimeoutDelta = FallTimeout;
                if (_hasAnimator) _animator.SetBool(_animIDFreeFall, false);
                if (_verticalVelocity < 0.0f) _verticalVelocity = -2f;
            }
            else
            {
                if (_fallTimeoutDelta >= 0.0f) _fallTimeoutDelta -= Time.deltaTime;
                else if (_hasAnimator) _animator.SetBool(_animIDFreeFall, true);
                _input.jump = false;
            }

            if (_verticalVelocity < _terminalVelocity) _verticalVelocity += Gravity * Time.deltaTime;
        }

        private void HandlePickingUpState()
        {
            ApplyGravity();
            if (_isPickUpCancelable && _input.move != Vector2.zero) FinishPickUp();
            _controller.Move(new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, 0f);
                _animator.SetFloat(_animIDMotionSpeed, 0f);
            }
        }

        private IEnumerator BecomeInvincible(float duration)
        {
            gameObject.layer = _phasingPlayerLayer;
            if (_controller != null) _controller.detectCollisions = false;
            yield return new WaitForSeconds(duration);
            gameObject.layer = _playerLayer;
            if (_controller != null) _controller.detectCollisions = true;
        }

        public bool IsInvulnerable() => _currentState == PlayerActivityState.Dashing;

        private void HandleBoostInput()
        {
            if (_input.sprint)
            {
                _playerStamina?.TryActivateBoost();
                _input.sprint = false;
            }
        }

        public bool StartPickUp(bool isCancelable)
        {
            if (!Grounded || _currentState != PlayerActivityState.Free) return false;
            _isPickUpCancelable = isCancelable;
            _currentState = PlayerActivityState.PickingUp;
            if (_hasAnimator) _animator.SetTrigger("StartPickUpTrigger");
            return true;
        }

        public void OnPickupAnimationFinished() => FinishPickUp();

        private void FinishPickUp()
        {
            if (_currentState == PlayerActivityState.PickingUp)
            {
                _currentState = PlayerActivityState.Free;
                OnPickupAnimationComplete?.Invoke();
                if (_hasAnimator) _animator.SetTrigger("ReturnToMovement");
            }
        }


      
    }


}