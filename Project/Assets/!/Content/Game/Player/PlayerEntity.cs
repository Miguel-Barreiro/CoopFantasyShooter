using System.Collections.Generic;
using System.Runtime.InteropServices;
using Core.Events;
using Core.Model;
using Core.Model.ModelSystems;
using Core.Systems;
using Core.View;
using FixedPointy;
using Game.Components;
using Game.Events;
using Game.Logic;
using Game.Player.Abilities;
using Game.StatusEffects;
using Global;
using UnityEngine;
using Zenject;

namespace Game.Player
{
	
	
	public enum PlayerAbilitySlot
	{
		None = -1, 
		Main = 0,
		Secondary = 1,
	}

	
	public class PlayerEntity : Entity, 
								IPlayer, 
								IStatusEffectOwner, 
								IWorldEntity, 
								IHierarchyEntity
	{
		public const int NUMBER_ABILITIES_TYPES = 2;

		public override ulong GetStartTags() => (ulong)GameTag.Player;

		public PlayerEntity(int playerNumber, Fix size)
		{
			ref PlayerData playerData = ref GetComponent<PlayerData>();
			playerData.PlayerNumber = playerNumber;

			ref WorldEntityData worldEntityData = ref GetComponent<WorldEntityData>();
			worldEntityData.Size = size;
		}
	}


	[ComponentData(ContainerType = typeof(PlayerContainer))]
	[StructLayout(LayoutKind.Auto)]
	public struct PlayerData : IComponentData
	{
		public EntId ID { get; set; }
		public int PlayerNumber;
		
		//Abilities
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = PlayerEntity.NUMBER_ABILITIES_TYPES)]
		public EntId[] AbilityIDs;

		
		
		// Movement
		public Vector3 MoveDirection;
		public Fix MoveValue;
		public Vector3 LookDirection;

		public void Init()
		{
			PlayerNumber = -1;
			MoveDirection = Vector2.zero;
			MoveValue = 0f;
			LookDirection = Vector2.right;
			
			AbilityIDs = new EntId[PlayerEntity.NUMBER_ABILITIES_TYPES];
			for (int i = 0; i < AbilityIDs.Length; i++)
				AbilityIDs[i] = EntId.Invalid;
		}
	}

	public interface IPlayer : IStatusEffectOwner,
								ISimulation, 
								IHierarchyEntity, 
								IWorldEntity, 
								Component<PlayerData>
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

		private readonly Fix SPEED_MULT = Fix.One * 300f;

		public void UpdateComponents(float deltaTime)
		{
			bool allPlayersDead = true;
			
			uint topIndex = PlayerContainer.TopEmptyIndex;
			for (int i = 0; i < topIndex; i++)
			{
				ref PlayerData playerData = ref PlayerContainer.Components[i];
				EntId playerDataID = playerData.ID;
				
				EntityViewAtributes viewAtributes = ViewEntitiesContainer.GetEntityViewAtributes(playerDataID);
				Rigidbody rigidbody = viewAtributes!.Get<Rigidbody>();
				if (rigidbody == null)
				{
					Debug.Log($"Player {playerDataID} has no rigidbody!");				
					continue;
				}
				
				Fix time = deltaTime;
				Fix speedValue = StatsSystem.GetStatValue(playerDataID, GameStatsContainer.Speed);
				Fix speed = speedValue * playerData.MoveValue * time * SPEED_MULT;

				//check if the player is moving backwards, and if so, apply a speed multiplier
				float dot = Vector3.Dot(playerData.MoveDirection, playerData.LookDirection);
				if(dot <= 0f)
					speed *= GameplayConfig.PlayerAttributes.BackwardSpeedMultiplier;


				Vector3 force = playerData.MoveDirection * (float)speed;
				// rigidbody.MovePosition(rigidbody.transform.position + force);
				rigidbody.AddForce(force, ForceMode.Acceleration );

				Fix health = StatsSystem.GetStatDepletedValue(playerDataID, GameStatsContainer.Health);
				if(health > 0f)
				{
					allPlayersDead = false;
				} else
				{
					rigidbody.linearVelocity = Vector3.zero;
					rigidbody.angularVelocity = Vector3.zero;
					EntityEventQueue.Execute<OnPlayerDeathEvent>(playerDataID);
				}
				
			}
			
			if(allPlayersDead && PlayerContainer.TopEmptyIndex > 0)
				EventQueue.Execute<OnAllPlayersDeadEvent>();
		}

		public bool Active { get; set; } = true;
		public SystemGroup Group { get; } = GameSystemsGroup.GAME_LOGIC_SYSTEM;
	}
	

}