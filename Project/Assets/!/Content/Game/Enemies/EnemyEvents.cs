using Core.Events;
using Core.Model;
using Core.View;
using Game.Enemies.EnenmySpawners;
using Game.Events;
using Game.Logic;
using Game.Player;
using Global;
using UnityEngine;
using UnityEngine.AI;
using Zenject;

namespace Game.Enemies
{
	
	public sealed class SpawnEnemyEvent : GameEvent<SpawnEnemyEvent>
	{
		[Inject] private readonly GameLogic GameLogic = null!;
		[Inject] private readonly GameplayConfig GameplayConfig = null!;


		public Vector2 Position;
		public EnemySpawnerConfig EnemySpawnerConfig;
		
		private readonly Vector2[] SpawnOffsets = new Vector2[]
		{
			Vector2.zero,
			Vector2.right * 0.4f,
			Vector2.one * 0.4f, 
			Vector2.left * 0.4f,
			Vector2.up * 0.4f,
			Vector2.down * 0.4f,
			-Vector2.one * 0.4f, 
		};
		
		public override void Execute()
		{
			uint spawnOffsetIndex = 0;
			foreach (EnemyConfig enemyConfig in EnemySpawnerConfig.Enemies)
			{
				Vector2 spawnPosition = Position + SpawnOffsets[spawnOffsetIndex % (uint)SpawnOffsets.Length];
				GameLogic.SpawnEnemy(spawnPosition, enemyConfig);

				spawnOffsetIndex++;
			}
		}
	}
	
	public sealed class EnemyAttackEvent : EntityEvent<EnemyAttackEvent>
	{
		[Inject] private readonly PlayerContainer PlayerContainer = null!;
		[Inject] private readonly BasicCompContainer<EnemyData> EnemyContainer = null!;
		[Inject] private readonly ViewEntitiesContainer ViewEntitiesContainer = null!;

		[Inject] private readonly StatsSystem StatsSystem = null!;
		[Inject] private readonly GameStatsContainer GameStatsContainer = null!;

		
		private readonly Collider[] ResultsCache = new Collider[15];
		
		public override void Execute()
		{
			EntityViewAtributes enemyViewAttributes = ViewEntitiesContainer.GetEntityViewAtributes(EntityID);
			EnemyView enemyView = enemyViewAttributes!.Get<EnemyView>();
			NavMeshAgent navMeshAgent = enemyViewAttributes.Get<NavMeshAgent>();
			float attackDistance = (float) StatsSystem.GetStatDepletedValue(EntityID, GameStatsContainer.AttackDistance);
			Vector3 direction = enemyView.transform.forward;
			direction.y = 0f;
			
			Vector3 position = enemyView.transform.position + direction * (0.1f + navMeshAgent.radius);
			int size = Physics.OverlapSphereNonAlloc(position, attackDistance, ResultsCache);
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