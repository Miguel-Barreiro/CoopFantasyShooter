using Core.Events;
using Core.Model;
using Core.Model.ModelSystems;
using Core.Systems;
using Core.View;
using Game.Components;
using Game.Spells.Bullet;
using Game.VFX;
using UnityEngine;
using Zenject;

namespace Game.Spells
{
	public sealed class BulletViewSystem : UpdateComponents<BulletData>, OnCreateComponent<BulletData>
	{
		[Inject] private readonly BasicCompContainer<WorldEntityData> WorldContainer = null!;
		[Inject] private readonly BasicCompContainer<BulletData> BulletContainer = null!;
		[Inject] private readonly ViewEntitiesContainer ViewEntitiesContainer = null!;

		[Inject] private readonly BasicCompContainer<VFXData> VFXContainer = null!;


		
		public void UpdateComponents(float deltaTime)
		{

			uint topIndex = BulletContainer.TopEmptyIndex;
			for (int i = 0; i < topIndex; i++)
			{
				ref BulletData componentData = ref BulletContainer.Components[i];
				EntId bulletID = componentData.ID;
				
				ref WorldEntityData worldEntityData = ref WorldContainer.GetComponent(bulletID);
				EntityViewAtributes entityViewAtributes = ViewEntitiesContainer.GetEntityViewAtributes(bulletID);
				Transform bulletTransform = entityViewAtributes!.GameObject!.transform;
				
				bulletTransform.position = worldEntityData.Position;
				bulletTransform.forward = worldEntityData.Direction;
			}
			
		}


		public void OnCreateComponent(EntId newComponentId)
		{
			ref BulletData bulletData = ref BulletContainer.GetComponent(newComponentId);
			
			EntityEventQueue.AddEntityEventListener<OnSpellHitEvent>(newComponentId, OnSpellHit);
			EntityEventQueue.AddEntityEventListener<OnSpellHitObstacleEvent>(newComponentId, OnSpellHitObstacle);
			if(bulletData.DataConfig.OriginVFX == null)
				return;
			
			ref WorldEntityData worldEntityData = ref WorldContainer.GetComponent(newComponentId);
			VFXEntity originVFX = new VFXEntity(bulletData.DataConfig.OriginVFX, 
													worldEntityData.Position, Quaternion.identity);
		}

		private void OnSpellHit(OnSpellHitEvent entityEvent){ CreateSpellHitVFX(entityEvent.EntityID); }
		private void OnSpellHitObstacle(OnSpellHitObstacleEvent entityEvent) { CreateSpellHitVFX(entityEvent.EntityID); }

		private void CreateSpellHitVFX(EntId bulletID )
		{
			ref BulletData bulletData = ref BulletContainer.GetComponent(bulletID);
			if(bulletData.DataConfig.ImpactVFX == null)
				return;
			
			ref WorldEntityData worldEntityData = ref WorldContainer.GetComponent(bulletID);
			VFXEntity originVFX = new VFXEntity(bulletData.DataConfig.ImpactVFX, 
												worldEntityData.Position, Quaternion.identity);

		}



		public bool Active { get; set; } = true;
		public SystemGroup Group { get; } = GameSystemsGroup.GAME_VIEW_SYSTEM;
	}
}