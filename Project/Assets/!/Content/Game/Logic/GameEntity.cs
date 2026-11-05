using System.Runtime.InteropServices;
using Core.Model;
using Core.Model.ModelSystems;

namespace Game.Logic
{
	public sealed class GameEntity :Entity, IGame, ISimulation
	{
		
	}

	[StructLayout(LayoutKind.Auto)]
	public struct GameData : IComponentData
	{
		public EntId ID { get; set; }
		

		public void Init() { }
	}

	public interface IGame : Component<GameData> { }



	[StructLayout(LayoutKind.Auto)]
	public struct SimulationData : IComponentData
	{
		public EntId ID { get; set; }
		

		public void Init() { }
	}

	public interface ISimulation : Component<SimulationData> { }
}