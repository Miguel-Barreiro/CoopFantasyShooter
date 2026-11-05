#nullable enable
using System.Runtime.InteropServices;
using Core.Model;
using Core.Model.ModelSystems;
using Core.Systems;
using Core.View;
using UnityEngine;
using Zenject;

namespace Game.Enemies
{
	// public sealed class DeadBodyEntity : Entity, IDeadBody
	// {
	// 	
	// 	public DeadBodyEntity()
	// 	{
	// 	}
	// }
	//

	[StructLayout(LayoutKind.Auto)]
	public struct DeadBodyData : IComponentData
	{
		public const float TIME_TO_DISAPPEAR = 5f;
		public EntId ID { get; set; }
		
		public float timeToDisappear;
		public bool Enabled;

		public void Init()
		{
			Enabled = false;
			timeToDisappear = TIME_TO_DISAPPEAR;
		}
	}

	public interface IDeadBody : Component<DeadBodyData> { }
	
	
	
	public sealed class DeadBodyViewSystem : UpdateComponents<DeadBodyData>
	{
		[Inject] private readonly EntitiesContainer EntitiesContainer = null!;
		[Inject] private readonly BasicCompContainer<DeadBodyData> DeadBodyContainer = null!;
		[Inject] private readonly ViewEntitiesContainer ViewEntitiesContainer = null!;


		public void UpdateComponents(float deltaTime)
		{
			uint topIndex = DeadBodyContainer.TopEmptyIndex;
			for (int i = 0; i < topIndex; i++)
			{
				ref DeadBodyData componentData = ref DeadBodyContainer.Components[i];
				if(!componentData.Enabled)
					continue;
				
				componentData.timeToDisappear -= deltaTime;
				
				// if (componentData.timeToDisappear <= 0f)
					// EntitiesContainer.DestroyEntity(componentData.ID);
				
			}
		}
		
		
		public bool Active { get; set; } = true;
		public SystemGroup Group { get; } = GameSystemsGroup.GAME_VIEW_SYSTEM;
	}
}