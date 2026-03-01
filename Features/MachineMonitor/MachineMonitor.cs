using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace GrowthInfoMod.Features.MachineMonitor
{
  /// <summary>
  /// Monitora máquinas com tempo de produção na fazenda (fornalha, barril, pote de geléia, etc.)
  /// e exibe um tooltip ao passar o mouse sobre elas.
  /// </summary>
  public class MachineMonitor
  {
    // ── Máquinas reconhecidas ─────────────────────────────────────────────
    private static readonly HashSet<string> MachineNames =
        new(StringComparer.OrdinalIgnoreCase)
    {
      "Keg", "Preserves Jar", "Cheese Press", "Mayonnaise Machine",
      "Oil Maker", "Loom", "Bee House", "Recycling Machine", "Seed Maker",
      "Furnace", "Kiln", "Crystalarium", "Geode Crusher", "Heavy Furnace",
      "Bone Mill", "Ostrich Incubator", "Slime Incubator", "Slime Egg-Press",
      "Charcoal Kiln", "Lightning Rod", "Solar Panel", "Mushroom Log",
      "Dehydrator", "Fish Smoker", "Bait Maker",
    };

    private static readonly Dictionary<string, string> MachineTranslations =
        new(StringComparer.OrdinalIgnoreCase)
    {
      { "Keg",                "Barril" },
      { "Preserves Jar",      "Pote de Conservas" },
      { "Cheese Press",       "Prensa de Queijo" },
      { "Mayonnaise Machine", "Máquina de Maionese" },
      { "Oil Maker",          "Prensa de Azeite" },
      { "Loom",               "Tear" },
      { "Bee House",          "Casa de Abelhas" },
      { "Recycling Machine",  "Recicladora" },
      { "Seed Maker",         "Fabricadora de Sementes" },
      { "Furnace",            "Fornalha" },
      { "Kiln",               "Forno Cerâmico" },
      { "Crystalarium",       "Cristalizador" },
      { "Geode Crusher",      "Trituradora de Geodos" },
      { "Heavy Furnace",      "Fornalha Pesada" },
      { "Bone Mill",          "Moinho de Ossos" },
      { "Ostrich Incubator",  "Incubadora de Avestruz" },
      { "Slime Incubator",    "Incubadora de Slime" },
      { "Slime Egg-Press",    "Prensa de Ovos de Slime" },
      { "Charcoal Kiln",      "Forno de Carvão" },
      { "Lightning Rod",      "Para-Raios" },
      { "Solar Panel",        "Painel Solar" },
      { "Mushroom Log",       "Tronco de Cogumelo" },
      { "Dehydrator",         "Desidratador" },
      { "Fish Smoker",        "Defumador de Peixe" },
      { "Bait Maker",         "Fabricadora de Isca" },
    };

    // ── Estado ────────────────────────────────────────────────────────────
    private readonly IMonitor Monitor;
    private readonly ModConfig Config;

    private string CurrentHoverText = "";
    private string PendingHoverText = "";
    private Vector2 LastMousePosition = Vector2.Zero;
    private Vector2 LastHoveredTile = new(-1, -1);
    private double TimeSinceHover = 0;

    private double HoverDelayMs => Config.HoverDelaySeconds * 1000.0;

    // ── Construtor ────────────────────────────────────────────────────────
    public MachineMonitor(IModHelper helper, IMonitor monitor, ModConfig config)
    {
      Monitor = monitor;
      Config = config;

      helper.Events.Display.RenderedHud += OnRenderedHud;
      helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
      helper.Events.Input.CursorMoved += OnCursorMoved;
    }

    // ── Eventos ───────────────────────────────────────────────────────────
    private void OnCursorMoved(object? sender, CursorMovedEventArgs e)
    {
      if (!Context.IsWorldReady) return;
      LastMousePosition = e.NewPosition.ScreenPixels;
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
      if (!Context.IsWorldReady || !e.IsMultipleOf(6)) return;
      if (Game1.currentLocation is not Farm) return;

      Vector2 tile = GetTileFromScreen(LastMousePosition);

      if (tile != LastHoveredTile)
      {
        LastHoveredTile = tile;
        TimeSinceHover = 0;
        CurrentHoverText = "";
        PendingHoverText = GetMachineInfo(tile);
        return;
      }

      if (!string.IsNullOrEmpty(PendingHoverText))
      {
        TimeSinceHover += 100;
        if (TimeSinceHover >= HoverDelayMs)
          CurrentHoverText = PendingHoverText;
      }
    }

    // ── Lógica de máquina ─────────────────────────────────────────────────
    private string GetMachineInfo(Vector2 tile)
    {
      var location = Game1.currentLocation;
      if (location is null) return "";

      if (!location.objects.TryGetValue(tile, out StardewValley.Object? obj) || obj is null)
        return "";

      if (!IsMachine(obj)) return "";

      string machineName = GetTranslatedName(obj.Name);

      // Vazia
      if (obj.heldObject.Value is null)
        return $"⚙️ {machineName}\n💤 Vazia";

      // Produzindo
      if (obj.MinutesUntilReady > 0)
      {
        string product = obj.heldObject.Value.DisplayName;
        string timeLeft = FormatMinutes(obj.MinutesUntilReady);
        string progress = BuildProgressBar(obj);

        return $"⚙️ {machineName}\n" +
               $"🔄 Produzindo: {product}\n" +
               $"⏱ Restante: {timeLeft}\n" +
               progress;
      }

      // Pronto
      {
        string product = obj.heldObject.Value.DisplayName;
        int quantity = obj.heldObject.Value.Stack;
        string qtd = quantity > 1 ? $" x{quantity}" : "";
        return $"⚙️ {machineName}\n✅ Pronto: {product}{qtd}";
      }
    }

    private static bool IsMachine(StardewValley.Object obj)
    {
      if (MachineNames.Contains(obj.Name)) return true;
      if (obj.bigCraftable.Value && obj.heldObject.Value is not null) return true;
      if (obj.bigCraftable.Value && obj.MinutesUntilReady > 0) return true;
      return false;
    }

    private static string GetTranslatedName(string name)
        => MachineTranslations.TryGetValue(name, out string? tr) ? tr : name;

    // ── Formatação ────────────────────────────────────────────────────────
    private static string FormatMinutes(int minutes)
    {
      if (minutes < 60) return $"{minutes} min";

      int h = minutes / 60;
      int m = minutes % 60;

      if (h >= 24)
      {
        int days = h / 24;
        int hrs = h % 24;
        return hrs > 0 ? $"{days}d {hrs}h {m}min" : $"{days}d {m}min";
      }

      return m > 0 ? $"{h}h {m}min" : $"{h}h";
    }

    private static string BuildProgressBar(StardewValley.Object obj)
    {
      int remaining = obj.MinutesUntilReady;
      int total = GetTotalTime(obj.Name);

      if (total <= 0 || remaining >= total) return "";

      float pct = 1f - (float)remaining / total;
      int filled = (int)(pct * 10);
      int empty = 10 - filled;

      return "[" + new string('█', filled) + new string('░', empty) + "] " + (int)(pct * 100) + "%";
    }

    private static int GetTotalTime(string name) => name switch
    {
      "Furnace" => 30,
      "Heavy Furnace" => 30,
      "Geode Crusher" => 60,
      "Bone Mill" => 240,
      "Charcoal Kiln" => 2880,
      "Keg" => 10000,
      "Preserves Jar" => 8400,
      "Cheese Press" => 3000,
      "Mayonnaise Machine" => 3000,
      "Oil Maker" => 6000,
      "Loom" => 4000,
      "Seed Maker" => 1440,
      "Recycling Machine" => 2880,
      "Crystalarium" => 10000,
      "Kiln" => 2880,
      "Solar Panel" => 7200,
      "Dehydrator" => 8640,
      "Fish Smoker" => 4320,
      "Bait Maker" => 3000,
      _ => 0
    };

    // ── Renderização ──────────────────────────────────────────────────────
    private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
    {
      if (!Context.IsWorldReady || !Config.ShowInfoBox) return;
      if (string.IsNullOrEmpty(CurrentHoverText)) return;
      if (Game1.activeClickableMenu is not null) return;
      if (Game1.currentLocation is not Farm) return;

      SpriteFont? font = Game1.smallFont;
      if (font is null) return;

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
        Monitor.Log($"[MachineMonitor] Erro ao desenhar tooltip: {ex.Message}", LogLevel.Trace);
      }
    }

    private static Vector2 GetTileFromScreen(Vector2 screenPixels)
        => new(
            (int)((screenPixels.X + Game1.viewport.X) / Game1.tileSize),
            (int)((screenPixels.Y + Game1.viewport.Y) / Game1.tileSize)
        );
  }
}