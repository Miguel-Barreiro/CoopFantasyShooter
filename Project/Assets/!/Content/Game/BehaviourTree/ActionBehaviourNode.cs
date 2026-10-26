using System.Collections.Generic;
using System.Linq;
using Core.Events;
using Core.Model;
using Core.Utils.CachedDataStructures;
using Core.View;
using FixedPointy;
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

	public sealed class SupportCastNode : ActionBehaviourNode
	{
		
		[Inject] private readonly ViewEntitiesContainer ViewEntitiesContainer = null!;
		[Inject] private readonly GameplayConfig GameplayConfig = null!;
		[Inject] private readonly BasicCompContainer<BehaviourAgentData> BehaviourAgentContainer = null!;
		[Inject] private readonly GameStatsContainer GameStatsContainer = null!;
		[Inject] private readonly StatsSystem StatsSystem = null!;
		[Inject] private readonly IRandomSystem RandomSystem = null!;

		[Inject] private readonly BasicCompContainer<EnemyData> EnemyContainer = null!;
		
		
		public override BehaviourStatus Process(EntId entityId, ref BehaviourNodeData nodeData, 
												ref BehaviourAgentData agentData, float deltaTime)
		{
			if(agentData.SupportTarget == EntId.Invalid)
				return BehaviourStatus.Failure;
			
			EntityViewAtributes viewAtributes = ViewEntitiesContainer.GetEntityViewAtributes(entityId);
			Transform transform = viewAtributes.GameObject.transform;
			NavMeshAgent navMeshAgent = viewAtributes.Get<NavMeshAgent>();
			EnemyView enemyView = viewAtributes.Get<EnemyView>();

			EntityViewAtributes targetViewAtributes = ViewEntitiesContainer.GetEntityViewAtributes(agentData.SupportTarget);
			Vector3 targetPosition = targetViewAtributes!.GameObject!.transform.position;
			
			Vector3 delta = targetPosition - transform.position;
			delta.y = 0f;
			float distanceToTarget = Vector3.SqrMagnitude(delta);
			

			float distanceToCast = (float) StatsSystem.GetStatDepletedValue(entityId, GameStatsContainer.CastDistance);
			if(distanceToTarget < distanceToCast)
			{
				// navMeshAgent.velocity = Vector3.zero;
				enemyView.SetCast(targetPosition, true);
				CastSupportDebug(ref agentData, targetViewAtributes.Get<EnemyView>());
				
				return BehaviourStatus.Success;
			}
			
			enemyView.SetCast(targetPosition, false);

			if (!navMeshAgent.isOnNavMesh)
			{
				return BehaviourStatus.Failure;
			}

			navMeshAgent.speed = (float) StatsSystem.GetStatDepletedValue(entityId, GameStatsContainer.Speed);
			navMeshAgent.SetDestination(targetPosition);
			transform.LookAt(targetPosition);
			return BehaviourStatus.Failure;

			void CastSupportDebug(ref BehaviourAgentData agentData, EnemyView targetView)
			{
				targetView.SetSupport(entityId, true);
				StatsSystem.RemoveAllModifiersFrom(entityId, agentData.SupportTarget, GameStatsContainer.Speed);
				StatsSystem.AddModifier(entityId, agentData.SupportTarget, 
										GameStatsContainer.Speed, 1.5f, 
										StatModifierType.Multiplicative);
				StatsSystem.AddModifier(entityId, agentData.SupportTarget, 
										GameStatsContainer.Acerelation, 1.5f, 
										StatModifierType.Multiplicative);
				StatsSystem.AddModifier(entityId, agentData.SupportTarget, 
										GameStatsContainer.TurningSpeed, 1.5f, 
										StatModifierType.Multiplicative);
				StatsSystem.ResetDepletedValueToMax(agentData.SupportTarget, GameStatsContainer.Speed);
				StatsSystem.ResetDepletedValueToMax(agentData.SupportTarget, GameStatsContainer.Acerelation);
				StatsSystem.ResetDepletedValueToMax(agentData.SupportTarget, GameStatsContainer.TurningSpeed);
			}
		}

		public override void Reset(EntId entityId, ref BehaviourNodeData nodeNodeData)
		{
			base.Reset(entityId, ref nodeNodeData);
			nodeNodeData.Cooldown = 0f;
		}
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

			if(agentData.Target == EntId.Invalid)
				return BehaviourStatus.Failure;
			
			EntityViewAtributes viewAtributes = ViewEntitiesContainer.GetEntityViewAtributes(entityId);
			Transform transform = viewAtributes.GameObject.transform;
			NavMeshAgent navMeshAgent = viewAtributes.Get<NavMeshAgent>();
			EnemyView enemyView = viewAtributes.Get<EnemyView>();
			
			EntityViewAtributes targetViewAtributes = ViewEntitiesContainer.GetEntityViewAtributes(agentData.Target);
			Vector3 targetPosition = targetViewAtributes!.GameObject!.transform.position;
			
			Vector3 delta = targetPosition - transform.position;
			delta.y = 0f;
			float distanceToTarget = Vector3.Magnitude(delta);

			float attackDistance = (float) StatsSystem.GetStatDepletedValue(entityId, GameStatsContainer.AttackDistance);
			
			if(distanceToTarget <= navMeshAgent.radius + 0.1f)
			{
				navMeshAgent.velocity = Vector3.zero;
			}

			float attackDuration = (float) StatsSystem.GetStatValue(entityId, GameStatsContainer.AttackDuration);
			float chargeDuration = (float) StatsSystem.GetStatValue(entityId, GameStatsContainer.AttackChargeDuration);

			float nextFrame;
			if(distanceToTarget < (navMeshAgent.radius + attackDistance))
			{
				// float currentFrame = GetAttackFrameFromCooldown(nodeData.Cooldown, attackDuration, chargeDuration, agentData.AttackFrameNorm);
				float newCooldown = nodeData.Cooldown + deltaTime;
				if (nodeData.Cooldown <= chargeDuration && newCooldown > chargeDuration)
					EntityEventQueue.Execute<EnemyAttackEvent>(enemyView.EntityID);

				
				nextFrame = GetAttackFrameFromCooldown(newCooldown, attackDuration, chargeDuration, agentData.AttackFrameNorm);
				enemyView.SetAttack(targetPosition, true, nextFrame);
				transform.LookAt(targetPosition);
				
				float attackTotalDuration = attackDuration + chargeDuration;
				
				if (newCooldown >= attackTotalDuration)
					nodeData.Cooldown = newCooldown - attackTotalDuration;
				else
					nodeData.Cooldown = newCooldown;

				return BehaviourStatus.Success;
			}
			
			float chargeDistance = navMeshAgent.radius + attackDistance * 6;
			if( distanceToTarget < chargeDistance)
			{
				nodeData.Cooldown = Mathf.Clamp(nodeData.Cooldown + deltaTime, 0f, chargeDuration);
				nextFrame = GetAttackFrameFromCooldown(nodeData.Cooldown, attackDuration, chargeDuration, agentData.AttackFrameNorm);
				enemyView.SetAttack(targetPosition, true, nextFrame);
				transform.LookAt(targetPosition);
				return BehaviourStatus.Success;
			}
			
			nextFrame = GetAttackFrameFromCooldown(nodeData.Cooldown, attackDuration, chargeDuration, agentData.AttackFrameNorm);
			nodeData.Cooldown = 0;
			enemyView.SetAttack(targetPosition, false, nextFrame);
			// nodeData.Cooldown -= Mathf.Clamp(deltaTime - deltaTime, 0f, nodeData.Cooldown);
			// float nextSlowdownAttackFrame = GetAttackFrameFromCooldown(nodeData.Cooldown, attackDuration, chargeDuration, agentData.AttackFrameNorm);
			// enemyView.SetAttack(targetPosition, false, nextSlowdownAttackFrame);
			return BehaviourStatus.Failure;
			
		}
		
		public float GetAttackFrameFromCooldown(float cooldown, float attackDuration, float chargeDuration, float attackFrameNorm)
		{
			if(cooldown < chargeDuration)
			{
				float normalizedChargeTime = Mathf.Clamp01(cooldown / chargeDuration);
				return normalizedChargeTime * attackFrameNorm;
			}
			
			float attackCooldown = cooldown - chargeDuration;
			float normalizedTime = Mathf.Clamp01( attackCooldown / attackDuration);
			return attackFrameNorm + normalizedTime*(1f - attackFrameNorm);
		}

		public override void Reset(EntId entityId, ref BehaviourNodeData nodeNodeData)
		{
			base.Reset(entityId, ref nodeNodeData);
			
			EntityViewAtributes viewAtributes = ViewEntitiesContainer.GetEntityViewAtributes(entityId);
			EnemyView enemyView = viewAtributes.Get<EnemyView>();
			
			enemyView.SetAttack(Vector3.zero, false, 0f);
			nodeNodeData.Cooldown = 0f;
		}
	}

		
}
	
	
	
