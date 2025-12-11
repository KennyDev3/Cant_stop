using UnityEngine;
using System;
using System.Collections;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/* Note: animations are called via the controller for both the character and capsule using animator null checks
 */

namespace StarterAssets
{
    public enum PlayerActivityState
    {
        Free,      // Default state: can move, sprint
        PickingUp, // Performing a non-interruptible/interruptible action
        Dashing,   // Performing a dash
    }

    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM
    [RequireComponent(typeof(PlayerInput))]
#endif

    public class ThirdPersonController : MonoBehaviour
    {
        [SerializeField] private StatController _stats;

        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 2.0f;

        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 5.335f;

        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Space(10)]
        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Dash")]
        [Tooltip("The speed of the player dash")]
        public float DashSpeed = 20.0f;

        [Tooltip("How long the player dashes for in seconds")]
        public float DashDuration = 0.2f;

        [Tooltip("The cooldown time for the dash in seconds")]
        public float DashCooldown = 1.0f;

        [Tooltip("How long the player is invincible during the dash in seconds")]
        public float InvincibilityDuration = 0.2f;


        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;

        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;

        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 70.0f;

        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -30.0f;

        [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
        public float CameraAngleOverride = 0.0f;

        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition = false;



        // ------------------ DECOUPLED EVENT ------------------
        public event Action OnPickupAnimationComplete;
        public event Action<Vector3> OnDashStart;
        // -----------------------------------------------------

        // cinemachine
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // player
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;
        private PlayerStamina _playerStamina;
        private PlayerGarbageHandler _playerGarbageHandler;

        // timeout deltatime
        private float _fallTimeoutDelta;

        // dash
        private float _dashCooldownTimer;

        // To allow player to phase during Dash
        private int _playerLayer;
        private int _phasingPlayerLayer;
        [SerializeField] private TrailRenderer _trailRenderer;

        private PlayerActivityState _currentState = PlayerActivityState.Free;
        public float _pickUpAnimationLength = 0.2f; // Duration set by the caller script
        private bool _isPickUpCancelable = false; // Is movement allowed to interrupt the pickup?

        // animation IDs
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

        private const float _threshold = 0.01f;

        private bool _hasAnimator;

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput.currentControlScheme == "KeyboardMouse";
#else
				return false;
#endif
            }
        }

        private void Awake()
        {
            // get a reference to our main camera
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
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

            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;

            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
            _playerStamina = GetComponent<PlayerStamina>();
#if ENABLE_INPUT_SYSTEM
            _playerInput = GetComponent<PlayerInput>();
            _playerGarbageHandler = GetComponent<PlayerGarbageHandler>();
            _phasingPlayerLayer = LayerMask.NameToLayer("PhasingPlayer");

#else
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

            AssignAnimationIDs();

            // reset our timeouts on start
            _fallTimeoutDelta = FallTimeout;
        }

        private void Update()
        {
            _hasAnimator = TryGetComponent(out _animator);

            // Update dash cooldown timer
            if (_dashCooldownTimer > 0.0f)
            {
                _dashCooldownTimer -= Time.deltaTime;
            }

            // Grounded Check always runs to ensure accurate state
            GroundedCheck();

            // Handle player input and states
            HandleState();
        }

        private void LateUpdate()
        {
            CameraRotation();
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
                    Move();
                    HandleDashInput();
                    HandleBoostInput();
                    break;
                case PlayerActivityState.PickingUp:
                    HandlePickingUpState();
                    break;
                case PlayerActivityState.Dashing:
                    ApplyGravity(); // Continue to apply gravity during dash
                    break;
            }
        }

        private void HandlePickingUpState()
        {
            // Still apply gravity to prevent floating!
            ApplyGravity();

            // Check for movement input if the action is cancelable
            if (_isPickUpCancelable && _input.move != Vector2.zero)
            {
                // If movement input is detected AND it's cancelable, force-exit
                FinishPickUp();
            }

            // Apply movement vector of zero to stop horizontal motion
            Vector3 verticalMovement = new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime;
            _controller.Move(verticalMovement);

            // Keep Speed animator parameter at 0 for idle pose
            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, 0f);
                _animator.SetFloat(_animIDMotionSpeed, 0f);
            }
        }

        private void GroundedCheck()
        {
            // set sphere position, with offset
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
            transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
              QueryTriggerInteraction.Ignore);

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }

        private void CameraRotation()
        {
            // if there is an input and camera position is not fixed
            if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                //Don't multiply mouse input by Time.deltaTime;
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
                _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
            }

            // clamp our rotations so our values are limited 360 degrees
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Cinemachine will follow this target
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
            _cinemachineTargetYaw, 0.0f);
        }

        private void Move()
        {
            float currentMoveSpeed = _stats ? _stats.GetStat(StatType.MoveSpeed) : MoveSpeed;

            float sprintMultiplier = SprintSpeed / MoveSpeed;
            float currentSprintSpeed = currentMoveSpeed * sprintMultiplier;

            float targetSpeed;
            bool isOverencumbered = _playerGarbageHandler != null && _playerGarbageHandler.IsOverencumbered;

            bool isBoosting = _playerStamina != null && _playerStamina.IsBoostActive();
            targetSpeed = isBoosting ? currentSprintSpeed : currentMoveSpeed;

            if (isOverencumbered)
                targetSpeed = currentMoveSpeed / 2f;

            if (_input.move == Vector2.zero)
                targetSpeed = 0.0f;

            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                    Time.deltaTime * SpeedChangeRate);

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
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                                  _mainCamera.transform.eulerAngles.y;

                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation,
                    ref _rotationVelocity, RotationSmoothTime);

                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }

            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                             new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }



        // ------------------  DASH LOGIC ------------------

        private void HandleDashInput()
        {
            if (_input.jump)
            {
                
                _input.jump = false;

                if (_dashCooldownTimer <= 0.0f)
                {
                    StartCoroutine(Dash());
                }

                
            }
        }

        private void HandleBoostInput()
        {
            if (_input.sprint) 
            {
                if (_playerStamina.TryActivateBoost())
                {
                    // Optional VFX hook
                    // boostTrail?.Play();
                }

                _input.sprint = false;
            }
        }


        private IEnumerator Dash()
        {
            _currentState = PlayerActivityState.Dashing;
            _dashCooldownTimer = DashCooldown;

            if (_trailRenderer != null) // Dash VFX Start
            {
                _trailRenderer.emitting = true;
            }

            // Start invincibility
            StartCoroutine(BecomeInvincible(InvincibilityDuration));

            float startTime = Time.time;

            // Determine dash direction
            Vector3 dashDirection = transform.forward;
            if (_input.move != Vector2.zero)
            {
                Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;
                float targetAngle = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;
                dashDirection = Quaternion.Euler(0.0f, targetAngle, 0.0f) * Vector3.forward;
            }

            OnDashStart?.Invoke(dashDirection); // Announace event

            float currentDuration = _stats ? _stats.GetStat(StatType.DashDuration) : DashDuration;

            while (Time.time < startTime + currentDuration)
            {
                
                Vector3 horizontalMovement = dashDirection.normalized * (DashSpeed * Time.deltaTime);
                Vector3 verticalMovement = new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime;
                _controller.Move(horizontalMovement + verticalMovement);
                

                yield return null;
            }

            if (_trailRenderer != null) // Dash VFX Stop
            {
                _trailRenderer.emitting = false;
            }

            _currentState = PlayerActivityState.Free;
        }

        private IEnumerator BecomeInvincible(float duration)
        {
            gameObject.layer = _phasingPlayerLayer; // Stop Collisions

            if (_controller != null)
            {
                _controller.detectCollisions = false;
            }

            yield return new WaitForSeconds(duration);

            gameObject.layer = _playerLayer; // Enable Collisions


            if (_controller != null)
            {
                _controller.detectCollisions = true;
            }
        }

        // Public method for PlayerHealth to check invulnerability
        public bool IsInvulnerable()
        {
            return _currentState == PlayerActivityState.Dashing;
        }

        // --------------------------------------------------------

        private void ApplyGravity()
        {
            if (Grounded)
            {
                // reset the fall timeout timer
                _fallTimeoutDelta = FallTimeout;

                // update animator if using character
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDFreeFall, false);
                }

                // stop our velocity dropping infinitely when grounded
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }
            }
            else
            {
                // fall timeout
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDFreeFall, true);
                    }
                }
                _input.jump = false;
            }

            // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
            Gizmos.DrawSphere(
            new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
            GroundedRadius);
        }

        public void UpgradePlayerSpeed(float increaseAmount)
        {
            MoveSpeed += increaseAmount;
            SprintSpeed += increaseAmount;
            Debug.Log(MoveSpeed);
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

        // ------------------ DECOUPLED PICKUP METHODS ------------------

        public bool StartPickUp(bool isCancelable)
        {
            if (!Grounded || _currentState != PlayerActivityState.Free)
            {
                Debug.LogWarning("Pickup blocked: Player is not in a 'Free' state.");
                return false;
            }

            _isPickUpCancelable = isCancelable;
            _currentState = PlayerActivityState.PickingUp;

            if (_hasAnimator)
            {
                Debug.Log("Starting PickUp animation trigger.");
                _animator.SetTrigger("StartPickUpTrigger");
            }

            return true;
        }


        public void OnPickupAnimationFinished()
        {
            FinishPickUp();
        }



        private void FinishPickUp()
        {
            if (_currentState == PlayerActivityState.PickingUp)
            {
                _currentState = PlayerActivityState.Free;

                // Fire event: The PlayerGarbageHandler (the coordinator) listens to this event
                // and will handle the item collection and destruction.
                OnPickupAnimationComplete?.Invoke();

                // ANIMATION: Return control to the Base Layer
                if (_hasAnimator)
                {
                    _animator.SetTrigger("ReturnToMovement");
                }
            }
        }
    }
}