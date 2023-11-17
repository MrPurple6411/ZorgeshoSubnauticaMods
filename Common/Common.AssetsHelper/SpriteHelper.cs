#if SUBNAUTICA
	using Sprite = Atlas.Sprite;
#elif BELOWZERO
	using Sprite = UnityEngine.Sprite;
#endif

namespace Common
{
	static partial class SpriteHelper
	{
		public static Sprite getSprite(string spriteID)
		{
			UnityEngine.Sprite sprite = AssetsHelper.loadSprite(spriteID);
#if SUBNAUTICA
			return sprite == null? null: new Sprite(sprite);
#elif BELOWZERO
			return sprite;
#endif
		}
	}
}