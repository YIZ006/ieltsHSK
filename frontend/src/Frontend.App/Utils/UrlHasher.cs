namespace Frontend.App.Utils;

public static class UrlHasher
{
    private const uint PRIME = 2654435761;
    // Nghịch đảo modulo đúng của PRIME: (PRIME * INVERSE) % 2^32 == 1
    private const uint INVERSE = 244002641;
    private const uint XOR_MASK = 0x5A5A5A5A;

    public static string Encode(int id)
    {
        uint uId = (uint)id;
        uint mixed = (uId * PRIME) ^ XOR_MASK;
        return mixed.ToString("x8"); 
    }

    public static int Decode(string hash)
    {
        if (string.IsNullOrEmpty(hash)) return 0;
        
        try
        {
            uint mixed = Convert.ToUInt32(hash, 16);
            uint uId = (mixed ^ XOR_MASK) * INVERSE;
            return (int)uId;
        }
        catch
        {
            return 0;
        }
    }
}
