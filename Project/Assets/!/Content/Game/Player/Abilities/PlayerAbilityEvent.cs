using Core.Events;
using Core.Model;
using Core.View;
using Core.VSEngine;
using Game.Components;
using Global;
using UnityEngine;
using Zenject;

namespace Game.Player.Abilities
{
	
	public abstract class PlayerAbilityEvent<T> : EntityEvent<T> 
		where T : PlayerAbilityEvent<T>, new()
	{
		[Inject] protected readonly BasicCompContainer<PlayerAbilityData> AbilityContainer = null!;
		
	}
	
	
	public sealed class OnReadyPlayerAbilityEvent : PlayerAbilityEvent<OnReadyPlayerAbilityEvent>
	{
		[Inject] private readonly GameStatsContainer GameStatsContainer = null!;
		[Inject] private readonly StatsSystem StatsSystem = null!;
		
		public override void Execute()
		{
			StatsSystem.ResetDepletedValueToMin(EntityID, GameStatsContainer.CastingTime);
			ref PlayerAbilityData playerAbilityData = ref AbilityContainer.GetComponent(EntityID);
			playerAbilityData.State = AbilityState.Ready;
		}
	}
	
	// public sealed class OnAbilityResetCooldownEvent : AbilityEvent<OnAbilityResetCooldownEvent>, ILateEvent
	// {
	// 	[Inject] private readonly GameStatsContainer GameStatsContainer = null!;
	// 	[Inject] private readonly StatsSystem StatsSystem = null!;
	// 	
	// 	public override void Execute()
	// 	{
	// 		ref PlayerAbilityData playerAbilityData = ref AbilityContainer.GetComponent(EntityID);
	// 		playerAbilityData.State = AbilityState.Recover;
	// 	}
	// }
	
	
	public sealed class ActivateAbilityEvent : EntityEvent<ActivateAbilityEvent>
	{
		[VSField()]
		public Vector3 OriginPosition;

		[VSField()]
		public Vector3 Direction = Vector2.zero;
		
		[VSField()]
		public Vector3 TargetPosition = Vector2.negativeInfinity;
		
		[VSField()]
		public EntId CasterOwner = EntId.Invalid;
		
		[VSField()]
		public uint InstanceNumber = 0;
		
		
		public override void Execute()
		{
			
		}
	}
	
	
	
}