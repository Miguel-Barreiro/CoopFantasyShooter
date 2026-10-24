using System.Collections.Generic;
using Core.Model.Data;
using UnityEngine;

namespace Game.Enemies.EnenmySpawners
{
	public sealed class EnemySpawnerConfig : DataConfig
	{

		[SerializeField] private List<EnemyConfig> _Enemies;
		public List<EnemyConfig> Enemies => _Enemies;

		
	}
}