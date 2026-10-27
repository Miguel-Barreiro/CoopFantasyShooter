using Core.Events;
using Core.Model;
using Core.VSEngine;
using UnityEngine;

namespace Game.Spells
{
	public sealed class ActivateAbilityEvent : EntityEvent<ActivateAbilityEvent>
	{
		[VSField()]
		public Vector3 OriginPosition;

		[VSField()]
		public Vector3 Direction = Vector2.zero;
		
		[VSField()]
		public Vector3 TargetPosition = Vector2.negativeInfinity;
		
		[VSField()]
		public EntId Owner = EntId.Invalid;
		
		public override void Execute()
		{
			
		}
	}
}