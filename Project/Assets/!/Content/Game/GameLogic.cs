using Com.LuisPedroFonseca.ProCamera2D;
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
		[Inject] private readonly ProCamera2D ProCamera2D = null!;


		
		public const uint MAX_PLAYERS = 4;

		
		public EntId AddPlayer(int newPlayerNumber)
		{
			EntId playerId = new PlayerEntity(newPlayerNumber).ID;
			
			EntityViewAtributes view = AddView(playerId, GameConfig.PlayerPrefab.gameObject);
			ProCamera2D.AddCameraTarget(view.GameObject.transform);
			
			
			return playerId;
		}

		
		protected EntityViewAtributes AddView(EntId entityId, GameObject prefab)
		{
			return ViewEntitiesContainer.Spawn(prefab, entityId);
		}

	}
}