using System;
using Core.Model.Data;
using FixedPointy;
using Game.Enemies;
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
		[SerializeField] private uint _maxBullets = 100;
		public uint MaxBullets => _maxBullets;
		
		[SerializeField] private uint _maxEnemies = 100;
		public uint MaxEnemies => _maxEnemies;
	
	
		[SerializeField] private uint _maxPlayers = 4;
		public uint MaxPlayers => _maxPlayers;
		
		[SerializeField] private uint _maxSpells = 100;
		public uint MaxSpells => _maxSpells;

		[SerializeField] public EnemiesAttributes EnemiesAttributes = new EnemiesAttributes();
		[SerializeField] public EnviromentAttributes EnviromentAttributes = new EnviromentAttributes();
		[SerializeField] public PlayerAttributes PlayerAttributes = new PlayerAttributes();
		[SerializeField] public BasicSpellsAttributes BasicSpellsAttributes = new BasicSpellsAttributes();
		[SerializeField] public GamelogicAttributes GamelogicAttributes = new GamelogicAttributes();
	}

	
	[Serializable]
	public class EnemiesAttributes
	{
		[SerializeField]
		[Range(0.1f, 2f)]
		private float _DistanceToAttack = 1f;
		public float DistanceToAttack => _DistanceToAttack;
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
	public sealed class GamelogicAttributes
	{
		[SerializeField]
		[Range(1, 50)]
		private float _EnemySpawnCooldown = 0.02f;
		public float EnemySpawnCooldown => _EnemySpawnCooldown;

		[SerializeField] private EnemyConfig _LowLevelEnemyConfig;
		public EnemyConfig LowLevelEnemyConfig => _LowLevelEnemyConfig;

		[SerializeField][Range(5, 35)] private float _EnemySpawnDistance = 20f;
		public float EnemySpawnDistance => _EnemySpawnDistance;
	}
	

	[Serializable]
	public class PlayerAttributes
	{
		private const float SPEED_MULT = 300f;
		// private const float SPEED_MULT = 10f;
		
		
		[SerializeField] private PlayerView _PlayerPrefab;
		public PlayerView PlayerPrefab => _PlayerPrefab;
		
		[Range(0, 20)]
		[SerializeField] private float _PlayerSpeed = 1;
		public float PlayerSpeed => _PlayerSpeed * SPEED_MULT;
		[Range(0, 2)]
		[SerializeField] private float _Size = 0.3f;
		public float Size => _Size;
		[SerializeField]
		[Range(0, 200)]
		private int _Health = 100;
		public int Health => _Health;
	}
}