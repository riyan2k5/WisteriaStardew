#nullable enable
using System;
using StardewModdingAPI;

namespace WisteriaTreeMod
{
    /// <summary>
    /// Minimal API surface for Generic Mod Config Menu (spacechase0.GenericModConfigMenu).
    /// Only the methods we actually call are declared here.
    /// </summary>
    public interface IGenericModConfigMenuApi
    {
        void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);

        void AddSectionTitle(IManifest mod, Func<string> text, Func<string>? tooltip = null);

        void AddBoolOption(
            IManifest mod,
            Func<bool> getValue,
            Action<bool> setValue,
            Func<string> name,
            Func<string>? tooltip = null,
            string? fieldId = null);
    }
}
