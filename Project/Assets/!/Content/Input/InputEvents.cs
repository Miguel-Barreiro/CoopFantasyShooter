using Core.Events;
using UnityEngine;

namespace Input
{
	public abstract class InputEvent<T> : Event<T> 
		where T : InputEvent<T>, new()
	{
		public int PlayerNumber = -1;
	}
	
	public sealed class RecoverPlayerInputEvent : InputEvent<RecoverPlayerInputEvent>, IEarlyEvent
	{
		public override void Execute() {}
	}
	
	
	public sealed class UseMainAbilityPlayerEvent : InputEvent<UseMainAbilityPlayerEvent>
	{
		public override void Execute() { }
	}
	
	public sealed class RotatePlayerEvent : InputEvent<RotatePlayerEvent>
	{
		public Vector2 NewLookDirection { get; set; } = Vector2.zero;
		
		public override void Execute() { }
	}
	
	public sealed class MovePlayerEvent : InputEvent<MovePlayerEvent>
	{
		public Vector2 Direction = Vector2.zero;
		public float MoveValue = 0f;

		public override void Execute() { }
	}

	
	public sealed class OnAEvent : InputEvent<OnAEvent>
	{
		public override void Execute() { }
	}
	
}