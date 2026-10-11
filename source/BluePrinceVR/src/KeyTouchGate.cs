namespace BluePrinceVR;
internal sealed class KeyTouchGate
{
    private bool armed;
    internal void Reset() => armed=false;
    internal bool Tick(float distance,bool eligible,float pressDistance=.018f,float releaseDistance=.035f)
    {
        if(distance>releaseDistance) armed=true;
        if(distance<pressDistance && armed && eligible) {armed=false;return true;}
        return false;
    }
}
