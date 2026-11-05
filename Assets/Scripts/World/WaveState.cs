namespace ResortEmpire.World
{
    /// <summary>
    /// States of the wave loop (build document §4.3). The resting state is
    /// <see cref="AwaitingWave"/>; never call it "idle".
    /// </summary>
    public enum WaveState
    {
        /// <summary>Resort is ready for guests; the player can press <c>Start Wave</c>.</summary>
        AwaitingWave,

        /// <summary>The cruise ship sails in to the island (about 2–4 s).</summary>
        WaveIncoming,

        /// <summary>
        /// Guests disembark until <c>wave_timer</c> runs out, then the wave goes on until
        /// every guest has been served.
        /// </summary>
        WaveActive
    }
}
