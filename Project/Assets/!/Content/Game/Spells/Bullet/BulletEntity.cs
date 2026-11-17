using System.Runtime.InteropServices;
using Core.Events;
using Core.Model;
using Core.Model.ModelSystems;
using Core.Systems;
using Core.View;
using FixedPointy;
using Game.Components;
using Game.Enemies;
using Game.Logic;
using UnityEngine;
using Zenject;

namespace Game.Spells.Bullet
{
	public sealed class BulletEntity : SpellEntity,
										IBullet, 
										ISimulation, 
										IWorldEntity
	{
		public BulletEntity(SpellConfig dataConfig, EntId casterID) : base(dataConfig)
		{
			ref BulletData bulletData = ref GetComponent<BulletData>();
			bulletData.CasterID = casterID;
			bulletData.TargetTags = EntitiesContainer.ToMask(dataConfig.HitTags);
		}
	}

	[StructLayout(LayoutKind.Auto)]
	public struct BulletData : IComponentData
	{
		public EntId ID { get; set; }
		
		public ulong TargetTags;
		public EntId CasterID;

		public void Init()
		{
			TargetTags = (ulong)GameTag.None;
		}
	}

	public interface IBullet : ISimulation,
								IWorldEntity, 
								Component<BulletData> { }


	public sealed class BulletSystem : UpdateComponents<BulletData>
	{
		[Inject] private readonly BasicCompContainer<BulletData> BulletContainer = null!;
		[Inject] private readonly ViewEntitiesContainer ViewEntitiesContainer = null!;
		[Inject] private readonly GameStatsContainer GameStatsContainer = null!;
		[Inject] private readonly StatsSystem StatsSystem = null!;
		[Inject] private readonly SpellsLogic SpellsLogic = null!;
		[Inject] private readonly BasicCompContainer<WorldEntityData> WorldEntityContainer = null!;


		
		
		public void UpdateComponents(float deltaTime)
		{

			uint topIndex = BulletContainer.TopEmptyIndex;
			for (int i = 0; i < topIndex; i++)
			{
				ref BulletData componentData = ref BulletContainer.Components[i];
				EntId bulletID = componentData.ID;
				
				ref WorldEntityData worldEntityData = ref WorldEntityContainer.GetComponent(bulletID);
				
				worldEntityData.Position += worldEntityData.Velocity * deltaTime;
				worldEntityData.Direction = worldEntityData.Velocity.normalized;
				

				// EntityViewAtributes viewAtributes = ViewEntitiesContainer.GetEntityViewAtributes(bulletID);
				// // Vector3 position = viewAtributes!.GameObject!.transform.position;
				// if(worldEntityData.Position.magnitude > 2000f)
				// {
				// 	EntitiesContainer.DestroyEntity(bulletID);
				// 	continue;
				// }
				
				
				// Rigidbody rigidbody = viewAtributes.Get<Rigidbody>();
				// Vector3 velocity = rigidbody.linearVelocity;

				//TODO: Add bullet size as a stat
				SpellsLogic.HitSpellInCirle(bulletID, worldEntityData.Position, worldEntityData.Velocity * deltaTime,
											componentData.TargetTags, componentData.CasterID, 0.15f );

				
				Fix newTimeToLive = StatsSystem.ChangeDepletedValue(bulletID, GameStatsContainer.TimeToLive, -deltaTime);
				if(newTimeToLive <= Fix.Zero)
					EntitiesContainer.DestroyEntity(bulletID);
				
			}


		}

		public bool Active { get; set; } = true;
		public SystemGroup Group { get; } = GameSystemsGroup.GAME_LOGIC_SYSTEM;
	}
	
	
}