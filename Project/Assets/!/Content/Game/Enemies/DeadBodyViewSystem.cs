using Core.Events;
using Core.Model;
using Core.Model.ModelSystems;
using Core.Systems;
using Core.View;
using Zenject;

namespace Game.Enemies
{
	public sealed class DeadBodyViewSystem : OnCreateComponent<DeadBodyData>
	{
		[Inject] private readonly EntitiesContainer EntitiesContainer = null!;
		[Inject] private readonly BasicCompContainer<DeadBodyData> DeadBodyContainer = null!;
		[Inject] private readonly ViewEntitiesContainer ViewEntitiesContainer = null!;

		
		public void OnCreateComponent(EntId newComponentId)
		{
			EntityEventQueue.AddEntityEventListener<EnemyDeathEvent>(newComponentId, OnEnemyDeath);
		}

		private void OnEnemyDeath(EnemyDeathEvent entityEvent)
		{
			EntityViewAtributes? entityViewAtributes = ViewEntitiesContainer.GetEntityViewAtributes(entityEvent.EntityID);
			EnemyView? enemyView = entityViewAtributes?.Get<EnemyView>();
			if(enemyView != null)
				enemyView.SetDead();

		}

		public bool Active { get; set; } = true;
		public SystemGroup Group { get; } = GameSystemsGroup.GAME_VIEW_SYSTEM;
	}
}