using System.Collections.Generic;
using Core.Events;
using Core.Model;
using Core.Model.Stats;
using Core.Systems;
using Core.View;
using FixedPointy;
using Game.BehaviourTree;
using Game.Components;
using Game.Enemies;
using Game.Enemies.EnemyAbilities;
using Game.Logic;
using Game.Player;
using Game.Spells;
using Game.Spells.Bullet;
using Game.Spells.Mortar;
using Game.StatusEffects;
using Global;
using UnityEngine;
using Zenject;

namespace Game
{

	public abstract class BaseGameLogic
	{
		[Inject] protected readonly GameplayConfig GameplayConfig = null!;
		[Inject] protected readonly ViewEntitiesContainer ViewEntitiesContainer = null!;
		[Inject] protected readonly StatsSystem StatsSystem = null!;
		[Inject] protected readonly GameStatsContainer GameStatsContainer = null!;

		protected EntityViewAtributes AddView(EntId entityId, GameObject prefab, Vector3 position, Quaternion rotation)
		{
			return ViewEntitiesContainer.Spawn(prefab, entityId, position, rotation);
		}
		
		protected EntityViewAtributes AddView(EntId entityId, GameObject prefab)
		{
			return ViewEntitiesContainer.Spawn(prefab, entityId);
		}


		public void OverrideDefaultStats(EntId newEntityID, IReadOnlyCollection<StatOverride> statOverrides)
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
		
	}

	public sealed class GameLogic : BaseGameLogic
	{
		[Inject] private readonly PlayerContainer PlayerContainer = null!;
		
		[Inject] private readonly IEntityHierarchySystem IEntityHierarchySystem = null!;

		[Inject] private readonly BasicCompContainer<BehaviourAgentData> BehaviourAgentContainer = null!;
		[Inject] private readonly BehaviourTreeContainer BehaviourTreeContainer = null!;
		[Inject] private readonly BasicCompContainer<DeadBodyData> DeadBodyContainer = null!;

		[Inject] private readonly BasicCompContainer<StatusEffectOwnerData> StatusEffectOwnerContainer = null!;
		[Inject] private readonly BasicCompContainer<StatusEffectData> StatusEffectContainer = null!;
		[Inject] private readonly StatusEffectSystem StatusEffectSystem = null!;

		[Inject] private readonly BasicCompContainer<WorldEntityData> WorldEntityContainer = null!;
		[Inject] private readonly BasicCompContainer<GameData> GameContainer = null!;
		

		public EntId SpawnEnemy(Vector2 position, EnemyConfig enemyConfig)
		{
			//TODO: we need to spawn enemies as premade clumps with potential positions premade.
			//Also we need to pass in some special world positions in the area if possible
			//to make interesting clumps of enemies. For now we will just spawn them in a random position around the player.
			
			Vector3 spawnPosition = new Vector3(position.x, 0f, position.y);
			EntId enemyId = new EnemyEntity(enemyConfig.Size).ID;
			EntityViewAtributes view = AddView(enemyId, enemyConfig.EnemyPrefab.gameObject, spawnPosition, Quaternion.identity);
			EnemyView enemyView = view.Get<EnemyView>();

			enemyView.NavMeshAgent.Warp(spawnPosition);
			enemyView.NavMeshAgent.radius = (float) enemyConfig.Size;
			// enemyView.SetSize(GameplayConfig.EnemyAttributes.Size);
			
			AddBehaviourTree();
			ref BehaviourAgentData agentData = ref BehaviourAgentContainer.GetComponent(enemyId);
			agentData.AttackFrameNorm = enemyConfig.AttackFrameNormalizedTime;
			agentData.Enable = true;
			
			OverrideDefaultStats(enemyId, enemyConfig.StatsOverride);

			
			
			// foreach (EnemyAbilityConfig abilityConfig in enemyConfig.Abilities)
			// 	AddEnemyAbility(abilityConfig, enemyId);
			
			// enemyView.TriggerSpawning();
			
			return enemyId;

			
			
			void AddGoblinPawnBehaviourTree()
			{
				ref BehaviourNodeData findTargetNode = ref BehaviourTreeContainer.AddNode(enemyId, NodeType.FindTarget, out _);
				findTargetNode.Cooldown = 0f;
				ref BehaviourNodeData moveToTargetNode = ref BehaviourTreeContainer.AddNode(enemyId, NodeType.MoveToTarget, out _);
				ref BehaviourNodeData attackNode = ref BehaviourTreeContainer.AddNode(enemyId, NodeType.AttackTarget, out _);
				
			}
			void AddGoblinShamanBehaviourTree()
			{
				// ref BehaviourNodeData findTargetNode = ref BehaviourTreeContainer.AddNode(enemyId, NodeType.FindTarget, out _);
				// findTargetNode.Cooldown = 0f;
				// ref BehaviourNodeData moveToTargetNode = ref BehaviourTreeContainer.AddNode(enemyId, NodeType.MoveToTarget);
				ref BehaviourNodeData moveAwayFromTargetNode = ref BehaviourTreeContainer.AddNode(enemyId, NodeType.MoveAwayFromTarget, out _);
				// ref BehaviourNodeData findSupportTargetNode = ref BehaviourTreeContainer.AddNode(enemyId, NodeType.FindSupportTarget, out _);
				// findSupportTargetNode.Cooldown = 0f;
				
				// ref BehaviourNodeData abilitiesParentData = ref BehaviourTreeContainer.AddNode(enemyId, NodeType.FirstChild, out abilitiesParent);
				//
				// foreach (EnemyAbilityConfig abilityConfig in enemyConfig.Abilities)
				// 	AddEnemyAbility(abilityConfig, enemyId, abilitiesParent);
				
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

				ref BehaviourNodeData abilitiesParentData = ref BehaviourTreeContainer.AddNode(enemyId, 
																								NodeType.FirstChild, 
																								out uint abilitiesParentOrder);
				foreach (EnemyAbilityConfig abilityConfig in enemyConfig.Abilities)
					AddEnemyAbility(abilityConfig, enemyId, abilitiesParentOrder);

			}
		}
		
		
		public EntId SpawnDirectionSpell(EntId ownerAbilityID, SpellConfig spellConfig, Vector3 direction, Vector3 position)
		{
			EntId newEntityID;
			switch (spellConfig.SpellType)
			{
				case SpellType.Projectile:
					newEntityID = SpawnBullet(ownerAbilityID, spellConfig, direction, position);
					break;
				default:
					return EntId.Invalid;
			}
			
			// SpellEntity spellEntity = new SpellEntity(spellConfig);
			//
			AddView(newEntityID, spellConfig.SpellPrefab.gameObject);
			
			return newEntityID;
		}


		public EntId SpawnPositionSpell(EntId ownerAbilityID, SpellConfig spellConfig, 
											Vector3 originPosition, Vector3 targetPosition)
		{
			EntId newEntityID;
			switch (spellConfig.SpellType)
			{
				case SpellType.Mortar:
					newEntityID = SpawnMortar(ownerAbilityID, spellConfig, originPosition, targetPosition);
					break;
				default:
					return EntId.Invalid;
			}
			
			// SpellEntity spellEntity = new SpellEntity(spellConfig);
			//
			AddView(newEntityID, spellConfig.SpellPrefab.gameObject);
			
			return newEntityID;
		}

		public EntId SpawnMortar(EntId ownerAbilityID, SpellConfig spellConfig, Vector3 originPosition, Vector3 targetPosition)
		{
			EntId mortarId = new MortarEntity(spellConfig, originPosition, targetPosition).ID;

			EntityViewAtributes view = AddView(mortarId, spellConfig.SpellPrefab.gameObject, targetPosition, Quaternion.identity);
			
			Fix damage = StatsSystem.GetStatValue(ownerAbilityID, GameStatsContainer.Damage);
			StatsSystem.SetBaseValue(mortarId, GameStatsContainer.Damage, damage);

			OverrideDefaultStats(mortarId, spellConfig.StatsOverride);
			
			return mortarId;
		}

		
		public EntId SpawnBullet(EntId ownerAbilityID, SpellConfig spellConfig, Vector3 direction, Vector3 position)
		{
			EntId casterID = IEntityHierarchySystem.GetParent(ownerAbilityID);
			EntId bulletId = new BulletEntity(spellConfig, casterID).ID;

			ref WorldEntityData worldEntityData = ref WorldEntityContainer.GetComponent(bulletId);
			
			EntityViewAtributes view = AddView(bulletId, spellConfig.SpellPrefab.gameObject, position, Quaternion.LookRotation(direction.normalized));
			
			Fix damage = StatsSystem.GetStatValue(ownerAbilityID, GameStatsContainer.Damage);
			StatsSystem.SetBaseValue(bulletId, GameStatsContainer.Damage, damage);

			OverrideDefaultStats(bulletId, spellConfig.StatsOverride);
			
			// Rigidbody rigidbody = view.Get<Rigidbody>();
			float speed = (float) StatsSystem.GetStatValue(bulletId, GameStatsContainer.Speed);
			Vector3 velocity = direction * speed;
			worldEntityData.Velocity = velocity;
			
			
			// rigidbody.linearVelocity = velocity;
			
			return bulletId;
		}
		
		
		public EntId AddEnemyAbility(EnemyAbilityConfig enemyAbilityConfig, EntId entityID, uint parentNodeOrder = 0)
		{
			EnemyAbilityEntity enemyAbilityEntity = new EnemyAbilityEntity(entityID, enemyAbilityConfig);
			EntId abilityEntityID = enemyAbilityEntity.ID;

			
			// TODO we need to add a specific node for each ability type, for now we will just add a generic node for all abilities.
			ref BehaviourNodeData nodeData = ref BehaviourTreeContainer.AddNode(entityID, NodeType.CastAbility, out _, parentNodeOrder);
			nodeData.AbilityID = abilityEntityID;

			OverrideDefaultStats(abilityEntityID, enemyAbilityConfig.StatsOverride);
			
			return abilityEntityID;
		}



		

		public void KillEnemy(EntId enemyID)
		{
			// DeadBodyEntity deadBodyEntity = new DeadBodyEntity();
			// ViewEntitiesContainer.SwitchOwnership(enemyID, deadBodyEntity.ID);

			// Entity destruction is deferred, so the behaviour tree would still run this frame without a view.
			ref BehaviourAgentData agentData = ref BehaviourAgentContainer.GetComponent(enemyID);
			agentData.Enable = false;
			
			
			EnemyDeathEvent enemyDeathEvent = EntityEventQueue.Execute<EnemyDeathEvent>(enemyID);

			
			// EntitiesContainer.DestroyEntity(enemyID);
			
			// return deadBodyEntity.ID;
		}

		public EntId ApplyStatusEffect(EntId target, StatusEffectConfig statusEffectConfig)
		{
			StatusEffectEntity statusEffectEntity = new StatusEffectEntity(target, statusEffectConfig);
			
			OverrideDefaultStats(statusEffectEntity.ID, statusEffectConfig.StatsOverride);
			
			return statusEffectEntity.ID;
		}


		
	}
}