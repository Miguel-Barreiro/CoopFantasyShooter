using Core.Events;
using Core.Model;
using Game.Player;
using UnityEngine;
using Zenject;

namespace Game.Events
{
	public sealed class RecoverPlayerInputEvent : Event<RecoverPlayerInputEvent>
	{
		[Inject] private readonly GameLogic GameLogic = null!;
		// [Inject] private readonly BasicCompContainer<GameData> GameContainer = null!;
		[Inject] private readonly BasicCompContainer<PlayerData> PlayerContainer = null!;

		
		
		public int PlayerNumber = -1;
		
		public override void Execute()
		{
			if(PlayerNumber < 1)
			{
				return;
			}
			
			// ref GameData gameData = ref GameContainer.Components[0];

			uint topIndex = PlayerContainer.TopEmptyIndex;
			for (int i = 0; i < topIndex; i++)
			{
				ref PlayerData playerData = ref PlayerContainer.Components[i];
				if(playerData.PlayerNumber == PlayerNumber)
				{
					Debug.Log($"Player {PlayerNumber} already exists, skipping recovery.");
					return;
				}
			}

			GameLogic.AddPlayer(PlayerNumber);
		}
	}
}