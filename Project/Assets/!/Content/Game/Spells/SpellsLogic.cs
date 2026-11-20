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
		
		
		public int HitSpellInCirle(EntId bulletID, Vector3 position, Vector3 velocity, 
									ulong targetTags, EntId ignoredEntity, float radius = 0.2f)
		{
			if (velocity == Vector3.zero)
			{
				return StaticCircleHit();
			} else
			{
				return MovingCircleHit();
			}

			
			int StaticCircleHit()
			{
				using CachedList<EntId> hitEntities = ListCache<EntId>.Get();
				int size = Physics.OverlapSphereNonAlloc(position, (float)radius, GameMemoryArena.ColliderHitsCache, 
														Physics.AllLayers, QueryTriggerInteraction.Ignore );
				if(size > 0)
				{
					bool hitObstacle = false;
					for (int j = 0; j < size; j++)
					{
						Collider hit = GameMemoryArena.ColliderHitsCache[j];
						EntityView view = hit.gameObject.GetComponent<EntityView>();

						if (view == null)
						{
							hitObstacle = true;
							continue;
						}
						EntId viewEntityID = view.EntityID;

						if (EntitiesContainer.HasAnyTag(viewEntityID, targetTags) && viewEntityID != ignoredEntity)
						{
							hitEntities.Add(viewEntityID);
						}

					}

					if (hitObstacle)
					{
						OnSpellHitObstacleEvent onSpellHitObstacleEvent = EntityEventQueue.Execute<OnSpellHitObstacleEvent>(bulletID);
					}

					if (hitEntities.Count > 0)
					{
						OnSpellHitEvent onSpellHitEvent = EntityEventQueue.Execute<OnSpellHitEvent>(bulletID);
						onSpellHitEvent.TargetIDs.AddRange(hitEntities);
					}

				}

				return size;
			}

			int MovingCircleHit()
			{
				int size = Physics.SphereCastNonAlloc(position, (float)radius, 
													velocity.normalized, GameMemoryArena.RaycastHitsCache,
													velocity.magnitude, 
													Physics.AllLayers, QueryTriggerInteraction.Ignore);
				if(size > 0)
				{
					using CachedList<EntId> hitEntities = ListCache<EntId>.Get();
					bool hitObstacle = false;
					for (int j = 0; j < size; j++)
					{
						RaycastHit hit = GameMemoryArena.RaycastHitsCache[j];
						
						EntityView view = hit.collider.gameObject.GetComponent<EntityView>();
						if (view == null)
						{
							hitObstacle = true;
							continue;
						}
						
						if (EntitiesContainer.HasAnyTag(view.EntityID, targetTags) &&
							view.EntityID != ignoredEntity)
						{
							hitEntities.Add(view.EntityID);
						}

					}
					
					
					if (hitObstacle)
					{
						OnSpellHitObstacleEvent onSpellHitObstacleEvent = EntityEventQueue.Execute<OnSpellHitObstacleEvent>(bulletID);
					}

					if (hitEntities.Count > 0)
					{
						OnSpellHitEvent onSpellHitEvent = EntityEventQueue.Execute<OnSpellHitEvent>(bulletID);
						onSpellHitEvent.TargetIDs.AddRange(hitEntities);
					}

				}
				return size;
			}
		}
		
		
	}
}