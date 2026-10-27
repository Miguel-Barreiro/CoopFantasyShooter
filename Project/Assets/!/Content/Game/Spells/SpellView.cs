using Core.View;

namespace Game.Spells
{
	public class SpellView : EntityView
	{
		public override bool IsActive { get => gameObject.activeSelf; } 
		
		public override void ActivateGameObject()
		{
			gameObject.SetActive(true);
		}
		public override void DeactivateGameObject()
		{
			gameObject.SetActive(false);
		}
	}
}