using Core.Events;
using Core.Model;
using Core.Model.ModelSystems;
using Core.Systems;
using Core.View;
using FixedPointy;
using Game.VFX;
using UnityEngine;
using Zenject;

namespace Game.Spells.Mortar
{
	public sealed class MortarViewSystem : UpdateComponents<MortarData>, OnCreateComponent<MortarData>
	{
		[Inject] private readonly BasicCompContainer<MortarData> MortarContainer = null!;
		[Inject] private readonly ViewEntitiesContainer ViewEntitiesContainer = null!;
		[Inject] private readonly BasicCompContainer<VFXData> VFXContainer = null!;

		
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


		public void OnCreateComponent(EntId newComponentId)
		{
			EntityEventQueue.AddEntityEventListener<OnMortarImpactEvent>(newComponentId, OnMortarImpact);
			EntId mortarID = newComponentId;
			ref MortarData componentData = ref MortarContainer.GetComponent(mortarID);
	
			if(componentData.DataConfig.OriginVFX == null)
				return;

			VFXEntity impactVFX = new VFXEntity(componentData.DataConfig.OriginVFX, 
												componentData.OriginPosition, Quaternion.identity);
			
			ref VFXData vfxData = ref VFXContainer.GetComponent(impactVFX.ID);
			vfxData.Target = componentData.TargetPosition;
			vfxData.Duration = (float) componentData.TimeToImpact;

		}

		private void OnMortarImpact(OnMortarImpactEvent entityEvent)
		{
			EntId mortarID = entityEvent.EntityID;
			ref MortarData componentData = ref MortarContainer.GetComponent(mortarID);
			
			if(componentData.DataConfig.ImpactVFX == null)
				return;
			
			VFXEntity impactVFX = new VFXEntity(componentData.DataConfig.ImpactVFX, 
												componentData.TargetPosition, Quaternion.identity);
			
		}

		public bool Active { get; set; } = true;
		public SystemGroup Group { get; } = GameSystemsGroup.GAME_VIEW_SYSTEM;
	}
	
}