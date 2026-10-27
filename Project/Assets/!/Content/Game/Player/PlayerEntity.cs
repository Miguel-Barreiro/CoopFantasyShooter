using System.Collections.Generic;
using System.Runtime.InteropServices;
using Core.Events;
using Core.Model;
using Core.Model.ModelSystems;
using Core.Systems;
using Core.View;
using FixedPointy;
using Game.Events;
using Global;
using UnityEngine;
using Zenject;

namespace Game.Player
{
	
	
	public class PlayerEntity : Entity, IPlayer, IHierarchyEntity
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
		public int PlayerNumber;
		
		//spells
		public EntId AttackAbiityID;
		
		
		// Movement
		public Vector3 MoveDirection;
		public float MoveValue;
		public Vector3 LookDirection;

		public void Init()
		{
			PlayerNumber = -1;
			MoveDirection = Vector2.zero;
			MoveValue = 0f;
			LookDirection = Vector2.right;
			
			AttackAbiityID = EntId.Invalid;
		}
	}

	public interface IPlayer : Component<PlayerData>, IHierarchyEntity
	{ }
	
	
	public sealed class PlayerContainer : BasicCompContainer<PlayerData>, OnCreateComponent<PlayerData>
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

		public void OnCreateComponent(EntId newPlayerId)
		{
			Debug.Log($"PlayerContainer:  new Player({newPlayerId})");
		}

		public bool Active { get; set; } = true;
		public SystemGroup Group { get; } = GameSystemsGroup.GAME_LOGIC_SYSTEM;
	}


	public sealed class PlayerSystem : UpdateComponents<PlayerData>
	{
		[Inject] private readonly PlayerContainer PlayerContainer = null!;
		[Inject] private readonly ViewEntitiesContainer ViewEntitiesContainer = null!;
		[Inject] private readonly GameplayConfig GameplayConfig = null!;
		[Inject] private readonly GameStatsContainer GameStatsContainer = null!;
		[Inject] private readonly StatsSystem StatsSystem = null!;
		[Inject] private readonly EventQueue EventQueue = null!;

		
		public void UpdateComponents(float deltaTime)
		{
			bool allPlayersDead = true;
			
			uint topIndex = PlayerContainer.TopEmptyIndex;
			for (int i = 0; i < topIndex; i++)
			{
				ref PlayerData playerData = ref PlayerContainer.Components[i];
				EntityViewAtributes viewAtributes = ViewEntitiesContainer.GetEntityViewAtributes(playerData.ID);
				Rigidbody rigidbody = viewAtributes!.Get<Rigidbody>();
				if (rigidbody == null)
				{
					Debug.Log($"Player {playerData.ID} has no rigidbody!");				
					continue;
				}

				
				float speed = GameplayConfig.PlayerAttributes.PlayerSpeed * playerData.MoveValue * deltaTime;
				Vector3 force = playerData.MoveDirection * speed;
				// rigidbody.MovePosition(rigidbody.transform.position + force);
				rigidbody.AddForce(force, ForceMode.Acceleration );

				Fix health = StatsSystem.GetStatDepletedValue(playerData.ID, GameStatsContainer.Health);
				if(health > 0f)
				{
					allPlayersDead = false;
				} else
				{
					rigidbody.linearVelocity = Vector3.zero;
					rigidbody.angularVelocity = Vector3.zero;
					EntityEventQueue.Execute<OnPlayerDeathEvent>(playerData.ID);
				}

				// rigidbody.linearVelocity = force;
				// rigidbody.Move(rigidbody.transform.position + force, rigidbody.transform.rotation);
				// rigidbody.transform.SetPositionAndRotation(rigidbody.transform.position + force, rigidbody.transform.rotation);
				// Debug.Log($"Moving Player {playerData.PlayerNumber} with direction {playerData.MoveDirection} and move value {speed}");
				
			}
			if(allPlayersDead && PlayerContainer.TopEmptyIndex > 0)
				EventQueue.Execute<OnAllPlayersDeadEvent>();
		}

		public bool Active { get; set; } = true;
		public SystemGroup Group { get; } = GameSystemsGroup.GAME_LOGIC_SYSTEM;
	}
	

}