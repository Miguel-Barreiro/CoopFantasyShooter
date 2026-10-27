using Core.Events;
using Core.Model;
using Core.View;
using FixedPointy;
using Game.Enemies;
using Game.Player;
using Global;
using UnityEngine;
using Zenject;

namespace Game.Spells
{
	public abstract class AbilityEvent<T> : EntityEvent<T> 
		where T : AbilityEvent<T>, new()
	{
		[Inject] protected readonly BasicCompContainer<AbilityData> AbilityContainer = null!;
		
	}
	
	
	
	public sealed class ReadyAbilityEvent : AbilityEvent<ReadyAbilityEvent>
	{
		[Inject] private readonly GameStatsContainer GameStatsContainer = null!;
		[Inject] private readonly StatsSystem StatsSystem = null!;
		
		public override void Execute()
		{
			StatsSystem.ResetDepletedValueToMin(EntityID, GameStatsContainer.CastingTime);
			ref AbilityData abilityData = ref AbilityContainer.GetComponent(EntityID);
			abilityData.State = AbilityState.Ready;
		}
	}
	
	public sealed class ActivatePlayerAbilityEvent : AbilityEvent<ActivatePlayerAbilityEvent>
	{
		[Inject] private readonly GameLogic GameLogic = null!;
		[Inject] private readonly StatsSystem StatsSystem = null!;
		[Inject] private readonly GameStatsContainer GameStatsContainer = null!;
		[Inject] private readonly PlayerContainer PlayerContainer = null!;
		[Inject] private readonly ViewEntitiesContainer ViewEntitiesContainer = null!;
		[Inject] private readonly GameplayConfig GameplayConfig = null!;

		
		public EntId PlayerID = EntId.Invalid;
		
		public override void Execute()
		{
			
			ref PlayerData playerData = ref PlayerContainer.GetComponent(PlayerID);
			EntityViewAtributes playerViewAtributes = ViewEntitiesContainer.GetEntityViewAtributes(PlayerID);

			Vector3 direction = playerData.LookDirection.normalized;
			playerData.LookDirection = direction.normalized;
			Vector3 playerPosition = playerViewAtributes!.GameObject!.transform.position;

			Vector3 position = playerPosition + 
							Vector3.up * 0.9f +
							direction.normalized * 
							(GameplayConfig.PlayerAttributes.Size + GameplayConfig.BasicSpellsAttributes.BulletSize);

			ActivateAbilityEvent abilityEvent = EntityEventQueue.Execute<ActivateAbilityEvent>(EntityID);
			abilityEvent.OriginPosition = position;
			abilityEvent.Direction = direction;
			abilityEvent.TargetPosition = Vector3.negativeInfinity;
			abilityEvent.Owner = EntityID;
			
			// GameLogic.SpawnBullet(PlayerID, GameplayConfig.BasicSpellsAttributes.BulletSpellConfig, direction, position);
		}
	}
	
	public sealed class OnAbilityResetCooldownEvent : AbilityEvent<OnAbilityResetCooldownEvent>, ILateEvent
	{
		[Inject] private readonly GameStatsContainer GameStatsContainer = null!;
		[Inject] private readonly StatsSystem StatsSystem = null!;
		
		public override void Execute()
		{
			ref AbilityData abilityData = ref AbilityContainer.GetComponent(EntityID);
			abilityData.State = AbilityState.Recover;
		}
	}
	
	
	public sealed class OnAbilityHitEvent : AbilityEvent<OnAbilityHitEvent>, ILateEvent
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