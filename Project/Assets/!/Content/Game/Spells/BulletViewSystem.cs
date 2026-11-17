using Core.Model;
using Core.Model.ModelSystems;
using Core.Systems;
using Core.View;
using Game.Components;
using Game.Spells.Bullet;
using UnityEngine;
using Zenject;

namespace Game.Spells
{
	public sealed class BulletViewSystem : UpdateComponents<BulletData>
	{
		[Inject] private readonly BasicCompContainer<WorldEntityData> WorldContainer = null!;
		[Inject] private readonly BasicCompContainer<BulletData> BulletContainer = null!;
		[Inject] private readonly ViewEntitiesContainer ViewEntitiesContainer = null!;

		
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
		
		
		public bool Active { get; set; } = true;
		public SystemGroup Group { get; } = GameSystemsGroup.GAME_VIEW_SYSTEM;
	}
}