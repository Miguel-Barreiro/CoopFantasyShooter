using Core.Model;

namespace Game.StatusEffects
{
	public interface IStatusEffectOwnerView
	{
		public void OnStatusEffectAdded(EntId statusEffectId, EntId ownerId, ref StatusEffectData statusEffectData);
		public void OnStatusEffectRemoved(EntId statusEffectId, EntId ownerId, ref StatusEffectData statusEffectData);
	}
}