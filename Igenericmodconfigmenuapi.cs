using System;
using Microsoft.Xna.Framework;
using StardewModdingAPI;

namespace GrowthInfoMod
{
  /// <summary>
  /// Interface mínima do Generic Mod Config Menu (GMCM).
  /// Copie este arquivo para o seu projeto — não é necessário referenciar
  /// a DLL do GMCM diretamente; o SMAPI resolve em tempo de execução.
  /// </summary>
  public interface IGenericModConfigMenuApi
  {
    /// <summary>Registra o mod no menu de configurações.</summary>
    void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);

    /// <summary>Adiciona um título de seção.</summary>
    void AddSectionTitle(IManifest mod, Func<string> text, Func<string> tooltip = null);

    /// <summary>Adiciona uma opção booleana (checkbox).</summary>
    void AddBoolOption(
        IManifest mod,
        Func<bool> getValue,
        Action<bool> setValue,
        Func<string> name,
        Func<string> tooltip = null,
        string fieldId = null
    );

    /// <summary>Adiciona uma opção numérica (slider).</summary>
    void AddNumberOption(
        IManifest mod,
        Func<float> getValue,
        Action<float> setValue,
        Func<string> name,
        Func<string> tooltip = null,
        float? min = null,
        float? max = null,
        float? interval = null,
        Func<float, string> formatValue = null,
        string fieldId = null
    );
  }
}