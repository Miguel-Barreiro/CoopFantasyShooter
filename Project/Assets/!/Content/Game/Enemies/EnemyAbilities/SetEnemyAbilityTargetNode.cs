#nullable enable
using System.Collections.Generic;
using Core.Events;
using Core.Model;
using Core.Utils;
using Core.VSEngine;
using Core.VSEngine.Nodes;
using Game.VSNodes;
using UnityEngine;
using Zenject;

namespace Game.Enemies.EnemyAbilities
{
	[CreateNodeMenu( VSGameNodeMenuNames.ENEMY_ABILITY_NODES + "/" + "Set Ability Target", order = VSNodeMenuNames.VERY_IMPORTANT)]
	public sealed class SetEnemyAbilityTargetNode : BaseEnemyAbilityTargetNode
	{
		
		[Input(typeConstraint = TypeConstraint.Strict, 
				connectionType = ConnectionType.Override, 
				backingValue = ShowBackingValue.Never), SerializeField]
		private List<EntId> TargetEntities;

		[Input(typeConstraint = TypeConstraint.Strict, 
				connectionType = ConnectionType.Override, 
				backingValue = ShowBackingValue.Never), SerializeField]
		private List<Vector3> TargetPositions;

		
		protected override void Action()
		{
			BaseEntityEvent? entityEvent = this.ExecutionControl.CoreEntityEvent;
			if (entityEvent == null)
			{
				Debug.LogError($"CoreEntityEvent is null for SetEnemyAbilityTargetNode {name} in graph {graph.name}");
				return;
			}
			
			ref EnemyAbilityData abilityData = ref EnemyAbilityContainer.GetComponent(entityEvent.EntityID);
			
			switch (TargetType)
			{
				case AbilityTargetType.Positions:{
					OperationResult<List<Vector3>> positionsResult = Resolve<List<Vector3>>(nameof(TargetPositions));
					if (positionsResult.IsFailure)
					{
						Debug.LogError($"Failed to resolve TargetPositions for SetEnemyAbilityTargetNode {name} in graph {graph.name}: {positionsResult.Exception}");
						return;
					}

					abilityData.SetTarget(positionsResult.Result);
					break;
				}
				case AbilityTargetType.Entities:{
					OperationResult<List<EntId>> entitiesResult = Resolve<List<EntId>>(nameof(TargetEntities));
					if (entitiesResult.IsFailure)
					{
						Debug.LogError($"Failed to resolve TargetEntities for SetEnemyAbilityTargetNode {name} in graph {graph.name}: {entitiesResult.Exception}");
						return;
					}

					abilityData.SetTarget(entitiesResult.Result);
					break;
				}
				case AbilityTargetType.PositionsAndEntities:{
					OperationResult<List<Vector3>> positionsResult = Resolve<List<Vector3>>(nameof(TargetPositions));
					if (positionsResult.IsFailure)
					{
						Debug.LogError($"Failed to resolve TargetPositions for SetEnemyAbilityTargetNode {name} in graph {graph.name}: {positionsResult.Exception}");
						return;
					}

					OperationResult<List<EntId>> entitiesResult = Resolve<List<EntId>>(nameof(TargetEntities));
					if (entitiesResult.IsFailure)
					{
						Debug.LogError($"Failed to resolve TargetEntities for SetEnemyAbilityTargetNode {name} in graph {graph.name}: {entitiesResult.Exception}");
						return;
					}

					abilityData.SetTarget(entitiesResult.Result, positionsResult.Result);
					break;
				}
			}	
		}
		
		public override OperationResult<object> GetValue(string portName)
		{
			return INVALID_INPUT<object>($"SetEnemyAbilityTargetNode {name} in graph {graph.name} does not provide any output values.");
		}
	}
	
	[NodeTint(VSGameNodeMenuNames.ENEMY_ABILITY_NODES_TINT)]	
	public abstract class BaseEnemyAbilityTargetNode : BasicFlowNode
	{
		[Inject] protected readonly BasicCompContainer<EnemyAbilityData> EnemyAbilityContainer = null!;
		
		[SerializeField] protected AbilityTargetType TargetType = AbilityTargetType.Entities;

		public enum AbilityTargetType
		{
			Positions,
			Entities, 
			PositionsAndEntities
		}
		
	}
}