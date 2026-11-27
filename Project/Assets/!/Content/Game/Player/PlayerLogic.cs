using System.Collections.Generic;
using Core.Model;
using Core.View;
using Game.Player.Abilities;
using Global;
using Input;
using UnityEngine;
using Zenject;

namespace Game.Player
{
	public sealed class PlayerLogic : BaseGameLogic
	{
		[Inject] private readonly PlayerContainer PlayerContainer = null!;
		[Inject] private readonly PlayerAbilitiesSystem PlayerAbilitiesSystem = null!;
		[Inject] private readonly CameraFollowView CameraFollowView = null!;


		[Inject] private readonly GameLogic GameLogic = null!;
		

		
		public void SpawnPlayers(IReadOnlyList<LocalPlayerInputController> players)
		{
			foreach (LocalPlayerInputController inputController in players)
			{
				SpawnPlayerEntity(inputController.PlayerIndex);
			}
		}

		
		public EntId SpawnPlayerEntity(int newPlayerNumber)
		{
			PlayerAttributes playerAttributes = GameplayConfig.PlayerAttributes;
			EntId playerId = new PlayerEntity(newPlayerNumber, playerAttributes.Size).ID;

			foreach (PlayerAbilityConfig playerAbility in playerAttributes.PlayerAbilities)
				AddPlayerAbility(playerAbility, playerId);

			OverrideDefaultStats(playerId, playerAttributes.PlayerStats);

			Vector3 spawnPosition = CameraFollowView.transform.position;
			EntityViewAtributes view = AddView(playerId, playerAttributes.PlayerPrefab.gameObject, spawnPosition, Quaternion.identity);

			PlayerView playerView = view.Get<PlayerView>();
			playerView.SetSize(playerAttributes.Size);
			
			return playerId;
		}
		
		public EntId AddPlayerAbility(PlayerAbilityConfig playerAbilityConfig, EntId entityID)
		{
			ref PlayerData playerData = ref PlayerContainer.GetComponent(entityID);
			EntId newAbilityEntityID;
			if(playerAbilityConfig.InputSlot != PlayerAbilitySlot.None)
			{
				int abilityIndex = (int)playerAbilityConfig.InputSlot;
				if (abilityIndex >= playerData.AbilityIDs.Length)
				{
					Debug.LogError($"Player {entityID} does not have enough ability slots for ability type {playerAbilityConfig.InputSlot}. " +
									$"Max ability slots: {playerData.AbilityIDs.Length}, Ability type index: {abilityIndex}");
					return EntId.Invalid;
				}
				if (playerData.AbilityIDs[abilityIndex] != EntId.Invalid)
				{
					Debug.LogError("Ability slot already occupied! We are going to overwrite the existing ability");
					EntitiesContainer.DestroyEntity(playerData.AbilityIDs[abilityIndex]);
				}
				PlayerAbilityEntity playerAbilityEntity = new PlayerAbilityEntity(entityID, playerAbilityConfig);
				newAbilityEntityID = playerAbilityEntity.ID;
				playerData.AbilityIDs[abilityIndex] = newAbilityEntityID;
			} else
			{
				PlayerAbilityEntity playerAbilityEntity = new PlayerAbilityEntity(entityID, playerAbilityConfig);
				newAbilityEntityID = playerAbilityEntity.ID;
			}
			
			OverrideDefaultStats(newAbilityEntityID, playerAbilityConfig.StatsOverride);

			return newAbilityEntityID;
		}

		
		
		public bool TryActivateAbility(EntId playerID, PlayerAbilitySlot abilitySlot)
		{
			ref PlayerData playerData = ref PlayerContainer.GetComponent(playerID);
			
			EntId abilityID = playerData.AbilityIDs[(int)abilitySlot];
			if( abilityID == EntId.Invalid)
			{
				Debug.Log($"Player {playerData.PlayerNumber} [{playerID}] does not have a main ability, cannot use it.");
				return false;
			}
			
			if(playerData.State != PlayerState.Default)
			{
				// Debug.Log($"Player {playerData.PlayerNumber} [{playerID}] is not in a normal state, cannot use main ability.");
				return false;
			}

			return PlayerAbilitiesSystem.TryActivate(abilityID);
		}

		public void RotatePlayer(int playerNumber, Vector2 newLookDirection)
		{
			EntId playerId = PlayerContainer.GetPlayerId(playerNumber);
			if(playerId == EntId.Invalid)
			{
				Debug.Log($"Player {playerNumber} not found, cannot rotate.");
				return;
			}
			
			ref PlayerData playerData = ref PlayerContainer.GetComponent(playerId);
			playerData.LookDirection = new Vector3(newLookDirection.x, 0f, newLookDirection.y).normalized;

		}

		public void MovePlayer(int playerNumber, Vector2 direction, float moveValue)
		{
			EntId playerId = PlayerContainer.GetPlayerId(playerNumber);
			if(playerId == EntId.Invalid)
			{
				Debug.Log($"Player {playerNumber} not found, cannot move.");
				return;
			}
			
			
			ref PlayerData playerData = ref PlayerContainer.GetComponent(playerId);

			playerData.MoveDirection = new Vector3(direction.x, 0f, direction.y).normalized;
			playerData.MoveValue = moveValue;

		}

	}
}