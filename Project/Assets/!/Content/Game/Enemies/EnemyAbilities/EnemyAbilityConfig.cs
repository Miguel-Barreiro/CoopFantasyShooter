using Game.Logic;
using UnityEngine;

namespace Game.Enemies.EnemyAbilities
{
	public sealed class EnemyAbilityConfig : BaseAbilityConfig
	{
		[SerializeField] private GameLogic.AbilityType _abilityType;
		public GameLogic.AbilityType AbilityType => _abilityType;

	}
}