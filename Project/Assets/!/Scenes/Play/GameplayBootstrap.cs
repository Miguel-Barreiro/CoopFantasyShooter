using Com.LuisPedroFonseca.ProCamera2D;
using Core.Initialization;
using Core.Model.ModelSystems;
using Core.Zenject.Source.Main;
using Game;
using Game.Input;
using Game.Player;
using UnityEngine;

namespace Scenes.Play
{
	public sealed class GameplayBootstrap : SceneBootstrap
	{

		// [SerializeField] private LocalMultiplayerInputManager LocalMultiplayerInputManager;
		[SerializeField] private Camera Camera;
		
		private GameplayInstaller _installer;

		public override SystemsInstallerBase GetLogicInstaller()
		{
			if(_installer == null)
				_installer = new GameplayInstaller(Container, 
													// LocalMultiplayerInputManager, 
													Camera);

			return _installer;
		}
	}
	
	public sealed class GameplayInstaller : SystemsInstallerBase
	{
		// private readonly LocalMultiplayerInputManager LocalMultiplayerInputManager;
		private readonly Camera Camera;

		public GameplayInstaller(DiContainer container,
								// LocalMultiplayerInputManager localMultiplayerInputManager, 
								Camera camera) 
			: base(container)
		{
			// LocalMultiplayerInputManager = localMultiplayerInputManager;
			Camera = camera;
		}

		public override void SetupConfigurations()
		{
			
		}

		protected override void InstallSystems()
		{
			BindInstance(Camera);
			ProCamera2D proCamera2D = Camera.GetComponent<ProCamera2D>();
			BindInstance(proCamera2D);

			BindInstance(new StartGameSystem());
			BindInstance(new PlayerSystem());
			BindInstance(new GameLogic());
			// BindInstance(LocalMultiplayerInputManager);
			
		}

		protected override void AddDebugOptions()
		{
		}

		public override void ResetComponentContainers(DataContainersController dataController)
		{
			
		}
	}
}