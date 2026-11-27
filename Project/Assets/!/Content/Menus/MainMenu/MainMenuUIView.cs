using System;
using Core.Events;
using Core.Initialization;
using Core.View.UI;
using Input;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Menus.MainMenu
{
    public class MainMenuUIView : UIView<MainMenuMessenger>
    {

        [SerializeField] private Button[] _buttons;
        [SerializeField] private GameObject GamepadButtonTooltip;
        private int currentSelectedButton = 0;
        
        [SerializeField] private Button PlayButton;
        
        [Inject] private readonly EventQueue EventQueue = null!;
        [Inject] private readonly UIRoot UIRoot = null!;
        [Inject] private readonly ScenesController ScenesController = null!;
        
        private void OnPlayButtonClicked()
        {
            Messenger.PlayButtonClick();

            // EventQueue.Execute<MainMenuToGameEvent>();
            // MainMenuPlayButton.onClick.RemoveListener(OnPlayButtonClicked);
        }
        
        protected override void OnShow()
        {
            PlayButton.onClick.AddListener(OnPlayButtonClicked);
            GamepadButtonTooltip.SetActive(false);
            currentSelectedButton = -1;
        }


        protected override void OnHide()
        {
            PlayButton.onClick.RemoveListener(OnPlayButtonClicked);
        }

        protected override void OnUnregister(MainMenuMessenger uiMessenger)
        {
            uiMessenger.OnMoveSelection -= OnMoveSelection;
            uiMessenger.OnSelection -= OnSelect;
            uiMessenger.OnEnableGamepadTooltip -= OnEnableGamepadTooltip;
        }

        protected override void OnRegister(MainMenuMessenger uiMessenger)
        {
            uiMessenger.OnMoveSelection += OnMoveSelection;
            uiMessenger.OnSelection += OnSelect;
            uiMessenger.OnEnableGamepadTooltip += OnEnableGamepadTooltip;
        }

        private void OnEnableGamepadTooltip()
        {
            GamepadButtonTooltip.SetActive(true);
        }

        private void OnMoveSelection(int delta)
        {
            GamepadButtonTooltip.SetActive(true);
            Math.DivRem(currentSelectedButton + delta, _buttons.Length, out currentSelectedButton);
            UpdateSelectedButton();
        }
        
        private void UpdateSelectedButton()
        {
            for (int i = 0; i < _buttons.Length; i++)
            {
                if (currentSelectedButton == i)
                {
                    Button button = _buttons[i];
                    button.Select();
                    GamepadButtonTooltip.transform.SetParent(button.transform, false);
                }
            }
        }

        private void OnSelect()
        {
            if(currentSelectedButton < 0 || currentSelectedButton >= _buttons.Length)
                return;
            
            _buttons[currentSelectedButton].onClick.Invoke();
        }
    }
    
    public sealed class MainMenuMessenger : UIMessenger
    {
        public event Action OnPlayButtonClicked;
        public event Action<int> OnMoveSelection;
        public event Action OnEnableGamepadTooltip;
        public event Action OnSelection;

        public void MoveSelection(int direction)
        {
            OnMoveSelection?.Invoke(direction);
        }
        
        public void Select()
        {
            OnSelection?.Invoke();
        }
        
        public void EnableGamepadTooltip()
        {
            OnEnableGamepadTooltip?.Invoke();
        }
        
        public void PlayButtonClick()
        {
            OnPlayButtonClicked?.Invoke();
        }
    }
}
