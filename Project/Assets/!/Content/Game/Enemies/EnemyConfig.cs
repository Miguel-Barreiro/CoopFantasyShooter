using Game.Logic;
using UnityEngine;

namespace Game.Enemies
{
	public sealed class EnemyConfig : BaseEntityConfig
	{
		[SerializeField] private EnemyView _EnemyPrefab;
		public EnemyView EnemyPrefab => _EnemyPrefab;

		[SerializeField] private EnemyBehaviourType _behaviourType;
		public EnemyBehaviourType BehaviourType => _behaviourType;

		[SerializeField]
		[Range(0.1f, 1f)]
		private float _AttackFrameNormalizedTime = 0.5f;
		public float AttackFrameNormalizedTime => _AttackFrameNormalizedTime;

	}
	
	public enum EnemyBehaviourType
	{
		None,
		GoblinPawn,
		GoblinSupport,
	}
}