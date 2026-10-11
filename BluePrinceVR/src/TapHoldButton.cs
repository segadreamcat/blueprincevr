namespace BluePrinceVR;
internal sealed class TapHoldButton
{
    private bool _down, _armed, _held;
    private float _started;
    public int Update(bool down, bool blocked, float now)
    {
        if(blocked){_down=down;_armed=false;_held=false;return 0;}
        if(down&&!_down){_armed=true;_held=false;_started=now;}
        int action=0;
        if(_armed&&!_held && now-_started>=.65f){_held=true;action=2;}
        if(!down){if(_down&&_armed&&!_held)action=1;_armed=false;}
        _down=down;
        return action;
    }
}
