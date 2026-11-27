using Core.Events;
using Core.Model;
using Input;
using UnityEngine;
using Zenject;

namespace Game.Player
{
	public sealed class GameInputSystem : IEventListener<RecoverPlayerInputEvent>, 
											IEventListener<UseMainAbilityPlayerEvent>, 
											IEventListener<RotatePlayerEvent>, 
											IEventListener<MovePlayerEvent>
	{
		
		[Inject] private readonly PlayerLogic PlayerLogic = null!;

		// [Inject] private readonly GameLogic GameLogic = null!;
		// [Inject] private readonly BasicCompContainer<GameData> GameContainer = null!;
		[Inject] private readonly PlayerContainer PlayerContainer = null!;
		[Inject] private readonly GameLogic GameLogic = null!;
		
		
		public void OnEvent(RecoverPlayerInputEvent recoverPlayerInputEvent)
		{
			int newPlayerNumber = recoverPlayerInputEvent.PlayerNumber;
			if(newPlayerNumber < 0)
				return;
			
			uint topIndex = PlayerContainer.TopEmptyIndex;
			for (int i = 0; i < topIndex; i++)
			{
				ref PlayerData playerData = ref PlayerContainer.Components[i];
				if(playerData.PlayerNumber == newPlayerNumber)
				{
					Debug.Log($"Player {newPlayerNumber} already exists, skipping recovery.");
					return;
				}
			}

			PlayerLogic.SpawnPlayerEntity(newPlayerNumber);
	
		}

		public void OnEvent(UseMainAbilityPlayerEvent onEvent)
		{
			int playerNumber = onEvent.PlayerNumber;
			
			if(playerNumber < 0)
				return;

			EntId playerId = PlayerContainer.GetPlayerId(playerNumber);
			if(playerId == EntId.Invalid)
			{
				Debug.Log($"Player {playerNumber} not found, cannot use main ability.");
				return;
			}

			PlayerLogic.TryActivateAbility(playerId, PlayerAbilitySlot.Main);

		}

		public void OnEvent(RotatePlayerEvent onEvent)
		{
			int playerNumber = onEvent.PlayerNumber;
			if(playerNumber < 0)
				return;
			
			PlayerLogic.RotatePlayer(playerNumber, onEvent.NewLookDirection);
			// Debug.Log($"Input ROTATE (player{PlayerNumber} [{playerId}]) with value {RotationValue}");
		}

		public void OnEvent(MovePlayerEvent onEvent)
		{
			int playerNumber = onEvent.PlayerNumber;
			if(playerNumber < 0)
				return;

			PlayerLogic.MovePlayer(playerNumber, onEvent.Direction, onEvent.MoveValue);
		}
	}
}