#nullable enable
using System.Runtime.InteropServices;
using Core.Model;
using Core.Model.ModelSystems;
using Core.Systems;
using Core.View;
using FixedPointy;
using Game.VSNodes.Search;
using UnityEngine;
using UnityEngine.AI;
using Zenject;

namespace Game.Components
{
	[StructLayout(LayoutKind.Auto)]
	public struct WorldEntityData : IComponentData
	{
		public EntId ID { get; set; }
		
		public Vector3 Position;
		public Vector3 Direction;
		public Fix Size;

		public void Init()
		{
			Position = Vector3.zero;
			Direction = Vector3.forward;
			Size = 0.15f;
		}
	}
	
	public interface IWorldEntity : Component<WorldEntityData> { }
	
	
	[OnCreateComponentProperties(Priority = SystemPriority.Late), UpdateComponentProperties(Priority = SystemPriority.Late)]
	public sealed class WorldEntitySystem : OnCreateComponent<WorldEntityData>, UpdateComponents<WorldEntityData>
	{
		[Inject] private readonly ViewEntitiesContainer ViewEntitiesContainer = null!;
		[Inject] private readonly BasicCompContainer<WorldEntityData> WorldEntityContainer = null!;

		[Inject] private readonly StatsSystem StatsSystem = null!;
		[Inject] private readonly GameStatsContainer GameStatsContainer = null!;


		
		public void OnCreateComponent(EntId newComponentId)
		{
			ref WorldEntityData worldEntityData = ref WorldEntityContainer.GetComponent(newComponentId);
			EntityViewAtributes? entityViewAtributes = ViewEntitiesContainer.GetEntityViewAtributes(newComponentId);
			if(entityViewAtributes is null)
			{
				Debug.LogError($"WorldEntitySystem: Entity {newComponentId} has no view atributes");
				return;
			}
			
			worldEntityData.Position = entityViewAtributes.GameObject!.transform.position;
			worldEntityData.Direction = entityViewAtributes.GameObject!.transform.forward;
		}
		
		
		public void UpdateComponents(float deltaTime)
		{
			uint topIndex = WorldEntityContainer.TopEmptyIndex;
			for (int i = 0; i < topIndex; i++)
			{
				ref WorldEntityData worldEntityData = ref WorldEntityContainer.Components[i];

				EntId entId = worldEntityData.ID;
				EntityViewAtributes? entityViewAtributes = ViewEntitiesContainer.GetEntityViewAtributes(entId);
				if(entityViewAtributes is null)
				{
					Debug.LogError($"WorldEntitySystem: Entity {entId} has no view atributes");
					continue;
				}
				
				worldEntityData.Position = entityViewAtributes.GameObject!.transform.position;
				worldEntityData.Direction = entityViewAtributes.GameObject!.transform.forward;
			}		
		}

		public bool Active { get; set; } = true;
		public SystemGroup Group { get; } = GameSystemsGroup.GAME_LOGIC_SYSTEM;
	}
	
}