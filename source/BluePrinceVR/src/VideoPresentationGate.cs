using System;
namespace BluePrinceVR;
internal sealed class VideoPresentationGate
{
    internal static bool ReachedEnd(bool looping,long frame,ulong count,double time,double length)
    {
        return !looping && ((count>1 && frame>=0 && (ulong)frame>=count-1) || (length>0 && time>=length));
    }
    private int _id;
    private long _frame=-1;
    private double _time=-1;
    private float _advancedAt;
    internal bool Show(int id,long frame,double time,float now,bool playing,bool presenting)
    {
        if(!playing||!presenting)return false;
        if(id!=_id||frame!=_frame||Math.Abs(time-_time)>.001) {
            _id=id;_frame=frame;_time=time;_advancedAt=now;
        }
        return now-_advancedAt<=1f;
    }
}
