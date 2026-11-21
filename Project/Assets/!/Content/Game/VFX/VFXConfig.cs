using Core.Model.Data;
using UnityEngine;

namespace Game.VFX
{
	public sealed class VFXConfig : DataConfig
	{
		[SerializeField] private VFXType _Type;
		public VFXType Type => _Type;
		
		[SerializeField] private VFXView _Prefab;
		public VFXView Prefab => _Prefab;


		[SerializeField] private float _Duration;
		public float Duration => _Duration;

		
		
		

		[SerializeField] private AnimationCurve _YAnimation;
		public AnimationCurve YAnimation => _YAnimation;

		[SerializeField] private AnimationCurve _ZAnimation;
		public AnimationCurve ZAnimation => _ZAnimation;
		
	}
	
	
	public enum VFXType
	{
		Static, 
		Projectile
	}
}
