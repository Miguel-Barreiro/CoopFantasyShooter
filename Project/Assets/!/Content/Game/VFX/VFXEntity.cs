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
		public VFXEntity(VFXConfig dataConfig, Vector3 origin, Quaternion rotation)
		{
			
			GameObject vfxPrefab = dataConfig.Prefab.gameObject;

			ref VFXData vfxData = ref GetComponent<VFXData>();
			vfxData.DataConfig = dataConfig;
			vfxData.Origin = origin;
			vfxData.Target = origin;
			vfxData.Duration = dataConfig.Duration;
			vfxData.ElapsedTime = 0f;
			
		}
	}

	[StructLayout(LayoutKind.Auto)]
	public struct VFXData : IComponentData
	{
		public EntId ID { get; set; }

		public Vector3 Origin;
		public Vector3 Target;
		public float Duration;
		public float ElapsedTime;
		public VFXConfig DataConfig;
		

		public void Init() { }
	}

	public interface IVFX : Component<VFXData> { }

	
	

	public sealed class VFXSystem : UpdateComponents<VFXData>, OnCreateComponent<VFXData>
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
				
				bool isAlive = vfxView.UpdateView(deltaTime, ref vfxData);
				if(!isAlive)
				{
					EntitiesContainer.DestroyEntity(vfxData.ID);
				}
			}
		}

		public void OnCreateComponent(EntId newComponentId)
		{
			ref VFXData vfxData = ref VFXContainer.GetComponent(newComponentId);
			
			EntityViewAtributes? view = ViewEntitiesContainer.Spawn(vfxData.DataConfig.Prefab.gameObject, vfxData.ID, 
																	vfxData.Origin, 
																	Quaternion.identity);
			VFXView? vfxView = view!.Get<VFXView>();
			vfxView!.Play();
		}
		public bool Active { get; set; } = true;
		public SystemGroup Group { get; } = GameSystemsGroup.GAME_VIEW_SYSTEM;
	}
}