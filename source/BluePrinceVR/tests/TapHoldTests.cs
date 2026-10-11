#if TAP_HOLD_TESTS
using System;
using BluePrinceVR;
internal static class TapHoldTests
{
    public static void Main()
    {
        var g=new TapHoldButton();
        void Check(int got,int expected){if(got!=expected)throw new Exception($"Expected {expected}, got {got}");}
        Check(g.Update(true,false,0),0);Check(g.Update(false,false,.2f),1);
        Check(g.Update(false,false,.3f),0);
        Check(g.Update(true,false,1),0);Check(g.Update(true,false,1.7f),2);
        Check(g.Update(true,false,2),0);Check(g.Update(false,false,2.1f),0);
        Check(g.Update(true,false,3),0);Check(g.Update(true,true,3.1f),0);
        Check(g.Update(true,false,4),0);Check(g.Update(false,false,4.1f),0);
        Check(g.Update(true,false,5),0);Check(g.Update(false,false,6),2);
        Console.WriteLine("PASS tap once, hold once, no tap after hold, blocked press/release and delayed frame");
    }
}
#endif
