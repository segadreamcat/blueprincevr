using UnityEngine;
namespace BluePrinceVR;
public partial class VRPlugin
{
    private void FitArmReach(ArmsModel model)
    {
        if (!_importedArms) return;
        var bones=model.Bones;
        var oldMatrices=new Matrix4x4[bones.Length];
        for(int i=0;i<bones.Length;i++) oldMatrices[i]=bones[i].localToWorldMatrix;
        float shoulderWidth=Vector3.Distance(model.UpperArmL.position,model.UpperArmR.position)+.08f;
        var tip=model.Find("f_middle.03.R");
        const float handSize=.88f;
        float handLength=(tip==null ? .14f : Vector3.Distance(model.HandR.position,tip.position)+.02f)*handSize;
        float target=(1.8796f-shoulderWidth)*.5f-handLength;
        float factor=Mathf.Clamp(target/(model.UpperLenR+model.ForeLenR),1f,2f);
        // Move joints along the longer arm chain, then restore all original scales.
        // Rebuild the rest mesh lengthwise below: no giant hands or wider forearms.
        var upperL=model.UpperArmL.localScale;var upperR=model.UpperArmR.localScale;
        var handL=model.HandL.localScale;var handR=model.HandR.localScale;
        model.UpperArmL.localScale=upperL*factor;model.UpperArmR.localScale=upperR*factor;
        model.HandL.localScale=handL/factor;model.HandR.localScale=handR/factor;
        var positions=new Vector3[bones.Length];
        for(int i=0;i<bones.Length;i++) {
            positions[i]=bones[i].position;
            string n=model.BoneNames[i];
            if(n.EndsWith(".L") || n.EndsWith(".R")) {
                var upper=n.EndsWith(".L") ? model.Find("upper_arm.L") : model.Find("upper_arm.R");
                positions[i]+=Vector3.right*(Mathf.Sign(upper.position.x-model.Root.transform.position.x)*.065f);
            }
        }
        model.UpperArmL.localScale=upperL;model.UpperArmR.localScale=upperR;
        model.HandL.localScale=handL;model.HandR.localScale=handR;
        // Asset order is parent before child (validated by the asset tests).
        for(int i=0;i<bones.Length;i++) bones[i].position=positions[i];
        // Shorten the forearm only; leave upper-arm length and hand size alone.
        Vector3 shortenL=(model.HandL.position-model.ForearmL.position)*.28f;
        Vector3 shortenR=(model.HandR.position-model.ForearmR.position)*.28f;
        for(int i=0;i<bones.Length;i++) {
            for(var p=bones[i];p!=null;p=p.parent) {
                if(p==model.HandL) { positions[i]-=shortenL;break; }
                if(p==model.HandR) { positions[i]-=shortenR;break; }
            }
        }
        for(int i=0;i<bones.Length;i++) bones[i].position=positions[i];
        // Shrink finger joint spacing about each wrist, not about the world origin.
        // Parent-first assignment must use captured positions to avoid double scaling.
        for(int i=0;i<bones.Length;i++) {
            Transform wrist=null;
            for(var p=bones[i];p!=null;p=p.parent)
                if(p==model.HandL || p==model.HandR) { wrist=p;break; }
            if(wrist!=null) positions[i]=wrist.position+(positions[i]-wrist.position)*handSize;
        }
        for(int i=0;i<bones.Length;i++) bones[i].position=positions[i];
        var renderer=model.Renderer;var mesh=renderer.sharedMesh;
        var vertices=mesh.vertices;var weights=mesh.boneWeights;
        var matrices=new Matrix4x4[bones.Length];
        var binds=new Matrix4x4[bones.Length];
        for(int i=0;i<bones.Length;i++) {
            string name=model.BoneNames[i];
            bool limb=name.StartsWith("upper_arm.") || name.StartsWith("forearm.");
            bool hand=false;
            for(var p=bones[i];p!=null;p=p.parent)
                if(p==model.HandL || p==model.HandR) { hand=true;break; }
            Vector3 shape=limb ? new Vector3(.85f,factor*(name.StartsWith("forearm.")?.72f:1f),.85f) : hand ? Vector3.one*handSize : Vector3.one;
            matrices[i]=renderer.transform.worldToLocalMatrix*bones[i].localToWorldMatrix*
                Matrix4x4.Scale(shape)*oldMatrices[i].inverse*renderer.transform.localToWorldMatrix;
            binds[i]=bones[i].worldToLocalMatrix*renderer.transform.localToWorldMatrix;
        }
        for(int i=0;i<vertices.Length;i++) {
            var w=weights[i];var p=vertices[i];
            vertices[i]=matrices[w.boneIndex0].MultiplyPoint3x4(p)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(p)*w.weight1+
                matrices[w.boneIndex2].MultiplyPoint3x4(p)*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(p)*w.weight3;
        }
        // Relocate the bend without stretching the sleeve/skin boundary or hand.
        // Keep the completed rest mesh fixed and rebind to the higher elbow.
        for(int side=0;side<2;side++) {
            var upper=side==0?model.UpperArmL:model.UpperArmR;
            var fore=side==0?model.ForearmL:model.ForearmR;
            var wrist=side==0?model.HandL:model.HandR;
            Vector3 wristPosition=wrist.position;
            // One anatomical ratio replaces the accumulated 7 cm elbow offset.
            // Keep controller reach and the hand joints unchanged, but avoid a
            // short upper arm followed by an unnaturally long exposed forearm.
            fore.position=Vector3.Lerp(upper.position,wristPosition,.55f);
            wrist.position=wristPosition;
            int upperIndex=System.Array.IndexOf(bones,upper),foreIndex=System.Array.IndexOf(bones,fore);
            Vector3 axis=(wrist.position-fore.position).normalized;
            for(int v=0;v<vertices.Length;v++) {
                var w=weights[v];
                bool Limb(int index,float weight)=>weight<=.00001f || index==upperIndex || index==foreIndex;
                if(!Limb(w.boneIndex0,w.weight0) || !Limb(w.boneIndex1,w.weight1) ||
                    !Limb(w.boneIndex2,w.weight2) || !Limb(w.boneIndex3,w.weight3)) continue;
                float distance=Vector3.Dot(renderer.transform.TransformPoint(vertices[v])-fore.position,axis);
                float t=Mathf.Clamp01((distance+.050f)/.10f);t=t*t*(3f-2f*t);
                weights[v]=new BoneWeight { boneIndex0=upperIndex,weight0=1f-t,boneIndex1=foreIndex,weight1=t };
            }
        }
        // Keep the proximal wrist on the forearm, then smoothly hand off to the
        // palm. Distance-to-bone weighting previously leaked hand rotation far
        // up the forearm and gave the watch a moving, pinched attachment surface.
        for(int side=0;side<2;side++) {
            var fore=side==0?model.ForearmL:model.ForearmR;
            var wrist=side==0?model.HandL:model.HandR;
            int fi=System.Array.IndexOf(bones,fore),hi=System.Array.IndexOf(bones,wrist);
            var axis=(wrist.position-fore.position).normalized;
            for(int v=0;v<vertices.Length;v++) {
                var w=weights[v];
                bool Wrist(int b,float a)=>a<=.00001f || b==fi || b==hi;
                if(!Wrist(w.boneIndex0,w.weight0)||!Wrist(w.boneIndex1,w.weight1)||
                    !Wrist(w.boneIndex2,w.weight2)||!Wrist(w.boneIndex3,w.weight3))continue;
                float d=Vector3.Dot(renderer.transform.TransformPoint(vertices[v])-wrist.position,axis);
                if(d<-.065f || d>.035f)continue;
                float t=Mathf.Clamp01((d+.015f)/.045f);t=t*t*(3f-2f*t);
                weights[v]=new BoneWeight{boneIndex0=fi,weight0=1f-t,boneIndex1=hi,weight1=t};
            }
        }
        for(int i=0;i<bones.Length;i++) binds[i]=bones[i].worldToLocalMatrix*renderer.transform.localToWorldMatrix;
        mesh.vertices=vertices;mesh.boneWeights=weights;mesh.bindposes=binds;mesh.RecalculateNormals();mesh.RecalculateBounds();
        renderer.localBounds=mesh.bounds;
        model.UpperLenL=Vector3.Distance(model.UpperArmL.position,model.ForearmL.position);
        model.ForeLenL=Vector3.Distance(model.ForearmL.position,model.HandL.position);
        model.UpperLenR=Vector3.Distance(model.UpperArmR.position,model.ForearmR.position);
        model.ForeLenR=Vector3.Distance(model.ForearmR.position,model.HandR.position);
        Log($"[arms] Simon proportion pass: shoulder-to-wrist={model.UpperLenR+model.ForeLenR:F3}m; upper/forearm 55/45, hands 88%, matte ivory sleeves.");
    }
}
