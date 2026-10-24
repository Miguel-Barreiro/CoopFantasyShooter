using Core.Model.Data;
using Core.Model.Stats;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game
{
	public sealed class GameStatsContainer : DataTable
	{

		
		[SerializeField] private StatConfig _CastingTime;
		public StatConfig CastingTime => _CastingTime;


		[SerializeField] private StatConfig _Cooldown;
		public StatConfig Cooldown => _Cooldown;
		
		
		[SerializeField] private StatConfig _CastingMultiplier;
		public StatConfig CastingMultiplier => _CastingMultiplier;
		
		
		[SerializeField] private StatConfig _Damage;
		public StatConfig Damage => _Damage;

		
		[SerializeField] private StatConfig _Health;
		public StatConfig Health => _Health;
		
		[SerializeField] private StatConfig _TurningSpeed;
		public StatConfig TurningSpeed => _TurningSpeed;
		
		[SerializeField] private StatConfig _Acerelation;
		public StatConfig Acerelation => _Acerelation;
		
		
		[SerializeField] private StatConfig _Speed;
		public StatConfig Speed => _Speed;
		
		
		[SerializeField] private StatConfig _AttackDistance;
		public StatConfig AttackDistance => _AttackDistance;
		
		[SerializeField] private StatConfig _AttackDuration;
		public StatConfig AttackDuration => _AttackDuration;

		[SerializeField] private StatConfig _AttackChargeDuration;
		public StatConfig AttackChargeDuration => _AttackChargeDuration;
		
		
		[SerializeField] private StatConfig _AttackRate;
		public StatConfig AttackRate => _AttackRate;

		[SerializeField] private StatConfig _CastDistance;
		public StatConfig CastDistance => _CastDistance;
		
	}
}