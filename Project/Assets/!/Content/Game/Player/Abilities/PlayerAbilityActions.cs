using Core.Events;
using Core.Model;
using FixedPointy;
using Game.Components;
using Game.Spells;
using Global;
using UnityEngine;
using Zenject;

namespace Game.Player.Abilities
{
	public abstract class PlayerAbilityAction
	{
		public const int NUMBER_ABILITY_ACTION_TYPES = 3;
		
	
		[Inject] protected readonly StatsSystem StatsSystem = null!;
		[Inject] protected readonly GameStatsContainer GameStatsContainer = null!;
		[Inject] private readonly GameplayConfig GameplayConfig = null!;

		[Inject] private readonly PlayerContainer PlayerContainer = null!;
		[Inject] private readonly BasicCompContainer<WorldEntityData> WorldEntityContainer = null!;


		public abstract void Init(ref PlayerAbilityData abilityData);
		public abstract void Update(ref PlayerAbilityData abilityData, Fix deltaTime);
		public abstract bool TryActivate(ref PlayerAbilityData abilityData);
		
		protected void NormalActivate(ref PlayerAbilityData abilityData)
		{
			EntId abilityID = abilityData.ID;
			EntId playerID = abilityData.playerID;
			
			ref PlayerData playerData = ref PlayerContainer.GetComponent(playerID);
			ref WorldEntityData worldEntityData = ref WorldEntityContainer.GetComponent(playerID);

			Vector3 direction = playerData.LookDirection.normalized;
			Vector3 playerPosition = worldEntityData.Position;
			Vector3 position = playerPosition + 
								Vector3.up * 0.9f +
								direction.normalized * 
								(GameplayConfig.PlayerAttributes.Size);

			
			Fix castingMultiplier = StatsSystem.GetStatValue(abilityID, GameStatsContainer.CastingMultiplier);
			for (uint i = 0; i < castingMultiplier; i++)
			{
				ActivateAbilityEvent abilityEvent = EntityEventQueue.Execute<ActivateAbilityEvent>(abilityID);
				abilityEvent.CasterOwner = playerID;
				abilityEvent.OriginPosition = position;
				abilityEvent.Direction = direction;
				abilityEvent.TargetPosition = Vector3.negativeInfinity;
				abilityEvent.InstanceNumber = i;
			}
		}
	}
	
	
	internal sealed class AmmoPlayerAbility : PlayerAbilityAction
	{
		public override bool TryActivate(ref PlayerAbilityData abilityData)
		{
			if(abilityData.State != AbilityState.Ready)
				return false;
			
			EntId abilityID = abilityData.ID;
			Fix current = StatsSystem.GetStatDepletedValue(abilityID, GameStatsContainer.Ammo);
			if (current <= 0)
			{
				StartReload(ref abilityData);
				return false;
			}
			
			Fix newAmmo = StatsSystem.ChangeDepletedValue(abilityID, GameStatsContainer.Ammo, -1);
			if(newAmmo <= 0)
			{
				NormalActivate(ref abilityData);
				StartReload(ref abilityData);
				return true;
			}
			
			NormalActivate(ref abilityData);
			
			abilityData.State = AbilityState.Cast;
			StatsSystem.ResetDepletedValueToMax(abilityID, GameStatsContainer.CastingTime);
			
			return true;

			void StartReload(ref PlayerAbilityData playerAbilityData)
			{
				// EntityEventQueue.Execute<OnAbilityResetCooldownEvent>(abilityID);
				
				StatsSystem.ResetDepletedValueToMax(abilityID, GameStatsContainer.Cooldown);
				StatsSystem.ResetDepletedValueToMin(abilityID, GameStatsContainer.CastingTime);

				playerAbilityData.State = AbilityState.Recover;
			}
		}

		public override void Init(ref PlayerAbilityData abilityData)
		{
			EntId abilityID = abilityData.ID;
			
			StatsSystem.ResetDepletedValueToMax(abilityID, GameStatsContainer.CastingTime);
			StatsSystem.ResetDepletedValueToMax(abilityID, GameStatsContainer.Cooldown);
			StatsSystem.ResetDepletedValueToMin(abilityID, GameStatsContainer.CastingTime);
			StatsSystem.ResetDepletedValueToMax(abilityID, GameStatsContainer.Ammo);

			abilityData.State = AbilityState.Ready;
		}

		public override void Update(ref PlayerAbilityData abilityData, Fix deltaTime)
		{
			EntId abilityID = abilityData.ID;
			switch (abilityData.State)
			{
				case AbilityState.Cast:
					Fix newCastingTime = StatsSystem.ChangeDepletedValue(abilityID, GameStatsContainer.CastingTime, -deltaTime);
					if (newCastingTime <= 0)
					{
						Fix maxCastingTime = StatsSystem.GetStatValue(abilityID, GameStatsContainer.CastingTime);
						StatsSystem.SetDepletedValue(abilityID, GameStatsContainer.CastingTime, maxCastingTime + newCastingTime);
						abilityData.State = AbilityState.Ready;
					}


					break;
				case AbilityState.Recover:
					Fix newCooldown = StatsSystem.ChangeDepletedValue(abilityID, GameStatsContainer.Cooldown, -deltaTime);
					if (newCooldown <= 0)
					{
						StatsSystem.ResetDepletedValueToMin(abilityID, GameStatsContainer.CastingTime);
						StatsSystem.ResetDepletedValueToMax(abilityID, GameStatsContainer.Ammo);

						OnReadyPlayerAbilityEvent onReadyPlayerAbilityEvent = EntityEventQueue.Execute<OnReadyPlayerAbilityEvent>(abilityID);
					}

					break;
				// case AbilityState.Ready:
				// 	StatsSystem.ChangeDepletedValue(spellData.ID, GameStatsContainer.CastingTime, -deltaTime);
						
			}
		}
	}
	
	internal sealed class ChargePlayerAbility : PlayerAbilityAction
	{
		public override void Init(ref PlayerAbilityData abilityData)
		{
			throw new System.NotImplementedException();
		}

		public override void Update(ref PlayerAbilityData abilityData, Fix deltaTime)
		{
			
		}

		public override bool TryActivate(ref PlayerAbilityData abilityData)
		{
			// EntId abilityDataID = abilityData.ID;
			// Fix castingInstances = StatsSystem.ChangeDepletedValue(abilityDataID, GameStatsContainer.CastingMultiplier, -1);
			// if (castingInstances < 0)
			// {
			// 	ResetAbility(ref abilityData);
			// 	return false;
			// }
			//
			// if (castingInstances == 0)
			// 	ResetAbility(ref abilityData);
			//
			// return true;
			//
			// void ResetAbility(ref PlayerAbilityData abilityData)
			// {
			// 	StatsSystem.ResetDepletedValueToMax(abilityDataID, GameStatsContainer.Cooldown);
			// 	StatsSystem.ResetDepletedValueToMax(abilityDataID, GameStatsContainer.CastingMultiplier);
			// 	StatsSystem.ResetDepletedValueToMin(abilityDataID, GameStatsContainer.CastingTime);
			// 	abilityData.State = AbilityState.Recover;
			// 	EntityEventQueue.Execute<OnAbilityResetCooldownEvent>(abilityDataID);
			// }
			return false; 
		}
	}

	internal sealed class CustomPlayerAbility : PlayerAbilityAction
	{
		//TODO: we need to add events to call the vs graphs for custom abilities,
		// here is where we will do it
		
		
		public override bool TryActivate(ref PlayerAbilityData abilityData)
		{

			return false;
		}

		public override void Init(ref PlayerAbilityData abilityData)
		{
			
		}

		public override void Update(ref PlayerAbilityData abilityData, Fix deltaTime)
		{
			
			
		}
	}

	
}