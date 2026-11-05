using Core.Model;
using Core.Model.ModelSystems;
using Core.Systems;
using Core.View;
using Zenject;

namespace Game.Enemies
{
	public sealed class EnemyViewSystem : UpdateComponents<EnemyData>
	{
		[Inject] private readonly BasicCompContainer<EnemyData> EnemyContainer = null!;
		[Inject] private readonly ViewEntitiesContainer ViewEntitiesContainer = null!;

		public void UpdateComponents(float deltaTime)
		{

			uint topIndex = EnemyContainer.TopEmptyIndex;
			for (int i = 0; i < topIndex; i++)
			{
				ref EnemyData componentData = ref EnemyContainer.Components[i];
				EntId enemyID = componentData.ID;
				EntityViewAtributes entityViewAtributes = ViewEntitiesContainer.GetEntityViewAtributes(enemyID);
				EnemyView enemyView = entityViewAtributes!.Get<EnemyView>();
				if(enemyView != null)
					enemyView.UpdateView(deltaTime, ref componentData);
			}

		}

		public bool Active { get; set; } = true;
		public SystemGroup Group { get; } = GameSystemsGroup.GAME_VIEW_SYSTEM;
	}
}