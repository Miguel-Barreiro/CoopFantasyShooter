using Core.Model.Data;
using Game.Logic;
using UnityEngine;

namespace Game.Spells
{
	public sealed class SpellConfig : BaseVSEntityConfig
	{
		[Header("LOGIC")] 
		[SerializeField] private SpellType _SpellType;
		public SpellType SpellType => _SpellType;

		
		[Header("VIEW")] 
		[SerializeField] private SpellView _SpellPrefab;
		public SpellView SpellPrefab => _SpellPrefab;
		
	}
	
	
	public enum SpellType
	{
		Projectile = 0,
	}
}