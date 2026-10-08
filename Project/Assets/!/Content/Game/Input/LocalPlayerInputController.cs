using Core.Events;
using Game.Events;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace Game.Input
{
	/// <summary>
	/// Per-player input receiver for local multiplayer. Lives on the player prefab next to a
	/// <see cref="PlayerInput"/> (Behavior = Invoke C# Events) spawned by <see cref="PlayerInputManager"/>.
	/// Each player gets its own copy of the action asset and its own paired devices.
	/// </summary>
	[RequireComponent(typeof(PlayerInput))]
	public sealed class LocalPlayerInputController : MonoBehaviour
	{
		
		private const string PLAYER_MAP = "Player";

		private PlayerInput _playerInput;

		public int PlayerIndex => _playerInput.playerIndex;

		private void Awake()
		{
			_playerInput = GetComponent<PlayerInput>();
		}

		private void OnEnable()
		{
			_playerInput.onActionTriggered += OnActionTriggered;
			_playerInput.onDeviceLost += OnDeviceLost;
			_playerInput.onDeviceRegained += OnDeviceRegained;
		}

		private void OnDisable()
		{
			_playerInput.onActionTriggered -= OnActionTriggered;
			_playerInput.onDeviceLost -= OnDeviceLost;
			_playerInput.onDeviceRegained -= OnDeviceRegained;
		}

		private void OnActionTriggered(InputAction.CallbackContext context)
		{
			if (context.action.actionMap.name != PLAYER_MAP)
				return;

			Debug.Log($"Action triggered: {context.action.name}");
			
			switch (context.action.name)
			{
				case "Move": OnMove(context); break;
				case "Look": OnLook(context); break;
				case "Attack": OnAttack(context); break;
				case "Interact": OnInteract(context); break;
				case "Crouch": OnCrouch(context); break;
				case "Jump": OnJump(context); break;
				case "Previous": OnPrevious(context); break;
				case "Next": OnNext(context); break;
				case "Sprint": OnSprint(context); break;
			}
		}

		private void OnMove(InputAction.CallbackContext context)
		{
			Vector2 direction = context.ReadValue<Vector2>();
			
			MovePlayerEvent movePlayerEvent = EventQueue.Trigger<MovePlayerEvent>();
			movePlayerEvent.PlayerNumber = PlayerIndex;
			movePlayerEvent.Direction = direction;
		}

		private void OnLook(InputAction.CallbackContext context)
		{
			Vector2 direction = context.ReadValue<Vector2>();
			
			RotatePlayerEvent movePlayerEvent = EventQueue.Trigger<RotatePlayerEvent>();
			movePlayerEvent.PlayerNumber = PlayerIndex;
			movePlayerEvent.NewLookDirection = direction;
		}

		private void OnAttack(InputAction.CallbackContext context)
		{
		}

		private void OnInteract(InputAction.CallbackContext context)
		{
		}

		private void OnCrouch(InputAction.CallbackContext context)
		{
		}

		private void OnJump(InputAction.CallbackContext context)
		{
		}

		private void OnPrevious(InputAction.CallbackContext context)
		{
		}

		private void OnNext(InputAction.CallbackContext context)
		{
		}

		private void OnSprint(InputAction.CallbackContext context)
		{
		}

		private void OnDeviceLost(PlayerInput playerInput)
		{
		}

		private void OnDeviceRegained(PlayerInput playerInput)
		{
			RecoverPlayerInputEvent recoverPlayerInputEvent = EventQueue.Trigger<RecoverPlayerInputEvent>();
			recoverPlayerInputEvent.PlayerNumber = playerInput.playerIndex;

			Debug.Log($"RECOVER INPUT: {playerInput.playerIndex}");
		}
	}
}
