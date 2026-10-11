using UnityEditor;
using System.IO;
public static class BuildShaders {
 public static void Run() {
  Directory.CreateDirectory("../assets");
  var build = new AssetBundleBuild { assetBundleName="codex-shaders.bundle", assetNames=new[]{"Assets/CodexToon.shader","Assets/FoveatedComposite.shader"} };
  var manifest=BuildPipeline.BuildAssetBundles("../assets",new[]{build},BuildAssetBundleOptions.ForceRebuildAssetBundle,BuildTarget.StandaloneWindows64);
  if(manifest==null) throw new System.Exception("Shader bundle build failed");
 }
}
