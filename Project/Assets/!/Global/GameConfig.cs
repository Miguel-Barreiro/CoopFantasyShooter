using Core.Model.Data;
using Game.Player;
using UnityEngine;

namespace Global
{
	public sealed class GameConfig : DataConfig
	{
		[SerializeField] private PlayerView _PlayerPrefab;
		public PlayerView PlayerPrefab => _PlayerPrefab;
	}
}