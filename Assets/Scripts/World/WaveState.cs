namespace ResortEmpire.World
{
    /// <summary>
    /// States of the wave loop (build document §4.3). The resting state is
    /// <see cref="AwaitingWave"/>; never call it "idle". The loop runs
    /// <see cref="WaveIncoming"/> → <see cref="AwaitingWave"/> → <see cref="WaveActive"/> →
    /// <see cref="WaveIncoming"/>; rejecting a ship goes from <see cref="AwaitingWave"/> back to
    /// <see cref="WaveIncoming"/>.
    /// </summary>
    public enum WaveState
    {
        /// <summary>
        /// A cruise ship lies at the dock and the resort is ready for guests. The player either
        /// presses <c>Start Wave</c> to let its guests disembark or <c>Reject</c> to send it away.
        /// </summary>
        AwaitingWave,

        /// <summary>
        /// The next cruise ship sails in to the island (about 2–4 s) once the previous one has
        /// left: at the start of the game, after a wave, and after a rejected ship.
        /// </summary>
        WaveIncoming,

        /// <summary>
        /// Guests disembark until <c>wave_timer</c> runs out, then the wave goes on until
        /// every guest has been served.
        /// </summary>
        WaveActive
    }
}
