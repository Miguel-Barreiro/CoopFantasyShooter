using System;
using Core.Model.Data;
using FixedPointy;
using Game.Player;
using Game.Spells;
using Game.Spells.Bullet;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace Global
{
	public class GameplayConfig : DataConfig
	{
		[SerializeField]
		public EnviromentAttributes EnviromentAttributes = new EnviromentAttributes();
		[SerializeField]
		public PlayerAttributes PlayerAttributes = new PlayerAttributes();
		[SerializeField]
		public BasicSpellsAttributes BasicSpellsAttributes = new BasicSpellsAttributes();
	}


	[Serializable]
	public class EnviromentAttributes
	{
		[SerializeField] private NavMeshSurface _NavMeshPrefab;
		public NavMeshSurface NavMeshPrefab => _NavMeshPrefab;

	}


	[Serializable]
	public class BasicSpellsAttributes
	{
		[SerializeField]
		private BulletView _BulletPrefab;
		public BulletView BulletPrefab => _BulletPrefab;
		
		[SerializeField] private float _BulletSize = 0.1f;
		public float BulletSize => _BulletSize;
		
		[SerializeField] private float _BulletSpeed = 500f;
		public float BulletSpeed => _BulletSpeed;

		[SerializeField] private SpellConfig _bulletSpellConfig = null!;
		public  SpellConfig BulletSpellConfig => _bulletSpellConfig;
	}


	[Serializable]
	public class PlayerAttributes
	{
		private const float SPEED_MULT = 300f;
		// private const float SPEED_MULT = 10f;
		
		
		[SerializeField] private PlayerView _PlayerPrefab;
		public PlayerView PlayerPrefab => _PlayerPrefab;
		
		[Range(0, 10)]
		[SerializeField] private float _PlayerSpeed = 1;
		public float PlayerSpeed => _PlayerSpeed * SPEED_MULT;
		[Range(0, 2)]
		[SerializeField] private float _Size = 0.3f;
		public float Size => _Size;
	}
}