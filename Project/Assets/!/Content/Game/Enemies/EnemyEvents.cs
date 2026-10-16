using Core.Events;
using Game.Logic;
using UnityEngine;
using Zenject;

namespace Game.Enemies
{
	
	public class SpawnEnemyEvent : GameEvent<SpawnEnemyEvent>
	{
		[Inject] private readonly GameLogic GameLogic = null!;

		public Vector2 Position;
		public EnemyConfig EnemyConfig;
		
		public override void Execute()
		{
			GameLogic.SpawnEnemy(Position, EnemyConfig);
		}
	}
}