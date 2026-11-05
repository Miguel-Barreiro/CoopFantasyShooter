#nullable enable
using System.Collections.Generic;
using Core.Model;
using Core.Utils;
using Core.View;
using Core.VSEngine.Nodes;
using FixedPointy;
using Game.Components;
using Game.Enemies;
using Game.Enemies.EnemyAbilities;
using Game.Player;
using UnityEngine;
using UnityEngine.Serialization;
using Zenject;

namespace Game.VSNodes.Search
{
	
	[CreateNodeMenu( VSGameNodeMenuNames.SEARCH + "/" + "By distance", order = VSNodeMenuNames.VERY_IMPORTANT)]
	public sealed class SearchEntitiesByDistanceNode : BaseSearchEntitiesNode
	{

		[Inject] private readonly BasicCompContainer<EnemyAbilityData> EnemyAbilityContainer = null!;

		[Inject] private readonly ViewEntitiesContainer ViewEntitiesContainer = null!;
		[Inject] private readonly BasicCompContainer<EnemyData> EnemyContainer = null!;
		[Inject] private readonly PlayerContainer PlayerContainer = null!;

		[Inject] private readonly BasicCompContainer<WorldEntityData> WorldEntityContainer = null!;
		
		[SerializeField] private SearchEntityType SearchEntityType = SearchEntityType.Enemy;
		
		[Input(typeConstraint = TypeConstraint.Strict, 
				connectionType = ConnectionType.Override, 
				backingValue = ShowBackingValue.Never), SerializeField]
		private Vector3 SearchPosition = Vector3.zero;
		
		[Input(ShowBackingValue.Never, ConnectionType.Override), SerializeField]
		private Fix MaxDistance = 1;

		[Input(ShowBackingValue.Never, ConnectionType.Override), SerializeField]
		private Fix MinDistance = 1;
		
		[Input(ShowBackingValue.Never, ConnectionType.Override), SerializeField]
		private Fix SearchNumber = 1;
		
		
		
		protected override bool SearchEntities(List<EntId> resultEntities)
		{
			OperationResult<Vector3> posResult = Resolve<Vector3>(nameof(SearchPosition));
			if (posResult.IsFailure)
			{
				Debug.LogError($"Failed to resolve SearchPosition for SearchEntitiesByDistanceNode {name} in graph {graph.name}: {posResult.Exception}");
				return false;
			}
			
			OperationResult<Fix> resMaxDistance = Resolve<Fix>(nameof(MaxDistance));
			if (resMaxDistance.IsFailure)
			{
				Debug.LogError($"Failed to resolve MaxDistance for {this.name} {name} in graph {graph.name}: {resMaxDistance.Exception}");
				return false;
			}
			Fix valueMaxDistance = resMaxDistance.Result;
			Fix valueMinDistance;
			
			OperationResult<Fix> resMinDistance = Resolve<Fix>(nameof(MinDistance));
			if (resMinDistance.IsFailure)
			{
				// Debug.LogError($"Failed to resolve MinDistance for {this.name} {name} in graph {graph.name}: {resMinDistance.Exception}");
				valueMinDistance = 0f;
			} else
			{
				valueMinDistance = resMinDistance.Result;
			}

			
			switch (SearchEntityType)
			{
				case SearchEntityType.Enemy:
					SearchEnemies(resultEntities, posResult, valueMinDistance, valueMaxDistance);
					break;
				case SearchEntityType.Player:
					SearchPlayers(resultEntities, posResult, valueMinDistance, valueMaxDistance);
					break;
				default:
					Debug.LogError($"Unsupported EntityType {SearchEntityType} for SearchEntitiesByDistanceNode {name} in graph {graph.name}");
					break;
			}

			return true;
		}

		private void SearchPlayers(List<EntId> resultEntities, OperationResult<Vector3> posResult, 
									Fix valueMinDistance, Fix valueMaxDistance)
		{
			
			uint topIndex = PlayerContainer.TopEmptyIndex;
			for (int i = 0; i < topIndex && resultEntities.Count < SearchNumber; i++)
			{
				ref PlayerData playerData = ref PlayerContainer.Components[i];
				ref WorldEntityData worldEntityData = ref WorldEntityContainer.GetComponent(playerData.ID);
				
				float distance = Vector3.Distance(worldEntityData.Position, posResult.Result);
				if (distance <= valueMaxDistance && distance >= valueMinDistance)
					resultEntities.Add(playerData.ID);
			}
		}

		private void SearchEnemies(List<EntId> resultEntities, OperationResult<Vector3> posResult, 
									Fix valueMinDistance, Fix valueMaxDistance)
		{
			
			uint topIndex = EnemyContainer.TopEmptyIndex;
			for (int i = 0; i < topIndex && resultEntities.Count < SearchNumber; i++)
			{
				ref EnemyData enemyData = ref EnemyContainer.Components[i];
				ref WorldEntityData worldEntityData = ref WorldEntityContainer.GetComponent(enemyData.ID);

				float distance = Vector3.Distance(worldEntityData.Position, posResult.Result);
				if (distance <= valueMaxDistance && distance >= valueMinDistance)
					resultEntities.Add(enemyData.ID);
			}
		}
	}
}