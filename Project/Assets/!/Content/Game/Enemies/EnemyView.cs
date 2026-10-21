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
		
		public bool Debug;
		public Image HealthImage;
		public RectTransform UI;
		public Animator Animator;
		
		public NavMeshAgent NavMeshAgent { get; private set; }


		[Inject] private readonly GameStatsContainer GameStatsContainer = null!;
		[Inject] private readonly StatsSystemRo StatsSystem = null!;
		[Inject] private readonly Camera Camera = null!;
		[Inject] private readonly BasicCompContainer<EnemyData> EnemyContainer = null!;


		private void Start() { NavMeshAgent = GetComponent<NavMeshAgent>(); }

		private void Update()
		{
			if (Debug)
				return;
			Fix healthPercentage = StatsSystem.GetStatDepletedPercentage(EntityID,GameStatsContainer.Health);
			HealthImage.fillAmount = (float) healthPercentage;
			UI.transform.LookAt(Camera.transform);

			Animator.SetBool(IS_RUNNING_ANIM, NavMeshAgent.velocity.magnitude > 0.1f);
		}

		public void SetAttack(Vector3 targetPosition, bool isAttacking)
		{
			Animator.SetBool(IS_ATTACKING_ANIM, isAttacking);
		}

	}
}