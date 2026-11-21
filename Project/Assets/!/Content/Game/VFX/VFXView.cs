using Core.View;
using UnityEngine;

namespace Game.VFX
{
	public class VFXView : EntityView
	{
		[SerializeField] private ParticleSystem ParticleSystem = null!;
		
		public override bool IsActive => gameObject.activeSelf;
		public override void ActivateGameObject()
		{
			ParticleSystem.gameObject.SetActive(true);
			gameObject.SetActive(true);
			// ParticleSystem.Play();
		}
		public override void DeactivateGameObject()
		{
			// ParticleSystem.Clear();
			ParticleSystem.Stop();
			gameObject.SetActive(false);
		}

		public virtual bool UpdateView(float deltaTime, ref VFXData vfxData)
		{
			switch (vfxData.DataConfig.Type)
			{
				case VFXType.Projectile:
					UpdateProjectile(ref vfxData, deltaTime);
					break;
			}
			
			
			if(ParticleSystem.isPlaying)
				return true;
			else
				return false;
		}

		public virtual void Play()
		{
			ParticleSystem.Play();
		}
		

		private void UpdateProjectile(ref VFXData vfxData, float deltaTime)
		{
			vfxData.ElapsedTime += deltaTime;
			float progress = vfxData.ElapsedTime / vfxData.Duration;
			if(progress > 1f)
				progress = 1f;

			float zProgress = vfxData.DataConfig.ZAnimation.Evaluate(progress);
			float yProgress = vfxData.DataConfig.YAnimation.Evaluate(progress);

			Vector3 newPosition = Vector3.Lerp(vfxData.Origin, vfxData.Target, zProgress) + new Vector3(0f, yProgress, 0f);

			transform.position = newPosition;
		}

	}
	
}