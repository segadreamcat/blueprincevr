using System;
namespace BluePrinceVR;

internal static class FingerControls
{
    internal static bool PointReady(bool eligible,bool pickup,bool blocked,float grip,float trigger)
        => eligible && !pickup && !blocked && grip < .55f && trigger < .55f;
    internal static float IdleTogether(float grip, float trigger, bool pickup)
        => pickup ? 0f : 1f - Math.Clamp(Math.Max(grip, trigger) / .55f, 0f, 1f);
    internal static float PoseAngle(int finger, int joint, float grip, float trigger, bool pickup, bool open)
    {
        float angle = Angle(finger, joint, grip, trigger);
        // Hover feedback survives light resting pressure; a deliberate squeeze wins.
        if (pickup && grip < .55f && trigger < .55f)
            return Math.Max(angle, finger == 4 ? 3f : joint == 1 ? 10f : 5f);
        if (!open && grip < .05f && trigger < .05f)
            return finger == 4 ? 2f : joint == 1 ? 3f : 1f;
        return angle;
    }
    // 0 index, 1 middle, 2 ring, 3 pinky, 4 thumb.
    internal static float Angle(int finger, int joint, float grip, float trigger)
    {
        float amount = Math.Clamp(finger == 0 ? trigger : grip, 0f, 1f);
        float limit = finger == 4 ? (joint == 0 ? 15f : 10f) :
            (joint == 0 ? 70f : joint == 1 ? 85f : 45f);
        return amount * limit;
    }
}
