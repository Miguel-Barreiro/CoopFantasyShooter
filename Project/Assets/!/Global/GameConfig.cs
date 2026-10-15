using Core.Model.Data;
using Game.Input;
using Game.Player;
using UnityEngine;
using UnityEngine.Serialization;

namespace Global
{
	public sealed class GameConfig : DataConfig
	{

		[SerializeField] private LocalMultiplayerInputManager _LocalMultiplayerInputManagerPrefab;
		public LocalMultiplayerInputManager LocalMultiplayerInputManagerPrefab => _LocalMultiplayerInputManagerPrefab;

		

		
	}
}