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
	[RequireComponent(typeof(Collider))]
	public sealed class BulletView : EntityView
	{

		public Rigidbody Rigidbody { get; private set; } = null!;

		private void Awake()
		{
			Rigidbody = GetComponent<Rigidbody>();
		}

		private void OnCollisionEnter(Collision collision)
		{
			EnemyView enemyView = collision.gameObject.GetComponent<EnemyView>();
			OnSpellHitEvent onSpellHitEvent = EntityEventQueue.Execute<OnSpellHitEvent>(EntityID);
			
			if(enemyView != null)
				onSpellHitEvent.EnemyID = enemyView.EntityID;
			else
				onSpellHitEvent.EnemyID = EntId.Invalid;
		}

	}
}