using System.Collections.Generic;
using System.Runtime.InteropServices;
using Core.Events;
using Core.Initialization;
using Core.Model;
using Core.Model.ModelSystems;
using Core.Systems;
using FixedPointy;
using Game.Logic;
using Game.Player.Abilities;
using Game.Spells;
using UnityEngine;
using Zenject;
using static Game.Enemies.EnemyAbilities.EnemyAbilityTargetType;

namespace Game.Enemies.EnemyAbilities
{
	public sealed class EnemyAbilityEntity : Entity,
											IEnemyAbility,
											IVSAbility,
											IHierarchyEntity
	{
		public EnemyAbilityEntity(EntId ownerID, EnemyAbilityConfig dataConfig) 
			: base()
		{
			var hierarchySystem = GetSystem<IEntityHierarchySystem>();
			hierarchySystem.AddChild(ownerID, ID);
	            
			ref VSAbilityData abilityData = ref GetComponent<VSAbilityData>();
			abilityData.dataConfig = dataConfig;
			
			ref EnemyAbilityData enemyAbilityData = ref GetComponent<EnemyAbilityData>();
			enemyAbilityData.OwnerID = ownerID;
			enemyAbilityData.AbilityType = dataConfig.AbilityType;
		}
		
	}

	public enum EnemyAbilityTargetType
	{
		AlwaysValid                 = 0,
		AlivePlayer                 = 1,
		PositionWithinRange         = 2,
		AliveEnemy                  = 3,
		DeadEnemy                   = 4,
	}

	public enum EnemyAbilityType
	{
		Custom = 0,
		Ammo = 1,
	}

	[StructLayout(LayoutKind.Auto)]
	public struct EnemyAbilityData : IComponentData
	{
		private const int MAX_TARGETS = 5;
		public EntId ID { get; set; }
		public EnemyAbilityType AbilityType;
		public EntId OwnerID;
		
		public AbilityState State;
			

		#region Targets

		public EnemyAbilityTargetType TargetType;
		public float FindTargetCooldown;
		public float FindTargetCooldownMax;
		
		
		[MarshalAs(UnmanagedType.ByValArray, SizeConst=MAX_TARGETS)] public EntId[] Targets;
		public uint TargetCount;
		
		[MarshalAs(UnmanagedType.ByValArray, SizeConst=MAX_TARGETS)] public Vector2[] TargetPositions;
		public uint TargetPositionCount;
		
		public bool TargetSet;

		#endregion

		public void Init()
		{
			AbilityType = EnemyAbilityType.Ammo;
			TargetSet = false;
			State = AbilityState.Ready;
			TargetCount = 0;
			TargetPositionCount = 0;
			FindTargetCooldown = DEFAULT_FIND_TARGET_COOLDOWN;
			FindTargetCooldownMax = DEFAULT_FIND_TARGET_COOLDOWN;
			Targets = new EntId[MAX_TARGETS];
			TargetPositions = new Vector2[MAX_TARGETS];
		}

		private const float DEFAULT_FIND_TARGET_COOLDOWN = 1.5f;

		public bool HasTarget()
		{
			return TargetSet;
		}
		
		public void SetTarget(List<EntId> targets)
		{
			TargetSet = true;
			TargetCount = (uint)targets.Count;
			for (int i = 0; i < targets.Count && i < MAX_TARGETS; i++)
				Targets[i] = targets[i];
			
			TargetPositionCount = 0;
		}

		public void SetTarget(List<EntId> targets, List<Vector3> targetPositions)
		{
			TargetSet = true;
			TargetCount = (uint)targets.Count;
			for (int i = 0; i < targets.Count && i < MAX_TARGETS; i++)
				Targets[i] = targets[i];
			
			TargetPositionCount = (uint)targetPositions.Count;
			for (int i = 0; i < targetPositions.Count && i < MAX_TARGETS; i++)
				TargetPositions[i] = new Vector2(targetPositions[i].x, targetPositions[i].z);
		}

		public void SetTarget(List<EntId> targets, List<Vector2> targetPositions)
		{
			TargetSet = true;
			TargetCount = (uint)targets.Count;
			for (int i = 0; i < targets.Count && i < MAX_TARGETS; i++)
				Targets[i] = targets[i];
			
			TargetPositionCount = (uint)targetPositions.Count;
			for (int i = 0; i < targetPositions.Count && i < MAX_TARGETS; i++)
				TargetPositions[i] = targetPositions[i];
		}
		
		
		public void SetTarget(List<Vector3> targetPositions)
		{
			TargetSet = true;
			TargetCount = 0;
			TargetPositionCount = (uint)targetPositions.Count;
			for (int i = 0; i < targetPositions.Count && i < MAX_TARGETS; i++)
				TargetPositions[i] = new Vector2(targetPositions[i].x, targetPositions[i].z);
		}

		
		public void SetTarget(List<Vector2> targetPositions)
		{
			TargetSet = true;
			TargetCount = 0;
			TargetPositionCount = (uint)targetPositions.Count;
			for (int i = 0; i < targetPositions.Count && i < MAX_TARGETS; i++)
				TargetPositions[i] = targetPositions[i];
			
		}
		
		public void ClearTarget()
		{
			TargetSet = false;
			TargetPositionCount = 0;
			TargetCount = 0;
		}
		
	}
	public interface IEnemyAbility : ISimulation, Component<EnemyAbilityData> { }
	
	
	public sealed class EnemyAbilitySystem : UpdateComponents<EnemyAbilityData>, IInitSystem
	{
		[Inject] private readonly BasicCompContainer<EnemyAbilityData> EnemyAbilityContainer = null!;
		[Inject] private readonly ObjectBuilder ObjectBuilder = null!;
		[Inject] private readonly StatsSystem StatsSystem = null!;
		[Inject] private readonly GameStatsContainer GameStatsContainer = null!;



		
		private readonly Dictionary<EnemyAbilityTargetType, AblilityTargetType> TargetTypeHandlers = new()
		{
			{ AlwaysValid, new AlwaysValidTargetType() },
			{ AlivePlayer, new AlivePlayerTargetType() },
			{ PositionWithinRange, new PositionWithinRangeTargetType() },
			{ AliveEnemy, new AliveEnemyTargetType() },
			{ DeadEnemy, new DeadEnemyTargetType() },
		};

		public void Initialize()
		{
			foreach (AblilityTargetType ablilityTargetType in TargetTypeHandlers.Values)
				ObjectBuilder.Inject(ablilityTargetType);
		}
		
		public bool IsTargetValid(EntId abilityID)
		{
			ref EnemyAbilityData abilityData = ref EnemyAbilityContainer.GetComponent(abilityID);
			// if(!abilityData.HasTarget())
			// 	return false;
			// 	
			return TargetTypeHandlers[abilityData.TargetType].IsValidTarget(ref abilityData);
		}
		
		public bool IsTargetValid(ref EnemyAbilityData abilityData)
		{
			return TargetTypeHandlers[abilityData.TargetType].IsValidTarget(ref abilityData);
		}

		
		
		public void UpdateComponents(float deltaTime)
		{
			uint topIndex = EnemyAbilityContainer.TopEmptyIndex;
			for (int i = 0; i < topIndex; i++)
			{
				ref EnemyAbilityData abilityData = ref EnemyAbilityContainer.Components[i];
				EntId abilityID = abilityData.ID;

				abilityData.FindTargetCooldown -= deltaTime;
				if (abilityData.FindTargetCooldown <= 0f)
				{
					abilityData.FindTargetCooldown += abilityData.FindTargetCooldownMax;
					abilityData.ClearTarget();

					FindTargetsAbilityEvent findTargetsAbilityEvent = EntityEventQueue.Execute<FindTargetsAbilityEvent>(abilityID);
					findTargetsAbilityEvent.EnemyOwner = abilityData.OwnerID;
				}


				switch (abilityData.AbilityType)
				{
					case EnemyAbilityType.Ammo:
						AmmoAbilityUpdate(ref abilityData);
						break;
				}
				void AmmoAbilityUpdate(ref EnemyAbilityData enemyAbilityData)
				{
					switch (enemyAbilityData.State)
					{
						case AbilityState.Recover:
							Fix newDepletedValue = StatsSystem.ChangeDepletedValue(enemyAbilityData.ID, GameStatsContainer.Cooldown, -deltaTime);
							if (newDepletedValue <= Fix.Zero)
								enemyAbilityData.State = AbilityState.Ready;
							break;
					}
				}
				
			}
		}

		public bool Active { get; set; } = true;
		public SystemGroup Group { get; } = GameSystemsGroup.GAME_LOGIC_SYSTEM;

	}
	
	
}
