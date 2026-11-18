using Core.Model;
using Core.Model.ModelSystems;
using Core.Systems;
using Core.View;
using FixedPointy;
using Zenject;

namespace Game.Spells.Mortar
{
	public sealed class MortarViewSystem : UpdateComponents<MortarData>
	{
		[Inject] private readonly BasicCompContainer<MortarData> MortarContainer = null!;
		[Inject] private readonly ViewEntitiesContainer ViewEntitiesContainer = null!;

		
		public void UpdateComponents(float deltaTime)
		{
			uint topIndex = MortarContainer.TopEmptyIndex;
			for (int i = 0; i < topIndex; i++)
			{
				ref MortarData mortarData = ref MortarContainer.Components[i];
				EntId mortarID = mortarData.ID;
				EntityViewAtributes entityViewAtributes = ViewEntitiesContainer.GetEntityViewAtributes(mortarID);
				MortarView mortarView = entityViewAtributes.Get<MortarView>();
				Fix fillProgress = mortarData.ElapsedTime / mortarData.TimeToImpact;
				mortarView.UpdateView(deltaTime, fillProgress, mortarData.Radius);
			}

		}


		public bool Active { get; set; } = true;
		public SystemGroup Group { get; } = GameSystemsGroup.GAME_VIEW_SYSTEM;
	}
	
}