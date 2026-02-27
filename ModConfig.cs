using Microsoft.Xna.Framework;

namespace GrowthInfoMod
{
  public class ModConfig
  {
    public bool ShowInfoBox { get; set; } = true;
    public float BoxOpacity { get; set; } = 0.85f;
    public Color TextColor { get; set; } = Color.Black;
    public float HoverDelaySeconds { get; set; } = 0.5f;

  }
}