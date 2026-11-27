using Core.Model.Data;
using Core.Model.Stats;
using Game.Player;
using Input;
using UnityEngine;
using UnityEngine.Serialization;

namespace Global
{
	public sealed class GameConfig : DataConfig
	{

		[SerializeField] private LocalMultiplayerInputManager _LocalMultiplayerInputManagerPrefab;
		public LocalMultiplayerInputManager LocalMultiplayerInputManagerPrefab => _LocalMultiplayerInputManagerPrefab;

		
		[SerializeField] private StatConfig _defaultVSTimerStat;
		public StatConfig DefaultVSTimerStat => _defaultVSTimerStat;

		
	}
}