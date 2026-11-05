using System.Collections.Generic;
using FixedPointy;
using Game.Logic;
using Game.Spells;
using UnityEngine;

namespace Game.Enemies
{
	public sealed class EnemyConfig : BaseEntityConfig
	{
		[SerializeField] private List<AbilityConfig> _abilities;
		public List<AbilityConfig> Abilities => _abilities;
		
		
		[SerializeField] private EnemyView _EnemyPrefab;
		public EnemyView EnemyPrefab => _EnemyPrefab;

		[SerializeField] private EnemyBehaviourType _behaviourType;
		public EnemyBehaviourType BehaviourType => _behaviourType;

		[SerializeField]
		[Range(0.1f, 1f)]
		private float _AttackFrameNormalizedTime = 0.5f;
		public float AttackFrameNormalizedTime => _AttackFrameNormalizedTime;

		[SerializeField]
		[Range(0.1f, 2f)]
		private float _size = 0.4f;
		public float Size => _size;

		
	}
	
	public enum EnemyBehaviourType
	{
		None,
		GoblinPawn,
		GoblinSupport,
	}
}