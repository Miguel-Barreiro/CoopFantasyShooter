using System.Runtime.InteropServices;
using Core.Model;
using Core.Model.ModelSystems;
using Game.Logic;

namespace Game.Spells
{
	public abstract class SpellEntity : Entity, 
										ISpell, 
										IVSAbility
	{
		public override ulong GetStartTags() => (ulong)GameTag.Projectile;
		
		protected SpellEntity(SpellConfig dataConfig) : base() 
		{
			ref VSAbilityData abilityData = ref GetComponent<VSAbilityData>();
			abilityData.dataConfig = dataConfig;

			ref SpellData spellData = ref GetComponent<SpellData>();
			spellData.SpellType = dataConfig.SpellType;
		}

	}

	[StructLayout(LayoutKind.Auto)]
	public struct SpellData : IComponentData
	{
		public EntId ID { get; set; }
		
		public SpellType SpellType;

		public void Init() { }
	}

	public interface ISpell : Component<SpellData>, 
							IVSAbility , 
							ISimulation
	{ }
}