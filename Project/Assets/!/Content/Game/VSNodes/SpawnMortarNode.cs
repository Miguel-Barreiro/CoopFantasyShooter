using Core.Model;
using Core.Utils;
using Core.VSEngine;
using Core.VSEngine.Nodes;
using Game.Spells;
using UnityEngine;
using XNode;
using Zenject;

namespace Game.VSNodes
{
	[Node.CreateNodeMenu(VSGameNodeMenuNames.SPELLS + "/" + "Spawn Mortar Spell", order = VSNodeMenuNames.VERY_IMPORTANT)]
	public sealed class SpawnMortarNode : BasicFlowNode
	{
		[Inject] private readonly GameLogic GameLogic = null!;

		
		[SerializeField] private SpellConfig _config;
		public SpellConfig config => _config;
		

		[Input(ShowBackingValue.Never, ConnectionType.Override), SerializeField]
		private Vector3 OriginPosition;

		[Input(ShowBackingValue.Never, ConnectionType.Override), SerializeField]
		private Vector3 TargetPosition;
		
		[Input(ShowBackingValue.Never, ConnectionType.Override), SerializeField] 
		private EntId Owner;
		
		protected override void Action()
		{
			OperationResult<EntId> operationResult = Resolve<EntId>(nameof(Owner));
			if (Check(!operationResult.IsSuccess, $"Failed to resolve Owner for SpawnSpellNode {name} in graph {graph.name}:"))
				return;
			
			
			OperationResult<Vector3> positionResult = Resolve<Vector3>(nameof(OriginPosition));
			if (Check(!positionResult.IsSuccess, $"Failed to resolve Position for SpawnSpellNode {name} in graph {graph.name}:"))
				return;
			
			OperationResult<Vector3> targetPositionResult = Resolve<Vector3>(nameof(TargetPosition));
			if (Check(!targetPositionResult.IsSuccess, $"Failed to resolve TargetPosition for SpawnSpellNode {name} in graph {graph.name}:"))
				return;
			
			var position = positionResult.Result;
			var targetPosition = targetPositionResult.Result;
			EntId ownerId = operationResult.Result;
			
			GameLogic.SpawnPositionSpell(ownerId, config, position, targetPosition);
		}
		public override OperationResult<object> GetValue(string portName)
		{
			return INVALID_INPUT<object>(portName);
			
		}
	}}