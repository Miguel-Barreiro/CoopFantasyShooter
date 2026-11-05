using System.Runtime.InteropServices;
using Core.Events;
using Core.Model;
using Core.Model.ModelSystems;
using Core.Systems;
using FixedPointy;
using Game.Logic;
using Zenject;

namespace Game.Spells
{
	public sealed class PlayerAbilityEntity : Entity,
										IPlayerAbility,
										IVSAbility,
										IHierarchyEntity
	{
		public PlayerAbilityEntity(EntId ownerID, AbilityConfig dataConfig) : base() 
		{
			var hierarchySystem = GetSystem<IEntityHierarchySystem>();
			hierarchySystem.AddChild(ownerID, ID);
            
			ref VSAbilityData abilityData = ref GetComponent<VSAbilityData>();
			abilityData.dataConfig = dataConfig;
		}
		
	}


	[StructLayout(LayoutKind.Auto)]
	public struct PlayerAbilityData : IComponentData
	{
		public EntId ID { get; set; }

		public AbilityState State;
		
		public void Init()
		{
			State = AbilityState.Ready;
		}
	}

	public enum AbilityState
	{
		Ready,
		Cast, 
		Recover
	}
	
	public interface IPlayerAbility : ISimulation, Component<PlayerAbilityData>, IHierarchyEntity { }
	

	public sealed class PlayerAbilitiesSystem : UpdateComponents<PlayerAbilityData>
	{
		[Inject] private readonly StatsSystem StatsSystem = null!;
		[Inject] private readonly GameStatsContainer GameStatsContainer = null!;
		// [Inject] private readonly GameplayConfig GameplayConfig = null!;
		[Inject] private readonly GameLogic GameLogic = null!;
		[Inject] private readonly BasicCompContainer<PlayerAbilityData> AbilityContainer = null!;

		
		public void TryActivate(EntId spellID, EntId playerId)
		{
			ref PlayerAbilityData spellData = ref AbilityContainer.GetComponent(spellID);
			if (spellData.State == AbilityState.Ready)
			{
				StatsSystem.ResetDepletedValueToMax(spellID, GameStatsContainer.CastingTime);
				spellData.State = AbilityState.Cast;

				ActivatePlayerAbilityEvent activatePlayerAbilityEvent = EntityEventQueue.Execute<ActivatePlayerAbilityEvent>(spellID);
				activatePlayerAbilityEvent.PlayerID = playerId;
				
				
				Fix castingInstances = StatsSystem.ChangeDepletedValue(spellID, GameStatsContainer.CastingMultiplier, -1);
				if (castingInstances == 0)
				{
					StatsSystem.ResetDepletedValueToMax(spellID, GameStatsContainer.Cooldown);
					StatsSystem.ResetDepletedValueToMax(spellID, GameStatsContainer.CastingMultiplier);
					StatsSystem.ResetDepletedValueToMin(spellID, GameStatsContainer.CastingTime);
					spellData.State = AbilityState.Recover;
					EntityEventQueue.Execute<OnAbilityResetCooldownEvent>(spellID);
				}

			}
		}


		public void UpdateComponents(float deltaTime)
		{
			uint topIndex = AbilityContainer.TopEmptyIndex;
			for (int i = 0; i < topIndex; i++)
			{
				ref PlayerAbilityData playerAbilityData = ref AbilityContainer.Components[i];
				switch (playerAbilityData.State)
				{
					case AbilityState.Cast:
						Fix newCastingTime = StatsSystem.ChangeDepletedValue(playerAbilityData.ID, GameStatsContainer.CastingTime, -deltaTime);
						if(newCastingTime == 0)
							playerAbilityData.State = AbilityState.Ready;

						break;
					
					case AbilityState.Recover:
						Fix newCooldown = StatsSystem.ChangeDepletedValue(playerAbilityData.ID, GameStatsContainer.Cooldown, -deltaTime);
						if (newCooldown == 0)
							EntityEventQueue.Execute<ReadyAbilityEvent>(playerAbilityData.ID);
						break;
					
					// case AbilityState.Ready:
					// 	StatsSystem.ChangeDepletedValue(spellData.ID, GameStatsContainer.CastingTime, -deltaTime);
						
				}
				// StatsSystem.ChangeDepletedValue(spellData.ID, GameStatsContainer.CastingTime, deltaTime);
			}		

		}
		
		
		public bool Active { get; set; } = true;
		public SystemGroup Group { get; } = GameSystemsGroup.GAME_LOGIC_SYSTEM;
	}

}