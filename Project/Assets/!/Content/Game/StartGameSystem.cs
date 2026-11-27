using Core.Initialization;
using Core.Model;
using Core.Systems;
using Cysharp.Threading.Tasks;
using Game.Logic;
using Game.Player;
using Global;
using Input;
using Zenject;

namespace Game
{
	public class StartGameSystem : IStartSystem
	{
		[Inject] private readonly ObjectBuilder ObjectBuilder = null!;
		[Inject] private readonly GameConfig GameConfig = null!;
		[Inject] private readonly PlayerLogic PlayerLogic = null!;
		[Inject] private readonly GameLogic GameLogic = null!;



		[Inject] private readonly LocalMultiplayerInputManager LocalMultiplayerInputManager = null!;
		[Inject] private readonly BasicCompContainer<GameData> GameContainer = null!;

		
		public void StartSystem()
		{
			_ = UniTask.RunOnThreadPool(Action);

			async UniTask Action()
			{
				await UniTask.Delay(1000);
				PlayerLogic.SpawnPlayers(LocalMultiplayerInputManager.Players);
				await UniTask.Delay(5000);
			}

			
			// LocalMultiplayerInputManager localMultiplayerInputManager = ObjectBuilder.Instantiate(GameConfig.LocalMultiplayerInputManagerPrefab);
			//TODO: we need to make the gameobject persistent across scenes,
			//but we need to make sure it doesn't get duplicated when we reload the scene.


		}
	}
}