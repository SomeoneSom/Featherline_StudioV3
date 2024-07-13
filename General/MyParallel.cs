namespace Featherline;

static class MyParallel
{
    public static Action<int, int, Action<int>>? Run;

    public static void Initialize()
    {
        var opt = new ParallelOptions() { MaxDegreeOfParallelism = Settings.MaxThreadCount };

        Run = Settings.MaxThreadCount == 1 ? NonParallel : (start, to, Act) => Parallel.For(start, to, opt, Act);

        void NonParallel(int start, int to, Action<int> Act)
        {
            for (int i = start; i < to; i++)
                Act(i);
        }
    }
}
