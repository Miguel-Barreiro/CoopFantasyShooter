using Core.Model;
using Core.Utils;
using Core.VSEngine;
using UnityEngine;
using Zenject;

namespace Game.StatusEffects
{
	public class ApplyStatusEffectNode : BasicFlowNode
	{
		[Input(ShowBackingValue.Never, ConnectionType.Override), SerializeField]
		private EntId Target;

		[SerializeField] private StatusEffectConfig StatusEffectConfig;

		[Inject] private readonly GameLogic GameLogic = null!;

		
		protected override void Action()
		{
			OperationResult<EntId> resTarget = Resolve<EntId>(nameof(Target));
			if (resTarget.IsFailure)
			{
				Debug.LogError($"Failed to resolve Target for {this.name} {name} in graph {graph.name}: {resTarget.Exception}");
				return;
			}
			EntId valueTarget = resTarget.Result;
			EntId newStatusEffect = GameLogic.ApplyStatusEffect(valueTarget, StatusEffectConfig);
			
			
		}
		
		public override OperationResult<object> GetValue(string portName)
		{
			return INVALID_INPUT<object>($"GetValue is not supported for ApplyStatusEffectNode {name} in graph {graph.name}");
		}

		
	}
}