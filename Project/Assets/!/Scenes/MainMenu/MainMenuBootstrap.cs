using Core.Initialization;
using Core.Model.ModelSystems;
using Core.Zenject.Source.Main;
using Global;
using Menus.MainMenu;
using UnityEngine;

namespace Scenes.MainMenu
{
	public sealed class MainMenuBootstrap : SceneBootstrap
	{
		[SerializeField] private MenusConfig MenusConfig;
		
		private MainMenuInstaller _mainMenuInstaller;
		public override SystemsInstallerBase GetLogicInstaller()
		{
			if(_mainMenuInstaller == null)
				_mainMenuInstaller = new MainMenuInstaller(Container, MenusConfig);
			
			return _mainMenuInstaller;
		}
	}
	
	public sealed class MainMenuInstaller : SystemsInstallerBase
	{
		private readonly MenusConfig MenusConfig;

		public MainMenuInstaller(DiContainer container, MenusConfig menusConfig)
			: base(container)
		{
			MenusConfig = menusConfig;
		}
		
		public override void SetupConfigurations() { }
		
		protected override void InstallSystems()
		{
			MainMenuMessenger mainMenuMessenger = new MainMenuMessenger();
			BindInstance(mainMenuMessenger);
			RegisterUIScreenDefinition(MenusConfig.MainMenuUI, mainMenuMessenger);
			BindInstance(new MainMenuController());
			
		}

		protected override void AddDebugOptions()
		{
			
		}

		public override void ResetComponentContainers(DataContainersController dataController)
		{
			
			
		}
	}
}