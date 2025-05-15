using System.Numerics;


namespace DataStructure;

public static class Morton3D
{
    // Expands a 10-bit integer into 30 bits
    // by inserting 2 zeros after each bit.
    public static uint ExpandBits(uint v)
    {
        v = (v * 0x00010001u) & 0xFF0000FFu;
        v = (v * 0x00000101u) & 0x0F00F00Fu;
        v = (v * 0x00000011u) & 0xC30C30C3u;
        v = (v * 0x00000005u) & 0x49249249u;

        return v;
    }

    // Calculates a 30-bit Morton code for the
    // given 3D point located within the unit cube [0,1].
    public static uint Encode(Vector3 position)
    {
        // position이 -100, 100의 범위만을 갖는다고 가정
        position.X = map(position.X, -100, 100, 0, 1);
        position.Y = map(position.Y, -100, 100, 0, 1);
        position.Z = map(position.Z, -100, 100, 0, 1);

        position.X = Math.Min(Math.Max(position.X * 1024, 0), 1023);
        position.Y = Math.Min(Math.Max(position.Y * 1024, 0), 1023);
        position.Z = Math.Min(Math.Max(position.Z * 1024, 0), 1023);

        uint xx = ExpandBits((uint)position.X);
        uint yy = ExpandBits((uint)position.Y);
        uint zz = ExpandBits((uint)position.Z);

        return xx * 4 + yy * 2 + zz;
    }

    private static float map(float value, float fromLow, float fromHigh, float toLow, float toHigh) 
    {
        return (value - fromLow) * (toHigh - toLow) / (fromHigh - fromLow) + toLow;
    }
}