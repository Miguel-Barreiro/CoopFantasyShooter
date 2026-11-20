using Game.Logic;
using Game.VFX;
using UnityEngine;

namespace Game.Spells
{
	public class SpellConfig : BaseVSEntityConfig
	{
		[SerializeField] private SpellType _SpellType;
		public SpellType SpellType => _SpellType;
		
		[SerializeField] private GameTag _HitTags;
		public GameTag HitTags => _HitTags;

		[SerializeField] private SpellView _SpellPrefab;
		public SpellView SpellPrefab => _SpellPrefab;


		[SerializeField] private VFXView _ImpactVFXPrefab;
		public VFXView ImpactVFXPrefab => _ImpactVFXPrefab;
		

		[SerializeField] private VFXView _OriginVFXPrefab;
		public VFXView OriginVFXPrefab => _OriginVFXPrefab;
		
	}
	
	
	
	public enum SpellType
	{
		Projectile = 0,
		Mortar = 1,
	}
}