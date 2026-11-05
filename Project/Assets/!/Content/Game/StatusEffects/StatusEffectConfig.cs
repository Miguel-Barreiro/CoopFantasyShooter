using Game.Logic;
using UnityEngine;

namespace Game.StatusEffects
{
	public sealed class StatusEffectConfig : BaseVSEntityConfig
	{
		[SerializeField] private bool  _Overrides;
		public bool Overrides => _Overrides;

		[SerializeField] private uint _OverridePriority;
		public uint OverridePriority => _OverridePriority;

		[SerializeField] private StatusEffectType _statusEffectType;
		public StatusEffectType StatusEffectType => _statusEffectType;

	}
	
}