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

		public virtual bool UpdateView(float deltaTime)
		{
			if(ParticleSystem.isPlaying)
				return true;
			else
				return false;
		}

		public virtual void Play()
		{
			ParticleSystem.Play();
		}
	}
	
}