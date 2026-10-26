using Core.Systems;

namespace Game
{
	public static class GameSystemsGroup
	{
		public static readonly SystemGroup DEBUG_SYSTEM = new SystemGroup("DEBUG_SYSTEM");
		public static readonly SystemGroup GAME_LOGIC_SYSTEM = new SystemGroup("GAME_LOGIC_SYSTEM");
		public static readonly SystemGroup GAME_VIEW_SYSTEM = new SystemGroup("GAME_VIEW_SYSTEM");
	}
}