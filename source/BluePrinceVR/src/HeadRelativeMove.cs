using System;
namespace BluePrinceVR;
internal static class HeadRelativeMove
{
    internal static void Rotate(float x,float y,float forwardX,float forwardZ,float snapYaw,ref float lastHeading,out float moveX,out float moveY)
    {
        // Near vertical gaze has no usable horizontal heading; keep the last
        // valid yaw instead of reversing or jittering. Never scale by pitch.
        if(forwardX*forwardX+forwardZ*forwardZ>.0001f)
            lastHeading=(float)Math.Atan2(forwardX,forwardZ);
        double angle=lastHeading+snapYaw*Math.PI/180;
        float c=(float)Math.Cos(angle),s=(float)Math.Sin(angle);
        moveX=x*c+y*s;moveY=y*c-x*s;
    }
}
