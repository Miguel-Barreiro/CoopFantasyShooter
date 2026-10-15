using System.Collections.Generic;
using Core.Model.Data;
using Core.Model.Stats;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Spells
{
	public sealed class SpellConfig : DataConfig
	{
		[SerializeField] private List<StatOverride> _Stats;
		public List<StatOverride> Stats => _Stats;
	}
}