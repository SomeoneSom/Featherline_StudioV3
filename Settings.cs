namespace Featherline;
public class Settings
{
    public static string? Favorite; 
    public static int Framecount = 120;

    public static int Population = 50;
    public static int Generations = 2000;
    public static int SurvivorCount = 20;

    public static float MutationMagnitude = 8;
    public static int MaxMutChangeCount = 5;

    public static string[]? Checkpoints;

    public static bool AvoidWalls = false;

    public static string[]? ManualHitboxes;

    public static bool FrameBasedOnly = false;
    public static bool TimingTestFavDirectly = false;
    public static int GensPerTiming = 150;

    public static int ShuffleCount = 6;

    public static int MaxThreadCount = Environment.ProcessorCount;

    public static List<object>? Info;

    public static string? Output;
}
