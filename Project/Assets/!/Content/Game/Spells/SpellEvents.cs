using System.Collections.Generic;
using Core.Events;
using Core.Model;
using Core.VSEngine;
using FixedPointy;
using Zenject;

namespace Game.Spells
{
	public abstract class SpellEvent<T> : EntityEvent<T> 
		where T : SpellEvent<T>, new()
	{

		[Inject] protected readonly BasicCompContainer<SpellData> SpellContainer = null!;
		
	}
	
	public sealed class OnSpellHitEvent : SpellEvent<OnSpellHitEvent>, ILateEvent
	{
		[Inject] private readonly GameStatsContainer GameStatsContainer = null!;
		[Inject] private readonly StatsSystem StatsSystem = null!;
		[Inject] private readonly EntitiesContainer EntitiesContainer = null!;


		[VSField()]
		public List<EntId> TargetIDs = new List<EntId>();
		
		public override void Execute()
		{
			Fix pierceTimes = StatsSystem.GetStatDepletedValue(EntityID, GameStatsContainer.PierceTimes);

			foreach (EntId targetID in TargetIDs)
			{
				if (targetID == EntId.Invalid)
					continue;

				Fix damage = StatsSystem.GetStatDepletedValue(EntityID, GameStatsContainer.Damage);
				StatsSystem.ChangeDepletedValue(targetID, GameStatsContainer.Health, -damage);

				OnEntityHitEvent onEntityHitEvent = EntityEventQueue.Execute<OnEntityHitEvent>(targetID);
				onEntityHitEvent.ExecuterID = EntityID;
				
			}
			if(pierceTimes <= 0)
				EntitiesContainer.DestroyEntity(EntityID);
			else
				StatsSystem.ChangeDepletedValue(EntityID, GameStatsContainer.PierceTimes, -1);
			
		}
	}
}