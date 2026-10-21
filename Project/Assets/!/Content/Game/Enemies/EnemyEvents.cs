using Core.Events;
using Core.Model;
using Core.View;
using Game.Events;
using Game.Logic;
using Game.Player;
using Global;
using UnityEngine;
using Zenject;

namespace Game.Enemies
{
	
	public sealed class SpawnEnemyEvent : GameEvent<SpawnEnemyEvent>
	{
		[Inject] private readonly GameLogic GameLogic = null!;
		[Inject] private readonly GameplayConfig GameplayConfig = null!;


		public Vector2 Position;
		public EnemyConfig EnemyConfig;
		
		public override void Execute()
		{
			GameLogic.SpawnEnemy(Position, EnemyConfig);
			GameLogic.SpawnEnemy(Position + Vector2.right * 0.4f, EnemyConfig);
			GameLogic.SpawnEnemy(Position + Vector2.one * 0.4f , EnemyConfig);
		}
	}
	
	public sealed class EnemyAttackEvent : EntityEvent<EnemyAttackEvent>
	{
		[Inject] private readonly PlayerContainer PlayerContainer = null!;
		[Inject] private readonly BasicCompContainer<EnemyData> EnemyContainer = null!;
		[Inject] private readonly ViewEntitiesContainer ViewEntitiesContainer = null!;

		[Inject] private readonly StatsSystem StatsSystem = null!;
		[Inject] private readonly GameStatsContainer GameStatsContainer = null!;

		
		private readonly Collider[] ResultsCache = new Collider[10];
		
		public override void Execute()
		{
			EntityViewAtributes enemyViewAttributes = ViewEntitiesContainer.GetEntityViewAtributes(EntityID);
			EnemyView enemyView = enemyViewAttributes!.Get<EnemyView>();
			
			Vector3 direction = enemyView.transform.forward;
			direction.y = 0f;
			
			Vector3 position = enemyView.transform.position + direction * 0.3f;
			int size = Physics.OverlapSphereNonAlloc(position, 0.5f, ResultsCache);
			if(size > 0)
			{
				for (int j = 0; j < size; j++)
				{
					Collider collider = ResultsCache[j];
						
					PlayerView playerView = collider.gameObject.GetComponent<PlayerView>();
					if (playerView != null)
					{
						HitPlayerEvent hitPlayerEvent = EntityEventQueue.Execute<HitPlayerEvent>(playerView.EntityID);
						hitPlayerEvent.Damage = StatsSystem.GetStatDepletedValue(EntityID, GameStatsContainer.Damage);
					}

				}
			}

		}
	}
}