using System.Text.RegularExpressions;
using System.Globalization;
using System.Collections;
using System.Security.AccessControl;

namespace Featherline;

public static class Level
{
    public static DeathMapInfo? DeathMap;

    public static IntVec2[]? Spinners;
    public static RectangleHitbox[]? Killboxes;
    public static Spike[]? Spikes;

    public static SolidTileInfo? Tiles;

    public static WindTrigger[]? WindTriggers;
    public static Vector2 InitWind;

    public static bool HasHazards;

    public static RectangleHitbox[]? Colliders;

    public static NormalJT[]? NormalJTs;
    public static CustomJT[]? CustomJTs;

    public static Checkpoint[]? Checkpoints;

    public static Savestate? startState;
    public static Savestate? StartState { get => startState?.Copy(); }
    public static bool defineStartBoost;

    private static Regex getIntPair = new Regex(@"(-?\d+)\.?\d*, (-?\d+)\.?\d*");
    private static IntVec2 MatchToIntVec2(Match src) => new IntVec2(int.Parse(src.Groups[1].Value), int.Parse(src.Groups[2].Value));
    private static IntVec2[] GetIntPairs(string src) => getIntPair.Matches(src).Select(MatchToIntVec2).ToArray();
    private static bool[] GetBools(string src) => Regex.Matches(src, @"True|False").Select(s => s.Value == "True").ToArray();

    public static void PermanentDistFilter(AngleSet ind)
    {
        var poses = new FeatherSim().GetAllFrameData(ind, out _, out _)
            .Select(st => st.fState.pos).ToArray();
        Spinners = Spinners?.Where(spnr => poses.Any(p => p.Dist(spnr) < 50)).ToArray();
        Killboxes = Killboxes?.Where(RectBoxClose).ToArray();
        Spikes = Spikes?.Where(RectBoxClose).ToArray();
        WindTriggers = WindTriggers?.Where(RectBoxClose).ToArray();
        Colliders = Colliders?.Where(RectBoxClose).ToArray();
        NormalJTs = NormalJTs?.Where(RectBoxClose).ToArray();
        CustomJTs = CustomJTs?.Where(RectBoxClose).ToArray();

        bool RectBoxClose(RectangleHitbox hb) => poses.Any(p => hb.GetActualDistance(p) < 50);
    }

    public static void Prepare()
    {
        Colliders = new RectangleHitbox[0];
        Killboxes = new RectangleHitbox[0];

        GetStartState();

        GetSpinners();
        GetLightning();
        GetSpikes();

        GetWind();

        GetStaticTileEntities();
        GetJumpThrus();

        GetSolidTiles();

        Settings.ManualHitboxes ??= new string[0];
        GetCustomHitboxes();
        Settings.Checkpoints ??= new string[0];
        GetCheckpoints();

        CreateDangerBitfield();
    }

    private static void GetStartState()
    {
        startState = new Savestate();

        startState.fState.spd = new Vector2(Settings.Info.Player.Speed);
        startState.fState.moveCounter = new Vector2(Settings.Info.Player.PositionRemainder);
        startState.fState.pos = new IntVec2(new Vector2(Settings.Info.Player.Position) - new Vector2(Settings.Info.Player.PositionRemainder));
        startState.fState.lerp = Settings.Info.Player.starFlySpeedLerp;

        if (startState.fState.spd.X == 0 && startState.fState.spd.Y == 0)
            throw new ArgumentException();
    }

    private static void GetSpinners()
    {
        Spinners = GetIntVecs(Settings.Info.Spinners);
    }

    private static void GetLightning()
    {
        Killboxes = Killboxes.Concat(Settings.Info.Lightning.Select(m => new RectangleHitbox(new Bounds(m.X, m.Y, m.W, m.H).Expand(false)
            ))).ToArray();
    }

    private static void GetSpikes()
    {
        Spikes = Settings.Info.Spikes.Select(v => new Spike(new Bounds(v.X, v.Y, v.W, v.H).Expand(false), nameof(v.Direction))).ToArray();
    }

    private static void GetJumpThrus()
    {
        var normalJTs = new List<NormalJT>();
        var customJTs = new List<CustomJT>();
        foreach (var v in Settings.Info.JumpThrus) {
            switch (v.Direction) {
                case StudioCommunication.GameState.Direction.Up:
                    normalJTs.Add(new NormalJT(new Bounds(v.X, v.Y, v.W, v.H).Expand(true)));
                    break;
                case StudioCommunication.GameState.Direction.Right:
                    customJTs.Add(new CustomJT(new Bounds(v.X, v.Y, v.W, v.H).Expand(true), Facings.Right, v.PullsPlayer));
                    break;
                case StudioCommunication.GameState.Direction.Left:
                    customJTs.Add(new CustomJT(new Bounds(v.X, v.Y, v.W, v.H).Expand(true), Facings.Left, v.PullsPlayer));
                    break;
                case StudioCommunication.GameState.Direction.Down:
                    customJTs.Add(new CustomJT(new Bounds(v.X, v.Y, v.W, v.H).Expand(true), Facings.Down, v.PullsPlayer));
                    break;
            }
        }
        NormalJTs = normalJTs.ToArray();
        CustomJTs = customJTs.ToArray();
    }

    private static void GetWind()
    {
        InitWind = new Vector2(Settings.Info.Level.WindDirection);

        var listWT = new List<WindTrigger>();

        foreach (var wt in Settings.Info.WindTriggers) {
            (bool vertical, float stren, bool valid) pattern = wt.Pattern switch {
                StudioCommunication.GameState.WindPattern.Left => (false, -40, true),
                StudioCommunication.GameState.WindPattern.LeftStrong => (false, -80, true),
                StudioCommunication.GameState.WindPattern.Right => (false, 40, true),
                StudioCommunication.GameState.WindPattern.RightStrong => (false, 80, true),
                StudioCommunication.GameState.WindPattern.RightCrazy => (false, 120, true),
                StudioCommunication.GameState.WindPattern.Up => (true, -40, true),
                StudioCommunication.GameState.WindPattern.Down => (true, 30, true),
                StudioCommunication.GameState.WindPattern.None => (false, 0, true),
                _ => (false, 0, false)
            };

            if (!pattern.valid) {
                Console.WriteLine($"There was a wind trigger pattern \"{wt.Pattern}\" that couldn't be processed.\n The algorithm will run without accounting for it after you press enter.");
                Console.ReadLine();
                continue;
            }

            listWT.Add(new WindTrigger(new IntVec2((int)wt.X, (int)wt.Y), new IntVec2((int)wt.W, (int)wt.H), pattern.vertical, pattern.stren));
        }

        WindTriggers = listWT.ToArray();

        FeatherSim.doWind = InitWind.X != 0 || InitWind.Y != 0 || WindTriggers.Length != 0;
    }

    private static void CreateDangerBitfield()
    {
        try {
            var hazards = Spinners?.Select(s => new IntVec2(s.X / 8 * 8, s.Y / 8 * 8))
                .Concat(Killboxes.Select(k => new IntVec2(k.bounds.L / 8 * 8, k.bounds.U / 8 * 8)))
                .Concat(Killboxes.Select(k => new IntVec2(k.bounds.R / 8 * 8, k.bounds.D / 8 * 8)))
                .Concat(Spikes.Select(s => new IntVec2(s.bounds.L / 8 * 8, s.bounds.U / 8 * 8)))
                .Concat(Spikes.Select(s => new IntVec2(s.bounds.R / 8 * 8, s.bounds.D / 8 * 8)))
                .ToArray();

            DeathMap = new DeathMapInfo() {
                xMin = hazards.Min(s => s.X) - 24,
                xMax = hazards.Max(s => s.X) + 24,
                yMin = hazards.Min(s => s.Y) - 24,
                yMax = hazards.Max(s => s.Y) + 24
            };

            DeathMap.widthInTiles = (DeathMap.xMax - DeathMap.xMin) / 8;
            DeathMap.heightInTiles = (DeathMap.yMax - DeathMap.yMin) / 8;

            HasHazards = true;
        }
        catch {
            HasHazards = false;
            return;
        }

        DeathMap.map =
            new BitArray[(DeathMap.xMax - DeathMap.xMin) / 8 + 1].Select(b =>
            new BitArray((DeathMap.yMax - DeathMap.yMin) / 8 + 1)).ToArray();

        for (int i = 0; i < Spinners.Length; i++)
            AddSingleSpinnerCollision(Spinners[i]);

        for (int i = 0; i < Killboxes.Length; i++)
            AddSingleKillboxCollision(Killboxes[i]);

        for (int i = 0; i < Spikes.Length; i++)
            AddSingleKillboxCollision(Spikes[i]);
    }
    private static void AddSingleSpinnerCollision(IntVec2 s)
    {
        var bitIndex = new IntVec2(
            (s.X - DeathMap.xMin) / 8,
            (s.Y - DeathMap.yMin) / 8);

        int xStart = Math.Max(bitIndex.X - 2, 0);
        int xEnd = Math.Min(bitIndex.X + 2, DeathMap.widthInTiles);
        int yStart = Math.Max(bitIndex.Y - 2, 0);
        int yEnd = Math.Min(bitIndex.Y + 2, DeathMap.heightInTiles);

        for (int y = yStart; y <= yEnd; y++) {
            int yCoord = DeathMap.yMin + y * 8 + 1;

            for (int x = xStart; x <= xEnd; x++) {
                int xCoord = DeathMap.xMin + x * 8;

                if (SingleSpinnerColl(xCoord, yCoord) ||
                    SingleSpinnerColl(xCoord + 7, yCoord) ||
                    SingleSpinnerColl(xCoord, yCoord + 7) ||
                    SingleSpinnerColl(xCoord + 7, yCoord + 7))

                    DeathMap.map[x][y] = true;

                bool SingleSpinnerColl(int x, int y) =>
                    (x - s.X).Square() + (y - 4 - s.Y).Square() < 200d
                    && ((s.X - 7 < x && x < s.X + 7 && s.Y - 3 < y && y < s.Y + 15) || // tall
                        (s.X - 8 < x && x < s.X + 8 && s.Y - 2 < y && y < s.Y + 14) || // square
                        (s.X - 9 < x && x < s.X + 9 && s.Y - 1 < y && y < s.Y + 13) || // squished
                        (s.X - 11 < x && x < s.X + 11 && s.Y < y && y < s.Y + 10)); // horizontal bar
            }
        }
    }
    private static void AddSingleKillboxCollision(RectangleHitbox hb)
    {
        int xStart = Math.Max(0, (hb.bounds.L - DeathMap.xMin) / 8 - 2);
        int xEnd = Math.Min(DeathMap.widthInTiles, (hb.bounds.R - DeathMap.xMin) / 8 + 3);
        int yStart = Math.Max(0, (hb.bounds.U - DeathMap.yMin) / 8 - 2);
        int yEnd = Math.Min(DeathMap.heightInTiles, (hb.bounds.D - DeathMap.yMin) / 8 + 3);

        for (int x = xStart; x < xEnd; x++) {
            int xCoord = DeathMap.xMin + x * 8;
            for (int y = yStart; y < yEnd; y++) {
                int yCoord = DeathMap.yMin + y * 8 + 1;
                if (hb.TouchingAsFeather(new IntVec2(xCoord, yCoord)) ||
                    hb.TouchingAsFeather(new IntVec2(xCoord + 7, yCoord)) ||
                    hb.TouchingAsFeather(new IntVec2(xCoord, yCoord + 7)) ||
                    hb.TouchingAsFeather(new IntVec2(xCoord + 7, yCoord + 7)))
                    DeathMap.map[x][y] = true;
            }
        }
    }

    private static void GetSolidTiles()
    {
        Tiles = new SolidTileInfo() {
            x = Settings.Info.Level.Bounds.X,
            y = Settings.Info.Level.Bounds.Y,
            width = Settings.Info.Level.Bounds.W,
            height = Settings.Info.Level.Bounds.H,
        };
        Tiles.rightBound = Tiles.x + Tiles.width;
        Tiles.lowestYIndex = Tiles.height / 8 - 1;

        int widthInTiles = Tiles.width / 8;

        string tileMap = Regex.Replace(Settings.Info.SolidsData, @",\s", "");
        var rowMatches = Regex.Matches(tileMap, @"(?<= )[^ ]*");
        Tiles.map = rowMatches.Select(RowStrToBitArr).ToArray();

        int expectedRowCount = Tiles.lowestYIndex + 1;
        if (Tiles.map.Length < expectedRowCount) {
            var addedRows = Enumerable.Repeat(new BitArray(widthInTiles), expectedRowCount - Tiles.map.Length);
            Tiles.map = Tiles.map.Concat(addedRows).ToArray();
        }

        BitArray RowStrToBitArr(Match row)
        {
            var res = new BitArray(row.Value.Select(c => c != '0').ToArray());
            res.Length = widthInTiles;
            return res;
        }
    }

    private static void GetCustomHitboxes()
    {
        var kbs = new List<RectangleHitbox>();
        var colls = new List<RectangleHitbox>();

        var lineEmpty = new Regex(@"^\s*$");
        var parseLine = new Regex(@"^\s*(.?\d+),\s*(.?\d+),\s*(.?\d+),\s*(.?\d+)(?:\s*$|\s*([cC]))");

        for (int i = 0; i < Settings.ManualHitboxes.Length; i++) {
            if (lineEmpty.IsMatch(Settings.ManualHitboxes[i])) continue;

            var parse = parseLine.Match(Settings.ManualHitboxes[i]);

            if (!parse.Success)
                throw new ArgumentException($"Invalid hitbox definition on line {i + 1}");

            var rawBounds = new Bounds(int.Parse(parse.Groups[1].Value),
                                       int.Parse(parse.Groups[2].Value),
                                       int.Parse(parse.Groups[3].Value),
                                       int.Parse(parse.Groups[4].Value));

            if (parse.Groups[5].Success) {
                colls.Add(new RectangleHitbox(rawBounds.Expand(true)));
                continue;
            }

            kbs.Add(new RectangleHitbox(rawBounds.Expand(false)));
        }

        Colliders = Colliders.Concat(colls).ToArray();
        Killboxes = Killboxes.Concat(kbs).ToArray();
    }
    
    private static void GetCheckpoints()
    {
        var res = new List<Checkpoint>();
        var lineEmpty = new Regex(@"^\s*$");
        var parseLine = new Regex(@"^\s*(.?\d+),\s*(.?\d+),\s*(.?\d+),\s*(.?\d+)(?:\s*$|\s*([pP]))");

        for (int i = 0; i < Settings.Checkpoints.Length; i++) {
            if (lineEmpty.IsMatch(Settings.Checkpoints[i])) continue;

            var parse = parseLine.Match(Settings.Checkpoints[i]);

            if (!parse.Success)
                throw new ArgumentException($"Invalid checkpoint definition on line {i + 1}");

            var bounds = new Bounds(int.Parse(parse.Groups[1].Value),
                                    int.Parse(parse.Groups[2].Value),
                                    int.Parse(parse.Groups[3].Value),
                                    int.Parse(parse.Groups[4].Value));

            if (parse.Groups[5].Success) {
                res.Add(new Checkpoint(bounds.Expand()));
                continue;
            }

            res.Add(new Checkpoint(bounds.Expand(false)));
        }
        Checkpoints = res.ToArray();
    }

    private static void GetStaticTileEntities()
    {
        var res = new List<RectangleHitbox>();

        foreach (var s in Settings.Info.StaticSolids) {
            res.Add(new RectangleHitbox(new Bounds(s.X, s.Y, s.W, s.H).Expand(true)));
        }

        Colliders = Colliders.Concat(res).ToArray();
    }

    private static IntVec2[] GetIntVecs((float, float)[] info) {
        return info.Select(v => new IntVec2(new Vector2(v))).ToArray();
    }
}