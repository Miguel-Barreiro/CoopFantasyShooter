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
		
		[SerializeField] private StatConfig _Speed;
		public StatConfig Speed => _Speed;
		

	}
}