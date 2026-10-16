using Core.Events;
using Core.Model;

namespace Game.Logic
{
	public abstract class GameEvent<T> : EntityEvent<T> 
		where T : GameEvent<T>, new() 
	{
		protected EntId GameEntityID => EntityID;
	}
}