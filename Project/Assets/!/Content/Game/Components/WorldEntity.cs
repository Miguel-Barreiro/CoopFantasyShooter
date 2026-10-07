using System.Runtime.InteropServices;
using Core.Model;
using Core.Model.ModelSystems;
using Core.Systems;
using Core.View;
using UnityEngine;
using Zenject;

namespace Game.Components
{
	[StructLayout(LayoutKind.Auto)]
	public struct DynamicWorldEntityData : IComponentData
	{
		public EntId ID { get; set; }

		public Vector2 Position;
		public Vector2 Direction;

		public void Init()
		{
			Position = Vector2.zero;
			Direction = Vector2.right;
		}
	}

	public interface IDynamicWorldEntity : Component<DynamicWorldEntityData> { }
	
	
	
	public sealed class DynamicWorldEntitySystem : UpdateComponents<DynamicWorldEntityData>
	{
		[Inject] private readonly ViewEntitiesContainer ViewEntitiesContainer = null!;
		[Inject] private readonly BasicCompContainer<DynamicWorldEntityData> DynamicWorldEntityContainer = null!;



		public void UpdateComponents(float deltaTime)
		{
			uint topIndex = DynamicWorldEntityContainer.TopEmptyIndex;
			for (int i = 0; i < topIndex; i++)
			{
				ref DynamicWorldEntityData componentData = ref DynamicWorldEntityContainer.Components[i];

				EntityViewAtributes viewAtributes = ViewEntitiesContainer.GetEntityViewAtributes(componentData.ID);
				if (viewAtributes != null)
				{
					Transform transform = viewAtributes.Get<Transform>();
					transform.position = componentData.Position;
					transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(componentData.Direction.y, componentData.Direction.x) * Mathf.Rad2Deg);
				}
			}		
			
		}

		public bool Active { get; set; } = true;
		public SystemGroup Group { get; } = GameSystemsGroup.GAME_LOGIC_SYSTEM; 
	}
	
	
	
}


