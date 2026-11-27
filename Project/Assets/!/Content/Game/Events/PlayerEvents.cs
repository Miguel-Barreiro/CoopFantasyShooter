using Core.Events;
using Core.Initialization;
using Core.Model;
using FixedPointy;
using Game.Player;
using UnityEngine;
using Zenject;

namespace Game.Events
{
	
	public sealed class HitPlayerEvent : EntityEvent<HitPlayerEvent>
	{
		[Inject] private readonly StatsSystem StatsSystem = null!;
		[Inject] private readonly GameStatsContainer GameStatsContainer = null!;
		[Inject] private readonly GameLogic GameLogic = null!;

		
		// [Inject] private readonly PlayerContainer PlayerContainer = null!;
		// [Inject] private readonly GameLogic GameLogic = null!;

		public Fix Damage = 0f;
		
		public override void Execute()
		{
			StatsSystem.ChangeDepletedValue(EntityID, GameStatsContainer.Health, -Damage);
		}
	}

	public sealed class OnAllPlayersDeadEvent : Event<OnAllPlayersDeadEvent>
	{
		[Inject] private readonly ScenesController ScenesController = null!;
		[Inject] private readonly GameLogic GameLogic = null!;

		public override void Execute()
		{
			Debug.Log("All players are dead. Switching to end game scene.");
			// ScenesController.SwitchScene(SceneNames.MAIN_MENU);
			
			// EventQueue.Trigger<GoFromGameToMainEvent>();
		}
	}
	
	
	public sealed class OnPlayerDeathEvent : EntityEvent<OnPlayerDeathEvent>
	{
		[Inject] private readonly PlayerContainer PlayerContainer = null!;
		[Inject] private readonly GameLogic GameLogic = null!;

		[Inject] private readonly StatsSystem StatsSystem = null!;
		[Inject] private readonly GameStatsContainer GameStatsContainer = null!;
		

		public override void Execute()
		{

		}
	}
	
}