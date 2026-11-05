#nullable enable
using Core.Model;
using Core.View;
using FixedPointy;
using UnityEngine;
using Zenject;

namespace Game.Enemies.EnemyAbilities
{
	public abstract class AblilityTargetType
	{
		[Inject] protected readonly GameStatsContainer GameStatsContainer = null!;
		[Inject] protected readonly StatsSystem StatsSystem = null!;
		[Inject] protected readonly ViewEntitiesContainer ViewEntitiesContainer = null!;
		public abstract bool IsValidTarget(ref EnemyAbilityData abilityData);

		protected Vector3 GetOriginPosition3(EntId ownerID)
		{
			EntityViewAtributes? viewAtributes = ViewEntitiesContainer.GetEntityViewAtributes(ownerID);
			if(viewAtributes == null)
				return Vector3.zero;
			return viewAtributes!.GameObject!.transform.position;
		}
		
		protected Vector2 GetOriginPosition2(EntId ownerID)
		{
			EntityViewAtributes? viewAtributes = ViewEntitiesContainer.GetEntityViewAtributes(ownerID);
			if(viewAtributes == null)
				return Vector2.zero;
			Vector3 result = viewAtributes!.GameObject!.transform.position;
			return new Vector2(result.x, result.z);
		}

	}
	
	public sealed class AlivePlayerTargetType : AblilityTargetType
	{

		public override bool IsValidTarget(ref EnemyAbilityData abilityData)
		{
			Vector3 originPosition = this.GetOriginPosition3(abilityData.OwnerID);
			EntId abilityDataID = abilityData.ID;

			for (int i = 0; i < abilityData.TargetCount; i++)
			{
				EntId target = abilityData.Targets[i];
				if(target == EntId.Invalid)
					continue;

				EntityViewAtributes? viewAtributes = ViewEntitiesContainer.GetEntityViewAtributes(target);
				if(viewAtributes == null)
					continue;

				Vector3 targetPosition = viewAtributes!.GameObject!.transform.position;
				float distance = Vector3.Distance(originPosition, targetPosition);

				Fix abilityRange = StatsSystem.GetStatValue(abilityDataID, GameStatsContainer.CastDistance);
				if(distance <= abilityRange)
					return true;
			}
			return false;
		}
	}
	
	public sealed class AlwaysValidTargetType : AblilityTargetType
	{
		public override bool IsValidTarget(ref EnemyAbilityData abilityData) { return true; }
	}

	
	public sealed class PositionWithinRangeTargetType : AblilityTargetType
	{
		public override bool IsValidTarget(ref EnemyAbilityData abilityData)
		{
			
			Vector2 originPosition = this.GetOriginPosition2(abilityData.OwnerID);
			EntId abilityDataID = abilityData.ID;

			for (int i = 0; i < abilityData.TargetPositionCount; i++)
			{
				Vector2 targetPosition = abilityData.TargetPositions[i];
				
				float distance = Vector2.Distance(originPosition, targetPosition);

				Fix abilityRange = StatsSystem.GetStatValue(abilityDataID, GameStatsContainer.CastDistance);
				if(distance <= abilityRange)
					return true;
			}
			return false;
		}
	}


		
	public sealed class AliveEnemyTargetType : AblilityTargetType
	{
		
		public override bool IsValidTarget(ref EnemyAbilityData abilityData)
		{
			
			Vector3 originPosition = this.GetOriginPosition3(abilityData.OwnerID);
			EntId abilityDataID = abilityData.ID;

			for (int i = 0; i < abilityData.TargetCount; i++)
			{
				EntId target = abilityData.Targets[i];
				if(target == EntId.Invalid)
					continue;

				Fix currentHealth = StatsSystem.GetStatDepletedValue(target, GameStatsContainer.Health);
				if(currentHealth <= 0)
					continue;

				EntityViewAtributes? viewAtributes = ViewEntitiesContainer.GetEntityViewAtributes(target);
				if(viewAtributes == null)
					continue;

				Vector3 targetPosition = viewAtributes!.GameObject!.transform.position;
				float distance = Vector3.Distance(originPosition, targetPosition);

				Fix abilityRange = StatsSystem.GetStatValue(abilityDataID, GameStatsContainer.CastDistance);
				if(distance <= abilityRange)
					return true;
			}
			return false;
		}
	}


		
	public sealed class DeadEnemyTargetType : AblilityTargetType
	{
		public override bool IsValidTarget(ref EnemyAbilityData abilityData)
		{
			
			Vector3 originPosition = this.GetOriginPosition3(abilityData.OwnerID);
			EntId abilityDataID = abilityData.ID;

			for (int i = 0; i < abilityData.TargetCount; i++)
			{
				EntId target = abilityData.Targets[i];
				if(target == EntId.Invalid)
					continue;

				Fix currentHealth = StatsSystem.GetStatDepletedValue(target, GameStatsContainer.Health);
				if(currentHealth > 0)
					continue;

				EntityViewAtributes? viewAtributes = ViewEntitiesContainer.GetEntityViewAtributes(target);
				if(viewAtributes == null)
					continue;

				Vector3 targetPosition = viewAtributes!.GameObject!.transform.position;
				float distance = Vector3.Distance(originPosition, targetPosition);

				Fix abilityRange = StatsSystem.GetStatValue(abilityDataID, GameStatsContainer.CastDistance);
				if(distance <= abilityRange)
					return true;
			}
			return false;
		}

	}

	
}