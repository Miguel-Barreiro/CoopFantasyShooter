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
	public sealed class BulletView : EntityView
	{

		public Rigidbody Rigidbody { get; private set; } = null!;

		private void Awake()
		{
			Rigidbody = GetComponent<Rigidbody>();
		}

		// private void Update()
		// {
		// 	RaycastHit[] raycastHits = Physics.SphereCastAll(transform.position, 0.1f, 
		// 														Rigidbody.linearVelocity.normalized, 
		// 														Rigidbody.linearVelocity.magnitude * Time.deltaTime);
		// 	
		// 	if(raycastHits.Length > 0)
		// 	{
		// 		foreach(RaycastHit hit in raycastHits)
		// 		{
		// 			EnemyView enemyView = hit.collider.gameObject.GetComponent<EnemyView>();
		// 			
		// 			OnSpellHitEvent onSpellHitEvent = EntityEventQueue.Execute<OnSpellHitEvent>(EntityID);
		// 			
		// 			if(enemyView != null)
		// 				onSpellHitEvent.EnemyID = enemyView.EntityID;
		// 			else
		// 				onSpellHitEvent.EnemyID = EntId.Invalid;
		// 		}
		// 	}
		// }

		// public void Fire()
		// {
		// 	
		// }

		// private void OnCollisionEnter(Collision collision)
		// {
		// 	EnemyView enemyView = collision.gameObject.GetComponent<EnemyView>();
		// 	OnSpellHitEvent onSpellHitEvent = EntityEventQueue.Execute<OnSpellHitEvent>(EntityID);
		// 	
		// 	if(enemyView != null)
		// 		onSpellHitEvent.EnemyID = enemyView.EntityID;
		// 	else
		// 		onSpellHitEvent.EnemyID = EntId.Invalid;
		// }
		//
		// private void OnTriggerEnter(Collider other) 
		// { 
		// 	EnemyView enemyView = other.gameObject.GetComponent<EnemyView>();
		// 	OnSpellHitEvent onSpellHitEvent = EntityEventQueue.Execute<OnSpellHitEvent>(EntityID);
		// 	
		// 	if(enemyView != null)
		// 		onSpellHitEvent.EnemyID = enemyView.EntityID;
		// 	else
		// 		onSpellHitEvent.EnemyID = EntId.Invalid;
		// } 
	}
}