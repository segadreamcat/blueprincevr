using UnityEngine;
using Il2CppTMPro;
using System.Text.RegularExpressions;
namespace BluePrinceVR;
public partial class VRPlugin
{
    private float _captionScanAt;
    private float _captionRefreshAt;
    private readonly System.Collections.Generic.List<TMP_Text> _captionTexts=new();
    private void HideRoomCaption()
    {
        if (!_inHouse || Time.unscaledTime<_captionScanAt) return;
        _captionScanAt=Time.unscaledTime+.5f;
        // Scene-wide native searches are expensive in IL2CPP; retain references
        // between refreshes, while still checking label changes twice a second.
        if(Time.unscaledTime>=_captionRefreshAt) {
            _captionRefreshAt=Time.unscaledTime+5f;
            _captionTexts.Clear();
            foreach(var t in UnityEngine.Object.FindObjectsOfType<TextMeshPro>()) CacheCaption(t);
            foreach(var t in UnityEngine.Object.FindObjectsOfType<TextMeshProUGUI>()) CacheCaption(t);
        }
        foreach(var t in _captionTexts) HideCaptionGroup(t);
    }
    private void CacheCaption(TMP_Text text)
    {
        if(text==null) return;
        for(var p=text.transform;p!=null;p=p.parent) {
            string n=p.name;
            if(n.IndexOf("LOWER RIGHT",System.StringComparison.OrdinalIgnoreCase)>=0 ||
                n.IndexOf("ROOM NAME",System.StringComparison.OrdinalIgnoreCase)>=0 ||
                n.IndexOf("ROOMNAME",System.StringComparison.OrdinalIgnoreCase)>=0) {
                _captionTexts.Add(text);return;
            }
        }
    }
    private void HideCaptionGroup(TMP_Text text)
    {
        if(text==null || text.transform.parent==null) return;
        string plain=Regex.Replace(text.text??"","<[^>]*>","").Trim();
        if(!Regex.IsMatch(plain,@"^Rank\s*\d+$",RegexOptions.IgnoreCase)) return;
        bool caption=false;
        for(var p=text.transform;p!=null;p=p.parent) {
            string n=p.name.ToUpperInvariant();
            if(n.Contains("LOWER RIGHT") || n.Contains("ROOM NAME") || n.Contains("ROOMNAME")) caption=true;
        }
        if(!caption) return;
        var labels=text.transform.parent.GetComponentsInChildren<TMP_Text>(true);
        // Only a small standalone title/rank group; never hide a card/menu root.
        if(labels.Length>3) return;
        foreach(var label in labels) if(label!=null) label.alpha=0f;
    }
}
