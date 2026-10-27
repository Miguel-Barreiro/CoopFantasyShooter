using System.Runtime.InteropServices;
using Core.Events;
using Core.Model;
using Core.Model.ModelSystems;
using Core.Systems;
using FixedPointy;
using Zenject;

namespace Game.Spells
{
	public sealed class AbilityEntity : Entity,
										IAbility,
										IVSAbility,
										IHierarchyEntity
	{
		public AbilityEntity(EntId ownerID, AbilityConfig dataConfig) : base() 
		{
			var hierarchySystem = GetSystem<IEntityHierarchySystem>();
			hierarchySystem.AddChild(ownerID, ID);
            
			ref VSAbilityData abilityData = ref GetComponent<VSAbilityData>();
			abilityData.dataConfig = dataConfig;
		}
		
	}


	[StructLayout(LayoutKind.Auto)]
	public struct AbilityData : IComponentData
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
	
	public interface IAbility : Component<AbilityData>, IHierarchyEntity { }
	

	public sealed class AbilitySystem : UpdateComponents<AbilityData>
	{
		[Inject] private readonly StatsSystem StatsSystem = null!;
		[Inject] private readonly GameStatsContainer GameStatsContainer = null!;
		// [Inject] private readonly GameplayConfig GameplayConfig = null!;
		[Inject] private readonly GameLogic GameLogic = null!;
		[Inject] private readonly BasicCompContainer<AbilityData> AbilityContainer = null!;

		
		public void TryActivate(EntId spellID, EntId playerId)
		{
			ref AbilityData spellData = ref AbilityContainer.GetComponent(spellID);
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
				ref AbilityData abilityData = ref AbilityContainer.Components[i];
				switch (abilityData.State)
				{
					case AbilityState.Cast:
						Fix newCastingTime = StatsSystem.ChangeDepletedValue(abilityData.ID, GameStatsContainer.CastingTime, -deltaTime);
						if(newCastingTime == 0)
							abilityData.State = AbilityState.Ready;

						break;
					
					case AbilityState.Recover:
						Fix newCooldown = StatsSystem.ChangeDepletedValue(abilityData.ID, GameStatsContainer.Cooldown, -deltaTime);
						if (newCooldown == 0)
							EntityEventQueue.Execute<ReadyAbilityEvent>(abilityData.ID);
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