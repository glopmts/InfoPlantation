using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.TerrainFeatures;
using StardewValley.Menus;

namespace GrowthInfoMod
{
  public class ModEntry : Mod
  {
    private ModConfig Config;

    private ITranslationHelper I18n => Helper.Translation;

    private string CurrentHoverText = "";
    private string PendingHoverText = "";
    private Vector2 LastMousePosition;
    private Vector2 LastCheckedTile = new Vector2(-1, -1);
    private Vector2 LastHoveredTile = new Vector2(-1, -1);
    private double TimeSinceHoverStarted = 0;

    // Lê o delay diretamente do Config, convertendo para ms
    private double HoverDelayMs => Config.HoverDelaySeconds * 1000.0;


    public override void Entry(IModHelper helper)
    {
      Config = helper.ReadConfig<ModConfig>();

      helper.Events.Display.RenderedHud += OnRenderedHud;
      helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
      helper.Events.Input.CursorMoved += OnCursorMoved;
      helper.Events.GameLoop.GameLaunched += OnGameLaunched;
    }

    // ──────────────────────────────────────────────
    // Registro do Generic Mod Config Menu (GMCM)
    // ──────────────────────────────────────────────
    private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
    {
      var gmcm = Helper.ModRegistry
          .GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");

      // GMCM não instalado → sem menu, sem crash
      if (gmcm is null) return;

      gmcm.Register(
          mod: ModManifest,
          reset: () => Config = new ModConfig(),
          save: () => Helper.WriteConfig(Config)
      );

      // ── Mostrar tooltip ─────────────────────────
      gmcm.AddBoolOption(
          mod: ModManifest,
          name: () => "Mostrar tooltip",
          tooltip: () => "Ativa ou desativa a exibição das informações de crescimento.",
          getValue: () => Config.ShowInfoBox,
          setValue: v => Config.ShowInfoBox = v
      );

      // ── Delay do hover ──────────────────────────
      gmcm.AddNumberOption(
          mod: ModManifest,
          name: () => "Delay do hover (segundos)",
          tooltip: () => "Tempo que o mouse deve ficar parado sobre o tile\n" +
                          "antes de o tooltip aparecer.\n" +
                          "0 = instantâneo  |  máx. 10 s",
          getValue: () => Config.HoverDelaySeconds,
          setValue: v => Config.HoverDelaySeconds = v,
          min: 0f,
          max: 10f,
          interval: 0.5f     // slider em passos de 0,5 s
      );

      // ── Cor do texto (R / G / B) ─────────────────
      gmcm.AddSectionTitle(
          mod: ModManifest,
          text: () => "Cor do texto do tooltip"
      );

      gmcm.AddNumberOption(
          mod: ModManifest,
          name: () => "Vermelho (0-255)",
          getValue: () => (float)Config.TextColor.R,
          setValue: v => Config.TextColor = new Color((int)v, Config.TextColor.G, Config.TextColor.B),
          min: 0, max: 255, interval: 1
      );

      gmcm.AddNumberOption(
          mod: ModManifest,
          name: () => "Verde (0-255)",
          getValue: () => (float)Config.TextColor.G,
          setValue: v => Config.TextColor = new Color(Config.TextColor.R, (int)v, Config.TextColor.B),
          min: 0, max: 255, interval: 1
      );

      gmcm.AddNumberOption(
          mod: ModManifest,
          name: () => "Azul (0-255)",
          getValue: () => (float)Config.TextColor.B,
          setValue: v => Config.TextColor = new Color(Config.TextColor.R, Config.TextColor.G, (int)v),
          min: 0, max: 255, interval: 1
      );
    }

    // ──────────────────────────────────────────────
    // Lógica de hover com delay dinâmico
    // ──────────────────────────────────────────────
    private void OnCursorMoved(object sender, CursorMovedEventArgs e)
    {
      if (!Context.IsWorldReady) return;
      LastMousePosition = e.NewPosition.ScreenPixels;
    }

    private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
    {
      if (!Context.IsWorldReady || !e.IsMultipleOf(6)) return;

      Vector2 tile = GetTileFromScreen(LastMousePosition);

      // Mouse mudou de tile → reseta
      if (tile != LastHoveredTile)
      {
        LastHoveredTile = tile;
        TimeSinceHoverStarted = 0;
        CurrentHoverText = "";

        PendingHoverText = "";
        LastCheckedTile = tile;
        UpdatePendingInfo(tile);
        return;
      }

      // Mouse parado: +100 ms por chamada (IsMultipleOf(6) @ 60tps = 10×/s)
      if (!string.IsNullOrEmpty(PendingHoverText))
      {
        TimeSinceHoverStarted += 100;

        if (TimeSinceHoverStarted >= HoverDelayMs)
          CurrentHoverText = PendingHoverText;
      }
    }

    private Vector2 GetTileFromScreen(Vector2 screenPixels)
    {
      return new Vector2(
          (int)((screenPixels.X + Game1.viewport.X) / Game1.tileSize),
          (int)((screenPixels.Y + Game1.viewport.Y) / Game1.tileSize)
      );
    }

    private void UpdatePendingInfo(Vector2 tile)
    {
      PendingHoverText = "";

      var location = Game1.currentLocation;
      if (location == null) return;

      if (location.terrainFeatures.TryGetValue(tile, out TerrainFeature feature))
      {
        if (feature is HoeDirt dirt && dirt.crop != null)
        {
          PendingHoverText = GetCropInfo(dirt.crop);
          return;
        }
      }

      if (location.objects.TryGetValue(tile, out StardewValley.Object obj))
      {
        if (IsSeed(obj))
          PendingHoverText = GetSeedInfo(obj);
      }
    }

    // ──────────────────────────────────────────────
    // Helpers de informação
    // ──────────────────────────────────────────────
    private bool IsSeed(StardewValley.Object obj)
    {
      return obj.Category == StardewValley.Object.SeedsCategory
          || obj.Type == "Seeds"
          || obj.Name.Contains("Seeds", StringComparison.OrdinalIgnoreCase)
          || obj.Name.Contains("Seed", StringComparison.OrdinalIgnoreCase);
    }

    private string GetCropInfo(Crop crop)
    {
      if (crop == null) return "";

      if (crop.dead.Value)
        return I18n.Get("crop.dead");

      int currentPhase = crop.currentPhase.Value;
      var phaseDays = crop.phaseDays;
      int totalPhases = phaseDays.Count;
      int regrowDays = crop.GetData()?.RegrowDays ?? -1;

      string cropName = "?";
      try
      {
        var harvestData = ItemRegistry.GetData(crop.indexOfHarvest.Value.ToString());
        if (harvestData != null)
          cropName = harvestData.DisplayName;
      }
      catch { }

      if (currentPhase >= totalPhases - 1)
      {
        string key = regrowDays > 0 ? "crop.ready.regrow" : "crop.ready";
        return I18n.Get(key, new { name = cropName, days = regrowDays });
      }

      int daysLeftInPhase = crop.dayOfCurrentPhase.Value;
      int totalDaysLeft = daysLeftInPhase;
      for (int i = currentPhase + 1; i < totalPhases - 1; i++)
        totalDaysLeft += phaseDays[i];

      string phaseLabel = GetPhaseName(currentPhase, totalPhases);
      string extra = regrowDays > 0
          ? I18n.Get("crop.info.regrow", new { days = regrowDays })
          : "";

      return I18n.Get("crop.info", new
      {
        name = cropName,
        phase = phaseLabel,
        current = currentPhase + 1,
        total = totalPhases - 1,
        daysPhase = daysLeftInPhase,
        daysTotal = totalDaysLeft
      }) + extra;
    }

    private string GetPhaseName(int phase, int totalPhases)
    {
      float progress = (float)phase / (totalPhases - 2);
      return progress switch
      {
        < 0.25f => I18n.Get("phase.seed"),
        < 0.5f => I18n.Get("phase.sprout"),
        < 0.75f => I18n.Get("phase.growing"),
        _ => I18n.Get("phase.almost")
      };
    }

    private string GetSeedInfo(StardewValley.Object seed)
    {
      if (seed == null) return "";

      string cropName = seed.Name
          .Replace("Seeds", "", StringComparison.OrdinalIgnoreCase)
          .Replace("Seed", "", StringComparison.OrdinalIgnoreCase)
          .Trim();

      string growthInfo = GetStaticSeedInfo(seed.ItemId);

      return string.IsNullOrEmpty(growthInfo)
          ? $"🌾 {seed.Name}"
          : $"🌾 {cropName}\n{growthInfo}";
    }

    private string GetStaticSeedInfo(string itemId)
    {
      var info = new Dictionary<string, string>
      {
        { "472", "Pastinaga — 4 dias" },
        { "473", "Feijão Verde — 10 dias (treliça, repetível)" },
        { "474", "Couve-flor — 12 dias" },
        { "475", "Batata — 6 dias" },
        { "476", "Tulipa — 6 dias" },
        { "477", "Alho — 7 dias" },
        { "478", "Couve — 8 dias (repetível)" },
        { "479", "Melancia — 16 dias" },
        { "480", "Tomate — 11 dias (repetível)" },
        { "481", "Mirtilo — 13 dias (repetível)" },
        { "482", "Pimenta — 10 dias (repetível)" },
        { "483", "Trigo — 4 dias" },
        { "484", "Rabanete — 3 dias" },
        { "485", "Girassol — 8 dias" },
        { "486", "Milho — 14 dias (repetível, verão/outono)" },
        { "487", "Berinjela — 7 dias (repetível)" },
        { "488", "Uva — 10 dias (repetível)" },
        { "489", "Abóbora — 13 dias" },
        { "490", "Inhame — 6 dias" },
        { "491", "Oxicoco — 7 dias (repetível)" },
        { "492", "Repolho — 6 dias" },
        { "745", "Morango — 8 dias (repetível)" },
        { "499", "Semente Antiga — 28 dias" },
        { "628", "Cerejeira — 28 dias" },
        { "629", "Abacateiro — 28 dias" },
        { "630", "Macieira — 28 dias" },
        { "631", "Laranjeira — 28 dias" },
        { "632", "Pêssego — 28 dias" },
        { "633", "Romã — 28 dias" },
      };

      return info.TryGetValue(itemId, out string result) ? result : null;
    }

    // ──────────────────────────────────────────────
    // Renderização do tooltip
    // ──────────────────────────────────────────────
    private void OnRenderedHud(object sender, RenderedHudEventArgs e)
    {
      if (!Context.IsWorldReady || !Config.ShowInfoBox) return;
      if (string.IsNullOrEmpty(CurrentHoverText)) return;
      if (Game1.activeClickableMenu != null) return;

      SpriteFont font = Game1.smallFont;
      if (font == null) return;

      int mouseX = Game1.getMouseX();
      int mouseY = Game1.getMouseY();

      Vector2 textSize = font.MeasureString(CurrentHoverText);
      int padding = 20;
      int boxWidth = (int)textSize.X + padding * 2;
      int boxHeight = (int)textSize.Y + padding;

      int boxX = mouseX + 20;
      int boxY = mouseY + 20;

      if (boxX + boxWidth > Game1.uiViewport.Width) boxX = mouseX - boxWidth - 10;
      if (boxY + boxHeight > Game1.uiViewport.Height) boxY = mouseY - boxHeight - 10;

      boxX = Math.Max(5, boxX);
      boxY = Math.Max(5, boxY);

      try
      {
        IClickableMenu.drawTextureBox(
            e.SpriteBatch,
            boxX - 4, boxY - 4,
            boxWidth + 8, boxHeight + 8,
            Color.White
        );

        e.SpriteBatch.DrawString(
            font,
            CurrentHoverText,
            new Vector2(boxX + padding, boxY + padding / 2),
            Config.TextColor
        );
      }
      catch (Exception ex)
      {
        Monitor.Log($"Erro ao desenhar tooltip: {ex.Message}", LogLevel.Trace);
      }
    }
  }
}