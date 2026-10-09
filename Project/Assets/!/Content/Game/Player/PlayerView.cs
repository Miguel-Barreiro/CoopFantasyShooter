using Core.View;
using UnityEngine;
using Zenject;

namespace Game.Player
{
	[RequireComponent(typeof(Rigidbody))]
	public sealed class PlayerView: EntityView
	{
		public Rigidbody Rigidbody { get; private set; } = null!;
		public Animator Animator { get => _animator; }
		[SerializeField] private Animator _animator = null!;

		[Inject] private readonly PlayerContainer PlayerContainer = null!;

		private readonly int SPEEDX = Animator.StringToHash("SpeedX");
		private readonly int SPEEDZ = Animator.StringToHash("SpeedZ");

		[SerializeField] private float _gizmoLength = 2f;
		[SerializeField] private float _gizmoHeight = 1f;

		private Vector3 _lookDirection;
		private Vector3 _rightDirection;
		private Vector3 _moveDirection;

		private void Awake()
		{
			Rigidbody = GetComponent<Rigidbody>(); 
		}


		private void Update()
		{
			ref PlayerData playerData = ref PlayerContainer.GetComponent(EntityID);

			Vector3 lookDirection = playerData.LookDirection.normalized;
			Vector3 moveDirection = playerData.MoveDirection.normalized;

			Vector3 rightDirection = Quaternion.AngleAxis( 90f, Vector3.up) * lookDirection;
			float dotZ = Vector3.Dot(lookDirection, moveDirection);
			float dotX =Vector3.Dot(rightDirection, moveDirection);

			_lookDirection = lookDirection;
			_rightDirection = rightDirection;

			float moveValueZ = dotZ;
			float moveValueX = dotX;
			
			_animator.SetFloat(SPEEDZ, moveValueZ);
			_animator.SetFloat(SPEEDX, moveValueX);
			
			_moveDirection = moveDirection;
			_animator.transform.rotation = Quaternion.LookRotation(playerData.LookDirection);
		}

		private void OnDrawGizmos()
		{
			Vector3 origin = transform.position + Vector3.up * _gizmoHeight;

			DrawDirection(origin, _lookDirection, Color.blue);
			DrawDirection(origin, _rightDirection, Color.red);
			DrawDirection(origin, _moveDirection, Color.green);
			void DrawDirection(Vector3 origin, Vector3 direction, Color color)
			{
				if (direction == Vector3.zero)
				{
					return;
				}

				Vector3 end = origin + direction * _gizmoLength;
				Gizmos.color = color;
				Gizmos.DrawLine(origin, end);
				Gizmos.DrawSphere(end, 0.08f);
			}
		}

	}
	
}
