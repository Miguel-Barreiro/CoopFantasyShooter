using Com.LuisPedroFonseca.ProCamera2D;
using Core.Initialization;
using Core.Model.ModelSystems;
using Core.Zenject.Source.Main;
using Game;
using Game.Enemies;
using Game.Input;
using Game.Logic;
using Game.Player;
using Game.Spells;
using Game.Spells.Bullet;
using Global;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace Scenes.Play
{
	public sealed class GameplayBootstrap : SceneBootstrap
	{

		// [SerializeField] private LocalMultiplayerInputManager LocalMultiplayerInputManager;
		[SerializeField] private Camera Camera;
		[SerializeField] private GameplayConfig GameplayConfig;
		[SerializeField] private NavMeshSurface NavMeshSurface;
		
		
		private GameplayInstaller _installer;

		public override SystemsInstallerBase GetLogicInstaller()
		{
			if(_installer == null)
				_installer = new GameplayInstaller(Container, 
													Camera,
													GameplayConfig,
													NavMeshSurface);

			return _installer;
		}
	}
	
	public sealed class GameplayInstaller : SystemsInstallerBase
	{
		// private readonly LocalMultiplayerInputManager LocalMultiplayerInputManager;
		private readonly Camera Camera;
		private readonly GameplayConfig GameplayConfig;
		private readonly NavMeshSurface NavMeshSurface;
		public GameplayInstaller(DiContainer container,
								// LocalMultiplayerInputManager localMultiplayerInputManager, 
								Camera camera, GameplayConfig gameplayConfig, NavMeshSurface navMeshSurface) 
			: base(container)
		{
			// LocalMultiplayerInputManager = localMultiplayerInputManager;
			Camera = camera;
			GameplayConfig = gameplayConfig;
			NavMeshSurface = navMeshSurface;
		}


		public override void SetupConfigurations()
		{
			BindInstance(GameplayConfig);
		}

		protected override void InstallSystems()
		{
			BindInstance(Camera);
			ProCamera2D proCamera2D = Camera.GetComponent<ProCamera2D>();
			BindInstance(proCamera2D);

			BindInstance(new StartGameSystem());
			BindInstance(new PlayerSystem());
			BindInstance(new GameLogic());

			BindInstance(new SpellSystem());
			BindInstance(new BulletSystem());
			
			BindInstance(new EnemySpawnerSystem());
			BindInstance( new GameSetupSystem());
			
			// InstantiatePrefabAndBind(GameplayConfig.EnviromentAttributes.NavMeshPrefab);
			
			// BindInstance(LocalMultiplayerInputManager);
			

			BindInstance(NavMeshSurface);
			
			//DEBUG
			BindInstance(new DebugGameplaySystem());
		}

		protected override void AddDebugOptions()
		{
			
		}

		
		
		public override void ResetComponentContainers(DataContainersController dataController)
		{
			dataController.ResizeComponentsContainer<BulletData>(GameplayConfig.MaxBullets);
			dataController.ResizeComponentsContainer<PlayerData>(GameplayConfig.MaxPlayers);
			dataController.ResizeComponentsContainer<SpellData>(GameplayConfig.MaxSpells);
			dataController.ResizeComponentsContainer<EnemyData>(GameplayConfig.MaxEnemies);
		}
	}
}