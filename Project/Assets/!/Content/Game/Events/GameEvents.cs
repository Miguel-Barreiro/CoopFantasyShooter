using System.Collections.Generic;
using Core.Events;
using Core.Initialization;
using Core.Model;
using Core.Systems;
using Core.Utils.CachedDataStructures;
using Core.View;
using Cysharp.Threading.Tasks;
using Game.Enemies;
using Game.Logic;
using Game.Player;
using Game.Spells;
using Game.Spells.Bullet;
using Game.StatusEffects;
using Global;
using UnityEngine;
using Zenject;

namespace Game.Events
{
	public sealed class GoFromGameToMainEvent : Event<GoFromGameToMainEvent>
	{
		[Inject] private readonly ScenesController ScenesController = null!;
		[Inject] private readonly EntitiesContainer EntitiesContainer = null!;
		[Inject] private readonly SystemsController SystemsController = null!;
		// [Inject] private readonly ViewEntitiesContainer ViewEntitiesContainer = null!;
		[Inject] private readonly GenericGameObjectPool GenericGameObjectPool = null!;




		
		public override void Execute()
		{
			if(ScenesController.IsSwitchingScene)
				return;
			
			DestroyAll<ISimulation>();
			
			SwitchMainScene().Forget();
		}
		
		private async UniTask SwitchMainScene()
		{

			
			await UniTask.DelayFrame(2);
			GenericGameObjectPool.ClearAll();
			
			// SystemsController.PauseLoop();

			Debug.Log($"LOAD MAIN MENU SCENE");
				
			await UniTask.Delay(200, true);
			await UniTask.DelayFrame(1);
			
			await ScenesController.SwitchScene(SceneNames.MAIN_MENU);
		}
		
		
		private void DestroyAll<T>() 
			where T : class, IEntity
		{
			IEnumerable<T> entities = EntitiesContainer.GetAllEntitiesByType<T>();
			using CachedList<T> entityList = ListCache<T>.Get(entities);
			
			foreach (T entity in entityList)
				EntitiesContainer.DestroyEntity(entity.ID);
		}
	}
}