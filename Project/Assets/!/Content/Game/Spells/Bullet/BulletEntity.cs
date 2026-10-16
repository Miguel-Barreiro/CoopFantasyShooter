using System.Runtime.InteropServices;
using Core.Events;
using Core.Model;
using Core.Model.ModelSystems;
using Core.Systems;
using Core.View;
using Game.Enemies;
using UnityEngine;
using Zenject;

namespace Game.Spells.Bullet
{
	public sealed class BulletEntity : Entity,
									IBullet
	{
		
	}

	[StructLayout(LayoutKind.Auto)]
	public struct BulletData : IComponentData
	{
		public EntId ID { get; set; }
		

		public void Init() { }
	}

	public interface IBullet : Component<BulletData> { }


	public sealed class BulletSystem : UpdateComponents<BulletData>
	{
		[Inject] private readonly BasicCompContainer<BulletData> BulletContainer = null!;
		[Inject] private readonly ViewEntitiesContainer ViewEntitiesContainer = null!;

		
		private readonly RaycastHit[] ResultsCache = new RaycastHit[10];
		
		public void UpdateComponents(float deltaTime)
		{

			uint topIndex = BulletContainer.TopEmptyIndex;
			for (int i = 0; i < topIndex; i++)
			{
				ref BulletData componentData = ref BulletContainer.Components[i];
				EntId bulletID = componentData.ID;
				
				EntityViewAtributes viewAtributes = ViewEntitiesContainer.GetEntityViewAtributes(bulletID);
				Vector3 position = viewAtributes!.GameObject!.transform.position;
				if(position.magnitude > 2000f)
				{
					EntitiesContainer.DestroyEntity(bulletID);
					continue;
				}
				
				Rigidbody rigidbody = viewAtributes.Get<Rigidbody>();
				Vector3 velocity = rigidbody.linearVelocity;

				int size = Physics.SphereCastNonAlloc(position, 0.1f, 
													velocity.normalized, ResultsCache,
													velocity.magnitude * Time.deltaTime);
				
				if(size > 0)
				{
					for (int j = 0; j < size; j++)
					{
						RaycastHit hit = ResultsCache[j];
						
						EnemyView enemyView = hit.collider.gameObject.GetComponent<EnemyView>();
						OnSpellHitEvent onSpellHitEvent = EntityEventQueue.Execute<OnSpellHitEvent>(bulletID);
							
						if(enemyView != null)
							onSpellHitEvent.EnemyID = enemyView.EntityID;
						else
							onSpellHitEvent.EnemyID = EntId.Invalid;
					}
				}
				
				
			}		

			
			
			
		}

		public bool Active { get; set; } = true;
		public SystemGroup Group { get; } = GameSystemsGroup.GAME_LOGIC_SYSTEM;
	}
	
	
}