namespace Common
{
	static partial class Mod
	{
		public static class Consts
		{
			public const bool isDevBuild =
#if DEBUG
				true;
#else
				false;
#endif

			public const bool isBranchStable =
#if BRANCH_STABLE
				true;
#else
				false;
#endif

			public const bool isGameSN =
#if SUBNAUTICA
				true;
#else
				false;
#endif
			public const bool isGameSNStable =
#if SUBNAUTICA && BRANCH_STABLE
				true;
#else
				false;
#endif
			public const bool isGameSNExp =
#if SUBNAUTICA && BRANCH_EXP
				true;
#else
				false;
#endif

			public const bool isGameBZ =
#if BELOWZERO
				true;
#else
				false;
#endif
		}
	}
}