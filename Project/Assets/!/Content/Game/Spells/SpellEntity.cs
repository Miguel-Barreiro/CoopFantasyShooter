using System.Runtime.InteropServices;
using Core.Model;
using Core.Model.ModelSystems;
using Core.Systems;

namespace Game.Spells
{
	public abstract class SpellEntity : Entity, 
										ISpell, 
										IVSAbility
	{
		protected SpellEntity(SpellConfig dataConfig) : base() 
		{
			ref VSAbilityData abilityData = ref GetComponent<VSAbilityData>();
			abilityData.dataConfig = dataConfig;
		}

	}

	[StructLayout(LayoutKind.Auto)]
	public struct SpellData : IComponentData
	{
		public EntId ID { get; set; }
		

		public void Init() { }
	}

	public interface ISpell : Component<SpellData>, 
							IVSAbility 
	{ }
}