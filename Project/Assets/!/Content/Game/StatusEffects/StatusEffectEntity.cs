using System.Runtime.InteropServices;
using Core.Model;
using Core.Model.ModelSystems;
using Core.Systems;
using Core.View;
using FixedPointy;
using Game.Logic;
using Zenject;

namespace Game.StatusEffects
{
	public enum StatusEffectType
	{
		Haste = 0,
		Slow = 1,
		Stun = 2,
		Root = 3,
		Shield = 4,
		Regen = 5,
		Poison = 6,
		Fire = 7,
	}

	
	public sealed class StatusEffectEntity : Entity, 
											IStatusEffect, 
											IVSAbility, 
											IHierarchyEntity
	{
		public StatusEffectEntity(EntId owner, StatusEffectConfig config)
		{
			var hierarchySystem = GetSystem<IEntityHierarchySystem>();
			hierarchySystem.AddChild(owner, ID);

			ref VSAbilityData abilityData = ref GetComponent<VSAbilityData>();
			abilityData.dataConfig = config;
			
			ref StatusEffectData statusEffectData = ref GetComponent<StatusEffectData>();
			statusEffectData.Owner = owner;
			statusEffectData.Overrides = config.Overrides;
			statusEffectData.StatusEffectType = config.StatusEffectType;
			// statusEffectData.Config	= config;
		}
	}

	[StructLayout(LayoutKind.Auto)]
	public struct StatusEffectData : IComponentData
	{
		public EntId ID { get; set; }
		public StatusEffectType StatusEffectType;

		public bool Overrides;
		public uint OverridePriority;
		public EntId Owner;

		public void Init()
		{
			OverridePriority = 0;
			Overrides = false;
			Owner = EntId.Invalid;
			StatusEffectType = StatusEffectType.Haste;
		}
	}


	public interface IStatusEffect : ISimulation, Component<StatusEffectData> { }


	

	[StructLayout(LayoutKind.Auto)]
	public struct StatusEffectOwnerData : IComponentData
	{
		public const int MAX_STATUS_EFFECTS_TYPES = 8;
		public const int MAX_STATUS_EFFECTS_APPLIED = 10;
		
		public EntId ID { get; set; }
		
		[MarshalAs(UnmanagedType.ByValArray, SizeConst=MAX_STATUS_EFFECTS_TYPES)] public StatusEffectApplied[] StatusEffects;
		
		public struct StatusEffectApplied
		{
			public EntId StatusEffectID;
			public uint OverridePriority;
			
		}

		public void Init()
		{
			StatusEffects = new StatusEffectApplied[MAX_STATUS_EFFECTS_TYPES];
			for(int i = 0; i < MAX_STATUS_EFFECTS_TYPES; i++)
				StatusEffects[i] = new StatusEffectApplied { StatusEffectID = EntId.Invalid, OverridePriority = 0 };
		}
		
		public void AddStatusEffect(ref StatusEffectData statusEffectData)
		{
			
			if (statusEffectData.Overrides)
			{
				ref StatusEffectApplied statusEffectApplied = ref StatusEffects[(int) statusEffectData.StatusEffectType];
				if(statusEffectApplied.StatusEffectID != EntId.Invalid)
				{
					if(statusEffectApplied.OverridePriority >= statusEffectData.OverridePriority)
					{
						// Debug.Log($"StatusEffect({statusEffectData.ID}) cannot override existing StatusEffect({statusEffectApplied.StatusEffectID}) on entity {ID}");
						return;
					}
					else
					{
						EntitiesContainer.DestroyEntity(statusEffectApplied.StatusEffectID);
					}
				}
				
				statusEffectApplied.StatusEffectID = statusEffectData.ID;
				statusEffectApplied.OverridePriority = statusEffectData.OverridePriority;
			}


		}
		
		public void RemoveStatusEffect(ref StatusEffectData statusEffectData)
		{
			if (statusEffectData.Overrides)
			{
				ref StatusEffectApplied statusEffectApplied = ref StatusEffects[(int) statusEffectData.StatusEffectType];
				if(statusEffectApplied.StatusEffectID == statusEffectData.ID)
				{
					statusEffectApplied.StatusEffectID = EntId.Invalid;
					statusEffectApplied.OverridePriority = 0;
				}
			}
		}
	}

	public interface IStatusEffectOwner : ISimulation, Component<StatusEffectOwnerData> { }
	
	
	[UpdateComponentProperties(Priority = SystemPriority.Early)]
	public sealed class StatusEffectSystem : UpdateComponents<StatusEffectData>, 
											OnCreateComponent<StatusEffectData>, 
											OnDestroyComponent<StatusEffectData>
	{
		[Inject] private readonly BasicCompContainer<StatusEffectOwnerData> StatusEffectOwnerContainer = null!;
		[Inject] private readonly BasicCompContainer<StatusEffectData> StatusEffectContainer = null!;
		[Inject] private readonly ViewEntitiesContainer ViewEntitiesContainer = null!;
		[Inject] private readonly GameStatsContainer GameStatsContainer = null!;
		[Inject] private readonly StatsSystem StatsSystem = null!;

		
		public void UpdateComponents(float deltaTime)
		{
			uint topIndex = StatusEffectContainer.TopEmptyIndex;
			for (int i = 0; i < topIndex; i++)
			{
				ref StatusEffectData componentData = ref StatusEffectContainer.Components[i];
				EntId statusEffectID = componentData.ID;

				Fix statDepletedValue = StatsSystem.ChangeDepletedValue(componentData.ID, GameStatsContainer.TimeToLive, -deltaTime);
				if(statDepletedValue <= 0f)
					EntitiesContainer.DestroyEntity(statusEffectID);
			}
		}

		public void OnCreateComponent(EntId newComponentId)
		{
			ref StatusEffectData effectData = ref StatusEffectContainer.GetComponent(newComponentId);
			ref StatusEffectOwnerData ownerData = ref StatusEffectOwnerContainer.GetComponent(effectData.Owner);
			
			ownerData.AddStatusEffect(ref effectData);
		}


		public void OnDestroyComponent(EntId destroyedComponentId)
		{
			ref StatusEffectData effectData = ref StatusEffectContainer.GetComponent(destroyedComponentId);
			ref StatusEffectOwnerData ownerData = ref StatusEffectOwnerContainer.GetComponent(effectData.Owner);
			if(ownerData.ID == EntId.Invalid)
				return;
			
			// Remove the status effect from the owner
			ownerData.RemoveStatusEffect(ref effectData);
		}
		public bool Active { get; set; } = true;
		public SystemGroup Group { get; } = GameSystemsGroup.GAME_LOGIC_SYSTEM;
	}
}