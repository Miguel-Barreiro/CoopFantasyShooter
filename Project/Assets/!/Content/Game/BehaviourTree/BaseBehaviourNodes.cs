using System.Collections.Generic;
using Core.Initialization;
using Core.Model;
using Core.Model.ModelSystems;
using Core.Systems;
using UnityEngine;
using Zenject;

namespace Game.BehaviourTree
{

	public enum NodeType
	{
		Sequence = 0,
		MoveToTarget = 1,
		FindTarget = 2,
		FirstInPriority = 3, 
		AttackTarget = 4,
	}
	
	
	public sealed class BehaviourNodesContainer : IInitSystem
	{
		[Inject] private readonly ObjectBuilder ObjectBuilder = null!;

		public BehaviourNodesContainer()
		{
			Nodes = new Dictionary<NodeType, IBehaviourNode>()
			{
				{ NodeType.Sequence, new SequenceNode() },
				{ NodeType.MoveToTarget, new MoveToTargetNode() },
				{ NodeType.FindTarget, new ChoseTargetNode() },
				{ NodeType.FirstInPriority, new PriorityNode() },
				{ NodeType.AttackTarget, new SimpleAttackNode() }
			};
		}

		public readonly Dictionary<NodeType, IBehaviourNode> Nodes;
		
		public void Initialize()
		{
			foreach (IBehaviourNode iBehaviourNode in Nodes.Values)
				ObjectBuilder.Inject(iBehaviourNode);
		}
	}

	public interface IBehaviourNode
	{
		public BehaviourStatus Process(EntId entityId, ref BehaviourNodeData nodeData, 
										ref BehaviourAgentData agentData, float deltaTime);
		public void Reset(EntId entityId, ref BehaviourNodeData nodeNodeData);
	}

	public abstract class BaseBehaviourNode : IBehaviourNode
	{
		[Inject] protected readonly BehaviourTreeContainer BehaviourTreeContainer = null!;
		[Inject] protected readonly BehaviourTreeSystem BehaviourTreeSystem = null!;

		public abstract BehaviourStatus Process(EntId entityId, ref BehaviourNodeData nodeData, 
										ref BehaviourAgentData agentData, float deltaTime);

		public virtual void Reset(EntId entityId, ref BehaviourNodeData nodeNodeData)
		{
			nodeNodeData.currentChild = 0;
		}

		protected ref BehaviourNodeData GetChild(ref EntityComponentsHeader header, uint childOrder)
		{
			if (childOrder >= header.Indexes.Length)
			{
				Debug.LogError($"Child order {childOrder} is out of range of {header.Indexes.Length}");
				return ref BehaviourTreeContainer.Invalid;
			}
			
			uint childIndex = header.Indexes[childOrder];
			ref BehaviourNodeData childNodeData = ref BehaviourTreeContainer.GetComponent(childIndex);
			return ref childNodeData;
		}
	}

	public sealed class PriorityNode : BaseBehaviourNode
	{
		public override BehaviourStatus Process(EntId entityId,
												ref BehaviourNodeData nodeData, 
												ref BehaviourAgentData agentData,
												float deltaTime)
		{
			ref EntityComponentsHeader header = ref BehaviourTreeContainer.GetEntityComponentsHeader(entityId);
			
			for (int i = 0; i < nodeData.childrenCount; i++)
			{
				ref BehaviourNodeData childNodeData = ref GetChild(ref header, nodeData.children[i]);
				BehaviourStatus status = BehaviourTreeSystem.ProcessNode(entityId, ref childNodeData, ref agentData, deltaTime);
				switch (status)
				{
					case BehaviourStatus.Running:
						return BehaviourStatus.Running;
					case BehaviourStatus.Success:
						Reset(entityId, ref nodeData);
						return BehaviourStatus.Success;
						break;
					default:
						continue;
				}

			}
			
			Reset(entityId, ref nodeData);
			return BehaviourStatus.Failure;
		}
	}

	
	public sealed class SequenceNode : BaseBehaviourNode
	{
		public override BehaviourStatus Process(EntId entityId, ref BehaviourNodeData nodeData, 
												ref BehaviourAgentData agentData, float deltaTime)
		{
			ref EntityComponentsHeader header = ref BehaviourTreeContainer.GetEntityComponentsHeader(entityId);
			
			while (nodeData.currentChild < nodeData.childrenCount)
			{
				ref BehaviourNodeData childNodeData = ref GetChild(ref header, nodeData.children[nodeData.currentChild]);
				BehaviourStatus status = BehaviourTreeSystem.ProcessNode(entityId, ref childNodeData, ref agentData, deltaTime);
				switch (status)
				{
					case BehaviourStatus.Running:
						return BehaviourStatus.Running;
					case BehaviourStatus.Success:
						nodeData.currentChild++;
						break;
					case BehaviourStatus.Failure:
						Reset(entityId, ref nodeData);
						return BehaviourStatus.Failure;
				}
			}

			Reset(entityId, ref nodeData);
			return BehaviourStatus.Success;
		}
	}

	/*public sealed class SequenceNode : IBehaviourNode
	{
		public BehaviourStatus Process(EntId entityId, ref BehaviourData behaviourData, float deltaTime)
		{
			behaviourData.children
			for (int i = 0; i < behaviourData.children; i++)
			{
				var child = _children[i];
				var status = child.Process(entityId, ref behaviourData, deltaTime);

				if (status != BehaviourStatus.Success)
				{
					return status;
				}
			}

			return BehaviourStatus.Success;
		}
	}*/
}