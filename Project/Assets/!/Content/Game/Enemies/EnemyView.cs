using System;
using Core.Model;
using Core.View;
using FixedPointy;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using Zenject;

namespace Game.Enemies
{
	[RequireComponent(typeof(NavMeshAgent))]
	
	public class EnemyView : EntityView
	{
		public Image HealthImage;
		public RectTransform UI;


		[Inject] private readonly GameStatsContainer GameStatsContainer = null!;
		[Inject] private readonly StatsSystemRo StatsSystem = null!;
		[Inject] private readonly Camera Camera = null!;
		[Inject] private readonly BasicCompContainer<EnemyData> EnemyContainer = null!;

		
		private void Update()
		{
			Fix healthPercentage = StatsSystem.GetStatDepletedPercentage(EntityID,GameStatsContainer.Health);
			HealthImage.fillAmount = (float) healthPercentage;
			UI.transform.LookAt(Camera.transform);
		}
	}
}