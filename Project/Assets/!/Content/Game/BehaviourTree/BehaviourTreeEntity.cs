#nullable enable
using System.Runtime.InteropServices;
using Core.Model;
using Core.Model.ModelSystems;
using Core.Systems;
using Core.View;
using Game.Components;
using Game.Logic;
using Game.Player;
using UnityEngine;
using UnityEngine.AI;
using Zenject;

namespace Game.BehaviourTree
{
	// public sealed class BehaviourTreeEntity : Entity, IBehaviour, IBehaviourAgent
	// {
	// 	
	// }

	[StructLayout(LayoutKind.Auto)]
	public struct BehaviourAgentData : IComponentData
	{
		public const int MAX_ABILITIES = 4;
		
		public EntId ID { get; set; }
		public Vector3 Position;
		

		public EntId Target;
		public EntId SupportTarget;

		public float AttackFrameNorm;

		public EntId ClosestEnemyID;
		public Vector3 ClosestEnemyPosition;
		
		public bool Enable;
		
		public void Init()
		{
			Enable = true;
			Target = EntId.Invalid;
			SupportTarget = EntId.Invalid;
			AttackFrameNorm = 0.5f;
			ClosestEnemyID = EntId.Invalid;
			ClosestEnemyPosition = Vector3.zero;
		}
	}

	public interface IBehaviourAgent : Component<BehaviourAgentData> { }
	
	
	public enum BehaviourStatus { Success, Failure, Running }
	
	[ComponentData(ContainerType = typeof(BehaviourTreeContainer))]
	[StructLayout(LayoutKind.Auto)]
	public struct BehaviourNodeData : IComponentData
	{
		private const int MAX_CHILDREN = 5;
		
		public EntId ID { get; set; }

		
		
		public NodeType NodeType;
		public float Cooldown;
		
		public EntId AbilityID;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst=MAX_CHILDREN)] public uint[] children;
		public uint childrenCount;
		public uint currentChild;
			
		
#if DEBUG
		public readonly string name;
#endif
		
		public void Init()
		{
			children = new uint[MAX_CHILDREN];
			currentChild = 0;
			childrenCount = 0;
			Cooldown = 0f;
			NodeType = NodeType.Sequence;
		}
		
		public void AddChild(uint childOrder)
		{
			if (childrenCount >= MAX_CHILDREN)
			{
				Debug.LogError($"BehaviourData: Cannot add more than {MAX_CHILDREN} children");
				return;
			}
			children[childrenCount] = childOrder;
			childrenCount++;
		}
	}

	public interface IBehaviour : Component<BehaviourNodeData> , IBehaviourAgent { }
	
	public sealed class BehaviourTreeContainer : MultipleComponentEntityContainer<BehaviourNodeData>
	{
		public BehaviourTreeContainer(uint maxNumber) : base(maxNumber) { }
		
		
		
		public ref BehaviourNodeData AddNode(EntId entityId, NodeType nodeType, out uint newChildOrder, uint parent = 0)
		{
			ref EntityComponentsHeader header = ref GetEntityComponentsHeader(entityId);
			if(parent > header.Count)
			{
				newChildOrder = uint.MaxValue;
				Debug.LogError($"BehaviourTreeContainer: Parent index {parent} is out of bounds for entity {entityId.Id}");
				return ref Invalid;
			}
			
			newChildOrder = AddComponent(entityId);
			uint newNodeIndex = GetChildIndex(entityId, newChildOrder);
			ref BehaviourNodeData newNodeNodeData = ref GetComponent(newNodeIndex);
			newNodeNodeData.NodeType = nodeType;


			uint headerIndex = header.Indexes[parent];
			ref BehaviourNodeData parentNodeData = ref GetComponent(headerIndex);
			parentNodeData.AddChild(newChildOrder);

			return ref newNodeNodeData;
		}
	}
	
	public sealed class BehaviourTreeSystem : UpdateComponents<BehaviourNodeData>
	{
		[Inject] private readonly BehaviourTreeContainer BehaviourContainer = null!;
		[Inject] private readonly BehaviourNodesContainer BehaviourNodesContainer = null!;
		[Inject] private readonly BasicCompContainer<BehaviourAgentData> BehaviourAgentContainer = null!;
		[Inject] private readonly PlayerContainer PlayerContainer = null!;
		[Inject] private readonly BasicCompContainer<WorldEntityData> WorldEntityContainer = null!;
		[Inject] private readonly ViewEntitiesContainer ViewEntitiesContainer = null!;
		[Inject] private readonly GameMemoryArena GameMemoryArena = null!;



		
		public void UpdateComponents(float deltaTime)
		{

			uint topIndex = BehaviourContainer.TopEntityEmptyIndex;

			for (int i = 0; i < topIndex; i++)
			{
				ref EntityComponentsHeader entityHeader = ref BehaviourContainer.EntityComponentsHeaders[i];

				uint currentNode = entityHeader.Indexes[0];
				ref BehaviourNodeData mainNodeNodeData = ref BehaviourContainer.Components[currentNode];
				EntId entityId = mainNodeNodeData.ID;
				ref BehaviourAgentData agentData = ref BehaviourAgentContainer.GetComponent(entityId);
				ref WorldEntityData worldEntityData = ref WorldEntityContainer.GetComponent(entityId);

				if(agentData.Enable == false) {
					continue;
				}
				
			
				SetClosestPlayer(worldEntityData.Position, ref agentData);
				agentData.Position = worldEntityData.Position;
#if DEBUG
				if (entityId == EntId.Invalid)
				{
					Debug.LogError($"BehaviourTreeSystem: Invalid entity ID for main node at index {i}. Skipping.");				
					continue;
				}
#endif				
				
				EntityViewAtributes? viewAtributes = ViewEntitiesContainer.GetEntityViewAtributes(entityId)!;
				ref BaseBehaviourNode.AgentMotion agentMotion = ref GameMemoryArena.AgentMotionCache;
				agentMotion.NavMeshAgent = viewAtributes.Get<NavMeshAgent>();
				agentMotion.Transform = viewAtributes!.GameObject!.transform;
				
				ProcessNode(entityId, ref mainNodeNodeData, ref agentData, deltaTime, agentMotion);
			}


			void SetClosestPlayer(Vector3 entityPosition, ref BehaviourAgentData agentData)
			{
				float closestDistance = float.MaxValue;
				uint topPlayerIndex = PlayerContainer.TopEmptyIndex;
				for (int i = 0; i < topPlayerIndex; i++)
				{
					ref PlayerData componentData = ref PlayerContainer.Components[i];
					ref WorldEntityData worldEntityData = ref WorldEntityContainer.GetComponent(componentData.ID);
					float distance = Vector3.Distance(entityPosition, worldEntityData.Position);
					if (distance < closestDistance)
					{
						closestDistance = distance;
						agentData.ClosestEnemyID = componentData.ID;
						agentData.ClosestEnemyPosition = worldEntityData.Position;
					}
				}
			}
		}
		public BehaviourStatus ProcessNode(EntId entityId, ref BehaviourNodeData childNodeData, 
											ref BehaviourAgentData agentData, float deltaTime,
												BaseBehaviourNode.AgentMotion agentMotion)
		{
			if (entityId == EntId.Invalid)
			{
				Debug.LogError($"BehaviourTreeSystem: Invalid entity ID for node({childNodeData.NodeType}). Skipping.");				
				return BehaviourStatus.Failure;
			}
			return BehaviourNodesContainer.Nodes[childNodeData.NodeType].Process(entityId, ref childNodeData, ref agentData, deltaTime, agentMotion);
		}

		public bool Active { get; set; } = true;
		public SystemGroup Group { get; } = GameSystemsGroup.GAME_LOGIC_SYSTEM;
	}		
}