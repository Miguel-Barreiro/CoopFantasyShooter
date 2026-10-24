using Core.Model;
using Core.Systems;
using Global;
using UnityEngine;
using Zenject;

namespace Game.Logic
{
	public sealed class GameSetupSystem : IStartSystem
	{

		[Inject] private readonly StatsSystem StatsSystem = null!;
		[Inject] private readonly GameStatsContainer GameStatsContainer = null!;


		[Inject] private readonly GameplayConfig GameplayConfig = null!;

		public void StartSystem()
		{
			Random.InitState(Mathf.CeilToInt(Time.time * 100f));
			
			GameEntity gameEntity = new GameEntity();
			StatsSystem.SetBaseValue(gameEntity.ID, 
									GameStatsContainer.Cooldown, 
									GameplayConfig.GamelogicAttributes.EnemySpawnCooldown);
			
			StatsSystem.SetDepletedValue(gameEntity.ID, 
									GameStatsContainer.Cooldown, 
									3f);

			Debug.Log("GAME START");
		}
	}
}