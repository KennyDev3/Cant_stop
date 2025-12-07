using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
	public class StarterAssetsInputs : MonoBehaviour
	{
		[Header("Character Input Values")]
		public Vector2 move;
		public Vector2 look;
		public bool jump;
		public bool sprint;
		public bool interact;
        public bool pickUpGarbage;
        public bool parry;
        public bool chuck;


        [Header("Debug Input")]
        public bool debugAdd;



        [Header("Movement Settings")]
		public bool analogMovement;

		[Header("Mouse Cursor Settings")]
		public bool cursorLocked = true;
		public bool cursorInputForLook = true;

#if ENABLE_INPUT_SYSTEM
		public void OnMove(InputValue value)
		{
			MoveInput(value.Get<Vector2>());
		}

		public void OnLook(InputValue value)
		{
			if(cursorInputForLook)
			{
				LookInput(value.Get<Vector2>());
			}
		}

		public void OnJump(InputValue value)
		{
			JumpInput(value.isPressed);
		}

		public void OnSprint(InputValue value)
		{
			SprintInput(value.isPressed);
		}

		public void OnInteract(InputValue value)
		{
			InteractInput(value.isPressed);
			Debug.Log("E has been pressed");
		}

        public void OnParry(InputValue value)
        {
            ParryInput(value.isPressed);
        }

        public void OnChuck(InputValue value)
        {
            ChuckInput(value.isPressed);
        }

        public void OnPickUpGarbage(InputValue value)
        {
            pickUpGarbage = value.isPressed;
        }

        public void OnPause(InputValue value)
        {
            if (value.isPressed)
            {
                // Directly toggle the Game Manager
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.TogglePause();
                }
            }
        }

        public void OnDebugAdd(InputValue value)
        {
            DebugAddInput(value.isPressed);
        }
#endif


        public void MoveInput(Vector2 newMoveDirection)
		{
			move = newMoveDirection;
		} 

		public void LookInput(Vector2 newLookDirection)
		{
			look = newLookDirection;
		}

		public void JumpInput(bool newJumpState)
		{
			jump = newJumpState;
		}

		public void SprintInput(bool newSprintState)
		{
			sprint = newSprintState;
		}

		public void InteractInput(bool newInteractState)
		{
			interact = newInteractState;
		}

        public void ParryInput(bool newParryState)
        {
            parry = newParryState;
        }

        public void ChuckInput(bool newChuckState)
        {
            chuck = newChuckState;
        }

        public void PikcUpGarbageInput(bool newpickUpGarbageState)
		{
            pickUpGarbage = newpickUpGarbageState;

        }

        public void DebugAddInput(bool newDebugAddState)
        {
            debugAdd = newDebugAddState;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing)
            {
                SetCursorState(cursorLocked);
            }
        }

        private void SetCursorState(bool newState)
		{
			Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
		}
	}
	
}