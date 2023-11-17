#if SUBNAUTICA
	using Sprite = Atlas.Sprite;
#elif BELOWZERO
	using Sprite = UnityEngine.Sprite;
#endif

namespace Common
{
	static partial class SpriteHelper
	{
		public static Sprite getSprite(TechType spriteID) => SpriteManager.Get(spriteID);
	}
}