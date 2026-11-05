#nullable enable
using System.Collections;
using System.Collections.Generic;
using Core.Model;
using Core.Utils;
using Core.Utils.CachedDataStructures;
using Core.VSEngine;
using Core.VSEngine.Nodes;
using Game.Enemies;
using Game.Enemies.EnemyAbilities;
using Game.Logic;
using Game.Player;
using Game.Spells;
using UnityEngine;
using UnityEngine.Serialization;
using XNode;
using Zenject;

namespace Game.VSNodes.Search
{

	[CreateNodeMenu(VSGameNodeMenuNames.SEARCH + "/" + "Search Entities", order = VSNodeMenuNames.VERY_IMPORTANT)]
	public class SearchEntitiesNode : BaseSearchEntitiesNode
	{
		[Inject] private readonly PlayerContainer PlayerContainer = null!;
		[Inject] private readonly BasicCompContainer<EnemyData> EnemyContainer = null!;
		[Inject] private readonly BasicCompContainer<EnemyAbilityData> EnemyAbilityContainer = null!;
		[Inject] private readonly BasicCompContainer<PlayerAbilityData> PlayerAbilityContainer = null!;

		
		[FormerlySerializedAs("EntityType")] [SerializeField] private SearchEntityType SearchEntityType = SearchEntityType.Enemy;

		[Input(Node.ShowBackingValue.Never, Node.ConnectionType.Override, TypeConstraint.Strict, connectionType = Node.ConnectionType.Override), SerializeField]
		private bool Condition;
		
		[Node.Output(typeConstraint = TypeConstraint.Strict, connectionType = Node.ConnectionType.Override, backingValue = Node.ShowBackingValue.Never), SerializeField]
		private EntId CurrentEntity;

		
		private const string CURRENT_ENTITY_VAR = "CurrentEntityLoopVar";

		protected override bool SearchEntities(List<EntId> resultEntities)
		{
			using CachedList<EntId> allEntities = ListCache<EntId>.Get();
			switch (SearchEntityType)
			{
				case SearchEntityType.Player:
					GetPlayerEntities(allEntities);
					break;
				case SearchEntityType.Enemy:
					GetEnemyEntities(allEntities);
					break;
				default:
					return false;
			}

			foreach (EntId entity in allEntities)
			{
				SetLocalVariable(CURRENT_ENTITY_VAR, entity);
				OperationResult<bool> conditionResult = Resolve<bool>(nameof(Condition));
				if(conditionResult.IsFailure)
				{
					Debug.LogError($"Failed to resolve Condition for SearchEntitiesNode {name} in graph {graph.name}: {conditionResult.Exception}");
					return false;
				}

				if (conditionResult.Result)
					resultEntities.Add(entity);
			}
			
			return true;
			
			void GetEnemyEntities(List<EntId> entities)
			{
			
			}

			void GetPlayerEntities(List<EntId> entities)
			{
				
			
			}
		}

		public new OperationResult<object> GetValue(string portName)
		{
			switch (portName)
			{
				case nameof(CurrentEntity):{
					OperationResult<object?> operationResult = GetLocalVariable(CURRENT_ENTITY_VAR);
					if(operationResult.IsFailure)
						return INVALID_EXECUTION_MESSAGE<object>($"Failed to get local variable {CURRENT_ENTITY_VAR}: {operationResult.Exception}");
					EntId currentEntityID = (EntId) operationResult.Result!;
					return SUCCESS_RETURN<object>(currentEntityID);
				}
				default:
					return base.GetValue(portName);
			}
			
		}
		
	}

	
	
	[NodeWidth(300)]
	public abstract class BaseSearchEntitiesNode : ValueOnlyNode
	{
		
		[Output(typeConstraint = TypeConstraint.Strict, connectionType = ConnectionType.Override, backingValue = ShowBackingValue.Never), SerializeField]
		private List<EntId> ResultEntities;

		abstract protected bool SearchEntities(List<EntId> resultEntities);

		[Inject] private readonly GameMemoryArena GameMemoryArena = null!;

		
		private const string SEARCH_ENTITIES_RESULT_VAR = "SearchEntitiesResult";

		public override OperationResult<object> GetValue(string portName)
		{
			switch (portName)
			{
				case nameof(ResultEntities):{
					return GetResultEntities();
				}
				default:
					return INVALID_INPUT<object>($"SearchEntitiesNode {name} in graph {graph.name} does not have a port named {portName}.");
			}
			
		}
		
		private OperationResult<object> GetResultEntities()
		{
			if (TryCache(out object? entities))
				return SUCCESS_RETURN<object>(entities);
			
			List<EntId> resultEntities = GameMemoryArena.GetEntIdList();
			if(!SearchEntities(resultEntities))
				return INVALID_EXECUTION_MESSAGE<object>($"SearchEntitiesNode {name} in graph {graph.name} failed to search entities.");

			CacheResult(resultEntities);
			
			return SUCCESS_RETURN<object>(resultEntities);
		}

	}
	
	
}