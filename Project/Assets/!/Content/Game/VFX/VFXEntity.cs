#nullable enable
using System.Runtime.InteropServices;
using Core.Model;
using Core.Model.ModelSystems;
using Core.Systems;
using Core.View;
using Game.Components;
using Game.Logic;
using UnityEngine;
using Zenject;

namespace Game.VFX
{
	
	public sealed class VFXEntity : Entity, IWorldEntity, ISimulation, IVFX
	{
		public VFXEntity(GameObject vfxPrefab, Vector3 position, Quaternion rotation)
		{
			ViewEntitiesContainer viewEntitiesContainer = GetSystem<ViewEntitiesContainer>();

			EntityViewAtributes? view = viewEntitiesContainer.Spawn(vfxPrefab, ID, position, rotation);
			VFXView? vfxView = view!.Get<VFXView>();
			vfxView!.Play();
		}
	}

	[StructLayout(LayoutKind.Auto)]
	public struct VFXData : IComponentData
	{
		public EntId ID { get; set; }
		

		public void Init() { }
	}

	public interface IVFX : Component<VFXData> { }

	
	

	public sealed class VFXSystem : UpdateComponents<VFXData>
	{
		[Inject] private readonly BasicCompContainer<VFXData> VFXContainer = null!;
		[Inject] private readonly ViewEntitiesContainer ViewEntitiesContainer = null!;

		
		public void UpdateComponents(float deltaTime)
		{
			uint topIndex = VFXContainer.TopEmptyIndex;
			for (int i = 0; i < topIndex; i++)
			{
				ref VFXData vfxData = ref VFXContainer.Components[i];
				EntityViewAtributes entityViewAtributes = ViewEntitiesContainer.GetEntityViewAtributes(vfxData.ID);

				if (entityViewAtributes == null)
				{
					EntitiesContainer.DestroyEntity(vfxData.ID);
					continue;
				}

				VFXView vfxView = entityViewAtributes.Get<VFXView>();
				
				if(vfxView == null)
				{
					EntitiesContainer.DestroyEntity(vfxData.ID);
					continue;
				}
				
				bool isAlive = vfxView.UpdateView(deltaTime);
				if(!isAlive)
				{
					EntitiesContainer.DestroyEntity(vfxData.ID);
				}
			}
		}

		public bool Active { get; set; } = true;
		public SystemGroup Group { get; } = GameSystemsGroup.GAME_VIEW_SYSTEM;
	}
}