using System;
using Core.View;
using UnityEngine;

namespace Game.Player
{
	[RequireComponent(typeof(Rigidbody))]
	public sealed class PlayerView: EntityView
	{
		public Rigidbody Rigidbody { get; private set; } = null!;

		private void Awake() { Rigidbody = GetComponent<Rigidbody>(); }
	}
	
}
