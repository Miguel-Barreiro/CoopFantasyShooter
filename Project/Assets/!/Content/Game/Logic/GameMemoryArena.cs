using Core.VSEngine;
using UnityEngine;

namespace Game.Logic
{
	public sealed class GameMemoryArena : BaseMemoryArena
	{
		
		public readonly Collider[] ColliderHitsCache = new Collider[20];
		public readonly RaycastHit[] RaycastHitsCache = new RaycastHit[20];
		
	}
}