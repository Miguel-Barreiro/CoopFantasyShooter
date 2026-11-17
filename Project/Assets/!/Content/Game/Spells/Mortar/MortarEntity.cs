using System.Runtime.InteropServices;
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
		public MortarEntity(SpellConfig dataConfig) : base(dataConfig) { }
	}


	[StructLayout(LayoutKind.Auto)]
	public struct MortarData : IComponentData
	{
		public EntId ID { get; set; }
		
		public Fix ElapsedTime;
		public Fix TimeToImpact;

		public Fix Radius; 
		public Vector3 OriginPosition;
		public Vector3 TargetPosition;

		public ulong TargetTags;
		
		public void Init()
		{
			ElapsedTime = Fix.Zero;
			TargetTags = (ulong)GameTag.None;
		}
	}

	public interface IMortar : Component<MortarData> { }
	
	
	public sealed class MortarSystem : UpdateComponents<MortarData>
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
					
					SpellsLogic.HitSpellInCirle(mortarID, position, Vector3.zero, 
												componentData.TargetTags, EntId.Invalid, 
												radius);
					
					EntitiesContainer.DestroyEntity(mortarID);
					// GameLogic.SpawnExplosion(mortarID, componentData.TargetPosition, radius, damage);
					// MortarContainer.Remove(mortarID);
				}
			}
		}

		public bool Active { get; set; } = true;
		public SystemGroup Group { get; } = GameSystemsGroup.GAME_LOGIC_SYSTEM;
	}
}