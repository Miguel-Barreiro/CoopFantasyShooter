using Com.LuisPedroFonseca.ProCamera2D;
using Core.Initialization;
using Core.Model.ModelSystems;
using Core.Systems;
using Core.Zenject.Source.Main;
using Game;
using Game.BehaviourTree;
using Game.Components;
using Game.Enemies;
using Game.Enemies.EnemyAbilities;
using Game.Input;
using Game.Logic;
using Game.Player;
using Game.Spells;
using Game.Spells.Bullet;
using Game.StatusEffects;
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
			
			BindInstance(new WorldEntitySystem());
			BindInstance(new StatusEffectSystem());
			BindInstance(new StatusEffectViewSystem());
			
			BindInstance(new PlayerSystem());
			BindInstance(new PlayerViewSystem());
			BindInstance(new GameLogic());

			BindInstance(new PlayerAbilitiesSystem());
			BindInstance(new BulletSystem());
			
			BindInstance(new EnemySpawnerSystem());
			BindInstance( new GameSetupSystem());
			
			BindInstance(new EnemySystem());
			BindInstance(new EnemyViewSystem());
			BindInstance(new BehaviourNodesContainer());
			BindInstance(new BehaviourTreeSystem());
			
			// InstantiatePrefabAndBind(GameplayConfig.EnviromentAttributes.NavMeshPrefab);
			
			// BindInstance(LocalMultiplayerInputManager);
			
			
			BindInstance(new DeadBodyViewSystem());
			BindInstance(new EnemyAbilitySystem());
			

			BindInstance(NavMeshSurface);
			
			BindInstance(new GameMemoryArena());
			
			//DEBUG
			BindInstance(new DebugGameplaySystem());
			
		}

		protected override void AddDebugOptions()
		{
			
		}

		
		
		public override void ResetComponentContainers(DataContainersController dataController)
		{

			#region SPELLS
			dataController.ResizeComponentsContainer<BulletData>(GameplayConfig.MaxBullets);
			dataController.ResizeComponentsContainer<PlayerData>(GameplayConfig.MaxPlayers);
			dataController.ResizeComponentsContainer<PlayerAbilityData>(GameplayConfig.MaxAbilities);
			#endregion

			
			
			#region ENEMIES
			dataController.ResizeComponentsContainer<EnemyData>(GameplayConfig.MaxEnemies);
			dataController.ResizeComponentsContainer<BehaviourNodeData>(GameplayConfig.MaxEnemies);
			dataController.ResizeComponentsContainer<BehaviourAgentData>(GameplayConfig.MaxEnemies);
			dataController.ResizeComponentsContainer<DeadBodyData>(GameplayConfig.MaxEnemies);
			#endregion

			#region Status Effects

			uint maxStatusEffects = (GameplayConfig.MaxEnemies + GameplayConfig.MaxPlayers) * 
										StatusEffectOwnerData.MAX_STATUS_EFFECTS_APPLIED; 
			
			dataController.ResizeComponentsContainer<StatusEffectData>(maxStatusEffects);
			dataController.ResizeComponentsContainer<StatusEffectOwnerData>(maxStatusEffects);

			#endregion
			
			uint totalEntities = GameplayConfig.MaxEnemies +
								GameplayConfig.MaxPlayers +
								GameplayConfig.MaxBullets +  
								GameplayConfig.MaxSpells + 
								GameplayConfig.MaxAbilities;

			dataController.ResizeComponentsContainer<WorldEntityData>(totalEntities);
			dataController.ResizeComponentsContainer<HierarchyData>(totalEntities);
			dataController.ResizeComponentsContainer<SimulationData>(totalEntities);

		}
	}
}