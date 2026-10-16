using System.Collections.Generic;
using Core.Model.Data;
using Core.Model.Stats;
using UnityEngine;

namespace Game.Logic
{
	public abstract class BaseEntityConfig : DataConfig
	{
		
		[SerializeField] private List<StatOverride> _Stats;
		public List<StatOverride> Stats => _Stats;

	}
}