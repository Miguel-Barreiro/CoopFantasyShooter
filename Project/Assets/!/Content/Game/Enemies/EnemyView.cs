using System;
using System.Collections;
using Core.Model;
using Core.View;
using FixedPointy;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Zenject;

namespace Game.Enemies
{
	[RequireComponent(typeof(NavMeshAgent))]
	
	public class EnemyView : EntityView
	{
		
		private int IS_RUNNING_ANIM = UnityEngine.Animator.StringToHash("isRunning");
		private int IS_ATTACKING_ANIM = UnityEngine.Animator.StringToHash("isAttacking");
		private int IS_CASTING_ANIM = UnityEngine.Animator.StringToHash("isAttacking");
		private int ATTACK_RATE_ANIM = UnityEngine.Animator.StringToHash("AttackRate");
		private int ATTACK_FRAME_TIME = UnityEngine.Animator.StringToHash("AttackFrameTime");
		private int RANDOM_ANIM = UnityEngine.Animator.StringToHash("Random");
		private int IS_DEAD_ANIM = UnityEngine.Animator.StringToHash("Die");
		private int IS_SPAWNING_ANIM = UnityEngine.Animator.StringToHash("Spawn");
		private int HIT_ANIM = UnityEngine.Animator.StringToHash("Hit");
		
		public bool Debug;
		public Image HealthImage;
		public Image SupportImage;
		public RectTransform UI;
		public Animator Animator;
		public Collider Collider;
		public NavMeshAgent NavMeshAgent { get; private set; }


		[Inject] private readonly GameStatsContainer GameStatsContainer = null!;
		[Inject] private readonly StatsSystemRo StatsSystem = null!;
		[Inject] private readonly Camera Camera = null!;
		[Inject] private readonly BasicCompContainer<EnemyData> EnemyContainer = null!;
		[Inject] private readonly IRandomSystem RandomSystem = null!;


		private void Awake()
		{
			NavMeshAgent = GetComponent<NavMeshAgent>();
			float range = (float) RandomSystem.Range(0f, 1f);
			Animator.SetFloat(RANDOM_ANIM, range);
		}
		
		private void Update()
		{
			if (Debug)
				return;
			Fix healthPercentage = StatsSystem.GetStatDepletedPercentage(EntityID,GameStatsContainer.Health);
			HealthImage.fillAmount = (float) healthPercentage;
			UI.transform.LookAt(Camera.transform);
			
			Animator.SetBool(IS_RUNNING_ANIM, NavMeshAgent.velocity.magnitude > 0.1f);
		}
		
		public void SetSupport(EntId casterId, bool isSupporting)
		{
			SupportImage.gameObject.SetActive(isSupporting);
		}

		public void SetAttack(Vector3 targetPosition, bool isAttacking, float attackFrameTime)
		{
			// float attackRate = (float) StatsSystem.GetStatDepletedValue(EntityID, GameStatsContainer.AttackRate);
			// Animator.SetFloat(ATTACK_RATE_ANIM, attackRate);
			Animator.SetFloat(ATTACK_FRAME_TIME, attackFrameTime);
			Animator.SetBool(IS_ATTACKING_ANIM, isAttacking);
		}

		public override void Reset()
		{
			base.Reset();
			
			UI.gameObject.SetActive(true);
			
			NavMeshAgent.enabled = true;
			if (NavMeshAgent.isOnNavMesh)
			{
				NavMeshAgent.ResetPath();
			}
			float range = (float) RandomSystem.Range(0f, 1f);
			Animator.SetFloat(RANDOM_ANIM, range);
			
			
			Animator.SetBool(IS_RUNNING_ANIM, false);
			Animator.SetBool(IS_ATTACKING_ANIM, false);
			Animator.SetBool(IS_CASTING_ANIM, false);
			Animator.SetBool(IS_DEAD_ANIM, false);
			SetAttack(Vector3.zero, false, 0f);
			SetSupport(EntId.Invalid, false);
			
		}

		public override bool IsActive { get => gameObject.activeSelf; }
		public override void ActivateGameObject()
		{
			
			gameObject.SetActive(true);
			UI.gameObject.SetActive(true);
			Collider.enabled = true;
			
			float range = (float) RandomSystem.Range(0f, 1f);
			Animator.SetFloat(RANDOM_ANIM, range);
			
			Animator.SetBool(IS_RUNNING_ANIM, false);
			Animator.SetBool(IS_ATTACKING_ANIM, false);
			Animator.SetBool(IS_CASTING_ANIM, false);
			Animator.SetBool(IS_DEAD_ANIM, false);
			SetAttack(Vector3.zero, false, 0f);
			SetSupport(EntId.Invalid, false);

			NavMeshAgent.enabled = true;
			if (NavMeshAgent.isOnNavMesh)
			{
				NavMeshAgent.ResetPath();
			}

		}
		public override void DeactivateGameObject()
		{
			NavMeshAgent.ResetPath();
			NavMeshAgent.enabled = false;
			Collider.enabled = false;
			gameObject.SetActive(false);
		}


		public void TriggerSpawning()
		{
			Animator.SetTrigger(IS_SPAWNING_ANIM);
			Animator.SetBool(IS_DEAD_ANIM, false);
		}

		public void SetCast(Vector3 targetPosition, bool isCasting)
		{
			Animator.SetBool(IS_CASTING_ANIM, isCasting);
		}
		public void SetDead()
		{
			Animator.SetBool(IS_RUNNING_ANIM, false);
			Animator.SetBool(IS_ATTACKING_ANIM, false);
			Animator.SetBool(IS_DEAD_ANIM, true);
			DisableNondeadComponents();
		}

		public void TriggerHit()
		{
			Animator.SetTrigger(HIT_ANIM);
		}

		private void DisableNondeadComponents()
		{
			NavMeshAgent.enabled = false;
			UI.gameObject.SetActive(false);
		}

		private void OnDrawGizmos()
		{
			Vector3 origin = transform.position + Vector3.up;
			if (!Debug)
			{
				float attackDistance = (float) StatsSystem.GetStatDepletedValue(EntityID, GameStatsContainer.AttackDistance);
				Color orangeRed = new Color(1f, 0.27f, 0f, 0.5f);
				DrawCircle(origin + transform.forward * ( 0.1f + NavMeshAgent.radius ), attackDistance, orangeRed);
				
			}

			DrawDirection(origin, transform.forward, Color.green);
			
			DrawLine(origin, NavMeshAgent.destination, Color.gold);
			
			void DrawLine(Vector3 origin, Vector3 end, Color color)
			{

				Gizmos.color = color;
				Gizmos.DrawLine(origin, end);
				Gizmos.DrawSphere(end, 0.15f);
			}
			
			void DrawDirection(Vector3 origin, Vector3 direction, Color color)
			{
				if (direction == Vector3.zero)
				{
					return;
				}

				Vector3 end = origin + direction * 0.5f;
				Gizmos.color = color;
				Gizmos.DrawLine(origin, end);
				Gizmos.DrawSphere(end, 0.08f);
			}
			void DrawCircle(Vector3 position, float size, Color color)
			{
				Gizmos.color = color;
				Gizmos.DrawSphere(position, size);
			}
		}

	}
}