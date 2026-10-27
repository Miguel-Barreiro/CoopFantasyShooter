using System;
using Core.Model.Data;
using FixedPointy;
using Game.Enemies;
using Game.Enemies.EnenmySpawners;
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
		private const uint MINOR_EXTRA = 10;
			
		[SerializeField][Range(1, 500)] private uint _maxBullets = 100;
		public uint MaxBullets => _maxBullets;
		
		[SerializeField][Range(1, 1000)] private uint _maxEnemies = 100;
		public uint MaxEnemies => _maxEnemies;
	
	
		[SerializeField][Range(1, 10)] private uint _maxPlayers = 4;
		public uint MaxPlayers => _maxPlayers;
		
		[SerializeField][Range(1, 100)] private uint _maxSpells = 100;
		public uint MaxSpells => _maxSpells;

		[SerializeField][Range(1, 100)] private uint _maxAbilitiesPerEntity = 10;
		public uint MaxAbilities => _maxAbilitiesPerEntity * (MaxPlayers + MaxEnemies + MINOR_EXTRA);

		
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
		private float _MinDistanceToTarget = 0.3f;
		public float MinDistanceToTarget => _MinDistanceToTarget;
		
		[SerializeField]
		[Range(3f, 25f)]
		private float _SafeDistanceToTarget = 10f;
		public float SafeDistanceToTarget => _SafeDistanceToTarget;

		
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

		[SerializeField] private AbilityConfig _bulletAbilityConfig = null!;
		public AbilityConfig BulletAbilityConfig => _bulletAbilityConfig;
		
		
		[SerializeField] private SpellConfig _bulletSpellConfig = null!;
		public SpellConfig BulletSpellConfig => _bulletSpellConfig;

		
	}

	[Serializable]
	public sealed class GamelogicAttributes
	{
		[SerializeField]
		[Range(1, 350)]
		private float _EnemySpawnCooldown = 0.02f;
		public float EnemySpawnCooldown => _EnemySpawnCooldown;

		[SerializeField] private EnemyConfig _LowLevelEnemyConfig;
		public EnemyConfig LowLevelEnemyConfig => _LowLevelEnemyConfig;

		[SerializeField] private EnemySpawnerConfig _LowLevelSpawnConfig;
		public EnemySpawnerConfig LowLevelSpawnConfig => _LowLevelSpawnConfig;

		
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