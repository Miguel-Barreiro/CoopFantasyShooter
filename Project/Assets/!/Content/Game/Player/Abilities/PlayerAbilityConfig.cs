using System.Collections.Generic;
using Game.Logic;
using UnityEngine;

namespace Game.Player.Abilities
{
	public sealed class PlayerAbilityConfig : BaseVSEntityConfig
	{
		[SerializeField] private GameLogic.AbilityType _abilityType;
		public GameLogic.AbilityType AbilityType => _abilityType;


		[SerializeField] private PlayerAbilitySlot _InputSlot;
		public PlayerAbilitySlot InputSlot => _InputSlot;
		
	}
	




}