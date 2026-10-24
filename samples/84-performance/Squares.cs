namespace SupportDesk;
public static class Squares
{
    public static bool Valid(int count)=>count is >=1 and <=1_000_000;
    public static long Loop(int count)
    {
        if(!Valid(count)) throw new ArgumentOutOfRangeException(nameof(count));
        long sum=0; for(var i=1;i<=count;i++) sum+=(long)i*i; return sum;
    }
    public static long Formula(int count)
    {
        if(!Valid(count)) throw new ArgumentOutOfRangeException(nameof(count));
        return (long)count*(count+1)*(2L*count+1)/6;
    }
}
