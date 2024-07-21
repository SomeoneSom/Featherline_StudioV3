namespace Featherline;

public struct Info {
    public (float, float) Speed;
    public (float, float) Pos;
    public (float, float) PosRemainder;
    public float Lerp;
    public List<(float, float)> Spinners;
    public List<(float, float)> LightningUL;
    public List<(float, float)> LightningDR;
    public List<(float, float)> SpikeUL;
    public List<(float, float)> SpikeDR;
    public List<int> SpikeDir;
    public (float, float) Wind;
    public List<(float, float)> WTPos;
    public List<int> WTPattern;
    public List<float> WTWidth;
    public List<float> WTHeight;
    public List<(float, float)> StarJumpUL;
    public List<(float, float)> StarJumpDR;
    public List<bool> StarJumpSinks;
    public List<(float, float)> JThruUL;
    public List<(float, float)> JThruDR;
    public List<(float, float)> SideJTUL;
    public List<(float, float)> SideJTDR;
    public List<bool> SideJTIsRight;
    public List<bool> SideJTPushes;
    public List<(float, float)> UpsJTUL;
    public List<(float, float)> UpsJTDR;
    public List<bool> UpsJTPushes;
    public int BoundsX;
    public int BoundsY;
    public int BoundsWidth;
    public int BoundsHeight;
    public string Solids;
}

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