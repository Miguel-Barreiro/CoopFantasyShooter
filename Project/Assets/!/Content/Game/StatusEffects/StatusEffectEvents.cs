using Core.Events;
using Core.Model;

namespace Game.StatusEffects
{
	public abstract class StatusEffectEvent<T> : EntityEvent<T> 
		where T : StatusEffectEvent<T>, new() 
	{
	
		
		
	}
	
	
	public sealed class StatusEffectAppliedEvent : StatusEffectEvent<StatusEffectAppliedEvent>
	{
		public EntId Target = EntId.Invalid;
		
		public override void Execute() { }
	}
	
	public sealed class StatusEffectRemovedEvent : StatusEffectEvent<StatusEffectRemovedEvent>
	{
		public EntId Target = EntId.Invalid;
		
		public override void Execute() { }
	}
	
	public sealed class StatusEffectTickEvent : StatusEffectEvent<StatusEffectTickEvent>
	{
		public EntId Target = EntId.Invalid;
		
		public override void Execute() { }
	}
	
}