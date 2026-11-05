#nullable enable
using Core.Model;
using Core.Model.ModelSystems;
using Core.Systems;
using Core.View;
using FixedPointy;
using Zenject;

namespace Game.StatusEffects
{
	
	
	[UpdateComponentProperties(Priority = SystemPriority.Late)]
	public sealed class StatusEffectViewSystem : UpdateComponents<StatusEffectData>, 
												OnCreateComponent<StatusEffectData>, 
												OnDestroyComponent<StatusEffectData>
	{
		[Inject] private readonly BasicCompContainer<StatusEffectOwnerData> StatusEffectOwnerContainer = null!;
		[Inject] private readonly BasicCompContainer<StatusEffectData> StatusEffectContainer = null!;
		[Inject] private readonly ViewEntitiesContainer ViewEntitiesContainer = null!;
		[Inject] private readonly GameStatsContainer GameStatsContainer = null!;
		[Inject] private readonly StatsSystem StatsSystem = null!;


		
		public void UpdateComponents(float deltaTime)
		{
			// uint topIndex = StatusEffectContainer.TopEmptyIndex;
			// for (int i = 0; i < topIndex; i++)
			// {
			// 	ref StatusEffectData componentData = ref StatusEffectContainer.Components[i];
			// 	EntId statusEffectID = componentData.ID;
			//
			// 	Fix statDepletedValue = StatsSystem.ChangeDepletedValue(componentData.ID, GameStatsContainer.TimeToLive, -deltaTime);
			// 	if(statDepletedValue <= 0f)
			// 		EntitiesContainer.DestroyEntity(statusEffectID);
			// }
		}

		public void OnCreateComponent(EntId newComponentId)
		{
			ref StatusEffectData effectData = ref StatusEffectContainer.GetComponent(newComponentId);
			ref StatusEffectOwnerData ownerData = ref StatusEffectOwnerContainer.GetComponent(effectData.Owner);

			EntityViewAtributes? entityViewAtributes = ViewEntitiesContainer.GetEntityViewAtributes(ownerData.ID);
			if(entityViewAtributes != null && entityViewAtributes.GameObject != null)
			{
				IStatusEffectOwnerView? statusEffectOwnerView = entityViewAtributes.Get<IStatusEffectOwnerView>();
				if(statusEffectOwnerView != null)
				{
					statusEffectOwnerView.OnStatusEffectAdded(effectData.ID, ownerData.ID, ref effectData);
				}
			}
		}


		public void OnDestroyComponent(EntId destroyedComponentId)
		{
			ref StatusEffectData effectData = ref StatusEffectContainer.GetComponent(destroyedComponentId);
			ref StatusEffectOwnerData ownerData = ref StatusEffectOwnerContainer.GetComponent(effectData.Owner);

			EntityViewAtributes? entityViewAtributes = ViewEntitiesContainer.GetEntityViewAtributes(ownerData.ID);
			if(entityViewAtributes != null && entityViewAtributes.GameObject != null)
			{
				IStatusEffectOwnerView? statusEffectOwnerView = entityViewAtributes.Get<IStatusEffectOwnerView>();
				if(statusEffectOwnerView != null)
					statusEffectOwnerView.OnStatusEffectRemoved(effectData.ID, ownerData.ID, ref effectData);
			}
		}
		public bool Active { get; set; } = true;
		public SystemGroup Group { get; } = GameSystemsGroup.GAME_LOGIC_SYSTEM;
	}

}