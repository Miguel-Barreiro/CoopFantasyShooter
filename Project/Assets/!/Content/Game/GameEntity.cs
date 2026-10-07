using System.Runtime.InteropServices;
using Core.Model;
using Core.Model.ModelSystems;

namespace Game
{
	public class GameEntity : Entity, IGame
	{
	}

	[StructLayout(LayoutKind.Auto)]
	public struct GameData : IComponentData
	{
		public EntId ID { get; set; }
			
		
	
		public void Init()
		{
		}
	}

	public interface IGame : Component<GameData> { }
}