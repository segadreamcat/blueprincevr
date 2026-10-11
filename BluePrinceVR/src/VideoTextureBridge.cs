using System;
using UnityEngine;
using UnityEngine.Video;
namespace BluePrinceVR;
public partial class VRPlugin
{
    private VideoPlayer[] _moviePlayers=Array.Empty<VideoPlayer>();
    private Texture _movieTexture;
    private float _nextMovieScan;
    private int _lastMovieId;
    private bool _movieHasPresented,_moviePresentationEnded;
    private readonly VideoPresentationGate _movieGate=new();
    private void ScanMovieTexture()
    {
        _movieTexture=null;
        try {
            // Antechamber is a live camera cutscene, not a decoded prop video.
            if(_anteCinematic)return;
            if(Time.unscaledTime>=_nextMovieScan) {
                _nextMovieScan=Time.unscaledTime+.5f;
                var players=UnityEngine.Object.FindObjectsOfType<VideoPlayer>();
                _moviePlayers=new VideoPlayer[players.Length];
                for(int i=0;i<players.Length;i++)_moviePlayers[i]=players[i];
            }
            VideoPlayer selected=null;
            foreach(var player in _moviePlayers) {
                if(player==null || !player.isActiveAndEnabled || !player.isPlaying || player.texture==null)continue;
                if(VideoPresentationGate.ReachedEnd(player.isLooping,player.frame,player.frameCount,player.time,player.length))continue;
                // The game retains its Cinematic Player after returning to walking.
                // A texture alone (especially a paused final black frame) is not a presentation.
                if(_inHouse && _visionCamera==null && !_computerKeyboardMode && Cursor.lockState!=CursorLockMode.None)continue;
                // Prefer the currently presenting cinematic over incidental looping
                // videos on props. Never redirect the player's output/audio settings.
                if(selected==null || (!player.isLooping && selected.isLooping) ||
                    (player.isLooping==selected.isLooping && player.GetInstanceID()<selected.GetInstanceID()))selected=player;
            }
            if(selected==null)return;
            int id=selected.GetInstanceID();
            if(!_movieGate.Show(id,selected.frame,selected.time,Time.unscaledTime,selected.isPlaying,true))return;
            _movieTexture=selected.texture;
            _movieHasPresented=true;
            if(selected.GetInstanceID()!=_lastMovieId) {
                _lastMovieId=selected.GetInstanceID();
                Log("[VR video] Mirroring decoded video texture: "+selected.name+" / "+selected.renderMode);
            }
        }
        catch(Exception e) {
            _movieTexture=null;
            if(_lastMovieId!=-1){LogWarn("[VR video] Texture unavailable; retaining camera fallback: "+e.Message);_lastMovieId=-1;}
        }
        finally {
            // Re-arm after leaving the presentation so a later camera-only vision
            // is not suppressed by the completed intro's latch.
            if(_movieTexture==null && _visionCamera==null && !Screens.VideoUp)
                _movieHasPresented=false;
            // The native VIDEO CAM can remain enabled during the house fly-in.
            // Once its decoded movie ends it must not keep an empty flat panel alive.
            _moviePresentationEnded=!_anteCinematic&&_movieHasPresented&&_movieTexture==null;
        }
    }
}
