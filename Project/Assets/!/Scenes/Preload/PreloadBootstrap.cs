using Global.Logic;
using Core.Initialization;
using Core.Model.ModelSystems;
using Core.Systems;
using Core.View.UI;
using Core.Zenject.Source.Main;
using Global;
using Zenject;

namespace Game.Bootstrap
{
	public class PreloadBootstrap : SceneBootstrap
	{
		
		private PreloadInstaller _installer = null;
		public override SystemsInstallerBase GetLogicInstaller()
		{
			if (_installer == null)
				_installer = new PreloadInstaller(Container);

			return _installer;
		}
	}

	public sealed class PreloadInstaller : SystemsInstallerBase
	{
		
		public PreloadInstaller(DiContainer container) : base(container)
		{
		
		}

		public override void SetupConfigurations()
		{
		
		}

		protected override void InstallSystems()
		{
			 BindInstance(new PreloadViewController());
		}

		protected override void AddDebugOptions()
		{
		}

		public override void ResetComponentContainers(DataContainersController dataController)
		{
			
		}
	}

	public class PreloadViewController : IStartSystem 
	{
		[Inject] private readonly UIRoot CLASS = null!;
		[Inject] private readonly ScenesController ScenesController = null!;
		
		public void StartSystem()
		{
			ScenesController.SwitchScene(SceneNames.MAIN_MENU);
		}
	}




	
}
