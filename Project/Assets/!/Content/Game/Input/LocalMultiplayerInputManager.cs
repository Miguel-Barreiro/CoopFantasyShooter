using System.Collections.Generic;
using Core.Events;
using Core.Initialization;
using Game.Events;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace Game.Input
{
	/// <summary>
	/// Scene-level hub for local multiplayer. Sits next to a <see cref="PlayerInputManager"/>
	/// (Notification Behavior = Invoke C# Events) and tracks players as they join/leave.
	/// All players share a single camera, so split screen stays disabled.
	/// </summary>
	[RequireComponent(typeof(PlayerInputManager))]
	public sealed class LocalMultiplayerInputManager : MonoBehaviour
	{
		
		[Inject] private readonly EventQueue EventQueue = null!;
		[Inject] private readonly ObjectBuilder ObjectBuilder = null!;

		

		private readonly List<LocalPlayerInputController> _players = new List<LocalPlayerInputController>();
		private PlayerInputManager _playerInputManager;

		public IReadOnlyList<LocalPlayerInputController> Players => _players;

		private void Awake()
		{
			_playerInputManager = GetComponent<PlayerInputManager>();
		}

		private void OnEnable()
		{
			_playerInputManager.onPlayerJoined += OnPlayerJoined;
			_playerInputManager.onPlayerLeft += OnPlayerLeft;
		}

		private void OnDisable()
		{
			_playerInputManager.onPlayerJoined -= OnPlayerJoined;
			_playerInputManager.onPlayerLeft -= OnPlayerLeft;
		}

		private void OnPlayerJoined(PlayerInput playerInput)
		{
			if (playerInput.TryGetComponent(out LocalPlayerInputController controller))
			{
				_players.Add(controller);
				ObjectBuilder.Inject(controller);
			}
			
			RecoverPlayerInputEvent recoverPlayerInputEvent = EventQueue.Execute<RecoverPlayerInputEvent>();
			recoverPlayerInputEvent.PlayerNumber = playerInput.playerIndex;

			Debug.Log($"PLAYER JOINED: {playerInput.playerIndex}");
		}

		private void OnPlayerLeft(PlayerInput playerInput)
		{
			if (playerInput.TryGetComponent(out LocalPlayerInputController controller))
				_players.Remove(controller);
			
			
			Debug.Log($"PLAYER LEFT: {playerInput.playerIndex}");
		}
	}
}
