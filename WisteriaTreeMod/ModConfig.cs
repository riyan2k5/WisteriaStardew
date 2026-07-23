namespace WisteriaTreeMod
{
    /// <summary>Mod configuration options, editable via Generic Mod Config Menu.</summary>
    public class ModConfig
    {
        /// <summary>
        /// When enabled, Wisteria trees advance one growth stage every day
        /// instead of following the normal 28-day season growth cycle.
        /// </summary>
        public bool FastGrowth { get; set; } = false;
    }
}
