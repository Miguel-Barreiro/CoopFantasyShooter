#nullable enable
using Core.Model;
using Core.Utils;
using Core.View;
using Core.VSEngine;
using Core.VSEngine.Nodes;
using Game.Components;
using UnityEngine;
using Zenject;

namespace Game.VSNodes.Values
{
	
	[CreateNodeMenu(VSGameNodeMenuNames.GAME_ENTITIES + "/" + "Get Entity Position", order = VSNodeMenuNames.VERY_IMPORTANT)]
	public sealed class GetEntityPositionNode : ValueOnlyNode
	{
		// [Inject] private readonly ViewEntitiesContainer ViewEntitiesContainer = null!;
		[Inject] private readonly BasicCompContainer<WorldEntityData> WorldEntityContainer = null!;
		
		[Input(ShowBackingValue.Never, ConnectionType.Override), SerializeField]
		private EntId Entity;
		
		[Output(typeConstraint = TypeConstraint.Strict, connectionType = ConnectionType.Override, backingValue = ShowBackingValue.Never), SerializeField]
		private Vector3 Position;
		
		public override OperationResult<object> GetValue(string portName)
		{
			switch (portName)
			{
				case nameof(Position):
					return SUCCESS_RETURN<object>(GetEntityPosition());
				default:
					return INVALID_INPUT<object>($"GetEntityPositionNode {name} in graph {graph.name} does not have a port named {portName}.");
			}
		}

		private Vector3 GetEntityPosition()
		{
			OperationResult<EntId> operationResult = Resolve<EntId>(nameof(Entity));
			if (operationResult.IsFailure)
			{
				Debug.LogError($"Failed to resolve Entity for GetEntityPositionNode {name} in graph {graph.name}: {operationResult.Exception}");
				return Vector3.zero;
			}
			EntId entityId = operationResult.Result;
			if (!entityId.IsValid())
			{
				Debug.LogError($"Invalid EntityId for GetEntityPositionNode {name} in graph {graph.name}");
				return Vector3.zero;
			}
			
			ref WorldEntityData worldEntityData = ref WorldEntityContainer.GetComponent(entityId);

			// EntityViewAtributes? entityViewAtributes = ViewEntitiesContainer.GetEntityViewAtributes(entityId);
			// if(entityViewAtributes == null || entityViewAtributes.GameObject == null)
			// {
			// 	Debug.LogError($"EntityViewAtributes not found for EntityId {entityId} in GetEntityPositionNode {name} in graph {graph.name}");
			// 	return Vector3.zero;
			// }
			
			return worldEntityData.Position;
		}
	}
}