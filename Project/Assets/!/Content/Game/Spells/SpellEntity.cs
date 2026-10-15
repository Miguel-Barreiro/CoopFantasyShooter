using System.Runtime.InteropServices;
using Core.Events;
using Core.Model;
using Core.Model.ModelSystems;
using Core.Systems;
using FixedPointy;
using Game.Player;
using Global;
using UnityEngine;
using Zenject;

namespace Game.Spells
{
	public sealed class SpellEntity : Entity,  
									ISpell,
									IHierarchyEntity
	{ }


	[StructLayout(LayoutKind.Auto)]
	public struct SpellData : IComponentData
	{
		public EntId ID { get; set; }

		public SpellState State;
		
		public enum SpellState
		{
			Ready,
			Cast, 
			Recover
		}

		public void Init()
		{
			State = SpellState.Ready;
		}
	}

	public interface ISpell : Component<SpellData>, IHierarchyEntity { }
	

	public sealed class SpellSystem : UpdateComponents<SpellData>
	{
		[Inject] private readonly StatsSystem StatsSystem = null!;
		[Inject] private readonly GameStatsContainer GameStatsContainer = null!;
		// [Inject] private readonly GameplayConfig GameplayConfig = null!;
		[Inject] private readonly GameLogic GameLogic = null!;
		[Inject] private readonly BasicCompContainer<SpellData> SpellContainer = null!;

		
		public void TryCastSpell(EntId spellID, EntId playerId)
		{
			ref SpellData spellData = ref SpellContainer.GetComponent(spellID);
			if (spellData.State == SpellData.SpellState.Ready)
			{
				StatsSystem.ResetDepletedValueToMax(spellID, GameStatsContainer.CastingTime);
				spellData.State = SpellData.SpellState.Cast;

				CastSpellEvent castSpellEvent = EntityEventQueue.Execute<CastSpellEvent>(spellID);
				castSpellEvent.PlayerID = playerId;
				
				Fix castingInstances = StatsSystem.ChangeDepletedValue(spellID, GameStatsContainer.CastingMultiplier, -1);
				if (castingInstances == 0)
				{
					StatsSystem.ResetDepletedValueToMax(spellID, GameStatsContainer.Cooldown);
					StatsSystem.ResetDepletedValueToMax(spellID, GameStatsContainer.CastingMultiplier);
					StatsSystem.ResetDepletedValueToMin(spellID, GameStatsContainer.CastingTime);
					spellData.State = SpellData.SpellState.Recover;
					EntityEventQueue.Execute<OnSpellResetCooldownEvent>(spellID);
				}

			}
		}


		public void UpdateComponents(float deltaTime)
		{
			uint topIndex = SpellContainer.TopEmptyIndex;
			for (int i = 0; i < topIndex; i++)
			{
				ref SpellData spellData = ref SpellContainer.Components[i];
				switch (spellData.State)
				{
					case SpellData.SpellState.Cast:
						Fix newCastingTime = StatsSystem.ChangeDepletedValue(spellData.ID, GameStatsContainer.CastingTime, -deltaTime);
						if(newCastingTime == 0)
							spellData.State = SpellData.SpellState.Ready;

						break;
					
					case SpellData.SpellState.Recover:
						Fix newCooldown = StatsSystem.ChangeDepletedValue(spellData.ID, GameStatsContainer.Cooldown, -deltaTime);
						if (newCooldown == 0)
							EntityEventQueue.Execute<ReadySpellEvent>(spellData.ID);
						break;
					
					// case SpellData.SpellState.Ready:
					// 	StatsSystem.ChangeDepletedValue(spellData.ID, GameStatsContainer.CastingTime, -deltaTime);
						
				}
				// StatsSystem.ChangeDepletedValue(spellData.ID, GameStatsContainer.CastingTime, deltaTime);
			}		

		}
		
		
		public bool Active { get; set; } = true;
		public SystemGroup Group { get; } = GameSystemsGroup.GAME_LOGIC_SYSTEM;
	}

}