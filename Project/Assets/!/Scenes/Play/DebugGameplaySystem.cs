using Core.Model;
using Core.Model.ModelSystems;
using Core.Systems;
using Game;
using Game.Player;
using Global;
using Zenject;

namespace Scenes.Play
{
	public sealed class DebugGameplaySystem : IStartSystem, OnCreateComponent<PlayerData>
	{
		[Inject] private readonly PlayerContainer PlayerContainer = null!;
		[Inject] private readonly GameLogic GameLogic = null!;
		[Inject] private readonly GameplayConfig GameplayConfig = null!;

		
		public void StartSystem() { }

		public void OnCreateComponent(EntId newPlayerId)
		{
			ref PlayerData playerData = ref PlayerContainer.GetComponent(newPlayerId);

			EntId spellID = GameLogic.CreateSpell(GameplayConfig.BasicSpellsAttributes.BulletSpellConfig, newPlayerId);
			playerData.AttackSpellID = spellID;
		}
		

		public bool Active { get; set; } = true;
		public SystemGroup Group { get; } = GameSystemsGroup.DEBUG_SYSTEM;
	}
}