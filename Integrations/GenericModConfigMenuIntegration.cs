using Microsoft.Xna.Framework;
using StardewModdingAPI;

namespace GrowthInfoMod.Integrations
{
  /// <summary>
  /// Registra as opções do mod no Generic Mod Config Menu (GMCM).
  /// Chamado em OnGameLaunched do ModEntry.
  /// </summary>
  public static class GenericModConfigMenuIntegration
  {
    public static void Register(
        IModHelper helper,
        IManifest manifest,
        ModConfig config)
    {
      var gmcm = helper.ModRegistry
          .GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");

      if (gmcm is null) return;

      // ── Registro ──────────────────────────────────────────────────────
      gmcm.Register(
          mod: manifest,
          reset: () =>
          {
            config.ShowInfoBox = true;
            config.TextColor = Color.White;
            config.HoverDelaySeconds = 4f;
          },
          save: () => helper.WriteConfig(config)
      );

      // ── Geral ─────────────────────────────────────────────────────────
      gmcm.AddBoolOption(
          mod: manifest,
          name: () => "Mostrar tooltip",
          tooltip: () => "Ativa ou desativa todos os tooltips do mod.",
          getValue: () => config.ShowInfoBox,
          setValue: v => config.ShowInfoBox = v
      );

      gmcm.AddNumberOption(
          mod: manifest,
          name: () => "Delay do hover (segundos)",
          tooltip: () => "Tempo parado antes de o tooltip aparecer.\n0 = instantâneo | máx. 10 s",
          getValue: () => config.HoverDelaySeconds,
          setValue: v => config.HoverDelaySeconds = v,
          min: 0f,
          max: 10f,
          interval: 0.5f
      );

      // ── Cor do texto ──────────────────────────────────────────────────
      gmcm.AddSectionTitle(
          mod: manifest,
          text: () => "Cor do texto do tooltip"
      );

      gmcm.AddNumberOption(
          mod: manifest,
          name: () => "Vermelho (0-255)",
          getValue: () => config.TextColor.R,
          setValue: v => config.TextColor = new Color((int)v, config.TextColor.G, config.TextColor.B),
          min: 0f, max: 255f, interval: 1f
      );

      gmcm.AddNumberOption(
          mod: manifest,
          name: () => "Verde (0-255)",
          getValue: () => config.TextColor.G,
          setValue: v => config.TextColor = new Color(config.TextColor.R, (int)v, config.TextColor.B),
          min: 0f, max: 255f, interval: 1f
      );

      gmcm.AddNumberOption(
          mod: manifest,
          name: () => "Azul (0-255)",
          getValue: () => config.TextColor.B,
          setValue: v => config.TextColor = new Color(config.TextColor.R, config.TextColor.G, (int)v),
          min: 0f, max: 255f, interval: 1f
      );
    }
  }
}