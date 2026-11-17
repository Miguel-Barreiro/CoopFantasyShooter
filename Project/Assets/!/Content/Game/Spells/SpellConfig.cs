using Game.Logic;
using UnityEngine;

namespace Game.Spells
{
	public sealed class SpellConfig : BaseVSEntityConfig
	{
		[SerializeField] private SpellType _SpellType;
		public SpellType SpellType => _SpellType;
		
		[SerializeField] private GameTag _HitTags;
		public GameTag HitTags => _HitTags;

		[SerializeField] private SpellView _SpellPrefab;
		public SpellView SpellPrefab => _SpellPrefab;

	}
	
	
	public enum SpellType
	{
		Projectile = 0,
		Mortar = 1,
	}
}