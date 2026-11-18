#nullable enable
using System.Collections.Generic;
using Core.Model;
using Core.Utils.CachedDataStructures;
using Core.View;
using FixedPointy;
using Game.Components;
using Game.Enemies;
using Game.Player;
using Global;
using UnityEngine;
using UnityEngine.AI;
using Zenject;

namespace Game.BehaviourTree
{
	
	public sealed class MoveToTargetNode : BaseBehaviourNode
	{
		// [Inject] private readonly ViewEntitiesContainer ViewEntitiesContainer = null!;
		[Inject] private readonly GameplayConfig GameplayConfig = null!;
		[Inject] private readonly BasicCompContainer<BehaviourAgentData> BehaviourAgentContainer = null!;
		[Inject] private readonly GameStatsContainer GameStatsContainer = null!;
		[Inject] private readonly StatsSystem StatsSystem = null!;
		[Inject] private readonly BasicCompContainer<WorldEntityData> WorldEntityContainer = null!;
		
		public override BehaviourStatus Process(EntId entityId, ref BehaviourNodeData nodeData, 
												ref BehaviourAgentData agentData, float deltaTime, AgentMotion agentMotion)
		{
			if (agentData.Target == EntId.Invalid)
			{
				return BehaviourStatus.Failure;
			}

			NavMeshAgent navMeshAgent = agentMotion.NavMeshAgent;
			if (!navMeshAgent.isOnNavMesh)
			{
				return BehaviourStatus.Failure;
			}
			ref  WorldEntityData targetWorldEntityData = ref WorldEntityContainer.GetComponent(agentData.Target);
			Vector3 targetPosition = targetWorldEntityData.Position;

			Vector3 delta = targetPosition - agentData.Position;
			delta.y = 0f;

			Transform transform = agentMotion.Transform;
			
			float distanceToTarget = Vector3.Magnitude(delta);
			if(distanceToTarget < (GameplayConfig.EnemiesAttributes.MinDistanceToTarget + navMeshAgent.radius)) {
				navMeshAgent.speed = 0f;
				navMeshAgent.ResetPath();
				transform.LookAt(targetPosition);				
				return BehaviourStatus.Success;
			}

			bool isDestinationValid = navMeshAgent.SetDestination(targetPosition);
			if(isDestinationValid == false) {
				return BehaviourStatus.Failure;
			}
			
			navMeshAgent.speed = (float) StatsSystem.GetStatValue(entityId, GameStatsContainer.Speed);
			navMeshAgent.acceleration = (float) StatsSystem.GetStatValue(entityId, GameStatsContainer.Acerelation);
			navMeshAgent.angularSpeed = (float) StatsSystem.GetStatValue(entityId, GameStatsContainer.TurningSpeed);
			transform.LookAt(targetPosition);
			return BehaviourStatus.Success;
		}
		
		public override void Reset(EntId entityId, ref BehaviourNodeData behaviourNodeData, AgentMotion agentMotion)
		{
			base.Reset(entityId, ref behaviourNodeData, agentMotion);

			NavMeshAgent navMeshAgent = agentMotion.NavMeshAgent;
			
			navMeshAgent!.velocity = Vector3.zero;
			if (navMeshAgent.isOnNavMesh)
			{
				navMeshAgent.ResetPath();
			}
		}

	}
	
	
	public sealed class MoveAwayFromTargetNode : BaseBehaviourNode
	{
		[Inject] private readonly ViewEntitiesContainer ViewEntitiesContainer = null!;
		[Inject] private readonly GameplayConfig GameplayConfig = null!;
		[Inject] private readonly BasicCompContainer<BehaviourAgentData> BehaviourAgentContainer = null!;
		[Inject] private readonly GameStatsContainer GameStatsContainer = null!;
		[Inject] private readonly StatsSystem StatsSystem = null!;

		public override BehaviourStatus Process(EntId entityId, ref BehaviourNodeData nodeData, 
												ref BehaviourAgentData agentData, float deltaTime, 
													AgentMotion agentMotion)
		{
			EntityViewAtributes viewAtributes = ViewEntitiesContainer.GetEntityViewAtributes(entityId)!;
			Transform transform = viewAtributes!.GameObject!.transform;
			NavMeshAgent navMeshAgent = viewAtributes.Get<NavMeshAgent>()!;
			if (!navMeshAgent.isOnNavMesh)
			{
				return BehaviourStatus.Failure;
			}
			
			Vector3 targetPosition = agentData.ClosestEnemyPosition;

			Vector3 delta = transform.position - targetPosition;
			delta.y = 0f;
			float distanceToTarget = Vector3.Magnitude(delta);


			Fix fullSafeDistance = StatsSystem.GetStatValue(entityId, GameStatsContainer.SafeDistanceToTarget);
			Fix safeDistance = fullSafeDistance* 0.8f;
			if(distanceToTarget > safeDistance ) {
				return BehaviourStatus.Success;
			}
			
			navMeshAgent.speed = (float) StatsSystem.GetStatValue(entityId, GameStatsContainer.Speed);
			navMeshAgent.acceleration = (float) StatsSystem.GetStatValue(entityId, GameStatsContainer.Acerelation);
			navMeshAgent.angularSpeed = (float) StatsSystem.GetStatValue(entityId, GameStatsContainer.TurningSpeed);

			Vector3 runAwayNewPosition = targetPosition + delta.normalized * (float) fullSafeDistance;
			navMeshAgent.SetDestination(runAwayNewPosition);
			transform.LookAt(runAwayNewPosition);
			
			return BehaviourStatus.Running;
		}
	}
	
	public sealed class FindSupportTargetNode : BaseBehaviourNode
	{
		[Inject] private readonly ViewEntitiesContainer ViewEntitiesContainer = null!;
		[Inject] private readonly BasicCompContainer<BehaviourAgentData> BehaviourAgentContainer = null!;

		[Inject] private readonly GameStatsContainer GameStatsContainer = null!;
		[Inject] private readonly StatsSystem StatsSystem = null!;

		[Inject] private readonly IRandomSystem RandomSystem = null!;
		[Inject] private readonly BasicCompContainer<EnemyData> EnemyContainer = null!;
		[Inject] private readonly EntitiesContainer EntitiesContainer = null!;

		
		private const float CHOSE_TARGET_COOLDOWN = 5f;
		
		public override BehaviourStatus Process(EntId entityId, ref BehaviourNodeData nodeData, 
												ref BehaviourAgentData agentData, float deltaTime, 
													AgentMotion agentMotion)
		{
			uint maxEnemies = EnemyContainer.TopEmptyIndex;
			if(maxEnemies == 0) {
				agentData.SupportTarget = EntId.Invalid;
				return BehaviourStatus.Failure;
			}

			if(agentData.SupportTarget != EntId.Invalid) {
				Entity? entity = EntitiesContainer.GetEntity(agentData.SupportTarget);
				if(entity == null) {
					agentData.SupportTarget = EntId.Invalid;
				}
				
				nodeData.Cooldown -= deltaTime;
				if(nodeData.Cooldown > 0f) 
				{
					return BehaviourStatus.Success;
				} else
				{
					nodeData.Cooldown += CHOSE_TARGET_COOLDOWN;
				}
			}
			
			int enemyToBuff = (int)RandomSystem.Range(0, (int)maxEnemies);
			ref EnemyData enemyData = ref EnemyContainer.Components[enemyToBuff];
			IEnumerable<StatModId> currentSpells = StatsSystem.GetModifiers(entityId, enemyData.ID);

			using CachedList<StatModId> currentSpellsList = ListCache<StatModId>.Get(currentSpells);
			int maxAttempts = 10;
			while (currentSpellsList.Count > 0 && maxAttempts > 0)
			{
				enemyToBuff = (int)RandomSystem.Range(0, (int)maxEnemies);
				enemyData = ref EnemyContainer.Components[enemyToBuff];
				currentSpells = StatsSystem.GetModifiers(entityId, enemyData.ID);

				currentSpellsList.Clear();
				currentSpellsList.AddRange(currentSpells);
				maxAttempts--;
			}

			if (maxAttempts == 0)
			{
				return BehaviourStatus.Failure;
			}
			
			agentData.SupportTarget = enemyData.ID;
			return BehaviourStatus.Success;

		}

	}
	
	public sealed class ChoseTargetNode : BaseBehaviourNode
	{
		private const float CHOSE_TARGET_COOLDOWN = 5f;
		
		[Inject] private readonly ViewEntitiesContainer ViewEntitiesContainer = null!;
		[Inject] private readonly BasicCompContainer<BehaviourAgentData> BehaviourAgentContainer = null!;
		[Inject] private readonly PlayerContainer PlayerContainer = null!;
		
		public override BehaviourStatus Process(EntId entityId, ref BehaviourNodeData nodeData, 
												ref BehaviourAgentData agentData, float deltaTime, 
													AgentMotion agentMotion)
		{
			if(agentData.Target != EntId.Invalid) {
				nodeData.Cooldown -= deltaTime;
				if(nodeData.Cooldown > 0f) 
				{
					return BehaviourStatus.Success;
				} else
				{
					nodeData.Cooldown += CHOSE_TARGET_COOLDOWN;
				}
			}
			
			EntityViewAtributes ownViewAtributes = ViewEntitiesContainer.GetEntityViewAtributes(entityId);
			
			uint topIndex = PlayerContainer.TopEmptyIndex;
			if(topIndex == 0) {
				return BehaviourStatus.Failure;
			}

			float closestDistance = float.MaxValue;
			EntId closestPlayerId = EntId.Invalid;
			for (int i = 0; i < topIndex; i++)
			{
				ref PlayerData componentData = ref PlayerContainer.Components[i];
				EntityViewAtributes playerViewAtributes = ViewEntitiesContainer.GetEntityViewAtributes(componentData.ID);
				float distance = Vector3.Distance(playerViewAtributes!.GameObject!.transform.position, 
												ownViewAtributes!.GameObject!.transform.position);
				if(distance < closestDistance) {
					closestDistance = distance;
					closestPlayerId = componentData.ID;
				}
			}
			
			agentData.Target = closestPlayerId;
			
			return BehaviourStatus.Success;
		}

	}

}