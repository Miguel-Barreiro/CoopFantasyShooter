using System.Runtime.InteropServices;
using Core.Events;
using Core.Initialization;
using Core.Model;
using Core.Model.ModelSystems;
using Core.Systems;
using FixedPointy;
using Game.Logic;
using Game.Spells;
using UnityEngine;
using Zenject;

namespace Game.Player.Abilities
{
	public sealed class PlayerAbilityEntity : Entity,
										IPlayerAbility,
										IVSAbility,
										IHierarchyEntity
	{
		public PlayerAbilityEntity(EntId ownerID, PlayerAbilityConfig dataConfig) : base() 
		{
			var hierarchySystem = GetSystem<IEntityHierarchySystem>();
			hierarchySystem.AddChild(ownerID, ID);
            
			ref VSAbilityData abilityData = ref GetComponent<VSAbilityData>();
			abilityData.dataConfig = dataConfig;
			
			ref PlayerAbilityData playerAbilityData = ref GetComponent<PlayerAbilityData>();
			playerAbilityData.playerID = ownerID;
			playerAbilityData.AbilityType = dataConfig.AbilityType;
			playerAbilityData.PlayerAbilitySlot = dataConfig.InputSlot;

		}
		
	}


	[StructLayout(LayoutKind.Auto)]
	public struct PlayerAbilityData : IComponentData
	{
		public EntId ID { get; set; }

		public EntId playerID;
		
		public AbilityState State;

		public PlayerAbilitySlot PlayerAbilitySlot;
		public PlayerAbilityAction.AbilityType AbilityType;
		
		public void Init()
		{
			State = AbilityState.Ready;
			AbilityType = PlayerAbilityAction.AbilityType.Custom;
			PlayerAbilitySlot = PlayerAbilitySlot.None;
		}
	}

	public enum AbilityState
	{
		Ready,
		Cast, 
		Recover
	}
	
	public interface IPlayerAbility : ISimulation, Component<PlayerAbilityData>, IHierarchyEntity { }


	public sealed class PlayerAbilitiesSystem : UpdateComponents<PlayerAbilityData>, OnCreateComponent<PlayerAbilityData>, IInitSystem

	{
		
		[Inject] private readonly StatsSystem StatsSystem = null!;
		[Inject] private readonly GameStatsContainer GameStatsContainer = null!;
		[Inject] private readonly ObjectBuilder ObjectBuilder = null!;


		// [Inject] private readonly GameplayConfig GameplayConfig = null!;
		[Inject] private readonly GameLogic GameLogic = null!;
		[Inject] private readonly BasicCompContainer<PlayerAbilityData> AbilityContainer = null!;


		private readonly PlayerAbilityAction[] AbilityActions = new PlayerAbilityAction[PlayerAbilityAction.NUMBER_ABILITY_ACTION_TYPES]
		{
			new CustomPlayerAbility(),
			new AmmoPlayerAbility(),
			new ChargePlayerAbility()
		};

		
		public void Initialize()
		{
			for (int i = 0; i < PlayerAbilityAction.NUMBER_ABILITY_ACTION_TYPES; i++)
				ObjectBuilder.Inject(AbilityActions[i]);
		}


		public void TryActivate(EntId spellID)
		{
			ref PlayerAbilityData spellData = ref AbilityContainer.GetComponent(spellID);
			AbilityActions[(int)spellData.AbilityType].TryActivate(ref spellData);
		}


		public void UpdateComponents(float deltaTime)
		{
			uint topIndex = AbilityContainer.TopEmptyIndex;
			for (int i = 0; i < topIndex; i++)
			{
				ref PlayerAbilityData playerAbilityData = ref AbilityContainer.Components[i];
				AbilityActions[(int)playerAbilityData.AbilityType].Update(ref playerAbilityData, deltaTime);
			}

		}


		public void OnCreateComponent(EntId newComponentId)
		{
			ref PlayerAbilityData playerAbilityData = ref AbilityContainer.GetComponent(newComponentId);
			AbilityActions[(int)playerAbilityData.AbilityType].Init(ref playerAbilityData);
			
		}
		public bool Active { get; set; } = true;
		public SystemGroup Group { get; } = GameSystemsGroup.GAME_LOGIC_SYSTEM;
		
	}

}