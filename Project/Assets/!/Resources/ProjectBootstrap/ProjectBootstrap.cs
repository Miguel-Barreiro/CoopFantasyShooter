using Core.Initialization;
using Core.Model.ModelSystems;
using Core.View.UI;
using Core.Zenject.Source.Main;
using Game;
using UnityEngine;
using Zenject;

namespace Global.Logic
{
    public sealed class ProjectBootstrap : RuntimeProjectBootstrap
    {

        [SerializeField] private MenusConfig MenusConfig = null!;
        [SerializeField] private GameConfig GameConfig = null!;
        [SerializeField] private GameStatsContainer GameStatsContainer;
        
        
        // [SerializeField] private BouncyCoreStatsContainer CoreStatsContainer;
        // [SerializeField] private GameplayOptionsListConfig GameplayOptionsListConfig = null!;
        // [SerializeField] private LevelsConfig LevelsConfig; 
        // [SerializeField] private GamePrefabsConfig GamePrefabsConfig;
        
        private GameProjectInstaller _installer;
        public override SystemsInstallerBase GetLogicInstaller()
        {
            if(_installer == null)
            {
                _installer = new GameProjectInstaller(Container, MenusConfig, GameConfig, GameStatsContainer);
                // , CoreStatsContainer, 
                //     GameplayOptionsListConfig,
                //     LevelsConfig, GamePrefabsConfig);
            }

            return _installer;
        }
        
        
    }

    public sealed class GameProjectInstaller : SystemsInstallerBase
    {
        private MenusConfig _menusConfig;
        private GameConfig _gameConfig;
        private GameStatsContainer _gameStatsContainer;

        public GameProjectInstaller(DiContainer container, 
                                    MenusConfig menusConfig, 
                                    GameConfig gameConfig,
                                    GameStatsContainer gameStatsContainer)
            : base(container)
        {
            _gameStatsContainer = gameStatsContainer;
            _gameConfig = gameConfig;
            _menusConfig = menusConfig;
        }

        public override void SetupConfigurations()
        {
            // BouncyCoreStatsContainer bouncyCoreStatsContainer = GetSystem<BouncyCoreStatsContainer>();
            // TimerSystem timerSystem = GetSystem<TimerSystem>();
            // timerSystem.SetGlobalTimerScalerStat(bouncyCoreStatsContainer.SlowMotionScale);
        }

        protected override void InstallSystems()
        {
            BindInstance<GameStatsContainer>(_gameStatsContainer);
            BindInstance<GameConfig>(_gameConfig);
            BindInstance<MenusConfig>(_menusConfig);

            
            
//             PreConfigGameplayOptionsSystem preConfigGameplayOptionsSystem = new PreConfigGameplayOptionsSystem();
//             BindInstance(preConfigGameplayOptionsSystem);
//
//             GameplayOptions gameplayOptions = new GameplayOptions();
//             BindInstance(gameplayOptions);
//             
//             InputOptions inputOptions = new InputOptions();
//             BindInstance(inputOptions);
//             
// #if !DISABLE_SRDEBUGGER
//             SRDebug.Instance.AddOptionContainer(gameplayOptions);
//             SRDebug.Instance.AddOptionContainer(preConfigGameplayOptionsSystem);
//             SRDebug.Instance.AddOptionContainer(inputOptions);
// #endif
//             
//             BindInstance(CoreStatsContainer);
//             
//             //CONFIG
//             BindInstance(LevelsConfig);
//             BindInstance(GameplayOptionsListConfig);
//             BindInstance(GamePrefabsConfig);
//             
//             //SAVE
//             BindInstance<SaveGameSystem>(new SaveGameSystemImplementation());
//             BindInstance<LevelsSavegameSystem>(new LevelsSavegameSystemImplementation());
//             
//             
//             
//             //MODEL
//             BindInstance(new LevelsLogic());
//             BindInstance(new MainMenuModel());
//             BindInstance<NamedEntitiesSystem>(new NamedEntitiesSystemImplementation());
//             BindInstance(new NamedEntitiesSystemModel());
//             
//             
//
//             // DebugManager.instance.displayEditorUI
//             DebugManager.instance.displayEditorUI = false;
            // UnityEngine..Rendering..DebugManager.instance.enableRuntimeUI = false;
            // UnityEngine.Rendering.DebugManager.instance.enableRuntimeUI = false;
            Application.targetFrameRate = 60;
        }

        protected override void AddDebugOptions()
        {
            
        }
        public override void ResetComponentContainers(DataContainersController dataContainersController) { }
        
    }
    
//     public sealed class BouncyProjectInstaller : SystemsInstallerBase
//     {
//         private readonly BouncyCoreStatsContainer CoreStatsContainer;
//         private readonly GameplayOptionsListConfig GameplayOptionsListConfig;
//         private readonly LevelsConfig LevelsConfig;
//         private readonly GamePrefabsConfig GamePrefabsConfig;
//         
//         public BouncyProjectInstaller(DiContainer container,
//                                       BouncyCoreStatsContainer coreStatsContainer,
//                                       GameplayOptionsListConfig gameplayOptionsListConfig,
//                                       LevelsConfig levelsConfig, 
//                                       GamePrefabsConfig gamePrefabsConfig)
//             : base(container)
//         {
//             CoreStatsContainer = coreStatsContainer;
//             GameplayOptionsListConfig = gameplayOptionsListConfig;
//             LevelsConfig = levelsConfig;
//             GamePrefabsConfig = gamePrefabsConfig;
//         }
//
//         public override void SetupConfigurations()
//         {
//             BouncyCoreStatsContainer bouncyCoreStatsContainer = GetSystem<BouncyCoreStatsContainer>();
//             TimerSystem timerSystem = GetSystem<TimerSystem>();
//             timerSystem.SetGlobalTimerScalerStat(bouncyCoreStatsContainer.SlowMotionScale);
//         }
//
//         protected override void InstallSystems()
//         {
//             PreConfigGameplayOptionsSystem preConfigGameplayOptionsSystem = new PreConfigGameplayOptionsSystem();
//             BindInstance(preConfigGameplayOptionsSystem);
//
//             GameplayOptions gameplayOptions = new GameplayOptions();
//             BindInstance(gameplayOptions);
//             
//             InputOptions inputOptions = new InputOptions();
//             BindInstance(inputOptions);
//             
// #if !DISABLE_SRDEBUGGER
//             SRDebug.Instance.AddOptionContainer(gameplayOptions);
//             SRDebug.Instance.AddOptionContainer(preConfigGameplayOptionsSystem);
//             SRDebug.Instance.AddOptionContainer(inputOptions);
// #endif
//             
//             BindInstance(CoreStatsContainer);
//             
//             //CONFIG
//             BindInstance(LevelsConfig);
//             BindInstance(GameplayOptionsListConfig);
//             BindInstance(GamePrefabsConfig);
//             
//             //SAVE
//             BindInstance<SaveGameSystem>(new SaveGameSystemImplementation());
//             BindInstance<LevelsSavegameSystem>(new LevelsSavegameSystemImplementation());
//             
//             
//             
//             //MODEL
//             BindInstance(new LevelsLogic());
//             BindInstance(new MainMenuModel());
//             BindInstance<NamedEntitiesSystem>(new NamedEntitiesSystemImplementation());
//             BindInstance(new NamedEntitiesSystemModel());
//             
//             
//
//             // DebugManager.instance.displayEditorUI
//             DebugManager.instance.displayEditorUI = false;
//             // UnityEngine..Rendering..DebugManager.instance.enableRuntimeUI = false;
//             // UnityEngine.Rendering.DebugManager.instance.enableRuntimeUI = false;
//             Application.targetFrameRate = 60;
//         }
//         public override void ResetComponentContainers(DataContainersController dataContainersController) { }
//         
//     }
}
