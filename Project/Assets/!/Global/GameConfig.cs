using Core.Model.Data;
using Game.Input;
using Game.Player;
using UnityEngine;
using UnityEngine.Serialization;

namespace Global
{
	public sealed class GameConfig : DataConfig
	{
		[SerializeField] private PlayerView _PlayerPrefab;
		public PlayerView PlayerPrefab => _PlayerPrefab;


		[Range(0, 10)]
		[SerializeField] private float _PlayerSpeed = 1;
		public float PlayerSpeed => _PlayerSpeed * _PlayerSpeedMult;

		[Range(1, 10)]
		[SerializeField] private float _PlayerSpeedMult = 1;
		// public float PlayerSpeedMult => _PlayerSpeedMult;


		[SerializeField] private LocalMultiplayerInputManager _LocalMultiplayerInputManagerPrefab;
		public LocalMultiplayerInputManager LocalMultiplayerInputManagerPrefab => _LocalMultiplayerInputManagerPrefab;

		
		
	}
}