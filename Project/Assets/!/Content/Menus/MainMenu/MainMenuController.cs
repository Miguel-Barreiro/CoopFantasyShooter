using Core.Events;
using Core.Initialization;
using Core.Systems;
using Core.View.UI;
using Game;
using Global;
using Input;
using Zenject;

namespace Menus.MainMenu
{
	public sealed class MainMenuController : IStartSystem, IUpdateSystem, 
											IEventListener<MovePlayerEvent>, 
											IEventListener<OnAEvent>, 
											IEventListener<RecoverPlayerInputEvent>
	
	{
		[Inject] private readonly MenusConfig MenusConfig = null!;
		[Inject] private readonly UIRoot UIRoot = null!;
		[Inject] private readonly MainMenuMessenger MainMenuMessenger = null!;
		[Inject] private readonly ScenesController ScenesController = null!;
		
		private float _inputCooldown = 0.2f;

		public void StartSystem()
		{
			UIRoot.Show(MenusConfig.MainMenuUI);
			
			MainMenuMessenger.OnPlayButtonClicked += OnPlayButtonClicked;
			MainMenuMessenger.EnableGamepadTooltip();
		}

		private void OnPlayButtonClicked()
		{
			UIRoot.HideAll();
			ScenesController.SwitchScene(SceneNames.PLAY_SCENE);
		}

		public void OnEvent(MovePlayerEvent onEvent)
		{
			if (onEvent.MoveValue > 0.5f && _inputCooldown == 0f)
			{
				MainMenuMessenger.MoveSelection(onEvent.Direction.y > 0 ? -1 : 1);
				MainMenuMessenger.EnableGamepadTooltip();
				_inputCooldown = 0.2f;
			}
		}
		
		public void UpdateSystem(float deltaTime)
		{
			_inputCooldown -= deltaTime;
			if (_inputCooldown < 0)
			{
				_inputCooldown = 0;
			}
		}
		
		public void OnEvent(OnAEvent onEvent)
		{
			MainMenuMessenger.Select();
		}

		public void OnEvent(RecoverPlayerInputEvent onEvent)
		{
			MainMenuMessenger.EnableGamepadTooltip();
		}

		public bool Active { get; set; } = true;
		public SystemGroup Group { get; } = GameSystemsGroup.GAME_VIEW_SYSTEM;
	}
}