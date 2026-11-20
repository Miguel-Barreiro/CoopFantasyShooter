using System.Runtime.InteropServices;
using Core.Events;
using Core.Model;
using Core.Model.ModelSystems;
using Core.Systems;
using Core.View;
using FixedPointy;
using Game.Components;
using Game.Logic;
using UnityEngine;
using Zenject;

namespace Game.Spells.Mortar
{
	public class MortarEntity : SpellEntity,
								IMortar, 
								ISimulation, 
								IWorldEntity
	{
		public MortarEntity(SpellConfig dataConfig, Vector3 origin, Vector3 targetPosition) : base(dataConfig)
		{
			ref MortarData mortarData = ref GetComponent<MortarData>();
			mortarData.OriginPosition = origin;
			mortarData.TargetPosition = targetPosition;
			mortarData.TargetPosition.y = 0f;
			mortarData.TargetTags = (ulong) dataConfig.HitTags;
			mortarData.DataConfig = dataConfig;
		}
	}


	[StructLayout(LayoutKind.Auto)]
	public struct MortarData : IComponentData
	{
		public EntId ID { get; set; }
		
		public Fix Radius; 
		public ulong TargetTags;
		public Fix ElapsedTime;
		public Fix TimeToImpact;

		public Vector3 OriginPosition;
		public Vector3 TargetPosition;

		public SpellConfig DataConfig;
		
		public void Init()
		{
			Radius = Fix.One;
			TimeToImpact = Fix.One;
			ElapsedTime = Fix.Zero;
			TargetTags = (ulong)GameTag.None;
		}
	}

	public interface IMortar : Component<MortarData> { }
	
	
	[OnCreateComponentProperties(Priority = SystemPriority.Early)]
	public sealed class MortarSystem : UpdateComponents<MortarData>, OnCreateComponent<MortarData>
	{
		[Inject] private readonly BasicCompContainer<MortarData> MortarContainer = null!;
		[Inject] private readonly ViewEntitiesContainer ViewEntitiesContainer = null!;
		[Inject] private readonly GameStatsContainer GameStatsContainer = null!;
		[Inject] private readonly StatsSystem StatsSystem = null!;
		[Inject] private readonly GameLogic GameLogic = null!;
		[Inject] private readonly SpellsLogic SpellsLogic = null!;
		
		public void UpdateComponents(float deltaTime)
		{
			uint topIndex = MortarContainer.TopEmptyIndex;
			for (int i = 0; i < topIndex; i++)
			{
				ref MortarData componentData = ref MortarContainer.Components[i];
				EntId mortarID = componentData.ID;
				
				componentData.ElapsedTime += deltaTime;
				if(componentData.ElapsedTime >= componentData.TimeToImpact)
				{
					Vector3 position = componentData.TargetPosition;
					float radius = (float) componentData.Radius;
					
					EntityEventQueue.Execute<OnMortarImpactEvent>(mortarID);
					
					SpellsLogic.HitSpellInCirle(mortarID, position, Vector3.zero, 
												componentData.TargetTags, EntId.Invalid, 
												radius);
					EntitiesContainer.DestroyEntity(mortarID);
				}
			}
		}

		public void OnCreateComponent(EntId newComponentId)
		{
			Fix size = StatsSystem.GetStatValue(newComponentId, GameStatsContainer.Size);
			Fix castingTime = StatsSystem.GetStatValue(newComponentId, GameStatsContainer.CastingTime);
			ref MortarData mortarData = ref MortarContainer.GetComponent(newComponentId);
			mortarData.TimeToImpact = castingTime;
			mortarData.ElapsedTime = Fix.Zero;
			mortarData.Radius = size;
		}
		
		
		public bool Active { get; set; } = true;
		public SystemGroup Group { get; } = GameSystemsGroup.GAME_LOGIC_SYSTEM;
	}
}