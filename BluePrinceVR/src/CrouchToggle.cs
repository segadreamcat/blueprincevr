namespace BluePrinceVR;
internal sealed class CrouchToggle
{
    private bool _pressed,_armed;
    internal bool Crouched {get;private set;}
    internal bool Update(bool pressed,bool blocked,bool reset)
    {
        if(reset){Crouched=false;_armed=false;_pressed=pressed;return false;}
        if(blocked)_armed=false;
        else if(pressed&&!_pressed)_armed=true;
        // Commit on release so holding both clicks to open comfort never crouches.
        if(!pressed&&_pressed&&_armed){Crouched=!Crouched;_armed=false;}
        _pressed=pressed;
        return Crouched;
    }
}
