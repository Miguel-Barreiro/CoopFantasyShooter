using System;
using Core.Events;
using Core.Model;
using Core.View;
using Game.Enemies;
using UnityEngine;
using Zenject;

namespace Game.Spells.Bullet
{
	[RequireComponent(typeof(Rigidbody))]
	public sealed class BulletView : SpellView
	{

		public Rigidbody Rigidbody { get; private set; } = null!;

		private void Awake()
		{
			Rigidbody = GetComponent<Rigidbody>();
		}

		public override void Reset()
		{
			base.Reset();
			Rigidbody.linearVelocity = Vector3.zero;
			Rigidbody.angularVelocity = Vector3.zero;
		}

		public override void ActivateGameObject()
		{
			gameObject.SetActive(true);
		}
		public override void DeactivateGameObject()
		{
			gameObject.SetActive(false);
			Rigidbody.linearVelocity = Vector3.zero;
			Rigidbody.angularVelocity = Vector3.zero;
		}
	}
}