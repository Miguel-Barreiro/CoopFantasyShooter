using System.Collections.Generic;
using Core.Events;
using Core.Initialization;
using Core.Model;
using Cysharp.Threading.Tasks;
using Game.Enemies;
using Game.Player;
using Game.Spells;
using Game.Spells.Bullet;
using Global;
using Zenject;

namespace Game.Events
{
	public sealed class GoFromGameToMainEvent : Event<GoFromGameToMainEvent>
	{
		[Inject] private readonly ScenesController ScenesController = null!;
		[Inject] private readonly EntitiesContainer EntitiesContainer = null!;

		
		public override void Execute()
		{
			DestroyAll<IEnemy>();
			DestroyAll<IPlayer>();
			DestroyAll<IBullet>();
			DestroyAll<IAbility>();
			DestroyAll<Logic.IGame>();
			
			SwitchMainScene().Forget();
		}
		
		private async UniTask SwitchMainScene()
		{
			await UniTask.DelayFrame(2);
			await ScenesController.SwitchScene(SceneNames.MAIN_MENU);
		}
		
		
		private void DestroyAll<T>() 
			where T : class, IEntity
		{
			IEnumerable<T> entities = EntitiesContainer.GetAllEntitiesByType<T>();
			foreach (T entity in entities)
				EntitiesContainer.DestroyEntity(entity.ID);
		}
	}
}