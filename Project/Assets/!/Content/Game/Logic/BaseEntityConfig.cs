using System.Collections.Generic;
using Core.Model.Data;
using Core.Model.Stats;
using UnityEngine;

namespace Game.Logic
{
	
	public interface IBaseEntityConfig
	{
		public List<StatOverride> StatsOverride { get; }
	}
	
	public abstract class BaseEntityConfig : DataConfig, IBaseEntityConfig
	{
		[Header("STATS")]
		[SerializeField] private List<StatOverride> _Stats;
		public List<StatOverride> StatsOverride => _Stats;

	}
	
	public abstract class BaseVSEntityConfig : VSAbilityDataConfig, IBaseEntityConfig
	{
		
	}
}