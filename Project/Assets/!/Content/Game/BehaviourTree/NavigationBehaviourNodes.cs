using Core.Model;
using Core.View;
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
		[Inject] private readonly ViewEntitiesContainer ViewEntitiesContainer = null!;
		[Inject] private readonly GameplayConfig GameplayConfig = null!;
		[Inject] private readonly BasicCompContainer<BehaviourAgentData> BehaviourAgentContainer = null!;
		[Inject] private readonly GameStatsContainer GameStatsContainer = null!;
		[Inject] private readonly StatsSystem StatsSystem = null!;



		
		public override BehaviourStatus Process(EntId entityId, ref BehaviourNodeData nodeData, 
												ref BehaviourAgentData agentData, float deltaTime)
		{

			EntityViewAtributes viewAtributes = ViewEntitiesContainer.GetEntityViewAtributes(entityId);
			Transform transform = viewAtributes.GameObject.transform;
			NavMeshAgent navMeshAgent = viewAtributes.Get<NavMeshAgent>();
			EnemyView enemyView = viewAtributes.Get<EnemyView>();

			EntityViewAtributes targetViewAtributes = ViewEntitiesContainer.GetEntityViewAtributes(agentData.Target);
			Vector3 targetPosition = targetViewAtributes!.GameObject!.transform.position;

			Vector3 delta = targetPosition - transform.position;
			delta.y = 0f;
			float distanceToTarget = Vector3.SqrMagnitude(delta);

			if(distanceToTarget < GameplayConfig.EnemiesAttributes.DistanceToAttack) {
				return BehaviourStatus.Success;
			}
			navMeshAgent.speed = (float) StatsSystem.GetStatDepletedValue(entityId, GameStatsContainer.Speed);
			navMeshAgent.SetDestination(targetPosition);
			transform.LookAt(targetPosition);
			
			return BehaviourStatus.Running;
		}
		
		public override void Reset(EntId entityId, ref BehaviourNodeData behaviourNodeData)
		{
			base.Reset(entityId, ref behaviourNodeData);

			EntityViewAtributes viewAtributes = ViewEntitiesContainer.GetEntityViewAtributes(entityId);
			NavMeshAgent navMeshAgent = viewAtributes!.Get<NavMeshAgent>();
			
			navMeshAgent.ResetPath();
		}

	}
	
	
	public sealed class ChoseTargetNode : BaseBehaviourNode
	{
		private const float CHOSE_TARGET_COOLDOWN = 5f;
		
		[Inject] private readonly ViewEntitiesContainer ViewEntitiesContainer = null!;
		[Inject] private readonly BasicCompContainer<BehaviourAgentData> BehaviourAgentContainer = null!;
		[Inject] private readonly PlayerContainer PlayerContainer = null!;
		
		public override BehaviourStatus Process(EntId entityId, ref BehaviourNodeData nodeData, 
												ref BehaviourAgentData agentData, float deltaTime)
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