using System.Collections.Generic;
using Com.LuisPedroFonseca.ProCamera2D;
using Core.Model;
using Core.Model.Stats;
using Core.Systems;
using Core.View;
using FixedPointy;
using Game.BehaviourTree;
using Game.Enemies;
using Game.Player;
using Game.Spells;
using Game.Spells.Bullet;
using Global;
using UnityEngine;
using Zenject;

namespace Game
{
	public sealed class GameLogic
	{
		[Inject] private readonly GameplayConfig GameplayConfig = null!;
		[Inject] private readonly ViewEntitiesContainer ViewEntitiesContainer = null!;
		[Inject] private readonly ProCamera2D ProCamera2D = null!;
		[Inject] private readonly PlayerContainer PlayerContainer = null!;
		[Inject] private readonly StatsSystem StatsSystem = null!;
		[Inject] private readonly IEntityHierarchySystem IEntityHierarchySystem = null!;
		[Inject] private readonly GameStatsContainer GameStatsContainer = null!;

		[Inject] private readonly BasicCompContainer<BehaviourAgentData> BehaviourAgentContainer = null!;
		[Inject] private readonly BehaviourTreeContainer BehaviourTreeContainer = null!;


		public EntId SpawnEnemy(Vector2 position, EnemyConfig enemyConfig)
		{
			//TODO: we need to spawn enemies as premade clumps with potential positions premade.
			//Also we need to pass in some special world positions in the area if possible
			//to make interesting clumps of enemies. For now we will just spawn them in a random position around the player.
			
			Vector3 spawnPosition = new Vector3(position.x, 0f, position.y);
			EntId enemyId = new EnemyEntity().ID;
			EntityViewAtributes view = AddView(enemyId, enemyConfig.EnemyPrefab.gameObject, spawnPosition, Quaternion.identity);
			EnemyView enemyView = view.Get<EnemyView>();

			enemyView.NavMeshAgent.Warp(spawnPosition);
			// enemyView.SetSize(GameplayConfig.EnemyAttributes.Size);
			
			AddBehaviourTree();
			ref BehaviourAgentData agentData = ref BehaviourAgentContainer.GetComponent(enemyId);
			agentData.AttackFrameNorm = enemyConfig.AttackFrameNormalizedTime;
			agentData.Enable = true;
			
			OverrideDefaultStats(enemyId, enemyConfig.StatsOverride);

			foreach (AbilityConfig abilityConfig in enemyConfig.Abilities)
				AddAbility(abilityConfig, enemyId);
			
			// enemyView.TriggerSpawning();
			
			return enemyId;

			
			
			void AddGoblinPawnBehaviourTree()
			{
				ref BehaviourNodeData findTargetNode = ref BehaviourTreeContainer.AddNode(enemyId, NodeType.FindTarget);
				findTargetNode.Cooldown = 0f;
				ref BehaviourNodeData moveToTargetNode = ref BehaviourTreeContainer.AddNode(enemyId, NodeType.MoveToTarget);
				ref BehaviourNodeData attackNode = ref BehaviourTreeContainer.AddNode(enemyId, NodeType.AttackTarget);
				
			}
			void AddGoblinShamanBehaviourTree()
			{
				ref BehaviourNodeData findTargetNode = ref BehaviourTreeContainer.AddNode(enemyId, NodeType.FindTarget);
				findTargetNode.Cooldown = 0f;
				// ref BehaviourNodeData moveToTargetNode = ref BehaviourTreeContainer.AddNode(enemyId, NodeType.MoveToTarget);
				ref BehaviourNodeData moveAwayFromTargetNode = ref BehaviourTreeContainer.AddNode(enemyId, NodeType.MoveAwayFromTarget);
				ref BehaviourNodeData findSupportTargetNode = ref BehaviourTreeContainer.AddNode(enemyId, NodeType.FindSupportTarget);
				findSupportTargetNode.Cooldown = 0f;
				ref BehaviourNodeData castSupportSpell = ref BehaviourTreeContainer.AddNode(enemyId, NodeType.CastSupportTarget);
				// ref BehaviourNodeData attackNode = ref BehaviourTreeContainer.AddNode(enemyId, NodeType.AttackTarget);
			}
			
			void AddBehaviourTree()
			{
				switch (enemyConfig.BehaviourType)
				{
					case EnemyBehaviourType.GoblinPawn:
						AddGoblinPawnBehaviourTree();
						break;
					case EnemyBehaviourType.GoblinSupport:
						AddGoblinShamanBehaviourTree();
						break;
				}
			}
		}
		
		
		public EntId SpawnSpell(EntId owner, SpellConfig spellConfig, Vector3 direction, Vector3 position)
		{
			EntId newEntityID;
			switch (spellConfig.SpellType)
			{
				case SpellType.Projectile:
					newEntityID = SpawnBullet(owner, spellConfig, direction, position);
					break;
					default:
						return EntId.Invalid;
				
			}
			
			// SpellEntity spellEntity = new SpellEntity(spellConfig);
			//
			AddView(newEntityID, spellConfig.SpellPrefab.gameObject);
			
			return newEntityID;


			//
			// EntId bulletId = new BulletEntity().ID;
			//
			// ref PlayerData playerData = ref PlayerContainer.GetComponent(playerId);
			// EntityViewAtributes playerViewAtributes = ViewEntitiesContainer.GetEntityViewAtributes(playerId);
			//
			// playerData.LookDirection = playerData.LookDirection.normalized;
			// Vector3 playerPosition = playerViewAtributes!.GameObject.transform.position;
			//
			// Vector3 position = playerPosition + 
			// 					Vector3.up * 0.9f +
			// 					playerData.LookDirection.normalized * 
			// 					(GameplayConfig.PlayerAttributes.Size + GameplayConfig.BasicSpellsAttributes.BulletSize);
			// EntityViewAtributes view = AddView(bulletId, GameplayConfig.BasicSpellsAttributes.BulletPrefab.gameObject, position, Quaternion.LookRotation(playerData.LookDirection.normalized));
			//
			// Rigidbody rigidbody = view.Get<Rigidbody>();
			// // rigidbody.MovePosition(position);
			// // rigidbody.MoveRotation(Quaternion.LookRotation(playerData.LookDirection.normalized));
			// rigidbody.linearVelocity = playerData.LookDirection.normalized * GameplayConfig.BasicSpellsAttributes.BulletSpeed;
			// // rigidbody.AddForce(playerData.LookDirection.normalized * GameplayConfig.BasicSpellsAttributes.BulletSpeed, ForceMode.VelocityChange);
			//
			// Fix damage = StatsSystem.GetStatValue(playerData.AttackAbiityID, GameStatsContainer.Damage);
			// StatsSystem.SetBaseValue(bulletId, GameStatsContainer.Damage, damage);
			//
			// return bulletId;
		}

		
		
		public EntId SpawnBullet(EntId ownerId, SpellConfig spellConfig, Vector3 direction, Vector3 position)
		{
			EntId bulletId = new BulletEntity(spellConfig).ID;

			// // ref PlayerData playerData = ref PlayerContainer.GetComponent(playerId);
			// EntityViewAtributes playerViewAtributes = ViewEntitiesContainer.GetEntityViewAtributes(playerId);

			// Vector3 direction = playerData.LookDirection.normalized;
			// playerData.LookDirection = direction.normalized;
			// Vector3 playerPosition = playerViewAtributes!.GameObject.transform.position;
			
			// Vector3 position = playerPosition + 
			// 					Vector3.up * 0.9f +
			// 					direction.normalized * 
			// 					(GameplayConfig.PlayerAttributes.Size + GameplayConfig.BasicSpellsAttributes.BulletSize);
			EntityViewAtributes view = AddView(bulletId, GameplayConfig.BasicSpellsAttributes.BulletPrefab.gameObject, position, Quaternion.LookRotation(direction.normalized));
			
			Fix damage = StatsSystem.GetStatValue(ownerId, GameStatsContainer.Damage);
			StatsSystem.SetBaseValue(bulletId, GameStatsContainer.Damage, damage);

			OverrideDefaultStats(bulletId, spellConfig.StatsOverride);


			Rigidbody rigidbody = view.Get<Rigidbody>();
			float speed = (float) StatsSystem.GetStatValue(bulletId, GameStatsContainer.Speed);
			rigidbody.linearVelocity = direction * speed;
			
			return bulletId;
		}


		public EntId AddPlayer(int newPlayerNumber)
		{
			EntId playerId = new PlayerEntity(newPlayerNumber).ID;
			
			EntityViewAtributes view = AddView(playerId, GameplayConfig.PlayerAttributes.PlayerPrefab.gameObject);
			PlayerView playerView = view.Get<PlayerView>();
			playerView.SetSize(GameplayConfig.PlayerAttributes.Size);
			ProCamera2D.AddCameraTarget(view.GameObject.transform);
			
			StatsSystem.SetBaseValue(playerId, GameStatsContainer.Health, GameplayConfig.PlayerAttributes.Health);
			
			return playerId;
		}

		protected EntityViewAtributes AddView(EntId entityId, GameObject prefab, Vector3 position, Quaternion rotation)
		{
			return ViewEntitiesContainer.Spawn(prefab, entityId, position, rotation);
		}
		
		protected EntityViewAtributes AddView(EntId entityId, GameObject prefab)
		{
			return ViewEntitiesContainer.Spawn(prefab, entityId);
		}

		public EntId AddAbility(AbilityConfig abilityConfig, EntId entityID)
		{
			AbilityEntity abilityEntity = new AbilityEntity(entityID, abilityConfig);
			EntId abilityEntityID = abilityEntity.ID;

			OverrideDefaultStats(abilityEntityID, abilityConfig.StatsOverride);
			
			return abilityEntityID;
		}



		private void OverrideDefaultStats(EntId newEntityID, List<StatOverride> statOverrides)
		{
			foreach (StatOverride statOverride in statOverrides)
			{
				switch (statOverride.OverrideType)
				{
					case OverrideType.Override:
						StatsSystem.SetBaseValue(newEntityID, statOverride.StatConfig,statOverride.Value);
						break;
					case OverrideType.Additive:
						StatsSystem.AddModifier(newEntityID, newEntityID, 
												statOverride.StatConfig,statOverride.Value, 
												StatModifierType.Additive);
						
						break;
					case OverrideType.Multiplicative:
						StatsSystem.AddModifier(newEntityID, newEntityID, 
												statOverride.StatConfig,statOverride.Value, 
												StatModifierType.Multiplicative);
						break;
					
				}

				switch (statOverride.depletedValueOverride)
				{
					case DepletedValueOverrideType.MaxValue:
						StatsSystem.ResetDepletedValueToMax(newEntityID, statOverride.StatConfig);
						break;
					case DepletedValueOverrideType.MinValue:
						StatsSystem.ResetDepletedValueToMin(newEntityID, statOverride.StatConfig);
						break;
				}
			}
		}

		public EntId KillEnemy(EntId enemyID)
		{
			DeadBodyEntity deadBodyEntity = new DeadBodyEntity();
			ViewEntitiesContainer.SwitchOwnership(enemyID, deadBodyEntity.ID);

			// Entity destruction is deferred, so the behaviour tree would still run this frame without a view.
			ref BehaviourAgentData agentData = ref BehaviourAgentContainer.GetComponent(enemyID);
			agentData.Enable = false;
			EntitiesContainer.DestroyEntity(enemyID);
			
			return deadBodyEntity.ID;
		}
	}
}