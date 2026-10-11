using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using BluePrinceVR;
using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(ClaudeFixes.Mod), "BluePrinceVR Claude Fixes", "1.1.0", "Claude")]
[assembly: MelonGame(null, null)]

namespace ClaudeFixes;

// A companion mod that patches BluePrinceVR Codex from the outside, so that a
// rebuild of that mod cannot wipe these fixes. Every patch is resolved by name at
// startup and skipped with a log line if the target has moved, so a Codex refactor
// costs a feature rather than crashing the game.
public class Mod : MelonMod
{
    internal static Mod I;

    private static MelonPreferences_Category _cfg;
    private static MelonPreferences_Entry<float> _pNotebookBrightness;
    private static MelonPreferences_Entry<bool> _pRecenterPanel;
    private static MelonPreferences_Entry<bool> _pForkliftHold;
    private static MelonPreferences_Entry<bool> _pDigStanding;
    private static MelonPreferences_Entry<float> _pDigHorizontal;
    private static MelonPreferences_Entry<float> _pDigRise;
    private static MelonPreferences_Entry<bool> _pVerbose;
    private static MelonPreferences_Entry<bool> _pWatchCursor;
    private static MelonPreferences_Entry<bool> _pWatchButtons;
    private static MelonPreferences_Entry<bool> _pFixDraftMenu;
    private static MelonPreferences_Entry<bool> _pLogDraftLabels;
    private static MelonPreferences_Entry<bool> _pCrateReach;
    private static MelonPreferences_Entry<float> _pCrateGroundAllowance;
    private static MelonPreferences_Entry<bool> _pPointGrab;
    private static MelonPreferences_Entry<float> _pPointGrabRadius;
    private static MelonPreferences_Entry<float> _pPointGrabReach;
    private static MelonPreferences_Entry<bool> _pWidenPickups;
    private static MelonPreferences_Entry<string> _pExtraPickupNames;
    private static MelonPreferences_Entry<bool> _pTitleVR;
    private static MelonPreferences_Entry<bool> _pTitleFx;
    private static MelonPreferences_Entry<string> _pTitleSuffix;
    private static MelonPreferences_Entry<bool> _pPressQueue;
    private static MelonPreferences_Entry<float> _pPressQueueWindow;
    private static MelonPreferences_Entry<bool> _pComputerPullback;
    private static MelonPreferences_Entry<float> _pComputerScale;
    private static MelonPreferences_Entry<float> _pComputerPush;
    private static MelonPreferences_Entry<string> _pTitleTarget;
    private static MelonPreferences_Entry<float> _pTitleScale;
    private static MelonPreferences_Entry<float> _pTitleGap;
    private static MelonPreferences_Entry<float> _pTitleNudgeY;
    private static MelonPreferences_Entry<string> _pTitleColour;
    private static MelonPreferences_Entry<string> _pDraftMarkers;
    private static MelonPreferences_Entry<string> _pTitleBackdrop;
    private static MelonPreferences_Entry<string> _pTitleImage;
    private static MelonPreferences_Entry<float> _pTitleOffX;
    private static MelonPreferences_Entry<float> _pTitleOffY;
    private static MelonPreferences_Entry<float> _pTitleHeight;
    private static MelonPreferences_Entry<float> _pTitleDepth;
    private static MelonPreferences_Entry<bool> _pFixRewired;
    private static MelonPreferences_Entry<bool> _pSteerStale;
    private static MelonPreferences_Entry<float> _pSteerSnapDeg;
    private static MelonPreferences_Entry<bool> _pCrateReachFix;
    private static MelonPreferences_Entry<float> _pCrateReachMetres;
    private static MelonPreferences_Entry<string> _pCrateNames;
    private static MelonPreferences_Entry<bool> _pTitlePoof;
    private static MelonPreferences_Entry<float> _pTitlePoofDelay;
    private static MelonPreferences_Entry<bool> _pTitleSmoke;
    private static MelonPreferences_Entry<float> _pTitleSmokeRate;
    private static MelonPreferences_Entry<bool> _pFloorPlanInDraft;
    private static MelonPreferences_Entry<bool> _pDraftByCards;
    private static MelonPreferences_Entry<float> _pDraftPlanRange;
    private static MelonPreferences_Entry<bool> _pProbeDraft;
    private static MelonPreferences_Entry<bool> _pPumpPointPose;
    private static MelonPreferences_Entry<bool> _pKillBlur;
    private static MelonPreferences_Entry<bool> _pPanelAimLog;
    private static MelonPreferences_Entry<bool> _pPanelFollow;
    private static MelonPreferences_Entry<float> _pPanelDeadzone;
    private static MelonPreferences_Entry<float> _pPanelSnapAngle;
    private static MelonPreferences_Entry<float> _pPanelFollowSpeed;
    private static MelonPreferences_Entry<string> _pRoomPanelCams;
    private static MelonPreferences_Entry<bool> _pHandLight;
    private static MelonPreferences_Entry<float> _pHandLightFloor;
    private static MelonPreferences_Entry<float> _pHandLightCeiling;
    private static MelonPreferences_Entry<float> _pHandLightCurve;
    private static MelonPreferences_Entry<float> _pHandLightSmoothing;
    private static MelonPreferences_Entry<string> _pHandLightTargets;
    private static MelonPreferences_Entry<bool> _pIndexOnButton;
    private static MelonPreferences_Entry<float> _pThumbWrap;
    private static MelonPreferences_Entry<float> _pThumbCurl;
    private static MelonPreferences_Entry<float> _pIndexAimLimit;
    private static MelonPreferences_Entry<float> _pIndexTipScale;
    private static MelonPreferences_Entry<string> _pHoldDragNames;
    private static MelonPreferences_Entry<bool> _pForkliftLift;
    private static MelonPreferences_Entry<float> _pDragLiftSpeed;
    private static MelonPreferences_Entry<string> _pDragLiftDrive;
    private static MelonPreferences_Entry<bool> _pGripsSwapped;
    private static MelonPreferences_Entry<bool> _pMenuGameFont;
    private static MelonPreferences_Entry<bool> _pMenuIconSelector;
    private static MelonPreferences_Entry<bool> _pMainMenuVRSettings;
    private static MelonPreferences_Entry<float> _pMainMenuRowGap;
    private static MelonPreferences_Entry<float> _pRaiseResScale;

    private static AccessTools.FieldRef<VRPlugin, Color[]> _journalBase;
    private static AccessTools.FieldRef<VRPlugin, bool> _mouseHeld;
    private static AccessTools.FieldRef<VRPlugin, bool> _panelAimValid;
    private static AccessTools.FieldRef<VRPlugin, Quaternion> _headRot;
    private static AccessTools.FieldRef<VRPlugin, Camera> _roomPanelCam;
    private static AccessTools.FieldRef<VRPlugin, Camera> _lastExploreCam;
    private static AccessTools.FieldRef<VRPlugin, Il2CppTMPro.TextMeshPro> _comfortText;
    private static AccessTools.FieldRef<VRPlugin, int> _comfortRow;
    private static AccessTools.FieldRef<VRPlugin, bool> _comfortCalibrating;
    private static AccessTools.FieldRef<VRPlugin, bool> _steerLocked;
    private static AccessTools.FieldRef<VRPlugin, float> _steerHeld;
    private static AccessTools.FieldRef<VRPlugin, int> _steerWorse;
    private static AccessTools.FieldRef<VRPlugin, float> _steerLastErr;
    private static MethodInfo _repaintJournal;
    private static MethodInfo _isDigTarget;
    private static bool _isDigTargetStatic;

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint flags, int dx, int dy, uint data, IntPtr extra);

    public override void OnInitializeMelon()
    {
        I = this;
        _cfg = MelonPreferences.CreateCategory("ClaudeFixes");
        _pNotebookBrightness = _cfg.CreateEntry("NotebookBrightness", 0.55f, null,
            "Brightness of the in-VR notebook and photo journal pages. They use an unlit shader, so the paper ignores room light and reads like a lightbox in a dark room. 1.0 is the stock, blinding level.");
        _pRecenterPanel = _cfg.CreateEntry("RecenterPanelOnMenuOpen", true, null,
            "Re-aim the flat menu panel to where you are looking at the moment a menu becomes interactive. Fixes the casino/roulette menu opening off to one side because the panel was already up and anchored elsewhere. It stays world-fixed afterwards.");
        _pForkliftHold = _cfg.CreateEntry("ForkliftHoldToDrag", true, null,
            "Hold the right trigger to keep hold of the basement forklift and the other pullers/lifters, move it, release to let go. Taking hold changes game state, which trips a guard that releases the mouse button a frame later; this keeps the hold alive until you actually let go.");
        _pDigStanding = _cfg.CreateEntry("DigStandingReach", true, null,
            "Let a dirt pile be dug from standing height instead of only from a hand aimed down a narrow forward cone.");
        _pDigHorizontal = _cfg.CreateEntry("DigReachHorizontal", 1.3f, null,
            "How far out, flat along the ground, a pile may sit from your hand and still be diggable while standing.");
        _pDigRise = _cfg.CreateEntry("DigReachRise", 0.6f, null,
            "How far above your hand a pile's surface may sit and still count as reachable downward.");
        _pWatchCursor = _cfg.CreateEntry("WatchCursorMode", true, null,
            "Log the mod's cursor and panel state whenever it changes. This is how the lock pick problem gets diagnosed: it shows what state using the lock pick leaves the game in. No behaviour attached; set false once that is solved.");
        _pWatchButtons = _cfg.CreateEntry("WatchButtons", true, null,
            "Log cancel (B) and menu button edges, to show whether one B press is satisfying both actions. No behaviour attached.");
        _pFixDraftMenu = _cfg.CreateEntry("FixDraftMenuDetection", true, null,
            "Detect the room draft menu by its object hierarchy instead of by scanning for the words SELECT A ROOM. OFF by default: the THE DRAFT hierarchy is live during normal play too, so this flagged the draft constantly and that disabled the trigger, including the floor plan button. Needs the real heading first, which LogDraftLabels collects.");
        _pLogDraftLabels = _cfg.CreateEntry("LogDraftLabels", true, null,
            "Log the active text under the THE DRAFT hierarchy, so the real room-choice heading can be identified and matched exactly. Read-only, no behaviour attached.");
        _pCrateReach = _cfg.CreateEntry("ReachIntoCrates", true, null,
            "Let loot inside a crate, trunk or chest be grabbed without pushing your hand physically inside it. The container the item sits in stops counting as a wall between your palm and the item. Turn off if you can grab through a closed lid.");
        _pCrateGroundAllowance = _cfg.CreateEntry("CrateGroundAllowance", 0.35f, null,
            "How much terrain overlap to forgive under loot, in metres. Stock is 0.12 for coins and 0.35 for keys and gems; this applies the larger value to everything.");
        _pPointGrab = _cfg.CreateEntry("PointToGrabItems", true, null,
            "Let items be grabbed by pointing at them, the way doors already work, instead of only by putting your palm within 0.45m and above them. Only runs when the stock search finds nothing, so it can only add reach.");
        _pPointGrabRadius = _cfg.CreateEntry("PointGrabRadius", 0.15f, null,
            "Radius of the aim hitbox for items, in metres. Bigger means you need less precision. Lower it if the wrong item gets picked.");
        _pPointGrabReach = _cfg.CreateEntry("PointGrabReach", 2.6f, null,
            "How far you can grab an item by pointing, in metres. 2.6 matches the mod's own pickup reach; raise for longer range.");
        _pWidenPickups = _cfg.CreateEntry("WidenPickupNames", true, null,
            "Treat real inventory items (lock pick, shovel, telescope and the rest) as pickups. The mod only recognised coins, fruit, gems, keys and dice, so everything else missed out on every pickup assist.");
        _pExtraPickupNames = _cfg.CreateEntry("ExtraPickupNames", "", null,
            "Extra item names to treat as pickups, separated by |. Add anything still awkward to grab.");
        _pTitleFx = _cfg.CreateEntry("TitleScreenEffects", false, null,
            "The puff of smoke the VR wordmark used to burst out of, and the smoke that drifted afterwards. Off: the wordmark simply appears.");
        _pTitleVR = _cfg.CreateEntry("TitleScreenVR", true, null,
            "Append VR to the Blue Prince title on the menu screen, in the title's own font.");
        _pTitleSuffix = _cfg.CreateEntry("TitleScreenSuffix", " VR", null,
            "What to append to the title text.");
        _pPressQueue = _cfg.CreateEntry("QueueDroppedPresses", true, null,
            "If you press the trigger at something that is not accepting clicks that instant (a parlor box, a trunk, a lever mid-animation), hold the press and deliver it the moment the prop is ready, instead of discarding it. This is why some things take several tries. Cancels if you aim away, and never applies to pickups or to the forklift.");
        _pPressQueueWindow = _cfg.CreateEntry("PressQueueWindow", 1.0f, null,
            "How long a held press stays valid, in seconds.");
        _pComputerPullback = _cfg.CreateEntry("ComputerPullBack", true, null,
            "Sit back from a computer screen instead of being pushed right up against it, so the text fits. Applies to the Office computer and to the view after a room upgrade disk.");
        _pComputerScale = _cfg.CreateEntry("ComputerViewScale", 0.78f, null,
            "How much to shrink the computer screen panel. Lower means more of the screen fits; 1.0 is stock.");
        _pComputerPush = _cfg.CreateEntry("ComputerViewPush", 0.18f, null,
            "How much further away to place the computer screen panel, in metres.");
        _pTitleTarget = _cfg.CreateEntry("TitleLogoName", "", null,
            "Name (or part of a name) of the title logo object to sit the VR beside. Leave empty to auto-detect; the log reports what it picked.");
        _pTitleScale = _cfg.CreateEntry("TitleVRScale", 0.62f, null,
            "VR letter height as a fraction of the logo height.");
        _pTitleGap = _cfg.CreateEntry("TitleVRGap", 0.04f, null,
            "Gap between the logo and the VR, as a fraction of the logo width.");
        _pTitleNudgeY = _cfg.CreateEntry("TitleVRNudgeY", 0f, null,
            "Vertical nudge for the VR, as a fraction of the logo height. Positive moves it up.");
        _pTitleColour = _cfg.CreateEntry("TitleVRColour", "#C8991F", null,
            "Colour of the VR, as a hex string. The gold from the mockup is #C8991F.");
        _pCrateReachFix = _cfg.CreateEntry("CrateToolReach", true, null,
            "Let crates be opened from where you can see them instead of only from arm's length. Codex's box assist only knows TRUNK(Clone) and ParlorBox, so the wooden crates fall through to the stock 1.5m and get no aim help at all. This treats them like a trunk.");
        _pCrateReachMetres = _cfg.CreateEntry("CrateReachMetres", 3.2f, null,
            "How far a crate can be opened from, in metres. 3.2 is the reach the shovel already uses.");
        _pCrateNames = _cfg.CreateEntry("CrateRootNames", "CRATE", null,
            "Object names that count as a crate, separated by |, matched at the start of the name and ignoring case. The scene calls them Crate A and Crate B (n). Add BARREL or CHEST here if those feel short too.");
        _pDraftByCards = _cfg.CreateEntry("DraftByPlanCards", true, null,
            "Treat DRAFT PLAN cards that are actually in front of you as proof the room choice is open. Being active is not enough: the probe showed the game parks them 10,000 metres away between drafts, which is why matching on active alone flagged every later menu. DraftPlanMaxDistance is the line between the two.");
        _pDraftPlanRange = _cfg.CreateEntry("DraftPlanMaxDistance", 50f, null,
            "How near a DRAFT PLAN card must be, in metres, to mean the room choice is open. Parked cards sit about 10,000m out and a real one is a few metres away, so anything in between works.");
        _pProbeDraft = _cfg.CreateEntry("ProbeDraftObjects", true, null,
            "Log which draft objects are active and how far away they are each time a menu opens. Read-only. This is what finds the signal that separates the room choice from every other menu.");
        _pHoldDragNames = _cfg.CreateEntry("HoldDragNames", "Clicker|LOAD", null,
            "Object names that mean hold-to-drag, separated by |, matched at the start of the name on the collider or any parent. The forklift's handle is called Clicker. This is a direct match alongside the FSM signature test, because that test caches its answer the first time you aim at something and gets it wrong if the FSM has not woken up yet.");
        _pMainMenuVRSettings = _cfg.CreateEntry("MainMenuVRSettingsEntry", true, null,
            "Print 'PRESS X FOR VR OPTIONS' under QUIT GAME on the title screen. It is a caption rather than a menu row: the game's own layout script decides which rows the selector can reach, so a row added from out here can be seen but never chosen, and scrolling past a dead entry on the way to QUIT GAME is worse than no entry at all.");
        _pMainMenuCaption = _cfg.CreateEntry("MainMenuCaption", "VR OPTIONS   [X]", null,
            "What that line under QUIT GAME says. The rows it sits with are one or two words, so it is written like one of them; set it to 'PRESS X FOR VR OPTIONS' if you would rather it spelled out, and drop MainMenuCaptionSize to about 0.7 to keep a long line from running wider than the menu.");
        _pMainMenuCaptionSize = _cfg.CreateEntry("MainMenuCaptionSize", 1.0f, null,
            "Size of that line as a fraction of a real menu row. 1.0 matches the menu exactly.");
        _pMainMenuRowGap = _cfg.CreateEntry("MainMenuRowGap", 0.07f, null,
            "Spacing between title menu rows in metres, used to push QUIT GAME down and slot the new row in. The stock menu uses 0.07.");
        _pUseItemLatch = _cfg.CreateEntry("UseItemPressLatch", true, null,
            "Hold a use-item press (trigger and grip together) until the game actually reads it, up to 8 frames, then clear it. Codex does exactly this for interact with InteractDownFrames and TakeInteractPress; use-item was left as a single-frame flag, so whether the press registers depends on whether the game polls Rewired before or after the mod's update. This is why using the lock pick on a door sometimes does nothing at all.");
        _pMagnifierHold = _cfg.CreateEntry("MagnifierNeedsHold", true, null,
            "Make the document stick pointer want trigger and grip HELD, not tapped. The tap is the game's own use-item press, and Codex's panel code toggles the pointer on the same edge, so every single item use also switched the pointer on, which disabled hand pointing and zeroed A and the menu navigation. The item press still reaches the game untouched.");
        _pMagnifierHoldSeconds = _cfg.CreateEntry("MagnifierHoldSeconds", 0.8f, null,
            "How long trigger and grip must be held to toggle the document pointer.");
        _pMagnifierIdleOff = _cfg.CreateEntry("MagnifierIdleOffSeconds", 12f, null,
            "If the document pointer is on and nothing has been clicked with it for this long, switch it off and hand the panel back to the game. Insurance against being stuck in cursor mode with a menu you cannot drive. 0 disables it. B always switches it off too.");
        _pVRPage = _cfg.CreateEntry("TitleScreenVRSettingsPage", true, null,
            "On the title screen, open the VR settings as a column of the menu's own rows where the main menu was, the way the game shows its own SETTINGS, instead of the panel that floats in front of you. X, or holding both sticks, opens it; the stick and A work it. Set false to go back to the floating panel on the title screen too.");
        _pMenuGameFont = _cfg.CreateEntry("MenuGameFont", true, null,
            "Draw the VR settings menu in the game's own serif face instead of the plain default TMP font it is created with.");
        _pMenuControls = _cfg.CreateEntry("ComfortMenuControlsRow", true, null,
            "In the VR settings menu you get by holding both sticks, turn the reprojection row - which only ever printed a paragraph about settings the mod does not control - into a list of the VR controls. Choose it to read them, move the stick to come back.");
        _pMenuIconSelector = _cfg.CreateEntry("MenuIconSelector", true, null,
            "Mark the selected row with the Blue Prince crest instead of a '>' character. The artwork is UserData/BPVR_select.png, swap the file to change it.");
        _pForkliftLift = _cfg.CreateEntry("ForkliftGripLift", true, null,
            "While you are holding the forklift, the grips raise and lower the forks. This drives the game's own MouseDrag the way Codex's 2.10.57c17 did, which is the version that actually worked.");
        _pDragLiftSpeed = _cfg.CreateEntry("DragLiftSpeed", 260f, null,
            "How fast the grips move a held forklift. This is the same setting name c17 used, so the value already sitting in your config comes back into use.");
        _pDragLiftDrive = _cfg.CreateEntry("DragLiftDrive", "screendelta", null,
            "Which native input the grips feed. 'screendelta' mimics mouse movement, which is what worked. 'distance' pushes the drag distance directly; try it if the grips do not move the forks.");
        _pGripsSwapped = _cfg.CreateEntry("ForkliftGripsSwapped", false, null,
            "false is left grip up, right grip down, as you asked for. true is right up, left down, which is the way c17 actually shipped. Flip it if the original felt correct in the headset.");
        _pRecorderOffHand = _cfg.CreateEntry("RecorderInOffHand", true, null,
            "Hold the memo recorder in the off hand instead of the dominant one. X opens the quick memo and X is on the off-hand controller, so that is the hand the recorder belongs in, and it leaves the dominant hand free to point at the world. This flips the wrist it sits on, the side the record key is cut into, the hand whose fingers wrap it, and which trigger records.");
        _pRecorderPalm = _cfg.CreateEntry("RecorderPalmHold", true, null,
            "Hold the recorder the way a hand actually holds one: the back of the casing against the palm and the base of the fingers, long axis up the hand and tilted so the microphone end leads, instead of floating 12cm out past the fingertips. This also drops the (0.85, 1.5, 1) scaling that stretched the casing into a 17cm slab; the authored mesh is already about the size of a real microcassette recorder.");
        _pRecorderIndexShift = _cfg.CreateEntry("RecorderTowardIndexFrac", 0.07f, null,
            "How far the recorder slides along its own length toward the index finger, as a fraction of the palm length, so the microphone end projects past the hand instead of sticking out equally at both ends. Too much and the gripped part of it leaves the fingers.");
        _pRecorderGap = _cfg.CreateEntry("RecorderPalmGapFrac", 0.19f, null,
            "How far onto the palm side of the finger bones the recorder sits, as a fraction of the palm length.");
        _pKeyShift2 = _cfg.CreateEntry("RecorderKeyOffset", -0.022f, null,
            "Nudge the record key along the length of the recorder away from where the index pad sits, in the casing's own units. Negative moves it toward the heel of the hand, and about 0.011 is half an inch on screen. Replaces RecorderKeyShift, whose stored value would have pinned the old amount.");
        _pKeyShift = _cfg.CreateEntry("RecorderKeyShift", -0.011f, null,
            "Nudge the record key along the length of the recorder away from where the index pad sits, in the casing's own units. Negative moves it toward the heel of the hand. About 0.011 is half an inch on screen.");
        _pFreeHandCurl = _cfg.CreateEntry("RecorderFreeHandCurl", 0.28f, null,
            "How closed the hand NOT holding the recorder is held while the memo is out, 0 open and 1 a fist. It is pinned to this shape every frame so nothing else can animate it.");
        _pWidenCulling = _cfg.CreateEntry("WidenCullingForVR", true, null,
            "Cull against a frustum wide enough for the headset instead of the flat camera's. Codex swaps the projection per eye but never the culling matrix, so the game decides what to draw using the monitor's narrower view and things at the edges of your vision disappear.");
        _pTieCamYaw = _cfg.CreateEntry("TieGameCameraYawToHead", false, null,
            "Point the flat game camera where your head points, so the game's own room culling stops hiding rooms you are looking at. Blue Prince culls rooms in script against that camera, which the mod otherwise leaves facing wherever the stick last turned you, which is why a room seen through a doorway can vanish when you turn only your head. Applied after the mod has read the camera and removed before it reads it again, so turning and aiming are unchanged.");
        _pKeyClickVolume = _cfg.CreateEntry("KeyboardClickVolume", 0.8f, null,
            "Volume of the key click played at your fingertip when you press a key on the terminal. Codex plays one of its own at the computer at a fifth of this, which is easy to miss over a streamed headset. 0 switches this one off and leaves Codex's alone.");
        _pFullKeyboard = _cfg.CreateEntry("FullFingertipKeyboard", true, null,
            "Type on the terminal's own keys with your fingertips, the whole keyboard rather than Codex's six menu keys. Hold both grips for half a second at a computer to calibrate, then touch Q, P and SPACE with your right index tip, tapping A for each. Every room uses the same keyboard, so once is enough.");
        _pKeyPressFrac = _cfg.CreateEntry("KeyPressFraction", 0.55f, null,
            "How close the fingertip must come to a key centre before it fires, as a fraction of the key spacing. Lower it if keys trigger too eagerly.");
        _pKeyArmFrac = _cfg.CreateEntry("KeyArmFraction", 1.15f, null,
            "How far the fingertip must move away before that key can fire again, as a fraction of the key spacing. This is what stops a resting finger repeating.");
        _pMagGripGate = _cfg.CreateEntry("MagnifierGripGate", true, null,
            "Hold grip while a document is open to bring up the magnifier, and move it with the left stick. This replaces Codex's use-item toggle, which stopped responding once the use-item latch in this mod began rewriting that signal. Hold to use: releasing grip hands the panel straight back to the game, so the pointer cannot get stuck on.");
        // Renamed from MagnifierFollowsHand because the meaning changed and a stored value
        // would otherwise keep the old default: hand motion is now off unless asked for.
        _pMagnifierByHand = _cfg.CreateEntry("MagnifierHandMotion", false, null,
            "Move the magnifier by moving your hand rather than the left stick, while grip is held. Off by default; the stick is steadier for reading small print.");
        _pMagnifierGain = _cfg.CreateEntry("MagnifierHandGain", 3.0f, null,
            "How far the magnifier travels for a given hand movement, in screen heights per metre. At 3, about a 30cm sweep crosses the page. Lower it for finer control over small print, raise it to cover the page with less arm movement.");
        _pCatchItemComputer = _cfg.CreateEntry("CatchItemOpenedComputer", true, null,
            "Treat a computer opened by using an item on it, such as the upgrade disc, the same as one opened by hand. The game zooms its own inspection camera in that case and the VR mod never hears about it, so you get the flat zoom instead of standing back from the screen with the keyboard working.");
        _pKeepRoomsDrawn = _cfg.CreateEntry("KeepNearRoomsDrawn", true, null,
            "Stop the game switching off the renderers of a room you are standing next to. Blue Prince has a Culler component on each room that decides in its own LateUpdate, and in VR it gets that decision wrong for a room seen through a doorway while your body faces elsewhere, which is the void. Rooms beyond the radius below still cull normally.");
        _pKeepRoomsRadius = _cfg.CreateEntry("KeepNearRoomsRadius", 35f, null,
            "How far from you, in metres, a room is protected from being switched off. Raise it if a void appears further away, lower it if the frame rate suffers.");
        _pCullFollowsHead = _cfg.CreateEntry("CullingFollowsHead", true, null,
            "Cull against where your head is pointing rather than where the flat camera is pointing. Blue Prince leaves Unity to cull through that camera, which in VR faces wherever the stick last turned you, so a room seen through a doorway stops being drawn the moment you turn only your head. This replaces the earlier WidenCullingForVR, which widened the flat camera's cone instead of aiming it, and could not survive a large turn.");
        _pCullExtraDeg = _cfg.CreateEntry("CullingExtraDegrees", 70f, null,
            "How much wider than the game's own field of view the culling cone is, in degrees, so both eyes and a head turn between frames are comfortably inside it. Raise it if anything still vanishes at the edges, lower it to claw back frame rate. 0 draws everything, which never drops a thing and costs the most.");
        _pVoidProbe = _cfg.CreateEntry("VoidProbeLog", true, null,
            "Twice a second, cast one ray where you are looking and one where the flat camera is looking, and log what each one hit and whether that object is switched off, has its renderer disabled, or is simply not being drawn. This is what names the cause of a room vanishing through a doorway. Two raycasts a second, which is nothing; the earlier whole-scene census is gone.");
        _pCullExtraFov = _cfg.CreateEntry("CullExtraFov", 55f, null,
            "Degrees added to the camera's field of view for culling only, not for rendering. Raise it if things still vanish at the edges, lower it to claw back some frame rate. 0 switches culling off entirely, which never drops anything but draws the whole room.");
        _pFollowRecenter = _cfg.CreateEntry("FollowHeadsetRecenter", true, null,
            "When the headset's own recenter fires, which on a Steam Frame is the hardware button, move the mod's playspace origin with it so the view actually resets in game. Without this the runtime shifts the tracking universe, the mod keeps its old origin, and the button appears to do nothing.");
        _pRecenterJump = _cfg.CreateEntry("RecenterJumpMetres", 0.25f, null,
            "How far the head has to appear to move between two frames before it is treated as the runtime recentring rather than the player moving. A head cannot cross 25cm in one frame, so anything beyond that is the world moving, not you. Raise it if a normal fast movement ever trips it.");
        // Renamed from TerminalTypingBoard so this default actually lands: MelonPreferences
        // keeps a stored value forever, so a changed default never reaches an existing key.
        _pTypeBoard = _cfg.CreateEntry("TypingBoardFallback", false, null,
            "Off by design. A floating QWERTY board in front of you while a terminal is open, kept only as a fallback in case touching the terminal's own keys cannot be made to work. The intended way to type is to press the real keys on the in-game keyboard with a fingertip.");
        _pDumpKeys = _cfg.CreateEntry("DumpTerminalKeyGeometry", true, null,
            "The first time a computer terminal opens, write the object names, positions and vertex counts around it to UserData, so the real keyboard keys can be found and pressed with a fingertip instead of being calibrated by hand. Harmless, runs once per terminal, costs nothing after that.");
        _pBoardScale = _cfg.CreateEntry("TerminalBoardScale", 1.0f, null, "Overall size of the typing board.");
        _pBoardDrop = _cfg.CreateEntry("TerminalBoardDrop", 0.34f, null, "How far below eye level the typing board sits, metres.");
        _pBoardReach = _cfg.CreateEntry("TerminalBoardReach", 0.50f, null, "How far in front of you the typing board sits, metres.");
        _pBoardTilt = _cfg.CreateEntry("TerminalBoardTilt", 42f, null, "Degrees the typing board lies back toward the horizontal.");
        _pClickVolume = _cfg.CreateEntry("RecorderClickVolume", 0.55f, null,
            "Loudness of the record key's mechanical click. 0 silences it.");
        _pHissVolume = _cfg.CreateEntry("RecorderHissVolume", 0f, null,
            "Loudness of the tape hiss while it is running. 0 silences it.");
        _pIndexPressDeg = _cfg.CreateEntry("RecorderIndexPressDegrees", 6f, null,
            "How far the index finger bends as it presses the key down, in degrees at the middle joint. The key itself only travels 3mm, so this is small on purpose.");
        _pThumbCorner = _cfg.CreateEntry("RecorderThumbCorner", 0.80f, null,
            "Where along the near edge of the recorder the thumb rests, as a fraction of half its length. 1.0 is the very top corner by the microphone, 0 is halfway down, negative puts it below centre. The thumb is swung to that corner from its authored pose and clamped, so it cannot bend anywhere a thumb does not.");
        _pThumbBack = _cfg.CreateEntry("RecorderThumbBack", 0.045f, null,
            "How far back down the body the thumb sits, metres, measured toward the heel of the hand from where the authored pose puts it. 0.045 is about an inch and three quarters. Applied as one clamped swing on the base joint, so it cannot roll the thumb anywhere a thumb does not go.");
        _pGripSkin = _cfg.CreateEntry("RecorderGripSkin", 0.008f, null,
            "How far outside the casing the fingertips are aimed, metres, so pads rest on the surface instead of sinking into it. Raise it if the fingers still look fused to the recorder, lower it if they hover.");
        _pTapeSideOut = _cfg.CreateEntry("RecorderTapeSideOut", true, null,
            "Turn the recorder so the cassette window and speaker face out at you and the plain black back rests against the palm, the way a phone sits in a hand. This replaces RecorderFaceFlip, which was created as false and would have kept that stored value.");
        _pRecorderFaceFlip = _cfg.CreateEntry("RecorderFaceFlip", false, null,
            "Turn the recorder to face the other way if the cassette window ends up against the palm instead of looking out at you.");
        _pRecorderAlong = _cfg.CreateEntry("RecorderUpPalmGrip", 0.72f, null,
            "Where the recorder sits up the palm, as a fraction of the measured wrist-to-knuckle length, so it lands the same on any hand size. 0.78 puts its middle just below the knuckles, which leaves the microphone end clear of the index finger. This replaces RecorderAlong, which meant metres; leaving the name alone would have kept reading the old 0.052 as a fraction and parked the recorder on the wrist. Delete the dead RecorderAlong line if you like, nothing reads it.");
        _pRecorderTilt = _cfg.CreateEntry("RecorderTilt", 18f, null,
            "Degrees the recorder's length angles away from the knuckle row toward the fingertips. 0 lays it straight along the knuckles.");
        _pFingerWrap = _cfg.CreateEntry("RecorderFingerWrap", 0.55f, null,
            "How far the middle, ring and little fingers close around the casing. PoseHoldFlashlight alone leaves them almost straight and Codex then lifts the knuckles another 16 to 24 degrees, which is why the hand looked splayed with the recorder lying against it.");
        _pIndexCurl = _cfg.CreateEntry("RecorderIndexCurl", 0.22f, null,
            "How much shape the index finger keeps while it reaches for the record key. 0 is a straight stick.");
        _pRecorderScale = _cfg.CreateEntry("RecorderScale", 1.0f, null,
            "Overall recorder size. 1.0 is the authored mesh, about 5.4 x 11.6 x 2.3 cm.");
        _pIndexOnButton = _cfg.CreateEntry("RecorderIndexOnButton", true, null,
            "Press the recorder's record button with the index finger and wrap the thumb around the body. Codex built the grip the other way round, with the thumb reaching for the button and the index in the supporting row, which is why the index misses it and the thumb sits oddly.");
        _pThumbWrap = _cfg.CreateEntry("RecorderThumbWrap", 0.72f, null,
            "How closed the thumb is when wrapped around the casing, 0 open to 1 fist. Raise it if the thumb floats off the body, lower it if it sinks into it.");
        _pThumbCurl = _cfg.CreateEntry("RecorderThumbCurl", 24f, null,
            "Extra palmward bend on the thumb's two outer joints, in degrees, on the same anatomical hinge Codex uses so it can never fold backward.");
        _pIndexAimLimit = _cfg.CreateEntry("RecorderIndexAimLimit", 55f, null,
            "Most the index knuckle may swing per solve step to reach the button, in degrees. Lower if the finger looks dislocated, raise if it cannot reach.");
        _pIndexTipScale = _cfg.CreateEntry("RecorderIndexTipScale", 0.85f, null,
            "Where the fingertip is taken to be, as a fraction of the last bone's length past its joint. Only affects how precisely the pad lands on the button.");
        _pHandLight = _cfg.CreateEntry("HandLightResponse", false, null,
            "OFF: tried and abandoned. Let the hands, arms and recorder darken with the room. They are drawn with unlit shaders so nothing in the world can reach them, which is why they glow in a pitch black room. This measures the light where you are standing and dims their colours to match, leaving the flat painted shading intact.");
        _pHandLightFloor = _cfg.CreateEntry("HandLightFloor", 0.04f, null,
            "How dark they are allowed to get, 0 to 1. 0 is genuinely invisible in an unlit room; a little above that keeps a hint of shape.");
        _pHandLightCeiling = _cfg.CreateEntry("HandLightCeiling", 1.0f, null,
            "Brightness in a fully lit room. 1 is exactly how they look now.");
        _pHandLightCurve = _cfg.CreateEntry("HandLightCurve", 0.8f, null,
            "Gamma on the response. Below 1 holds mid light up so rooms do not crush to black too early; above 1 makes the falloff harsher.");
        _pHandLightSmoothing = _cfg.CreateEntry("HandLightSmoothing", 0.15f, null,
            "Seconds for the brightness to catch up. Stops a flickering lamp strobing your hands.");
        _pHandLightTargets = _cfg.CreateEntry("HandLightTargets", "hands|recorder|arms", null,
            "Which of the three this applies to, separated by |. Drop one from the list to leave it at full brightness.");
        _pRoomPanelCams = _cfg.CreateEntry("ExtraRoomPanelCameras", "Laundry Room Camera", null,
            "Cameras to treat the way Codex treats the Casino's Slot Cam, separated by |, matched at the start of the name. A room camera on this list gets captured onto the flat VR panel; one that is not on it is skipped by RenderUIPanel and ends up on the monitor only, which is why the laundry menu was invisible in the headset. Coat Check is deliberately absent: that one already arrives through the ordinary UI path.");
        _pPanelFollow = _cfg.CreateEntry("PanelFollowsGaze", true, null,
            "Keep the flat menu panel in front of you. Codex fixes the panel in place the moment a menu opens and never moves it again, so if that one instant catches you mid-turn, or the menu opened while you were facing elsewhere, it is stranded off to the side or behind you for as long as the menu is up. This brings it back to you instead of arguing about why it was wrong.");
        _pPanelDeadzone = _cfg.CreateEntry("PanelFollowDeadzone", 22f, null,
            "How far off centre the panel may sit, in degrees, before it starts coming back. Inside this it stays put so it does not swim while you read it.");
        _pPanelSnapAngle = _cfg.CreateEntry("PanelSnapAngle", 70f, null,
            "Past this many degrees off, the panel is put in front of you at once rather than gliding there. For menus that opened behind you.");
        _pPanelFollowSpeed = _cfg.CreateEntry("PanelFollowSpeed", 200f, null,
            "How fast the panel catches up, in degrees per second.");
        _pKillBlur = _cfg.CreateEntry("DisableScreenBlur", true, null,
            "Switch off the game's full-screen blur image effect. It is a flat-screen depth-of-field trick the game turns on behind things like the slot machine; in a headset it blurs the whole world around you and reads as a smeared mess.");
        _pPanelAimLog = _cfg.CreateEntry("LogPanelAim", true, null,
            "Log where the flat menu panel actually sits relative to where you are looking, in degrees. Read-only. This is what settles whether a menu really is off to the side or only looks that way on the monitor.");
        _pPumpPointPose = _cfg.CreateEntry("PumpStiffPoint", true, null,
            "Point at pump room buttons with a stiff finger instead of the curled typing hand. Codex reuses its computer-typing flag for the pump buttons, which brings the typing pose with it. Real keyboards are untouched.");
        _pFloorPlanInDraft = _cfg.CreateEntry("FloorPlanDuringDraft", true, null,
            "Let the floor plan be opened while you are choosing a room. Codex blocks the floor plan button whenever the draft is up, which is exactly when you want to look at the house. Only the floor plan button is told the draft is closed; stick navigation and everything else still see it.");
        _pTitleSmoke = _cfg.CreateEntry("TitleVRSmoke", true, null,
            "Keep the VR smouldering after the puff, with smoke drifting up off the lettering for as long as you sit on the title screen.");
        _pTitleSmokeRate = _cfg.CreateEntry("TitleVRSmokeRate", 0.16f, null,
            "Seconds between wisps. Lower is thicker smoke.");
        _pTitlePoof = _cfg.CreateEntry("TitleVRPoof", true, null,
            "Have the VR appear on the title screen in a puff of smoke rather than simply being there.");
        _pTitlePoofDelay = _cfg.CreateEntry("TitleVRPoofDelay", 1.1f, null,
            "Seconds to wait after the menu settles before the puff, so you are looking at the title when it happens.");
        _pSteerStale = _cfg.CreateEntry("DropStaleViewSteering", true, null,
            "Forget where hand-aim steering was pointing the moment a menu opens or the view is moved by the game. Codex locks onto a world point when you hold the trigger and keeps nudging the mouse until the view looks at it, but it never forgets that point when the cursor unlocks, so choosing a door leaves it steering at something in the room you just left. That is the head ending up facing the wrong way, and the mouse motion it generates is what flips the game into mouse mode so the plan can no longer be picked with the controller.");
        _pSteerSnapDeg = _cfg.CreateEntry("ViewSnapDegrees", 20f, null,
            "A view turn larger than this in a single frame is the game moving you, not your hand, so any steering goal is dropped. Lower it if the view still ends up crooked, raise it if steering gives up during fast turns.");
        _pFixRewired = _cfg.CreateEntry("FillRewiredActionNames", true, null,
            "Fill in the game's Rewired action names when they are blank. Codex ships them empty and expects each person to copy them out of their own log, so on a fresh install the sticks, interact, cancel and menu do nothing through the controllers and input falls back to keyboard. The names are the same in every copy of the game, so there is nothing to discover.");
        _pRaiseResScale = _cfg.CreateEntry("RaiseStockResolutionScale", 1.0f, null,
            "If Codex's ResolutionScale is still sitting on its stock 0.5, raise it to this. 0.5 renders each eye at half the headset's recommended size, which just looks blurry. Set 0 to leave it alone. Takes effect on the next launch, because the eye textures are sized once at startup.");
        _pTitleBackdrop = _cfg.CreateEntry("TitleVRBackdrop", "MAIN MENU BG", null,
            "Name of the menu backdrop object the VR is positioned against. The whole title screen is one painted quad with that name, so every other number here is a fraction of its size and stays right at any resolution. Leave it alone unless the log says the object was not found.");
        _pTitleImage = _cfg.CreateEntry("TitleVRImage", "BPVR_VR.png", null,
            "File name of the VR artwork, looked for in UserData, then in Mods, then in the game folder. A full path also works. Replace the file to change the lettering; it is used at its own aspect ratio.");
        _pTitleOffX = _cfg.CreateEntry("TitleVROffsetX", 0.0476f, null,
            "Where the VR sits across the title screen, as a fraction of the backdrop width from its centre. Bigger moves it right.");
        _pTitleOffY = _cfg.CreateEntry("TitleVROffsetY", 0.2019f, null,
            "Where the VR sits up the title screen, as a fraction of the backdrop height from its centre. Bigger moves it up.");
        _pTitleHeight = _cfg.CreateEntry("TitleVRHeight", 0.0931f, null,
            "Height of the VR artwork as a fraction of the backdrop height. Width follows the image's own shape.");
        _pTitleDepth = _cfg.CreateEntry("TitleVRDepth", 0.01f, null,
            "How far in front of the backdrop to float the VR, in metres, so it is never swallowed by the painting behind it.");
        _pDraftMarkers = _cfg.CreateEntry("DraftMarkers", "REDRAW|REMOVE FROM TODAY|FLOORPLANS|DRAFT THIS FLOORPLAN", null,
            "Wording that only appears during the room draft, separated by |. Codex looks for SELECT A ROOM which this build never shows. GEM REQUIREMENT is deliberately not here: it is on screen all the time.");
        _pVerbose = _cfg.CreateEntry("Verbose", true, null,
            "Log what each patch does. Useful while calibrating.");
    }

    public override void OnLateInitializeMelon()
    {
        var target = typeof(VRPlugin);
        LoggerInstance.Msg("Patching " + target.Assembly.GetName().Name + " from the outside.");
        LoggerInstance.Msg("ClaudeFixes build c96-menunav");
        FixCodexInputDefaults();

        BindWatchFields();
        _journalBase = SafeField<Color[]>("_journalBase");
        _mouseHeld = SafeField<bool>("_mouseHeld");
        _panelAimValid = SafeField<bool>("_panelAimValid");
        _codexReanchors = AccessTools.Field(typeof(VRPlugin), "_lastPanelRoomCamera") != null;
        if (_codexReanchors)
            LoggerInstance.Msg("[panel] Codex re-anchors the panel itself in this build; leaving it alone");
        _headRot = SafeField<Quaternion>("_headRot");
        _roomPanelCam = SafeField<Camera>("_roomPanelCamera");
        _lastExploreCam = SafeField<Camera>("_lastExplorationCamera");
        _comfortText = SafeField<Il2CppTMPro.TextMeshPro>("_comfortText");
        _comfortRow = SafeField<int>("_comfortRow");
        _comfortCalibrating = SafeField<bool>("_comfortCalibrate");
        BindComfortSettings(target);
        _magnifierRef = SafeField<bool>("_magnifierPointer");
        _steerLocked = SafeField<bool>("_steerLocked");
        _steerHeld = SafeField<float>("_steerHeld");
        _steerWorse = SafeField<int>("_worseFrames");
        _steerLastErr = SafeField<float>("_lastErr");
        _repaintJournal = AccessTools.Method(target, "RepaintJournal");
        _isDigTarget = AccessTools.Method(target, "IsDigTarget", new[] { typeof(Collider) });
        _isPickupTarget = AccessTools.Method(target, "IsPickupTarget", new[] { typeof(Collider) });
        if (_isPickupTarget != null) _isPickupTargetStatic = _isPickupTarget.IsStatic;
        if (_isDigTarget != null) _isDigTargetStatic = _isDigTarget.IsStatic;

        var self = typeof(Mod);
        Patch("LoadNotebookPage", null, null, self.GetMethod(nameof(JournalPostfix), BindingFlags.Static | BindingFlags.NonPublic));
        Patch("LoadJournalPhoto", null, null, self.GetMethod(nameof(JournalPostfix), BindingFlags.Static | BindingFlags.NonPublic));
        Patch("PlaceUIPanel", new[] { typeof(Quaternion), typeof(Vector3) },
            self.GetMethod(nameof(PanelPrefix), BindingFlags.Static | BindingFlags.NonPublic), null);
        Patch("ClearClickState", Type.EmptyTypes,
            self.GetMethod(nameof(ClearClickPrefix), BindingFlags.Static | BindingFlags.NonPublic), null);
        Patch("ClickFromTrigger", new[] { typeof(Hand) },
            self.GetMethod(nameof(ClickPrefix), BindingFlags.Static | BindingFlags.NonPublic),
            self.GetMethod(nameof(ClickPostfix), BindingFlags.Static | BindingFlags.NonPublic));
        Patch("PlaceUIPanel", new[] { typeof(Quaternion), typeof(Vector3) },
            null, self.GetMethod(nameof(ComputerViewPostfix), BindingFlags.Static | BindingFlags.NonPublic));
        Patch("PlaceUIPanel", new[] { typeof(Quaternion), typeof(Vector3) },
            null, self.GetMethod(nameof(PanelFollowPostfix), BindingFlags.Static | BindingFlags.NonPublic));
        Patch("CoatCheckView", Type.EmptyTypes, null,
            self.GetMethod(nameof(CoatCheckPostfix), BindingFlags.Static | BindingFlags.NonPublic));
        Patch("DirectHandClick", new[] { typeof(Hand), typeof(bool) },
            self.GetMethod(nameof(DirectHandClickPrefix), BindingFlags.Static | BindingFlags.NonPublic), null);
        Patch("IsPickupName", new[] { typeof(string) },
            null, self.GetMethod(nameof(PickupNamePostfix), BindingFlags.Static | BindingFlags.NonPublic));
        Patch("RoomPickupCandidate", new[] { typeof(Hand), typeof(Vector3), typeof(RaycastHit).MakeByRefType(), typeof(bool).MakeByRefType() },
            null, self.GetMethod(nameof(RoomPickupPostfix), BindingFlags.Static | BindingFlags.NonPublic));
        Patch("VisibleHoverItem", new[] { typeof(Collider), typeof(Vector3), typeof(RaycastHit).MakeByRefType() },
            null, self.GetMethod(nameof(VisiblePostfix), BindingFlags.Static | BindingFlags.NonPublic));
        Patch("DetectDraftMenu", Type.EmptyTypes, null,
            self.GetMethod(nameof(DraftPostfix), BindingFlags.Static | BindingFlags.NonPublic));
        Patch("OnUpdate", Type.EmptyTypes, null,
            self.GetMethod(nameof(UpdatePostfix), BindingFlags.Static | BindingFlags.NonPublic));
        // After Codex's Frame() has read the camera and placed the eyes, and before the
        // renderer culls, which is the only window where biasing the camera is harmless.
        Patch("OnLateUpdate", Type.EmptyTypes, null,
            self.GetMethod(nameof(LateUpdatePostfix), BindingFlags.Static | BindingFlags.NonPublic));
        PatchRoomCuller();
        PatchMagnifier();
        PatchCelHandPose();
        PatchRecorderGrip();
        PatchRecorderHand();
        PatchRecorderPolish();
        Patch("ComfortFrame", new[] { typeof(Vector3), typeof(Quaternion) },
            null, self.GetMethod(nameof(ComfortFramePostfix), BindingFlags.Static | BindingFlags.NonPublic));
        Patch("PollFloorPlanButton", Type.EmptyTypes,
            self.GetMethod(nameof(FloorPlanPrefix), BindingFlags.Static | BindingFlags.NonPublic),
            self.GetMethod(nameof(FloorPlanPostfix), BindingFlags.Static | BindingFlags.NonPublic));
        Patch("InteractionBoxRoot", new[] { typeof(Collider) }, null,
            self.GetMethod(nameof(BoxRootPostfix), BindingFlags.Static | BindingFlags.NonPublic));
        Patch("BoxReach", new[] { typeof(Transform) }, null,
            self.GetMethod(nameof(BoxReachPostfix), BindingFlags.Static | BindingFlags.NonPublic));
        Patch("SteerViewToHand", new[] { typeof(Camera), typeof(Hand) },
            self.GetMethod(nameof(SteerPrefix), BindingFlags.Static | BindingFlags.NonPublic), null);
        Patch("DigEdgeAssist", new[] { typeof(Hand), typeof(RaycastHit).MakeByRefType() },
            null, self.GetMethod(nameof(DigPostfix), BindingFlags.Static | BindingFlags.NonPublic));
        Patch("UpdatePanelStickPointer", Type.EmptyTypes,
            self.GetMethod(nameof(PanelStickPrefix), BindingFlags.Static | BindingFlags.NonPublic),
            self.GetMethod(nameof(PanelStickPostfix), BindingFlags.Static | BindingFlags.NonPublic));
        try {
            var buttonDown = AccessTools.Method(typeof(VRInput), "ButtonDownFor",
                new[] { typeof(string), typeof(bool).MakeByRefType() });
            if (buttonDown == null) LoggerInstance.Warning("skipped: no VRInput.ButtonDownFor in this build of the VR mod");
            else {
                HarmonyInstance.Patch(buttonDown, null,
                    new HarmonyMethod(self.GetMethod(nameof(ButtonDownPostfix), BindingFlags.Static | BindingFlags.NonPublic)));
                LoggerInstance.Msg("patched VRInput.ButtonDownFor");
            }
        } catch (Exception e) { LoggerInstance.Error("could not patch VRInput.ButtonDownFor: " + e.Message); }
    }

    private static AccessTools.FieldRef<VRPlugin, T> SafeField<T>(string name)
    {
        try {
            if (AccessTools.Field(typeof(VRPlugin), name) == null) return null;
            return AccessTools.FieldRefAccess<VRPlugin, T>(name);
        } catch { return null; }
    }

    private void Patch(string name, Type[] args, MethodInfo prefix, MethodInfo postfix)
    {
        try {
            var method = args == null ? AccessTools.Method(typeof(VRPlugin), name)
                                      : AccessTools.Method(typeof(VRPlugin), name, args);
            if (method == null) {
                LoggerInstance.Warning("skipped: no method VRPlugin." + name + " in this build of the VR mod");
                return;
            }
            HarmonyInstance.Patch(method,
                prefix == null ? null : new HarmonyMethod(prefix),
                postfix == null ? null : new HarmonyMethod(postfix));
            LoggerInstance.Msg("patched VRPlugin." + name);
        } catch (Exception e) {
            LoggerInstance.Error("could not patch VRPlugin." + name + ": " + e.Message);
        }
    }

    internal static void Say(string message)
    {
        if (_pVerbose != null && !_pVerbose.Value) return;
        I?.LoggerInstance.Msg(message);
    }

    // ---------- notebook brightness ----------

    private static Color[] _lastDimmed;

    private static void JournalPostfix(VRPlugin __instance)
    {
        if (_journalBase == null) return;
        try {
            Color[] page = _journalBase(__instance);
            if (page == null || ReferenceEquals(page, _lastDimmed)) return;
            float scale = Mathf.Clamp(_pNotebookBrightness == null ? .55f : _pNotebookBrightness.Value, 0.05f, 1f);
            _lastDimmed = page;
            if (scale >= 0.999f) return;
            for (int i = 0; i < page.Length; i++) {
                Color c = page[i];
                page[i] = new Color(c.r * scale, c.g * scale, c.b * scale, c.a);
            }
            _repaintJournal?.Invoke(__instance, null);
            Say("[notebook] page dimmed to " + scale.ToString("0.00") + " of stock brightness");
        } catch (Exception e) { Say("[notebook] dim failed: " + e.Message); }
    }

    // ---------- casino / menu panel re-anchor ----------

    private static bool _cursorWasFree;

    // Codex's rebuild added its own re-anchor at the top of PlaceUIPanel
    // ("never reuse an aim left behind by the previous menu"), and ends every
    // call with _panelAimValid = flag. Mine then cleared the aim a second time,
    // on the cursor-unlock edge, which is a frame earlier and can catch the head
    // mid-turn - and because the aim is then held for the life of the menu, the
    // panel stays wherever you happened to be facing. That is the casino panel
    // off to the right and the laundry menu out of view. Detect their version by
    // a field that only exists alongside it, and stand down.
    private static bool _codexReanchors;

    private static void PanelPrefix(VRPlugin __instance)
    {
        if (_codexReanchors) return;
        if (_panelAimValid == null || _pRecenterPanel == null || !_pRecenterPanel.Value) return;
        try {
            bool free = Cursor.lockState == CursorLockMode.None;
            if (free && !_cursorWasFree) {
                _panelAimValid(__instance) = false;
                Say("[panel] menu became interactive, re-aiming the panel to your gaze");
            }
            _cursorWasFree = free;
        } catch { }
    }

    // ---------- forklift hold-to-drag ----------

    private static bool _holding;
    private static float _holdSince;
    private static Collider _propCached;
    private static bool _propAnswer;

    // Every guard in ClickFromTrigger funnels through ClearClickState, which sends
    // mouse up. While a hold is live, that release is what drops the handle.
    private static bool ClearClickPrefix() => !_holding;

    private static bool ClickPrefix(VRPlugin __instance, Hand h)
    {
        if (!_holding) return true;
        bool down = false;
        try { down = h != null && !h.IsLeft && h.Valid && h.TriggerDown; } catch { }
        // Safety valve so a tracking dropout cannot leave the button stuck down.
        if (down && Time.unscaledTime - _holdSince < 90f) return false;
        ReleaseHold(__instance);
        return false;
    }

    private static void ClickPostfix(VRPlugin __instance, Hand h)
    {
        if (h != null && !h.IsLeft) _lastRightHand = h;
        QueuePress(__instance, h);
        if (_holding || _pForkliftHold == null || !_pForkliftHold.Value || _mouseHeld == null) return;
        try {
            if (h == null || h.IsLeft || !_mouseHeld(__instance)) return;
            if (!IsHoldDragProp(h.AimCollider)) return;
            _holding = true;
            _holdSince = Time.unscaledTime;
            Say("[forklift] holding '" + (h.AimCollider == null ? "?" : h.AimCollider.name) + "'; guards suspended until you let the trigger go");
        } catch { }
    }

    private static void ReleaseHold(VRPlugin instance)
    {
        _holding = false;
        try {
            if (_mouseHeld != null && _mouseHeld(instance)) {
                mouse_event(4u, 0, 0, 0u, IntPtr.Zero);
                _mouseHeld(instance) = false;
            }
            Say("[forklift] let go after " + (Time.unscaledTime - _holdSince).ToString("0.0") + "s");
        } catch (Exception e) { Say("[forklift] release failed: " + e.Message); }
    }

    // The forklift rig is a Curvy SplineController driven by a PlayMakerFSM that
    // owns a 'Let go of Handle' state plus activate/deactivate global transitions.
    private static float _propCachedAt;
    private static string _propLogged = "";

    private static bool NameSaysHoldDrag(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        string list = _pHoldDragNames?.Value;
        if (string.IsNullOrWhiteSpace(list)) return false;
        foreach (var raw in list.Split('|')) {
            string want = raw.Trim();
            if (want.Length > 0 && name.StartsWith(want, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private static bool IsHoldDragProp(Collider col)
    {
        if (col == null) return false;
        try {
            // The old cache was keyed on the collider and never expired, so the
            // very first frame you aimed at the forklift decided the answer for
            // good. PlayMaker builds Fsm lazily, so that first look can land
            // before the states exist, the read throws, and false sticks for
            // the rest of the session. Re-check a few times a second instead.
            float now = Time.unscaledTime;
            if (col == _propCached && now - _propCachedAt < .4f) return _propAnswer;
            _propCached = col;
            _propCachedAt = now;
            _propAnswer = false;

            int depth = 0;
            for (var t = col.transform; t != null && depth < 8; t = t.parent, depth++) {
                // A plain name match, which cannot be defeated by FSM timing.
                if (NameSaysHoldDrag(t.name)) { _propAnswer = true; break; }

                foreach (var fsm in t.gameObject.GetComponents<PlayMakerFSM>()) {
                    if (fsm == null) continue;
                    bool letGo = false, activate = false, deactivate = false;
                    try {
                        foreach (var state in fsm.Fsm.States)
                            if (state != null && state.Name.IndexOf("Let go", StringComparison.OrdinalIgnoreCase) >= 0) letGo = true;
                        foreach (var transition in fsm.Fsm.GlobalTransitions) {
                            if (transition == null) continue;
                            if (transition.EventName == "activate") activate = true;
                            if (transition.EventName == "deactivate") deactivate = true;
                        }
                    } catch { continue; }
                    if (letGo && activate && deactivate) { _propAnswer = true; break; }
                }
                if (_propAnswer) break;
                if (t.name == "Pullers and Lifters" || t.name == "_GAMEPLAY") break;
            }

            string key = (col.name ?? "?") + "=" + _propAnswer;
            if (key != _propLogged) { _propLogged = key; Say("[forklift] '" + (col.name ?? "?") + "' hold-to-drag: " + _propAnswer); }
            return _propAnswer;
        } catch { return false; }
    }

    private static readonly RaycastHit[] _sweepBuffer = new RaycastHit[32];

    private static Hand _lastRightHand;

    // Codex's Frame() has finished: the head pose for this frame is settled and the eyes
    // are placed, and nothing reads the camera's rotation again before the renderer culls.
    private static void LateUpdatePostfix(VRPlugin __instance)
    {
        TieCamYawToHead(__instance);
        VoidProbe(__instance);
        DriveMagnifier(__instance);
    }

    private static void UpdatePostfix(VRPlugin __instance)
    {
        _vr = __instance;
        DeliverQueuedPress(__instance, _lastRightHand);
        UpdateForkliftLift(__instance);
        LatchUseItem();
        // Before Codex's Frame() reads the camera in LateUpdate, so it reads the body yaw
        // it set itself and never sees the head bias added at the end of the last frame.
        ClearCamYawBias();
        NotePlayerPos();
        CullFromHead(__instance);
        CatchItemComputer(__instance);
        FollowSystemRecenter(__instance);
        DumpTerminalKeys(__instance);
        FullKeyboard(__instance);
        UpdateTypeBoard(__instance);
        try { if (_quickMemoRef != null) _memoOpenNow = _quickMemoRef(__instance); } catch { }
        WatchMagnifier(__instance);
        BuildMainMenuRow();
        UpdateVRPage();
        WatchCursorMode(__instance);
        WatchCancelAndMenu();
        LogDraftLabels();
        KillScreenBlur();
        UpdateHandLight();
        PlaceTitleVR();
        AnimateTitleVR();

    }

    // ---------- room draft menu detection ----------
    // Codex decides the draft is open by scanning on-screen text for "SELECT A ROOM"
    // / "CHOOSE ROOM" / "CHOOSE A ROOM". The real draft UI is the hierarchy
    // __SYSTEM/THE DRAFT/anchor/DRAFT UI/DRAFT PLAN n, whose heading does not contain
    // those words, so _draftMenu never becomes true and stick navigation never arms.
    // Detect the hierarchy instead of the wording.

    private static float _draftCheckAt;
    private static bool _draftSeen;
    private static bool _draftAnnounced;

    private static void DraftPostfix(VRPlugin __instance)
    {
        if (_draftMenu == null || _pFixDraftMenu == null || !_pFixDraftMenu.Value) return;
        try {
            if (Cursor.lockState != CursorLockMode.None) { _draftSeen = false; return; }
            if (_draftMenu(__instance)) return;

            if (Time.unscaledTime >= _draftCheckAt) {
                _draftCheckAt = Time.unscaledTime + .25f;
                _draftSeen = DraftUiOnScreen();
            }
            if (!_draftSeen) return;

            _draftMenu(__instance) = true;
            if (!_draftAnnounced) {
                _draftAnnounced = true;
                I?.LoggerInstance.Msg("[draft] room choice detected by its own wording; stick navigation armed");
            }
        } catch { }
    }

    // The draft moment is identified by wording that only appears then. Codex looks
    // for SELECT A ROOM / CHOOSE ROOM, none of which this build shows. What it
    // actually shows is REDRAW, Floorplans and REMOVE FROM TODAY'S POOL. GEM
    // REQUIREMENT is deliberately excluded: that one is on screen constantly, which
    // is what made detecting the DRAFT hierarchy far too broad.
    private static bool DraftUiOnScreen()
    {
        // Only ever called with the cursor unlocked, so the hierarchy test is
        // safe here even though it was far too broad as a standalone check.
        // The wording varies: a lock pick opens a draft with no REDRAW and no
        // REMOVE FROM TODAY'S POOL on it, which is why text alone kept missing
        // and left the room choice on the mouse.
        ProbeDraftObjects();
        if (_pDraftByCards != null && _pDraftByCards.Value) {
            try {
                var cam = Camera.main;
                Vector3 eye = cam != null ? cam.transform.position : Vector3.zero;
                float range = _pDraftPlanRange == null ? 50f : Mathf.Max(1f, _pDraftPlanRange.Value);
                for (int i = 1; i <= 4; i++) {
                    var plan = GameObject.Find("DRAFT PLAN " + i);
                    if (plan == null || !plan.activeInHierarchy) continue;
                    float d = Vector3.Distance(plan.transform.position, eye);
                    if (d > range) continue;      // parked off in the void, not on screen
                    if (!_draftByPlan) {
                        _draftByPlan = true;
                        Say("[draft] room choice seen: '" + plan.name + "' is " + d.ToString("0.0") + "m away");
                    }
                    return true;
                }
            } catch { }
        }

        try {
            string[] markers = (_pDraftMarkers?.Value ?? "REDRAW|REMOVE FROM TODAY|FLOORPLANS|DRAFT THIS FLOORPLAN").Split('|');
            foreach (var label in UnityEngine.Object.FindObjectsOfType<Il2CppTMPro.TMP_Text>()) {
                if (label == null || !label.isActiveAndEnabled) continue;
                string text = (label.text ?? "").ToUpperInvariant();
                if (text.Length == 0 || text.Length > 60) continue;
                foreach (var marker in markers) {
                    string m = marker.Trim().ToUpperInvariant();
                    if (m.Length > 0 && text.Contains(m)) return true;
                }
            }
        } catch { }
        return false;
    }

    private static bool _draftByPlan;

    // The cards alone are not the answer: once you have drafted, DRAFT PLAN 1..3
    // stay active for the rest of the day, so every later menu looked like the
    // room choice. Record what IS different while the choice is up, so the next
    // run names a signal that only holds then: which objects are live, how far
    // they are from the camera, and whether their renderers are drawing.
    private static string _probeLast = "";
    private static int _probeLogs;

    private static void ProbeDraftObjects()
    {
        if (_pProbeDraft == null || !_pProbeDraft.Value || _probeLogs >= 25) return;
        try {
            var cam = Camera.main;
            Vector3 eye = cam != null ? cam.transform.position : Vector3.zero;
            var parts = new List<string>();

            foreach (var name in new[] { "DRAFT PLAN 1", "DRAFT PLAN 2", "DRAFT PLAN 3",
                                         "DRAFT CAM", "DRAFT UI", "THE DRAFT", "DRAFT BUTTON" }) {
                var go = GameObject.Find(name);
                if (go == null) { parts.Add(name + "=gone"); continue; }
                string bit = name + "=on";
                try {
                    float d = Vector3.Distance(go.transform.position, eye);
                    bit += " " + d.ToString("0.0") + "m";
                } catch { }
                try {
                    var r = go.GetComponentInChildren<Renderer>(false);
                    bit += r == null ? " norend" : (r.isVisible ? " vis" : " hidden");
                } catch { }
                try {
                    var c = go.GetComponentInChildren<Camera>(false);
                    if (c != null) bit += c.enabled ? " CAM-on" : " CAM-off";
                } catch { }
                parts.Add(bit);
            }

            string line = string.Join(" | ", parts);
            if (line == _probeLast) return;
            _probeLast = line;
            _probeLogs++;
            I?.LoggerInstance.Msg("[draftprobe] " + line);
        } catch { }
    }



    // Read-only: the wording under THE DRAFT, used to identify the room-choice
    // moment. Kept so a future build change can be spotted quickly.
    private static float _labelScanAt;
    private static string _lastLabelSet;
    private static int _labelLogs;

    private static void LogDraftLabels()
    {
        if (_pLogDraftLabels == null || !_pLogDraftLabels.Value || _labelLogs >= 12) return;
        try {
            if (Cursor.lockState != CursorLockMode.None) return;
            if (Time.unscaledTime < _labelScanAt) return;
            _labelScanAt = Time.unscaledTime + .5f;

            var root = GameObject.Find("THE DRAFT");
            if (root == null || !root.activeInHierarchy) return;

            var seen = new List<string>();
            foreach (var label in root.GetComponentsInChildren<Il2CppTMPro.TMP_Text>(false)) {
                if (label == null || !label.isActiveAndEnabled) continue;
                string text = (label.text ?? "").Replace("\n", " ").Trim();
                if (text.Length == 0 || text.Length > 60) continue;
                if (!seen.Contains(text)) seen.Add(text);
                if (seen.Count >= 14) break;
            }
            if (seen.Count == 0) return;

            string signature = string.Join(" | ", seen);
            if (signature == _lastLabelSet) return;
            _lastLabelSet = signature;
            _labelLogs++;
            I?.LoggerInstance.Msg("[draftlabels] " + signature);
        } catch { }
    }

    // ---------- B button probe ----------
    // Backing out of a letter or picture with B also opens a menu, so B is very
    // likely satisfying both the cancel action and the menu action in one press.

    private static bool _cancelWas, _menuWas;

    private static void WatchCancelAndMenu()
    {
        if (_pWatchButtons == null || !_pWatchButtons.Value || _modeLogs > 300) return;
        try {
            bool cancel = VRInput.Cancel, menu = VRInput.Menu;
            if (cancel && !_cancelWas) I?.LoggerInstance.Msg("[btn] cancel edge (B) menuAlsoDown=" + menu);
            if (menu && !_menuWas) I?.LoggerInstance.Msg("[btn] menu edge cancelAlsoDown=" + cancel);
            _cancelWas = cancel;
            _menuWas = menu;
        } catch { }
    }

    // ---------- lock pick / cursor mode watch ----------
    // The lock pick is entirely native: the VR mod never mentions it. So what breaks
    // room selection afterwards has to be the cursor/panel state the mod is left in.
    // This logs that state whenever it changes, with no behaviour attached.

    private static AccessTools.FieldRef<VRPlugin, bool> _unlocked;
    private static AccessTools.FieldRef<VRPlugin, bool> _forcePanel;
    private static AccessTools.FieldRef<VRPlugin, bool> _draftMenu;
    private static AccessTools.FieldRef<VRPlugin, bool> _clickFreeze;
    private static AccessTools.FieldRef<VRPlugin, bool> _keyboardMode;
    private static AccessTools.FieldRef<VRPlugin, CursorLockMode> _savedLock;
    private static AccessTools.FieldRef<VRPlugin, GameObject> _uiQuad;
    private static string _lastModeLine;
    private static int _modeLogs;

    private void BindWatchFields()
    {
        _unlocked = SafeField<bool>("_unlocked");
        _forcePanel = SafeField<bool>("_forcePanel");
        _draftMenu = SafeField<bool>("_draftMenu");
        _clickFreeze = SafeField<bool>("_clickFreeze");
        _keyboardMode = SafeField<bool>("_computerKeyboardMode");
        _savedLock = SafeField<CursorLockMode>("_savedLock");
        _uiQuad = SafeField<GameObject>("_uiQuad");
    }

    private static void WatchCursorMode(VRPlugin instance)
    {
        if (_pWatchCursor == null || !_pWatchCursor.Value || _modeLogs > 300) return;
        try {
            string quad = "?";
            if (_uiQuad != null) {
                var go = _uiQuad(instance);
                quad = go == null ? "none" : (go.activeInHierarchy ? "active" : "off");
            }
            string line = "cursor=" + Cursor.lockState
                + " saved=" + (_savedLock == null ? "?" : _savedLock(instance).ToString())
                + " unlocked=" + Read(_unlocked, instance)
                + " forcePanel=" + Read(_forcePanel, instance)
                + " draftMenu=" + Read(_draftMenu, instance)
                + " clickFreeze=" + Read(_clickFreeze, instance)
                + " keyboardMode=" + Read(_keyboardMode, instance)
                + " uiQuad=" + quad;
            if (line == _lastModeLine) return;
            _lastModeLine = line;
            _modeLogs++;
            I?.LoggerInstance.Msg("[mode] " + line);
        } catch { }
    }

    private static string Read(AccessTools.FieldRef<VRPlugin, bool> field, VRPlugin instance)
    {
        try { return field == null ? "?" : field(instance).ToString(); } catch { return "?"; }
    }


    // ---------- reaching into crates ----------
    // VisibleHoverItem refuses a pickup when anything sits between palm and item.
    // It forgives the crate shell only while the Trunk Code FSM is in the single
    // state "Turn off Click Blocker", and forgives ground overlap by 0.12m for a
    // coin against 0.35m for a key or gem. Past that state the crate's own walls
    // block the line, so the hand has to go physically inside to reach the loot.
    // This re-runs the same test, forgiving the container the item is sitting in.

    private const float PickupHoverReach = 2.6f;

    private static Transform ContainerRoot(Transform from)
    {
        try {
            int depth = 0;
            for (var t = from; t != null && depth < 7; t = t.parent, depth++) {
                string n = t.gameObject.name;
                if (n.StartsWith("TRUNK(Clone)", StringComparison.OrdinalIgnoreCase)
                    || n.IndexOf("crate", StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("chest", StringComparison.OrdinalIgnoreCase) >= 0) return t;
            }
        } catch { }
        return null;
    }

    private static void VisiblePostfix(Collider item, Vector3 origin, ref RaycastHit hit, ref bool __result)
    {
        if (__result) return;
        if (_pCrateReach == null || !_pCrateReach.Value || item == null) return;
        try {
            Vector3 delta = item.bounds.center - origin;
            float distance = delta.magnitude;
            if (distance < .001f || distance > PickupHoverReach) return;

            Vector3 direction = delta / distance;
            Vector3 start = origin;
            if (item.bounds.Contains(start)) start = item.bounds.center - direction * (item.bounds.extents.magnitude + .02f);
            if (!item.Raycast(new Ray(start, direction), out var candidate, PickupHoverReach + .5f)) return;

            float reach = Vector3.Distance(origin, candidate.point);
            if (reach > PickupHoverReach) return;

            Transform container = ContainerRoot(item.transform);
            float allowance = Mathf.Clamp(_pCrateGroundAllowance == null ? .35f : _pCrateGroundAllowance.Value, 0f, 1f);

            foreach (var obstruction in Physics.RaycastAll(origin, direction, reach, -1, QueryTriggerInteraction.Ignore)) {
                var obstacle = obstruction.collider;
                if (obstacle == null || obstacle == item || obstacle.name.StartsWith("BPVR_")) continue;
                // The container the loot is sitting in is not a wall between us.
                if (container != null) {
                    try { if (obstacle.transform.IsChildOf(container)) continue; } catch { }
                }
                if (obstacle.name.IndexOf("terrain", StringComparison.OrdinalIgnoreCase) >= 0
                    && reach - obstruction.distance <= allowance) continue;
                return;
            }

            candidate.distance = reach;
            hit = candidate;
            __result = true;
            if (_crateLogs++ < 6) Say("[crate] reached '" + item.name + "' at " + reach.ToString("0.00") + "m through its own container");
        } catch { }
    }

    private static int _crateLogs;


    // ---------- point-to-grab for items ----------
    // Doors feel good because they use the aim/click path. Items go through the palm
    // path, whose gate is "within 0.45m horizontally of the palm AND at or below palm
    // height" (RoomPickupCandidate). Pointing at an item from a step away therefore
    // barely qualifies, which is why it takes several presses. This adds an aim
    // spherecast with a real hitbox, and only runs when the stock search found nothing.

    private static MethodInfo _isPickupTarget;
    private static bool _isPickupTargetStatic;
    private static int _pointGrabLogs;

    private static bool IsPickup(VRPlugin instance, Collider col)
    {
        if (_isPickupTarget == null || col == null) return false;
        try {
            object r = _isPickupTarget.Invoke(_isPickupTargetStatic ? null : instance, new object[] { col });
            return r is bool b && b;
        } catch { return false; }
    }

    private static void RoomPickupPostfix(VRPlugin __instance, Hand hand, Vector3 origin,
                                          ref RaycastHit hit, ref bool clickable, ref bool __result)
    {
        NoteHand(hand);
        if (__result) return;
        if (_pPointGrab == null || !_pPointGrab.Value || _isPickupTarget == null) return;
        try {
            if (hand == null || !hand.Valid) return;
            Vector3 direction = hand.AimDirection;
            if (direction.sqrMagnitude < 1e-6f) return;
            direction.Normalize();

            float radius = Mathf.Clamp(_pPointGrabRadius == null ? .15f : _pPointGrabRadius.Value, 0.01f, 1f);
            float reach = Mathf.Clamp(_pPointGrabReach == null ? 2.6f : _pPointGrabReach.Value, 0.2f, 6f);

            Collider best = null;
            RaycastHit bestHit = default;
            float bestScore = float.MaxValue;

            // SphereCastAll allocates a fresh array every call, and this runs for
            // both hands every frame the stock search comes up empty, which is most
            // of the time while walking. NonAlloc into a reusable buffer instead.
            int found = Physics.SphereCastNonAlloc(origin, radius, direction, _sweepBuffer, reach, -1, QueryTriggerInteraction.Collide);
            for (int si = 0; si < found; si++) {
                var sweep = _sweepBuffer[si];
                var col = sweep.collider;
                if (col == null || !col.enabled || !col.gameObject.activeInHierarchy) continue;
                if (!IsPickup(__instance, col)) continue;
                var pick = MousePick.For(col);
                if (pick == null || !pick.CanClick) continue;

                Vector3 toward = col.bounds.center - origin;
                float distance = toward.magnitude;
                if (distance < .001f || distance > reach) continue;
                // Prefer what is closest to the centre of where the hand points.
                float along = Vector3.Dot(toward, direction);
                if (along < 0f) continue;
                float off = (toward - direction * along).magnitude;
                float score = off + distance * .05f;
                if (score >= bestScore) continue;

                bestScore = score;
                best = col;
                bestHit = sweep;
            }

            if (best == null) return;

            bestHit.distance = Vector3.Distance(origin, best.bounds.center);
            hit = bestHit;
            clickable = true;
            __result = true;
            if (_pointGrabLogs++ < 8) Say("[point] item '" + best.name + "' at " + bestHit.distance.ToString("0.00") + "m via aim hitbox");
        } catch { }
    }


    // ---------- what counts as a pickup ----------
    // IsPickupName only matches COIN/APPLE/FRUIT/BANANA/ORANGE/GEM/DIAMOND/KEY/
    // DICE/DIE/PICKUP/COLLECTIBLE. So "Lock Pick" and most real inventory items are
    // not pickups at all and get none of the pickup assists. These are the item names
    // the game's own item-selection menu lists.
    private static readonly string[] ItemWords =
    {
        "LOCK PICK","LOCKPICK","BATTERY PACK","BURNING GLASS","COIN PURSE","LUCKY PURSE","COMPASS",
        "ORNATE COMPASS","COUPON BOOK","CURSED EFFIGY","DOWSING ROD","ELECTROMAGNET","GEAR WRENCH",
        "HALL PASS","JACK HAMMER","POWER HAMMER","SLEDGE HAMMER","LUNCH BOX","RABBIT","MAGNIFYING GLASS",
        "METAL DETECTOR","DETECTOR SHOVEL","SHOVEL","MICROCHIP","PAPER CROWN","AMPLIFIER","REPELLENT",
        "SCEPTER","RUNNING SHOES","SALT SHAKER","SLEEPING MASK","STOPWATCH","TELESCOPE","TREASURE MAP",
        "UPGRADE DISK","WATERING CAN","CHRONOGRAPH","BRACELET","PENDANT","SILVER SPOON","SHIELD",
        "MORNING STAR","TORCH","AXE","BROKEN LEVER","BROKEN LEVEL","KEYCARD","CUPCAKE"
    };

    private static void PickupNamePostfix(string name, ref bool __result)
    {
        if (__result) return;
        if (_pWidenPickups == null || !_pWidenPickups.Value) return;
        try {
            string n = (name ?? "").ToUpperInvariant();
            if (n.Length == 0) return;
            foreach (var word in ItemWords)
                if (n.Contains(word)) { __result = true; return; }
            string extra = _pExtraPickupNames?.Value;
            if (!string.IsNullOrEmpty(extra)) {
                foreach (var part in extra.Split('|')) {
                    string p = part.Trim().ToUpperInvariant();
                    if (p.Length > 0 && n.Contains(p)) { __result = true; return; }
                }
            }
        } catch { }
    }

    // The missing half of hold-to-drag. DirectGrab fires an instant OnClick and sets
    // DirectClickConsumed, which makes ClickFromTrigger bail before it ever reaches
    // the real held-mouse-button path. An instant click can never be a hold, so the
    // forklift could only ever grab and immediately let go. My side build fixed this
    // by editing that condition inline; from out here the same thing is achieved by
    // making DirectGrab decline these props, leaving the press to the mouse hold.
    private static bool DirectHandClickPrefix(Hand hand, ref bool __result)
    {
        NoteHand(hand);
        if (TookMainMenuClick(hand)) { __result = true; return false; }
        if (_pForkliftHold == null || !_pForkliftHold.Value) return true;
        try {
            if (hand == null || hand.IsLeft || hand.AimCollider == null) return true;
            if (!IsHoldDragProp(hand.AimCollider)) return true;
            if (_declineLogs++ < 6)
                Say("[forklift] DirectGrab declined '" + hand.AimCollider.name + "'; leaving it to the held mouse button");
            __result = false;
            return false;
        } catch { return true; }
    }

    private static int _declineLogs;

    // ---------- queued presses for anything that drops them ----------
    // One bug wearing three costumes. A trunk, a parlor box and the forklift all
    // fail the same way: the press arrives while the prop's FSM sits in a transient
    // state, its click transition is not live yet, and the press is discarded in
    // silence. Codex wrote a bespoke queue for trunks; this is the general form, so
    // props nobody has hit yet are covered too.
    //
    // Deliberately narrow: it only ever re-sends a press the player really made, at
    // the thing they were really aiming at, and only while they keep aiming at it.

    private static Collider _queuedTarget;
    private static float _queuedUntil;
    private static bool _triggerWasDown;
    private static int _queueLogs;

    private static bool ClickableNow(PickEvents pick)
    {
        if (pick == null) return false;
        try {
            if (pick.CanClick) return true;
            var fsm = pick.Fsm;
            if (fsm == null || !fsm.isActiveAndEnabled || fsm.Fsm.ActiveState == null) return false;
            foreach (var transition in fsm.Fsm.ActiveState.Transitions) {
                if (transition == null || string.IsNullOrEmpty(transition.ToState)) continue;
                string name = transition.EventName ?? "";
                if (name == "MOUSE DOWN" || name == "MOUSE UP AS BUTTON") return true;
                string lower = name.ToLowerInvariant();
                if (lower == "click" || lower == "onclick" || lower == "interact" || lower == "use") return true;
            }
        } catch { }
        return false;
    }

    // Queue a press only for props that take a click and are not already served by
    // another path: pickups have their own reach handling, and hold-drag props are
    // driven by the real mouse button.
    private static bool ShouldQueue(VRPlugin instance, Collider col, PickEvents pick)
    {
        if (col == null || pick == null) return false;
        try {
            if (string.IsNullOrEmpty(pick.Down)) return false;
            if (pick.Fsm == null) return false;
            if (IsPickup(instance, col)) return false;
            if (IsHoldDragProp(col)) return false;
        } catch { return false; }
        return true;
    }

    private static void QueuePress(VRPlugin instance, Hand h)
    {
        if (_pPressQueue == null || !_pPressQueue.Value) return;
        try {
            if (h == null || h.IsLeft) return;
            bool down = h.TriggerDown;
            bool edge = down && !_triggerWasDown;
            _triggerWasDown = down;
            if (!edge) return;

            var col = h.AimCollider;
            if (col == null) return;
            var pick = MousePick.For(col);
            if (!ShouldQueue(instance, col, pick)) return;
            if (ClickableNow(pick)) return;   // the normal path will take it

            _queuedTarget = col;
            _queuedUntil = Time.unscaledTime + Mathf.Clamp(_pPressQueueWindow == null ? 1f : _pPressQueueWindow.Value, .1f, 5f);
            if (_queueLogs++ < 20) Say("[queue] held your press on '" + col.name + "'; it was not accepting clicks yet");
        } catch { }
    }

    private static void DeliverQueuedPress(VRPlugin instance, Hand aiming)
    {
        if (_queuedTarget == null) return;
        try {
            if (Time.unscaledTime > _queuedUntil) {
                if (_queueLogs++ < 20) Say("[queue] press on '" + _queuedTarget.name + "' expired unused");
                _queuedTarget = null;
                return;
            }
            // Cancel if the hand has moved on, so a queued press can never fire at
            // something the player has already looked away from.
            if (aiming != null && aiming.AimCollider != null && aiming.AimCollider != _queuedTarget) {
                _queuedTarget = null;
                return;
            }

            var pick = MousePick.For(_queuedTarget);
            if (pick == null || pick.Fsm == null) { _queuedTarget = null; return; }
            if (!ClickableNow(pick)) return;

            string name = _queuedTarget.name;
            MousePick.Send(pick.Fsm, pick.Down);
            if (!string.IsNullOrEmpty(pick.Up)) MousePick.Send(pick.Fsm, pick.Up);
            _queuedTarget = null;
            if (_queueLogs++ < 20) Say("[queue] delivered your press to '" + name + "'");
        } catch { _queuedTarget = null; }
    }

    // ---------- computer view distance ----------
    // In computer mode Codex reprojects the native screen onto the panel at the
    // monitor's depth, sized from the native projection. When the game parks the
    // camera very close (Office, and after a room upgrade disk) that panel is huge
    // and the text runs off the edges. Sit it back and shrink it a little.

    private static void ComputerViewPostfix(VRPlugin __instance)
    {
        if (_pComputerPullback == null || !_pComputerPullback.Value) return;
        if (_keyboardMode == null || _uiQuad == null) return;
        try {
            if (!_keyboardMode(__instance)) return;
            var quad = _uiQuad(__instance);
            if (quad == null || !quad.activeInHierarchy) return;

            float shrink = Mathf.Clamp(_pComputerScale == null ? .78f : _pComputerScale.Value, 0.2f, 1f);
            float push = Mathf.Clamp(_pComputerPush == null ? .18f : _pComputerPush.Value, 0f, 1.5f);

            var t = quad.transform;
            t.localScale = new Vector3(t.localScale.x * shrink, t.localScale.y * shrink, t.localScale.z);
            t.position = t.position + t.forward * push;
        } catch { }
    }

    // ---------- keep the panel in front of you ----------
    // Codex aims the panel once, the frame a menu opens, and then holds that
    // aim for the life of the menu. When that instant is wrong - the menu
    // opened while you were facing the wheel, or your head was mid-turn, or
    // the pose had not settled - the panel is stranded and nothing recovers it.
    // Rather than keep guessing which of those it is, make it come back: hold
    // still while you are looking near it, and swing to you when you are not.

    private static int _followLogs;

    // ---------- VR SETTINGS on the title screen ----------
    // Codex's MainMenuVRSettings searches for a TextMeshProUGUI captioned
    // OPTIONS or SETTINGS inside a Canvas. This menu has no canvas text: the
    // rows are world-space TextMeshPro on MeshRenderers under MAINMENU LAYOUT,
    // which is why that entry has never appeared in any log. Clone a real row
    // instead, so it inherits the font, colour and size the game already uses,
    // then push QUIT GAME down by the one row gap the menu is built on.

    private static Collider _vrSettingsHit;
    private static float _menuRowCheckAt;
    private static int _rowLogs;
    private static MethodInfo _openVRSettings;

    private static GameObject FindMenuText(string name)
    {
        try {
            var go = GameObject.Find(name);
            return go != null && go.activeInHierarchy ? go : null;
        } catch { return null; }
    }

    private static void BuildMainMenuRow()
    {
        if (_pMainMenuVRSettings == null || !_pMainMenuVRSettings.Value) return;
        try {
            if (Time.unscaledTime < _menuRowCheckAt) return;
            _menuRowCheckAt = Time.unscaledTime + .5f;

            string scene = "";
            try { scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name ?? ""; } catch { }
            if (scene.IndexOf("Main Menu", StringComparison.OrdinalIgnoreCase) < 0) {
                _vrSettingsHint = null; _vrSettingsHit = null;
                return;
            }

            float gap = Mathf.Max(.01f, _pMainMenuRowGap == null ? .07f : _pMainMenuRowGap.Value);

            if (_vrSettingsHint == null) {
                var settings = FindMenuText("SETTINGS TEXT");
                var quit = FindMenuText("QUIT TEXT");
                if (settings == null || quit == null) return;
                BuildMainMenuHint(settings, quit, gap);
            }
        } catch (Exception e) {
            if (_rowLogs++ < 4) I?.LoggerInstance.Warning("[mainmenu] no caption under the title menu: " + e.Message);
        }
    }

    private static bool TookMainMenuClick(Hand hand)
    {
        if (_vrSettingsHit == null || hand == null || hand.AimCollider == null) return false;
        if (hand.AimCollider != _vrSettingsHit) return false;
        ToggleVRPage("the row was clicked");
        return true;
    }

    // A caption under QUIT GAME, not a row in the menu. The game's own layout
    // script owns which rows the selector can reach, so a row added from out here
    // can never be landed on - it only ever meant scrolling past a dead entry, and
    // the row below it is QUIT GAME. The menu is left exactly as the game built it
    // and the caption says which button opens the page.

    private static GameObject _vrSettingsHint;

    private static void BuildMainMenuHint(GameObject settings, GameObject quit, float gap)
    {
        if (_pMainMenuVRSettings == null || !_pMainMenuVRSettings.Value) return;
        try {
            var hint = UnityEngine.Object.Instantiate(settings);
            hint.name = "BPVR_VRSettingsHint";
            hint.transform.SetParent(settings.transform.parent, false);
            // one row gap below QUIT GAME, at the size the real rows are drawn, so
            // it reads as the next thing in the list rather than a footnote
            hint.transform.SetPositionAndRotation(
                new Vector3(settings.transform.position.x,
                            quit.transform.position.y - gap,
                            settings.transform.position.z),
                settings.transform.rotation);
            hint.transform.localScale = settings.transform.localScale
                * Mathf.Clamp(_pMainMenuCaptionSize == null ? 1f : _pMainMenuCaptionSize.Value, .3f, 1.5f);
            hint.layer = settings.layer;
            foreach (var box in hint.GetComponentsInChildren<Collider>(true))
                if (box != null) box.enabled = false;
            foreach (var label in hint.GetComponentsInChildren<Il2CppTMPro.TMP_Text>(true)) {
                if (label == null) continue;
                label.enableWordWrapping = false;
                label.text = (_pMainMenuCaption?.Value ?? "VR OPTIONS   [X]").Trim();
            }
            _vrSettingsHint = hint;
            if (_rowLogs++ < 4) Say("[mainmenu] caption added under QUIT GAME; the menu itself is untouched");
        } catch (Exception e) {
            if (_rowLogs++ < 4) I?.LoggerInstance.Warning("[mainmenu] no hint caption: " + e.Message);
        }
    }

    // ---------- using an item ----------
    // Two separate faults, one press. Pressing trigger-and-grip to use an item
    // sets VRInput.UseItemEdge for exactly one frame, and:
    //
    //   1. The game may never see it. ButtonDownFor("use item") returns that
    //      single-frame flag, and whether the game's own Update polls Rewired
    //      before or after a MelonLoader OnUpdate is not something either mod
    //      controls. Codex already hit this with interact and solved it there:
    //      InteractDownFrames=8 holds the press and TakeInteractPress consumes it
    //      on the first read. Use-item never got the same treatment, so the press
    //      falls in the gap and the lock pick does nothing.
    //
    //   2. The same frame flips the document stick pointer on, because
    //      UpdatePanelStickPointer toggles _magnifierPointer on UseItemEdge. That
    //      makes NativePanelMenu false, so the stick pointer takes the panel over
    //      and zeroes Select, Interact and all four Nav flags, and hand pointing
    //      goes off. The item menu is then on screen with nothing able to drive
    //      it - the cursor mode, and only B gets out of it.
    //
    // So: latch the press until the game reads it, and make the document pointer
    // want a deliberate hold rather than the tap that every item use is.

    private static MelonPreferences_Entry<bool> _pUseItemLatch;
    private static MelonPreferences_Entry<bool> _pMagnifierHold;
    private static MelonPreferences_Entry<float> _pMagnifierHoldSeconds;
    private static MelonPreferences_Entry<float> _pMagnifierIdleOff;

    private static bool _useLatch;
    private static int _useLatchFrame;
    private static int _useLogs;

    private static bool WantsUseItem(string id)
    {
        try {
            string want = VRInput.ActUseItem ?? "";
            return want.Length > 0 && string.Equals(want, id, StringComparison.OrdinalIgnoreCase);
        } catch { return false; }
    }

    // Codex's TakeInteractPress, for the one action that never had it.
    // How often the game asks for this button at all. If it only asks while the
    // item menu is up, that is a clean way to tell the item picker from a document
    // and the pointer could be blocked outright rather than put behind a hold.
    private static int _usePolls, _usePollReports;
    private static float _usePollWindow;

    private static void NoteUsePoll()
    {
        if (_usePollReports >= 3) return;
        _usePolls++;
        if (_usePollWindow == 0f) { _usePollWindow = Time.unscaledTime + 10f; return; }
        if (Time.unscaledTime < _usePollWindow) return;
        _usePollReports++;
        Say("[item] the game polled 'use item' " + _usePolls + " times in the last 10s");
        _usePolls = 0;
        _usePollWindow = Time.unscaledTime + 10f;
    }

    private static void ButtonDownPostfix(string id, ref bool value, ref bool __result)
    {
        if (_pUseItemLatch == null || !_pUseItemLatch.Value) return;
        try {
            if (!WantsUseItem(id)) return;
            NoteUsePoll();
            if (value) { _useLatch = false; return; }          // the game caught the real edge
            if (!_useLatch) return;
            if (Time.frameCount - _useLatchFrame > 8) { _useLatch = false; return; }
            value = true;
            __result = true;
            _useLatch = false;
            if (_useLogs++ < 12)
                Say("[item] use-item press handed over " + (Time.frameCount - _useLatchFrame)
                    + " frames late; without this the game polls Rewired before the edge exists and the press is lost");
        } catch { }
    }

    private static void LatchUseItem()
    {
        if (_pUseItemLatch == null || !_pUseItemLatch.Value) return;
        try {
            if (!VRInput.UseItemEdge) return;
            _useLatch = true;
            _useLatchFrame = Time.frameCount;
        } catch { }
    }

    // ---- the document pointer wants a hold, not a tap ----

    private static bool _useEdgeStashed, _magFired;
    private static float _magHeldSince = -1f;
    private static int _magLogs;

    private static bool GripGate => _pMagGripGate != null && _pMagGripGate.Value;
    private static bool HandMagnifier => _pMagnifierByHand != null && _pMagnifierByHand.Value;

    private static void PanelStickPrefix()
    {
        if (!GripGate && (_pMagnifierHold == null || !_pMagnifierHold.Value)) return;
        try {
            _useEdgeStashed = VRInput.UseItemEdge;
            if (GripGate) { VRInput.UseItemEdge = false; return; }
            if (!VRInput.UseItem) { _magHeldSince = -1f; _magFired = false; }

            // the tap belongs to the item, not to the pointer
            VRInput.UseItemEdge = false;

            if (!VRInput.UseItem) return;
            if (_magHeldSince < 0f) _magHeldSince = Time.unscaledTime;
            float need = Mathf.Clamp(_pMagnifierHoldSeconds == null ? .8f : _pMagnifierHoldSeconds.Value, .25f, 4f);
            if (_magFired || Time.unscaledTime - _magHeldSince < need) return;
            _magFired = true;
            VRInput.UseItemEdge = true;        // this one is deliberate
            if (_magLogs++ < 8) Say("[magnifier] trigger and grip held " + need.ToString("0.0") + "s; toggling the document pointer");
        } catch { }
    }

    private static void PanelStickPostfix()
    {
        if (!GripGate && (_pMagnifierHold == null || !_pMagnifierHold.Value)) return;
        try { VRInput.UseItemEdge = _useEdgeStashed; } catch { }
    }

    // ---- and it gives the panel back if it turns out not to be a document ----

    private static AccessTools.FieldRef<VRPlugin, bool> _magnifierRef;
    private static bool _magWas, _magClicked;
    private static float _magOnAt;

    private static void WatchMagnifier(VRPlugin vr)
    {
        if (GripGate) return;             // grip decides now, so nothing can get stuck on
        if (_magnifierRef == null || _pMagnifierIdleOff == null) return;
        try {
            if (!_magnifierRef(vr)) { _magWas = false; return; }
            if (!_magWas) { _magWas = true; _magOnAt = Time.unscaledTime; _magClicked = false; }
            if (_mouseHeld != null && _mouseHeld(vr)) _magClicked = true;

            float limit = _pMagnifierIdleOff.Value;
            if (limit <= 0f || _magClicked || Time.unscaledTime - _magOnAt < limit) return;
            _magnifierRef(vr) = false;
            _magWas = false;
            if (_magLogs++ < 8)
                Say("[magnifier] document pointer switched itself back off after " + limit.ToString("0")
                    + "s with nothing clicked; whatever is on screen is the game's to navigate again");
        } catch { }
    }

    // ---------- the title-screen VR settings page ----------
    // What the in-world comfort panel says, said the way the game says it: a
    // column of the menu's own rows, in the menu's own font, where the main menu
    // was. The game swaps MAINMENU LAYOUT off and SETTINGS LAYOUT on to show its
    // own settings; this does the same thing with a column of its own, so the
    // main menu cannot be navigated behind it - which also means a stray A press
    // can no longer land on QUIT GAME while this page has the player's attention.
    //
    // Navigation is the stick and A, like every other menu here. Pointing works
    // too when the hands happen to be live, but it is never relied on: the ray is
    // capped at four metres so a pose left over from the house cannot reach these
    // rows.

    private static MelonPreferences_Entry<bool> _pVRPage;
    private static MelonPreferences_Entry<string> _pMainMenuCaption;
    private static MelonPreferences_Entry<float> _pMainMenuCaptionSize;

    private static readonly string[] PageRows =
    {
        "turning", "angle", "hand", "haptics", "foveation", "recenter", "calibrate", "controls", "return"
    };

    private static bool _vrPageOpen, _vrPageControls;
    private static int _vrPageRow;
    private static GameObject _vrPageRoot;
    private static GameObject _mainLayout;
    private static readonly List<GameObject> _vrPageCells = new List<GameObject>();
    private static readonly List<Collider> _vrPageHits = new List<Collider>();
    private static readonly List<int> _vrPageIndex = new List<int>();
    private static Il2CppTMPro.TMP_Text _vrPageStatusLabel;
    private static SpriteRenderer _vrPageIcon;
    private static string _vrPageStatus = "";
    private static GameObject _pageTemplate;
    private static float _pageTopY;
    private static Color _vrPageBase = Color.white;
    private static readonly List<Il2CppTMPro.TMP_Text> _vrPageRowLabels = new List<Il2CppTMPro.TMP_Text>();
    private static bool _vrPageHandedOff;
    private static float _vrPadLogAt;
    private static int _vrPadLogs;
    private static int _vrPageLogs, _vrPagePointed = -1;
    private static bool _vrPageSelectWas, _vrPageCancelWas, _vrPageTriggerWas;
    private static int _vrPageNavWas;
    private static float _vrPageQuiet;

    // ---- the pieces of Codex's comfort state this page drives ----
    private static AccessTools.FieldRef<VRPlugin, bool> _comfortOpenRef;
    private static AccessTools.FieldRef<VRPlugin, bool> _journalBlockRef;
    private static AccessTools.FieldRef<VRPlugin, string> _comfortStatusRef;
    private static AccessTools.FieldRef<VRPlugin, Vector3> _originRef, _headPosRef;
    private static AccessTools.FieldRef<VRPlugin, Hand> _handLRef, _handRRef;
    private static FieldInfo _fComfort, _fNativeVrs, _fExtraVrs, _fFoveated, _fHaptics, _fVrsStatus;
    private static MethodInfo _saveComfortM, _initFoveationM;
    private static PropertyInfo _cpSnap, _cpLeft, _cpAngle;

    private static void BindComfortSettings(Type target)
    {
        _comfortOpenRef = SafeField<bool>("_comfortOpen");
        _journalBlockRef = SafeField<bool>("_journalBlockUntilRelease");
        _comfortStatusRef = SafeField<string>("_comfortStatus");
        _originRef = SafeField<Vector3>("_origin");
        _headPosRef = SafeField<Vector3>("_headPos");
        _handLRef = SafeField<Hand>("_handL");
        _handRRef = SafeField<Hand>("_handR");
        _fComfort = AccessTools.Field(target, "_comfort");
        _fNativeVrs = AccessTools.Field(target, "_pNativeVrs");
        _fExtraVrs = AccessTools.Field(target, "_pExtraStrongVrs");
        _fFoveated = AccessTools.Field(target, "_pFoveated");
        _fHaptics = AccessTools.Field(target, "_pHaptics");
        _fVrsStatus = AccessTools.Field(target, "_lastVrsStatus");
        _saveComfortM = AccessTools.Method(target, "SaveComfort");
        _initFoveationM = AccessTools.Method(target, "InitNativeFoveation");
        if (_fComfort == null || _comfortOpenRef == null)
            I?.LoggerInstance.Warning("[vr page] comfort state not found in this build of the VR mod; the title page will stay off");
    }

    // Read the pads the way Codex's own comfort menu reads them, straight off the
    // OpenVR function table, instead of through VRInput. VRInput is rewritten
    // several times a frame by the movement, panel and floor-plan code, and
    // whatever it holds by the time a postfix runs is not the stick: on the title
    // screen it left this page unable to move at all.

    private static FieldInfo _fSys;
    private static MethodInfo _controllerIndexM;

    private static bool ReadPad(bool left, out VRControllerState_t state)
    {
        state = default;
        try {
            if (_fSys == null) _fSys = AccessTools.Field(typeof(VRPlugin), "_sys");
            if (_fSys == null) return false;
            var sys = (IVRSystem_FnTable)_fSys.GetValue(null);
            if (sys.GetControllerState == null || sys.GetTrackedDeviceIndexForControllerRole == null) return false;

            var role = left ? ETrackedControllerRole.LeftHand : ETrackedControllerRole.RightHand;
            if (_controllerIndexM == null)
                _controllerIndexM = AccessTools.Method(typeof(VRPlugin), "ControllerIndex", new[] { typeof(ETrackedControllerRole) });

            uint index;
            // ControllerIndex is where Codex applies the dominant-hand swap, so go
            // through it rather than second-guessing which pad is which.
            if (_controllerIndexM != null && _vr != null)
                index = (uint)_controllerIndexM.Invoke(_vr, new object[] { role });
            else
                index = sys.GetTrackedDeviceIndexForControllerRole(role);
            if (index >= 64) return false;

            return sys.GetControllerState(index, ref state,
                (uint)System.Runtime.InteropServices.Marshal.SizeOf<VRControllerState_t>());
        } catch { return false; }
    }

    private static object Profile()
    {
        try {
            var p = _fComfort == null || _vr == null ? null : _fComfort.GetValue(_vr);
            if (p == null) return null;
            if (_cpSnap == null) {
                var t = p.GetType();
                _cpSnap = t.GetProperty("Snap");
                _cpLeft = t.GetProperty("LeftHanded");
                _cpAngle = t.GetProperty("SnapAngle");
            }
            return p;
        } catch { return null; }
    }

    private static bool Snap { get { var p = Profile(); return p != null && _cpSnap != null && (bool)_cpSnap.GetValue(p); } }
    private static bool LeftHanded { get { var p = Profile(); return p != null && _cpLeft != null && (bool)_cpLeft.GetValue(p); } }
    private static float SnapAngle { get { var p = Profile(); return p != null && _cpAngle != null ? (float)_cpAngle.GetValue(p) : 30f; } }

    private static void SaveProfile()
    {
        try { _saveComfortM?.Invoke(_vr, null); } catch (Exception e) { _vrPageStatus = "Settings not saved: " + e.Message; }
    }

    private static MelonPreferences_Entry<bool> Pref(FieldInfo field)
    {
        try { return field == null || _vr == null ? null : field.GetValue(_vr) as MelonPreferences_Entry<bool>; }
        catch { return null; }
    }

    private static string FoveationWord()
    {
        var native = Pref(_fNativeVrs);
        if (native == null || !native.Value) return "OFF";
        var extra = Pref(_fExtraVrs);
        return extra != null && extra.Value ? "EXTRA STRONG" : "NORMAL";
    }

    private static string RowLabel(int i)
    {
        switch (PageRows[i]) {
            case "turning":   return "TURNING: " + (Snap ? "SNAP" : "SMOOTH");
            case "angle":     return "SNAP ANGLE: " + Mathf.RoundToInt(SnapAngle);
            case "hand":      return "DOMINANT HAND: " + (LeftHanded ? "LEFT" : "RIGHT");
            case "haptics":   { var h = Pref(_fHaptics); return "HAPTICS: " + (h != null && h.Value ? "ON" : "OFF"); }
            case "foveation": return "FOVEATION: " + FoveationWord();
            case "recenter":  return "RE-CENTER VIEW";
            case "calibrate": return "WRIST CALIBRATION";
            case "controls":  return "VR CONTROLS";
            default:          return "RETURN";
        }
    }

    // One list, used by this page and by the in-world panel, so the two can never
    // drift apart. Written against what the mod actually reads: the sticks and the
    // face buttons in PollControllers, the physical-left X and Y in PhotoJournal
    // and FloorPlanInput, trigger-plus-grip in both hands, and the grips on a held
    // forklift.
    internal static string ControlsBlock()
    {
        return
            "MOVE - LEFT STICK\n" +
            "TURN - RIGHT STICK LEFT AND RIGHT\n" +
            "LOOK UP AND DOWN - RIGHT STICK\n" +
            "WALK SLOWLY - PRESS THE LEFT STICK\n" +
            "CROUCH - PRESS THE RIGHT STICK\n" +
            "INTERACT - TRIGGER, AT WHAT YOU POINT AT\n" +
            "PICK UP AND HOLD - GRIP\n" +
            "USE THE HELD ITEM - TRIGGER AND GRIP TOGETHER\n" +
            "JOURNAL - X, HELD FOR A QUICK MEMO\n" +
            "FLOOR PLAN - Y, HELD TO PAUSE\n" +
            "CHOOSE AND GO BACK - A AND B\n" +
            "FORKLIFT - HOLD THE TRIGGER TO DRIVE IT,\n" +
            "      LEFT GRIP RAISES, RIGHT GRIP LOWERS\n" +
            "THESE SETTINGS - PRESS AND HOLD BOTH STICKS,\n" +
            "      OR X ON THE TITLE SCREEN\n" +
            "\n" +
            "X AND Y STAY ON THE LEFT CONTROLLER EVEN WHEN\n" +
            "THE DOMINANT HAND IS SET TO LEFT; EVERYTHING\n" +
            "ELSE SWAPS WITH IT.";
    }

    private static void ToggleVRPage(string why)
    {
        if (_vrPageOpen) { CloseVRPage(); return; }
        if (_pVRPage != null && !_pVRPage.Value) {
            try { if (_openVRSettings == null) _openVRSettings = AccessTools.Method(typeof(VRPlugin), "OpenVRSettings"); _openVRSettings?.Invoke(_vr, null); } catch { }
            return;
        }
        if (_fComfort == null) return;
        _vrPageOpen = true;
        _vrPageControls = false;
        _vrPageRow = 0;
        _vrPageStatus = "";
        _vrPageQuiet = Time.unscaledTime + .25f;
        _vrPageSelectWas = _vrPageCancelWas = _vrPageTriggerWas = true;
        _vrPagePointed = -1;
        try { _mainLayout = GameObject.Find("MAINMENU LAYOUT"); } catch { _mainLayout = null; }
        BuildVRPage();
        if (_vrPageLogs++ < 8) Say("[vr page] opened on the title screen (" + why + ")");
    }

    private static void CloseVRPage()
    {
        _vrPageOpen = false;
        _vrPageControls = false;
        DestroyVRPage();
        try { if (_mainLayout != null) _mainLayout.SetActive(true); } catch { }
        _mainLayout = null;
        if (_vrPageLogs++ < 8) Say("[vr page] closed; the title menu is back");
    }

    private static void DestroyVRPage()
    {
        _vrPageCells.Clear();
        _vrPageHits.Clear();
        _vrPageRowLabels.Clear();
        _vrPageIndex.Clear();
        _vrPageStatusLabel = null;
        _vrPageIcon = null;
        try { if (_vrPageRoot != null) UnityEngine.Object.Destroy(_vrPageRoot); } catch { }
        _vrPageRoot = null;
    }

    // Every cell is a copy of a real menu row, so the face, the size, the colour
    // and the material are the menu's own rather than a guess at them.
    // The menu rows live in a column well left of the screen, which is right for a list
    // of short labels and wrong for a block of text: left-aligned inside a rect centred on
    // that column, the controls started almost a metre further left again and ran off the
    // side. Measure what was actually drawn and slide it back onto the menu's own centre.
    private static void CentreOnMenu(GameObject cell)
    {
        try {
            var renderer = cell.GetComponentInChildren<Renderer>(true);
            if (renderer == null) return;

            float target = float.NaN;
            foreach (var r in UnityEngine.Object.FindObjectsOfType<Renderer>()) {
                if (r == null) continue;
                string n = r.gameObject.name ?? "";
                if (!n.StartsWith("MAIN MENU BG", StringComparison.OrdinalIgnoreCase)) continue;
                target = r.bounds.center.x;
                break;
            }
            if (float.IsNaN(target)) return;

            var pos = cell.transform.position;
            cell.transform.position = new Vector3(pos.x + (target - renderer.bounds.center.x), pos.y, pos.z);
        } catch { }
    }

    private static GameObject PageCell(GameObject template, float y, float scale, string text, bool clickable)
    {
        var cell = UnityEngine.Object.Instantiate(template);
        cell.name = "BPVR_page_" + (_vrPageCells.Count);
        cell.transform.SetParent(_vrPageRoot.transform, false);
        cell.transform.SetPositionAndRotation(
            new Vector3(template.transform.position.x, y, template.transform.position.z),
            template.transform.rotation);
        cell.transform.localScale = template.transform.lossyScale * scale;
        cell.layer = template.layer;

        foreach (var label in cell.GetComponentsInChildren<Il2CppTMPro.TMP_Text>(true)) {
            if (label == null) continue;
            label.enableWordWrapping = false;
            label.overflowMode = Il2CppTMPro.TextOverflowModes.Overflow;
            label.text = text;
            // the mesh has to exist before the hit box is measured off it
            try { label.ForceMeshUpdate(false, false); } catch { }
        }

        foreach (var old in cell.GetComponentsInChildren<Collider>(true))
            if (old != null) UnityEngine.Object.Destroy(old);

        if (clickable) {
            var box = cell.AddComponent<BoxCollider>();
            box.isTrigger = true;
            var r = cell.GetComponentInChildren<Renderer>(true);
            if (r != null) {
                var size = cell.transform.InverseTransformVector(r.bounds.size);
                box.size = new Vector3(Mathf.Max(.2f, Mathf.Abs(size.x)), Mathf.Abs(size.y), Mathf.Max(.05f, Mathf.Abs(size.z)));
            }
            _vrPageHits.Add(box);
            _vrPageRowLabels.Add(cell.GetComponentInChildren<Il2CppTMPro.TMP_Text>(true));
        }
        _vrPageCells.Add(cell);
        return cell;
    }

    private static void BuildVRPage()
    {
        DestroyVRPage();
        try {
            // The template is captured while the title menu is still up, because
            // once this page hides MAINMENU LAYOUT nothing in it can be found by
            // name any more - and the page rebuilds itself on every switch.
            if (_pageTemplate == null) {
                _pageTemplate = FindMenuText("SETTINGS TEXT");
                var first = FindMenuText("CONTINUE TEXT") ?? _pageTemplate;
                if (first != null) _pageTopY = first.transform.position.y;
            }
            var settings = _pageTemplate;
            if (settings == null) { _vrPageOpen = false; return; }

            float gap = Mathf.Max(.01f, _pMainMenuRowGap == null ? .07f : _pMainMenuRowGap.Value);
            float y = _pageTopY;

            _vrPageRoot = new GameObject("BPVR_VRSettingsPage");
            _vrPageRoot.transform.position = Vector3.zero;
            _vrPageRoot.transform.rotation = Quaternion.identity;

            PageCell(settings, y, 1f, _vrPageControls ? "VR CONTROLS" : "VR OPTIONS", false);
            y -= gap * 1.6f;

            if (_vrPageControls) {
                var block = PageCell(settings, y - gap * 2.4f, .46f, ControlsBlock(), false);
                var line = block.GetComponentInChildren<Il2CppTMPro.TMP_Text>(true);
                if (line != null) line.alignment = Il2CppTMPro.TextAlignmentOptions.Left;
                CentreOnMenu(block);
                _vrPageIndex.Add(PageRows.Length - 1);
                PageCell(settings, y - gap * 5.6f, 1f, "RETURN", true);
            } else {
                for (int i = 0; i < PageRows.Length; i++) {
                    _vrPageIndex.Add(i);
                    PageCell(settings, y, 1f, RowLabel(i), true);
                    y -= gap;
                }
                var status = PageCell(settings, y - gap * .6f, .6f, _vrPageStatus, false);
                _vrPageStatusLabel = status.GetComponentInChildren<Il2CppTMPro.TMP_Text>(true);
                PageCell(settings, y - gap * 1.9f, .52f,
                    LeftHanded ? "LEFT STICK: MOVE   X: CHOOSE   Y: BACK"
                               : "LEFT STICK: MOVE   A: CHOOSE   B: BACK", false);
            }
            if (_vrPageRow >= _vrPageIndex.Count) _vrPageRow = 0;

            // one column for the crest, taken from the laid-out letters
            _vrPageIconLocalX = float.NaN;
            foreach (var label in _vrPageRowLabels) {
                if (!LineOf(label, out _, out _, out float left)) continue;
                if (float.IsNaN(_vrPageIconLocalX) || left < _vrPageIconLocalX) _vrPageIconLocalX = left;
            }
            var topRow = _vrPageRowLabels.Count > 0 ? _vrPageRowLabels[0] : null;
            if (topRow != null) _vrPageBase = topRow.color;
        } catch (Exception e) {
            if (_vrPageLogs++ < 4) I?.LoggerInstance.Warning("[vr page] could not be built: " + e.Message);
            _vrPageOpen = false;
            DestroyVRPage();
        }
    }

    private static void RefreshVRPage()
    {
        if (_vrPageControls) return;
        try {
            for (int i = 0; i < _vrPageIndex.Count && i + 1 < _vrPageCells.Count; i++) {
                var label = _vrPageCells[i + 1].GetComponentInChildren<Il2CppTMPro.TMP_Text>(true);
                if (label == null) continue;
                string want = RowLabel(_vrPageIndex[i]);
                if (label.text != want) label.text = want;
            }
            if (_vrPageStatusLabel != null && _vrPageStatusLabel.text != _vrPageStatus)
                _vrPageStatusLabel.text = _vrPageStatus;
        } catch { }
    }

    private static int PointedRow()
    {
        for (int pass = 0; pass < 2; pass++) {
            var h = pass == 0 ? _handRRef?.Invoke(_vr) : _handLRef?.Invoke(_vr);
            if (h == null || !h.Valid) continue;
            if (h.AimDirection.sqrMagnitude < .5f) continue;
            var ray = new Ray(h.AimOrigin, h.AimDirection);
            int best = -1; float nearest = 4f;
            for (int i = 0; i < _vrPageHits.Count; i++) {
                var c = _vrPageHits[i];
                if (c == null) continue;
                try {
                    if (c.Raycast(ray, out var hit, nearest) && hit.distance < nearest) { nearest = hit.distance; best = i; }
                } catch { }
            }
            if (best >= 0) return best;
        }
        return -1;
    }

    // The crest is measured off the glyphs, not off the renderer. A TMP object's
    // renderer bounds are its RECT - on these rows a wide, tall box with its left
    // edge far out past the text - which is why the first version of this came out
    // enormous and parked off to the left. textInfo.lineInfo is the laid-out line
    // itself: its height is the line, its extents are where the letters really
    // start. The crest is then parented to the row, so the row's own scale carries
    // it, and the final height is clamped so no measurement can ever blow it up
    // again.

    private static float _vrPageIconLocalX = float.NaN;

    private static bool LineOf(Il2CppTMPro.TMP_Text label, out float height, out float baseline, out float left)
    {
        height = baseline = left = 0f;
        try {
            if (label == null) return false;
            var info = label.textInfo;
            if (info == null || info.lineCount < 1) return false;
            var li = info.lineInfo[0];
            height = li.lineHeight;
            baseline = li.baseline;
            left = li.lineExtents.min.x;
            if (!float.IsFinite(height) || height <= 0f) return false;
            if (!float.IsFinite(baseline) || !float.IsFinite(left) || Mathf.Abs(left) > 50f) return false;
            return true;
        } catch { return false; }
    }

    private static void PlaceVRPageIcon()
    {
        if (_pMenuIconSelector != null && !_pMenuIconSelector.Value) return;
        if (_vrPageRow < 0 || _vrPageRow >= _vrPageRowLabels.Count) return;
        try {
            var tex = LoadArt("BPVR_select.png", ref _selectTex, ref _selectTexTried);
            if (tex == null) return;
            var label = _vrPageRowLabels[_vrPageRow];
            if (label == null) return;
            if (!LineOf(label, out float h, out float baseline, out float left)) {
                if (_vrPageIcon != null) _vrPageIcon.enabled = false;
                return;
            }

            if (_vrPageIcon == null) {
                var go = new GameObject("BPVR_PageSelector");
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(.5f, .5f), tex.height);
                sr.sortingOrder = 3000;
                EnsureSpriteMaterial(sr, tex);
                _vrPageIcon = sr;
            }

            // one column for every row, taken from whichever row starts furthest left
            float x = float.IsNaN(_vrPageIconLocalX) ? left : _vrPageIconLocalX;

            // a crest no taller than the row, whatever the measurement says
            float rowScale = Mathf.Abs(label.transform.lossyScale.y);
            if (rowScale < .0001f) rowScale = 1f;
            float world = Mathf.Clamp(h * rowScale * 1.1f, .014f, .045f);
            float local = world / rowScale;

            _vrPageIcon.transform.SetParent(label.transform, false);
            _vrPageIcon.gameObject.layer = label.gameObject.layer;
            _vrPageIcon.enabled = true;
            _vrPageIcon.transform.localPosition = new Vector3(x - local * .85f, baseline + local * .28f, -.02f);
            _vrPageIcon.transform.localRotation = Quaternion.identity;
            _vrPageIcon.transform.localScale = Vector3.one * local;
        } catch { }
    }

    // Belt and braces for "which row am I on": the live row is also drawn brighter
    // than the rest, so the answer does not depend on a sprite landing in the right
    // place.
    private static void HighlightVRPageRow()
    {
        try {
            for (int i = 0; i < _vrPageRowLabels.Count; i++) {
                var label = _vrPageRowLabels[i];
                if (label == null) continue;
                var want = i == _vrPageRow ? Color.Lerp(_vrPageBase, Color.white, .8f) : _vrPageBase;
                if (label.color != want) label.color = want;
            }
        } catch { }
    }

    private static void Activate(int action)
    {
        switch (PageRows[action]) {
            case "turning": {
                var p = Profile();
                if (p != null && _cpSnap != null) { _cpSnap.SetValue(p, !(bool)_cpSnap.GetValue(p)); SaveProfile(); }
                break;
            }
            case "angle": {
                var p = Profile();
                if (p != null && _cpAngle != null) {
                    float a = (float)_cpAngle.GetValue(p);
                    _cpAngle.SetValue(p, a < 40f ? 45f : a < 55f ? 60f : 30f);
                    SaveProfile();
                }
                _vrPageStatus = Snap ? "" : "ONLY USED WHEN TURNING IS SET TO SNAP.";
                break;
            }
            case "hand": {
                var p = Profile();
                if (p != null && _cpLeft != null) {
                    bool want = !(bool)_cpLeft.GetValue(p);
                    _cpLeft.SetValue(p, want);
                    SaveProfile();
                    if (_journalBlockRef != null && _vr != null) _journalBlockRef(_vr) = true;
                    _vrPageStatus = want ? "THE CONTROLLERS ARE SWAPPED. X AND Y STAY LEFT."
                                         : "THE CONTROLLERS ARE BACK THE USUAL WAY ROUND.";
                    BuildVRPage();
                }
                break;
            }
            case "haptics": {
                var h = Pref(_fHaptics);
                if (h != null) { h.Value = !h.Value; MelonPreferences.Save(); }
                break;
            }
            case "foveation": {
                var native = Pref(_fNativeVrs);
                if (native == null) { try { _initFoveationM?.Invoke(_vr, null); } catch { } native = Pref(_fNativeVrs); }
                var extra = Pref(_fExtraVrs);
                var soft = Pref(_fFoveated);
                if (native == null) { _vrPageStatus = "FOVEATION IS NOT AVAILABLE IN THIS BUILD."; break; }
                if (!native.Value) { native.Value = true; if (extra != null) extra.Value = false; }
                else if (extra != null && !extra.Value) extra.Value = true;
                else { native.Value = false; if (extra != null) extra.Value = false; }
                if (soft != null) soft.Value = false;
                MelonPreferences.Save();
                _vrPageStatus = !native.Value ? "FULL-RATE SHADING."
                    : extra != null && extra.Value ? "SOFTER OUTER VIEW. THE GAIN VARIES BY SCENE."
                    : "NVIDIA D3D11 ONLY. FIXED SHADING, NOT EYE TRACKING.";
                break;
            }
            case "recenter": {
                if (_originRef != null && _headPosRef != null && _vr != null) {
                    _originRef(_vr) = _headPosRef(_vr);
                    _vrPageStatus = "VIEW RE-CENTERED.";
                } else _vrPageStatus = "THE VIEW CANNOT BE RE-CENTERED FROM HERE.";
                break;
            }
            case "calibrate": {
                // Codex owns the T-pose measurement; hand the player to it rather
                // than keep a second copy of the pose test out here.
                if (_comfortOpenRef != null && _comfortCalibrating != null && _vr != null) {
                    CloseVRPage();
                    _vrPageHandedOff = true;
                    _comfortOpenRef(_vr) = true;
                    _comfortCalibrating(_vr) = true;
                    if (_comfortStatusRef != null) _comfortStatusRef(_vr) = "";
                    if (_vrPageLogs++ < 8) Say("[vr page] handed over to Codex's wrist calibration");
                }
                break;
            }
            case "controls": {
                _vrPageControls = true;
                _vrPageRow = 0;
                BuildVRPage();
                break;
            }
            default: {
                if (_vrPageControls) { _vrPageControls = false; _vrPageRow = PageRows.Length - 1; BuildVRPage(); }
                else CloseVRPage();
                break;
            }
        }
    }

    private static void UpdateVRPage()
    {
        if (_pVRPage != null && !_pVRPage.Value) return;
        if (_vr == null || _comfortOpenRef == null) return;
        try {
            string scene = "";
            try { scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name ?? ""; } catch { }
            bool onMenu = scene.IndexOf("Main Menu", StringComparison.OrdinalIgnoreCase) >= 0;

            if (!onMenu) {
                if (_vrPageOpen || _pageTemplate != null) {
                    _vrPageOpen = false; _vrPageControls = false;
                    DestroyVRPage();
                    _mainLayout = null; _pageTemplate = null;
                }
                return;
            }

            // Codex's own two ways in - X on the title screen, or both sticks held -
            // both just raise _comfortOpen. Take that as the request and answer it
            // with this page instead of the panel that floats in front of the player.
            if (!_comfortOpenRef(_vr)) _vrPageHandedOff = false;
            else if (!_vrPageHandedOff) {
                _comfortOpenRef(_vr) = false;
                if (_comfortCalibrating != null) _comfortCalibrating(_vr) = false;
                ToggleVRPage("X or both sticks");
            }

            if (!_vrPageOpen) return;
            if (_vrPageRoot == null) { BuildVRPage(); if (!_vrPageOpen) return; }

            // the main menu stays down for as long as this page is up, however
            // insistently the game's own layout wants it back
            try { if (_mainLayout != null && _mainLayout.activeSelf) _mainLayout.SetActive(false); } catch { }

            RefreshVRPage();
            HighlightVRPageRow();

            bool quiet = Time.unscaledTime < _vrPageQuiet;

            bool padL = ReadPad(true, out var ls), padR = ReadPad(false, out var rs);
            bool select, cancel, trigger;
            int nav;
            if (padL || padR) {
                nav = padL && Mathf.Abs(ls.rAxis0.y) > .65f ? (int)Mathf.Sign(ls.rAxis0.y) : 0;
                select = padR && (rs.ulButtonPressed & 0x80UL) != 0;
                cancel = padR && (rs.ulButtonPressed & 2UL) != 0;
                trigger = padR && ((rs.ulButtonPressed & 0x200000000UL) != 0 || rs.rAxis1.x > .6f);
            } else {
                // the function table was not reachable; VRInput is better than nothing
                nav = Mathf.Abs(VRInput.MoveY) > .65f ? (int)Mathf.Sign(VRInput.MoveY) : 0;
                select = VRInput.Select;
                cancel = VRInput.Cancel;
                var hand = _handRRef?.Invoke(_vr);
                trigger = hand != null && hand.Valid && hand.TriggerDown;
            }

            if (Time.unscaledTime >= _vrPadLogAt && _vrPadLogs++ < 12) {
                _vrPadLogAt = Time.unscaledTime + .75f;
                Say("[vr page] pads L=" + padL + " y=" + (padL ? ls.rAxis0.y.ToString("0.00") : "-")
                    + " R=" + padR + " A=" + select + " B=" + cancel + " trig=" + trigger
                    + " row=" + _vrPageRow + "/" + _vrPageIndex.Count);
            }

            int pointed = PointedRow();
            if (pointed >= 0 && pointed != _vrPagePointed) _vrPageRow = pointed;
            _vrPagePointed = pointed;

            if (nav != 0 && nav != _vrPageNavWas && _vrPageIndex.Count > 0)
                _vrPageRow = (_vrPageRow - nav + _vrPageIndex.Count) % _vrPageIndex.Count;
            _vrPageNavWas = nav;

            bool chose = (select && !_vrPageSelectWas) || (trigger && !_vrPageTriggerWas && pointed >= 0);
            bool back = cancel && !_vrPageCancelWas;
            _vrPageSelectWas = select;
            _vrPageCancelWas = cancel;
            _vrPageTriggerWas = trigger;

            PlaceVRPageIcon();
            if (quiet) return;

            if (back) {
                if (_vrPageControls) { _vrPageControls = false; _vrPageRow = PageRows.Length - 1; BuildVRPage(); }
                else CloseVRPage();
                return;
            }
            if (chose && _vrPageRow >= 0 && _vrPageRow < _vrPageIndex.Count) {
                int action = _vrPageIndex[_vrPageRow];
                if (_vrPageLogs++ < 40) Say("[vr page] chose '" + RowLabel(action) + "'");
                Activate(action);
            }
        } catch (Exception e) {
            if (_vrPageLogs++ < 4) I?.LoggerInstance.Warning("[vr page] " + e.Message);
            CloseVRPage();
        }
    }

    // ---------- the VR settings menu ----------
    // It is built as one TextMeshPro block with no font set, so it renders in
    // the default face rather than anything the game uses, and the selected row
    // is marked with a bare "> ". Give it the game's serif, drop the arrow, and
    // park the crest beside whichever row is live. The row's position comes from
    // TMP's own line layout rather than a guess at line height.

    private static Texture2D _selectTex;
    private static bool _selectTexTried;
    private static SpriteRenderer _selectIcon;
    private static bool _comfortFontSet;
    private static int _menuLogs;

    // The in-world panel gets the same two changes the title page has: the row
    // that only ever printed a paragraph about reprojection - which the mod does
    // not control and cannot change - becomes the control list instead. Codex's
    // row actions are written inline in ComfortInput and cannot be reached from
    // out here, so this works with what that row already does: it sets a status
    // string, and a status string is something a postfix can read. Selecting it
    // shows the controls; moving the stick clears it and the menu comes back.

    private const string ReprojMsg = "Reprojection is controlled outside the mod";
    private const string ReprojRow = "Reprojection: runtime-controlled (info)";
    private static MelonPreferences_Entry<bool> _pMenuControls;
    private static bool _ctrlPage;
    private static int _ctrlRowWas = -1;

    private static void ShowComfortControls(VRPlugin vr, Il2CppTMPro.TMP_Text text)
    {
        try {
            int row = _comfortRow == null ? -1 : _comfortRow(vr);
            string status = _comfortStatusRef == null ? "" : (_comfortStatusRef(vr) ?? "");
            bool showing = status.StartsWith(ReprojMsg, StringComparison.Ordinal);

            if (showing && _ctrlRowWas >= 0 && row != _ctrlRowWas) {
                if (_comfortStatusRef != null) _comfortStatusRef(vr) = "";
                showing = false;
            }

            if (showing) {
                _ctrlRowWas = row;
                _ctrlPage = true;
                string want = "VR CONTROLS\n\n" + ControlsBlock() + "\n\nOff-hand stick: back";
                if (text.text != want) { text.text = want; text.ForceMeshUpdate(false, false); }
                if (_selectIcon != null) _selectIcon.enabled = false;
                PlaceControlsPage(vr, text);
                return;
            }

            RestoreComfortPanel(text);
            _ctrlPage = false;
            _ctrlRowWas = -1;
            string body = text.text ?? "";
            string fixed_ = body;
            if (fixed_.IndexOf(ReprojRow, StringComparison.Ordinal) >= 0)
                fixed_ = fixed_.Replace(ReprojRow, "VR controls");

            // one name for one feature, wherever it is opened from
            if (fixed_.StartsWith("VR SETTINGS", StringComparison.Ordinal))
                fixed_ = "VR OPTIONS" + fixed_.Substring("VR SETTINGS".Length);
            if (fixed_ != body) { text.text = fixed_; text.ForceMeshUpdate(false, false); }
        } catch { _ctrlPage = false; }
    }

    // Codex puts the comfort panel 1.4m out along the PLAYSPACE yaw, which is where your
    // body faces, not where you are looking. For its short status lines that is fine. The
    // controls list is a dense block, so at that distance and off to the side it is
    // unreadable. While that page is up, pin it to the head and give it room.
    private static bool _ctrlPanelSaved;
    private static Vector2 _ctrlSizeWas;
    private static Vector3 _ctrlScaleWas;

    private static void PlaceControlsPage(VRPlugin vr, Il2CppTMPro.TMP_Text text)
    {
        try {
            if (!_ctrlPanelSaved) {
                _ctrlPanelSaved = true;
                _ctrlSizeWas = text.rectTransform.sizeDelta;
                _ctrlScaleWas = text.transform.localScale;
            }
            text.rectTransform.sizeDelta = new Vector2(2.8f, 2.2f);
            text.transform.localScale = Vector3.one * .42f;
            text.alignment = Il2CppTMPro.TextAlignmentOptions.Center;

            Camera cam = null;
            foreach (var c in Camera.allCameras) {
                if (c == null || !c.isActiveAndEnabled) continue;
                if ((c.name ?? "").Equals("FirstPersonCharacter", StringComparison.OrdinalIgnoreCase)) { cam = c; break; }
            }
            if (cam == null) return;
            var body = Quaternion.Euler(0f, cam.transform.eulerAngles.y, 0f);
            float comfort = 0f;
            try {
                if (_comfortYawRef == null) _comfortYawRef = SafeField<float>("_comfortYaw");
                comfort = _comfortYawRef == null ? 0f : _comfortYawRef(vr);
            } catch { }
            var hr = _headRot != null ? _headRot(vr) : Quaternion.identity;
            var world = body * Quaternion.Euler(0f, comfort, 0f) * hr;

            // Flatten the pitch so looking down does not tip the page away from you.
            Vector3 f = world * Vector3.forward;
            Vector3 flat = new Vector3(f.x, 0f, f.z);
            var facing = flat.sqrMagnitude < 1e-4f ? world : Quaternion.LookRotation(flat.normalized, Vector3.up);

            text.transform.SetPositionAndRotation(cam.transform.position + facing * new Vector3(0f, -.05f, .85f), facing);
        } catch { }
    }

    private static void RestoreComfortPanel(Il2CppTMPro.TMP_Text text)
    {
        if (!_ctrlPanelSaved) return;
        _ctrlPanelSaved = false;
        try {
            text.rectTransform.sizeDelta = _ctrlSizeWas;
            text.transform.localScale = _ctrlScaleWas;
        } catch { }
    }

    private static void ComfortFramePostfix(VRPlugin __instance)
    {
        if (_comfortText == null) return;
        try {
            var text = _comfortText(__instance);
            if (text == null || !text.gameObject.activeInHierarchy) {
                if (_selectIcon != null) _selectIcon.enabled = false;
                return;
            }

            if (_pMenuGameFont != null && _pMenuGameFont.Value && !_comfortFontSet) {
                var font = FindTitleFont();
                if (font != null) {
                    text.font = font;
                    _comfortFontSet = true;
                    if (_menuLogs++ < 4) Say("[menu] VR settings now drawn in '" + (font.name ?? "?") + "'");
                }
            }

            bool calib = _comfortCalibrating != null && _comfortCalibrating(__instance);
            if (!calib && (_pMenuControls == null || _pMenuControls.Value)) {
                ShowComfortControls(__instance, text);
                if (_ctrlPage) return;
            }

            if (_pMenuIconSelector == null || !_pMenuIconSelector.Value) return;

            bool calibrating = calib;
            string body = text.text ?? "";
            if (calibrating || body.IndexOf("> ", StringComparison.Ordinal) < 0 && _selectIcon == null) {
                if (_selectIcon != null) _selectIcon.enabled = false;
                if (calibrating) return;
            }

            // the arrow goes; the crest takes its place
            if (body.Contains("\n> ")) {
                text.text = body.Replace("\n> ", "\n  ");
                text.ForceMeshUpdate(false, false);
            }

            var tex = LoadArt("BPVR_select.png", ref _selectTex, ref _selectTexTried);
            if (tex == null) return;

            if (_selectIcon == null) {
                var go = new GameObject("BPVR_MenuSelector");
                go.transform.SetParent(text.transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(.5f, .5f), tex.height);
                sr.sortingOrder = 3000;
                EnsureSpriteMaterial(sr, tex);
                _selectIcon = sr;
            }

            // row r sits on line 2 + r: line 0 is the heading, line 1 is blank
            int row = _comfortRow == null ? 0 : _comfortRow(__instance);
            var info = text.textInfo;
            int line = 2 + row;
            if (info == null || line >= info.lineCount) { _selectIcon.enabled = false; return; }

            var li = info.lineInfo[line];
            float height = Mathf.Max(.02f, li.lineHeight);
            float y = li.baseline + height * .22f;
            float x = li.lineExtents.min.x - height * .75f;

            _selectIcon.enabled = true;
            _selectIcon.transform.localPosition = new Vector3(x, y, -.002f);
            _selectIcon.transform.localScale = Vector3.one * (height * 1.25f);
        } catch (Exception e) {
            if (_menuLogs++ < 4) I?.LoggerInstance.Warning("[menu] selector failed: " + e.Message);
        }
    }

    // ---------- forklift: grips raise and lower the forks ----------
    // Nothing drives the forks at all in this build. The settings that used to
    // (DragLiftDrive, DragLiftSpeed) are orphans in the config, left behind
    // when the Oct 6 rebuild took the in-tree version with it. Codex already
    // owns a way to hold a key down for the game, SetComputerKey, which is how
    // the sticks drive menu arrows; the same route sends up and down for as
    // long as a grip is held, and releases the instant it is not.

    private static Hand _handL, _handR;
    private static int _liftLogs;
    private static float _liftDiagNext;
    private static bool _liftIdleSaid;
    private static MouseDrag _dragHeld;
    private static bool _liftWasDriving;

    private static void NoteHand(Hand hand)
    {
        if (hand == null) return;
        if (hand.IsLeft) _handL = hand; else _handR = hand;
    }

    // The drag that is actually live. The forklift's MouseDrag is not on the
    // handle; it sits on a control object, so it is found by state rather than
    // by what the hand is pointing at.
    private static MouseDrag ActiveDrag()
    {
        try {
            foreach (var item in Resources.FindObjectsOfTypeAll(Il2CppInterop.Runtime.Il2CppType.Of<MouseDrag>())) {
                var drag = item == null ? null : item.TryCast<MouseDrag>();
                if (drag == null) continue;
                try { if (drag.IsDragging) return drag; } catch { }
            }
        } catch { }
        return null;
    }

    private static void UpdateForkliftLift(VRPlugin instance)
    {
        if (_pForkliftLift == null || !_pForkliftLift.Value) return;
        try {
            if (!_holding) { _dragHeld = null; _liftWasDriving = false; _liftIdleSaid = false; return; }
            if (_dragHeld == null) _dragHeld = ActiveDrag();
            if (_dragHeld == null) {
                // Holding something the game is not treating as a drag: the pallet jack
                // behaving unlike the forklift would show up exactly here.
                if (!_liftIdleSaid && Time.unscaledTime >= _liftDiagNext) {
                    _liftIdleSaid = true;
                    _liftDiagNext = Time.unscaledTime + 1f;
                    if (_liftLogs++ < 40) Say("[forklift] holding something, but no MouseDrag is active to lift");
                }
                return;
            }

            // Hand.GripDown is not dependable in every state, as the terminal keyboard
            // found out the hard way, so ask the pad and keep the flags as a fallback.
            bool padL = ReadPad(true,  out var lps) && (lps.ulButtonPressed & 4UL) != 0;
            bool padR = ReadPad(false, out var rps) && (rps.ulButtonPressed & 4UL) != 0;
            bool leftGrip  = padL || (_handL != null && _handL.Valid && _handL.GripDown);
            bool rightGrip = padR || (_handR != null && _handR.Valid && _handR.GripDown);

            if (Time.unscaledTime >= _liftDiagNext) {
                _liftDiagNext = Time.unscaledTime + 1f;
                if (_liftLogs < 40) {
                    _liftLogs++;
                    Say("[forklift] holding, drag='" + (_dragHeld == null ? "none" : _dragHeld.name)
                        + "' gripL=" + leftGrip + " (pad " + padL + ") gripR=" + rightGrip + " (pad " + padR + ")");
                }
            }
            bool swapped = _pGripsSwapped != null && _pGripsSwapped.Value;

            float dir = 0f;
            if (swapped) { if (rightGrip) dir += 1f; if (leftGrip) dir -= 1f; }
            else         { if (leftGrip)  dir += 1f; if (rightGrip) dir -= 1f; }

            if (dir == 0f) {
                if (_liftWasDriving) { _liftWasDriving = false; if (_liftLogs++ < 8) Say("[forklift] forks stopped"); }
                return;
            }

            float speed = _pDragLiftSpeed == null ? 260f : _pDragLiftSpeed.Value;
            float delta = dir * speed * Mathf.Min(Time.unscaledDeltaTime, .05f);
            string drive = (_pDragLiftDrive?.Value ?? "screendelta").Trim();

            if (string.Equals(drive, "distance", StringComparison.OrdinalIgnoreCase))
                _dragHeld._dragDistance += delta * .01f;
            else
                _dragHeld._screenPosDelta = new Vector2(0f, delta);

            if (!_liftWasDriving) {
                _liftWasDriving = true;
                if (_liftLogs++ < 8) Say("[forklift] forks " + (dir > 0f ? "rising" : "lowering") + " via " + drive);
            }
        } catch (Exception e) {
            if (_liftLogs++ < 4) I?.LoggerInstance.Warning("[forklift] lift drive failed: " + e.Message);
        }
    }

    // ---------- the recorder grip ----------
    // Codex built this with the digits the other way round. RestThumbOnRecorder
    // aims the THUMB at the record button:
    //
    //     var target=recorder.TransformPoint(RecorderButtonPosition(left,press)+...);
    //     thumbBase.rotation=Quaternion.RotateTowards(identity,delta,65f)*thumbBase.rotation;
    //
    // while the index sits in the supporting row with the other three. Alex
    // wants the index on the button and the thumb wrapped around the casing,
    // so this runs last and reassigns the two. Both halves already existed;
    // only which finger gets which job changes.
    //
    // The index is solved, not authored: the button's own transform is the
    // target, so the pad lands on it rather than near it. Aiming is clamped
    // per step and only the knuckle swings, following Codex's rule that a free
    // distal joint will happily bend the finger backward.

    private static Type _celType;
    private static FieldInfo _fThumbJoints, _fThumbTips, _fPoses, _fNodes, _fAlign, _fWrists, _fOuterBases, _fFingerEnds;
    private static MethodInfo _mButtonPos, _mButtonNormal;
    private static bool _gripBound, _gripBroken;
    private static int _gripLogs;

    private static bool BindGrip(object pack)
    {
        if (_gripBroken) return false;
        if (_gripBound) return true;
        try {
            _celType = pack.GetType();
            _fThumbJoints = AccessTools.Field(_celType, "_thumbJoints");
            _fThumbTips   = AccessTools.Field(_celType, "_thumbTips");
            _fPoses       = AccessTools.Field(_celType, "_poses");
            _fNodes       = AccessTools.Field(_celType, "_nodes");
            _fAlign       = AccessTools.Field(_celType, "_alignment");
            _fWrists      = AccessTools.Field(_celType, "Wrists");
            _fOuterBases  = AccessTools.Field(_celType, "_recorderOuterBases");
            _fFingerEnds  = AccessTools.Field(_celType, "_recorderFingerEnds");
            _mButtonPos    = AccessTools.Method(_celType, "RecorderButtonPosition");
            _mButtonNormal = AccessTools.Method(_celType, "RecorderButtonNormal");
            _gripBound = _fThumbJoints != null && _fPoses != null && _fNodes != null && _fAlign != null
                      && _fWrists != null && _fOuterBases != null && _fFingerEnds != null;
            if (!_gripBound) { _gripBroken = true; I?.LoggerInstance.Warning("[grip] cel hand internals moved; recorder grip left as Codex has it"); }
            return _gripBound;
        } catch (Exception e) {
            _gripBroken = true;
            I?.LoggerInstance.Warning("[grip] could not read the cel hand pack: " + e.Message);
            return false;
        }
    }

    private static void SetBlend(Transform bone, Transform[] nodes, Quaternion[] hold, Quaternion[] fist, float amount)
    {
        if (bone == null) return;
        int index = Array.IndexOf(nodes, bone);
        if (index < 0 || hold == null || fist == null || index >= hold.Length || index >= fist.Length) return;
        bone.localRotation = Quaternion.Slerp(hold[index], fist[index], Mathf.Clamp01(amount));
    }

    // A finger that is blended to a fixed curl and then aimed will always miss:
    // FromToRotation only turns the direction, never the length, so a finger too
    // long for its target drives its tip straight through the casing. That is the
    // "fused together" in the screenshots, and the 40mm the log kept reporting was
    // the overshoot, not a shortfall.
    //
    // So curl first, aim second: walk the curl until the tip sits the right
    // DISTANCE from the knuckle, then swing the knuckle to put it on the target.
    // The finger then lands on the casing instead of inside it.
    private static float FitFinger(Transform j1, Transform j3, Transform[] nodes,
                                   Quaternion[] hold, Quaternion[] fist,
                                   Vector3 target, float tipScale, float limit,
                                   float r1, float r2, float r3, float maxCurl)
    {
        if (j1 == null || j3 == null) return -1f;
        var j2 = j3.parent;

        void Curl(float c)
        {
            SetBlend(j1, nodes, hold, fist, Mathf.Clamp01(c * r1));
            if (j2 != null) SetBlend(j2, nodes, hold, fist, Mathf.Clamp01(c * r2));
            SetBlend(j3, nodes, hold, fist, Mathf.Clamp01(c * r3));
        }
        Vector3 Tip()
        {
            Vector3 along = j2 != null ? (j3.position - j2.position) : j3.forward;
            if (along.sqrMagnitude < 1e-10f) return j3.position;
            return j3.position + along.normalized * (along.magnitude * tipScale);
        }

        float want = Vector3.Distance(target, j1.position);
        float best = 0f, bestErr = float.MaxValue;
        const int steps = 10;
        for (int i = 0; i <= steps; i++) {
            float c = maxCurl * i / steps;
            Curl(c);
            float err = Mathf.Abs(Vector3.Distance(Tip(), j1.position) - want);
            if (err < bestErr) { bestErr = err; best = c; }
        }
        Curl(best);
        // Out of reach even at full curl: leave the authored pose alone rather
        // than wrench the knuckle toward something the finger cannot touch.
        if (bestErr > .025f) return -bestErr;

        for (int pass = 0; pass < 2; pass++) {
            Vector3 from = Tip() - j1.position;
            Vector3 to = target - j1.position;
            if (from.sqrMagnitude < 1e-10f || to.sqrMagnitude < 1e-10f) break;
            j1.rotation = Quaternion.RotateTowards(Quaternion.identity,
                              Quaternion.FromToRotation(from, to), limit) * j1.rotation;
        }
        return Vector3.Distance(Tip(), target);
    }

    private static void RecorderGripPostfix(object __instance, Transform recorder, bool left, float press)
    {
        if (_pIndexOnButton == null || !_pIndexOnButton.Value) return;
        if (__instance == null || recorder == null) return;
        if (!BindGrip(__instance)) return;
        try {
            int side = left ? 0 : 1;
            var nodes   = (Transform[])_fNodes.GetValue(__instance);
            var poses   = _fPoses.GetValue(__instance) as System.Collections.Generic.Dictionary<string, Quaternion[]>;
            var thumbs  = (Transform[,])_fThumbJoints.GetValue(__instance);
            var bases   = (Transform[,])_fOuterBases.GetValue(__instance);
            var ends    = (Transform[,])_fFingerEnds.GetValue(__instance);
            var wrists  = (Transform[])_fWrists.GetValue(__instance);
            var align   = (Quaternion[])_fAlign.GetValue(__instance);
            if (nodes == null || poses == null || thumbs == null || bases == null || ends == null) return;
            if (!poses.TryGetValue("PoseHoldFlashlight", out var hold) || !poses.TryGetValue("PoseFist", out var fist)) return;

            // Nothing can be solved until the casing has been measured.
            SeatRecordKey(recorder);
            if (!_keyKnown) return;

            float limit = Mathf.Clamp(_pIndexAimLimit == null ? 55f : _pIndexAimLimit.Value, 5f, 120f);
            float tipScale = Mathf.Clamp(_pIndexTipScale == null ? .85f : _pIndexTipScale.Value, 0f, 2f);
            float skin = Mathf.Clamp(_pGripSkin == null ? .008f : _pGripSkin.Value, 0f, .05f);

            // Local helpers: where a finger's pad actually is, and how far that is
            // outside the casing. Negative clearance is the pad sunk into it.
            Func<Transform, Vector3> Pad = end3 => {
                var mid = end3.parent;
                Vector3 along = mid != null ? (end3.position - mid.position) : end3.forward;
                return end3.position + (along.sqrMagnitude > 1e-10f
                    ? along.normalized * (along.magnitude * tipScale) : Vector3.zero);
            };
            Func<Transform, float> Clearance = end3 => {
                var l = recorder.InverseTransformPoint(Pad(end3));
                return Mathf.Max(Mathf.Abs(l.x) - _halfWide,
                       Mathf.Max(Mathf.Abs(l.y) - _halfLong, Mathf.Abs(l.z) - _halfDeep)) * 1000f;
            };

            // ---- the three support fingers set the shape of the grip ----
            float[] spread = { .05f, -.25f, -.55f };
            for (int finger = 1; finger < 4; finger++) {
                var padTarget = recorder.TransformPoint(new Vector3(
                    DistalX * _halfWide * .45f,
                    _halfLong * spread[finger - 1],
                    -(_halfDeep + skin)));
                FitFinger(bases[side, finger == 1 ? 2 : finger == 2 ? 3 : 1], ends[side, finger],
                          nodes, hold, fist, padTarget, tipScale, Mathf.Min(limit, 30f), .62f, 1.0f, .9f, .70f);
            }

            // ---- index onto the far edge, roughly where the key belongs ----
            var nominal = recorder.TransformPoint(new Vector3(
                DistalX * (_halfWide + skin * 2.2f), _halfLong * .76f, -_halfDeep * .3f));
            FitFinger(bases[side, 0], ends[side, 0], nodes, hold, fist,
                      nominal, tipScale, Mathf.Min(limit, 35f), .5f, 1.2f, 1.0f, .55f);

            // ---- then the key moves to the finger, not the finger to the key ----
            // Chasing a fixed key with a finger of fixed length can always miss, and
            // every miss so far has been a finger bent somewhere ugly or a key left
            // floating. The key is mine to place: put it under whatever the index
            // actually does, clamped to the casing so it stays a real record key.
            var seat = recorder.InverseTransformPoint(Pad(ends[side, 0]));
            // Proud of the edge, not flush with it. Codex builds four dark "Side
            // transport key" blocks that stand 3.5mm off one long edge, and they
            // were batched into the casing mesh at build time so they cannot be
            // moved individually. If the record key sits level with the surface,
            // the fingertip lands on one of those instead. Standing it off the edge
            // puts the red cap in front of all of them.
            _keyLocal = new Vector3(DistalX * (_halfWide + .004f),
                                    Mathf.Clamp(seat.y, -_halfLong * .70f, _halfLong * .70f),
                                    Mathf.Clamp(seat.z, -_halfDeep * .55f, _halfDeep * .55f));
            _keyPinned = true;
            SeatRecordKey(recorder);

            // The key travels 3mm when it goes down, so the finger on it travels
            // too. A few degrees on the middle joint about the hand's own hinge
            // axis, scaled by how far the key has moved: the pad stays on the cap
            // through the whole press instead of the cap sinking away from it.
            float pressNow = Mathf.Clamp01(press);
            if (pressNow > .001f) {
                float deg = (_pIndexPressDeg == null ? 6f : _pIndexPressDeg.Value) * pressNow;
                var hinge = (wrists[side].rotation * Quaternion.Inverse(align[side])) * Vector3.right;
                var mid = ends[side, 0] != null ? ends[side, 0].parent : null;
                if (mid != null) mid.rotation = Quaternion.AngleAxis(deg, hinge) * mid.rotation;
            }

            UpdateRecorderSound(recorder, pressNow > .5f);

            // ---- thumb: posed, never solved ----
            // A thumb is a saddle joint. Pointing one at a target with a free
            // minimal-swing rotation, as the other four get, lets it abduct and roll
            // past anything a real thumb can do, and that is the claw. Codex wrote
            // the rule and I broke it once already.
            float thumbBlend = Mathf.Clamp01((_pThumbWrap == null ? .72f : _pThumbWrap.Value) * .26f);
            for (int joint = 0; joint < 3; joint++)
                SetBlend(thumbs[side, joint], nodes, hold, fist, thumbBlend);

            var anatomical = wrists[side].rotation * Quaternion.Inverse(align[side]);
            float thumbFlex = Mathf.Clamp((_pThumbCurl == null ? 24f : _pThumbCurl.Value) * .35f, 0f, 20f);
            var flex = Quaternion.AngleAxis(thumbFlex, anatomical * Vector3.right);
            for (int joint = 1; joint < 3; joint++)
                thumbs[side, joint].rotation = flex * thumbs[side, joint].rotation;

            // Rest it on a corner of the casing rather than nudging it by a
            // distance: the near long edge, up toward the microphone end. A corner
            // is a place on the object, so it stays put when the recorder is
            // resized or re-angled. Still one clamped swing on the base joint.
            float upEdge = Mathf.Clamp(_pThumbCorner == null ? .80f : _pThumbCorner.Value, -1f, 1f);
            var cornerRest = recorder.TransformPoint(new Vector3(
                -DistalX * (_halfWide + .005f), _halfLong * upEdge, -_halfDeep * .35f));
            {
                var baseJoint = thumbs[side, 0];
                var tip = Pad(thumbs[side, 2]);
                Vector3 from = tip - baseJoint.position;
                Vector3 to = cornerRest - baseJoint.position;
                if (from.sqrMagnitude > 1e-10f && to.sqrMagnitude > 1e-10f)
                    baseJoint.rotation = Quaternion.RotateTowards(Quaternion.identity,
                        Quaternion.FromToRotation(from, to), 26f) * baseJoint.rotation;
            }

            // Numbers I can check without a screenshot: 0 is a pad resting on the
            // surface, negative is sunk into it, large positive is hovering.
            if (_gripLogs++ < 8)
                Say("[grip] pad clearance mm  index " + Clearance(ends[side, 0]).ToString("0")
                    + "  middle " + Clearance(ends[side, 1]).ToString("0")
                    + "  ring " + Clearance(ends[side, 2]).ToString("0")
                    + "  little " + Clearance(ends[side, 3]).ToString("0")
                    + "  (key pinned under the index)");
        } catch (Exception e) {
            _gripBroken = true;
            I?.LoggerInstance.Warning("[grip] recorder grip failed, leaving Codex's: " + e.Message);
        }
    }



    // ---------- the other mod that owns the recorder hand ----------
    // BluePrinceVR.RecorderPolish is a separate mod. Its RecorderFrame is a
    // POSTFIX on MemoRecorderFrame, so it runs after everything in here, and it
    // picks its hand with
    //
    //     bool left = (bool)LeftHanded.GetValue(Comfort.GetValue(plugin));
    //
    // which is the dominant-hand preference, not whoever is holding the recorder.
    // With a right-handed profile that is false, so side 1, and it then calls
    // Apply, PoseThumbOnButton and RaiseGripFingers on the RIGHT hand every frame,
    // animating the thumb against the record key with the live trigger value.
    // That is the hand Alex has been watching, and it explains why nothing I did
    // inside my own mod ever stuck: I was fixing a pose that a later postfix in
    // another assembly overwrote.
    //
    // This mod now owns the whole left-hand grip, so the three posing calls are
    // suppressed rather than redirected. One owner, no argument about ordering.
    // Everything else RecorderPolish does - the button styling, its tape motor,
    // its audio - is left alone.

    private static bool _inPolishFrame;

    private static void PolishFramePrefix() { _inPolishFrame = true; }

    private static void PolishFramePostfix()
    {
        _inPolishFrame = false;
        // It also nudges the casing 10mm toward the palm every frame, which undoes
        // the clearance push that keeps it out of the hand. Put that back.
        try {
            if (_memoShellRef == null || _vr == null) return;
            var shell = _memoShellRef(_vr);
            var recorder = shell != null ? shell.parent : null;
            if (recorder == null) return;
            recorder.position -= recorder.TransformDirection(Vector3.back) * .010f;
            if (shell.localScale != Vector3.one) shell.localScale = Vector3.one;
            if (shell.localPosition != Vector3.zero) shell.localPosition = Vector3.zero;
        } catch { }
    }

    private static bool PolishRigApplyPrefix() { return !_inPolishFrame; }
    private static bool PolishThumbPrefix() { return false; }
    private static bool PolishFingersPrefix() { return false; }

    private void PatchRecorderPolish()
    {
        try {
            var polish = AccessTools.TypeByName("RecorderPolishMod")
                      ?? AccessTools.TypeByName("BluePrinceVR.RecorderPolish.RecorderPolishMod");
            if (polish == null) { LoggerInstance.Msg("[recorder] RecorderPolish not loaded; nothing to stand down"); return; }
            var self = typeof(Mod);

            var frame = AccessTools.Method(polish, "RecorderFrame");
            if (frame != null)
                HarmonyInstance.Patch(frame,
                    new HarmonyMethod(self.GetMethod(nameof(PolishFramePrefix), BindingFlags.Static | BindingFlags.NonPublic)),
                    new HarmonyMethod(self.GetMethod(nameof(PolishFramePostfix), BindingFlags.Static | BindingFlags.NonPublic)));

            var thumb = AccessTools.Method(polish, "PoseThumbOnButton");
            if (thumb != null)
                HarmonyInstance.Patch(thumb, new HarmonyMethod(self.GetMethod(nameof(PolishThumbPrefix), BindingFlags.Static | BindingFlags.NonPublic)));

            var grip = AccessTools.Method(polish, "RaiseGripFingers");
            if (grip != null)
                HarmonyInstance.Patch(grip, new HarmonyMethod(self.GetMethod(nameof(PolishFingersPrefix), BindingFlags.Static | BindingFlags.NonPublic)));

            // IndexRig.Apply is called from two places: RecorderPolish's own frame
            // postfix, which has the wrong hand, and its prefix on
            // RestThumbOnRecorder, which inherits the hand my own prefix already
            // corrected. Only the first is suppressed.
            var rig = AccessTools.Inner(polish, "IndexRig");
            var apply = rig == null ? null : AccessTools.Method(rig, "Apply");
            if (apply != null)
                HarmonyInstance.Patch(apply, new HarmonyMethod(self.GetMethod(nameof(PolishRigApplyPrefix), BindingFlags.Static | BindingFlags.NonPublic)));

            LoggerInstance.Msg("[recorder] RecorderPolish stood down on the free hand: frame=" + (frame != null)
                + " thumb=" + (thumb != null) + " fingers=" + (grip != null) + " rig=" + (apply != null));
        } catch (Exception e) {
            LoggerInstance.Warning("[recorder] could not stand RecorderPolish down: " + e.Message);
        }
    }




    // ---------- culling that follows the headset, not the monitor ----------
    // RenderEyesThroughGameCamera swaps the game camera's worldToCameraMatrix and
    // projectionMatrix per eye, renders, and restores them. What it never sets is
    // cullingMatrix, and the finally block deliberately calls ResetCullingMatrix
    // with the note "assigning cullingMatrix pins it". So culling is still decided
    // by the flat camera: its position, its forward, its 60-odd degree FOV. The
    // headset sees a much wider stereo field and can be looking somewhere else
    // entirely, so geometry that is plainly in view gets dropped, and comes back
    // when the flat camera swings far enough round to include it.
    //
    // Widening the culling frustum is the cheap fix. The camera still culls, just
    // against a cone big enough to contain anything either eye can see, which is
    // the stereo field plus however far the head can turn before the game camera
    // follows. It costs some draw calls; that is the trade for things not
    // vanishing.

    private static MelonPreferences_Entry<bool> _pWidenCulling;
    private static MelonPreferences_Entry<float> _pCullExtraFov;
    private static int _cullLogs;

    // The game culls rooms in its own script, through Culler / CullableEntity / CullingGroup
    // and an IsVisible(GameObject, Camera) helper, all of which read the flat camera's real
    // frustum. cullingMatrix cannot reach any of that: it steers Unity's internal renderer
    // culling and nothing else. So the camera itself has to face where the head faces.
    //
    // It cannot simply be set, because Codex derives the playspace rotation FROM this camera
    // (Frame: rotation = camera.transform.rotation, flattened to yaw) and then composes the
    // head on top of it. Writing the head's yaw into the camera would be read back as body
    // yaw on the next frame and added to the head again, and the world would spin.
    //
    // So it is applied as a bias: added after Codex has read the camera for this frame, and
    // taken off again before it reads it next frame. Codex keeps seeing the body yaw it set,
    // while culling and every IsVisible call see where the player is actually looking.
    private static MelonPreferences_Entry<bool> _pTieCamYaw;
    private static AccessTools.FieldRef<VRPlugin, float> _comfortYawRef;
    private static AccessTools.FieldRef<VRPlugin, bool> _inHouseRef;
    private static float _camYawBias;
    private static Camera _biasCam;
    private static int _yawLogs;

    // Remove last frame's bias. Rotating by the negative keeps whatever the game's own
    // camera controller did in between, which plain restoring a saved rotation would eat.
    private static void ClearCamYawBias()
    {
        if (_camYawBias == 0f) return;
        try {
            if (_biasCam != null)
                _biasCam.transform.rotation = Quaternion.AngleAxis(-_camYawBias, Vector3.up) * _biasCam.transform.rotation;
        } catch { }
        _camYawBias = 0f;
        _biasCam = null;
    }

    private static void TieCamYawToHead(VRPlugin vr)
    {
        if (_pTieCamYaw != null && !_pTieCamYaw.Value) { ClearCamYawBias(); return; }
        try {
            if (_inHouseRef == null) _inHouseRef = SafeField<bool>("_inHouse");
            if (_comfortYawRef == null) _comfortYawRef = SafeField<float>("_comfortYaw");

            bool inHouse = _inHouseRef == null || _inHouseRef(vr);
            // At a terminal Codex swaps in its own tracking yaw, so leave that path alone.
            bool atTerminal = _keyboardMode != null && _keyboardMode(vr);
            if (!inHouse || atTerminal) { ClearCamYawBias(); return; }

            Camera cam = null;
            foreach (var c in Camera.allCameras) {
                if (c == null || !c.isActiveAndEnabled) continue;
                if ((c.name ?? "").Equals("FirstPersonCharacter", StringComparison.OrdinalIgnoreCase)) { cam = c; break; }
            }
            if (cam == null) { ClearCamYawBias(); return; }

            // Yaw of the headset off the forward vector, not eulerAngles.y, which goes
            // strange once pitch and roll are in the quaternion.
            var hr = _headRot != null ? _headRot(vr) : Quaternion.identity;
            Vector3 fwd = hr * Vector3.forward;
            Vector3 flat = new Vector3(fwd.x, 0f, fwd.z);
            if (flat.sqrMagnitude < 1e-4f) {           // looking straight up or down
                Vector3 up = hr * Vector3.up;
                flat = new Vector3(up.x, 0f, up.z);
                if (flat.sqrMagnitude < 1e-4f) { ClearCamYawBias(); return; }
            }
            float headYaw = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;

            // Snap and smooth turning live in Codex's comfort yaw, which sits between the
            // camera and the head, so it belongs in the bias too.
            float comfort = 0f;
            try { comfort = _comfortYawRef == null ? 0f : _comfortYawRef(vr); } catch { }

            float bias = Mathf.DeltaAngle(0f, comfort + headYaw);
            if (Mathf.Abs(bias) < .05f) { ClearCamYawBias(); return; }

            cam.transform.rotation = Quaternion.AngleAxis(bias, Vector3.up) * cam.transform.rotation;
            _camYawBias = bias;
            _biasCam = cam;

            if (_yawLogs++ < 3)
                Say("[culling] game camera yaw biased " + bias.ToString("0") + " degrees onto the head, so room culling and IsVisible see the headset's direction");
        } catch (Exception e) {
            if (_yawLogs++ < 3) I?.LoggerInstance.Warning("[culling] yaw tie: " + e.Message);
            ClearCamYawBias();
        }
    }

    // Three things can hide a room, and a raycast separates them without touching the rest
    // of the scene. Colliders do not care about renderers, so the ray reaches geometry that
    // is merely not being drawn, and misses geometry whose GameObject was switched off:
    //   ray hits nothing where a room should be   -> the game deactivated it, script culling
    //   hits, renderer disabled                   -> script culling by the other path
    //   hits, renderer on, isVisible false        -> Unity's own culling, camera direction
    // Cast once along the head and once along the flat camera, so the disagreement between
    // the two is in the same line of the log as its cause.
    private static MelonPreferences_Entry<bool> _pVoidProbe;
    private static float _voidProbeNext;
    private static int _voidProbeLines;
    private static string _voidProbeLast = "";

    private static string ProbeAlong(Vector3 origin, Vector3 dir)
    {
        if (!Physics.Raycast(origin, dir, out var hit, 40f, -1, QueryTriggerInteraction.Ignore))
            return "nothing within 40m";
        string name = hit.collider == null ? "?" : hit.collider.name;
        float d = hit.distance;
        Renderer r = null;
        try {
            r = hit.collider.GetComponent<Renderer>();
            if (r == null) r = hit.collider.GetComponentInChildren<Renderer>();
            if (r == null && hit.collider.transform.parent != null)
                r = hit.collider.transform.parent.GetComponentInChildren<Renderer>();
        } catch { }
        if (r == null) return "'" + name + "' at " + d.ToString("0.0") + "m, no renderer on it";
        return "'" + name + "' at " + d.ToString("0.0") + "m activeGO=" + r.gameObject.activeInHierarchy
             + " rendererOn=" + r.enabled + " isVisible=" + r.isVisible;
    }

    private static void VoidProbe(VRPlugin vr)
    {
        if (_pVoidProbe == null || !_pVoidProbe.Value) return;
        if (_voidProbeLines >= 400) return;
        if (Time.unscaledTime < _voidProbeNext) return;
        _voidProbeNext = Time.unscaledTime + .5f;

        try {
            Camera cam = null;
            foreach (var c in Camera.allCameras) {
                if (c == null || !c.isActiveAndEnabled) continue;
                if ((c.name ?? "").Equals("FirstPersonCharacter", StringComparison.OrdinalIgnoreCase)) { cam = c; break; }
            }
            if (cam == null) return;

            // Codex's own composition: body yaw off the flat camera, head on top of it.
            var body = Quaternion.Euler(0f, cam.transform.eulerAngles.y, 0f);
            float comfort = 0f;
            try {
                if (_comfortYawRef == null) _comfortYawRef = SafeField<float>("_comfortYaw");
                comfort = _comfortYawRef == null ? 0f : _comfortYawRef(vr);
            } catch { }
            var hr = _headRot != null ? _headRot(vr) : Quaternion.identity;
            var world = body * Quaternion.Euler(0f, comfort, 0f) * hr;

            Vector3 origin = cam.transform.position;
            Vector3 headDir = world * Vector3.forward;
            Vector3 camDir = cam.transform.forward;
            float apart = Vector3.Angle(
                new Vector3(headDir.x, 0f, headDir.z),
                new Vector3(camDir.x, 0f, camDir.z));

            string line = "[void] " + apart.ToString("0") + "deg apart | head sees " + ProbeAlong(origin, headDir)
                        + " | flatcam sees " + ProbeAlong(origin, camDir);

            // Only when something changed, so standing still does not fill the log.
            if (line == _voidProbeLast) return;
            _voidProbeLast = line;
            _voidProbeLines++;
            I?.LoggerInstance.Msg(line);
        } catch (Exception e) {
            _voidProbeLines = 400;
            I?.LoggerInstance.Warning("[void] probe unavailable: " + e.Message);
        }
    }

    // The probe settled it: a renderer goes isVisible=false while its GameObject is still
    // active and its renderer still enabled, purely as a function of the angle between the
    // head and the flat camera. That is Unity's own frustum culling, so cullingMatrix is
    // the right lever, and the earlier attempt only failed because it was built from the
    // camera's rotation. Build it from the head instead.
    //
    // Timing is the other half. RenderEyesThroughGameCamera calls ResetCullingMatrix in its
    // finally, after BOTH eyes, so a matrix assigned before Frame() runs survives the eye
    // renders that the player actually sees and is then released for the flat pass. Hence
    // this is called from the Update postfix, not the LateUpdate one.
    private static MelonPreferences_Entry<bool> _pCullFollowsHead;
    private static MelonPreferences_Entry<float> _pCullExtraDeg;
    private static int _headCullLogs;

    // The camera was never the problem. Il2Cpp.Culler is a MonoBehaviour sitting on each
    // room that decides in its own LateUpdate and switches the room's renderers off, which
    // is what the probe kept reporting as rendererOn=False, and what no cullingMatrix could
    // ever reach. Whatever it decides from, in VR it gets the answer wrong for a room you
    // are looking at through a doorway while your body faces elsewhere.
    //
    // Rather than fight its logic, overrule it near the player: inside a radius, a room is
    // never switched off. Distant rooms cull exactly as before, so the mansion is not all
    // drawn at once and the frame rate survives. Direction never enters into it, so there
    // is nothing here to race with the camera.
    private static MelonPreferences_Entry<bool> _pKeepRoomsDrawn;
    private static MelonPreferences_Entry<float> _pKeepRoomsRadius;
    private static Vector3 _playerPos;
    private static bool _havePlayerPos;
    private static int _cullerLogs;

    private static bool NearThePlayer(Transform t)
    {
        if (!_havePlayerPos || t == null) return false;
        float r = _pKeepRoomsRadius == null ? 35f : _pKeepRoomsRadius.Value;
        return (t.position - _playerPos).sqrMagnitude <= r * r;
    }

    // Keeps the radius check cheap: one camera lookup per frame instead of one per call,
    // and Culler.LateUpdate can fire for a great many rooms in a frame.
    private static void NotePlayerPos()
    {
        try {
            foreach (var c in Camera.allCameras) {
                if (c == null || !c.isActiveAndEnabled) continue;
                if ((c.name ?? "").Equals("FirstPersonCharacter", StringComparison.OrdinalIgnoreCase)) {
                    _playerPos = c.transform.position;
                    _havePlayerPos = true;
                    return;
                }
            }
        } catch { }
    }

    private static void CullerRenderersPrefix(Culler __instance, ref bool enable)
    {
        if (_pKeepRoomsDrawn == null || !_pKeepRoomsDrawn.Value) return;
        if (enable || __instance == null) return;
        if (!NearThePlayer(__instance.transform)) return;
        enable = true;
        if (_cullerLogs++ < 12)
            Say("[culling] kept '" + __instance.name + "' drawn, "
                + Vector3.Distance(__instance.transform.position, _playerPos).ToString("0.0")
                + "m away, the game wanted its renderers off");
    }

    private static bool CullerTurnOffPrefix(Culler __instance)
    {
        if (_pKeepRoomsDrawn == null || !_pKeepRoomsDrawn.Value) return true;
        if (__instance == null || !NearThePlayer(__instance.transform)) return true;
        if (_cullerLogs++ < 12)
            Say("[culling] blocked TurnOff on '" + __instance.name + "', "
                + Vector3.Distance(__instance.transform.position, _playerPos).ToString("0.0") + "m away");
        return false;
    }

    private void PatchMagnifier()
    {
        try {
            var self = typeof(Mod);
            Patch("UpdatePanelStickPointer", Type.EmptyTypes,
                self.GetMethod(nameof(PanelPointerPrefix), BindingFlags.Static | BindingFlags.NonPublic), null);
            Patch("PublishPanelStickPointer", Type.EmptyTypes,
                self.GetMethod(nameof(PanelPublishPrefix), BindingFlags.Static | BindingFlags.NonPublic), null);
            Patch("ClickFromPanelStick", new[] { typeof(Hand) },
                self.GetMethod(nameof(PanelClickPrefix), BindingFlags.Static | BindingFlags.NonPublic), null);
            LoggerInstance.Msg("[magnifier] grip plus hand movement wired up");
        } catch (Exception e) {
            LoggerInstance.Warning("[magnifier] could not wire hand control: " + e.Message);
        }
    }

    private void PatchRoomCuller()
    {
        try {
            var culler = AccessTools.TypeByName("Il2Cpp.Culler") ?? typeof(Culler);
            if (culler == null) { LoggerInstance.Warning("[culling] Culler type not found, room culling left alone"); return; }
            var self = typeof(Mod);

            var renderers = AccessTools.Method(culler, "EnableRenderers", new[] { typeof(bool) });
            if (renderers != null)
                HarmonyInstance.Patch(renderers, prefix: new HarmonyMethod(self.GetMethod(nameof(CullerRenderersPrefix), BindingFlags.Static | BindingFlags.NonPublic)));
            else LoggerInstance.Warning("[culling] Culler.EnableRenderers not found");

            var turnOff = AccessTools.Method(culler, "TurnOff", Type.EmptyTypes);
            if (turnOff != null)
                HarmonyInstance.Patch(turnOff, prefix: new HarmonyMethod(self.GetMethod(nameof(CullerTurnOffPrefix), BindingFlags.Static | BindingFlags.NonPublic)));
            else LoggerInstance.Warning("[culling] Culler.TurnOff not found");

            LoggerInstance.Msg("[culling] room culler patched: rooms within reach stay drawn");
        } catch (Exception e) {
            LoggerInstance.Warning("[culling] could not patch the room culler: " + e.Message);
        }
    }

    // Codex's magnifier is a screen pointer over the document panel, moved by the left
    // stick and toggled by the use-item gesture. That toggle rides on VRInput.UseItemEdge,
    // which the use-item latch elsewhere in this mod rewrites, which is why it stopped
    // responding. Rather than untangle the toggle, hold grip and move the pointer with the
    // hand, which is what reading a page in VR should feel like anyway.
    //
    // Hand travel is measured against the head's own right and up axes, so it tracks the
    // way the page faces you rather than some world axis, and it is relative: grip down
    // anchors wherever the pointer already is, so you can let go, reposition your arm and
    // carry on, exactly like lifting a mouse. The stick still works when grip is not held.
    private static MelonPreferences_Entry<bool> _pMagGripGate;
    private static MelonPreferences_Entry<bool> _pMagnifierByHand;
    private static AccessTools.FieldRef<VRPlugin, Vector2> _leftStickRef;
    private static MelonPreferences_Entry<float> _pMagnifierGain;
    private static AccessTools.FieldRef<VRPlugin, bool> _magPointerRef;
    private static FieldInfo _panelStickField, _pspActiveField, _pspPositionField;
    private static bool _magGripWas;
    private static Vector3 _magHandAnchor;
    private static Vector2 _magPointerAnchor;
    private static int _handMagLogs;

    private static Vector3 HandPoint(Hand h)
    {
        if (h == null || !h.Valid) return new Vector3(10000f, 10000f, 10000f);
        try {
            if (h.HandNode != null) return h.HandNode.transform.position;
            if (h.Tip != null) return h.Tip.position;
        } catch { }
        return new Vector3(10000f, 10000f, 10000f);
    }

    // hand.GripDown reads false for the whole time a document is open, which is what
    // every build from c69 on was quietly hanging off. The pad itself still knows, so ask
    // it directly, the same way the title-screen page had to. Mask 4 is grip.
    private static bool PadGrip(out bool leftHand)
    {
        leftHand = false;
        try {
            if (ReadPad(true, out var ls) && (ls.ulButtonPressed & 4UL) != 0) { leftHand = true; return true; }
            if (ReadPad(false, out var rs) && (rs.ulButtonPressed & 4UL) != 0) return true;
        } catch { }
        return false;
    }

    // The hand doing the moving. The pad decides whether a grip is held at all; the Hand
    // is only needed for its position, and only by the optional motion mode.
    private static Hand GrippingHand(VRPlugin vr)
    {
        if (!PadGrip(out bool left)) {
            // Fall back to Codex's own flags in case the pad read is unavailable.
            var r0 = _handRRef?.Invoke(vr);
            var l0 = _handLRef?.Invoke(vr);
            if (r0 != null && r0.Valid && r0.GripDown) return r0;
            if (l0 != null && l0.Valid && l0.GripDown) return l0;
            return null;
        }
        var want = left ? _handLRef?.Invoke(vr) : _handRRef?.Invoke(vr);
        if (want != null && want.Valid) return want;
        var other = left ? _handRRef?.Invoke(vr) : _handLRef?.Invoke(vr);
        if (other != null && other.Valid) return other;
        return null;
    }

    // Grip is held, whether or not a usable Hand came with it. The drag gate only needs
    // this much, so it keeps working even when the hands are not being posed.
    private static bool GripHeld(VRPlugin vr)
    {
        if (PadGrip(out _)) return true;
        var r = _handRRef?.Invoke(vr);
        var l = _handLRef?.Invoke(vr);
        return (r != null && r.Valid && r.GripDown) || (l != null && l.Valid && l.GripDown);
    }

    private static bool DocumentPanelOpen(VRPlugin vr)
    {
        try {
            var quad = _uiQuad?.Invoke(vr);
            return quad != null && quad.activeInHierarchy
                && Cursor.lockState == CursorLockMode.None && Application.isFocused;
        } catch { return false; }
    }

    // Keeps Codex from treating the document as a native menu and bailing out, which is
    // what its _magnifierPointer flag decides.
    private static void PanelPointerPrefix(VRPlugin __instance)
    {
        if (!GripGate) return;
        try {
            if (_magPointerRef == null) _magPointerRef = SafeField<bool>("_magnifierPointer");
            if (_magPointerRef == null) return;
            if (!DocumentPanelOpen(__instance)) return;
            // Hold to use. Releasing grip puts the panel back under the game's own
            // navigation, which is what stops it becoming a cursor-mode trap.
            _magPointerRef(__instance) = GripHeld(__instance);
        } catch { }
    }

    // Codex only calls its publish when its own stick tick succeeds, which is not every
    // frame, so hooking the publish alone left the anchor resetting before it could move
    // anything. Compute here, and drive it from LateUpdate as well as from Codex's call.
    private static MethodInfo _publishMethod;
    private static float _magDiagNext;

    private static void PanelPublishPrefix(VRPlugin __instance)
    {
        ApplyMagnifier(__instance);
    }

    // The magnifier is a DRAG, not a hover. Il2Cpp.MouseDrag drives it through
    // StartDrag / DragObject / StopDrag, so the glass only moves while the mouse button
    // is held down AND the pointer is moving. Codex presses that button only on Select,
    // which is why the pointer was visibly moving under a motionless glass.
    //
    // Codex already owns the press, in ClickFromPanelStick, off its _panelConfirm flag.
    // Holding grip simply holds that flag, so moving the stick becomes a drag.
    private static AccessTools.FieldRef<VRPlugin, bool> _panelConfirmRef;
    private static bool _magPressed;
    private static PropertyInfo _draggingProp;

    // The glass is a drag object, and a drag only takes hold if the pointer is already
    // ON it. Ours sits wherever it was left, so the press lands on blank page, or on a
    // page-turn zone, and only catches the glass when it happens to be underneath. So
    // find it and put the pointer on it before pressing.
    private static bool _magListed;

    // Every live MouseDrag near the open document, named with where it is on screen.
    private static void ListDragTargets()
    {
        if (_magListed) return;
        _magListed = true;
        try {
            var cam = Camera.main;
            int n = 0;
            foreach (var item in Resources.FindObjectsOfTypeAll(Il2CppInterop.Runtime.Il2CppType.Of<MouseDrag>())) {
                var drag = item == null ? null : item.TryCast<MouseDrag>();
                if (drag == null) continue;
                GameObject go;
                try { go = drag.gameObject; } catch { continue; }
                if (go == null || !go.activeInHierarchy) continue;
                if (n++ >= 8) break;
                string where = "?";
                try {
                    if (cam != null) {
                        var sp = cam.WorldToScreenPoint(go.transform.position);
                        where = sp.x.ToString("0") + "," + sp.y.ToString("0") + " z=" + sp.z.ToString("0.0");
                    }
                } catch { }
                Say("[magnifier] drag target '" + go.name + "' at screen " + where);
            }
            if (n == 0) Say("[magnifier] no live MouseDrag while this document is open");
        } catch (Exception e) { I?.LoggerInstance.Warning("[magnifier] listing drags: " + e.Message); }
    }

    // FindObjectsOfTypeAll is far too costly to run per frame, and this only needs to
    // answer "is there something draggable on screen", which does not change quickly.
    private static float _dragTargetNext;
    private static bool _dragTargetHas;

    private static bool HasDragTarget()
    {
        if (Time.unscaledTime < _dragTargetNext) return _dragTargetHas;
        _dragTargetNext = Time.unscaledTime + .4f;
        _dragTargetHas = DragTargetScreenPoint(out _);
        return _dragTargetHas;
    }

    // Where to put the pointer so the press lands on the glass.
    private static bool DragTargetScreenPoint(out Vector2 point)
    {
        point = default;
        try {
            var cam = Camera.main;
            if (cam == null) return false;
            float best = float.MaxValue;
            bool found = false;
            foreach (var item in Resources.FindObjectsOfTypeAll(Il2CppInterop.Runtime.Il2CppType.Of<MouseDrag>())) {
                var drag = item == null ? null : item.TryCast<MouseDrag>();
                if (drag == null) continue;
                GameObject go;
                try { go = drag.gameObject; } catch { continue; }
                if (go == null || !go.activeInHierarchy) continue;
                Vector3 sp;
                try { sp = cam.WorldToScreenPoint(go.transform.position); } catch { continue; }
                if (sp.z <= 0f) continue;                       // behind the camera
                if (sp.x < 0f || sp.y < 0f || sp.x > Screen.width || sp.y > Screen.height) continue;
                if (sp.z < best) { best = sp.z; point = new Vector2(sp.x, sp.y); found = true; }
            }
            return found;
        } catch { return false; }
    }

    private static void SnapPointerTo(VRPlugin vr, Vector2 point)
    {
        try {
            if (_panelStickField == null) _panelStickField = AccessTools.Field(typeof(VRPlugin), "_panelStick");
            var psp = _panelStickField?.GetValue(vr);
            if (psp == null) return;
            if (_pspActiveField == null) _pspActiveField = AccessTools.Field(psp.GetType(), "Active");
            if (_pspPositionField == null) _pspPositionField = AccessTools.Field(psp.GetType(), "Position");
            _pspActiveField?.SetValue(psp, true);
            _pspPositionField?.SetValue(psp, point);
            if (_publishMethod == null)
                _publishMethod = AccessTools.Method(typeof(VRPlugin), "PublishPanelStickPointer", Type.EmptyTypes);
            _publishMethod?.Invoke(vr, null);
        } catch { }
    }

    // Whether Codex's pointer has latched yet. A first grip that does nothing and a
    // second that works is what it looks like when this is still false on the first.
    private static string StickActiveText(VRPlugin vr)
    {
        try {
            if (_panelStickField == null) _panelStickField = AccessTools.Field(typeof(VRPlugin), "_panelStick");
            var psp = _panelStickField?.GetValue(vr);
            if (psp == null) return "?";
            if (_pspActiveField == null) _pspActiveField = AccessTools.Field(psp.GetType(), "Active");
            return _pspActiveField?.GetValue(psp)?.ToString() ?? "?";
        } catch { return "?"; }
    }

    // What the game thinks is happening, so a page turn can be told from a drag.
    private static string DragStateText()
    {
        try {
            if (_draggingProp == null)
                _draggingProp = AccessTools.Property(typeof(MouseDrag), "CurrentlyDragging");
            var v = _draggingProp?.GetValue(null);
            return v == null ? "?" : v.ToString();
        } catch { return "?"; }
    }

    private static void PanelClickPrefix(VRPlugin __instance)
    {
        if (!GripGate) return;
        try {
            if (_panelConfirmRef == null) _panelConfirmRef = SafeField<bool>("_panelConfirm");
            if (_panelConfirmRef == null) return;
            if (!DocumentPanelOpen(__instance)) { _magPressed = false; return; }

            if (!GripHeld(__instance) || !HasDragTarget()) { _magPressed = false; return; }

            // A press on its own is a click, and on a multi-page letter whatever sits
            // under the pointer when you squeeze is a page turn. So hold nothing until
            // the stick is actually moving: the button then goes down mid-movement,
            // which reads as a drag rather than a click, and a bare grip does nothing.
            if (!_magPressed) {
                float mag = 0f;
                try {
                    if (_leftStickRef == null) _leftStickRef = SafeField<Vector2>("_leftPanelStick");
                    if (_leftStickRef != null) mag = _leftStickRef(__instance).magnitude;
                } catch { }
                if (mag < .35f) { _panelConfirmRef(__instance) = false; return; }

                // Put the pointer on the glass first, otherwise the press grabs whatever
                // else is under it, which on a multi-page letter is a page turn.
                if (DragTargetScreenPoint(out var onGlass)) {
                    SnapPointerTo(__instance, onGlass);
                    if (_handMagLogs < 10)
                        Say("[magnifier] snapped the pointer onto the drag target at "
                            + onGlass.x.ToString("0") + "," + onGlass.y.ToString("0"));
                } else if (_handMagLogs < 10) {
                    Say("[magnifier] no drag target found on screen; pressing where the pointer is");
                }
                _magPressed = true;
                if (_handMagLogs++ < 10)
                    Say("[magnifier] stick moved while gripping, taking hold of the glass"
                        + " (dragging=" + DragStateText()
                        + ", pointerOn=" + (_magPointerRef != null && _magPointerRef(__instance))
                        + ", stickActive=" + StickActiveText(__instance) + ")");
            }
            _panelConfirmRef(__instance) = true;       // stays down for the rest of the grip
        } catch { }
    }

    // Every frame, whether or not Codex got around to publishing.
    private static void DriveMagnifier(VRPlugin vr)
    {
        if (!HandMagnifier) { MagnifierDiag(vr); return; }
        if (!ApplyMagnifier(vr)) return;
        try {
            if (_publishMethod == null)
                _publishMethod = AccessTools.Method(typeof(VRPlugin), "PublishPanelStickPointer", Type.EmptyTypes);
            _publishMethod?.Invoke(vr, null);
        } catch { }
    }

    // One line a second while a document is up, so a failure says which condition was
    // missing instead of leaving it to be guessed at.
    private static void MagnifierDiag(VRPlugin vr)
    {
        if (!GripGate) return;
        try {
            if (!DocumentPanelOpen(vr)) { _magListed = false; return; }
            ListDragTargets();
            if (Time.unscaledTime < _magDiagNext) return;
            _magDiagNext = Time.unscaledTime + 1f;
            if (_leftStickRef == null) _leftStickRef = SafeField<Vector2>("_leftPanelStick");
            string stick = "?";
            try { if (_leftStickRef != null) stick = _leftStickRef(vr).ToString("0.00"); } catch { }
            if (_handMagLogs++ < 30)
                Say("[magnifier] panel open, grip=" + GripHeld(vr) + " (pad=" + PadGrip(out _) + "), stick=" + stick);
        } catch { }
    }

    private static bool ApplyMagnifier(VRPlugin __instance)
    {
        if (!HandMagnifier) return false;
        try {
            if (_panelStickField == null) {
                _panelStickField = AccessTools.Field(typeof(VRPlugin), "_panelStick");
                if (_panelStickField == null) return false;
                var pspType = _panelStickField.FieldType;
                _pspActiveField = AccessTools.Field(pspType, "Active");
                _pspPositionField = AccessTools.Field(pspType, "Position");
            }
            if (_pspPositionField == null || _pspActiveField == null) return false;

            bool open = DocumentPanelOpen(__instance);
            var hand = GrippingHand(__instance);

            if (hand == null || !open) { _magGripWas = false; return false; }

            var psp = _panelStickField.GetValue(__instance);
            if (psp == null) return false;

            Vector3 point = HandPoint(hand);
            if (point.x > 9000f) { _magGripWas = false; return false; }

            var current = (Vector2)_pspPositionField.GetValue(psp);
            if (!_magGripWas) {
                _magGripWas = true;
                _magHandAnchor = point;
                _magPointerAnchor = current;
                if (_handMagLogs++ < 6) Say("[magnifier] grip held, the page now follows your hand");
                return true;                  // no jump on the frame you grab it
            }

            // Head axes, so the page moves with you rather than with the world.
            Camera cam = null;
            foreach (var c in Camera.allCameras) {
                if (c == null || !c.isActiveAndEnabled) continue;
                if ((c.name ?? "").Equals("FirstPersonCharacter", StringComparison.OrdinalIgnoreCase)) { cam = c; break; }
            }
            var body = cam == null ? Quaternion.identity : Quaternion.Euler(0f, cam.transform.eulerAngles.y, 0f);
            float comfort = 0f;
            try {
                if (_comfortYawRef == null) _comfortYawRef = SafeField<float>("_comfortYaw");
                comfort = _comfortYawRef == null ? 0f : _comfortYawRef(__instance);
            } catch { }
            var hr = _headRot != null ? _headRot(__instance) : Quaternion.identity;
            var world = body * Quaternion.Euler(0f, comfort, 0f) * hr;

            Vector3 moved = point - _magHandAnchor;
            float gain = (_pMagnifierGain == null ? 3f : _pMagnifierGain.Value) * Screen.height;
            float x = _magPointerAnchor.x + Vector3.Dot(moved, world * Vector3.right) * gain;
            float y = _magPointerAnchor.y + Vector3.Dot(moved, world * Vector3.up) * gain;

            var wanted = new Vector2(Mathf.Clamp(x, 1f, Screen.width - 2f),
                                     Mathf.Clamp(y, 1f, Screen.height - 2f));
            _pspActiveField.SetValue(psp, true);
            _pspPositionField.SetValue(psp, wanted);
            return true;
        } catch (Exception e) {
            if (_handMagLogs++ < 6) I?.LoggerInstance.Warning("[magnifier] hand control: " + e.Message);
            return false;
        }
    }


    // ================= the full fingertip keyboard =================
    // Codex proved the mechanism: six points calibrated by touching them with the index
    // tip, stored in the computer's local space, pressed by fingertip proximity with a
    // lift-before-press gate, delivered through SetComputerKey -> keybd_event. All that is
    // missing is the other forty keys, and the reason it stopped at six is that marking
    // forty-five points by hand per computer is not a thing anyone would do twice.
    //
    // The four profiles already on disk settle that. Laboratory, Office, Security and
    // Standalone agree to within 2-3cm, which is fingertip placement error, so every room
    // has the same keyboard prefab at the same local offset. One frame therefore serves
    // every computer: touch three keys, Q, P and SPACE, and the rest of the layout follows
    // by arithmetic. Q fixes the origin, P fixes the across direction and the key pitch,
    // SPACE fixes the down-the-keyboard direction, tilt included. Three taps, once.

    private const byte VkShift = 0x10, VkSpace = 0x20, VkBack = 0x08, VkEnter = 0x0D;

    private sealed class KeyDef
    {
        public string Label;
        public byte Vk;
        public byte Also;             // a second code sent with it, 0 for none
        public float U, V;            // column and row in key units from Q
        public float Reach = 1f;      // multiplier on the press distance
        public Vector3 Local;         // filled in once the frame is known
        public bool Armed = true;     // must lift before it will fire again
    }

    // ANSI layout in key units with Q at the origin, u across to the right of the
    // keyboard, v down it. The quarter-unit staggers are what make the rows line up.
    private static readonly (string label, byte vk, float u, float v)[] KeyLayout =
    {
        ("1",0x31,-0.5f,-1f), ("2",0x32,0.5f,-1f), ("3",0x33,1.5f,-1f), ("4",0x34,2.5f,-1f),
        ("5",0x35,3.5f,-1f),  ("6",0x36,4.5f,-1f), ("7",0x37,5.5f,-1f), ("8",0x38,6.5f,-1f),
        ("9",0x39,7.5f,-1f),  ("0",0x30,8.5f,-1f),

        // No I. This keyboard is missing that key, apparently on purpose, and assuming
        // it was there made Q->P nine gaps instead of eight. That put the pitch 11% short,
        // which accumulated across the row until the middle keys landed on their
        // neighbours: B typed N, T typed Y, U typed I, and K could not be hit at all.
        ("Q",0x51,0f,0f), ("W",0x57,1f,0f), ("E",0x45,2f,0f), ("R",0x52,3f,0f), ("T",0x54,4f,0f),
        ("Y",0x59,5f,0f), ("U",0x55,6f,0f), ("O",0x4F,7f,0f), ("P",0x50,8f,0f),
        // Measured, not guessed: a fingertip held on this key solves back to
        // u=9.55 v=0.09 against the calibrated frame, so Delete sits on the
        // letter row just right of P, not on the digit row where ANSI puts
        // Backspace. That whole-row error was the 5-6cm miss.
        ("DEL",VkBack,9.5f,0f),

        ("A",0x41,0.25f,1f), ("S",0x53,1.25f,1f), ("D",0x44,2.25f,1f), ("F",0x46,3.25f,1f),
        ("G",0x47,4.25f,1f),  ("H",0x48,5.25f,1f), ("J",0x4A,6.25f,1f), ("K",0x4B,7.25f,1f),
        ("L",0x4C,8.25f,1f),

        ("SHIFT",VkShift,-0.6f,2f),
        ("Z",0x5A,0.75f,2f), ("X",0x58,1.75f,2f), ("C",0x43,2.75f,2f), ("V",0x56,3.75f,2f),
        ("B",0x42,4.75f,2f), ("N",0x4E,5.75f,2f), ("M",0x4D,6.75f,2f),

        ("SPACE",VkSpace,3.5f,3f),
    };

    private static MelonPreferences_Entry<bool> _pFullKeyboard;
    private static MelonPreferences_Entry<float> _pKeyClickVolume;
    private static MelonPreferences_Entry<float> _pKeyPressFrac, _pKeyArmFrac;
    private static List<KeyDef> _keys;
    private static Vector3 _kbOrigin, _kbAcross, _kbDown;   // per key unit
    private static bool _kbReady;
    private static string _kbProfile = "";
    private static bool _kbShift;
    private static float _kbShiftUntil, _kbReachNext;
    private static bool _kbModsCleared;
    private static AccessTools.FieldRef<VRPlugin, int> _codexCalRef;
    private static float _codexCalStuck;
    private static int _kbCalStep = -1;
    private static bool _kbCalAWas, _kbBothGripWas;
    private static float _kbCalHeldSince = -1f, _kbCalDiagNext, _kbCalLive, _kbCalReadyAt, _kbCalBlockUntil;
    private static readonly Vector3[] _kbCalPoints = new Vector3[3];
    private static readonly string[] _kbCalNames = { "Q", "P", "Z" };
    private static readonly List<(byte vk, float at)> _kbReleases = new();
    private static int _kbLogs;
    private static MethodInfo _setKeyM, _clickM, _pulseM, _indexTipM;

    private static string KbFile(string profile)
        => System.IO.Path.Combine(Application.dataPath, "..", "UserData",
                                  "ClaudeFixes-keyboard-" + profile + ".txt");

    // Q, P and SPACE are enough for a full affine frame, which means tilt and a keyboard
    // that is not square to the desk both come out right.
    private static void BuildKeyFrame()
    {
        _kbAcross = (_kbCalPoints[1] - _kbCalPoints[0]) / 8f;        // Q -> P spans 8 gaps: no I key
        _kbOrigin = _kbCalPoints[0];
        // Z rather than SPACE: it is two rows down and barely across, so it is a short
        // reach. SPACE is the longest reach on the board and any overshoot there gets
        // divided by three and then multiplied into every row, which is how the first
        // attempt produced 12cm rows against 3cm columns.
        Vector3 toZ = _kbCalPoints[2] - _kbOrigin - _kbAcross * 0.75f;
        _kbDown = toZ / 2f;                                          // Z sits 2 rows down

        _keys = new List<KeyDef>();
        foreach (var k in KeyLayout)
            _keys.Add(new KeyDef {
                Label = k.label, Vk = k.vk, U = k.u, V = k.v,
                // Delete is the one key whose position is inferred rather than measured,
                // since the board is not ANSI and has no I. Give it room, and send Delete
                // as well as Backspace so whichever the field listens for lands.
                Also = k.label == "DEL" ? (byte)0x2E : (byte)0,
                Reach = k.label == "DEL" ? 1.6f : 1f,
                Local = _kbOrigin + _kbAcross * k.u + _kbDown * k.v });
        float across = _kbAcross.magnitude, down = _kbDown.magnitude;
        Say("[keyboard] points Q" + _kbCalPoints[0].ToString("F3")
            + " P" + _kbCalPoints[1].ToString("F3") + " Z" + _kbCalPoints[2].ToString("F3"));

        // A keyboard's rows and columns are within about half of each other. Anything
        // wilder means a point was marked somewhere other than the key it names, and
        // saving it silently is how you end up typing into thin air.
        if (across < .005f || down < .005f || down > across * 1.5f || across > down * 1.5f) {
            _kbReady = false;
            I?.LoggerInstance.Warning("[keyboard] calibration looks wrong: " + (across * 100f).ToString("0.0")
                + "cm across against " + (down * 100f).ToString("0.0")
                + "cm per row. On this keyboard they should both be near 4cm. Mark Q, P and Z again,"
                + " touching the centre of each key.");
            return;
        }
        _kbReady = true;
        Say("[keyboard] " + _keys.Count + " keys placed; pitch "
            + (across * 100f).ToString("0.0") + "cm across, "
            + (down * 100f).ToString("0.0") + "cm per row");
    }

    private static void SaveKeyFrame()
    {
        try {
            var p = new float[9];
            for (int i = 0; i < 3; i++) { p[i*3] = _kbCalPoints[i].x; p[i*3+1] = _kbCalPoints[i].y; p[i*3+2] = _kbCalPoints[i].z; }
            System.IO.File.WriteAllText(KbFile(_kbProfile), string.Join(",", p));
            Say("[keyboard] calibration saved for " + _kbProfile);
        } catch (Exception e) { I?.LoggerInstance.Warning("[keyboard] save failed: " + e.Message); }
    }

    // Any computer's frame will do, because they are all the same prefab; prefer this
    // one's own file, then fall back to whichever was calibrated first.
    private static bool LoadKeyFrame(string profile)
    {
        foreach (string candidate in new[] { profile, "shared" }) {
            try {
                string path = KbFile(candidate);
                if (!System.IO.File.Exists(path)) continue;
                var bits = System.IO.File.ReadAllText(path).Split(',');
                if (bits.Length != 9) continue;
                for (int i = 0; i < 3; i++)
                    _kbCalPoints[i] = new Vector3(float.Parse(bits[i*3]), float.Parse(bits[i*3+1]), float.Parse(bits[i*3+2]));
                BuildKeyFrame();
                return true;
            } catch { }
        }
        return false;
    }

    private static Vector3 TipLocal(VRPlugin vr, Transform computer, Hand hand)
    {
        try {
            if (hand == null || !hand.Valid || computer == null) return new Vector3(10000f, 10000f, 10000f);
            if (_indexTipM == null) _indexTipM = AccessTools.Method(typeof(VRPlugin), "IndexTip", new[] { typeof(Hand) });
            if (_indexTipM == null) return new Vector3(10000f, 10000f, 10000f);
            var tip = (Vector3)_indexTipM.Invoke(vr, new object[] { hand });
            if (tip.x > 9000f) return tip;
            return computer.InverseTransformPoint(tip);
        } catch { return new Vector3(10000f, 10000f, 10000f); }
    }

    private static void SendKey(VRPlugin vr, byte vk, bool down)
    {
        try {
            if (_setKeyM == null)
                _setKeyM = AccessTools.Method(typeof(VRPlugin), "SetComputerKey", new[] { typeof(byte), typeof(bool) });
            _setKeyM?.Invoke(vr, new object[] { vk, down });
        } catch { }
    }

    private static void PressKey(VRPlugin vr, KeyDef key, Hand hand)
    {
        if (key.Vk == VkShift) {                       // latches for the next key only
            _kbShift = !_kbShift;
            SendKey(vr, VkShift, _kbShift);
            _kbShiftUntil = _kbShift ? Time.unscaledTime + 2f : 0f;
            Say("[keyboard] shift " + (_kbShift ? "on" : "off"));
            return;
        }
        SendKey(vr, key.Vk, true);
        _kbReleases.Add((key.Vk, Time.unscaledTime + .09f));        // Codex's 90ms pulse
        if (key.Also != 0) {
            SendKey(vr, key.Also, true);
            _kbReleases.Add((key.Also, Time.unscaledTime + .09f));
        }
        // Any key clears it, Enter and Space included. Leaving shift held through those
        // turned every Enter into Shift+Enter and every arrow into Shift+Arrow, which the
        // terminal ignores, and the navigation looked broken with nothing in the log.
        if (_kbShift) {
            _kbShift = false;
            _kbShiftUntil = 0f;
            _kbReleases.Add((VkShift, Time.unscaledTime + .10f));   // after the character
        }
        // Codex's own click plays at the computer's pivot at volume .22, which over a
        // streamed headset is easy to miss entirely. Play one at the finger instead, with
        // a volume that can be turned up, and keep Codex's as well.
        KeyClick(vr, hand);
        try {
            if (_clickM == null) _clickM = AccessTools.Method(typeof(VRPlugin), "PlayKeyboardClick", Type.EmptyTypes);
            _clickM?.Invoke(vr, null);
            if (_pulseM == null) _pulseM = AccessTools.Method(typeof(VRPlugin), "InteractionPulse", new[] { typeof(Hand), typeof(bool) });
            if (hand != null) _pulseM?.Invoke(vr, new object[] { hand, true });
        } catch { }
        if (_kbLogs++ < 60) Say("[keyboard] " + key.Label);
    }

    private static AudioSource _keySource;
    private static AudioClip _keyClip;
    private static bool _keySoundTried;

    // A short mechanical click: a sharp noise transient for the switch, a lower thud for
    // the key bottoming out. Positioned at the fingertip so it reads as your own hand.
    private static void KeyClick(VRPlugin vr, Hand hand)
    {
        float volume = _pKeyClickVolume == null ? .8f : _pKeyClickVolume.Value;
        if (volume <= 0f) return;
        try {
            if (!_keySoundTried) {
                _keySoundTried = true;
                const int rate = 44100;
                var rnd = new System.Random(8112026);
                _keyClip = BuildClip("ClaudeFixes key", (int)(rate * .07f), rate, i => {
                    float t = i / (float)rate;
                    float snap = Mathf.Exp(-t * 220f) * (.75f * ((float)rnd.NextDouble() * 2f - 1f)
                                                       + .25f * Mathf.Sin(t * 2f * Mathf.PI * 2200f));
                    float thud = t > .012f ? Mathf.Exp(-(t - .012f) * 110f) * .35f
                                             * Mathf.Sin((t - .012f) * 2f * Mathf.PI * 420f) : 0f;
                    return (snap + thud) * Mathf.Min(1f, t * 6000f);
                });
                var go = new GameObject("ClaudeFixes_KeyClick");
                UnityEngine.Object.DontDestroyOnLoad(go);
                _keySource = go.AddComponent(Il2CppInterop.Runtime.Il2CppType.Of<AudioSource>()).Cast<AudioSource>();
                _keySource.playOnAwake = false;
                _keySource.spatialBlend = .5f;      // mostly positional, still audible
                _keySource.minDistance = .4f;
                _keySource.maxDistance = 6f;
                _keySource.dopplerLevel = 0f;
            }
            if (_keySource == null || _keyClip == null) return;
            if (hand != null && hand.Valid && hand.HandNode != null)
                _keySource.transform.position = hand.HandNode.transform.position;
            _keySource.PlayOneShot(_keyClip, Mathf.Clamp01(volume));
        } catch (Exception e) {
            _keySource = null;
            if (_kbLogs++ < 4) I?.LoggerInstance.Warning("[keyboard] click sound off: " + e.Message);
        }
    }

    private static void ReleaseDueKeys(VRPlugin vr)
    {
        // Belt and braces: a shift nobody used expires by itself rather than quietly
        // breaking Enter and the arrows for the rest of the session.
        if (_kbShift && _kbShiftUntil > 0f && Time.unscaledTime > _kbShiftUntil) {
            _kbShift = false; _kbShiftUntil = 0f;
            SendKey(vr, VkShift, false);
            Say("[keyboard] shift timed out and released");
        }
        for (int i = _kbReleases.Count - 1; i >= 0; i--) {
            if (Time.unscaledTime < _kbReleases[i].at) continue;
            SendKey(vr, _kbReleases[i].vk, false);
            _kbReleases.RemoveAt(i);
        }
    }

    private static void ReleaseAllKeys(VRPlugin vr)
    {
        for (int i = 0; i < _kbReleases.Count; i++) SendKey(vr, _kbReleases[i].vk, false);
        _kbReleases.Clear();
        if (_kbShift) { SendKey(vr, VkShift, false); _kbShift = false; _kbShiftUntil = 0f; }
    }

    // Both grips together for half a second starts calibration; A marks each key. Both
    // read straight off the pad, because the Hand flags are not reliable at a terminal.
    private static bool KbCalibrate(VRPlugin vr, Transform computer)
    {
        bool bothGrip = ReadPad(true, out var lp) && (lp.ulButtonPressed & 4UL) != 0
                     && ReadPad(false, out var rp) && (rp.ulButtonPressed & 4UL) != 0;
        if (bothGrip && _kbCalStep < 0 && Time.unscaledTime >= _kbCalBlockUntil) {
            if (_kbCalHeldSince < 0f) _kbCalHeldSince = Time.unscaledTime;
            // Half a second is fine when there is nothing to lose, but once a working
            // frame exists an accidental squeeze must not be able to replace it.
            float need = _kbReady ? 3f : .5f;
            if (Time.unscaledTime - _kbCalHeldSince > need) {
                _kbCalStep = 0; _kbCalHeldSince = -1f; _kbCalAWas = true;
                Say("[keyboard] calibration: LET GO OF THE GRIPS, then touch Q with your index tip and tap A. Then P, then Z.");
            }
        } else if (!bothGrip) _kbCalHeldSince = -1f;
        _kbBothGripWas = bothGrip;
        if (_kbCalStep < 0) return false;

        // Take the mark from A or the trigger, on either controller. Which physical pad
        // answers to "right" depends on the dominant-hand swap inside ControllerIndex, and
        // a calibration that silently ignores the button is worse than one that is generous.
        bool haveL = ReadPad(true, out var ls2), haveR = ReadPad(false, out var rs);
        ulong lb = haveL ? ls2.ulButtonPressed : 0UL, rb = haveR ? rs.ulButtonPressed : 0UL;
        // A only. c77 also accepted the trigger, and a trigger pull while reaching for
        // the next key marked it instantly at the previous key's position: Q and P came
        // back identical to the millimetre.
        bool aLeft  = (lb & 0x80UL) != 0;
        bool aRight = (rb & 0x80UL) != 0;
        bool a = aLeft || aRight;

        // Say what the pads are actually reporting, so a button that never arrives is
        // visible rather than guessed at.
        if (Time.unscaledTime >= _kbCalDiagNext) {
            _kbCalDiagNext = Time.unscaledTime + 1f;
            if (_kbLogs++ < 40)
                Say("[keyboard] marking " + _kbCalNames[Mathf.Clamp(_kbCalStep, 0, 2)]
                    + "; pads L=0x" + lb.ToString("X") + " R=0x" + rb.ToString("X")
                    + " mark=" + a);
        }

        if (a && !_kbCalAWas && Time.unscaledTime >= _kbCalReadyAt) {
            // The marking finger is the right index, as Codex's own calibration has it.
            // c77 took the hand OPPOSITE the button instead, so pressing A with the right
            // controller read the left fingertip: a hand resting on a controller, which
            // is why every mark landed in the same place.
            var rightTip = TipLocal(vr, computer, _handRRef?.Invoke(vr));
            var leftTip  = TipLocal(vr, computer, _handLRef?.Invoke(vr));
            var local = rightTip.x < 9000f ? rightTip : leftTip;
            Say("[keyboard] mark attempt: right" + (rightTip.x < 9000f ? rightTip.ToString("F3") : "(untracked)")
                + " left" + (leftTip.x < 9000f ? leftTip.ToString("F3") : "(untracked)"));
            // Two keys cannot share a spot. A mark on top of the last one is a stray
            // press, not a key, so refuse it rather than quietly wrecking the frame.
            bool tooClose = false;
            for (int i = 0; i < _kbCalStep; i++)
                if (local.x < 9000f && Vector3.Distance(local, _kbCalPoints[i]) < .02f) tooClose = true;

            if (tooClose) {
                Say("[keyboard] that is the same spot as the last mark; move your fingertip"
                    + " onto " + _kbCalNames[_kbCalStep] + " and tap A again");
            } else if (local.x < 9000f) {
                _kbCalPoints[_kbCalStep++] = local;
                _kbCalReadyAt = Time.unscaledTime + .4f;        // no double-takes
                if (_kbCalStep >= 3) {
                    _kbCalStep = -1;
                    _kbCalHeldSince = -1f;
                    try { if (_codexCalRef != null) _codexCalRef(vr) = -1; } catch { }
                    _kbCalBlockUntil = Time.unscaledTime + 3f;   // don't instantly restart
                    BuildKeyFrame();
                    if (_kbReady) {
                        SaveKeyFrame();
                        try { System.IO.File.WriteAllText(KbFile("shared"), System.IO.File.ReadAllText(KbFile(_kbProfile))); } catch { }
                    }
                } else Say("[keyboard] now touch " + _kbCalNames[_kbCalStep] + " and tap A");
            } else Say("[keyboard] neither fingertip is tracked; try again");
        }
        _kbCalAWas = a;
        return true;
    }

    private static void FullKeyboard(VRPlugin vr)
    {
        if (_pFullKeyboard == null || !_pFullKeyboard.Value) return;
        try {
            if (_keyboardMode == null || !_keyboardMode(vr) || !Application.isFocused) {
                if (_kbReleases.Count > 0 || _kbShift) ReleaseAllKeys(vr);
                _kbModsCleared = false;        // clear them again next time in
                // Computer mode flickers off for a frame here and there, and throwing the
                // calibration away on that is why it never got past the first key.
                if (_kbCalStep >= 0 && Time.unscaledTime - _kbCalLive > 2f) _kbCalStep = -1;
                return;
            }
            _kbCalLive = Time.unscaledTime;
            // keybd_event sets modifiers for the whole of Windows, not just the game, so
            // a Shift left down by a crash or an earlier session is still down now and
            // turns every Enter into Shift+Enter. Clear them on the way in, every time.
            if (!_kbModsCleared) {
                _kbModsCleared = true;
                foreach (byte mod in new byte[] { 0x10, 0x11, 0x12, 0xA0, 0xA1 })
                    SendKey(vr, mod, false);
                _kbShift = false; _kbShiftUntil = 0f;
                Say("[keyboard] cleared any stuck shift, ctrl and alt on entering the terminal");
            }

            // My calibration gesture is both grips plus A. Codex starts its own six-key
            // calibration on left grip plus A, so every mark I took also started his, and
            // it then waits at step 0 forever. While it waits, _calibrationConsumesA stays
            // true, and Codex's own Enter line refuses to fire the A button because of it.
            // That is what killed Enter, from the first time this was ever calibrated.
            if (_codexCalRef == null) _codexCalRef = SafeField<int>("_keyCalibration");
            if (_codexCalRef != null) {
                bool codexWaiting = _codexCalRef(vr) >= 0;
                if (codexWaiting && _kbCalStep < 0) {
                    if (_codexCalStuck <= 0f) _codexCalStuck = Time.unscaledTime;
                    else if (Time.unscaledTime - _codexCalStuck > 4f) {
                        _codexCalRef(vr) = -1;
                        _codexCalStuck = 0f;
                        Say("[keyboard] cancelled Codex's half-started six-key calibration; it was holding the A button off Enter");
                    }
                } else _codexCalStuck = 0f;
            }

            if (_activeComputerRef == null) _activeComputerRef = SafeField<Transform>("_activeComputer");
            if (_computerProfileRef == null) _computerProfileRef = SafeField<string>("_computerProfileId");
            var computer = _activeComputerRef?.Invoke(vr);
            if (computer == null) return;

            string profile = "unknown";
            try { profile = _computerProfileRef?.Invoke(vr) ?? "unknown"; } catch { }
            if (profile != _kbProfile) {
                _kbProfile = profile;
                _kbReady = false;
                if (!LoadKeyFrame(profile))
                    Say("[keyboard] no key frame yet for " + profile
                        + "; hold both grips half a second to calibrate Q, P and Z");
            }

            ReleaseDueKeys(vr);
            if (KbCalibrate(vr, computer)) return;      // marking points, not typing
            if (!_kbReady || _keys == null) return;

            var left = TipLocal(vr, computer, _handLRef?.Invoke(vr));
            var right = TipLocal(vr, computer, _handRRef?.Invoke(vr));
            float pitch = Mathf.Max(.004f, _kbAcross.magnitude);
            float press = pitch * (_pKeyPressFrac == null ? .55f : _pKeyPressFrac.Value);
            float arm   = pitch * (_pKeyArmFrac == null ? 1.15f : _pKeyArmFrac.Value);

            // Placed keys and no presses can mean two opposite things: the keys are in the
            // wrong place, or there are no fingertips to find because the hands are not
            // being posed. Say which, once a second, rather than guess again.
            if (Time.unscaledTime >= _kbReachNext) {
                _kbReachNext = Time.unscaledTime + 1f;
                if (_kbLogs < 50) {
                    float closest = float.MaxValue; string near = "none";
                    foreach (var k in _keys) {
                        float dl = left.x  > 9000f ? float.MaxValue : Vector3.Distance(left,  k.Local);
                        float dr = right.x > 9000f ? float.MaxValue : Vector3.Distance(right, k.Local);
                        float d = Mathf.Min(dl, dr);
                        if (d < closest) { closest = d; near = k.Label; }
                    }
                    _kbLogs++;
                    Say("[keyboard] tips L=" + (left.x  > 9000f ? "untracked" : left.ToString("F2"))
                      + " R=" + (right.x > 9000f ? "untracked" : right.ToString("F2"))
                      + "; nearest key " + near + " at "
                      + (closest > 9000f ? "n/a" : (closest * 100f).ToString("0") + "cm")
                      + "; press at " + (press * 100f).ToString("0") + "cm");
                }
            }

            KeyDef nearest = null; float best = float.MaxValue; Hand which = null;
            foreach (var key in _keys) {
                float dl = left.x  > 9000f ? float.MaxValue : Vector3.Distance(left,  key.Local);
                float dr = right.x > 9000f ? float.MaxValue : Vector3.Distance(right, key.Local);
                float d = Mathf.Min(dl, dr);
                if (d > arm * key.Reach) { key.Armed = true; continue; }   // lifted clear: ready again
                if (d < best / Mathf.Max(.01f, key.Reach)) { best = d; nearest = key; which = dl <= dr ? _handLRef?.Invoke(vr) : _handRRef?.Invoke(vr); }
            }
            // Only the nearest key is eligible, so a fingertip spanning two keys picks one.
            if (nearest != null && nearest.Armed && best <= press * nearest.Reach) {
                nearest.Armed = false;
                PressKey(vr, nearest, which);
            }
        } catch (Exception e) {
            if (_kbLogs++ < 6) I?.LoggerInstance.Warning("[keyboard] " + e.Message);
        }
    }


    // ---------- quality / performance mode ----------
    // Row 3 of the comfort menu is Foveation, which is already the performance knob, so
    // the mode lives there rather than inventing a row Codex's selection logic knows
    // nothing about. Most of a mode applies instantly: shading rate, shadows, LOD bias,
    // lights, filtering, culling width. Resolution is the exception, because Codex reads
    // ResolutionScale once when OpenVR comes up and sizes the eye targets from it, so that
    // part is written to the config and takes effect on the next launch. The row says so.
    private static MelonPreferences_Entry<int> _pRenderMode;
    private static MelonPreferences_Entry<float> _pQualityRes, _pPerfRes;
    private static bool _modeCaptured, _modeAWas, _modeApplied;
    private static float _qShadowDist, _qLodBias;
    private static int _qPixelLights;
    private static AnisotropicFiltering _qAniso;
    private static int _renderModeLogs;
    private static float _postNext;

    private static string ModeName => (_pRenderMode?.Value ?? 0) == 0 ? "Quality" : "Performance";

    private static void CaptureQuality()
    {
        if (_modeCaptured) return;
        _modeCaptured = true;
        try {
            _qShadowDist = QualitySettings.shadowDistance;
            _qLodBias = QualitySettings.lodBias;
            _qPixelLights = QualitySettings.pixelLightCount;
            _qAniso = QualitySettings.anisotropicFiltering;
        } catch { }
    }

    private static void ApplyRenderMode(VRPlugin vr, bool announce)
    {
        if (_pRenderMode == null) return;
        CaptureQuality();
        bool perf = _pRenderMode.Value != 0;
        try {
            if (perf) {
                QualitySettings.shadowDistance = Mathf.Max(8f, _qShadowDist * .4f);
                QualitySettings.lodBias = Mathf.Max(.4f, _qLodBias * .6f);
                QualitySettings.pixelLightCount = Mathf.Max(1, _qPixelLights - 2);
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
            } else {
                QualitySettings.shadowDistance = _qShadowDist;
                QualitySettings.lodBias = _qLodBias;
                QualitySettings.pixelLightCount = _qPixelLights;
                QualitySettings.anisotropicFiltering = _qAniso;
            }
        } catch { }

        // Hardware shading rate: Codex's own setting, driven here so the two agree.
        try {
            var vrs = AccessTools.Field(typeof(VRPlugin), "_pNativeVrs")?.GetValue(vr) as MelonPreferences_Entry<bool>;
            var strong = AccessTools.Field(typeof(VRPlugin), "_pExtraStrongVrs")?.GetValue(vr) as MelonPreferences_Entry<bool>;
            if (vrs != null) vrs.Value = perf;
            if (strong != null) strong.Value = perf;
        } catch { }

        // The measured frame says this is where the time goes. Stage 5 is GL.Flush plus
        // the eye submits, and it costs 10-12ms of a 28ms frame, which is GPU drain
        // showing up as CPU wall time. Codex's RenderWithoutBeautify already drops
        // Beautify for the eye render, but PostProcessLayer and SSAO are only bypassed on
        // the casino's Slot Cam, so on the main camera they run per eye at full eye
        // resolution, twice a frame. That is the biggest per-pixel cost left.
        SetHeavyPost(!perf);

        if (_pCullExtraDeg != null) _pCullExtraDeg.Value = perf ? 45f : 70f;
        if (_pKeepRoomsRadius != null) _pKeepRoomsRadius.Value = perf ? 26f : 35f;

        // Takes effect on the next launch, because the eye targets are sized at startup.
        try {
            var res = AccessTools.Field(typeof(VRPlugin), "_pResScale")?.GetValue(vr) as MelonPreferences_Entry<float>;
            if (res != null) res.Value = perf ? (_pPerfRes?.Value ?? .65f) : (_pQualityRes?.Value ?? .9f);
        } catch { }

        try { MelonPreferences.Save(); } catch { }
        _modeApplied = true;
        if (announce && _renderModeLogs++ < 10)
            Say("[mode] " + ModeName + ": shadows " + QualitySettings.shadowDistance.ToString("0")
                + "m, LOD " + QualitySettings.lodBias.ToString("0.00")
                + ", shading rate " + (perf ? "reduced" : "full")
                + ", resolution on next launch");
    }

    private static readonly string[] HeavyPost = { "PostProcessLayer", "ScreenSpaceAmbientOcclusion", "PostProcessVolume" };
    private static readonly Dictionary<int, bool> _postWas = new();

    private static void SetHeavyPost(bool on)
    {
        try {
            foreach (var cam in Camera.allCameras) {
                if (cam == null) continue;
                string name = cam.name ?? "";
                if (!name.Equals("FirstPersonCharacter", StringComparison.OrdinalIgnoreCase)
                 && !name.StartsWith("BPVR_Eye", StringComparison.OrdinalIgnoreCase)) continue;

                foreach (var component in cam.GetComponents<Component>()) {
                    if (component == null) continue;
                    string type;
                    try { type = component.GetIl2CppType().Name; } catch { continue; }
                    if (Array.IndexOf(HeavyPost, type) < 0) continue;
                    var behaviour = component.TryCast<Behaviour>();
                    if (behaviour == null) continue;

                    int id = behaviour.GetInstanceID();
                    if (!_postWas.ContainsKey(id)) _postWas[id] = behaviour.enabled;
                    bool want = on ? _postWas[id] : false;
                    if (behaviour.enabled != want) {
                        behaviour.enabled = want;
                        if (_renderModeLogs++ < 12)
                            Say("[mode] " + type + " on '" + name + "' " + (want ? "on" : "off"));
                    }
                }
            }
        } catch (Exception e) {
            if (_renderModeLogs++ < 6) I?.LoggerInstance.Warning("[mode] post stack: " + e.Message);
        }
    }

    // Watch for A on the mode row and own that press, rather than patching into the middle
    // of Codex's one big input method.
    private static void WatchRenderMode(VRPlugin vr)
    {
        if (_pRenderMode == null) return;
        try {
            if (!_modeApplied) ApplyRenderMode(vr, false);        // keep the world matching the setting
            // Cameras are rebuilt between rooms, so a one-shot disable would quietly undo
            // itself. Reassert twice a second; it is a handful of component reads.
            if (Time.unscaledTime >= _postNext) {
                _postNext = Time.unscaledTime + .5f;
                SetHeavyPost((_pRenderMode?.Value ?? 0) == 0);
            }
            bool open = _comfortOpenRef != null && _comfortOpenRef(vr);
            if (!open) { _modeAWas = false; return; }
            int row = _comfortRow == null ? -1 : _comfortRow(vr);
            bool a = ReadPad(false, out var rs) && (rs.ulButtonPressed & 0x80UL) != 0;
            if (row == 3 && a && !_modeAWas) {
                _pRenderMode.Value = _pRenderMode.Value == 0 ? 1 : 0;
                ApplyRenderMode(vr, true);
                if (_comfortStatusRef != null)
                    _comfortStatusRef(vr) = _pRenderMode.Value == 0
                        ? "Quality: full shading and shadows.\nSmoother image, more reprojection."
                        : "Performance: reduced shading, shorter shadows.\nResolution drops on the next launch.";
            }
            _modeAWas = a;
        } catch { }
    }

    // Codex only learns a terminal is open from a direct hand click:
    //   if (h.DirectClickConsumed) NoteComputerOpened(h.AimCollider);
    // Using an item on the computer, the upgrade disc for instance, goes through the
    // game's own use-item path instead. The game zooms its inspection camera into the
    // screen, Codex never hears about it, so there is no VR stand-off, no fingertip
    // keyboard and no keyboard mode: you get the flat zoom in a headset.
    //
    // So watch for the state that only happens then: the cursor unlocked, and the game
    // camera right up against a computer's click trigger. Then tell Codex what it missed.
    private static MelonPreferences_Entry<bool> _pCatchItemComputer;
    private static MethodInfo _noteComputerM;
    private static float _catchNext;
    private static int _catchLogs;

    private static void CatchItemComputer(VRPlugin vr)
    {
        if (_pCatchItemComputer != null && !_pCatchItemComputer.Value) return;
        if (Time.unscaledTime < _catchNext) return;
        _catchNext = Time.unscaledTime + .25f;
        try {
            if (_keyboardMode != null && _keyboardMode(vr)) return;        // already handled
            if (_inHouseRef == null) _inHouseRef = SafeField<bool>("_inHouse");
            if (_inHouseRef != null && !_inHouseRef(vr)) return;
            // The inspection view unlocks the cursor; ordinary walking around does not.
            if (Cursor.lockState != CursorLockMode.None) return;

            Camera cam = null;
            foreach (var c in Camera.allCameras) {
                if (c == null || !c.isActiveAndEnabled) continue;
                if ((c.name ?? "").Equals("FirstPersonCharacter", StringComparison.OrdinalIgnoreCase)) { cam = c; break; }
            }
            if (cam == null) return;

            Collider best = null; float bestD = float.MaxValue;
            foreach (var col in Physics.OverlapSphere(cam.transform.position, 1.3f, -1, QueryTriggerInteraction.Collide)) {
                if (col == null) continue;
                string n = col.name ?? "";
                if (n.IndexOf("Click Trigger", StringComparison.OrdinalIgnoreCase) < 0) continue;
                // only a trigger that belongs to a computer
                bool isComputer = false;
                for (var t = col.transform; t != null; t = t.parent)
                    if ((t.name ?? "").IndexOf("Computer", StringComparison.OrdinalIgnoreCase) >= 0) { isComputer = true; break; }
                if (!isComputer) continue;
                float d = Vector3.Distance(col.bounds.center, cam.transform.position);
                if (d < bestD) { bestD = d; best = col; }
            }
            if (best == null) return;

            if (_noteComputerM == null)
                _noteComputerM = AccessTools.Method(typeof(VRPlugin), "NoteComputerOpened", new[] { typeof(Collider) });
            if (_noteComputerM == null) {
                if (_catchLogs++ < 2) I?.LoggerInstance.Warning("[computer] NoteComputerOpened not found");
                return;
            }
            _noteComputerM.Invoke(vr, new object[] { best });
            if (_catchLogs++ < 8)
                Say("[computer] opened by using an item, which Codex does not see; terminal mode engaged at "
                    + bestD.ToString("0.0") + "m");
        } catch (Exception e) {
            if (_catchLogs++ < 4) I?.LoggerInstance.Warning("[computer] item-open catch: " + e.Message);
        }
    }

    private static void CullFromHead(VRPlugin vr)
    {
        if (_pCullFollowsHead != null && !_pCullFollowsHead.Value) return;
        try {
            if (_inHouseRef == null) _inHouseRef = SafeField<bool>("_inHouse");
            if (_inHouseRef != null && !_inHouseRef(vr)) return;

            Camera cam = null;
            foreach (var c in Camera.allCameras) {
                if (c == null || !c.isActiveAndEnabled) continue;
                if ((c.name ?? "").Equals("FirstPersonCharacter", StringComparison.OrdinalIgnoreCase)) { cam = c; break; }
            }
            if (cam == null) return;

            // Codex's composition, one frame stale, which costs nothing when the cone is
            // this much wider than the view anyway.
            var body = Quaternion.Euler(0f, cam.transform.eulerAngles.y, 0f);
            float comfort = 0f;
            try {
                if (_comfortYawRef == null) _comfortYawRef = SafeField<float>("_comfortYaw");
                comfort = _comfortYawRef == null ? 0f : _comfortYawRef(vr);
            } catch { }
            var hr = _headRot != null ? _headRot(vr) : Quaternion.identity;
            var world = body * Quaternion.Euler(0f, comfort, 0f) * hr;

            float extra = _pCullExtraDeg == null ? 70f : _pCullExtraDeg.Value;
            var view = Matrix4x4.TRS(cam.transform.position, world, new Vector3(1f, 1f, -1f)).inverse;

            if (extra <= 0f) {
                cam.cullingMatrix = Matrix4x4.Ortho(-9999f, 9999f, -9999f, 9999f, .001f, 9999f) * view;
                return;
            }
            float fov = Mathf.Clamp(cam.fieldOfView + extra, 1f, 170f);
            float aspect = Mathf.Max(.1f, cam.aspect) * 1.8f;   // stereo is wider than it is tall
            cam.cullingMatrix = Matrix4x4.Perspective(fov, aspect, cam.nearClipPlane, cam.farClipPlane) * view;

            if (_headCullLogs++ < 3)
                Say("[culling] culling now follows the head at " + fov.ToString("0")
                    + " degrees, instead of the flat camera at " + cam.fieldOfView.ToString("0"));
        } catch (Exception e) {
            if (_headCullLogs++ < 3) I?.LoggerInstance.Warning("[culling] head culling: " + e.Message);
        }
    }

    private static void WidenCulling(VRPlugin vr)
    {
        if (_pWidenCulling != null && !_pWidenCulling.Value) return;
        try {
            float extra = _pCullExtraFov == null ? 55f : _pCullExtraFov.Value;
            foreach (var cam in Camera.allCameras) {
                if (cam == null || !cam.isActiveAndEnabled) continue;
                string name = cam.name ?? "";
                bool world = name.Equals("FirstPersonCharacter", StringComparison.OrdinalIgnoreCase)
                          || name.StartsWith("BPVR_Eye", StringComparison.OrdinalIgnoreCase);
                if (!world) continue;

                var t = cam.transform;
                // the camera's own pose, not whatever matrix an eye render left behind
                var view = Matrix4x4.TRS(t.position, t.rotation, new Vector3(1f, 1f, -1f)).inverse;

                if (extra <= 0f) {
                    // no culling at all: everything inside a box bigger than the house
                    cam.cullingMatrix = Matrix4x4.Ortho(-9999f, 9999f, -9999f, 9999f, .001f, 9999f) * view;
                    continue;
                }
                float fov = Mathf.Clamp(cam.fieldOfView + extra, 1f, 170f);
                float aspect = Mathf.Max(.1f, cam.aspect) * 1.6f;   // stereo is wider than it is tall
                cam.cullingMatrix = Matrix4x4.Perspective(fov, aspect, cam.nearClipPlane, cam.farClipPlane) * view;

                if (_cullLogs++ < 3)
                    Say("[culling] '" + name + "' culls on " + fov.ToString("0") + " degrees instead of "
                        + cam.fieldOfView.ToString("0") + ", so the headset's view is covered");
            }
        } catch (Exception e) {
            if (_cullLogs++ < 3) I?.LoggerInstance.Warning("[culling] " + e.Message);
        }
    }

    // ---------- following the headset's own recenter ----------
    // The mod holds its own playspace origin:
    //
    //     if (!_haveOrigin) { _origin = _headPos; _haveOrigin = true; }
    //     ... eye = camera.position + (_headPos - _origin)
    //
    // _origin is captured once and only ever moved by Codex's own recenter, held
    // Menu or the VR OPTIONS row. When the headset's system recenter fires, and
    // on a Steam Frame that is the hardware button, SteamVR shifts the whole
    // tracking universe so the player's current pose becomes the new zero.
    // _headPos jumps; _origin does not. The difference between them, which is the
    // player's offset from the game camera, jumps by exactly the amount the
    // runtime just moved the world, so the view does not reset. It lurches, or
    // appears to ignore the button entirely.
    //
    // There is no event wired up to notice this: PollNextEvent exists in Codex's
    // function table as a bare IntPtr with no delegate bound to it, and
    // hand-marshalling VREvent_t means guessing at a struct layout that changes
    // between runtime versions. The discontinuity itself is the better signal. A
    // head cannot move 25cm between two frames; at 30fps that is seven and a half
    // metres a second. So a jump that large is the runtime moving the world, not
    // the player, and the honest response is to move the origin with it.

    private static MelonPreferences_Entry<bool> _pFollowRecenter;
    private static MelonPreferences_Entry<float> _pRecenterJump;
    private static Vector3 _lastHead;
    private static float _lastYaw;
    private static bool _haveLastHead;
    private static int _recenterLogs;

    private static void FollowSystemRecenter(VRPlugin vr)
    {
        if (_pFollowRecenter != null && !_pFollowRecenter.Value) return;
        if (_originRef == null || _headPosRef == null || _headRot == null) return;
        try {
            var head = _headPosRef(vr);
            float yaw = _headRot(vr).eulerAngles.y;
            if (!float.IsFinite(head.x + head.y + head.z)) return;

            if (!_haveLastHead) { _lastHead = head; _lastYaw = yaw; _haveLastHead = true; return; }

            float moved = Vector3.Distance(head, _lastHead);
            float turned = Mathf.Abs(Mathf.DeltaAngle(yaw, _lastYaw));
            _lastHead = head;
            _lastYaw = yaw;

            float limit = Mathf.Max(.05f, _pRecenterJump == null ? .25f : _pRecenterJump.Value);
            if (moved < limit) return;

            // The world moved under the player. Put the origin where their head is
            // now, which is what a recenter means to this mod.
            _originRef(vr) = head;
            if (_recenterLogs++ < 12)
                Say("[recenter] tracking jumped " + (moved * 100f).ToString("0") + "cm"
                    + (turned > 5f ? " and " + turned.ToString("0") + " degrees" : "")
                    + "; following it and moving the origin, so the headset's own recenter resets the view in game too");
        } catch { }
    }

    // ---------- a keyboard you can actually type on ----------
    // Codex wires six keys to the terminals: Enter, Escape and the four arrows,
    // through SetComputerKey -> keybd_event. That is enough to drive a menu and
    // nothing like enough to enter a password, which is what the computers
    // actually ask for. This puts a QWERTY board in front of you whenever a
    // terminal is open, and feeds its keys down the same path, so Codex's own
    // key bookkeeping and keyboard click come along for free.

    // Before any of that can be replaced by pressing the terminal's own keys, we have to
    // know what the terminal's keyboard actually is. Codex's six touch keys are hand
    // calibrated points, not geometry, so nothing in the mod knows whether the keyboard
    // is forty-odd separate key objects or one welded mesh. This writes the hierarchy
    // around the computer to its own file the first time a terminal opens, with a vertex
    // count per object, which is what settles it.
    private static AccessTools.FieldRef<VRPlugin, Transform> _activeComputerRef;
    private static AccessTools.FieldRef<VRPlugin, string> _computerProfileRef;
    private static bool _dumpedKeys, _keyboardWasOpen;

    private static MelonPreferences_Entry<bool> _pTypeBoard;
    private static MelonPreferences_Entry<bool> _pDumpKeys;
    private static MelonPreferences_Entry<float> _pBoardScale;
    private static MelonPreferences_Entry<float> _pBoardDrop;
    private static MelonPreferences_Entry<float> _pBoardReach;
    private static MelonPreferences_Entry<float> _pBoardTilt;

    private sealed class BoardKey
    {
        public byte Vk;
        public string Label;
        public Transform Cap;
        public Renderer Face;
        public Vector3 Rest;
        public bool Lit, Held;
    }

    private static readonly string[] BoardRows =
    {
        "1234567890",
        "QWERTYUIOP",
        "ASDFGHJKL",
        "ZXCVBNM"
    };

    private static GameObject _boardRoot;
    private static readonly List<BoardKey> _boardKeys = new List<BoardKey>();
    private static MethodInfo _setComputerKey, _indexTipOf;
    private static bool _boardShift, _boardTried;
    private static byte _boardSending;
    private static float _boardReleaseAt;
    private static int _boardLogs;
    private static readonly Color BoardFace = new Color(.10f, .11f, .13f);
    private static readonly Color BoardLit = new Color(.42f, .62f, .86f);
    private static readonly Color BoardDown = new Color(.85f, .76f, .38f);

    private static Transform BoardPart(Transform parent, string name, Vector3 local, Vector3 size, Color colour, out Renderer face)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "BPVR_" + name;
        var collider = go.GetComponent<Collider>();
        if (collider != null) UnityEngine.Object.Destroy(collider);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = local;
        go.transform.localScale = size;
        face = go.GetComponent<Renderer>();
        var shader = Shader.Find("Unlit/Color");
        if (shader != null) face.material = new Material(shader) { color = colour };
        face.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        face.receiveShadows = false;
        return go.transform;
    }

    private static void BoardLabel(Transform parent, string text, float size)
    {
        var go = new GameObject("BPVR_KeyLabel");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, 0f, -.62f);
        go.transform.localRotation = Quaternion.identity;
        // the cap is a flattened cube, so undo its scale for the text
        var s = parent.localScale;
        go.transform.localScale = new Vector3(size / Mathf.Max(.0001f, s.x),
                                              size / Mathf.Max(.0001f, s.y),
                                              size / Mathf.Max(.0001f, s.z));
        var label = go.AddComponent(Il2CppInterop.Runtime.Il2CppType.Of<Il2CppTMPro.TextMeshPro>()).Cast<Il2CppTMPro.TextMeshPro>();
        label.text = text;
        label.fontSize = 3.2f;
        label.alignment = Il2CppTMPro.TextAlignmentOptions.Center;
        label.enableWordWrapping = false;
        label.color = new Color(.90f, .93f, .97f);
        label.rectTransform.sizeDelta = new Vector2(4f, 2f);
        var font = FindTitleFont();
        if (font != null) label.font = font;
    }

    private static void AddBoardKey(byte vk, string label, Vector3 local, float width, float unit)
    {
        var cap = BoardPart(_boardRoot.transform, "Key_" + label, local,
                            new Vector3(unit * width * .92f, unit * .92f, unit * .22f), BoardFace, out var face);
        BoardLabel(cap, label, unit * .42f);
        _boardKeys.Add(new BoardKey { Vk = vk, Label = label, Cap = cap, Face = face, Rest = local });
    }

    private static void BuildTypeBoard()
    {
        if (_boardRoot != null || _boardTried) return;
        _boardTried = true;
        try {
            _boardRoot = new GameObject("BPVR_TypeBoard");
            UnityEngine.Object.DontDestroyOnLoad(_boardRoot);
            float unit = .042f;
            Renderer backing;
            BoardPart(_boardRoot.transform, "Backing", new Vector3(0f, -unit * 1.0f, unit * .14f),
                      new Vector3(unit * 11.6f, unit * 5.9f, unit * .10f), new Color(.03f, .035f, .045f), out backing);

            for (int r = 0; r < BoardRows.Length; r++) {
                string row = BoardRows[r];
                float y = unit * (1.55f - r * 1.05f);
                float x0 = -unit * (row.Length - 1) * .5f + (r == 2 ? unit * .5f : r == 3 ? unit * 1.0f : 0f);
                for (int c = 0; c < row.Length; c++) {
                    char ch = row[c];
                    byte vk = (byte)(char.IsDigit(ch) ? 0x30 + (ch - '0') : 0x41 + (ch - 'A'));
                    AddBoardKey(vk, ch.ToString(), new Vector3(x0 + c * unit * 1.05f, y, 0f), 1f, unit);
                }
            }

            float bottom = unit * (1.55f - 4 * 1.05f);
            AddBoardKey(0x10, "SHIFT", new Vector3(-unit * 4.2f, bottom, 0f), 2.0f, unit);
            AddBoardKey(0x20, "SPACE", new Vector3(-unit * .7f, bottom, 0f), 4.4f, unit);
            AddBoardKey(0x08, "BACK", new Vector3(unit * 2.7f, bottom, 0f), 1.9f, unit);
            AddBoardKey(0x0D, "ENTER", new Vector3(unit * 4.7f, bottom, 0f), 2.0f, unit);

            Say("[keyboard] typing board built: " + _boardKeys.Count + " keys");
        } catch (Exception e) {
            I?.LoggerInstance.Warning("[keyboard] could not build the typing board: " + e.Message);
            _boardRoot = null;
        }
    }

    private static Vector3 BoardTip(VRPlugin vr, bool left)
    {
        try {
            if (_indexTipOf == null)
                _indexTipOf = AccessTools.Method(typeof(VRPlugin), "IndexTip", new[] { typeof(Hand) });
            var hand = left ? _handLRef?.Invoke(vr) : _handRRef?.Invoke(vr);
            if (hand == null || !hand.Valid || _indexTipOf == null) return new Vector3(9999f, 9999f, 9999f);
            return (Vector3)_indexTipOf.Invoke(vr, new object[] { hand });
        } catch { return new Vector3(9999f, 9999f, 9999f); }
    }

    private static void SendBoardKey(VRPlugin vr, BoardKey key)
    {
        try {
            if (_setComputerKey == null)
                _setComputerKey = AccessTools.Method(typeof(VRPlugin), "SetComputerKey", new[] { typeof(byte), typeof(bool) });
            if (_setComputerKey == null) return;

            if (key.Vk == 0x10) {               // shift latches rather than pulses
                _boardShift = !_boardShift;
                _setComputerKey.Invoke(vr, new object[] { (byte)0x10, _boardShift });
                return;
            }
            _setComputerKey.Invoke(vr, new object[] { key.Vk, true });
            _boardSending = key.Vk;
            _boardReleaseAt = Time.unscaledTime + .05f;
            if (_boardShift && key.Vk != 0x0D && key.Vk != 0x08 && key.Vk != 0x20) {
                _boardShift = false;            // one shot, like a real shift for one letter
                _boardReleaseAt = Time.unscaledTime + .06f;
            }
            if (_boardLogs++ < 30) Say("[keyboard] typed " + key.Label);
        } catch { }
    }

    // One pass over everything within reach of the terminal, written to its own file.
    // Transforms rather than Renderers, because a key could be a bare transform with the
    // mesh on a child, and a vertex count per object, because that is the tell: forty
    // objects of a hundred verts each is a keyboard with pressable keys, one object of
    // four thousand is a welded mesh that has to be gridded from three calibrated corners.
    private static void DumpTerminalKeys(VRPlugin vr)
    {
        if (_pDumpKeys != null && !_pDumpKeys.Value) return;
        if (_keyboardMode == null) return;

        bool open = _keyboardMode(vr);
        bool entered = open && !_keyboardWasOpen;
        _keyboardWasOpen = open;
        if (!entered || _dumpedKeys) return;

        try {
            // keybd_event sets modifiers for the whole of Windows, not just the game, so
            // a Shift left down by a crash or an earlier session is still down now and
            // turns every Enter into Shift+Enter. Clear them on the way in, every time.
            if (!_kbModsCleared) {
                _kbModsCleared = true;
                foreach (byte mod in new byte[] { 0x10, 0x11, 0x12, 0xA0, 0xA1 })
                    SendKey(vr, mod, false);
                _kbShift = false; _kbShiftUntil = 0f;
                Say("[keyboard] cleared any stuck shift, ctrl and alt on entering the terminal");
            }

            // My calibration gesture is both grips plus A. Codex starts its own six-key
            // calibration on left grip plus A, so every mark I took also started his, and
            // it then waits at step 0 forever. While it waits, _calibrationConsumesA stays
            // true, and Codex's own Enter line refuses to fire the A button because of it.
            // That is what killed Enter, from the first time this was ever calibrated.
            if (_codexCalRef == null) _codexCalRef = SafeField<int>("_keyCalibration");
            if (_codexCalRef != null) {
                bool codexWaiting = _codexCalRef(vr) >= 0;
                if (codexWaiting && _kbCalStep < 0) {
                    if (_codexCalStuck <= 0f) _codexCalStuck = Time.unscaledTime;
                    else if (Time.unscaledTime - _codexCalStuck > 4f) {
                        _codexCalRef(vr) = -1;
                        _codexCalStuck = 0f;
                        Say("[keyboard] cancelled Codex's half-started six-key calibration; it was holding the A button off Enter");
                    }
                } else _codexCalStuck = 0f;
            }

            if (_activeComputerRef == null) _activeComputerRef = SafeField<Transform>("_activeComputer");
            if (_computerProfileRef == null) _computerProfileRef = SafeField<string>("_computerProfileId");
            var computer = _activeComputerRef?.Invoke(vr);
            if (computer == null) { Say("[keys] terminal open but _activeComputer is null, nothing to dump"); return; }

            string profile = "unknown";
            try { profile = _computerProfileRef?.Invoke(vr) ?? "unknown"; } catch { }

            // Codex's own room walk: up until a prefab clone, so sibling visual geometry
            // is included. The functional Computer object holds only click blockers.
            Transform room = computer;
            for (int i = 0; i < 12 && room.parent != null && !room.name.Contains("(Clone)"); i++) room = room.parent;

            var lines = new List<string>();
            lines.Add("terminal " + profile);
            lines.Add("computer '" + computer.name + "' at " + computer.position.ToString("F4")
                      + " rotation " + computer.eulerAngles.ToString("F2"));
            lines.Add("room '" + room.name + "'");
            lines.Add("columns: verts | name | localToComputer | worldPos | path");
            lines.Add("");

            int kept = 0, skipped = 0;
            foreach (var t in room.GetComponentsInChildren<Transform>(true)) {
                if (t == null) continue;
                float away = Vector3.Distance(t.position, computer.position);
                if (away > 1.6f) { skipped++; continue; }
                if (kept++ >= 900) break;

                int verts = -1;
                try {
                    var mf = t.GetComponent<MeshFilter>();
                    var mesh = mf == null ? null : mf.sharedMesh;
                    if (mesh != null) verts = mesh.vertexCount;
                } catch { }

                string path = t.name;
                for (var p = t.parent; p != null && p != room; p = p.parent) path = p.name + "/" + path;

                lines.Add((verts < 0 ? "-" : verts.ToString()) + " | " + t.name
                          + " | " + computer.InverseTransformPoint(t.position).ToString("F4")
                          + " | " + t.position.ToString("F4")
                          + " | " + path);
            }
            lines.Add("");
            lines.Add(kept + " objects within 1.6m, " + skipped + " further away and skipped");

            string dir = System.IO.Path.Combine(Application.dataPath, "..", "UserData");
            string file = System.IO.Path.Combine(dir, "ClaudeFixes-keys-" + profile + ".txt");
            System.IO.File.WriteAllLines(file, lines.ToArray());
            _dumpedKeys = true;
            I?.LoggerInstance.Msg("[keys] wrote " + kept + " objects around the " + profile
                                  + " terminal to UserData/ClaudeFixes-keys-" + profile + ".txt");
        } catch (Exception e) {
            I?.LoggerInstance.Warning("[keys] dump failed: " + e.Message);
            _dumpedKeys = true;
        }
    }

    private static void UpdateTypeBoard(VRPlugin vr)
    {
        if (_pTypeBoard != null && !_pTypeBoard.Value) return;
        try {
            bool open = _keyboardMode != null && _keyboardMode(vr);
            if (!open) {
                if (_boardRoot != null && _boardRoot.activeSelf) {
                    _boardRoot.SetActive(false);
                    if (_boardShift && _setComputerKey != null) {
                        _setComputerKey.Invoke(vr, new object[] { (byte)0x10, false });
                        _boardShift = false;
                    }
                }
                return;
            }

            BuildTypeBoard();
            if (_boardRoot == null) return;

            // release the previous keystroke
            if (_boardSending != 0 && Time.unscaledTime >= _boardReleaseAt) {
                if (_setComputerKey != null) _setComputerKey.Invoke(vr, new object[] { _boardSending, false });
                _boardSending = 0;
            }

            // park it below the screen, square to the player, tilted up to meet the hands
            if (!_boardRoot.activeSelf) _boardRoot.SetActive(true);
            var eye = _headPosRef != null ? _headPosRef(vr) : Vector3.zero;
            var rot = _headRot != null ? _headRot(vr) : Quaternion.identity;
            float yaw = rot.eulerAngles.y;
            var flat = Quaternion.Euler(0f, yaw, 0f);
            float reach = _pBoardReach == null ? .50f : _pBoardReach.Value;
            float drop = _pBoardDrop == null ? .34f : _pBoardDrop.Value;
            float tilt = _pBoardTilt == null ? 42f : _pBoardTilt.Value;
            var want = eye + flat * new Vector3(0f, -drop, reach);
            _boardRoot.transform.position = Vector3.Lerp(_boardRoot.transform.position, want,
                                                         Mathf.Clamp01(Time.unscaledDeltaTime * 6f));
            _boardRoot.transform.rotation = Quaternion.Slerp(_boardRoot.transform.rotation,
                                                             flat * Quaternion.Euler(tilt, 0f, 0f),
                                                             Mathf.Clamp01(Time.unscaledDeltaTime * 6f));
            float scale = Mathf.Clamp(_pBoardScale == null ? 1f : _pBoardScale.Value, .4f, 2.5f);
            _boardRoot.transform.localScale = Vector3.one * scale;

            // fingertips, and the right hand's ray as a fallback for pointing
            var tipR = BoardTip(vr, false);
            var tipL = BoardTip(vr, true);
            var hand = _handRRef?.Invoke(vr);
            bool trigger = hand != null && hand.Valid && hand.TriggerDown;

            BoardKey aimed = null; float aimedDot = float.MaxValue;
            if (hand != null && hand.Valid && hand.AimDirection.sqrMagnitude > .5f) {
                var plane = new Plane(-_boardRoot.transform.forward, _boardRoot.transform.position);
                var ray = new Ray(hand.AimOrigin, hand.AimDirection);
                if (plane.Raycast(ray, out float enter) && enter < 3f) {
                    var hit = _boardRoot.transform.InverseTransformPoint(ray.GetPoint(enter));
                    foreach (var key in _boardKeys) {
                        var d = hit - key.Rest;
                        var half = key.Cap.localScale * .5f;
                        if (Mathf.Abs(d.x) > half.x || Mathf.Abs(d.y) > half.y) continue;
                        float score = Mathf.Abs(d.x) + Mathf.Abs(d.y);
                        if (score < aimedDot) { aimedDot = score; aimed = key; }
                    }
                }
            }

            foreach (var key in _boardKeys) {
                if (key.Cap == null || key.Face == null) continue;
                var local = key.Cap.localPosition;

                // poke: either fingertip inside the cap's footprint and through its face
                bool poke = false;
                for (int side = 0; side < 2 && !poke; side++) {
                    var tip = side == 0 ? tipR : tipL;
                    if (tip.x > 9000f) continue;
                    var d = _boardRoot.transform.InverseTransformPoint(tip) - key.Rest;
                    var half = key.Cap.localScale * .5f;
                    if (Mathf.Abs(d.x) > half.x || Mathf.Abs(d.y) > half.y) continue;
                    poke = d.z > -half.z * 1.2f && d.z < half.z + .028f / Mathf.Max(.0001f, scale);
                }

                bool wants = poke || (aimed == key && trigger);
                bool lit = poke || aimed == key || (key.Vk == 0x10 && _boardShift);

                if (wants && !key.Held) { key.Held = true; SendBoardKey(vr, key); }
                else if (!wants && key.Held) key.Held = false;

                var colour = key.Held ? BoardDown : lit ? BoardLit : BoardFace;
                if (key.Lit != lit || key.Held) {
                    key.Lit = lit;
                    try { key.Face.material.color = colour; } catch { }
                }
                key.Cap.localPosition = key.Held ? key.Rest + new Vector3(0f, 0f, key.Cap.localScale.z * .35f) : key.Rest;
            }
        } catch (Exception e) {
            if (_boardLogs++ < 4) I?.LoggerInstance.Warning("[keyboard] typing board: " + e.Message);
        }
    }

    // ---------- the recorder makes a noise ----------
    // Two clips, both generated here rather than shipped as files: a mechanical
    // key click, and a tape hiss loop for while it is running. The reels already
    // turn, Codex does that; this is what they should sound like.

    private static MelonPreferences_Entry<float> _pClickVolume;
    private static MelonPreferences_Entry<float> _pHissVolume;
    private static MelonPreferences_Entry<float> _pIndexPressDeg;

    private static AudioSource _hissSource, _clickSource;
    private static AudioClip _hissClip, _clickClip;
    private static bool _soundTried, _recWas;
    private static int _soundLogs;

    private static AudioClip BuildClip(string name, int samples, int rate, Func<int, float> sample)
    {
        var data = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<float>(samples);
        for (int i = 0; i < samples; i++) data[i] = Mathf.Clamp(sample(i), -1f, 1f);
        var clip = AudioClip.Create(name, samples, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static void BuildRecorderSound(Transform recorder)
    {
        if (_soundTried || recorder == null) return;
        _soundTried = true;
        try {
            const int rate = 44100;
            var rnd = new System.Random(20261009);

            // A key going down: the snap of the detent, a short body knock, and the
            // broadband tick of plastic on plastic, all decaying inside 60ms.
            _clickClip = BuildClip("BPVR_RecClick", rate / 16, rate, i => {
                float t = i / (float)rate;
                float tick = (float)(rnd.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-t * 260f) * .55f;
                float snap = Mathf.Sin(2f * Mathf.PI * 2300f * t) * Mathf.Exp(-t * 340f) * .45f;
                float body = Mathf.Sin(2f * Mathf.PI * 520f * t) * Mathf.Exp(-t * 95f) * .30f;
                return tick + snap + body;
            });

            // Tape: high-weighted hiss, a little transport rumble, and a slow wow on
            // the level. One-pole filters, and the tail is crossfaded into the head
            // so the loop does not tick.
            int len = rate * 2;
            float hp = 0f, lp = 0f;
            var hiss = new float[len];
            for (int i = 0; i < len; i++) {
                float t = i / (float)rate;
                float white = (float)(rnd.NextDouble() * 2.0 - 1.0);
                lp += (white - lp) * .55f;                      // tame the very top
                hp += (lp - hp) * .04f;                         // and take out the mud
                float tape = (lp - hp) * .5f;
                float rumble = Mathf.Sin(2f * Mathf.PI * 47f * t) * .035f
                             + Mathf.Sin(2f * Mathf.PI * 94f * t) * .018f;
                float wow = 1f + .12f * Mathf.Sin(2f * Mathf.PI * 3.1f * t);
                hiss[i] = (tape + rumble) * wow;
            }
            int fade = rate / 10;
            for (int i = 0; i < fade; i++) {
                float k = i / (float)fade;
                hiss[i] = Mathf.Lerp(hiss[len - fade + i], hiss[i], k);
            }
            _hissClip = BuildClip("BPVR_TapeHiss", len - fade, rate, i => hiss[i]);

            _clickSource = recorder.gameObject.AddComponent<AudioSource>();
            _clickSource.clip = _clickClip;
            _clickSource.playOnAwake = false;
            _clickSource.loop = false;
            _clickSource.spatialBlend = 1f;
            _clickSource.minDistance = .15f;
            _clickSource.maxDistance = 6f;

            _hissSource = recorder.gameObject.AddComponent<AudioSource>();
            _hissSource.clip = _hissClip;
            _hissSource.playOnAwake = false;
            _hissSource.loop = true;
            _hissSource.spatialBlend = 1f;
            _hissSource.minDistance = .15f;
            _hissSource.maxDistance = 4f;

            Say("[recorder] key click and tape hiss built");
        } catch (Exception e) {
            if (_soundLogs++ < 3) I?.LoggerInstance.Warning("[recorder] no sound: " + e.Message);
        }
    }

    private static void UpdateRecorderSound(Transform recorder, bool recording)
    {
        BuildRecorderSound(recorder);
        try {
            if (recording != _recWas) {
                _recWas = recording;
                if (_clickSource != null) {
                    _clickSource.volume = Mathf.Clamp01(_pClickVolume == null ? .55f : _pClickVolume.Value);
                    // down and up are the same mechanism; the release is lighter
                    _clickSource.pitch = recording ? 1f : 1.18f;
                    _clickSource.Play();
                }
            }
            if (_hissSource == null) return;
            float want = recording ? Mathf.Clamp01(_pHissVolume == null ? .22f : _pHissVolume.Value) : 0f;
            _hissSource.volume = Mathf.MoveTowards(_hissSource.volume, want, Time.unscaledDeltaTime * 2.5f);
            if (want > 0f && !_hissSource.isPlaying) _hissSource.Play();
            else if (want <= 0f && _hissSource.volume <= .001f && _hissSource.isPlaying) _hissSource.Stop();
        } catch { }
    }

    // ---------- the recorder moves to the other hand, and sits in the palm ----------
    // Every piece of "which hand" in the recorder code reads _comfort.LeftHanded,
    // so flipping that one boolean on its way into each of them moves the whole
    // thing across: the wrist it is placed on, the side the record key is cut
    // into, and the hand whose digits are posed around it. X opens the quick memo
    // and X is on the off hand, so that is the hand that should be holding it.
    //
    // The shape was the other half of the problem. The recorder root was scaled
    // (0.85, 1.5, 1) and the casing scaled 1.47 back out on X, which left a
    // 6.7 x 17.4 x 2.3 cm slab. A real microcassette recorder is about
    // 6 x 11 x 2.5 cm, so the authored mesh was already right and the scaling was
    // what made it look wrong. It was also parked 12.2 cm out along the finger
    // axis, which is past the fingertips of an 18 cm hand - floating, not held.
    //
    // How these are actually held, one-handed: the back of the casing lies against
    // the palm and the base of the fingers, the long axis up and tilted out so the
    // microphone end points toward the mouth, the middle, ring and little fingers
    // curl around the far edge, the thumb lies along the near edge, and the index
    // comes over the front face onto the record key. That is the pose below: the
    // body in the palm, and RecorderGripPostfix already puts the index on the key.

    private static MelonPreferences_Entry<bool> _pRecorderOffHand;
    private static MelonPreferences_Entry<bool> _pRecorderPalm;
    private static MelonPreferences_Entry<float> _pRecorderIndexShift;
    private static MelonPreferences_Entry<float> _pRecorderGap;
    private static MelonPreferences_Entry<bool> _pRecorderFaceFlip;
    private static MelonPreferences_Entry<bool> _pTapeSideOut;
    private static MelonPreferences_Entry<float> _pGripSkin;
    private static MelonPreferences_Entry<float> _pThumbBack;
    private static MelonPreferences_Entry<float> _pThumbCorner;
    private static MelonPreferences_Entry<float> _pFreeHandCurl;
    private static MelonPreferences_Entry<float> _pKeyShift;
    private static MelonPreferences_Entry<float> _pKeyShift2;
    private static MelonPreferences_Entry<float> _pRecorderAlong;
    private static MelonPreferences_Entry<float> _pRecorderTilt;
    private static MelonPreferences_Entry<float> _pFingerWrap;
    private static MelonPreferences_Entry<float> _pIndexCurl;
    private static MelonPreferences_Entry<float> _pRecorderScale;

    private static bool OffHand => _pRecorderOffHand == null || _pRecorderOffHand.Value;

    // one place decides which hand, so the four call sites cannot disagree
    private static void FlipHandPrefix(ref bool left)
    {
        if (OffHand) left = !left;
    }

    private static void ButtonPosPrefix(ref bool left) { FlipHandPrefix(ref left); }
    private static void ButtonNormalPrefix(ref bool left) { FlipHandPrefix(ref left); }
    private static void ThumbRestPrefix(ref bool left) { FlipHandPrefix(ref left); }

    // Pose's recorderHold is decided at Codex's call site as "the dominant hand",
    // which is now the wrong one. Decide it here from where the recorder is.
    // The pinned relaxed pose that used to live here is gone. It existed to beat
    // RecorderPolish's postfix, which is now stood down, so holding the hand in a
    // fixed shape is just a second wrong pose replacing the first. With nothing
    // fighting over it, the free hand should do what it does in ordinary play:
    // follow the real grip, trigger and thumb off the controller.

    // The one override worth keeping on the free hand. Codex's memo branch calls
    //     Fingers.Drive(h.IsLeft, 1f, 0f, 1f, dt, false, false)
    // with those three values hardcoded: grip fully closed, trigger fully open,
    // thumb fully out. That is not the controller's state, it is a fixed shape,
    // and it is the finger gun. Suppressed for the free hand so the ordinary pose
    // from the real grip and trigger is what shows.
    private static int _driveLogs2;

    private static bool FingersDrivePrefix(bool isLeft)
    {
        if (!OffHand || isLeft) return true;
        try {
            if (_quickMemoRef == null || _vr == null || !_quickMemoRef(_vr)) return true;
            if (_driveLogs2++ < 3) Say("[recorder] free hand left on its own controller pose");
            return false;
        } catch { return true; }
    }

    private static void RecorderHoldPrefix(ref bool left, ref float trigger, ref float thumb, ref bool recorderHold)
    {
        if (!OffHand) return;
        try {
            // Read Codex's flag live rather than a cached copy. The cache is set
            // from OnUpdate and this runs from the render path, so a frame where
            // the two disagree is a frame where the free hand is left as it was.
            bool memo = _quickMemoRef != null && _vr != null && _quickMemoRef(_vr);
            recorderHold = memo && left;           // left == pose side 0 == the off hand

        } catch { }
    }

    // PoseHand freezes a hand solid whenever the quick memo is open:
    //
    //     if (... || (_quickMemo && !h.IsLeft)) {
    //         HideHeldTools();
    //         Fingers.Drive(h.IsLeft, 1f, 0f, 1f, ...);     // grip 1, a closed fist
    //         h.AimCollider = null; ... return;
    //     }
    //
    // That was right when the recorder lived in that hand. It is the wrong hand
    // now, so the hand that is NOT holding anything gets clamped into a fist with
    // no aim, no beam and its held tools hidden. Hide the flag for the length of
    // that one method and the hand behaves exactly as it always did; the recorder
    // hold is driven from _memoOpenNow instead, so it does not notice.

    private static bool _memoOpenNow, _memoStashed;

    private static void PoseHandPrefix()
    {
        if (!OffHand || _quickMemoRef == null || _vr == null) return;
        try {
            _memoStashed = _quickMemoRef(_vr);
            if (!_memoStashed) return;
            _memoOpenNow = true;
            _quickMemoRef(_vr) = false;
        } catch { }
    }

    private static MethodInfo _hideHeldTools;

    private static void PoseHandPostfix()
    {
        if (!OffHand || _quickMemoRef == null || _vr == null) return;
        try {
            if (!_memoStashed) { _memoOpenNow = false; return; }
            _quickMemoRef(_vr) = true;
            _memoStashed = false;
            // Suppressing the flag also stopped Codex calling HideHeldTools while
            // the memo is up, which can leave the free hand posed around a tool it
            // is no longer showing. That part should still happen; the fist, the
            // dead aim and the hidden beam should not.
            if (_hideHeldTools == null) _hideHeldTools = AccessTools.Method(typeof(VRPlugin), "HideHeldTools");
            _hideHeldTools?.Invoke(_vr, null);
        } catch { }
    }

    private static int _placeLogs;

    private static void PlaceRecorderPostfix(object __instance, Transform recorder, bool left)
    {
        if (_pRecorderPalm == null || !_pRecorderPalm.Value) return;
        if (__instance == null || recorder == null) return;
        if (!BindGrip(__instance)) return;
        try {
            int side = left ? 0 : 1;
            var wrists = (Transform[])_fWrists.GetValue(__instance);
            var align = (Quaternion[])_fAlign.GetValue(__instance);
            var bases = (Transform[,])_fOuterBases.GetValue(__instance);
            if (wrists == null || align == null || bases == null || wrists[side] == null) return;

            var wrist = wrists[side];
            // frame is LookRotation(wrist -> middle knuckle, dorsal normal), which is
            // how CelHandPack builds _alignment. forward runs up the hand, up is the
            // back of the hand, so the palm is -up.
            var frame = wrist.rotation * Quaternion.Inverse(align[side]);
            float mirror = left ? 1f : -1f;                       // frame right faces the index on side 0

            var knuckle = bases[side, 2] != null ? bases[side, 2] : bases[side, 0];
            float palm = knuckle != null ? Vector3.Distance(wrist.position, knuckle.position) : .09f;
            if (!float.IsFinite(palm) || palm < .03f || palm > .25f) palm = .09f;

            Vector3 toIndex = frame * Vector3.right * mirror;
            Vector3 intoPalm = -(frame * Vector3.up);
            Vector3 upHand = frame * Vector3.forward;

            // The long axis lies along the knuckle row, angled a little toward the
            // fingertips, with the microphone end projecting past the index.
            float t = Mathf.Deg2Rad * (_pRecorderTilt == null ? 18f : _pRecorderTilt.Value);
            Vector3 longAxis = (toIndex * Mathf.Cos(t) + upHand * Mathf.Sin(t)).normalized;

            float alongFrac = Mathf.Clamp(_pRecorderAlong == null ? .72f : _pRecorderAlong.Value, 0f, 2f);
            if (alongFrac < .25f) alongFrac = .72f;               // a stale metres value, not a choice

            // Size it off the grip itself: the span from the index knuckle to the
            // little one is what the fingers have to close around, and a real
            // microcassette recorder is about a third longer than that span.
            float span = (bases[side, 0] != null && bases[side, 1] != null)
                ? Vector3.Distance(bases[side, 0].position, bases[side, 1].position) : palm * .55f;
            // In the reference photographs the device runs about twice the width of
            // the four knuckles it is held across, sticking well out past both the
            // index and the heel of the hand. 1.45 was the length of the casing
            // alone and left it looking like a toy.
            float scale = Mathf.Clamp(span * 2.1f / .116f, .7f, 2.0f);
            scale *= Mathf.Clamp(_pRecorderScale == null ? 1f : _pRecorderScale.Value, .4f, 2f);
            _lastScale = scale;

            // Clear of the palm: the flesh, plus half the casing's real depth.
            // Clear of the palm: the flesh, plus half the casing's real depth. Both
            // off the knuckle span, because the wrist-to-knuckle "palm" on this rig
            // is 14cm and includes a stretch of forearm.
            float gap = span * .22f + _halfDeep * scale;
            float shift = span * (_pRecorderIndexShift == null ? .07f : _pRecorderIndexShift.Value) * 2f;

            Vector3 centre = wrist.position
                           + upHand * (palm * alongFrac)
                           + intoPalm * gap
                           + toIndex * shift;

            // Held like a phone: the plain back against the fingers, the cassette
            // face looking out at you. Spinning about the long axis rather than
            // flipping the LookRotation keeps the microphone end pointing past the
            // index; it does swap which long edge is the far one, which is why the
            // key edge below is measured rather than assumed.
            var facing = Quaternion.LookRotation(intoPalm, longAxis);
            if (_pTapeSideOut == null || _pTapeSideOut.Value)
                facing = Quaternion.AngleAxis(180f, longAxis) * facing;
            recorder.SetPositionAndRotation(centre, facing);

            // Which local X now points toward the fingertips: that is the edge the
            // fingers come around and the edge the record key belongs on.
            _distalX = Vector3.Dot(recorder.right, upHand) >= 0f ? 1f : -1f;

            recorder.localScale = Vector3.one * scale;
            _recorderLeft = left;

            // Two solid objects. Nothing so far ever stopped the casing occupying
            // the same space as the hand; the pads were already hovering 8 to 18mm
            // clear, so what was sinking in was the body against the palm and the
            // knuckles. Push it out along the palm normal until every one of those
            // bones is on the back side of the box, with a margin for flesh. That
            // lifts it off the palm and brings the front face up to the fingertips
            // in the same move, which is why the pads should close at the same time.
            float hw = _halfWide * scale, hl = _halfLong * scale, hd = _halfDeep * scale;
            float push = 0f;
            for (int bone = 0; bone <= 4; bone++) {
                var b = bone < 4 ? bases[side, bone] : wrist;
                if (b == null) continue;
                Vector3 d = b.position - recorder.position;
                if (Mathf.Abs(Vector3.Dot(d, recorder.right)) > hw + .012f) continue;   // off the ends
                if (Mathf.Abs(Vector3.Dot(d, recorder.up)) > hl + .012f) continue;
                float need = Vector3.Dot(d, intoPalm) + hd + .007f;
                if (need > push) push = need;
            }
            if (push > 0f) recorder.position += intoPalm * Mathf.Min(push, .06f);

            if (_placeLogs++ < 4)
                Say("[recorder] across the " + (left ? "left" : "right") + " hand, palm "
                    + (palm * 100f).ToString("0.0") + "cm, grip " + (palm * alongFrac * 100f).ToString("0.0")
                    + "cm up it, grip span " + (span * 100f).ToString("0.0") + "cm, "
                    + (gap * 100f).ToString("0.0") + "cm off the bone line, mic end "
                    + (shift * 100f).ToString("0.0") + "cm past the index, scaled x" + scale.ToString("0.00"));
        } catch (Exception e) {
            if (_placeLogs++ < 4) I?.LoggerInstance.Warning("[recorder] placement left as Codex has it: " + e.Message);
        }
    }

    // MemoRecorderFrame widens the casing on X and slides it off-centre to keep
    // the old face width. With the stretch gone that offset only skews it, so put
    // the shell back to the mesh it was authored as.
    private static AccessTools.FieldRef<VRPlugin, Transform> _memoShellRef;
    private static AccessTools.FieldRef<VRPlugin, Transform> _memoMountRef;
    private static AccessTools.FieldRef<VRPlugin, Transform> _memoKeyRef;
    private static AccessTools.FieldRef<VRPlugin, float> _memoPressRef;
    private static AccessTools.FieldRef<VRPlugin, bool> _quickMemoRef;
    private static bool _recorderLeft = true;
    private static float _lastScale = 1f;
    private static Vector3 _keyLocal;
    private static bool _keyPinned, _keyGrown;
    private static float _halfWide, _halfLong, _halfDeep;
    private static bool _keyKnown;
    private static int _keyLogs;

    // Local +X of the casing is the proximal long edge for the left hand, so the
    // edge the fingers come around is the other one.
    private static float _distalX;
    private static float DistalX => _distalX != 0f ? _distalX : (_recorderLeft ? -1f : 1f);

    private static void MemoShellPostfix(VRPlugin __instance)
    {
        if (_pRecorderPalm == null || !_pRecorderPalm.Value) return;
        if (_memoShellRef == null) return;
        try {
            var shell = _memoShellRef(__instance);
            if (shell == null) return;
            if (shell.localScale != Vector3.one) shell.localScale = Vector3.one;
            if (shell.localPosition != Vector3.zero) shell.localPosition = Vector3.zero;

            SeatRecordKey(null);
        } catch { }
    }

    // Seat the record key on the casing's own edge. Codex positions it from
    // numbers written for a casing that used to be scaled 1.47 wider and slid
    // sideways; with the casing back to its authored shape those numbers leave it
    // floating a centimetre off the corner, which is what the screenshots showed.
    private static void SeatRecordKey(Transform known)
    {
        try {
            if (_vr == null || _memoShellRef == null || _memoMountRef == null || _memoKeyRef == null) return;
            var shell = _memoShellRef(_vr);
            var mount = _memoMountRef(_vr);
            var key = _memoKeyRef(_vr);
            if (shell == null || mount == null || key == null) return;
            var recorder = known != null ? known : mount.parent;
            if (recorder == null) return;

            if (!_keyKnown) {
                _halfWide = .027f;      // .054 wide
                _halfLong = .058f;      // .116 long
                _halfDeep = .0115f;     // .023 deep
                _keyKnown = true;
                if (_keyLogs++ < 3)
                    Say("[recorder] casing taken as " + (_halfWide * 200f).ToString("0.0") + " x "
                        + (_halfLong * 200f).ToString("0.0") + " x " + (_halfDeep * 200f).ToString("0.0")
                        + "cm from the source, not measured; record key seated on its edge");
            }

            // Recomputed every frame, because turning the recorder swaps which long
            // edge faces the fingers, unless the grip has pinned it under the index.
            if (!_keyPinned)
                _keyLocal = new Vector3(DistalX * (_halfWide + .004f), _halfLong * .35f, -.004f);
            float press = _memoPressRef == null ? 0f : _memoPressRef(_vr);
            var seated = _keyLocal - new Vector3(DistalX * .003f * Mathf.Clamp01(press), 0f, 0f);
            // The dark mount is authored taller and deeper than the red cap, so it
            // frames it and the fingertip ends up on the black surround instead of
            // the button. Sink the mount well in and make the cap the proud part.
            mount.localPosition = _keyLocal - new Vector3(DistalX * .009f, 0f, 0f);
            key.localPosition = seated - mount.localPosition;
            if (!_keyGrown) {
                _keyGrown = true;
                var sz = key.localScale;
                key.localScale = new Vector3(sz.x * 1.6f, sz.y * 1.7f, sz.z * 1.5f);
            }
        } catch { }
    }

    // And the trigger that records: the recorder is in the off hand now, so it is
    // that hand's trigger. Codex passes the dominant controller in as 'tool'.
    private static int _memoTrigLogs;

    private static void QuickMemoInputPrefix(ref bool toolValid, ref VRControllerState_t tool)
    {
        if (!OffHand) return;
        try {
            if (!ReadPad(true, out var off)) return;      // logical left = the off hand
            toolValid = true;
            tool = off;
            if (_memoTrigLogs++ < 4) Say("[recorder] recording from the off-hand trigger, the hand holding it");
        } catch { }
    }

    private void PatchRecorderHand()
    {
        try {
            var type = AccessTools.TypeByName("BluePrinceVR.CelHandPack");
            var self = typeof(Mod);
            if (type != null) {
                PatchOn(type, "PlaceRecorder", new HarmonyMethod(self.GetMethod(nameof(ThumbRestPrefix), BindingFlags.Static | BindingFlags.NonPublic)),
                        new HarmonyMethod(self.GetMethod(nameof(PlaceRecorderPostfix), BindingFlags.Static | BindingFlags.NonPublic)));
                PatchOn(type, "RecorderButtonPosition", new HarmonyMethod(self.GetMethod(nameof(ButtonPosPrefix), BindingFlags.Static | BindingFlags.NonPublic)), null);
                PatchOn(type, "RecorderButtonNormal", new HarmonyMethod(self.GetMethod(nameof(ButtonNormalPrefix), BindingFlags.Static | BindingFlags.NonPublic)), null);
                PatchOn(type, "Pose", new HarmonyMethod(self.GetMethod(nameof(RecorderHoldPrefix), BindingFlags.Static | BindingFlags.NonPublic)), null);
            }
            try {
                var fingers = AccessTools.TypeByName("BluePrinceVR.Fingers");
                var drive = fingers == null ? null : AccessTools.Method(fingers, "Drive");
                if (drive == null) LoggerInstance.Warning("skipped: no Fingers.Drive; the free hand may still be redrawn");
                else {
                    HarmonyInstance.Patch(drive,
                        new HarmonyMethod(self.GetMethod(nameof(FingersDrivePrefix), BindingFlags.Static | BindingFlags.NonPublic)));
                    LoggerInstance.Msg("patched Fingers.Drive");
                }
            } catch (Exception e) { LoggerInstance.Warning("skipped Fingers.Drive: " + e.Message); }
            _memoShellRef = SafeField<Transform>("_memoShell");
            _memoMountRef = SafeField<Transform>("_memoRecordMount");
            _memoKeyRef = SafeField<Transform>("_memoRecordButton");
            _memoPressRef = SafeField<float>("_memoButtonPress");
            _quickMemoRef = SafeField<bool>("_quickMemo");
            Patch("MemoRecorderFrame", Type.EmptyTypes, null,
                self.GetMethod(nameof(MemoShellPostfix), BindingFlags.Static | BindingFlags.NonPublic));
            var memoInput = AccessTools.Method(typeof(VRPlugin), "QuickMemoInput");
            if (memoInput == null) LoggerInstance.Warning("skipped: no QuickMemoInput; the recorder trigger stays on the dominant hand");
            else {
                HarmonyInstance.Patch(memoInput,
                    new HarmonyMethod(self.GetMethod(nameof(QuickMemoInputPrefix), BindingFlags.Static | BindingFlags.NonPublic)));
                LoggerInstance.Msg("patched VRPlugin.QuickMemoInput");
            }
        } catch (Exception e) {
            LoggerInstance.Warning("skipped the recorder hand swap: " + e.Message);
        }
    }

    private void PatchOn(Type type, string name, HarmonyMethod prefix, HarmonyMethod postfix)
    {
        try {
            var method = AccessTools.Method(type, name);
            if (method == null) { LoggerInstance.Warning("skipped: no " + type.Name + "." + name + " in this build"); return; }
            HarmonyInstance.Patch(method, prefix, postfix);
            LoggerInstance.Msg("patched " + type.Name + "." + name);
        } catch (Exception e) {
            LoggerInstance.Warning("skipped " + type.Name + "." + name + ": " + e.Message);
        }
    }

    private void PatchRecorderGrip()
    {
        try {
            var type = AccessTools.TypeByName("BluePrinceVR.CelHandPack");
            if (type == null) { LoggerInstance.Warning("skipped: no CelHandPack, recorder grip unchanged"); return; }
            var method = AccessTools.Method(type, "RestThumbOnRecorder");
            if (method == null) { LoggerInstance.Warning("skipped: no RestThumbOnRecorder, recorder grip unchanged"); return; }
            HarmonyInstance.Patch(method,
                new HarmonyMethod(typeof(Mod).GetMethod(nameof(ThumbRestPrefix), BindingFlags.Static | BindingFlags.NonPublic)),
                new HarmonyMethod(typeof(Mod).GetMethod(nameof(RecorderGripPostfix), BindingFlags.Static | BindingFlags.NonPublic)));
            LoggerInstance.Msg("patched CelHandPack.RestThumbOnRecorder");
        } catch (Exception e) {
            LoggerInstance.Warning("skipped CelHandPack.RestThumbOnRecorder: " + e.Message);
        }
    }

    // ---------- hands that know the lights are off ----------
    // Three separate things are drawn unlit, so nothing in the room can touch
    // them: the cel hand body material, every primitive the recorder is built
    // from ("Unlit/Color" with a flat colour), and the arms when ArmsUnlit is
    // on. Alex keeps the painted cel look, so the fix is not a lit shader: it
    // is to measure the light where he is standing and scale those flat colours
    // by it. Shading stays banded, the hands just go dark with the room.

    private sealed class Tint
    {
        public Material Mat;
        public string Prop;
        public Color Base;
        public Shader OrigShader;
    }

    private static readonly List<Tint> _tints = new List<Tint>();
    private static readonly List<Light> _lights = new List<Light>();
    private static float _lightScanAt, _tintScanAt, _litLevel = -1f;
    private static int _lightLogs;
    private static object _celPack;
    private static bool _tintsRestored = true;

    private static bool WantsTarget(string name)
    {
        string list = _pHandLightTargets?.Value;
        if (string.IsNullOrWhiteSpace(list)) return false;
        foreach (var raw in list.Split('|'))
            if (string.Equals(raw.Trim(), name, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    // Sprites/Default multiplies by the vertex colour stream. The cel hand mesh
    // is built at runtime with no colour channel, so that multiply came out as
    // zero and the hands went flat black however bright the room was. Give the
    // mesh white vertex colours first and the same shader behaves.
    private static bool EnsureWhiteVertexColours(Renderer r)
    {
        try {
            Mesh mesh = null;
            var skinned = r.TryCast<SkinnedMeshRenderer>();
            if (skinned != null) mesh = skinned.sharedMesh;
            else {
                var filter = r.GetComponent<MeshFilter>();
                if (filter != null) mesh = filter.sharedMesh;
            }
            if (mesh == null) return false;
            int count = mesh.vertexCount;
            if (count <= 0) return false;
            if (mesh.colors != null && mesh.colors.Length == count) return true;

            var colours = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<Color>(count);
            for (int i = 0; i < count; i++) colours[i] = Color.white;
            mesh.colors = colours;
            if (_lightLogs++ < 6) Say("[handlight] gave '" + (mesh.name ?? "?") + "' white vertex colours so it can be tinted");
            return true;
        } catch (Exception e) {
            if (_lightLogs++ < 4) I?.LoggerInstance.Warning("[handlight] vertex colours failed: " + e.Message);
            return false;
        }
    }

    private static void AddRenderer(Renderer r)
    {
        if (r == null) return;
        foreach (var m in r.materials) {
            if (m == null) continue;
            bool known = false;
            foreach (var t in _tints) if (t.Mat == m) { known = true; break; }
            if (known) continue;

            // a colour the shader already reads is always preferable to a swap
            string found = null;
            foreach (var prop in new[] { "_Color", "_BaseColor", "_TintColor" })
                if (m.HasProperty(prop)) { found = prop; break; }
            if (found != null) {
                _tints.Add(new Tint { Mat = m, Prop = found, Base = m.GetColor(found), OrigShader = m.shader });
                continue;
            }

            // nothing to dim: move it to the one tintable unlit shader certain
            // to be in the build, but only if the mesh can feed it properly
            var orig = m.shader;
            try {
                if (!EnsureWhiteVertexColours(r)) {
                    if (_lightLogs++ < 6) Say("[handlight] left '" + (m.name ?? "?") + "' alone; no colour to dim and its mesh cannot carry one");
                    continue;
                }
                var sprite = Shader.Find("Sprites/Default");
                if (sprite == null) continue;
                var tex = m.mainTexture;
                m.shader = sprite;
                if (tex != null) m.mainTexture = tex;
                if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 1f);
                m.renderQueue = 2000;
                if (!m.HasProperty("_Color")) { m.shader = orig; continue; }
                _tints.Add(new Tint { Mat = m, Prop = "_Color", Base = Color.white, OrigShader = orig });
                if (_lightLogs++ < 6) Say("[handlight] '" + (m.name ?? "?") + "' had no colour to dim; moved it onto Sprites/Default");
            } catch {
                try { m.shader = orig; } catch { }
            }
        }
    }

    private static void RestoreTints()
    {
        foreach (var t in _tints) {
            if (t.Mat == null) continue;
            try {
                if (t.OrigShader != null && t.Mat.shader != t.OrigShader) t.Mat.shader = t.OrigShader;
                else if (t.Mat.HasProperty(t.Prop)) t.Mat.SetColor(t.Prop, t.Base);
            } catch { }
        }
        _tints.Clear();
        _litLevel = -1f;
        _tintScanAt = 0f;
    }

    private static void CollectTintTargets()
    {
        try {
            if (WantsTarget("hands") && _celPack != null) {
                var field = AccessTools.Field(_celPack.GetType(), "Renderer");
                if (field != null) AddRenderer(field.GetValue(_celPack) as Renderer);
            }
            if (WantsTarget("arms")) {
                var model = HandRig.Model;
                if (model != null) AddRenderer(model.Renderer);
            }
            if (WantsTarget("recorder")) {
                var rec = GameObject.Find("BPVR_MemoRecorder");
                if (rec != null) foreach (var r in rec.GetComponentsInChildren<Renderer>(true)) AddRenderer(r);
            }
        } catch (Exception e) {
            if (_lightLogs++ < 4) I?.LoggerInstance.Warning("[handlight] could not gather materials: " + e.Message);
        }
    }

    private static float SampleLightAt(Vector3 point)
    {
        float level = 0f;
        try {
            var amb = RenderSettings.ambientLight;
            level += (amb.r * .299f + amb.g * .587f + amb.b * .114f) * Mathf.Max(0f, RenderSettings.ambientIntensity);
        } catch { }

        foreach (var light in _lights) {
            if (light == null || !light.isActiveAndEnabled || light.intensity <= 0f) continue;
            float lum;
            try {
                var c = light.color;
                lum = (c.r * .299f + c.g * .587f + c.b * .114f) * light.intensity;
            } catch { continue; }

            try {
                if (light.type == LightType.Directional) { level += lum; continue; }
                Vector3 toLight = light.transform.position - point;
                float d = toLight.magnitude;
                if (light.range > 0f && d > light.range) continue;
                float atten = light.range > 0f ? Mathf.Clamp01(1f - d / light.range) : 1f;
                atten *= atten;
                if (light.type == LightType.Spot) {
                    float angle = Vector3.Angle(light.transform.forward, -toLight.normalized);
                    if (angle > light.spotAngle * .5f) continue;
                    atten *= Mathf.Clamp01(1f - angle / Mathf.Max(1f, light.spotAngle * .5f));
                }
                level += lum * atten;
            } catch { }
        }
        return level;
    }

    private static void UpdateHandLight()
    {
        if (_pHandLight == null || !_pHandLight.Value) {
            if (!_tintsRestored) { _tintsRestored = true; RestoreTints(); Say("[handlight] off; colours put back"); }
            return;
        }
        _tintsRestored = false;
        try {
            float now = Time.unscaledTime;

            if (now >= _lightScanAt) {
                _lightScanAt = now + 2f;
                _lights.Clear();
                foreach (var l in UnityEngine.Object.FindObjectsOfType<Light>()) if (l != null) _lights.Add(l);
            }
            if (now >= _tintScanAt) {
                _tintScanAt = now + 2f;
                CollectTintTargets();
            }
            if (_tints.Count == 0) return;

            var cam = Camera.main;
            Vector3 at = cam != null ? cam.transform.position : Vector3.zero;

            float raw = Mathf.Clamp01(SampleLightAt(at));
            float curve = Mathf.Clamp(_pHandLightCurve == null ? .8f : _pHandLightCurve.Value, .1f, 4f);
            float floor = Mathf.Clamp01(_pHandLightFloor == null ? .04f : _pHandLightFloor.Value);
            float ceiling = Mathf.Clamp(_pHandLightCeiling == null ? 1f : _pHandLightCeiling.Value, floor, 2f);
            float want = floor + (ceiling - floor) * Mathf.Pow(raw, curve);

            float smoothing = Mathf.Max(0f, _pHandLightSmoothing == null ? .15f : _pHandLightSmoothing.Value);
            if (_litLevel < 0f || smoothing <= 0f) _litLevel = want;
            else _litLevel = Mathf.Lerp(_litLevel, want, 1f - Mathf.Exp(-Time.unscaledDeltaTime / smoothing));

            for (int i = _tints.Count - 1; i >= 0; i--) {
                var t = _tints[i];
                if (t.Mat == null) { _tints.RemoveAt(i); continue; }
                var c = t.Base;
                t.Mat.SetColor(t.Prop, new Color(c.r * _litLevel, c.g * _litLevel, c.b * _litLevel, c.a));
            }

            if (_lightLogs < 3 && Time.frameCount % 600 == 0) {
                _lightLogs++;
                Say("[handlight] room light " + raw.ToString("0.00") + " -> hands at " + _litLevel.ToString("0.00")
                    + " across " + _tints.Count + " materials");
            }
        } catch (Exception e) {
            if (_lightLogs++ < 4) I?.LoggerInstance.Warning("[handlight] stopped: " + e.Message);
        }
    }

    // ---------- the laundry menu on the monitor only ----------
    // RenderUIPanel skips inspection-scene cameras unless they are the room
    // panel camera:
    //
    //     if (IsInspectionSceneCamera(camera) && camera != _roomPanelCamera) continue;
    //
    // and IsInspectionSceneCamera counts "Laundry Room Camera". CoatCheckView
    // only ever assigns _roomPanelCamera from "Slot Cam", so the casino is
    // exempt and the laundry is not: its camera keeps drawing to the screen and
    // is never captured onto the VR panel. Claiming it the same way the casino
    // does fixes all three places that key off it - the capture, the forced
    // panel, and the opaque clear.

    private static int _roomCamLogs;
    private static string _roomCamLast = "";

    private static void CoatCheckPostfix(VRPlugin __instance, ref Camera __result)
    {
        if (_pRoomPanelCams == null || _roomPanelCam == null) return;
        try {
            // Slot Cam wins: Codex already handles the casino.
            if (_roomPanelCam(__instance) != null) return;

            string list = _pRoomPanelCams.Value;
            if (string.IsNullOrWhiteSpace(list)) return;

            Camera best = null;
            foreach (var cam in Camera.allCameras) {
                if (cam == null || !cam.isActiveAndEnabled || cam.targetTexture != null) continue;
                string n = cam.name ?? "";
                bool wanted = false;
                foreach (var raw in list.Split('|')) {
                    string want = raw.Trim();
                    if (want.Length > 0 && n.StartsWith(want, StringComparison.OrdinalIgnoreCase)) { wanted = true; break; }
                }
                if (!wanted) continue;
                // same tie-break Codex uses, so the choice cannot alternate
                if (best == null || cam.GetInstanceID() < best.GetInstanceID()) best = cam;
            }
            if (best == null) return;

            _roomPanelCam(__instance) = best;

            // CoatCheckView decides the VR viewpoint with this line:
            //
            //   if(_roomPanelCamera!=null && player==null && _lastExplorationCamera!=null)
            //       selected=_lastExplorationCamera;
            //
            // but it evaluates _roomPanelCamera before anything can claim the
            // laundry, so the test fails and the laundry camera is handed back
            // as the world camera. RenderUIPanel then excludes it for being the
            // world camera, which is why claiming it alone changed nothing: the
            // menu still never reached the panel. Run Codex's own branch again
            // now that the room camera is actually known.
            bool playerLive = __result != null
                && string.Equals(__result.name, "FirstPersonCharacter", StringComparison.OrdinalIgnoreCase);
            if (!playerLive && _lastExploreCam != null) {
                var explore = _lastExploreCam(__instance);
                if (explore != null) __result = explore;
            }

            if (best.name != _roomCamLast && _roomCamLogs++ < 8) {
                _roomCamLast = best.name;
                Say("[roompanel] '" + best.name + "' is the room panel camera now; VR view stays on '"
                    + (__result == null ? "none" : __result.name) + "'");
            }
        } catch (Exception e) {
            if (_roomCamLogs++ < 3) I?.LoggerInstance.Warning("[roompanel] could not claim a room camera: " + e.Message);
        }
    }

    private static bool HeadForward(VRPlugin instance, Quaternion playspaceRotation, out Vector3 look)
    {
        // BPVR_EyeL sits at the origin: Codex poses its eye cameras through
        // matrices, not that transform, so reading it gave a head at (0,0,0)
        // pointing nowhere. The real aim is the one Codex computes itself.
        look = Vector3.forward;
        try {
            if (_headRot == null) return false;
            look = playspaceRotation * _headRot(instance) * Vector3.forward;
            if (look.sqrMagnitude < 1e-6f) look = playspaceRotation * Vector3.forward;
            look.Normalize();
            if (look.y > .85f || look.y < -.85f) { look.y = Mathf.Clamp(look.y, -.85f, .85f); look.Normalize(); }
            return look.sqrMagnitude > 1e-6f;
        } catch { return false; }
    }

    private static void PanelFollowPostfix(VRPlugin __instance, Quaternion playspaceRotation, Vector3 eyePosition)
    {
        if (_pPanelFollow == null || !_pPanelFollow.Value || _uiQuad == null) return;
        try {
            // The computer path pins the panel to the monitor on purpose.
            if (_keyboardMode != null && _keyboardMode(__instance)) return;
            // The room choice places itself and you asked for it left alone.
            if (_draftMenu != null && _draftMenu(__instance)) return;
            if (_roomPanelCam != null && _roomPanelCam(__instance) != null) return;

            var quad = _uiQuad(__instance);
            if (quad == null || !quad.activeInHierarchy) return;
            MeasurePanel(__instance, playspaceRotation, eyePosition, quad);
            if (!HeadForward(__instance, playspaceRotation, out var look)) return;

            Vector3 from = quad.transform.position - eyePosition;
            float dist = from.magnitude;
            if (dist < .05f) return;
            Vector3 dir = from / dist;

            float off = Vector3.Angle(dir, look);
            float deadzone = Mathf.Clamp(_pPanelDeadzone == null ? 22f : _pPanelDeadzone.Value, 0f, 180f);
            if (off <= deadzone) return;

            float snap = Mathf.Clamp(_pPanelSnapAngle == null ? 70f : _pPanelSnapAngle.Value, deadzone, 180f);
            Vector3 target = look;
            if (off < snap) {
                float speed = Mathf.Max(10f, _pPanelFollowSpeed == null ? 200f : _pPanelFollowSpeed.Value);
                float step = Mathf.Min(off - deadzone, speed * Time.unscaledDeltaTime);
                target = Vector3.RotateTowards(dir, look, step * Mathf.Deg2Rad, 0f).normalized;
            }

            quad.transform.position = eyePosition + target * dist;
            quad.transform.rotation = Quaternion.LookRotation(target, Vector3.up);

            if (_followLogs++ < 6)
                Say("[panel] menu was " + off.ToString("0") + " deg off; bringing it back in front of you");
        } catch (Exception e) {
            if (_followLogs++ < 3) I?.LoggerInstance.Warning("[panel] follow failed: " + e.Message);
        }
    }

    // ---------- the game's screen blur ----------
    // The scene carries UnityStandardAssets.ImageEffects.BlurOptimized. On a
    // monitor it is a depth-of-field trick behind things like the slot machine.
    // In a headset it blurs the entire world around you, which is what the
    // "weird blur thing behind the slots" is. Image effects live on cameras, so
    // only cameras need checking, which keeps this cheap.

    private static float _blurCheckAt;
    private static int _blurLogs;

    private static bool _effectsListed;

    private static void KillScreenBlur()
    {
        if (_pKillBlur == null || !_pKillBlur.Value) return;
        if (Time.unscaledTime < _blurCheckAt) return;
        _blurCheckAt = Time.unscaledTime + .3f;
        try {
            // FindObjectsOfTypeAll catches cameras that are currently disabled,
            // which is where an effect waiting for the slot machine would sit.
            foreach (var o in Resources.FindObjectsOfTypeAll(Il2CppInterop.Runtime.Il2CppType.Of<Camera>())) {
                var cam = o == null ? null : o.TryCast<Camera>();
                if (cam == null) continue;
                foreach (var behaviour in cam.GetComponents<Behaviour>()) {
                    if (behaviour == null) continue;
                    string n;
                    try { n = behaviour.GetIl2CppType().Name ?? ""; } catch { continue; }
                    if (n.IndexOf("Blur", StringComparison.OrdinalIgnoreCase) < 0
                        && n.IndexOf("DepthOfField", StringComparison.OrdinalIgnoreCase) < 0) continue;
                    if (!behaviour.enabled) continue;
                    behaviour.enabled = false;
                    if (_blurLogs++ < 8) Say("[blur] switched off '" + n + "' on camera '" + cam.name + "'");
                }
            }

            // One inventory of what is actually attached, so a blur under some
            // other name is not invisible to me.
            if (!_effectsListed) {
                _effectsListed = true;
                var seen = new List<string>();
                foreach (var o in Resources.FindObjectsOfTypeAll(Il2CppInterop.Runtime.Il2CppType.Of<Camera>())) {
                    var cam = o == null ? null : o.TryCast<Camera>();
                    if (cam == null) continue;
                    foreach (var behaviour in cam.GetComponents<Behaviour>()) {
                        if (behaviour == null) continue;
                        string n;
                        try { n = behaviour.GetIl2CppType().Name ?? ""; } catch { continue; }
                        if (n == "Camera" || n == "Transform") continue;
                        string entry = cam.name + "/" + n;
                        if (!seen.Contains(entry)) seen.Add(entry);
                        if (seen.Count >= 40) break;
                    }
                }
                if (seen.Count > 0) I?.LoggerInstance.Msg("[blur] camera components present: " + string.Join(" | ", seen));
            }
        } catch (Exception e) {
            if (_blurLogs++ < 3) I?.LoggerInstance.Warning("[blur] could not check cameras: " + e.Message);
        }
    }

    // ---------- where is the panel, really ----------
    // The monitor view is not evidence: the flat camera and the headset look in
    // different directions, so a panel dead ahead in VR can sit at the edge of
    // the mirror. Measure it against the eye camera instead, which is the real
    // headset pose, and report the angle.

    private static float _aimLogAt;
    private static int _aimLogs;

    private static float _lastAimOff = -999f, _lastAimDist = -999f;

    private static void MeasurePanel(VRPlugin instance, Quaternion playspaceRotation, Vector3 eyePosition, GameObject quad)
    {
        if (_pPanelAimLog == null || !_pPanelAimLog.Value || _aimLogs >= 200) return;
        try {
            if (Time.unscaledTime < _aimLogAt) return;
            _aimLogAt = Time.unscaledTime + .4f;
            string scene = "";
            try { scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name ?? ""; } catch { }
            if (scene.IndexOf("Main Menu", StringComparison.OrdinalIgnoreCase) >= 0
                || scene.IndexOf("Initialize", StringComparison.OrdinalIgnoreCase) >= 0) return;
            if (!HeadForward(instance, playspaceRotation, out var look)) return;

            Vector3 from = quad.transform.position - eyePosition;
            float dist = from.magnitude;
            if (dist < .01f) return;
            float off = Vector3.Angle(from.normalized, look);
            if (Mathf.Abs(off - _lastAimOff) < 3f && Mathf.Abs(dist - _lastAimDist) < .15f) return;
            _lastAimOff = off; _lastAimDist = dist;
            _aimLogs++;
            I?.LoggerInstance.Msg("[panelaim] " + off.ToString("0") + " deg off centre, " + dist.ToString("0.00")
                + "m, draft=" + (_draftMenu != null && _draftMenu(instance))
                + " roomcam=" + (_roomPanelCam != null && _roomPanelCam(instance) != null));
        } catch { }
    }

    // ---------- pump room buttons ----------
    // Codex drives the pump buttons off the same 'typing' flag as a computer
    // keyboard, and passes that straight through as computerTyping, so the
    // hand adopts the curled keyboard pose while you are pressing a wall
    // button. The finger rig is already posed to point; only the cel hand
    // overlay disagrees. Force computerTyping off unless a computer keyboard
    // is genuinely open, which leaves real typing exactly as it was.

    private static VRPlugin _vr;
    private static int _pumpPoseLogs;

    private void PatchCelHandPose()
    {
        try {
            var type = AccessTools.TypeByName("BluePrinceVR.CelHandPack");
            if (type == null) { LoggerInstance.Warning("skipped: no CelHandPack in this build, pump pose unchanged"); return; }
            var method = AccessTools.Method(type, "Pose");
            if (method == null) { LoggerInstance.Warning("skipped: no CelHandPack.Pose, pump pose unchanged"); return; }
            HarmonyInstance.Patch(method, new HarmonyMethod(typeof(Mod).GetMethod(nameof(CelPosePrefix), BindingFlags.Static | BindingFlags.NonPublic)));
            LoggerInstance.Msg("patched CelHandPack.Pose");
        } catch (Exception e) {
            LoggerInstance.Warning("skipped CelHandPack.Pose: " + e.Message);
        }
    }

    private static void CelPosePrefix(object __instance, ref bool computerTyping)
    {
        if (__instance != null) _celPack = __instance;
        if (!computerTyping) return;
        if (_pPumpPointPose == null || !_pPumpPointPose.Value) return;
        try {
            bool realKeyboard = _keyboardMode != null && _vr != null && _keyboardMode(_vr);
            if (realKeyboard) return;
            computerTyping = false;
            if (_pumpPoseLogs++ < 3) Say("[pump] pointing with a stiff finger instead of the typing hand");
        } catch { }
    }

    // ---------- the floor plan during a room choice ----------
    // PollFloorPlanButton lists _draftMenu among the things that block it, so
    // now that the draft is detected properly the floor plan button stops
    // working exactly when it is most wanted: while picking the next room.
    // Hide the draft from that one method and put it back immediately, so
    // stick navigation and every other consumer still see it.

    private static bool _draftHidden;

    private static void FloorPlanPrefix(VRPlugin __instance)
    {
        _draftHidden = false;
        if (_pFloorPlanInDraft == null || !_pFloorPlanInDraft.Value || _draftMenu == null) return;
        try {
            if (!_draftMenu(__instance)) return;
            _draftMenu(__instance) = false;
            _draftHidden = true;
        } catch { _draftHidden = false; }
    }

    private static void FloorPlanPostfix(VRPlugin __instance)
    {
        if (!_draftHidden) return;
        _draftHidden = false;
        try { if (_draftMenu != null) _draftMenu(__instance) = true; } catch { }
    }

    // ---------- crates ----------
    // Codex's box assist recognises TRUNK(Clone) and ParlorBox and nothing else.
    // The scene calls the wooden ones 'Crate A' and 'Crate B (n)', so they miss
    // it entirely: WorldInteractionReach falls through to the bare 1.5m default
    // and WithinInteractionReach then also refuses to exceed the object's own
    // mouse reach, because the clause that allows that only fires for a trunk.
    // Teaching it the name fixes both at once, since reach is derived from the
    // root it finds.

    private static bool IsCrateName(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        string list = _pCrateNames?.Value;
        if (string.IsNullOrWhiteSpace(list)) list = "CRATE";
        foreach (var raw in list.Split('|')) {
            string want = raw.Trim();
            if (want.Length == 0) continue;
            if (!name.StartsWith(want, StringComparison.OrdinalIgnoreCase)) continue;
            // 'CRATES' is the group holding all of them; taking that as the root
            // would lump every crate in the room into one interaction.
            if (name.Equals(want + "S", StringComparison.OrdinalIgnoreCase)) continue;
            return true;
        }
        return false;
    }

    private static Transform CrateRoot(Collider col)
    {
        try {
            int depth = 0;
            for (var t = col == null ? null : col.transform; t != null && depth < 8; t = t.parent, depth++) {
                if (IsCrateName(t.name)) return t;
                if (t.name == "_GAMEPLAY" || t.name == "Room Spawn Pools" || t.name == "Pickup Spawn Pools") break;
            }
        } catch { }
        return null;
    }

    private static int _crateBoxLogs;

    private static void BoxRootPostfix(Collider col, ref Transform __result)
    {
        if (_pCrateReachFix == null || !_pCrateReachFix.Value) return;
        if (__result != null) return;
        try {
            var root = CrateRoot(col);
            if (root == null) return;
            __result = root;
            if (_crateBoxLogs++ < 5) Say("[crate] '" + root.name + "' now counts as an openable box, reach "
                + (_pCrateReachMetres == null ? 3.2f : _pCrateReachMetres.Value).ToString("0.0") + "m");
        } catch { }
    }

    private static void BoxReachPostfix(Transform root, ref float __result)
    {
        if (_pCrateReachFix == null || !_pCrateReachFix.Value) return;
        try {
            if (root == null || !IsCrateName(root.name)) return;
            __result = Mathf.Clamp(_pCrateReachMetres == null ? 3.2f : _pCrateReachMetres.Value, .5f, 8f);
        } catch { }
    }

    // ---------- stale hand-aim steering ----------
    // SteerViewToHand locks a world point the frame you press the trigger and
    // then walks the mouse until the game camera looks at it. Releasing the
    // trigger clears that lock, but the early return for an unlocked cursor
    // does not: it resets the hold timer and leaves _steerLocked set with the
    // old goal still in it. Choose a door and the menu unlocks the cursor, the
    // game moves you, the cursor locks again, and steering carries on aiming at
    // a point in the room you just left. The view ends up crooked, and the
    // stream of synthetic mouse motion is what makes the game decide you are on
    // mouse and stop taking controller input for the plan choice.
    //
    // Intermittent because it depends on whether the trigger was still down
    // across the transition.

    private static Vector3 _lastCamPos;
    private static float _lastCamYaw;
    private static bool _camSeen;
    private static bool _steerSuppressed;
    private static int _steerDropLogs;

    private static void ClearSteerLock(VRPlugin instance)
    {
        try {
            if (_steerLocked != null) _steerLocked(instance) = false;
            if (_steerHeld != null) _steerHeld(instance) = 0f;
            if (_steerWorse != null) _steerWorse(instance) = 0;
            if (_steerLastErr != null) _steerLastErr(instance) = -1f;
        } catch { }
    }

    private static bool SteerPrefix(VRPlugin __instance, Camera gameCam, Hand h)
    {
        if (_pSteerStale == null || !_pSteerStale.Value) return true;
        try {
            bool triggerDown = h != null && h.Valid && h.TriggerDown;
            bool unlocked = Cursor.lockState == CursorLockMode.None;

            // A jump this big in one frame is the game moving you. Our own
            // steering is rate limited to a few degrees, so it cannot be us.
            bool snapped = false;
            if (gameCam != null) {
                var pos = gameCam.transform.position;
                float yaw = gameCam.transform.eulerAngles.y;
                if (_camSeen) {
                    float limit = _pSteerSnapDeg == null ? 20f : Mathf.Max(5f, _pSteerSnapDeg.Value);
                    if ((pos - _lastCamPos).sqrMagnitude > .5f * .5f) snapped = true;
                    if (Mathf.Abs(Mathf.DeltaAngle(yaw, _lastCamYaw)) > limit) snapped = true;
                }
                _lastCamPos = pos;
                _lastCamYaw = yaw;
                _camSeen = true;
            }

            if (unlocked || snapped) {
                ClearSteerLock(__instance);
                // Do not pick a fresh goal off a trigger that was already held
                // through the transition; wait for a deliberate new press.
                if (triggerDown && !_steerSuppressed) {
                    _steerSuppressed = true;
                    if (_steerDropLogs++ < 8)
                        Say("[steer] dropped the aim goal (" + (unlocked ? "a menu opened" : "the game moved the view")
                            + ") and holding off until you let go of the trigger");
                }
            }

            if (_steerSuppressed) {
                if (!triggerDown) { _steerSuppressed = false; return true; }
                ClearSteerLock(__instance);
                return false;
            }
        } catch (Exception e) {
            if (_steerDropLogs++ < 3) I?.LoggerInstance.Warning("[steer] stale-goal guard failed: " + e.Message);
        }
        return true;
    }

    // ---------- Codex's blank input defaults ----------
    // Codex ships ActionMoveX/MoveY/LookX/LookY/Interact/Cancel/Menu as empty
    // strings and tells you to copy the real names out of your own log. Its
    // matcher returns false on an empty name, so until someone does that by
    // hand the sticks, interact, cancel and menu are all dead on the
    // controllers and the game falls back to keyboard - which is exactly the
    // "left controller does nothing", "right stick became WASD" and "keyboard
    // and mouse was fine" that testers report. The names are the game's own
    // Rewired actions, identical in every copy, so there is nothing per-machine
    // to discover and they belong in the defaults.

    private static readonly string[,] _rewiredNames = {
        { "ActionMoveX",    "Move Horizontal" },
        { "ActionMoveY",    "Move Vertical"   },
        { "ActionLookX",    "Look Horizontal" },
        { "ActionLookY",    "Look Vertical"   },
        { "ActionInteract", "click"           },
        { "ActionCancel",   "back"            },
        { "ActionMenu",     "system menu"     },
    };

    // Codex's category is "BluePrinceVR_Codex", not "BluePrinceVR". Try the
    // known name first and then anything that looks like it, so a rename in
    // Codex costs a log line rather than the whole fix.
    private static MelonPreferences_Category FindCodexCategory()
    {
        foreach (var name in new[] { "BluePrinceVR_Codex", "BluePrinceVR" }) {
            try { var c = MelonPreferences.GetCategory(name); if (c != null) return c; } catch { }
        }
        try {
            foreach (var c in MelonPreferences.Categories) {
                if (c == null) continue;
                string id = c.Identifier ?? "";
                if (id.StartsWith("BluePrinceVR", StringComparison.OrdinalIgnoreCase)
                    && id.IndexOf("ClaudeFixes", StringComparison.OrdinalIgnoreCase) < 0) return c;
            }
        } catch { }
        return null;
    }

    private static bool _cfgTouched;

    private static void FixCodexInputDefaults()
    {
        try {
            var cat = FindCodexCategory();
            if (cat == null) { I?.LoggerInstance.Warning("[input] Codex preference category not found; leaving input alone"); return; }

            if (_pFixRewired != null && _pFixRewired.Value) {
                var filled = new List<string>();
                for (int i = 0; i < _rewiredNames.GetLength(0); i++) {
                    string key = _rewiredNames[i, 0], want = _rewiredNames[i, 1];
                    try {
                        var entry = cat.GetEntry<string>(key);
                        if (entry == null) continue;
                        if (!string.IsNullOrWhiteSpace(entry.Value)) continue;
                        entry.Value = want;
                            filled.Add(key + " = \"" + want + "\"");
                        _cfgTouched = true;
                    } catch { }
                }

                // Codex copies the preferences into VRInput once, during its own
                // startup, which has already happened by now - so set the live
                // fields too or the fix would not land until the next launch.
                if (string.IsNullOrWhiteSpace(VRInput.ActMoveX)) VRInput.ActMoveX = "Move Horizontal";
                if (string.IsNullOrWhiteSpace(VRInput.ActMoveY)) VRInput.ActMoveY = "Move Vertical";
                if (string.IsNullOrWhiteSpace(VRInput.ActLookX)) VRInput.ActLookX = "Look Horizontal";
                if (string.IsNullOrWhiteSpace(VRInput.ActLookY)) VRInput.ActLookY = "Look Vertical";
                if (string.IsNullOrWhiteSpace(VRInput.ActInteract)) VRInput.ActInteract = "click";
                if (string.IsNullOrWhiteSpace(VRInput.ActCancel)) VRInput.ActCancel = "back";
                if (string.IsNullOrWhiteSpace(VRInput.ActMenu)) VRInput.ActMenu = "system menu";

                if (filled.Count > 0) {
                    I?.LoggerInstance.Msg("[input] Rewired action names were blank; filled " + filled.Count + ": " + string.Join(", ", filled));
                } else {
                    I?.LoggerInstance.Msg("[input] Rewired action names already set; nothing to fill");
                }
            }

            float raise = _pRaiseResScale == null ? 0f : _pRaiseResScale.Value;
            if (raise > 0f) {
                try {
                    var res = cat.GetEntry<float>("ResolutionScale");
                    if (res != null && Mathf.Abs(res.Value - .5f) < .0005f && Mathf.Abs(raise - .5f) > .0005f) {
                        res.Value = Mathf.Clamp(raise, .3f, 2f);
                        _cfgTouched = true;
                        I?.LoggerInstance.Msg("[input] ResolutionScale was still on the stock 0.5 (half the headset's recommended size); raised to "
                            + res.Value.ToString("0.00") + ". Eye textures are sized at startup, so this one takes effect next launch.");
                    }
                } catch { }
            }

            if (_cfgTouched) { try { MelonPreferences.Save(); } catch { } }
        } catch (Exception e) {
            I?.LoggerInstance.Warning("[input] could not repair Codex input defaults: " + e);
        }
    }

    // ---------- VR wordmark on the title screen ----------
    // There is nothing to append the VR to. Reading the menu scene out of the
    // game's own data shows the whole title screen is a single painted quad,
    // 'MAIN MENU BG', with "BLUE PRINCE" baked into the picture, and the only
    // other visible things in the scene are the 3D TextMeshPro menu entries.
    // No logo object, no UI image, no title label. So the VR has to arrive as
    // its own object placed on top of the painting.
    //
    // Everything below is a fraction of that quad's world bounds, so it lands
    // on the same spot of the artwork at any resolution, any aspect ratio and
    // in either eye. The defaults come from measuring Alex's own mockup against
    // the menu camera (60 degree vertical FOV, 2.611m from the quad): the VR's
    // left edge just past the final E, centred on the cap height of the word.

    private static GameObject _vrArt;
    private static Texture2D _vrTex;
    private static bool _vrTexTried;
    private static Texture2D _puffTex;
    private static bool _puffTexTried;
    private static Texture2D _smokeTex;
    private static bool _smokeTexTried;
    private static float _vrCheckAt;
    private static string _vrScene = "";
    private static bool _vrDumped;
    private static float _vrMenuSince;
    private static int _vrLogs;

    // A scene change destroys anything we put in the old scene, so forget it.
    private static bool OnMainMenu()
    {
        string n = "";
        try { n = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name ?? ""; } catch { }
        if (n != _vrScene) {
            _vrScene = n;
            _vrArt = null;
            _vrDumped = false;
            _vrSprite = null;
            _poofAt = 0f;
            _poofDone = true;
            _puffs = null;
            _wisps = null;
        }
        return n.IndexOf("Main Menu", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    // Only the menu painting will do. The scene is already called 'Main Menu'
    // while the publisher splash is still up, and that splash is its own quad
    // ('background for logo') parented to the camera less than a metre away -
    // which is what the first build grabbed, and why the VR floated in front of
    // the picture instead of sitting in it. MAIN MENU BG turns on a moment
    // later, so wait for it by name rather than taking the biggest thing going.
    private static Renderer FindBackdrop()
    {
        string want = (_pTitleBackdrop?.Value ?? "MAIN MENU BG").Trim();
        if (want.Length == 0) want = "MAIN MENU BG";
        try {
            foreach (var r in UnityEngine.Object.FindObjectsOfType<MeshRenderer>()) {
                if (r == null || !r.enabled) continue;
                var go = r.gameObject;
                if (go == null || !go.activeInHierarchy) continue;
                string n = go.name ?? "";
                if (string.Equals(n, want, StringComparison.OrdinalIgnoreCase)) return r;
                // 'MAIN MENU BG (1)' is the same quad at the same transform
                if (n.StartsWith(want, StringComparison.OrdinalIgnoreCase)) return r;
            }
        } catch (Exception e) {
            if (_vrLogs++ < 3) I?.LoggerInstance.Warning("[title] backdrop search failed: " + e.Message);
        }
        return null;
    }

    private static IEnumerable<string> ArtPaths(string file)
    {
        file = (file ?? "").Trim();
        if (file.Length == 0) file = "BPVR_VR.png";
        if (file.Contains(":") || file.StartsWith("\\") || file.StartsWith("/")) { yield return file; yield break; }
        string mods = null;
        try { mods = System.IO.Path.GetDirectoryName(typeof(Mod).Assembly.Location); } catch { }
        if (!string.IsNullOrEmpty(mods)) {
            string root = null;
            try { root = System.IO.Directory.GetParent(mods)?.FullName; } catch { }
            if (!string.IsNullOrEmpty(root)) {
                yield return System.IO.Path.Combine(root, "UserData", file);
                yield return System.IO.Path.Combine(root, file);
            }
            yield return System.IO.Path.Combine(mods, file);
        }
        string cwd = null;
        try { cwd = Environment.CurrentDirectory; } catch { }
        if (!string.IsNullOrEmpty(cwd)) {
            yield return System.IO.Path.Combine(cwd, "UserData", file);
            yield return System.IO.Path.Combine(cwd, "Mods", file);
            yield return System.IO.Path.Combine(cwd, file);
        }
    }

    // The same artwork is baked into this DLL, so the VR still appears if the
    // loose file is missing, moved, or never installed.
    private static byte[] EmbeddedArt(string file)
    {
        try {
            var asm = typeof(Mod).Assembly;
            foreach (var name in asm.GetManifestResourceNames()) {
                if (name == null || !name.EndsWith(file, StringComparison.OrdinalIgnoreCase)) continue;
                using (var stream = asm.GetManifestResourceStream(name)) {
                    if (stream == null) continue;
                    var buffer = new byte[stream.Length];
                    int read = 0;
                    while (read < buffer.Length) {
                        int n = stream.Read(buffer, read, buffer.Length - read);
                        if (n <= 0) break;
                        read += n;
                    }
                    if (read == buffer.Length) return buffer;
                }
            }
        } catch (Exception e) {
            I?.LoggerInstance.Warning("[title] embedded artwork unreadable: " + e.Message);
        }
        return null;
    }

    private static Texture2D TextureFrom(byte[] bytes, string where)
    {
        try {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            if (!ImageConversion.LoadImage(tex, bytes)) {
                I?.LoggerInstance.Warning("[title] " + where + " is not a readable image");
                return null;
            }
            I?.LoggerInstance.Msg("[title] loaded VR artwork " + tex.width + "x" + tex.height + " from " + where);
            return tex;
        } catch (Exception e) {
            I?.LoggerInstance.Warning("[title] could not decode " + where + ": " + e.Message);
            return null;
        }
    }

    private static Texture2D LoadArt(string file, ref Texture2D cache, ref bool tried)
    {
        if (tried) return cache;
        tried = true;
        string looked = "";
        foreach (var path in ArtPaths(file)) {
            looked += (looked.Length > 0 ? ", " : "") + path;
            try {
                if (!System.IO.File.Exists(path)) continue;
                cache = TextureFrom(System.IO.File.ReadAllBytes(path), path);
                if (cache != null) return cache;
            } catch (Exception e) {
                I?.LoggerInstance.Warning("[title] could not read '" + path + "': " + e.Message);
            }
        }
        var baked = EmbeddedArt(file);
        if (baked != null) {
            cache = TextureFrom(baked, file + " baked into this mod");
            if (cache != null) return cache;
        }
        I?.LoggerInstance.Warning("[title] no " + file + " anywhere (looked in: " + looked + ")");
        return null;
    }

    private static Texture2D LoadVRArt()
    {
        return LoadArt((_pTitleImage?.Value ?? "BPVR_VR.png"), ref _vrTex, ref _vrTexTried);
    }

    private static Texture2D LoadPuffArt()
    {
        return LoadArt("BPVR_puff.png", ref _puffTex, ref _puffTexTried);
    }

    private static Texture2D LoadSmokeArt()
    {
        var tex = LoadArt("BPVR_smoke.png", ref _smokeTex, ref _smokeTexTried);
        return tex != null ? tex : LoadPuffArt();
    }

    // One line in the log listing what is actually on the menu, so a miss can be
    // diagnosed without another round trip.
    private static void DumpMenu(string why)
    {
        if (_vrDumped) return;
        _vrDumped = true;
        try {
            var parts = new List<string>();
            foreach (var r in UnityEngine.Object.FindObjectsOfType<MeshRenderer>()) {
                if (r == null || !r.enabled) continue;
                var go = r.gameObject;
                if (go == null || !go.activeInHierarchy) continue;
                var b = r.bounds;
                parts.Add(go.name + " [L" + go.layer
                    + " c(" + b.center.x.ToString("0.00") + "," + b.center.y.ToString("0.00") + "," + b.center.z.ToString("0.00")
                    + ") s(" + b.size.x.ToString("0.00") + "," + b.size.y.ToString("0.00") + ")]");
                if (parts.Count >= 24) break;
            }
            I?.LoggerInstance.Msg("[title] " + why + "; scene '" + _vrScene + "' renderers: " + string.Join(" | ", parts));
        } catch { }
    }

    private static Il2CppTMPro.TMP_FontAsset FindTitleFont()
    {
        Il2CppTMPro.TMP_FontAsset fallback = null;
        try {
            foreach (var font in Resources.FindObjectsOfTypeAll(Il2CppInterop.Runtime.Il2CppType.Of<Il2CppTMPro.TMP_FontAsset>())) {
                var f = font == null ? null : font.TryCast<Il2CppTMPro.TMP_FontAsset>();
                if (f == null) continue;
                string n = f.name ?? "";
                if (n.IndexOf("Margo", StringComparison.OrdinalIgnoreCase) >= 0) return f;
                if (n.IndexOf("Versica", StringComparison.OrdinalIgnoreCase) >= 0) return f;
                if (fallback == null || n.IndexOf("Serif", StringComparison.OrdinalIgnoreCase) >= 0) fallback = f;
            }
        } catch { }
        return fallback;
    }

    // Sprites are drawn with Unity's built-in sprite material. It is normally in
    // the build (this scene ships 75 sprite renderers), but if the shader were
    // stripped the artwork would render as magenta or not at all, so check and
    // substitute, and say which shader was used either way.
    private static void EnsureSpriteMaterial(SpriteRenderer sr, Texture2D tex)
    {
        try {
            var mat = sr.sharedMaterial;
            var shader = mat == null ? null : mat.shader;
            string name = shader == null ? "" : (shader.name ?? "");
            bool bad = name.Length == 0 || name.IndexOf("InternalError", StringComparison.OrdinalIgnoreCase) >= 0;
            I?.LoggerInstance.Msg("[title] sprite shader: " + (name.Length == 0 ? "<none>" : name) + (bad ? "  <- unusable, substituting" : ""));
            if (!bad) return;

            var found = FindShader("Sprites/Default", "UI/Default", "Unlit/Transparent", "Sprites/Diffuse");
            if (found == null) { I?.LoggerInstance.Warning("[title] no usable shader found for the VR artwork"); return; }
            var replacement = new Material(found);
            replacement.hideFlags = HideFlags.HideAndDontSave;
            replacement.mainTexture = tex;
            sr.material = replacement;
            I?.LoggerInstance.Msg("[title] substituted shader '" + (found.name ?? "?") + "'");
        } catch (Exception e) {
            I?.LoggerInstance.Warning("[title] shader check failed: " + e.Message);
        }
    }

    private static Shader FindShader(params string[] names)
    {
        foreach (var n in names) {
            try { var s = Shader.Find(n); if (s != null) return s; } catch { }
        }
        try {
            foreach (var o in Resources.FindObjectsOfTypeAll(Il2CppInterop.Runtime.Il2CppType.Of<Shader>())) {
                var s = o == null ? null : o.TryCast<Shader>();
                if (s == null) continue;
                string n = s.name ?? "";
                if (n.IndexOf("Sprite", StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("Unlit/Trans", StringComparison.OrdinalIgnoreCase) >= 0) return s;
            }
        } catch { }
        return null;
    }

    // ---------- the poof ----------
    // A conjuring trick: a cloud of smoke bursts where the VR is about to be,
    // and the lettering is simply there when it clears. The puffs are one soft
    // texture drawn several times at different sizes, rotations and speeds,
    // which reads as smoke far better than any one sprite does, and the
    // lettering fades up from slightly oversized so it looks like it arrived
    // rather than faded in.

    private static SpriteRenderer _vrSprite;
    private static float _vrHeight;
    private static float _poofAt;
    private static bool _poofDone = true;
    private static SpriteRenderer[] _puffs;
    private static Vector3[] _puffDrift;
    private static float[] _puffSpin, _puffDelay, _puffSize;
    private const int PuffCount = 9;
    private const float PuffLife = 1.15f;

    private static void SpawnPuffs()
    {
        var tex = LoadPuffArt();
        if (tex == null || _vrSprite == null) { _poofDone = true; ShowVRNow(); return; }

        var centre = _vrSprite.transform.position;
        int layer = _vrSprite.gameObject.layer;
        float unit = Mathf.Max(.01f, _vrHeight);

        _puffs = new SpriteRenderer[PuffCount];
        _puffDrift = new Vector3[PuffCount];
        _puffSpin = new float[PuffCount];
        _puffDelay = new float[PuffCount];
        _puffSize = new float[PuffCount];

        for (int i = 0; i < PuffCount; i++) {
            var go = new GameObject("BPVR_TitlePuff" + i);
            go.layer = layer;
            var sr = go.AddComponent<SpriteRenderer>();
            float ppu = tex.height / (unit * 1.7f);
            sr.sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(.5f, .5f), ppu);
            sr.sortingOrder = 2001 + (i % 2);
            EnsureSpriteMaterial(sr, tex);
            sr.color = new Color(1f, 1f, 1f, 0f);

            // spread along the lettering, not in a tidy ring
            float t = (i + .5f) / PuffCount;
            float spread = (t - .5f) * 2f;
            var jitter = new Vector3(spread * unit * 1.5f + Rand(i, 1) * unit * .35f,
                                     Rand(i, 2) * unit * .45f,
                                     -.004f * i);
            go.transform.position = centre + jitter;
            go.transform.rotation = Quaternion.Euler(0f, 0f, Rand(i, 3) * 180f);

            _puffs[i] = sr;
            _puffDrift[i] = new Vector3(spread * unit * 1.1f + Rand(i, 4) * unit * .5f,
                                        unit * (.5f + Mathf.Abs(Rand(i, 5)) * .7f), 0f);
            _puffSpin[i] = Rand(i, 6) * 55f;
            _puffDelay[i] = Mathf.Abs(Rand(i, 7)) * .13f;
            _puffSize[i] = .55f + Mathf.Abs(Rand(i, 8)) * .75f;
        }
    }

    // deterministic spread, so it looks the same every launch
    private static float Rand(int i, int salt)
    {
        int h = (i * 73856093) ^ (salt * 19349663);
        h = (h ^ (h >> 13)) * 1274126177;
        return ((h ^ (h >> 16)) & 0xFFFF) / 32767.5f - 1f;
    }

    private static void ShowVRNow()
    {
        if (_vrSprite == null) return;
        _vrSprite.color = Color.white;
        _vrSprite.transform.localScale = Vector3.one;
    }

    private static void AnimateTitleVR()
    {
        if (_vrSprite == null) return;
        // The burst and the drifting smoke were showy for the sake of it. The wordmark
        // appears, and that is the whole of it now.
        if (_pTitleFx == null || !_pTitleFx.Value) {
            if (!_poofDone) { _poofDone = true; ShowVRNow(); }
            return;
        }
        if (_poofDone) { DriftSmoke(); return; }
        try {
            if (_poofAt <= 0f) return;
            float t = Time.unscaledTime - _poofAt;
            if (t < 0f) return;

            if (_puffs == null) { SpawnPuffs(); if (_puffs == null) return; }

            bool alive = false;
            for (int i = 0; i < _puffs.Length; i++) {
                var sr = _puffs[i];
                if (sr == null) continue;
                float age = t - _puffDelay[i];
                if (age < 0f) { alive = true; continue; }
                float k = age / PuffLife;
                if (k >= 1f) { UnityEngine.Object.Destroy(sr.gameObject); _puffs[i] = null; continue; }
                alive = true;
                // burst out fast, then slow: 1 - (1-k)^3
                float ease = 1f - Mathf.Pow(1f - k, 3f);
                float fade = (k < .12f) ? (k / .12f) : Mathf.Pow(1f - (k - .12f) / .88f, 1.6f);
                sr.transform.localScale = Vector3.one * (_puffSize[i] * (.35f + ease * 1.5f));
                sr.transform.position += _puffDrift[i] * (Time.unscaledDeltaTime * (1f - ease) * 1.6f);
                sr.transform.Rotate(0f, 0f, _puffSpin[i] * Time.unscaledDeltaTime, Space.Self);
                sr.color = new Color(1f, 1f, 1f, Mathf.Clamp01(fade) * .62f);
            }

            // the lettering is revealed inside the cloud, a touch oversized
            float r = Mathf.Clamp01((t - .20f) / .38f);
            float soft = r * r * (3f - 2f * r);
            _vrSprite.color = new Color(1f, 1f, 1f, soft);
            _vrSprite.transform.localScale = Vector3.one * Mathf.Lerp(1.22f, 1f, soft);

            if (!alive && r >= 1f) {
                _poofDone = true;
                _puffs = null;
                ShowVRNow();
                _smokeNext = Time.unscaledTime;
                Say("[title] VR conjured in");
            }
        } catch (Exception e) {
            _poofDone = true;
            ShowVRNow();
            I?.LoggerInstance.Warning("[title] the poof failed, showing the VR plainly: " + e.Message);
        }
    }

    // After the trick, the lettering goes on smouldering: single wisps peel off
    // the top of the letters, rise, spread and thin out. Slots are reused, so
    // this is a fixed handful of sprites no matter how long the menu is up.

    private const int WispCount = 20;
    private static SpriteRenderer[] _wisps;
    private static float[] _wispBorn, _wispLife, _wispRise, _wispSway, _wispSize, _wispSpin;
    private static Vector3[] _wispFrom;
    private static float _smokeNext;
    private static int _wispSeq;

    private static bool _smokeBroken;

    private static void DriftSmoke()
    {
        if (_smokeBroken) return;
        if (_pTitleSmoke == null || !_pTitleSmoke.Value) return;
        if (_vrSprite == null) return;
        try {
            var tex = LoadSmokeArt();
            if (tex == null) return;
            float unit = Mathf.Max(.01f, _vrHeight);
            var centre = _vrSprite.transform.position;

            if (_wisps == null) {
                _wisps = new SpriteRenderer[WispCount];
                _wispBorn = new float[WispCount];
                _wispLife = new float[WispCount];
                _wispRise = new float[WispCount];
                _wispSway = new float[WispCount];
                _wispSize = new float[WispCount];
                _wispSpin = new float[WispCount];
                _wispFrom = new Vector3[WispCount];
            }

            float now = Time.unscaledTime;
            float rate = Mathf.Clamp(_pTitleSmokeRate == null ? .16f : _pTitleSmokeRate.Value, .03f, 5f);

            // Tufts leave a narrow base one at a time. The column is what you
            // get from many small torn pieces overlapping, not from big puffs.
            if (now >= _smokeNext) {
                for (int i = 0; i < WispCount; i++) {
                    if (_wisps[i] != null && _wispBorn[i] > 0f) continue;
                    int seq = _wispSeq++;
                    if (_wisps[i] == null) {
                        var go = new GameObject("BPVR_TitleSmoke" + i);
                        go.layer = _vrSprite.gameObject.layer;
                        var sr = go.AddComponent<SpriteRenderer>();
                        float ppu = tex.height / unit;
                        sr.sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(.5f, .5f), ppu);
                        sr.sortingOrder = 1999;   // behind the lettering
                        EnsureSpriteMaterial(sr, tex);
                        _wisps[i] = sr;
                    }
                    _wispBorn[i] = now;
                    _wispLife[i] = 2.4f + Mathf.Abs(Rand(seq, 11)) * 2.2f;
                    _wispRise[i] = unit * (1.5f + Mathf.Abs(Rand(seq, 12)) * 1.3f);
                    _wispSway[i] = Rand(seq, 13) * unit * .75f;
                    _wispSize[i] = .22f + Mathf.Abs(Rand(seq, 14)) * .16f;
                    _wispSpin[i] = Rand(seq, 15) * 16f;
                    // narrow source across the middle of the lettering
                    _wispFrom[i] = centre + new Vector3(Rand(seq, 16) * unit * .34f,
                                                        unit * (.12f + Rand(seq, 17) * .10f), .006f);
                    var t0 = _wisps[i].transform;
                    t0.position = _wispFrom[i];
                    t0.rotation = Quaternion.Euler(0f, 0f, Rand(seq, 18) * 32f);
                    t0.localScale = new Vector3(_wispSize[i], _wispSize[i], 1f);
                    _wisps[i].color = new Color(1f, 1f, 1f, 0f);
                    _smokeNext = now + rate * (.6f + Mathf.Abs(Rand(seq, 19)) * .8f);
                    break;
                }
            }

            for (int i = 0; i < WispCount; i++) {
                var sr = _wisps[i];
                if (sr == null || _wispBorn[i] <= 0f) continue;
                float k = (now - _wispBorn[i]) / _wispLife[i];
                if (k >= 1f) { _wispBorn[i] = 0f; sr.color = new Color(1f, 1f, 1f, 0f); continue; }

                float climb = 1f - Mathf.Pow(1f - k, 2.1f);
                // drift sideways more the higher it gets, and wander as it goes
                float wander = Mathf.Sin((now * .55f) + i * 1.7f) * unit * .16f * climb;
                sr.transform.position = _wispFrom[i]
                    + new Vector3(_wispSway[i] * climb * climb + wander, _wispRise[i] * climb, 0f);
                // grows, and stretches upward as it is pulled apart
                float grow = _wispSize[i] * (1f + climb * 2.6f);
                sr.transform.localScale = new Vector3(grow * (1f + climb * .35f), grow * (1f + climb * .95f), 1f);
                sr.transform.Rotate(0f, 0f, _wispSpin[i] * Time.unscaledDeltaTime, Space.Self);
                float fade = (k < .18f) ? (k / .18f) : Mathf.Pow(1f - (k - .18f) / .82f, 1.7f);
                sr.color = new Color(1f, 1f, 1f, Mathf.Clamp01(fade) * .30f);
            }
        } catch (Exception e) {
            _smokeBroken = true;
            _wisps = null;
            I?.LoggerInstance.Warning("[title] smoke stopped: " + e.Message);
        }
    }

    private static void PlaceTitleVR()
    {
        if (_pTitleVR == null || !_pTitleVR.Value) return;
        try {
            if (Time.unscaledTime < _vrCheckAt) return;
            _vrCheckAt = Time.unscaledTime + .75f;

            if (!OnMainMenu()) { _vrMenuSince = 0f; return; }
            if (_vrArt != null) return;
            if (_vrMenuSince <= 0f) _vrMenuSince = Time.unscaledTime;

            var bg = FindBackdrop();
            if (bg == null) {
                // the menu takes a few seconds to come up behind the splash
                if (Time.unscaledTime - _vrMenuSince > 12f) DumpMenu("menu backdrop never appeared");
                return;
            }
            var bounds = bg.bounds;
            if (bounds.size.y < .05f) { DumpMenu("backdrop '" + bg.gameObject.name + "' has no height yet"); return; }

            float offX = _pTitleOffX == null ? .0476f : _pTitleOffX.Value;
            float offY = _pTitleOffY == null ? .2019f : _pTitleOffY.Value;
            float hFrac = Mathf.Clamp(_pTitleHeight == null ? .0931f : _pTitleHeight.Value, .005f, 1f);
            float depth = _pTitleDepth == null ? .01f : _pTitleDepth.Value;

            float height = bounds.size.y * hFrac;
            var pos = new Vector3(bounds.center.x + offX * bounds.size.x,
                                  bounds.center.y + offY * bounds.size.y,
                                  bounds.center.z - depth);

            var tex = LoadVRArt();
            GameObject go;
            string how;

            if (tex != null) {
                go = new GameObject("BPVR_TitleVR");
                go.layer = bg.gameObject.layer;
                var sr = go.AddComponent<SpriteRenderer>();
                // pixelsPerUnit chosen so the artwork comes out exactly `height`
                // tall in world units, with its own width to match.
                float ppu = Mathf.Max(1f, tex.height / Mathf.Max(.0001f, height));
                sr.sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(.5f, .5f), ppu);
                sr.sortingOrder = 2000;
                EnsureSpriteMaterial(sr, tex);
                _vrSprite = sr;
                _vrHeight = height;
                if (_pTitlePoof != null && _pTitlePoof.Value) {
                    sr.color = new Color(1f, 1f, 1f, 0f);
                    _poofAt = Time.unscaledTime + Mathf.Max(0f, _pTitlePoofDelay == null ? 1.1f : _pTitlePoofDelay.Value);
                    _poofDone = false;
                }
                how = "artwork " + tex.width + "x" + tex.height + ", " + (height * tex.width / tex.height).ToString("0.000")
                    + "m x " + height.ToString("0.000") + "m";
            } else {
                go = new GameObject("BPVR_TitleVR");
                go.layer = bg.gameObject.layer;
                go.AddComponent<RectTransform>();
                var label = go.AddComponent<Il2CppTMPro.TextMeshPro>();
                var font = FindTitleFont();
                if (font != null) label.font = font;
                label.text = (_pTitleSuffix?.Value ?? "VR").Trim();
                label.enableWordWrapping = false;
                label.alignment = Il2CppTMPro.TextAlignmentOptions.Center;
                label.fontSize = height * 13f;
                Color colour = new Color(.78f, .60f, .20f, 1f);
                try {
                    string hex = _pTitleColour?.Value;
                    if (!string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex.StartsWith("#") ? hex : "#" + hex, out var parsed)) colour = parsed;
                } catch { }
                label.color = colour;
                var rt = go.GetComponent<RectTransform>();
                if (rt != null) rt.sizeDelta = new Vector2(height * 3f, height * 1.6f);
                how = "lettering in '" + (font == null ? "default" : font.name) + "'";
            }

            go.transform.position = pos;
            go.transform.rotation = Quaternion.identity;
            _vrArt = go;

            if (_vrLogs++ < 4) {
                I?.LoggerInstance.Msg("[title] VR placed as " + how
                    + " at (" + pos.x.ToString("0.000") + "," + pos.y.ToString("0.000") + "," + pos.z.ToString("0.000") + ")"
                    + " against '" + bg.gameObject.name + "' c(" + bounds.center.x.ToString("0.00") + "," + bounds.center.y.ToString("0.00") + "," + bounds.center.z.ToString("0.00")
                    + ") s(" + bounds.size.x.ToString("0.00") + "," + bounds.size.y.ToString("0.00") + ") layer " + go.layer);
            }
            DumpMenu("placed");
        } catch (Exception e) {
            if (_vrLogs++ < 4) I?.LoggerInstance.Warning("[title] could not place the VR: " + e);
        }
    }

    // ---------- digging from standing height ----------

    private static bool IsDig(VRPlugin instance, Collider col)
    {
        if (_isDigTarget == null) return false;
        try {
            object result = _isDigTarget.Invoke(_isDigTargetStatic ? null : instance, new object[] { col });
            return result is bool b && b;
        } catch { return false; }
    }

    // Additive: only runs when the mod's own search found nothing, so it can only
    // widen what is reachable, never reject something that already worked.
    private static void DigPostfix(VRPlugin __instance, Hand hand, ref RaycastHit best, ref bool __result)
    {
        if (__result) return;
        if (_pDigStanding == null || !_pDigStanding.Value || _isDigTarget == null) return;
        try {
            if (hand == null || !hand.Valid) return;
            const float reachMax = 3.2f;
            float flatMax = Mathf.Clamp(_pDigHorizontal == null ? 1.3f : _pDigHorizontal.Value, 0.2f, 3f);
            float riseMax = Mathf.Clamp(_pDigRise == null ? 0.6f : _pDigRise.Value, 0f, 2f);

            float scoreBest = float.MaxValue;
            RaycastHit found = default;
            bool got = false;

            foreach (var col in Physics.OverlapSphere(hand.AimOrigin, reachMax, -1, QueryTriggerInteraction.Collide)) {
                if (col == null || !col.enabled || !col.gameObject.activeInHierarchy) continue;
                if (!IsDig(__instance, col)) continue;
                var pick = MousePick.For(col);
                if (pick == null || !pick.CanClick) continue;

                Vector3 near = col.bounds.ClosestPoint(hand.AimOrigin) - hand.AimOrigin;
                float flat = new Vector2(near.x, near.z).magnitude;
                if (near.y > riseMax || flat > flatMax) continue;

                Vector3 toward = col.bounds.center - hand.AimOrigin;
                float distance = toward.magnitude;
                if (distance < .001f || distance > reachMax) continue;

                float score = flat + distance * .1f;
                if (score >= scoreBest) continue;

                Vector3 direction = toward / distance;
                Vector3 start = hand.AimOrigin;
                if (col.bounds.Contains(start)) start = col.bounds.center - direction * (col.bounds.extents.magnitude + .1f);
                if (!col.Raycast(new Ray(start, direction), out var hit, reachMax + 1f)) continue;

                float reach = Vector3.Distance(hand.AimOrigin, hit.point);
                if (reach > reachMax) continue;

                bool blocked = false;
                foreach (var obstruction in Physics.RaycastAll(hand.AimOrigin, direction, reach, -1, QueryTriggerInteraction.Ignore)) {
                    var obstacle = obstruction.collider;
                    if (obstacle == null || obstacle == col || obstacle.name.StartsWith("BPVR_") || IsDig(__instance, obstacle)) continue;
                    if (reach - obstruction.distance > .25f) { blocked = true; break; }
                }
                if (blocked) continue;

                hit.distance = reach;
                found = hit;
                scoreBest = score;
                got = true;
            }

            if (got) {
                best = found;
                __result = true;
            }
        } catch (Exception e) { Say("[dig] standing search failed: " + e.Message); }
    }
}
