using Core.Model.Data;
using Game.Input;
using Game.Player;
using UnityEngine;
using UnityEngine.Serialization;

namespace Global
{
	public sealed class GameConfig : DataConfig
	{
		private const float SPEED_MULT = 300f;
		// private const float SPEED_MULT = 10f;
		[SerializeField] private PlayerView _PlayerPrefab;
		public PlayerView PlayerPrefab => _PlayerPrefab;


		[Range(0, 10)]
		[SerializeField] private float _PlayerSpeed = 1;
		public float PlayerSpeed => _PlayerSpeed * SPEED_MULT;


		[SerializeField] private LocalMultiplayerInputManager _LocalMultiplayerInputManagerPrefab;
		public LocalMultiplayerInputManager LocalMultiplayerInputManagerPrefab => _LocalMultiplayerInputManagerPrefab;

		
		
	}
}