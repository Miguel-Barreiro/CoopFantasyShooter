using Core.Model;
using Core.View;
using Game.Player;
using Global;
using UnityEngine;
using Zenject;

namespace Game
{
	public sealed class GameLogic
	{
		[Inject] private readonly GameConfig GameConfig = null!;
		[Inject] private readonly ViewEntitiesContainer ViewEntitiesContainer = null!;

		
		public const uint MAX_PLAYERS = 4;

		
		public EntId AddPlayer(int newPlayerNumber)
		{
			EntId playerId = new PlayerEntity(newPlayerNumber).ID;
			
			
			AddView(playerId, GameConfig.PlayerPrefab.gameObject);
			
			
			return playerId;
		}

		
		protected void AddView(EntId entityId, GameObject prefab)
		{
			ViewEntitiesContainer.Spawn(prefab, entityId);

		}

	}
}