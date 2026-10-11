using System;
using UnityEngine;
namespace BluePrinceVR;
public partial class VRPlugin
{
    private AudioSource _keyAudio;
    private AudioClip _keyClick;
    private bool _keyAudioFailed;
    private void PlayKeyboardClick()
    {
        if(_keyAudioFailed || _activeComputer==null) return;
        try {
            if(_keyAudio==null) {
                var go=new GameObject("BPVR_KeyboardClick");UnityEngine.Object.DontDestroyOnLoad(go);
                _keyAudio=go.AddComponent(Il2CppInterop.Runtime.Il2CppType.Of<AudioSource>()).Cast<AudioSource>();
                _keyAudio.playOnAwake=false;_keyAudio.spatialBlend=.65f;_keyAudio.volume=.22f;
                _keyAudio.minDistance=.5f;_keyAudio.maxDistance=4f;_keyAudio.dopplerLevel=0f;
                const int rate=22050;var samples=new float[1764];var rng=new System.Random(731);
                for(int i=0;i<samples.Length;i++) {
                    double t=(double)i/rate;
                    double click=Math.Exp(-t*180)*(.7*(rng.NextDouble()*2-1)+.3*Math.Sin(t*2*Math.PI*1800));
                    double clack=t>.014 ? Math.Exp(-(t-.014)*95)*.25*Math.Sin((t-.014)*2*Math.PI*480) : 0;
                    samples[i]=(float)((click+clack)*Math.Min(1,t*5000));
                }
                _keyClick=AudioClip.Create("Codex mechanical key",samples.Length,1,rate,false);
                _keyClick.SetData(samples,0);
            }
            _keyAudio.transform.position=_activeComputer.position;
            _keyAudio.PlayOneShot(_keyClick);
        } catch(Exception e) {_keyAudioFailed=true;LogWarn("[keyboard] Click sound disabled; input/VR remain active: "+e.Message);}
    }
}
