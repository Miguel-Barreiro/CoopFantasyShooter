using Core.Model;
using Core.View;
using Game.Enemies;
using Global;
using UnityEngine;
using UnityEngine.AI;
using Zenject;

namespace Game.BehaviourTree
{
	public abstract class ActionBehaviourNode : BaseBehaviourNode
	{
		
	}
	
	
	
	public sealed class SimpleAttackNode : ActionBehaviourNode
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

			
			if(distanceToTarget <= 0.5f)
			{
				navMeshAgent.velocity = Vector3.zero;
			}
			
			if(distanceToTarget < GameplayConfig.EnemiesAttributes.DistanceToAttack)
			{
				// navMeshAgent.velocity = Vector3.zero;
				enemyView.SetAttack(targetPosition, true);
				
				return BehaviourStatus.Success;
			}
			

			
			// enemyView.SetAttack(targetPosition, false);
			
			enemyView.SetAttack(targetPosition, false);
			// navMeshAgent.speed = (float) StatsSystem.GetStatDepletedValue(entityId, GameStatsContainer.Speed);
			// navMeshAgent.SetDestination(targetPosition);
			// transform.LookAt(targetPosition);
			return BehaviourStatus.Failure;
		}
	}
}