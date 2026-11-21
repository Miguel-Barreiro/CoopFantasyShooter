#nullable enable
using System.Runtime.InteropServices;
using Core.Events;
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
	
	public sealed class DeadBodySystem : UpdateComponents<DeadBodyData>, OnCreateComponent<DeadBodyData>
	{
		[Inject] private readonly BasicCompContainer<DeadBodyData> DeadBodyContainer = null!;
		
		public void UpdateComponents(float deltaTime)
		{
			uint topIndex = DeadBodyContainer.TopEmptyIndex;
			for (int i = 0; i < topIndex; i++)
			{
				ref DeadBodyData componentData = ref DeadBodyContainer.Components[i];
				if(!componentData.Enabled)
					continue;
				
				componentData.timeToDisappear -= deltaTime;
				
				if (componentData.timeToDisappear <= 0f)
					EntitiesContainer.DestroyEntity(componentData.ID);
				
			}
		}

		public void OnCreateComponent(EntId newComponentId)
		{
			EntityEventQueue.AddEntityEventListener<EnemyDeathEvent>(newComponentId, OnEnemyDeath);
		}

		private void OnEnemyDeath(EnemyDeathEvent entityEvent)
		{
			ref DeadBodyData componentData = ref DeadBodyContainer.GetComponent(entityEvent.EntityID);
			componentData.Enabled = true;
		}

		public bool Active { get; set; } = true;
		public SystemGroup Group { get; } = GameSystemsGroup.GAME_LOGIC_SYSTEM;
	}
	
	
	
	
	
}