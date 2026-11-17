using Core.Events;
using Core.Model;
using Core.Utils.CachedDataStructures;
using Core.View;
using Game.Logic;
using Sirenix.Reflection.Editor;
using UnityEngine;
using Zenject;

namespace Game.Spells
{
	public sealed class SpellsLogic
	{

		[Inject] private readonly GameMemoryArena GameMemoryArena = null!;
		[Inject] private readonly StatsSystem StatsSystem = null!;
		[Inject] private readonly GameStatsContainer GameStatsContainer = null!;
		[Inject] private readonly EntitiesContainer EntitiesContainer = null!;
		
		
		public void HitSpellInCirle(EntId bulletID, Vector3 position, Vector3 velocity, 
									ulong targetTags, EntId ignoredEntity, float radius = 0.1f)
		{
			if (velocity == Vector3.zero)
			{
				StaticCircleHit();
			} else
			{
				MovingCircleHit();
			}

			
			void StaticCircleHit()
			{
				using CachedList<EntId> hitEntities = ListCache<EntId>.Get();
				int size = Physics.OverlapSphereNonAlloc(position, (float)radius, GameMemoryArena.ColliderHitsCache);
				if(size > 0)
				{
					for (int j = 0; j < size; j++)
					{
						Collider hit = GameMemoryArena.ColliderHitsCache[j];
						EntityView view = hit.gameObject.GetComponent<EntityView>();
						if (view != null &&
							EntitiesContainer.HasAnyTag(view.EntityID, targetTags) &&
							view.EntityID != ignoredEntity)
						{
							hitEntities.Add(view.EntityID);
						}

					}

					if (hitEntities.Count > 0)
					{
						OnSpellHitEvent onSpellHitEvent = EntityEventQueue.Execute<OnSpellHitEvent>(bulletID);
						onSpellHitEvent.TargetIDs.AddRange(hitEntities);
					}

				}
			}

			void MovingCircleHit()
			{
				int size = Physics.SphereCastNonAlloc(position, (float)radius, 
													velocity.normalized, GameMemoryArena.RaycastHitsCache,
													velocity.magnitude);
				if(size > 0)
				{
					using CachedList<EntId> hitEntities = ListCache<EntId>.Get();
					for (int j = 0; j < size; j++)
					{
						RaycastHit hit = GameMemoryArena.RaycastHitsCache[j];
						
						EntityView view = hit.collider.gameObject.GetComponent<EntityView>();
						if (view != null &&
							EntitiesContainer.HasAnyTag(view.EntityID, targetTags) &&
							view.EntityID != ignoredEntity)
						{
							hitEntities.Add(view.EntityID);
						}

					}

					if (hitEntities.Count > 0)
					{
						OnSpellHitEvent onSpellHitEvent = EntityEventQueue.Execute<OnSpellHitEvent>(bulletID);
						onSpellHitEvent.TargetIDs.AddRange(hitEntities);
					}

				}
			}
		}
		
		
	}
}