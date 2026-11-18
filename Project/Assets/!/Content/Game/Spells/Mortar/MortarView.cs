using DTT.AreaOfEffectRegions;
using FixedPointy;
using UnityEngine;

namespace Game.Spells.Mortar
{
	
	
	
	public sealed class MortarView : SpellView
	{
		[SerializeField] private CircleRegion TargetCircle;

		public void UpdateView(Fix deltaTime, Fix fillProgress, Fix radius)
        {
            TargetCircle.FillProgress = (float) fillProgress;
			TargetCircle.Radius = (float) radius;
		}

		public override void ActivateGameObject()
		{
			TargetCircle.FillProgress = 0f;
			gameObject.SetActive(true);
		}
	}
}