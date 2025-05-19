using DataStructure;
using System.Numerics;
using System.Diagnostics;


static bool RayCheck(Vector3 origin, Vector3 dir_inv, Bounds bounds)
{
    var min = bounds.Min;
    var max = bounds.Max;

    float t1 = (min.X - origin.X) * dir_inv.X;
    float t2 = (max.X - origin.X) * dir_inv.X;
    float t3 = (min.Y - origin.Y) * dir_inv.Y;
    float t4 = (max.Y - origin.Y) * dir_inv.Y;
    float t5 = (min.Z - origin.Z) * dir_inv.Z;
    float t6 = (max.Z - origin.Z) * dir_inv.Z;

    float tmin = float.Max(float.Max(float.Min(t1, t2), float.Min(t3, t4)), float.Min(t5, t6));
    float tmax = float.Min(float.Min(float.Max(t1, t2), float.Max(t3, t4)), float.Max(t5, t6));

    return !(tmax < 0 || tmin > tmax);
}


const int SAMPLES = 8192 * 8 * 8;
const int RANDOM_SEED = 0;
const float SPACE = 1f;
const float MIN_SIZE = 0.001f;
const float MAX_SIZE = 0.01f;

Console.WriteLine($"{nameof(SAMPLES)}: {SAMPLES}");
Console.WriteLine($"{nameof(RANDOM_SEED)}: {RANDOM_SEED}");
Console.WriteLine($"{nameof(SPACE)}: {SPACE}");
Console.WriteLine($"{nameof(MIN_SIZE)}: {MIN_SIZE}");
Console.WriteLine($"{nameof(MAX_SIZE)}: {MAX_SIZE}");
Console.WriteLine();

var bvh = new BVH<int>(SAMPLES);
var boundsArr = new Bounds[SAMPLES];
var random = new Random(RANDOM_SEED);


for (int i = 0; i < SAMPLES; i++)
{
    var center = new Vector3(random.NextSingle() * SPACE - SPACE * 0.5f,
                             random.NextSingle() * SPACE - SPACE * 0.5f,
                            random.NextSingle() * SPACE - SPACE * 0.5f);
    var size = new Vector3(random.NextSingle() * (MAX_SIZE - MIN_SIZE) + MIN_SIZE,
                           random.NextSingle() * (MAX_SIZE - MIN_SIZE) + MIN_SIZE,
                           random.NextSingle() * (MAX_SIZE - MIN_SIZE) + MIN_SIZE);
    
    boundsArr[i] = new Bounds(center, size);
}

var sw = Stopwatch.StartNew();

for (int i = 0; i < SAMPLES; i++)
    bvh.Insert(i, boundsArr[i]);

sw.Stop();

var buildTime = sw.Elapsed;
Console.WriteLine($"{nameof(buildTime)}: {buildTime}");

sw.Restart();

for (int i = 0; i < SAMPLES; i++)
    bvh.Remove(i);

sw.Stop();

var removeTime = sw.Elapsed;
Console.WriteLine($"{nameof(removeTime)}: {removeTime}");

for (int i = 0; i < SAMPLES; i++)
    bvh.Insert(i, boundsArr[i]);

sw.Restart();

bvh.BottomUp();

sw.Stop();
var bottomUpTime = sw.Elapsed;
Console.WriteLine($"{nameof(bottomUpTime)}: {bottomUpTime}");

int cnt = 0;
var emptyFunc = (int i) => {cnt += 1;};

sw.Restart();
for (int i = 0; i < 1000; i++)
    bvh.Traversal(bounds => RayCheck(Vector3.Zero, Vector3.One, bounds), emptyFunc);
sw.Stop();
Console.WriteLine($"hit: {cnt / 1000}");
cnt = 0;
Console.WriteLine($"average query time: {sw.Elapsed / 1000f}");

#region BRUTEFORCE QUERYTIME

sw.Restart();
for (int i = 0; i < 1000; ++i)
{
    for (int j = 0; j < SAMPLES; ++j)
    {
        if (RayCheck(Vector3.Zero, Vector3.One, boundsArr[j]))
        {
            emptyFunc(j);
        }
    }
}
sw.Stop();
var averageBruteForceQueryTime = sw.Elapsed / 1000f;

#endregion

Console.WriteLine($"hit: {cnt / 1000}");
Console.WriteLine($"{nameof(averageBruteForceQueryTime)}: {averageBruteForceQueryTime}");