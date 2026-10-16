using System;
using Core.Events;
using Core.Model;
using Core.Model.ModelSystems;
using Core.Systems;
using Core.Utils;
using Core.View;
using FixedPointy;
using Game.Enemies;
using Game.Player;
using Global;
using UnityEngine;
using Zenject;
using Random = UnityEngine.Random;

namespace Game.Logic
{
	public sealed class EnemySpawnerSystem : UpdateComponents<GameData>
	{
		[Inject] private readonly GameplayConfig GameplayConfig = null!;
		[Inject] private readonly BasicCompContainer<GameData> GameContainer = null!;
		[Inject] private readonly StatsSystem StatsSystem = null!;
		[Inject] private readonly GameStatsContainer GameStatsContainer = null!;

		[Inject] private readonly ViewEntitiesContainer ViewEntitiesContainer = null!;


		[Inject] private readonly PlayerContainer PlayerContainer = null!;
		[Inject] private readonly IRandomSystem RandomSystem = null!;



		public void UpdateComponents(float deltaTime)
		{

			uint topIndex = GameContainer.TopEmptyIndex;
			for (int i = 0; i < topIndex; i++)
			{
				ref GameData componentData = ref GameContainer.Components[i];
				EntId gameID = componentData.ID;

				Fix cooldown = StatsSystem.ChangeDepletedValue(gameID, GameStatsContainer.Cooldown, -deltaTime);
				if(cooldown == 0)
				{
					CalculatePlayerParameters(out FixVec2 averagePositionVec);

					Vector2 averagePosition =  new Vector2((float) averagePositionVec.X, (float) averagePositionVec.Y);
					
					int range = (int) RandomSystem.Range(-180, 180);
					double x = Math.Cos(range * Mathf.Deg2Rad);
					double y = Math.Sin(range * Mathf.Deg2Rad);
					Vector2 spawnDirection = new Vector2((float)x, (float)y).normalized;


					Vector2 deltaSpawn = spawnDirection * GameplayConfig.GamelogicAttributes.EnemySpawnDistance;
					Vector2 spawnLocation = averagePosition + deltaSpawn;

					SpawnEnemyEvent spawnEnemyEvent = EntityEventQueue.Execute<SpawnEnemyEvent>(gameID);
					spawnEnemyEvent.Position = spawnLocation;
					spawnEnemyEvent.EnemyConfig = GameplayConfig.GamelogicAttributes.LowLevelEnemyConfig;

					
					StatsSystem.ResetDepletedValueToMax(gameID, GameStatsContainer.Cooldown);
				}
				
				
			}


			void CalculatePlayerParameters(out FixVec2 averagePosition)
			{
				averagePosition = FixVec2.Zero;
				
				uint topPlayerIndex = PlayerContainer.TopEmptyIndex;
				for (int i = 0; i < topPlayerIndex; i++)
				{
					EntId entId = PlayerContainer.Components[i].ID;
					EntityViewAtributes? viewAtributes = ViewEntitiesContainer.GetEntityViewAtributes(entId);
					Vector3 playerWorldPosition = viewAtributes!.GameObject!.transform.position;
					FixVec2 pos = new FixVec2(playerWorldPosition.x, playerWorldPosition.z);
					averagePosition += pos;
				}
 
				if (topPlayerIndex > 0)
				{
					averagePosition /= topPlayerIndex;
				}
			}
		}
		
		
		public bool Active { get; set; } = true;
		public SystemGroup Group { get; } = GameSystemsGroup.GAME_LOGIC_SYSTEM;
	}
}