using Core.Events;
using Core.Model;

namespace Game
{
	
	public sealed class OnEntityHitEvent : EntityEvent<OnEntityHitEvent>
	{
		public EntId ExecuterID;
		
		public override void Execute()
		{
			
		}
	}
}