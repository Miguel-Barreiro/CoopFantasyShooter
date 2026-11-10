using Core.Initialization;
using Core.Systems;
using Core.View;
using Game.Input;
using Global;
using Zenject;

namespace Game
{
	public class StartGameSystem : IStartSystem
	{
		[Inject] private readonly ObjectBuilder ObjectBuilder = null!;
		[Inject] private readonly GameConfig GameConfig = null!;

		
		public void StartSystem()
		{
			LocalMultiplayerInputManager localMultiplayerInputManager = ObjectBuilder.Instantiate(GameConfig.LocalMultiplayerInputManagerPrefab);
			//TODO: we need to make the gameobject persistent across scenes,
			//but we need to make sure it doesn't get duplicated when we reload the scene.
		}
	}
}