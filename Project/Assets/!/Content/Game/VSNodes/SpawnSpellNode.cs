using Core.Model;
using Core.Utils;
using Core.VSEngine;
using Core.VSEngine.Nodes;
using Game.Spells;
using UnityEngine;
using Zenject;

namespace Game.VSNodes
{
	
	[CreateNodeMenu(VSGameNodeMenuNames.GAME + "/" + "Spawn Spell", order = VSNodeMenuNames.VERY_IMPORTANT)]
	public sealed class SpawnSpellNode : BasicFlowNode
	{
		[Inject] private readonly GameLogic GameLogic = null!;

		
		[SerializeField] private SpellConfig _config;
		public SpellConfig config => _config;
		
		[Input(ShowBackingValue.Never, ConnectionType.Override), SerializeField]
		private Vector3 Direction;

		[Input(ShowBackingValue.Never, ConnectionType.Override), SerializeField]
		private Vector3 Position;

		
		[Input(ShowBackingValue.Never, ConnectionType.Override), SerializeField] 
		private EntId Owner;
		
		protected override void Action()
		{
			
			EntId ownerId = EntId.Invalid;
			OperationResult<EntId> operationResult = Resolve<EntId>(nameof(Owner));
			if (Check(operationResult.IsSuccess, $"Failed to resolve Owner for SpawnSpellNode {name} in graph {graph.name}:"))
				 ownerId = operationResult.Result;

			OperationResult<Vector3> directionResult = Resolve<Vector3>(nameof(Direction));
			if (Check(directionResult.IsSuccess, $"Failed to resolve Direction for SpawnSpellNode {name} in graph {graph.name}:"))
				Direction = directionResult.Result;
			
			OperationResult<Vector3> positionResult = Resolve<Vector3>(nameof(Position));
			if (Check(positionResult.IsSuccess, $"Failed to resolve Position for SpawnSpellNode {name} in graph {graph.name}:"))
				Position = positionResult.Result;
			
			GameLogic.SpawnSpell(ownerId, config, Direction, Position);
		}
		public override OperationResult<object> GetValue(string portName)
		{
			return INVALID_INPUT<object>(portName);
		}
	}
}