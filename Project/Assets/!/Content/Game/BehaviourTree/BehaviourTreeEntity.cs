using System.Runtime.InteropServices;
using Core.Model;
using Core.Model.ModelSystems;
using Core.Systems;
using UnityEngine;
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
		
		public EntId Target;
		public EntId SupportTarget;

		public float AttackFrameNorm;

		
		public bool Enable;
		
		public void Init()
		{
			Enable = true;
			Target = EntId.Invalid;
			SupportTarget = EntId.Invalid;
			AttackFrameNorm = 0.5f;
		}
	}

	public interface IBehaviourAgent : Component<BehaviourAgentData> { }
	
	
	public enum BehaviourStatus { Success, Failure, Running }
	
	[ComponentData(ContainerType = typeof(BehaviourTreeContainer))]
	[StructLayout(LayoutKind.Auto)]
	public struct BehaviourNodeData : IComponentData
	{
		private const int MAX_CHILDREN = 4;
		
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
		
		public void UpdateComponents(float deltaTime)
		{
			// uint topIndex = BehaviourContainer.TopEmptyIndex;
			
			
			uint topIndex = BehaviourContainer.TopEntityEmptyIndex;

			for (int i = 0; i < topIndex; i++)
			{
				ref EntityComponentsHeader entityHeader = ref BehaviourContainer.EntityComponentsHeaders[i];

				uint currentNode = entityHeader.Indexes[0];
				ref BehaviourNodeData mainNodeNodeData = ref BehaviourContainer.Components[currentNode];
				EntId entityId = mainNodeNodeData.ID;
				ref BehaviourAgentData agentData = ref BehaviourAgentContainer.GetComponent(entityId);

				if(agentData.Enable == false) {
					continue;
				}
#if DEBUG
				if (entityId == EntId.Invalid)
				{
					Debug.LogError($"BehaviourTreeSystem: Invalid entity ID for main node at index {i}. Skipping.");				
					continue;
				}
#endif				
				
				ProcessNode(entityId, ref mainNodeNodeData, ref agentData, deltaTime);
			}


		}
		public BehaviourStatus ProcessNode(EntId entityId, ref BehaviourNodeData childNodeData, 
											ref BehaviourAgentData agentData, float deltaTime)
		{
			if (entityId == EntId.Invalid)
			{
				Debug.LogError($"BehaviourTreeSystem: Invalid entity ID for node({childNodeData.NodeType}). Skipping.");				
				return BehaviourStatus.Failure;
			}
			return BehaviourNodesContainer.Nodes[childNodeData.NodeType].Process(entityId, ref childNodeData, ref agentData, deltaTime);
		}

		public bool Active { get; set; } = true;
		public SystemGroup Group { get; } = GameSystemsGroup.GAME_LOGIC_SYSTEM;
	}		
}