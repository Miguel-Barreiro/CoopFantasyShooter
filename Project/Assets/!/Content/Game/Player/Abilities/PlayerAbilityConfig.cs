using System.Collections.Generic;
using Game.Logic;
using UnityEngine;

namespace Game.Player.Abilities
{
	public sealed class PlayerAbilityConfig : BaseVSEntityConfig
	{
		[SerializeField] private PlayerAbilityAction.AbilityType _abilityType;
		public PlayerAbilityAction.AbilityType AbilityType => _abilityType;


		[SerializeField] private PlayerAbilitySlot _InputSlot;
		public PlayerAbilitySlot InputSlot => _InputSlot;
		
	}
	




}