using Core.Events;
using Core.Model;
using Core.View;
using Game.Components;
using Game.Player;
using Global;
using UnityEngine;
using Zenject;

namespace Game.Events
{
	public sealed class RecoverPlayerInputEvent : Event<RecoverPlayerInputEvent>, IEarlyEvent
	{
		[Inject] private readonly GameLogic GameLogic = null!;
		// [Inject] private readonly BasicCompContainer<GameData> GameContainer = null!;
		[Inject] private readonly PlayerContainer PlayerContainer = null!;

		
		
		public int PlayerNumber = -1;
		
		public override void Execute()
		{
			if(PlayerNumber < 0)
			{
				return;
			}
			
			// ref GameData gameData = ref GameContainer.Components[0];

			uint topIndex = PlayerContainer.TopEmptyIndex;
			for (int i = 0; i < topIndex; i++)
			{
				ref PlayerData playerData = ref PlayerContainer.Components[i];
				if(playerData.PlayerNumber == PlayerNumber)
				{
					Debug.Log($"Player {PlayerNumber} already exists, skipping recovery.");
					return;
				}
			}

			GameLogic.AddPlayer(PlayerNumber);
		}
	}
	
	
	public sealed class RotatePlayerEvent : Event<RotatePlayerEvent>
	{
		[Inject] private readonly PlayerContainer PlayerContainer = null!;
		[Inject] private readonly BasicCompContainer<DynamicWorldEntityData> DynamicWorldEntityContainer = null!;

		public int PlayerNumber = -1;
		public Vector2 NewLookDirection { get; set; } = Vector2.zero;
		
		public override void Execute()
		{
			if(PlayerNumber < 0)
				return;

			EntId playerId = PlayerContainer.GetPlayerId(PlayerNumber);
			if(playerId == EntId.Invalid)
			{
				Debug.Log($"Player {PlayerNumber} not found, cannot rotate.");
				return;
			}
			
			ref PlayerData playerData = ref PlayerContainer.GetComponent(playerId);
			playerData.LookDirection = NewLookDirection;
			
			// Debug.Log($"Input ROTATE (player{PlayerNumber} [{playerId}]) with value {RotationValue}");
		}

	}
	
	public sealed class MovePlayerEvent : Event<MovePlayerEvent>
	{
		[Inject] private readonly PlayerContainer PlayerContainer = null!;
		[Inject] private readonly BasicCompContainer<DynamicWorldEntityData> DynamicWorldEntityContainer = null!;

		public int PlayerNumber = -1;
		public Vector2 Direction = Vector2.zero;
		
		public override void Execute()
		{
			if(PlayerNumber < 0)
				return;

			EntId playerId = PlayerContainer.GetPlayerId(PlayerNumber);
			if(playerId == EntId.Invalid)
			{
				Debug.Log($"Player {PlayerNumber} not found, cannot move.");
				return;
			}
			
			ref PlayerData playerData = ref PlayerContainer.GetComponent(playerId);

			playerData.MoveDirection = Direction.normalized;
			playerData.MoveValue = 1;
			
			// Debug.Log($"Input MOVE (player{PlayerNumber} [{playerId}]) with direction {Direction}");
		}
	}
	
	public sealed class ResetPlayerPositionEvent : Event<ResetPlayerPositionEvent>
	{
		[Inject] private readonly PlayerContainer PlayerContainer = null!;
		[Inject] private readonly ViewEntitiesContainer ViewEntitiesContainer = null!;
		[Inject] private readonly GameConfig GameConfig = null!;

		public int PlayerNumber = -1;
		
		public override void Execute()
		{
			if(PlayerNumber < 0)
				return;

			EntId playerId = PlayerContainer.GetPlayerId(PlayerNumber);
			if(playerId == EntId.Invalid)
			{
				Debug.Log($"Player {PlayerNumber} not found, cannot reset position.");
				return;
			}
			
			EntityViewAtributes viewAtributes = ViewEntitiesContainer.GetEntityViewAtributes(playerId);
			Rigidbody rigidbody = viewAtributes!.Get<Rigidbody>();
			rigidbody.position = Vector3.zero;
			Debug.Log($"Resetting Player {PlayerNumber}");
		}
	}

	
}