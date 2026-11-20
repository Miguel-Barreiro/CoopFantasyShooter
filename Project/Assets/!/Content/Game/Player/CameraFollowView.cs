using System;
using Com.LuisPedroFonseca.ProCamera2D;
using Core.Model;
using Core.Systems;
using Game.Components;
using Global;
using UnityEngine;
using Zenject;

namespace Game.Player
{
	public sealed class CameraFollowView : MonoBehaviour
	{ 
		
		private Vector3 targetPosition;

		[SerializeField][Range(1f, 200f)] private float smoothTime = 150f;
		
		private void Awake() { targetPosition = transform.position; }

		private void Update()
		{
			transform.position = Vector3.Lerp(transform.position, targetPosition, smoothTime * Time.deltaTime);
		}

		public void SetPosition(Vector3 newPosition)
		{
			targetPosition = newPosition;
		}
	}
	
	public sealed class CameraFollowTargetSystem : IUpdateSystem
	{
		[Inject] private readonly ProCamera2D ProCamera2D = null!;
		[Inject] private readonly PlayerContainer PlayerContainer = null!;
		[Inject] private readonly BasicCompContainer<WorldEntityData> WorldEntityContainer = null!;
		[Inject] private readonly GameplayConfig GameplayConfig = null!;
		[Inject] private readonly CameraFollowView CameraFollowView = null!;


		public void UpdateSystem(float deltaTime)
		{
			float minX = float.MaxValue;
			float maxX = float.MinValue;
			float minZ = float.MaxValue;
			float maxZ = float.MinValue;

			uint topIndex = PlayerContainer.TopEmptyIndex;
			for (int i = 0; i < topIndex; i++)
			{
				ref PlayerData componentData = ref PlayerContainer.Components[i];
				ref WorldEntityData worldEntityData = ref WorldEntityContainer.GetComponent(componentData.ID);

				float positionX = worldEntityData.Position.x;
				if(minX > positionX) {
					minX = positionX;
				}
				if(maxX < positionX) {
					maxX = positionX;
				}

				float positionZ = worldEntityData.Position.z;
				if(minZ > positionZ) {
					minZ = positionZ;
				}
				if(maxZ < positionZ) {
					maxZ = positionZ;
				}
			}
			
			CameraFollowView.SetPosition(new Vector3((minX + maxX) / 2f, 0f,  (minZ + maxZ) / 2f));
		}


		public bool Active { get; set; } = true;
		public SystemGroup Group { get; } = GameSystemsGroup.GAME_VIEW_SYSTEM;
	}
}