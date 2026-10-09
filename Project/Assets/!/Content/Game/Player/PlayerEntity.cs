using System.Collections.Generic;
using System.Runtime.InteropServices;
using Core.Model;
using Core.Model.ModelSystems;
using Core.Systems;
using Core.View;
using Game.Components;
using Global;
using UnityEngine;
using Zenject;

namespace Game.Player
{
	
	
	public class PlayerEntity : Entity, IPlayer, IDynamicWorldEntity
	{
		[Inject] private readonly GameConfig GameConfig = null!;

		
		public PlayerEntity(int playerNumber)
		{
			ref PlayerData playerData = ref GetComponent<PlayerData>();
			playerData.PlayerNumber = playerNumber;

		}
	}


	[ComponentData(ContainerType = typeof(PlayerContainer))]
	[StructLayout(LayoutKind.Auto)]
	public struct PlayerData : IComponentData
	{
		public EntId ID { get; set; }
		public int PlayerNumber { get; set; }
		public Vector3 MoveDirection { get; set; }
		public float MoveValue { get; set; }
		public Vector3 LookDirection { get; set; }

		public void Init()
		{
			PlayerNumber = -1;
			MoveDirection = Vector2.zero;
			MoveValue = 0f;
			LookDirection = Vector2.zero;
		}
	}

	public interface IPlayer : Component<PlayerData>, 
								IDynamicWorldEntity 
	{ }
	
	
	public sealed class PlayerContainer : BasicCompContainer<PlayerData>
	{
		protected Dictionary<int, EntId> PlayerIdByPlayerNumber = new Dictionary<int, EntId>();
		
		public PlayerContainer(uint capacity) : base(capacity)
		{
		}
		
		public EntId GetPlayerId(int playerNumber)
		{
			if (PlayerIdByPlayerNumber.TryGetValue(playerNumber, out EntId playerId))
				return playerId;

			uint topIndex = TopEmptyIndex;
			for (int i = 0; i < topIndex; i++)
			{
				ref PlayerData playerData = ref Components[i];
				if (playerData.PlayerNumber == playerNumber)
				{
					PlayerIdByPlayerNumber[playerNumber] = playerData.ID;
					return playerData.ID;
				}
			}
			return EntId.Invalid;
		}
		
	}


	public sealed class PlayerSystem : UpdateComponents<PlayerData>
	{
		[Inject] private readonly PlayerContainer PlayerContainer = null!;
		[Inject] private readonly ViewEntitiesContainer ViewEntitiesContainer = null!;
		[Inject] private readonly GameConfig GameConfig = null!;



		public void UpdateComponents(float deltaTime)
		{
			uint topIndex = PlayerContainer.TopEmptyIndex;
			for (int i = 0; i < topIndex; i++)
			{
				ref PlayerData playerData = ref PlayerContainer.Components[i];
				EntityViewAtributes viewAtributes = ViewEntitiesContainer.GetEntityViewAtributes(playerData.ID);
				Rigidbody rigidbody = viewAtributes!.Get<Rigidbody>();
				
				float speed = GameConfig.PlayerSpeed * playerData.MoveValue * deltaTime;
				Vector3 force = playerData.MoveDirection * speed;
				// rigidbody.MovePosition(rigidbody.transform.position + force);
				rigidbody.AddForce(force, ForceMode.Acceleration );

				
				// rigidbody.linearVelocity = force;
				// rigidbody.Move(rigidbody.transform.position + force, rigidbody.transform.rotation);
				// rigidbody.transform.SetPositionAndRotation(rigidbody.transform.position + force, rigidbody.transform.rotation);
				// Debug.Log($"Moving Player {playerData.PlayerNumber} with direction {playerData.MoveDirection} and move value {speed}");
				
			}
		}

		public bool Active { get; set; } = true;
		public SystemGroup Group { get; } = GameSystemsGroup.GAME_LOGIC_SYSTEM;
	}
	

}