using Core.Events;
using Core.Model;
using Core.View;
using FixedPointy;
using Game.Enemies;
using Game.Player.Abilities;
using Zenject;

namespace Game.Spells
{
	public abstract class SpellEvent<T> : EntityEvent<T> 
		where T : SpellEvent<T>, new()
	{

		[Inject] protected readonly BasicCompContainer<SpellData> SpellContainer = null!;
		
	}
	
	public sealed class OnSpellHitEvent : AbilityEvent<OnSpellHitEvent>, ILateEvent
	{
		[Inject] private readonly GameStatsContainer GameStatsContainer = null!;
		[Inject] private readonly StatsSystem StatsSystem = null!;
		[Inject] private readonly EntitiesContainer EntitiesContainer = null!;
		[Inject] private readonly ViewEntitiesContainer ViewEntitiesContainer = null!;



		public EntId EnemyID = EntId.Invalid;

		public override void Execute()
		{
			if (EnemyID != EntId.Invalid)
			{
				if (EntitiesContainer.IsEntityOfType<EnemyEntity>(EnemyID))
				{
					Fix damage = StatsSystem.GetStatDepletedValue(EntityID, GameStatsContainer.Damage);
					StatsSystem.ChangeDepletedValue(EnemyID, GameStatsContainer.Health, -damage);
					EntityViewAtributes entityViewAtributes = ViewEntitiesContainer.GetEntityViewAtributes(EnemyID);
					entityViewAtributes!.Get<EnemyView>().TriggerHit();
				}
			} 
			EntitiesContainer.DestroyEntity(EntityID);
		}
	}
}