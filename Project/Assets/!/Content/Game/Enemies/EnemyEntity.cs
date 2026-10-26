using System.Runtime.InteropServices;
using Core.Model;
using Core.Model.ModelSystems;
using Core.Systems;
using Core.View;
using FixedPointy;
using Game.BehaviourTree;
using Unity.AI.Navigation;
using UnityEngine.AI;
using Zenject;

namespace Game.Enemies
{
	public sealed class EnemyEntity : Entity, 
									IEnemy, 
									IBehaviour
	{
		public EnemyEntity()
		{
			ref BehaviourAgentData agentData = ref GetComponent<BehaviourAgentData>();
			agentData.Enable = true;
		}
	}

	[StructLayout(LayoutKind.Auto)]
	public struct EnemyData : IComponentData
	{
		public EntId ID { get; set; }
		public NavMeshAgent NavMeshAgent;
		
		public EntId TargetPlayerID;

		public void Init()
		{
			TargetPlayerID = EntId.Invalid;
		}
	}

	public interface IEnemy : Component<EnemyData> { }
	
	
	public sealed class EnemySystem : OnCreateComponent<EnemyData>, UpdateComponents<EnemyData>
	{
		[Inject] private readonly BasicCompContainer<EnemyData> EnemyContainer = null!; 
		[Inject] private readonly StatsSystem StatsSystem = null!;
		[Inject] private readonly GameStatsContainer GameStatsContainer = null!;
		[Inject] private readonly ViewEntitiesContainer ViewEntitiesContainer = null!;
		[Inject] private readonly GameLogic GameLogic = null!;

		
		public void OnCreateComponent(EntId newComponentId)
		{
			EntityViewAtributes entityViewAtributes = ViewEntitiesContainer.GetEntityViewAtributes(newComponentId);
			EnemyContainer.GetComponent(newComponentId).NavMeshAgent = entityViewAtributes!.Get<NavMeshAgent>();
			
		}

		public void UpdateComponents(float deltaTime)
		{
			uint topIndex = EnemyContainer.TopEmptyIndex;
			for (int i = 0; i < topIndex; i++)
			{
				ref EnemyData componentData = ref EnemyContainer.Components[i];

				EntId enemyID = componentData.ID;
				Fix health = StatsSystem.GetStatDepletedValue(enemyID, GameStatsContainer.Health);
				if(health <= 0)
				{
					GameLogic.KillEnemy(enemyID);
				}
			}
		}
		
		
		
		public bool Active { get; set; } = true;
		public SystemGroup Group { get; } = GameSystemsGroup.GAME_LOGIC_SYSTEM;
	}
}