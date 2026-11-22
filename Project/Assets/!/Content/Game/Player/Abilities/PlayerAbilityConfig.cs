using Game.Logic;
using UnityEngine;

namespace Game.Player.Abilities
{
	public sealed class PlayerAbilityConfig : BaseVSEntityConfig
	{
		[SerializeField] private PlayerAbilityAction.PlayerAbilityType _PlayerAbilityType;
		public PlayerAbilityAction.PlayerAbilityType PlayerAbilityType => _PlayerAbilityType;


		[SerializeField] private PlayerAbilitySlot _InputSlot;
		public PlayerAbilitySlot InputSlot => _InputSlot;
		
		
		
	}
	




}