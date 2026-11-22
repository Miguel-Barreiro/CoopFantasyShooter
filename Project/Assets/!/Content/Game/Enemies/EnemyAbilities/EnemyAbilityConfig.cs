using Game.Logic;
using Game.Player.Abilities;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Enemies.EnemyAbilities
{
	public sealed class EnemyAbilityConfig : BaseAbilityConfig
	{
		[SerializeField] private EnemyAbilityType _AbilityType;
		public EnemyAbilityType AbilityType => _AbilityType;

	}
}