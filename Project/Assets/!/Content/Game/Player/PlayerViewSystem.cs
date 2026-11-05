using Core.Model;
using Core.Model.ModelSystems;
using Core.Systems;
using Core.View;
using Zenject;

namespace Game.Player
{
	public sealed class PlayerViewSystem : UpdateComponents<PlayerData>
	{
		[Inject] private readonly PlayerContainer PlayerContainer = null!;
		[Inject] private readonly ViewEntitiesContainer ViewEntitiesContainer = null!;

		
		public void UpdateComponents(float deltaTime)
		{

			uint topIndex = PlayerContainer.TopEmptyIndex;
			for (int i = 0; i < topIndex; i++)
			{
				ref PlayerData componentData = ref PlayerContainer.Components[i];
				EntityViewAtributes entityViewAtributes = ViewEntitiesContainer.GetEntityViewAtributes(componentData.ID);
				PlayerView playerView = entityViewAtributes.Get<PlayerView>();
				if(playerView != null)
					playerView.UpdateView(deltaTime, ref componentData);
			}		


		}
		public bool Active { get; set; } = true;
		public SystemGroup Group { get; } = GameSystemsGroup.GAME_VIEW_SYSTEM;
	}
}