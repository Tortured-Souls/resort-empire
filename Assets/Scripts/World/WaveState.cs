namespace ResortEmpire.World
{
    /// <summary>
    /// States of the wave loop (build document §4.3). The resting state is
    /// <see cref="AwaitingWave"/>; never call it "idle". The loop runs
    /// <see cref="AwaitingWave"/> → (<c>Start Wave</c>) → <see cref="WaveIncoming"/> →
    /// <see cref="WaveActive"/> → <see cref="WaveResolving"/> → <see cref="WaveComplete"/> →
    /// (<c>Claim Rewards</c>) → <see cref="Upgrade"/> → (upgrade or skip) → <see cref="AwaitingWave"/>.
    /// Sending the app to the background turns <see cref="WaveActive"/> into
    /// <see cref="WavePaused"/> until it comes back.
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
        WaveActive,

        /// <summary>
        /// The arrival is closed and every guest has been served; the wave is evaluated. Lasts one
        /// frame, then the result follows.
        /// </summary>
        WaveResolving,

        /// <summary>
        /// The result of the wave (S4) waits until the player claims the rewards; claiming ends
        /// the wave.
        /// </summary>
        WaveComplete,

        /// <summary>
        /// The optional suggested upgrade (S5) and a newly available area unlock (S9). Buildings
        /// can be upgraded and plots bought until the player confirms or skips it.
        /// </summary>
        Upgrade,

        /// <summary>
        /// The app is in the background during <see cref="WaveActive"/>: the arrival timer and
        /// every guest are frozen until it comes back.
        /// </summary>
        WavePaused
    }
}
