using System.Collections.Generic;
using Com.LuisPedroFonseca.ProCamera2D;
using Core.Model;
using Core.Model.Stats;
using Core.Systems;
using Core.View;
using FixedPointy;
using Game.Player;
using Game.Spells;
using Game.Spells.Bullet;
using Global;
using UnityEngine;
using Zenject;

namespace Game
{
	public sealed class GameLogic
	{
		[Inject] private readonly GameplayConfig GameplayConfig = null!;
		[Inject] private readonly ViewEntitiesContainer ViewEntitiesContainer = null!;
		[Inject] private readonly ProCamera2D ProCamera2D = null!;
		[Inject] private readonly PlayerContainer PlayerContainer = null!;
		[Inject] private readonly StatsSystem StatsSystem = null!;
		[Inject] private readonly IEntityHierarchySystem IEntityHierarchySystem = null!;
		[Inject] private readonly GameStatsContainer GameStatsContainer = null!;




		
		public const uint MAX_PLAYERS = 4;


		public EntId SpawnBullet(EntId playerId)
		{
			EntId bulletId = new BulletEntity().ID;

			ref PlayerData playerData = ref PlayerContainer.GetComponent(playerId);
			EntityViewAtributes playerViewAtributes = ViewEntitiesContainer.GetEntityViewAtributes(playerId);
			
			playerData.LookDirection = playerData.LookDirection.normalized;
			Vector3 playerPosition = playerViewAtributes!.GameObject.transform.position;
			
			Vector3 position = playerPosition + 
								Vector3.up * 0.9f +
								playerData.LookDirection.normalized * 
								(GameplayConfig.PlayerAttributes.Size + GameplayConfig.BasicSpellsAttributes.BulletSize);
			EntityViewAtributes view = AddView(bulletId, GameplayConfig.BasicSpellsAttributes.BulletPrefab.gameObject, position, Quaternion.LookRotation(playerData.LookDirection.normalized));
			
			Rigidbody rigidbody = view.Get<Rigidbody>();
			// rigidbody.MovePosition(position);
			// rigidbody.MoveRotation(Quaternion.LookRotation(playerData.LookDirection.normalized));
			rigidbody.linearVelocity = playerData.LookDirection.normalized * GameplayConfig.BasicSpellsAttributes.BulletSpeed;
			// rigidbody.AddForce(playerData.LookDirection.normalized * GameplayConfig.BasicSpellsAttributes.BulletSpeed, ForceMode.VelocityChange);

			Fix damage = StatsSystem.GetStatValue(playerData.AttackSpellID, GameStatsContainer.Damage);
			StatsSystem.SetBaseValue(bulletId, GameStatsContainer.Damage, damage);
			
			return bulletId;
		}


		public EntId AddPlayer(int newPlayerNumber)
		{
			EntId playerId = new PlayerEntity(newPlayerNumber).ID;
			
			EntityViewAtributes view = AddView(playerId, GameplayConfig.PlayerAttributes.PlayerPrefab.gameObject);
			PlayerView playerView = view.Get<PlayerView>();
			playerView.SetSize(GameplayConfig.PlayerAttributes.Size);
			ProCamera2D.AddCameraTarget(view.GameObject.transform);
			
			return playerId;
		}

		protected EntityViewAtributes AddView(EntId entityId, GameObject prefab, Vector3 position, Quaternion rotation)
		{
			return ViewEntitiesContainer.Spawn(prefab, entityId, position, rotation);
		}
		
		protected EntityViewAtributes AddView(EntId entityId, GameObject prefab)
		{
			return ViewEntitiesContainer.Spawn(prefab, entityId);
		}

		public EntId CreateSpell(SpellConfig spellConfig, EntId playerId)
		{
			SpellEntity spellEntity = new SpellEntity();
			EntId spellEntityID = spellEntity.ID;
			IEntityHierarchySystem.AddChild(playerId, spellEntityID);

			OverrideDefaultStats(spellEntityID, spellConfig.Stats);
			
			return spellEntityID;
		}



		private void OverrideDefaultStats(EntId newEntityID, List<StatOverride> statOverrides)
		{
			foreach (StatOverride statOverride in statOverrides)
			{
				switch (statOverride.OverrideType)
				{
					case OverrideType.Override:
						StatsSystem.SetBaseValue(newEntityID, statOverride.StatConfig,statOverride.Value);
						break;
					case OverrideType.Additive:
						StatsSystem.AddModifier(newEntityID, newEntityID, 
												statOverride.StatConfig,statOverride.Value, 
												StatModifierType.Additive);
						
						break;
					case OverrideType.Multiplicative:
						StatsSystem.AddModifier(newEntityID, newEntityID, 
												statOverride.StatConfig,statOverride.Value, 
												StatModifierType.Multiplicative);
						break;
					
				}
				StatsSystem.ResetDepletedValueToMax(newEntityID, statOverride.StatConfig);
			}
		}
	}
}