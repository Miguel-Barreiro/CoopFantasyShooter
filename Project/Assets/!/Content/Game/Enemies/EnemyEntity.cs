using System.Runtime.InteropServices;
using Core.Model;
using Core.Model.ModelSystems;

namespace Game.Enemies
{
	public sealed class EnemyEntity : Entity, 
									IEnemy
	{
		
	}

	[StructLayout(LayoutKind.Auto)]
	public struct EnemyData : IComponentData
	{
		public EntId ID { get; set; }
		

		public void Init() { }
	}

	public interface IEnemy : Component<EnemyData> { }
}