using Game.Logic;
using UnityEngine;

namespace Game.Enemies
{
	public sealed class EnemyConfig : BaseEntityConfig
	{
		[SerializeField] private EnemyView _EnemyPrefab;
		public EnemyView EnemyPrefab => _EnemyPrefab;

		
	}
}