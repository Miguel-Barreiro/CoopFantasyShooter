using System.Runtime.InteropServices;
using Core.Model;
using Core.Model.ModelSystems;
using Game.Components;
using Global;
using Zenject;

namespace Game.Player
{
	
	
	public class PlayerEntity : Entity, IDynamicPlayer, IDynamicWorldEntity
	{
		[Inject] private readonly GameConfig GameConfig = null!;

		
		public PlayerEntity(int playerNumber)
		{
			ref PlayerData playerData = ref GetComponent<PlayerData>();
			playerData.PlayerNumber = playerNumber;

		}
	}


	[StructLayout(LayoutKind.Auto)]
	public struct PlayerData : IComponentData
	{
		public EntId ID { get; set; }
		public int PlayerNumber { get; set; }

		public void Init()
		{
			PlayerNumber = -1;
		}
	}

	public interface IDynamicPlayer : Component<PlayerData>, 
								IDynamicWorldEntity 
	{ }
}