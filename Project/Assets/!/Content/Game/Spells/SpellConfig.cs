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


		[SerializeField] private VFXConfig _ImpactVFX;
		public VFXConfig ImpactVFX => _ImpactVFX;
		

		[SerializeField] private VFXConfig _OriginVFX;
		public VFXConfig OriginVFX => _OriginVFX;
		
	}
	
	
	
	public enum SpellType
	{
		Projectile = 0,
		Mortar = 1,
	}
}