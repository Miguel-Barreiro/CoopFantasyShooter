using Core.Model;
using Core.View;
using FixedPointy;
using Game.Spells;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Game.Player
{
	[RequireComponent(typeof(Collider))]
	[RequireComponent(typeof(Rigidbody))]
	public sealed class PlayerView: BasicEntityView
	{
		
		public Image HealthImage;
		public Image CooldownImage;
		public TMP_Text PlayerText;
		public RectTransform UI;
		public Rigidbody Rigidbody { get; private set; } = null!;
		public Animator Animator { get => _animator; }
		[SerializeField] private Animator _animator = null!;


		[Inject] private readonly Camera Camera = null!;
		[Inject] private readonly GameStatsContainer GameStatsContainer = null!;
		[Inject] private readonly StatsSystemRo StatsSystem = null!;
		[Inject] private readonly PlayerContainer PlayerContainer = null!;
		[Inject] private readonly BasicCompContainer<AbilityData> AbilityContainer = null!;


		private readonly int DIE_ANIMATION = Animator.StringToHash("Die");
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
			UI.transform.LookAt(Camera.transform);
			if(StatsSystem.GetStatDepletedValue(EntityID, GameStatsContainer.Health) <= 0f)
			{
				_animator.SetTrigger(DIE_ANIMATION);
				string playerTextText = $"DEAD";
				PlayerText.text = playerTextText;
				PlayerText.gameObject.SetActive(true);
				CooldownImage.gameObject.SetActive(false);
				return;
			}
			
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
			_animator.transform.rotation = Quaternion.Lerp(_animator.transform.rotation, 
															Quaternion.LookRotation(playerData.LookDirection), 
															Time.deltaTime * 5f);
			ref AbilityData abilityData = ref AbilityContainer.GetComponent(playerData.AttackAbiityID);
			switch (abilityData.State)
			{
				case AbilityState.Recover:
					float cooldownPercentage = (float) StatsSystem.GetStatDepletedPercentage(playerData.AttackAbiityID, GameStatsContainer.Cooldown);

					CooldownImage.fillAmount = cooldownPercentage;
					PlayerText.gameObject.SetActive(false);
					CooldownImage.gameObject.SetActive(true);
					break;
				case AbilityState.Cast:
				case AbilityState.Ready:
					int ammo = (int) StatsSystem.GetStatDepletedValue(playerData.AttackAbiityID, GameStatsContainer.CastingMultiplier);
					int ammoTotal = (int) StatsSystem.GetStatValue(playerData.AttackAbiityID, GameStatsContainer.CastingMultiplier);
					PlayerText.gameObject.SetActive(true);
					CooldownImage.gameObject.SetActive(false);
					
					PlayerText.color = Color.white;
					string playerTextText = $"{ammo}/{ammoTotal}";
					PlayerText.text = playerTextText;
					break;
				
			}

			HealthImage.fillAmount = (float) StatsSystem.GetStatDepletedPercentage(EntityID, GameStatsContainer.Health);
			
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

		public void SetSize(float playerSize)
		{
			CapsuleCollider capsuleCollider = GetComponent<CapsuleCollider>();
			capsuleCollider.radius = playerSize;
		}
	}
	
}
