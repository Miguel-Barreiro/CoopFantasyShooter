using System;

namespace Game
{
	[Flags]
	[Serializable]
	public enum GameTag : int
	{
		None = 0,
		Player = 1 << 0,
		Enemy = 1 << 1,
		Projectile = 1 << 2,
		
		
		
		All = int.MaxValue,
	}
}