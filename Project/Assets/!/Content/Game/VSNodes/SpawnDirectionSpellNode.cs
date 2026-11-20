using System.Collections.Generic;
using Core.Model;
using Core.Model.Stats;
using Core.Utils;
using Core.VSEngine;
using Core.VSEngine.Nodes;
using Game.Spells;
using UnityEngine;
using UnityEngine.Serialization;
using Zenject;

namespace Game.VSNodes
{
	
	[CreateNodeMenu(VSGameNodeMenuNames.SPELLS + "/" + "Spawn Direction Spell", order = VSNodeMenuNames.VERY_IMPORTANT)]
	[NodeWidth(300)]
	public sealed class SpawnDirectionSpellNode : BasicFlowNode
	{
		[Inject] private readonly GameLogic GameLogic = null!;


		[SerializeField] private List<StatOverride> StatsOverrides;

		
		[SerializeField] private SpellConfig _config;
		public SpellConfig config => _config;
		
		[Input(ShowBackingValue.Never, ConnectionType.Override), SerializeField]
		private Vector3 Direction;

		[Input(ShowBackingValue.Never, ConnectionType.Override), SerializeField]
		private Vector3 OriginPosition;

		
		[Input(ShowBackingValue.Never, ConnectionType.Override), SerializeField] 
		private EntId AbilityOwner;
		
		protected override void Action()
		{
			OperationResult<EntId> operationResult = Resolve<EntId>(nameof(AbilityOwner));
			if (Check(!operationResult.IsSuccess, $"Failed to resolve Owner for SpawnSpellNode {name} in graph {graph.name}:"))
				return;
			
			OperationResult<Vector3> directionResult = Resolve<Vector3>(nameof(Direction));
			if (Check(!directionResult.IsSuccess, $"Failed to resolve Direction for SpawnSpellNode {name} in graph {graph.name}:"))
				return;
			
			OperationResult<Vector3> positionResult = Resolve<Vector3>(nameof(OriginPosition));
			if (Check(!positionResult.IsSuccess, $"Failed to resolve Position for SpawnSpellNode {name} in graph {graph.name}:"))
				return;
			
			var direction = directionResult.Result;
			var position = positionResult.Result;
			EntId ownerId = operationResult.Result;

			EntId newSpellID = GameLogic.SpawnDirectionSpell(ownerId, config, direction, position);
			GameLogic.OverrideDefaultStats(newSpellID, StatsOverrides);
		}
		public override OperationResult<object> GetValue(string portName)
		{
			return INVALID_INPUT<object>(portName);
		}
	}
}