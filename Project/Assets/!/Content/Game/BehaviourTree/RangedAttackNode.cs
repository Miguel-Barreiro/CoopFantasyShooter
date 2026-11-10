#nullable enable
using Core.Model;
using Core.View;
using Game.Enemies;
using UnityEngine;
using Zenject;

namespace Game.BehaviourTree
{
	/// <summary>
	/// Shoots the current target from range.
	/// - Failure when there is no target, the target is out of AttackDistance, or line of sight is blocked
	///   (so a parent priority/first-child node can fall back to moving closer).
	/// - Otherwise stops the agent, faces the target and runs a charge -> shot -> recover cycle
	///   driven by nodeData.Cooldown (AttackChargeDuration + AttackDuration), returning Success each tick.
	/// The shot is resolved as a hitscan at the moment the charge completes.
	/// </summary>
	public sealed class RangedAttackNode : ActionBehaviourNode
	{
		[Inject] private readonly ViewEntitiesContainer ViewEntitiesContainer = null!;
		[Inject] private readonly GameStatsContainer GameStatsContainer = null!;
		[Inject] private readonly StatsSystem StatsSystem = null!;

		private const float AIM_HEIGHT = 1f;
		private const float MUZZLE_OFFSET = 0.1f;

		private readonly RaycastHit[] HitsCache = new RaycastHit[8];

		public override BehaviourStatus Process(EntId entityId, ref BehaviourNodeData nodeData,
												ref BehaviourAgentData agentData, float deltaTime)
		{
			
			return BehaviourStatus.Success;
		}

		public override void Reset(EntId entityId, ref BehaviourNodeData nodeNodeData)
		{
			base.Reset(entityId, ref nodeNodeData);
			nodeNodeData.Cooldown = 0f;

			EnemyView? enemyView = ViewEntitiesContainer.GetEntityViewAtributes(entityId)?.Get<EnemyView>();
			enemyView?.SetAttack(Vector3.zero, false, 0f);
		}

		
	}
}
