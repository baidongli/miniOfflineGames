namespace MiniGames.GameModule.Random
{
    /// <summary>
    /// Game-deterministic random source. Same seed produces the same
    /// sequence on every device, which is what multiplayer parity needs.
    ///
    /// Tests can inject FixedSequenceRng to control outcomes precisely.
    /// </summary>
    public interface IRng
    {
        int NextInt();
        int NextInt(int maxExclusive);
        int NextInt(int minInclusive, int maxExclusive);
        double NextDouble();
    }

    public static class RngFactory
    {
        public static IRng FromSeed(int seed) => new SystemRng(seed);
    }
}
