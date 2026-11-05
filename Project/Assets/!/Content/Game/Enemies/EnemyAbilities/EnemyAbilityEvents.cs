using System.Collections.Generic;
using Core.Events;
using Core.Model;
using Core.VSEngine;
using UnityEngine;
using UnityEngine.UIElements;
using Zenject;

namespace Game.Enemies.EnemyAbilities
{
	public abstract class EnemyAbilityEvent<T> : EntityEvent<T> 
		where T : EnemyAbilityEvent<T>, new()
	{
		[VSField(IsWritable = false)]
		public EntId Owner = EntId.Invalid;
		
		
	}
	
	
	public sealed class ActivateEnemyAbilityEvent : EnemyAbilityEvent<ActivateEnemyAbilityEvent>
	{
		[Inject] private readonly BasicCompContainer<EnemyAbilityData> EnemyAbilityContainer = null!;
		
		[VSField()]
		public Vector3 OriginPosition;
		
		[VSField()]
		public List<Vector3> TargetPositions = new List<Vector3>();

		[VSField(IsWritable = false)]
		public List<EntId> Targets = new List<EntId>();
		
		public override void Execute()
		{
			ref EnemyAbilityData abilityData = ref EnemyAbilityContainer.GetComponent(EntityID);
			abilityData.ClearTarget();
		}

		public void SetTargets(ref EnemyAbilityData abilityData, Vector3 origin)
		{
			Targets.Clear();
			for (int i = 0; i < abilityData.TargetCount; i++)
			{
				Targets.Add(abilityData.Targets[i]);
			}
			TargetPositions.Clear();
			for (int i = 0; i < abilityData.TargetPositionCount; i++)
				TargetPositions.Add(new Vector3(abilityData.TargetPositions[i].x, 0f, abilityData.TargetPositions[i].y));

			OriginPosition = origin;

		}
	}
	
	public sealed class FindTargetsAbilityEvent : EnemyAbilityEvent<FindTargetsAbilityEvent>
	{
		
		public override void Execute()
		{
		}
	}
	
	
}