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
        #region Configuration
        [Header("External References")]
        [SerializeField] private StatController _stats;
        [SerializeField] private TrailRenderer _trailRenderer;
        [SerializeField] private SoundDef _dashSound; // Renamed to standard convention

        [Header("Input Settings")]
        [Tooltip("If true, rotation is controlled by the Mouse cursor (Raycast). If false, it uses the Gamepad/Stick input.")]
        public bool UseMouseRotation = true;

        [Header("Movement")]
        public float MoveSpeed = 2.0f;
        public float SprintSpeed = 5.335f;
        public float SpeedChangeRate = 10.0f;
        public float RotationSmoothTime = 0.05f;

        [Header("Gravity & Grounding")]
        public float Gravity = -15.0f;
        public float FallTimeout = 0.15f;
        public float GroundedOffset = -0.14f;
        public float GroundedRadius = 0.28f;
        public LayerMask GroundLayers;
        public bool Grounded = true;

        [Header("Dash")]
        public float DashSpeed = 20.0f;
        public float DashDuration = 0.2f;
        public float DashCooldown = 1.0f;
        public float InvincibilityDuration = 0.2f;

        [Header("Audio")]
        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Header("Cinemachine (Camera Follow)")]
        public GameObject CinemachineCameraTarget;
        public float TopClamp = 70.0f;
        public float BottomClamp = -30.0f;
        public float CameraAngleOverride = 0.0f;
        public bool LockCameraPosition = false;
        #endregion

        #region Events
        public event Action OnPickupAnimationComplete;
        public event Action<Vector3> OnDashStart;
        #endregion

        #region Internal State
        // State
        private PlayerActivityState _currentState = PlayerActivityState.Free;
        private bool _isPickUpCancelable = false;
        private float _fallTimeoutDelta;
        private float _dashCooldownTimer;

        // Physics variables
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        // Layer caching
        private int _playerLayer;
        private int _phasingPlayerLayer;

        // Component References
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;
        private PlayerStamina _playerStamina;
        private PlayerGarbageHandler _playerGarbageHandler;

        // Helper Classes
        private PlayerAnimatorHandler _animHandler;
        #endregion

        private void Awake()
        {
            if (_mainCamera == null) _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");

            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
            _playerStamina = GetComponent<PlayerStamina>();
            _playerGarbageHandler = GetComponent<PlayerGarbageHandler>();

            // Initialize Animator Handler
            if (TryGetComponent(out Animator animator))
                _animHandler = new PlayerAnimatorHandler(animator);

            _playerLayer = gameObject.layer;
#if ENABLE_INPUT_SYSTEM
            _phasingPlayerLayer = LayerMask.NameToLayer("PhasingPlayer");
#endif
        }

        private void Start()
        {
            InitializeStats();
            _fallTimeoutDelta = FallTimeout;
        }

        private void Update()
        {
            UpdateTimers();
            CheckGrounded();
            UpdateStateMachine();
        }

        private void InitializeStats()
        {
            if (_stats != null)
            {
                _stats.InitializeStat(StatType.MoveSpeed, MoveSpeed);
                _stats.InitializeStat(StatType.SprintSpeed, SprintSpeed);
                _stats.InitializeStat(StatType.DashDuration, DashDuration);
            }
        }

        private void UpdateTimers()
        {
            if (_dashCooldownTimer > 0.0f) _dashCooldownTimer -= Time.deltaTime;
        }

        #region State Machine

        private void UpdateStateMachine()
        {
            switch (_currentState)
            {
                case PlayerActivityState.Free:
                    UpdateFreeState();
                    break;
                case PlayerActivityState.PickingUp:
                    UpdatePickingUpState();
                    break;
                case PlayerActivityState.Dashing:
                    UpdateDashingState();
                    break;
            }
        }

        private void UpdateFreeState()
        {
            ApplyGravity();
            HandleRotation();
            HandleMovement();
            HandleDashInput();
            HandleBoostInput();
        }

        private void UpdatePickingUpState()
        {
            ApplyGravity();

            // Allow cancelling pickup if configured and moving
            if (_isPickUpCancelable && _input.move != Vector2.zero)
                FinishPickUp();

            // Apply only vertical movement (Gravity)
            _controller.Move(new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            _animHandler?.StopMotion();
        }

        private void UpdateDashingState()
        {
            ApplyGravity();
            // Movement is handled by the Coroutine, but gravity is updated here
        }

        #endregion

        #region Movement & Physics

        private void HandleMovement()
        {
            float targetSpeed = CalculateTargetSpeed();

            // Check for zero input
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            // Speed Blending (Acceleration/Deceleration)
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

            // Calculate Direction
            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            // Rotation Logic for Movement direction (distinct from Looking direction)
            if (_input.move != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;
            }

            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            // Move the controller
            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            // Update Animation
            float baseSpeed = _speed / (_stats ? _stats.GetStat(StatType.MoveSpeed) : MoveSpeed);
            _animHandler?.UpdateMovementAnimation(targetDirection.normalized, baseSpeed, transform);
        }

        private float CalculateTargetSpeed()
        {
            float currentMoveSpeed = _stats ? _stats.GetStat(StatType.MoveSpeed) : MoveSpeed;
            float sprintMultiplier = SprintSpeed / MoveSpeed;
            float currentSprintSpeed = currentMoveSpeed * sprintMultiplier;

            float targetSpeed = (_playerStamina != null && _playerStamina.IsBoostActive()) ? currentSprintSpeed : currentMoveSpeed;

            if (_playerGarbageHandler != null && _playerGarbageHandler.IsOverencumbered)
                targetSpeed /= 2f;

            return targetSpeed;
        }

        private void HandleRotation()
        {
#if ENABLE_INPUT_SYSTEM
            if (UseMouseRotation)
            {
                HandleMouseRotation();
            }
            else
            {
                HandleGamepadRotation();
            }
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private void HandleMouseRotation()
        {
            if (Cursor.lockState != CursorLockMode.None)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }

            Plane playerPlane = new Plane(Vector3.up, transform.position);
            Ray ray = _mainCamera.GetComponent<Camera>().ScreenPointToRay(Mouse.current.position.ReadValue());

            if (playerPlane.Raycast(ray, out float hitDist))
            {
                Vector3 targetPoint = ray.GetPoint(hitDist);
                Vector3 lookDirection = (targetPoint - transform.position).normalized;
                lookDirection.y = 0;

                if (lookDirection != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 20f);
                }
            }
        }

        private void HandleGamepadRotation()
        {
            if (Cursor.visible)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }

            Vector2 lookInput = _input.look;
            if (lookInput.sqrMagnitude > 0.1f)
            {
                float targetAngle = Mathf.Atan2(lookInput.x, lookInput.y) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;
                Quaternion targetRotation = Quaternion.Euler(0, targetAngle, 0);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 20f);
            }
        }
#endif

        private void ApplyGravity()
        {
            if (Grounded)
            {
                _fallTimeoutDelta = FallTimeout;
                _animHandler?.SetFreeFall(false);

                // Constant downward force to keep player grounded
                if (_verticalVelocity < 0.0f) _verticalVelocity = -2f;
            }
            else
            {
                if (_fallTimeoutDelta >= 0.0f) _fallTimeoutDelta -= Time.deltaTime;
                else _animHandler?.SetFreeFall(true);

                _input.jump = false; // Prevent jumping mid-air
            }

            if (_verticalVelocity < _terminalVelocity)
                _verticalVelocity += Gravity * Time.deltaTime;
        }

        private void CheckGrounded()
        {
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
            _animHandler?.SetGrounded(Grounded);
        }

        #endregion

        #region Actions (Dash, Boost, Pickup)

        private void HandleDashInput()
        {
            // Hub upgrade: dash is off until unlocked
            if (GameManager.Instance != null && !GameManager.Instance.IsHubUpgradeUnlocked(HubUpgradeKeys.DashUnlock))
                return;

            if (_input.jump)
            {
                _input.jump = false;
                if (_dashCooldownTimer <= 0.0f)
                {
                    StartCoroutine(DashRoutine());
                }
            }
        }

        private void HandleBoostInput()
        {
            if (_input.sprint)
            {
                _playerStamina?.TryActivateBoost();
                _input.sprint = false;
            }
        }

        private IEnumerator DashRoutine()
        {
            _currentState = PlayerActivityState.Dashing;
            _dashCooldownTimer = DashCooldown;
            SoundManager.Instance.Play(_dashSound, transform.position);

            if (_trailRenderer != null) _trailRenderer.emitting = true;

            // Handle Invincibility
            StartCoroutine(ToggleInvincibility(true, InvincibilityDuration));

            // Determine Dash Direction
            Vector3 dashDirection = CalculateDashDirection();
            OnDashStart?.Invoke(dashDirection);

            float startTime = Time.time;
            float currentDuration = _stats ? _stats.GetStat(StatType.DashDuration) : DashDuration;

            // Dash Loop
            while (Time.time < startTime + currentDuration)
            {
                Vector3 horizontalMovement = dashDirection.normalized * (DashSpeed * Time.deltaTime);
                Vector3 verticalMovement = new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime;

                _controller.Move(horizontalMovement + verticalMovement);
                _animHandler?.UpdateDashAnimation(dashDirection, transform);

                yield return null;
            }

            if (_trailRenderer != null) _trailRenderer.emitting = false;
            _currentState = PlayerActivityState.Free;
        }

        private Vector3 CalculateDashDirection()
        {
            if (_input.move != Vector2.zero)
            {
                Vector3 inputDir = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;
                float dashAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;
                return Quaternion.Euler(0.0f, dashAngle, 0.0f) * Vector3.forward;
            }
            return transform.forward;
        }

        private IEnumerator ToggleInvincibility(bool enable, float duration)
        {
            if (enable)
            {
                gameObject.layer = _phasingPlayerLayer;
                if (_controller != null) _controller.detectCollisions = false;

                yield return new WaitForSeconds(duration);

                gameObject.layer = _playerLayer;
                if (_controller != null) _controller.detectCollisions = true;
            }
        }

        public bool StartPickUp(bool isCancelable)
        {
            if (!Grounded || _currentState != PlayerActivityState.Free) return false;

            _isPickUpCancelable = isCancelable;
            _currentState = PlayerActivityState.PickingUp;
            _animHandler?.TriggerPickUp();
            return true;
        }

        private void FinishPickUp()
        {
            if (_currentState == PlayerActivityState.PickingUp)
            {
                _currentState = PlayerActivityState.Free;
                OnPickupAnimationComplete?.Invoke();
                _animHandler?.ReturnToMovement();
            }
        }

        public void OnPickupAnimationFinished() => FinishPickUp();

        #endregion

        #region Public Methods & Animation Events

        public void UpgradePlayerSpeed(float increaseAmount)
        {
            MoveSpeed += increaseAmount;
            SprintSpeed += increaseAmount;
        }

        public bool IsInvulnerable() => _currentState == PlayerActivityState.Dashing;

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f && FootstepAudioClips.Length > 0)
            {
                var index = UnityEngine.Random.Range(0, FootstepAudioClips.Length);
                AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }

        #endregion

        #region Nested Helper Classes

        
        private class PlayerAnimatorHandler
        {
            private readonly Animator _animator;

            // Animation IDs
            private readonly int _animIDSpeed;
            private readonly int _animIDGrounded;
            private readonly int _animIDFreeFall;
            private readonly int _animIDMotionSpeed;
            private readonly int _animIDPickUpTrigger; 
            private readonly int _animIDReturnToMovement; 
            private readonly int _animIDVelocityX;
            private readonly int _animIDVelocityZ;

            public PlayerAnimatorHandler(Animator animator)
            {
                _animator = animator;
                _animIDSpeed = Animator.StringToHash("Speed");
                _animIDGrounded = Animator.StringToHash("Grounded");
                _animIDFreeFall = Animator.StringToHash("FreeFall");
                _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
                _animIDPickUpTrigger = Animator.StringToHash("StartPickUpTrigger"); // Assuming this was the trigger string
                _animIDReturnToMovement = Animator.StringToHash("ReturnToMovement"); // Assuming this was the trigger string
                _animIDVelocityX = Animator.StringToHash("VelocityX");
                _animIDVelocityZ = Animator.StringToHash("VelocityZ");
            }

            public void UpdateMovementAnimation(Vector3 targetDirection, float speedRatio, Transform playerTransform)
            {
                Vector3 localVelocity = playerTransform.InverseTransformDirection(targetDirection);
                float blendMagnitude = Mathf.Clamp01(speedRatio);

                _animator.SetFloat(_animIDVelocityX, localVelocity.x * blendMagnitude, 0.05f, Time.deltaTime);
                _animator.SetFloat(_animIDVelocityZ, localVelocity.z * blendMagnitude, 0.05f, Time.deltaTime);
                _animator.SetFloat(_animIDMotionSpeed, speedRatio);
            }

            public void UpdateDashAnimation(Vector3 dashDirection, Transform playerTransform)
            {
                Vector3 localDashDir = playerTransform.InverseTransformDirection(dashDirection.normalized);
                _animator.SetFloat(_animIDVelocityX, localDashDir.x, 0.05f, Time.deltaTime);
                _animator.SetFloat(_animIDVelocityZ, localDashDir.z, 0.05f, Time.deltaTime);
                _animator.SetFloat(_animIDMotionSpeed, 2.5f);
            }

            public void StopMotion()
            {
                _animator.SetFloat(_animIDSpeed, 0f);
                _animator.SetFloat(_animIDMotionSpeed, 0f);
            }

            public void SetGrounded(bool grounded) => _animator.SetBool(_animIDGrounded, grounded);
            public void SetFreeFall(bool isFalling) => _animator.SetBool(_animIDFreeFall, isFalling);
            public void TriggerPickUp() => _animator.SetTrigger(_animIDPickUpTrigger);
            public void ReturnToMovement() => _animator.SetTrigger(_animIDReturnToMovement);
        }

        #endregion
    }
}