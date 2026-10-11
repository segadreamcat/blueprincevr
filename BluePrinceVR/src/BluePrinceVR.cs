using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using BluePrinceVR;
using HarmonyLib;
using Il2Cpp;
using Il2CppBluePrince.Inputs;
using Il2CppHutongGames.PlayMaker;
using Il2CppHutongGames.PlayMaker.Actions;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppRewired;
using Il2CppSystem;
using Il2CppSystem.Reflection;
using Il2CppTMPro;
using MelonLoader;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: MelonInfo(typeof(VRPlugin), "BluePrinceVR Codex", "2.10.57", "Alex / Codex", null)]
[assembly: MelonGame(null, null)]
[assembly: AssemblyVersion("0.0.0.0")]
namespace BluePrinceVR;

public class ArmsModel
{
	public GameObject Root;

	public SkinnedMeshRenderer Renderer;

	public Transform[] Bones;

	public string[] BoneNames;

	public Transform ShoulderL;

	public Transform UpperArmL;

	public Transform ForearmL;

	public Transform HandL;

	public Transform ShoulderR;

	public Transform UpperArmR;

	public Transform ForearmR;

	public Transform HandR;

	public float UpperLenL;

	public float ForeLenL;

	public float UpperLenR;

	public float ForeLenR;

	public Vector3 HeadLocal;

	public Transform Find(string name)
	{
		for (int i = 0; i < BoneNames.Length; i++)
		{
			if (BoneNames[i] == name)
			{
				return Bones[i];
			}
		}
		return null;
	}
}
public static class ArmsAsset
{
	public static bool IncludeLegacyAccessories = true;
	private const string Magic = "BPVRARM1";

	public static bool Verbose = true;

	public static float JointThickness = 1f;

	public static float ThumbThickness = 0.93f;

	public static float Brightness = 1f;

	public static float Scale = 1f;

	public static bool SwapSides;

	public static string LastError { get; private set; }

	private static Color SleeveColor => Dim(new Color(0.94f, 0.92f, 0.83f));

	private static Color CuffColor => Dim(new Color(0.79f, 0.77f, 0.69f));

	private static void Tint(GameObject go, Color c, Shader shader)
	{
		Renderer component = go.GetComponent<Renderer>();
		if (!(component == null))
		{
			if (shader != null)
			{
				component.material = new Material(shader);
			}
			Texture2D texture2D = new Texture2D(1, 1, TextureFormat.RGBA32, mipChain: false);
			texture2D.SetPixel(0, 0, c);
			texture2D.Apply(updateMipmaps: false);
			texture2D.wrapMode = TextureWrapMode.Repeat;
			component.material.mainTexture = texture2D;
			if (component.material.HasProperty("_Color"))
			{
				component.material.color = c;
			}
			component.shadowCastingMode = ShadowCastingMode.Off;
			component.receiveShadows = false;
		}
	}

	private static Color Dim(Color c)
	{
		return new Color(c.r * Brightness, c.g * Brightness, c.b * Brightness);
	}

	public static ArmsModel Load(string path, Shader shader)
	{
		LastError = null;
		try
		{
			return LoadInner(path, shader);
		}
		catch (System.Exception ex)
		{
			LastError = ex.GetType().Name + ": " + ex.Message;
			return null;
		}
	}

	private static ArmsModel LoadInner(string path, Shader shader)
	{
		if (!File.Exists(path))
		{
			LastError = "not found: " + path;
			return null;
		}
		using (FileStream input = File.OpenRead(path))
		{
			using BinaryReader binaryReader = new BinaryReader(input);
			string text = Encoding.ASCII.GetString(binaryReader.ReadBytes(8));
			if (text != "BPVRARM1")
			{
				LastError = "bad magic '" + text + "'";
				return null;
			}
			Step("reading vertices");
			int num = binaryReader.ReadInt32();
			if (num <= 0 || num > 1000000)
			{
				LastError = "vertex count out of range: " + num;
				return null;
			}
			Vector3[] array = new Vector3[num];
			Vector3[] array2 = new Vector3[num];
			Vector2[] array3 = new Vector2[num];
			BoneWeight[] array4 = new BoneWeight[num];
			for (int i = 0; i < num; i++)
			{
				array[i] = new Vector3(binaryReader.ReadSingle(), binaryReader.ReadSingle(), binaryReader.ReadSingle());
				array2[i] = new Vector3(binaryReader.ReadSingle(), binaryReader.ReadSingle(), binaryReader.ReadSingle());
				array3[i] = new Vector2(binaryReader.ReadSingle(), binaryReader.ReadSingle());
				int boneIndex = binaryReader.ReadInt32();
				int boneIndex2 = binaryReader.ReadInt32();
				int boneIndex3 = binaryReader.ReadInt32();
				int boneIndex4 = binaryReader.ReadInt32();
				float num2 = binaryReader.ReadSingle();
				float num3 = binaryReader.ReadSingle();
				float num4 = binaryReader.ReadSingle();
				float num5 = binaryReader.ReadSingle();
				float num6 = num2 + num3 + num4 + num5;
				if (num6 > 1E-06f)
				{
					num2 /= num6;
					num3 /= num6;
					num4 /= num6;
					num5 /= num6;
				}
				else
				{
					boneIndex = 0;
					num2 = 1f;
					num3 = (num4 = (num5 = 0f));
				}
				array4[i] = new BoneWeight
				{
					boneIndex0 = boneIndex,
					weight0 = num2,
					boneIndex1 = boneIndex2,
					weight1 = num3,
					boneIndex2 = boneIndex3,
					weight2 = num4,
					boneIndex3 = boneIndex4,
					weight3 = num5
				};
			}
			Step("reading triangles");
			int num7 = binaryReader.ReadInt32();
			if (num7 <= 0 || num7 > 3000000)
			{
				LastError = "index count out of range: " + num7;
				return null;
			}
			int[] array5 = new int[num7];
			for (int j = 0; j < num7; j++)
			{
				array5[j] = binaryReader.ReadInt32();
			}
			Step("reading bones");
			int num8 = binaryReader.ReadInt32();
			string[] array6 = new string[num8];
			int[] array7 = new int[num8];
			Vector3[] array8 = new Vector3[num8];
			Quaternion[] array9 = new Quaternion[num8];
			Vector3[] array10 = new Vector3[num8];
			for (int k = 0; k < num8; k++)
			{
				int count = binaryReader.ReadUInt16();
				array6[k] = Encoding.UTF8.GetString(binaryReader.ReadBytes(count));
				array7[k] = binaryReader.ReadInt32();
				array8[k] = new Vector3(binaryReader.ReadSingle(), binaryReader.ReadSingle(), binaryReader.ReadSingle());
				array9[k] = new Quaternion(binaryReader.ReadSingle(), binaryReader.ReadSingle(), binaryReader.ReadSingle(), binaryReader.ReadSingle());
				array10[k] = new Vector3(binaryReader.ReadSingle(), binaryReader.ReadSingle(), binaryReader.ReadSingle());
			}
			Step("reading texture bytes");
			int count2 = binaryReader.ReadInt32();
			byte[] array11 = binaryReader.ReadBytes(count2);
			Step("building rig root");
			ArmsModel armsModel = new ArmsModel();
			armsModel.Root = new GameObject("BPVR_Arms");
			armsModel.Root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
			UnityEngine.Object.DontDestroyOnLoad(armsModel.Root);
			Transform[] array12 = new Transform[num8];
			for (int l = 0; l < num8; l++)
			{
				GameObject gameObject = new GameObject(array6[l]);
				array12[l] = gameObject.transform;
			}
			for (int m = 0; m < num8; m++)
			{
				Transform parent = ((array7[m] >= 0) ? array12[array7[m]] : armsModel.Root.transform);
				array12[m].SetParent(parent, worldPositionStays: false);
				array12[m].localPosition = array8[m];
				array12[m].localRotation = array9[m];
				array12[m].localScale = array10[m];
			}
			Step("building mesh");
			Mesh mesh = new Mesh();
			mesh.name = "BPVR_Arms";
			Step("mesh.vertices");
			mesh.vertices = array;
			Step("mesh.normals");
			mesh.normals = array2;
			Step("mesh.uv");
			mesh.uv = array3;
			Step("mesh.triangles");
			mesh.triangles = array5;
			Step("computing bindposes");
			Matrix4x4[] array13 = new Matrix4x4[num8];
			for (int n = 0; n < num8; n++)
			{
				array13[n] = array12[n].worldToLocalMatrix * armsModel.Root.transform.localToWorldMatrix;
			}
			Step("mesh.bindposes");
			mesh.bindposes = array13;
			Step("assigning bone weights");
			mesh.boneWeights = array4;
			mesh.RecalculateBounds();
			Step("creating SkinnedMeshRenderer");
			GameObject gameObject2 = new GameObject("BPVR_ArmsMesh");
			gameObject2.transform.SetParent(armsModel.Root.transform, worldPositionStays: false);
			SkinnedMeshRenderer skinnedMeshRenderer = gameObject2.AddComponent<SkinnedMeshRenderer>();
			Step("smr.bones");
			skinnedMeshRenderer.bones = array12;
			Step("attaching mesh to renderer");
			skinnedMeshRenderer.sharedMesh = mesh;
			skinnedMeshRenderer.rootBone = array12[0];
			skinnedMeshRenderer.updateWhenOffscreen = true;
			skinnedMeshRenderer.shadowCastingMode = ShadowCastingMode.Off;
			skinnedMeshRenderer.receiveShadows = false;
			Step("decoding texture");
			Texture2D texture2D = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: true);
			if (!texture2D.LoadImage(array11))
			{
				MelonLogger.Warning("[arms] texture failed to decode");
			}
			texture2D.wrapMode = TextureWrapMode.Clamp;
			if (Brightness < 0.999f)
			{
				try
				{
					Il2CppStructArray<Color> pixels = texture2D.GetPixels();
					for (int num9 = 0; num9 < pixels.Length; num9++)
					{
						pixels[num9] = new Color(pixels[num9].r * Brightness, pixels[num9].g * Brightness, pixels[num9].b * Brightness, pixels[num9].a);
					}
					texture2D.SetPixels(pixels);
					texture2D.Apply(updateMipmaps: true);
				}
				catch (System.Exception ex)
				{
					MelonLogger.Warning("[arms] dimming: " + ex.Message);
				}
			}
			Step("creating material");
			Material material = new Material(shader);
			material.mainTexture = texture2D;
			if (material.HasProperty("_Glossiness"))
			{
				material.SetFloat("_Glossiness", 0.1f);
			}
			if (material.HasProperty("_Metallic"))
			{
				material.SetFloat("_Metallic", 0f);
			}
			skinnedMeshRenderer.material = material;
			armsModel.Renderer = skinnedMeshRenderer;
			armsModel.Bones = array12;
			armsModel.BoneNames = array6;
			string text2 = "L";
			string text3 = "R";
			if (SwapSides)
			{
				text2 = "R";
				text3 = "L";
			}
			armsModel.ShoulderL = armsModel.Find("shoulder." + text2);
			armsModel.UpperArmL = armsModel.Find("upper_arm." + text2);
			armsModel.ForearmL = armsModel.Find("forearm." + text2);
			armsModel.HandL = armsModel.Find("hand." + text2);
			armsModel.ShoulderR = armsModel.Find("shoulder." + text3);
			armsModel.UpperArmR = armsModel.Find("upper_arm." + text3);
			armsModel.ForearmR = armsModel.Find("forearm." + text3);
			armsModel.HandR = armsModel.Find("hand." + text3);
			armsModel.Root.transform.localScale = new Vector3(Scale, Scale, Scale);
			if (armsModel.UpperArmL != null && armsModel.UpperArmR != null)
			{
				MelonLogger.Msg($"[arms] left='upper_arm.{text2}' x={armsModel.UpperArmL.position.x:F3}  right='upper_arm.{text3}' x={armsModel.UpperArmR.position.x:F3}  " + "(model faces -Z; the body is turned to match)");
			}
			armsModel.Root.transform.localScale = new Vector3(Scale, Scale, Scale);
			if (armsModel.UpperArmL != null)
			{
				MelonLogger.Msg($"[arms] left chain 'upper_arm.{text2}' x={armsModel.UpperArmL.position.x:F3}, right chain 'upper_arm.{text3}' x={armsModel.UpperArmR.position.x:F3}");
			}
			if (armsModel.UpperArmL != null && armsModel.ForearmL != null && armsModel.HandL != null)
			{
				armsModel.UpperLenL = (armsModel.ForearmL.position - armsModel.UpperArmL.position).magnitude;
				armsModel.ForeLenL = (armsModel.HandL.position - armsModel.ForearmL.position).magnitude;
			}
			if (armsModel.UpperArmR != null && armsModel.ForearmR != null && armsModel.HandR != null)
			{
				armsModel.UpperLenR = (armsModel.ForearmR.position - armsModel.UpperArmR.position).magnitude;
				armsModel.ForeLenR = (armsModel.HandR.position - armsModel.ForearmR.position).magnitude;
			}
			if (IncludeLegacyAccessories)
			{
			Transform[] array14 = new Transform[2] { armsModel.ForearmL, armsModel.ForearmR };
			for (int num10 = 0; num10 < array14.Length; num10++)
			{
				Thicken(array14[num10], JointThickness);
			}
			array14 = new Transform[2] { armsModel.HandL, armsModel.HandR };
			for (int num10 = 0; num10 < array14.Length; num10++)
			{
				Thicken(array14[num10], JointThickness);
			}
			array14 = new Transform[2] { armsModel.UpperArmL, armsModel.UpperArmR };
			for (int num10 = 0; num10 < array14.Length; num10++)
			{
				Thicken(array14[num10], 1f + (JointThickness - 1f) * 0.5f);
			}
			string[] array15 = new string[2] { "L", "R" };
			foreach (string text4 in array15)
			{
				string[] array16 = new string[3] { "thumb.01", "thumb.02", "thumb.03" };
				foreach (string text5 in array16)
				{
					Thicken(armsModel.Find(text5 + "." + text4), ThumbThickness);
				}
			}
			if (armsModel.HandR != null)
			{
				GameObject gameObject3 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
				gameObject3.name = "BPVR_Bracelet";
				Collider component = gameObject3.GetComponent<Collider>();
				if (component != null)
				{
					UnityEngine.Object.Destroy(component);
				}
				gameObject3.transform.SetParent(armsModel.HandR, worldPositionStays: false);
				gameObject3.transform.localPosition = new Vector3(0f, 0.012f, 0f);
				gameObject3.transform.localRotation = Quaternion.identity;
				gameObject3.transform.localScale = new Vector3(0.076f, 0.009f, 0.076f);
				Tint(gameObject3, new Color(0.12f, 0.12f, 0.14f), shader);
			}
			array14 = new Transform[2] { armsModel.ForearmL, armsModel.ForearmR };
			foreach (Transform transform in array14)
			{
				if (!(transform == null))
				{
					GameObject gameObject4 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
					gameObject4.name = "BPVR_ElbowCuff";
					Collider component2 = gameObject4.GetComponent<Collider>();
					if (component2 != null)
					{
						UnityEngine.Object.Destroy(component2);
					}
					gameObject4.transform.SetParent(transform, worldPositionStays: false);
					gameObject4.transform.localPosition = new Vector3(0f, 0.035f, 0f);
					gameObject4.transform.localRotation = Quaternion.identity;
					gameObject4.transform.localScale = new Vector3(0.098f, 0.048f, 0.098f);
					Tint(gameObject4, SleeveColor, shader);
				}
			}
			}
			Transform transform2 = armsModel.Find("camera");
			armsModel.HeadLocal = ((transform2 != null) ? (transform2.position - armsModel.Root.transform.position) : new Vector3(0f, 1.743f * Scale, 0f));
			MelonLogger.Msg($"[arms] loaded {num} verts, {num7 / 3} tris, {num8} bones, upper={armsModel.UpperLenR:F3}m fore={armsModel.ForeLenR:F3}m head@{armsModel.HeadLocal}");
			return armsModel;
		}
		static void Step(string what)
		{
			if (Verbose)
			{
				MelonLogger.Msg("[arms] .. " + what);
			}
		}
		static void Thicken(Transform t, float num12)
		{
			if (!(t == null) && !Mathf.Approximately(num12, 1f))
			{
				Vector3 localScale = t.localScale;
				t.localScale = new Vector3(localScale.x * num12, localScale.y, localScale.z * num12);
			}
		}
	}
}
public static class Beam
{
	public static Vector3 Origin;

	public static Vector3 Direction;

	public static bool Valid;

	public static bool Enabled = true;

	private static int _log;

	public static bool Attached;

	public static int Calls;

	public static int Gated;

	public static int Hits;

	public static int Misses;

	public static bool LastInHouse;

	public static bool LastMenuUp;

	private static float _next;

	public static void Set(Vector3 origin, Vector3 direction, bool valid)
	{
		Origin = origin;
		Direction = direction;
		Valid = valid && direction.sqrMagnitude > 1E-06f;
	}

	public static void Report()
	{
		if (!(Time.unscaledTime < _next))
		{
			_next = Time.unscaledTime + 5f;
			MelonLogger.Msg($"[beam] patch={(Attached ? "on" : "NOT ATTACHED")} calls={Calls} gated={Gated} hit={Hits} miss={Misses} valid={Valid} inHouse={LastInHouse} menuUp={LastMenuUp}");
		}
	}

	public static void Note(string what)
	{
		if (_log++ < 6)
		{
			MelonLogger.Msg("[beam] " + what);
		}
	}
}
[HarmonyPatch(typeof(ActionHelpers), "MousePick", new System.Type[]
{
	typeof(float),
	typeof(int)
})]
internal static class P_ActionHelpersMousePick
{
	private static void Prepare()
	{
		Beam.Attached = true;
	}

	private static void Postfix(float __0, int __1, ref RaycastHit __result)
	{
		Beam.Calls++;
		if (!Beam.Enabled || !Beam.Valid)
		{
			Beam.Gated++;
			return;
		}
		try
		{
			float maxDistance = Mathf.Min((__0 > 0.01f) ? __0 : 1.5f, 1.5f);
			if (!Physics.Raycast(Beam.Origin, Beam.Direction, out var hitInfo, maxDistance, __1, QueryTriggerInteraction.Collide))
			{
				Beam.Misses++;
				__result = default;
				return;
			}
			Beam.Hits++;
			__result = hitInfo;
			Beam.Note($"answering MousePick with '{hitInfo.collider?.name}' at {hitInfo.distance:F2}m");
		}
		catch (System.Exception ex)
		{
			Beam.Note("failed: " + ex.Message);
		}
	}
}
public static class Clickable
{
	public static float RadiusScale = 3f;

	public static float ReachScale = 1.6f;

	public static bool Enabled = true;

	public static Collider Target;

	public static bool Has;

	private static string _chose = "";

	private static bool _pickWarn;

	private static float _baseRadius = -1f;

	private static float _baseReach = -1f;

	private static bool _noted;

	private static float _next;

	public static void Tune(ClickableRaycaster rc)
	{
		if (!Enabled || rc == null)
		{
			return;
		}
		try
		{
			if (_baseRadius < 0f)
			{
				_baseRadius = rc.SpherecastRadius;
				_baseReach = rc.RaycastMaxDist;
				MelonLogger.Msg($"[clickable] game defaults: radius={_baseRadius:F3} reach={_baseReach:F2}");
			}
			rc.SpherecastRadius = Mathf.Min(_baseRadius, 0.025f);
			rc.RaycastMaxDist = Mathf.Min(_baseReach, 1.5f);
		}
		catch (System.Exception ex)
		{
			if (!_noted)
			{
				_noted = true;
				MelonLogger.Warning("[clickable] tune: " + ex.Message);
			}
			Enabled = false;
		}
	}

	public static bool IsBlocker(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return false;
		}
		if (name.IndexOf("blocker", System.StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return true;
		}
		if (name.StartsWith("COLLIDER_", System.StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		return false;
	}

	public static bool IsValid(GameObject go, float dist)
	{
		if (go == null)
		{
			return false;
		}
		try
		{
			ClickableRaycaster active = ClickableRaycaster.Active;
			if (active == null)
			{
				return false;
			}
			return active.IsValidClickable(go, dist);
		}
		catch
		{
			return false;
		}
	}

	public static void Observe(ClickableRaycaster rc)
	{
		if (rc == null)
		{
			return;
		}
		try
		{
			if (Beam.Enabled && Beam.Valid && Physics.Raycast(Beam.Origin, Beam.Direction, out var hitInfo, rc.RaycastMaxDist, rc.RaycastMask, QueryTriggerInteraction.Collide))
			{
				rc._targetHit = hitInfo.collider;
				Il2CppSystem.Collections.Generic.Dictionary<GameObject, MousePickEvent> pickers = rc._pickers;
				GameObject gameObject = null;
				Transform transform = hitInfo.collider.transform;
				int num = 0;
				while (transform != null && num < 6)
				{
					try
					{
						if (pickers != null && pickers.ContainsKey(transform.gameObject))
						{
							gameObject = transform.gameObject;
							break;
						}
					}
					catch
					{
					}
					num++;
					transform = transform.parent;
				}
				Il2CppReferenceArray<ClickableTarget> targets = rc._targets;
				if (gameObject != null && targets != null && targets.Length > 0)
				{
					targets[0].Set(gameObject);
					rc._currentClickable = 0;
					rc._raycastHitCount = 1;
					if (_chose != gameObject.name)
					{
						_chose = gameObject.name;
						MelonLogger.Msg($"[clickable] chose '{gameObject.name}' as the pick ({pickers?.Count ?? 0} registered)");
					}
				}
			}
		}
		catch (System.Exception ex)
		{
			if (!_pickWarn)
			{
				_pickWarn = true;
				MelonLogger.Warning("[clickable] pick: " + ex.Message);
			}
		}
		try
		{
			Has = rc.HasTarget();
			Target = rc.GetCurrentHitCollider();
		}
		catch
		{
			Has = false;
			Target = null;
		}
		if (!(Time.unscaledTime < _next))
		{
			_next = Time.unscaledTime + 5f;
			string targetName = Snapshot.TargetName;
			string value = "?";
			try
			{
				value = rc.GetCurrentScreenPoint().ToString();
			}
			catch
			{
			}
			MelonLogger.Msg($"[clickable] game target='{((Target != null) ? Target.name : "-")}' has={Has} | beam target='{targetName}' | screenPoint={value} of {Screen.width}x{Screen.height} | radius={_baseRadius * RadiusScale:F3} reach={_baseReach * ReachScale:F2} mask=0x{(int)rc.RaycastMask:X}");
		}
	}
}
[HarmonyPatch(typeof(ClickableRaycaster), "LateUpdate")]
internal static class P_ClickableRaycasterLateUpdate
{
	private static void Prefix(ClickableRaycaster __instance)
	{
		Clickable.Tune(__instance);
	}

	private static void Postfix(ClickableRaycaster __instance)
	{
		Clickable.Observe(__instance);
	}
}
public static class DoorProbe
{
	public static bool Enabled = true;

	private static readonly HashSet<string> _seen = new HashSet<string>();

	public static void Look(Collider col)
	{
		if (Enabled && !(col == null) && _seen.Count <= 6)
		{
			Transform transform;
			try
			{
				transform = DoorRoot(col.transform);
			}
			catch
			{
				return;
			}
			if (!(transform == null) && _seen.Add(transform.name))
			{
				StringBuilder stringBuilder = new StringBuilder();
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(18, 1, stringBuilder2);
				handler.AppendLiteral("[door] '");
				handler.AppendFormatted(transform.name);
				handler.AppendLiteral("' subtree:");
				stringBuilder2.AppendLine(ref handler);
				Walk(transform, 0, stringBuilder, 0);
				MelonLogger.Msg(stringBuilder.ToString());
			}
		}
	}

	private static Transform DoorRoot(Transform t)
	{
		Transform result = null;
		int num = 0;
		while (t != null && num < 8)
		{
			if (t.name.IndexOf("Door", System.StringComparison.OrdinalIgnoreCase) >= 0)
			{
				result = t;
			}
			num++;
			t = t.parent;
		}
		return result;
	}

	private static void Walk(Transform t, int depth, StringBuilder sb, int count)
	{
		if (t == null || depth > 4 || count > 40)
		{
			return;
		}
		string text = "";
		try
		{
			Il2CppArrayBase<Component> components = t.GetComponents<Component>();
			for (int i = 0; components != null && i < components.Length; i++)
			{
				if (!(components[i] == null))
				{
					string name;
					try
					{
						name = components[i].GetIl2CppType().Name;
					}
					catch
					{
						continue;
					}
					if (!(name == "Transform"))
					{
						text = text + " " + name;
					}
				}
			}
		}
		catch
		{
		}
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(15, 4, sb);
		handler.AppendLiteral("[door]   ");
		handler.AppendFormatted(new string(' ', depth * 2));
		handler.AppendLiteral("'");
		handler.AppendFormatted(t.name);
		handler.AppendLiteral("'");
		handler.AppendFormatted(text);
		handler.AppendLiteral(" ");
		handler.AppendLiteral("at ");
		handler.AppendFormatted(t.localPosition);
		sb.AppendLine(ref handler);
		for (int j = 0; j < t.childCount && j < 20; j++)
		{
			Walk(t.GetChild(j), depth + 1, sb, count + j + 1);
		}
	}
}
public enum FingerPose
{
	Relax,
	Point,
	Open,
	Fist,
	ThumbsUp
}
public static class Fingers
{
	private class Digit
	{
		public Transform[] Bone = new Transform[3];

		public Quaternion[] Rest = new Quaternion[3];

		public Vector3[] Axis = new Vector3[3];

		public float[] Now = new float[3];

		public Vector3 SpreadAxis;

		public float SpreadSign = 1f;

		public float SpreadNow;
		public float IdleAdduction;
		public float IdleAdductionNow;
		public float ReachSpread;
	}

	private class Side
	{
		public Digit[] D = new Digit[5];

		public bool Ready;

		public Quaternion ThumbUp = Quaternion.identity;

		public bool HaveThumbUp;

		public float ThumbUpNow;

		public Quaternion ThumbRest = Quaternion.identity;
	}

	private static readonly float[,] Curl = new float[5, 15]
	{
		{
			12f, 16f, 14f, 24f, 30f, 26f, 30f, 36f, 30f, 34f,
			40f, 32f, 14f, 12f, 10f
		},
		{
			2f, 2f, 0f, 66f, 82f, 72f, 72f, 86f, 76f, 74f,
			88f, 76f, 22f, 18f, 14f
		},
		{
			4f, 6f, 4f, 6f, 8f, 6f, 8f, 10f, 8f, 10f,
			12f, 10f, 6f, 4f, 4f
		},
		{
			70f, 86f, 78f, 72f, 88f, 80f, 74f, 88f, 80f, 74f,
			90f, 80f, 46f, 38f, 30f
		},
		{
			74f, 90f, 82f, 76f, 90f, 84f, 78f, 90f, 84f, 78f,
			90f, 84f, 0f, 0f, 0f
		}
	};

	private static readonly float[,] Spread = new float[5, 5]
	{
		{ 3f, 0f, -1f, -3f, 0f },
		{ 0f, 4f, 4f, 4f, 0f },
		{ -7f, -3f, 3f, 7f, 0f },
		{ 16f, 5f, -5f, -15f, 0f },
		{ 10f, 3f, -3f, -9f, 0f }
	};

	private static readonly Side[] _side = new Side[2]
	{
		new Side(),
		new Side()
	};

	private static bool _bound;

	public static Vector3 PalmLocalL = Vector3.forward;

	public static Vector3 PalmLocalR = Vector3.forward;

	public static bool PalmKnown;

	public static Vector3 AimFromLocalL;

	public static Vector3 AimDirLocalL;

	public static Vector3 AimFromLocalR;

	public static Vector3 AimDirLocalR;

	public static bool AimKnown;

	private static Transform F(ArmsModel m, string a, string s)
	{
		return m.Find(a + "." + s);
	}

	private static bool _importedRig;

	public static void Bind(ArmsModel m, bool importedRig = false)
	{
		if (m == null || _bound)
		{
			return;
		}
		_bound = true;
		_importedRig = importedRig;
		string[] array = new string[2]
		{
			ArmsAsset.SwapSides ? "R" : "L",
			ArmsAsset.SwapSides ? "L" : "R"
		};
		string[][] array2 = new string[5][]
		{
			new string[3] { "f_index.01", "f_index.02", "f_index.03" },
			new string[3] { "f_middle.01", "f_middle.02", "f_middle.03" },
			new string[3] { "f_ring.01", "f_ring.02", "f_ring.03" },
			new string[3] { "f_pinky.01", "f_pinky.02", "f_pinky.03" },
			new string[3] { "thumb.01", "thumb.02", "thumb.03" }
		};
		for (int i = 0; i < 2; i++)
		{
			Side side = _side[i];
			string text = array[i];
			Transform transform = F(m, "f_index.01", text);
			Transform transform2 = F(m, "f_pinky.01", text);
			Transform transform3 = F(m, "thumb.01", text);
			Transform transform4 = F(m, "f_index.03", text);
			Transform transform5 = F(m, "f_pinky.03", text);
			if (transform == null || transform2 == null || transform3 == null || transform4 == null || transform5 == null)
			{
				MelonLogger.Warning("[fingers] " + text + ": bones missing, hands stay static");
				continue;
			}
			Vector3 normalized = ((transform4.position + transform5.position) * 0.5f - (transform.position + transform2.position) * 0.5f).normalized;
			Vector3 normalized2 = (transform2.position - transform.position).normalized;
			normalized2 = (normalized2 - normalized * Vector3.Dot(normalized2, normalized)).normalized;
			Vector3 vector = Vector3.Cross(normalized2, normalized).normalized;
			if (Vector3.Dot(vector, transform3.position - transform.position) < 0f)
			{
				vector = -vector;
			}
			Vector3 normalized3 = Vector3.Cross(normalized, vector).normalized;
			Transform transform6 = F(m, "hand", text);
			if (transform6 != null)
			{
				Vector3 vector2 = Quaternion.Inverse(transform6.rotation) * vector;
				if (i == 0)
				{
					PalmLocalL = vector2;
				}
				else
				{
					PalmLocalR = vector2;
				}
				PalmKnown = true;
				Transform transform7 = F(m, "f_index.01", text);
				Transform transform8 = F(m, "f_index.03", text);
				if (transform7 != null && transform8 != null)
				{
					Vector3 normalized4 = (transform8.position - transform7.position).normalized;
					Vector3 position = transform8.position + normalized4 * 0.03f;
					Vector3 vector3 = transform6.InverseTransformDirection(normalized4);
					Vector3 vector4 = transform6.InverseTransformPoint(position);
					if (i == 0)
					{
						AimDirLocalL = vector3;
						AimFromLocalL = vector4;
					}
					else
					{
						AimDirLocalR = vector3;
						AimFromLocalR = vector4;
					}
					AimKnown = true;
				}
			}
			for (int j = 0; j < 5; j++)
			{
				Digit digit = new Digit();
				Vector3 vector5 = normalized3;
				if (j == 4)
				{
					Transform transform9 = F(m, "thumb.01", text);
					Transform transform10 = F(m, "thumb.03", text);
					if (transform9 != null && transform10 != null)
					{
						Vector3 vector6 = Vector3.Cross((transform10.position - transform9.position).normalized, vector);
						if (vector6.sqrMagnitude > 1E-06f)
						{
							vector5 = vector6.normalized;
						}
					}
				}
				Transform transform11 = F(m, array2[j][0], text);
				Transform transform12 = F(m, array2[j][2], text);
				Transform transform13 = F(m, "f_middle.01", text);
				if (transform11 != null && transform12 != null && transform13 != null)
				{
					Vector3 rhs = transform12.position - transform11.position;
					Vector3 lhs = Vector3.Cross(normalized, rhs);
					Vector3 rhs2 = transform13.position - transform11.position;
					digit.SpreadSign = ((Vector3.Dot(lhs, rhs2) >= 0f) ? 1f : (-1f));
					digit.SpreadAxis = Quaternion.Inverse(transform11.rotation) * normalized;
					if (importedRig)
					{
						// Adduct in the palm plane, never roll a finger around its shaft.
						Vector3 middleDirection = transform13.rotation * Vector3.up;
						Vector3 dorsal = transform11.rotation * Vector3.forward;
						Vector3 target = Vector3.ProjectOnPlane(middleDirection - normalized2 * (j == 3 ? .08f : 0f), dorsal).normalized;
						digit.IdleAdduction = Mathf.Clamp(Vector3.SignedAngle(transform11.rotation * Vector3.up, target, dorsal), j == 4 ? -30f : -15f, j == 4 ? 30f : 15f) * (j == 4 ? .95f : .85f);
						float spread = j == 0 ? -.55f : j == 1 ? -.12f : j == 2 ? .28f : j == 3 ? .7f : -1.15f;
						Vector3 reach = Vector3.ProjectOnPlane(middleDirection + normalized2 * spread, dorsal).normalized;
						digit.ReachSpread = Mathf.Clamp(Vector3.SignedAngle(transform11.rotation * Vector3.up, reach, dorsal), -25f, 25f);
					}
				}
				for (int k = 0; k < 3; k++)
				{
					Transform transform14 = F(m, array2[j][k], text);
					digit.Bone[k] = transform14;
					if (!(transform14 == null))
					{
						digit.Rest[k] = transform14.localRotation;
						digit.Axis[k] = Quaternion.Inverse(transform14.rotation) * vector5;
					}
				}
				side.D[j] = digit;
			}
			Transform transform15 = F(m, "thumb.01", text);
			Transform transform16 = F(m, "thumb.03", text);
			if (transform15 != null && transform16 != null && transform15.parent != null)
			{
				Vector3 normalized5 = (transform16.position - transform15.position).normalized;
				Quaternion quaternion = Quaternion.Inverse(transform15.parent.rotation);
				Vector3 normalized6 = (-normalized2 - vector * 0.28f).normalized;
				side.ThumbUp = quaternion * (Quaternion.FromToRotation(normalized5, normalized6) * transform15.rotation);
				Vector3 normalized7 = (normalized * 0.85f - normalized2 * 0.35f + vector * 0.2f).normalized;
				side.ThumbRest = quaternion * (Quaternion.FromToRotation(normalized5, normalized7) * transform15.rotation);
				side.HaveThumbUp = true;
			}
			side.Ready = true;
		}
		MelonLogger.Msg("[fingers] bound; curl axis measured from the rest pose");
	}

	public static void Drive(bool isLeft, float grip, float trigger, float thumb, float dt, bool grabReady = false, bool openHand = false)
	{
		int num = ((!isLeft) ? 1 : 0);
		Side side = _side[num];
		if (!side.Ready)
		{
			return;
		}
		grip = Mathf.Clamp01(grip);
		trigger = Mathf.Clamp01(trigger);
		thumb = Mathf.Clamp01(thumb);
		if (_importedRig)
		{
			// Palmward Blender -X flexion becomes Unity +X after basis reflection.
			// Idle adduction is measured in each imported bone's palm plane.
			float blend = 1f - Mathf.Exp(-18f * Mathf.Clamp(dt, 0f, 0.1f));
			for (int finger = 0; finger < 5; finger++)
			{
				Digit digit = side.D[finger];
				if (digit == null) continue;
				bool thumbUp = finger == 4 && grip > .5f && thumb < .5f;
				float adduction = thumbUp ? digit.IdleAdduction :
					(grabReady ? digit.ReachSpread : digit.IdleAdduction) * FingerControls.IdleTogether(grip, trigger, false);
				digit.IdleAdductionNow = Mathf.Lerp(digit.IdleAdductionNow, adduction, blend);
				for (int joint = 0; joint < 3; joint++)
				{
					if (digit.Bone[joint] == null) continue;
					float angle = FingerControls.PoseAngle(finger, joint, grip, trigger, grabReady, openHand);
					if (finger == 4 && grip > .5f && thumb < .5f) angle = 0f;
					digit.Now[joint] = Mathf.Lerp(digit.Now[joint], angle, blend);
					digit.Bone[joint].localRotation = digit.Rest[joint] *
						Quaternion.AngleAxis(joint == 0 ? digit.IdleAdductionNow : 0f, Vector3.forward) *
						Quaternion.AngleAxis(digit.Now[joint], Vector3.right);
					// Imported thumb uses its own local joint axes, never the legacy
					// whole-thumb target which can rotate it backward through the wrist.
				}
			}
			return;
		}
		float[] array = new float[5] { trigger, grip, grip, grip, 0f };
		float t = 1f - Mathf.Exp(-18f * Mathf.Max(dt, 0.0001f));
		side.ThumbUpNow = Mathf.Lerp(side.ThumbUpNow, (grip > 0.5f && thumb < 0.5f) ? 1f : 0f, t);
		for (int i = 0; i < 5; i++)
		{
			Digit digit = side.D[i];
			if (digit == null)
			{
				continue;
			}
			float t2 = array[i];
			digit.SpreadNow = Mathf.Lerp(digit.SpreadNow, Mathf.Lerp(Spread[0, i], Spread[3, i], t2), t);
			for (int j = 0; j < 3; j++)
			{
				Transform transform = digit.Bone[j];
				if (!(transform == null))
				{
					float b = Mathf.Lerp(Curl[0, i * 3 + j], Curl[3, i * 3 + j], t2);
					digit.Now[j] = Mathf.Lerp(digit.Now[j], b, t);
					Quaternion localRotation = digit.Rest[j] * Quaternion.AngleAxis(digit.Now[j], digit.Axis[j]);
					if (j == 0 && digit.SpreadAxis.sqrMagnitude > 1E-06f)
					{
						localRotation *= Quaternion.AngleAxis(digit.SpreadNow * digit.SpreadSign, digit.SpreadAxis);
					}
					if (i == 4 && side.HaveThumbUp)
					{
						localRotation = ((j == 0) ? Quaternion.Slerp(side.ThumbRest, side.ThumbUp, side.ThumbUpNow) : digit.Rest[j]);
					}
					transform.localRotation = localRotation;
				}
			}
		}
	}
}
public static class VRAim
{
	public static Vector2 ScreenPoint;

	public static bool Valid;

	public static int Called;

	public static int MouseReads;

	public static int RewiredReads;

	public static bool Bypass;

	private static int _logged;

	public static void Note(string what)
	{
		if (_logged++ < 5)
		{
			MelonLogger.Msg("[pick] " + what);
		}
	}
}
[HarmonyPatch(typeof(ClickableRaycaster), "GetCurrentScreenPoint")]
internal static class P_ClickableScreenPoint
{
	private static void Postfix(ref Vector2 __result)
	{
		VRAim.Called++;
		if (VRAim.Called <= 3)
		{
			MelonLogger.Msg($"[pick] GetCurrentScreenPoint: game {__result}, ours {VRAim.ScreenPoint} (valid={VRAim.Valid})");
		}
		if (VRAim.Valid)
		{
			__result = VRAim.ScreenPoint;
		}
	}
}
[HarmonyPatch(typeof(Mouse), "screenPosition", MethodType.Getter)]
internal static class P_RewiredMouse
{
	private static void Postfix(ref Vector2 __result)
	{
		if (!VRAim.Bypass && VRAim.Valid)
		{
			VRAim.RewiredReads++;
			if (VRAim.RewiredReads <= 3)
			{
				MelonLogger.Msg($"[pick] Rewired mouse asked: real {__result}, answering {VRAim.ScreenPoint}");
			}
			__result = VRAim.ScreenPoint;
		}
	}
}
[HarmonyPatch(typeof(Input), "mousePosition", MethodType.Getter)]
internal static class P_MousePosition
{
	private static void Postfix(ref Vector3 __result)
	{
		if (!VRAim.Bypass && VRAim.Valid)
		{
			VRAim.MouseReads++;
			if (VRAim.MouseReads <= 3)
			{
				MelonLogger.Msg($"[pick] mousePosition asked: real {__result}, answering {VRAim.ScreenPoint}");
			}
			__result = new Vector3(VRAim.ScreenPoint.x, VRAim.ScreenPoint.y, 0f);
		}
	}
}
public static class Grab
{
	private static int _log;

	public static bool SendNamedEvents;

	private static readonly string[] ClickEvents = new string[2] { "MOUSE DOWN", "MOUSE UP AS BUTTON" };

	private static readonly string[] Dangerous = new string[23]
	{
		"open", "sesame", "dead", "secret", "lock", "skip", "destruct", "freeze", "disable", "north",
		"east", "south", "west", "security", "plan", "passage", "axe", "greenhouse", "begin", "go",
		"create", "experiment", "check"
	};

	private static readonly string[] ClickNames = new string[9] { "click", "onclick", "interact", "use", "select", "activate", "take", "grab", "pickup" };

	private static readonly HashSet<string> _dumped = new HashSet<string>();


	private static string SafeState(PlayMakerFSM fsm)
	{
		try
		{
			return fsm.Fsm.ActiveStateName ?? "";
		}
		catch
		{
			return "";
		}
	}

	public static PlayMakerFSM Find(Collider col)
	{
		if (col == null)
		{
			return null;
		}
		Transform transform = col.transform;
		int num = 0;
		while (transform != null && num < 6)
		{
			PlayMakerFSM component = transform.gameObject.GetComponent<PlayMakerFSM>();
			if (component != null)
			{
				return component;
			}
			num++;
			transform = transform.parent;
		}
		return null;
	}

	private static System.Collections.Generic.List<string> Listening(PlayMakerFSM fsm)
	{
		System.Collections.Generic.List<string> list = new System.Collections.Generic.List<string>();
		try
		{
			Fsm fsm2 = fsm.Fsm;
			FsmState activeState = fsm2.ActiveState;
			if (activeState != null)
			{
				Il2CppReferenceArray<FsmTransition> transitions = activeState.Transitions;
				int num = 0;
				while (transitions != null && num < transitions.Length)
				{
					string text = ((transitions[num] != null) ? transitions[num].EventName : null);
					if (!string.IsNullOrEmpty(text))
					{
						list.Add(text);
					}
					num++;
				}
			}
			Il2CppReferenceArray<FsmTransition> globalTransitions = fsm2.GlobalTransitions;
			int num2 = 0;
			while (globalTransitions != null && num2 < globalTransitions.Length)
			{
				string text2 = ((globalTransitions[num2] != null) ? globalTransitions[num2].EventName : null);
				if (!string.IsNullOrEmpty(text2) && !list.Contains(text2))
				{
					list.Add(text2);
				}
				num2++;
			}
		}
		catch (System.Exception ex)
		{
			if (_log++ < 6)
			{
				MelonLogger.Warning("[grab] reading transitions: " + ex.Message);
			}
		}
		return list;
	}

	private static bool IsMouse(string n)
	{
		if (n != null)
		{
			return n.IndexOf("MOUSE", System.StringComparison.OrdinalIgnoreCase) >= 0;
		}
		return false;
	}

	private static bool IsSafeEvent(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return false;
		}
		if (name.Equals("FINISHED", System.StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		if (name.StartsWith("Event ", System.StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		if (name.IndexOf("Tile ", System.StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return false;
		}
		bool flag = true;
		for (int i = 0; i < name.Length; i++)
		{
			if (!char.IsDigit(name[i]))
			{
				flag = false;
				break;
			}
		}
		if (flag)
		{
			return false;
		}
		string[] dangerous = Dangerous;
		foreach (string value in dangerous)
		{
			if (name.IndexOf(value, System.StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return false;
			}
		}
		return true;
	}

	private static string BestClickEvent(System.Collections.Generic.List<string> names)
	{
		string[] clickNames = ClickNames;
		foreach (string value in clickNames)
		{
			foreach (string name in names)
			{
				if (name.Equals(value, System.StringComparison.OrdinalIgnoreCase))
				{
					return name;
				}
			}
		}
		clickNames = ClickNames;
		foreach (string value2 in clickNames)
		{
			foreach (string name2 in names)
			{
				if (name2.IndexOf(value2, System.StringComparison.OrdinalIgnoreCase) >= 0 && IsSafeEvent(name2))
				{
					return name2;
				}
			}
		}
		return null;
	}

	private static void DumpGraph(PlayMakerFSM fsm, string label)
	{
		try
		{
			if (fsm == null || _dumped.Count > 40 || !_dumped.Add(label))
			{
				return;
			}
			Fsm fsm2 = fsm.Fsm;
			MelonLogger.Msg($"[dump] '{label}' fsm '{fsm.FsmName}' at '{fsm2.ActiveStateName}'");
			Il2CppReferenceArray<FsmState> states = fsm2.States;
			int num = 0;
			while (states != null && num < states.Length && num < 24)
			{
				FsmState fsmState = states[num];
				if (fsmState != null)
				{
					StringBuilder stringBuilder = new StringBuilder();
					Il2CppReferenceArray<FsmStateAction> actions = fsmState.Actions;
					int num2 = 0;
					while (actions != null && num2 < actions.Length)
					{
						if (actions[num2] != null)
						{
							string name = actions[num2].GetIl2CppType().Name;
							stringBuilder.Append(name).Append(' ');
						}
						num2++;
					}
					StringBuilder stringBuilder2 = new StringBuilder();
					Il2CppReferenceArray<FsmTransition> transitions = fsmState.Transitions;
					int num3 = 0;
					while (transitions != null && num3 < transitions.Length)
					{
						if (transitions[num3] != null)
						{
							stringBuilder2.Append('\'').Append(transitions[num3].EventName).Append("' ");
						}
						num3++;
					}
					MelonLogger.Msg($"[dump]   state '{fsmState.Name}' actions: {stringBuilder}| exits: {stringBuilder2}");
				}
				num++;
			}
		}
		catch (System.Exception ex)
		{
			MelonLogger.Warning("[dump] " + ex.Message);
		}
	}

	private static void DumpAncestors(Collider col)
	{
		try
		{
			Transform transform = ((col != null) ? col.transform : null);
			for (int i = 0; transform != null && i < 8; i++, transform = transform.parent)
			{
				PlayMakerFSM[] array;
				try
				{
					array = transform.gameObject.GetComponents<PlayMakerFSM>();
				}
				catch
				{
					continue;
				}
				if (array == null)
				{
					continue;
				}
				foreach (PlayMakerFSM playMakerFSM in array)
				{
					if (!(playMakerFSM == null))
					{
						string value = "?";
						string value2 = "?";
						try
						{
							value = playMakerFSM.Fsm.ActiveStateName;
							value2 = playMakerFSM.FsmName;
						}
						catch
						{
						}
						MelonLogger.Msg($"[dump]   ancestor {i} '{transform.name}' fsm '{value2}' at '{value}'");
					}
				}
			}
		}
		catch
		{
		}
	}

	private static bool IsPointerState(Fsm f, string stateName)
	{
		if (f == null || string.IsNullOrEmpty(stateName))
		{
			return false;
		}
		try
		{
			Il2CppReferenceArray<FsmState> states = f.States;
			int num = 0;
			while (states != null && num < states.Length)
			{
				FsmState fsmState = states[num];
				if (fsmState != null && !(fsmState.Name != stateName))
				{
					Il2CppReferenceArray<FsmStateAction> actions = fsmState.Actions;
					for (int i = 0; actions != null && i < actions.Length; i++)
					{
						if (actions[i] != null)
						{
							string name;
							try
							{
								name = actions[i].GetIl2CppType().Name;
							}
							catch
							{
								continue;
							}
							if (name.IndexOf("MousePick", System.StringComparison.Ordinal) >= 0)
							{
								return true;
							}
						}
					}
					return false;
				}
				num++;
			}
		}
		catch
		{
		}
		return false;
	}

	private static string ClickDestination(Fsm f, string ev)
	{
		if (f == null || string.IsNullOrEmpty(ev))
		{
			return null;
		}
		try
		{
			Il2CppReferenceArray<FsmState> states = f.States;
			int num = 0;
			while (states != null && num < states.Length)
			{
				FsmState fsmState = states[num];
				if (fsmState != null)
				{
					Il2CppReferenceArray<FsmTransition> transitions = fsmState.Transitions;
					for (int i = 0; transitions != null && i < transitions.Length; i++)
					{
						if (transitions[i] == null)
						{
							continue;
						}
						string eventName;
						try
						{
							eventName = transitions[i].EventName;
						}
						catch
						{
							continue;
						}
						if (string.Equals(eventName, ev, System.StringComparison.Ordinal))
						{
							string toState;
							try
							{
								toState = transitions[i].ToState;
							}
							catch
							{
								continue;
							}
							if (!string.IsNullOrEmpty(toState))
							{
								return toState;
							}
						}
					}
				}
				num++;
			}
		}
		catch
		{
		}
		return null;
	}

	private static string StateListeningFor(Fsm f, string ev)
	{
		if (f == null || string.IsNullOrEmpty(ev))
		{
			return null;
		}
		try
		{
			Il2CppReferenceArray<FsmState> states = f.States;
			int num = 0;
			while (states != null && num < states.Length)
			{
				FsmState fsmState = states[num];
				if (fsmState != null)
				{
					Il2CppReferenceArray<FsmTransition> transitions = fsmState.Transitions;
					for (int i = 0; transitions != null && i < transitions.Length; i++)
					{
						if (transitions[i] != null)
						{
							string eventName;
							try
							{
								eventName = transitions[i].EventName;
							}
							catch
							{
								continue;
							}
							if (string.Equals(eventName, ev, System.StringComparison.Ordinal))
							{
								return fsmState.Name;
							}
						}
					}
				}
				num++;
			}
		}
		catch
		{
		}
		return null;
	}

	public static void TickPending()
	{
		// No delayed retries or forced FSM states: room generation owns transitions.
	}

	public static bool Click(Collider col)
	{
		try
		{
			PickEvents pick = MousePick.For(col);
			if (pick == null || !pick.CanClick || pick.Fsm == null) return false;
			if (!IsPointerState(pick.Fsm.Fsm, SafeState(pick.Fsm))) return false;
			MousePick.Send(pick.Fsm, pick.Over);
			// Exactly one recognized click. Never guess events, force SetState,
			// or also inject a desktop click after this path consumes the press.
			MousePick.Send(pick.Fsm, pick.Down);
			MousePick.Send(pick.Fsm, pick.Up);
			if (_log++ < 40) MelonLogger.Msg("[grab] single pointer click: " + col.name);
			return true;
		}
		catch (System.Exception ex)
		{
			if (_log++ < 40) MelonLogger.Warning("[grab] " + ex.Message);
			return false;
		}
	}
	public static void Tick()
	{
	}
	public static bool Ready(PickEvents pick)
	{
		try {
			if(pick==null||!pick.CanClick||pick.Fsm==null||!pick.Fsm.isActiveAndEnabled)return false;
			var active=pick.Fsm.Fsm.ActiveState;if(active==null)return false;
			foreach(var action in active.Actions)
				if(action!=null&&action.GetIl2CppType().Name.IndexOf("MousePick",System.StringComparison.Ordinal)>=0)return true;
			return false;
		}
		catch { return false; }
	}
}
public class Hand
{
	public bool RayFilterReady;
	public Vector3 FilteredRayOrigin, FilteredRayDirection;
	public GameObject Root;

	public GameObject HandNode;

	public Transform Tip;

	public GameObject RayObj;

	public GameObject HitDot;

	public GameObject Shoulder;

	public GameObject UpperArm;

	public GameObject Elbow;

	public GameObject Forearm;

	public GameObject Wrist;

	public uint DeviceIndex = uint.MaxValue;

	public bool IsLeft;

	public bool Valid;

	public float HiddenSince;

	public bool TriggerDown;

	public bool DirectClickConsumed;

	public bool TriggerWasDown;

	public bool GripDown;

	public bool GripWasDown;

	public float GripAmount;

	public float TriggerAmount;

	public float ThumbAmount;

	public bool OverGrabbable;

	public Vector3 AimOrigin;

	public Vector3 AimDirection;

	public Collider AimCollider;
	public Vector3 AimPoint, AimNormal;
	public float AimDistance;
	public bool AimClickable;
}
public static class HandRig
{
    private static ArmsModel _reachModel;
    private static Vector3 _reachRestL,_reachRestR;
	private static readonly Color Skin = new Color(1f, 0.87f, 0.78f);

	private static readonly Color Shade = new Color(0.89f, 0.73f, 0.63f);

	private static readonly Color Band = new Color(0.12f, 0.12f, 0.14f);

	private static readonly Color Sleeve = new Color(0.97f, 0.96f, 0.9f);

	private static readonly Color RayCol = new Color(0.45f, 0.7f, 1f);

	private static readonly Color GrabCol = new Color(1f, 0.82f, 0.28f);

	private static readonly Color DotCol = new Color(0.94f, 0.92f, 0.86f);

	public static readonly Quaternion GemTilt = Quaternion.Euler(35.264f, 45f, 0f);

	public const float UpperLen = 0.28f;

	public const float ForeLen = 0.27f;

	public static ArmsModel Model;

	public static Vector3 HandEuler = new Vector3(90f, 0f, 0f);

	public static bool ImportedRig;

	public static float BodyYaw = 180f;

	public static float ShoulderDrop = 0.08f;

	public static Vector3 HeadPos;

	public static bool ShowBeam = false;

	public static void PlaceBody(Vector3 headPos, Quaternion headRot)
	{
		HeadPos = headPos;
		if (Model != null && !(Model.Root == null))
		{
			Vector3 vector = headRot * Vector3.forward;
			vector.y = 0f;
			Quaternion quaternion = ((vector.sqrMagnitude > 1E-06f) ? Quaternion.LookRotation(vector.normalized, Vector3.up) : Quaternion.identity) * Quaternion.Euler(0f, BodyYaw, 0f);
			Vector3 vector2 = Model.HeadLocal + Vector3.up * ShoulderDrop;
			Model.Root.transform.SetPositionAndRotation(headPos - quaternion * vector2, quaternion);
		}
	}

	private static Quaternion AimY(Vector3 dir, Vector3 rollRef)
	{
		dir = dir.normalized;
		Vector3 vector = rollRef - dir * Vector3.Dot(rollRef, dir);
		if (vector.sqrMagnitude < 1E-06f)
		{
			vector = Vector3.Cross(dir, Vector3.right);
			if (vector.sqrMagnitude < 1E-06f)
			{
				vector = Vector3.Cross(dir, Vector3.up);
			}
		}
		return Quaternion.LookRotation(vector.normalized, dir);
	}

	public static Hand Build(string name, bool leftHand)
	{
		Hand hand = new Hand();
		hand.IsLeft = leftHand;
		hand.Root = new GameObject(name);
		UnityEngine.Object.DontDestroyOnLoad(hand.Root);
		if (Model != null)
		{
			hand.HandNode = new GameObject("hand");
			hand.HandNode.transform.SetParent(hand.Root.transform, worldPositionStays: false);
			GameObject gameObject = new GameObject("tip");
			gameObject.transform.SetParent(hand.HandNode.transform, worldPositionStays: false);
			gameObject.transform.localPosition = new Vector3(0f, 0f, 0.105f);
			hand.Tip = gameObject.transform;
			hand.RayObj = Local(PrimitiveType.Cylinder, hand.Tip, RayCol, new Vector3(0f, 0f, 0.5f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.004f, 0.5f, 0.004f), "aimray");
			hand.HitDot = Joint(null, DotCol, 0.03f, "hitdot", PrimitiveType.Cube);
			UnityEngine.Object.DontDestroyOnLoad(hand.HitDot);
			return hand;
		}
		hand.Shoulder = Joint(hand.Root.transform, Sleeve, 0.062f, "shoulder");
		hand.UpperArm = Joint(hand.Root.transform, Sleeve, 0.05f, "upperarm", PrimitiveType.Capsule);
		hand.Elbow = Joint(hand.Root.transform, Sleeve, 0.05f, "elbow");
		hand.Forearm = Joint(hand.Root.transform, Skin, 0.046f, "forearm", PrimitiveType.Capsule);
		hand.Wrist = Joint(hand.Root.transform, Skin, 0.036f, "wrist");
		hand.HandNode = new GameObject("hand");
		hand.HandNode.transform.SetParent(hand.Root.transform, worldPositionStays: false);
		float num = (leftHand ? (-1f) : 1f);
		if (!leftHand)
		{
			Local(PrimitiveType.Cylinder, hand.HandNode.transform, Band, new Vector3(0f, 0f, -0.048f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.074f, 0.018f, 0.07f), "bracelet");
		}
		Local(PrimitiveType.Sphere, hand.HandNode.transform, Skin, new Vector3(0f, 0f, 0.02f), Quaternion.identity, new Vector3(0.078f, 0.046f, 0.105f), "hand");
		Local(PrimitiveType.Sphere, hand.HandNode.transform, Skin, new Vector3(0f, 0f, 0.072f), Quaternion.identity, new Vector3(0.062f, 0.038f, 0.052f), "knuckles");
		Local(PrimitiveType.Capsule, hand.HandNode.transform, Skin, new Vector3(0.036f * num, 0.002f, 0.026f), Quaternion.Euler(80f, 0f, -40f * num), new Vector3(0.02f, 0.028f, 0.02f), "thumb");
		GameObject gameObject2 = new GameObject("tip");
		gameObject2.transform.SetParent(hand.HandNode.transform, worldPositionStays: false);
		gameObject2.transform.localPosition = new Vector3(0f, 0f, 0.105f);
		hand.Tip = gameObject2.transform;
		hand.RayObj = Local(PrimitiveType.Cylinder, hand.Tip, RayCol, new Vector3(0f, 0f, 0.5f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.004f, 0.5f, 0.004f), "aimray");
		hand.HitDot = Joint(null, DotCol, 0.03f, "hitdot", PrimitiveType.Cube);
		UnityEngine.Object.DontDestroyOnLoad(hand.HitDot);
		return hand;
	}

	public static void Solve(Hand h, Vector3 shoulder, Vector3 handPos, Quaternion handRot, Vector3 pole)
	{
		if (h == null)
		{
			return;
		}
		if (Model != null)
		{
			SolveModel(h, handPos, handRot, pole);
			return;
		}
		Vector3 vector = handPos - shoulder;
		float num = 0.55f;
		float num2 = Mathf.Clamp(vector.magnitude, 0.02f, num - 0.005f);
		Vector3 vector2 = ((vector.sqrMagnitude > 1E-06f) ? vector.normalized : Vector3.forward);
		float num3 = (0.0054999962f + num2 * num2) / (2f * num2);
		float num4 = Mathf.Sqrt(Mathf.Max(0f, 0.0784f - num3 * num3));
		Vector3 vector3 = pole - vector2 * Vector3.Dot(pole, vector2);
		vector3 = ((vector3.sqrMagnitude > 1E-06f) ? vector3.normalized : Vector3.up);
		Vector3 vector4 = shoulder + vector2 * num3 + vector3 * num4;
		Vector3 vector5 = shoulder + vector2 * num2;
		Vector3 vector6 = vector5 - vector4;
		vector6 = ((vector6.sqrMagnitude > 1E-06f) ? vector6.normalized : vector2);
		Vector3 to = handRot * Vector3.forward;
		Vector3 upwards = handRot * Vector3.up;
		float num5 = Vector3.Angle(vector6, to);
		Quaternion quaternion = handRot;
		if (num5 > 62f)
		{
			quaternion = Quaternion.RotateTowards(Quaternion.LookRotation(vector6, upwards), handRot, 62f);
		}
		SetPos(h.Shoulder, shoulder);
		SetPos(h.Elbow, vector4);
		SetPos(h.Wrist, vector5);
		Segment(h.UpperArm, shoulder, vector4, 0.05f, vector3);
		Segment(h.Forearm, vector4, vector5, 0.046f, quaternion * Vector3.forward);
		if (h.HandNode != null)
		{
			h.HandNode.transform.SetPositionAndRotation(vector5, quaternion);
		}
	}

	private static void SolveModel(Hand h, Vector3 handPos, Quaternion handRot, Vector3 pole)
	{
		Transform transform = (h.IsLeft ? Model.UpperArmL : Model.UpperArmR);
		Transform transform2 = (h.IsLeft ? Model.ForearmL : Model.ForearmR);
		Transform transform3 = (h.IsLeft ? Model.HandL : Model.HandR);
		if (transform == null || transform2 == null || transform3 == null)
		{
			return;
		}
		float num = (h.IsLeft ? Model.UpperLenL : Model.UpperLenR);
		float num2 = (h.IsLeft ? Model.ForeLenL : Model.ForeLenR);
		if(ImportedRig) {
			if(_reachModel!=Model) {
				_reachModel=Model;_reachRestL=Model.UpperArmL.localPosition;_reachRestR=Model.UpperArmR.localPosition;
			}
			// Shoulder protraction adds the final reach without making forearms
			// permanently longer. Always start from rest: no accumulating drift.
			transform.localPosition=h.IsLeft?_reachRestL:_reachRestR;
			Vector3 reach=handPos-transform.position;
			float extra=Mathf.Clamp(reach.magnitude-(num+num2-.015f),0f,.12f);
			if(reach.sqrMagnitude>.000001f)transform.position+=reach.normalized*extra;
		}
		if (!(num < 0.0001f) && !(num2 < 0.0001f))
		{
			Vector3 position = transform.position;
			Vector3 vector = handPos - position;
			float num3 = num + num2;
			float num4 = Mathf.Clamp(vector.magnitude, 0.02f, num3 - 0.005f);
			Vector3 vector2 = ((vector.sqrMagnitude > 1E-06f) ? vector.normalized : Vector3.forward);
			float num5 = (num * num - num2 * num2 + num4 * num4) / (2f * num4);
			float num6 = Mathf.Sqrt(Mathf.Max(0f, num * num - num5 * num5));
			Vector3 vector3 = pole - vector2 * Vector3.Dot(pole, vector2);
			vector3 = ((vector3.sqrMagnitude > 1E-06f) ? vector3.normalized : Vector3.up);
			Vector3 vector4 = position + vector2 * num5 + vector3 * num6;
			Vector3 vector5 = position + vector2 * num4;
			transform.rotation = AimY(vector4 - position, vector3);
			Quaternion wristRotation = handRot * Quaternion.Euler(HandEuler);
			if (ImportedRig)
			{
				// Keep palm flexion within a plausible range. Controller aiming stays
				// independent: this limits only the rendered skeleton, not clicks.
				Vector3 foreAxis = (vector5 - transform2.position).normalized;
				Quaternion straight = Quaternion.FromToRotation(wristRotation * Vector3.up, foreAxis) * wristRotation;
				wristRotation = Quaternion.RotateTowards(straight, wristRotation, 45f);
				Quaternion neutralFore = AimY(foreAxis, vector3);
				Quaternion requestedFore = AimY(foreAxis, wristRotation * Vector3.forward);
				Quaternion limitedFore = Quaternion.RotateTowards(neutralFore, requestedFore, 75f);
				wristRotation = limitedFore * Quaternion.Inverse(requestedFore) * wristRotation;
			}
			// Match the forearm's roll to the wrist's actual Z axis. The imported
			// rig's Z faces down; controller-up previously introduced a half-turn
			// across the wrist and collapsed the blended skin into a corkscrew.
			transform2.rotation = AimY(vector5 - transform2.position,
				ImportedRig ? wristRotation * Vector3.forward : handRot * Vector3.up);
			transform3.rotation = wristRotation;
			if (h.HandNode != null)
			{
				h.HandNode.transform.SetPositionAndRotation(vector5, handRot);
			}
		}
	}

	private static void SetPos(GameObject go, Vector3 p)
	{
		if (go != null)
		{
			go.transform.position = p;
		}
	}

	private static void Segment(GameObject go, Vector3 a, Vector3 b, float radius, Vector3 rollRef)
	{
		if (!(go == null))
		{
			Vector3 vector = b - a;
			float magnitude = vector.magnitude;
			if (!(magnitude < 0.0001f))
			{
				Vector3 vector2 = vector / magnitude;
				Vector3 vector3 = rollRef - vector2 * Vector3.Dot(rollRef, vector2);
				Quaternion rotation = ((vector3.sqrMagnitude > 1E-06f) ? Quaternion.LookRotation(vector3.normalized, vector2) : Quaternion.FromToRotation(Vector3.up, vector2));
				go.transform.position = (a + b) * 0.5f;
				go.transform.rotation = rotation;
				go.transform.localScale = new Vector3(radius * 2f, magnitude * 0.5f, radius * 2f);
			}
		}
	}

	private static GameObject Joint(Transform parent, Color c, float radius, string name, PrimitiveType t = PrimitiveType.Sphere)
	{
		GameObject gameObject = Make(t, c);
		gameObject.name = name;
		if (parent != null)
		{
			gameObject.transform.SetParent(parent, worldPositionStays: false);
		}
		gameObject.transform.localScale = new Vector3(radius * 2f, radius * 2f, radius * 2f);
		return gameObject;
	}

	private static GameObject Local(PrimitiveType t, Transform parent, Color c, Vector3 pos, Quaternion rot, Vector3 scale, string name)
	{
		GameObject gameObject = Make(t, c);
		gameObject.name = name;
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		gameObject.transform.localPosition = pos;
		gameObject.transform.localRotation = rot;
		gameObject.transform.localScale = scale;
		return gameObject;
	}

	private static GameObject Make(PrimitiveType t, Color c)
	{
		GameObject gameObject = GameObject.CreatePrimitive(t);
		Collider component = gameObject.GetComponent<Collider>();
		if (component != null)
		{
			UnityEngine.Object.DestroyImmediate(component);
		}
		Renderer component2 = gameObject.GetComponent<Renderer>();
		if (component2 != null && component2.material != null)
		{
			component2.material.color = c;
			try
			{
				component2.material.SetFloat("_Glossiness", 0f);
				component2.material.SetFloat("_Smoothness", 0f);
				component2.material.SetFloat("_Metallic", 0f);
			}
			catch
			{
			}
			component2.shadowCastingMode = ShadowCastingMode.Off;
			component2.receiveShadows = false;
		}
		return gameObject;
	}

	public static void SetVisible(Hand h, bool on)
	{
		if (h != null)
		{
			if (h.Root != null && h.Root.activeSelf != on)
			{
				h.Root.SetActive(on);
			}
			if (h.RayObj != null && h.RayObj.activeSelf != on)
			{
				h.RayObj.SetActive(on);
			}
			if (!on && h.HitDot != null && h.HitDot.activeSelf)
			{
				h.HitDot.SetActive(value: false);
			}
		}
	}

	public static void UpdateRay(Hand h, Vector3 origin, Vector3 dir, float length, bool show)
	{
		if (h == null || h.RayObj == null)
		{
			return;
		}
		show = show && ShowBeam;
		if (h.RayObj.activeSelf != show)
		{
			h.RayObj.SetActive(show);
		}
		if (show && !(dir.sqrMagnitude < 1E-06f))
		{
			Transform transform = h.RayObj.transform;
			if (transform.parent != null)
			{
				transform.SetParent(null, worldPositionStays: true);
				UnityEngine.Object.DontDestroyOnLoad(h.RayObj);
			}
			dir = dir.normalized;
			float num = Mathf.Clamp(length, 0.05f, 25f);
			transform.position = origin + dir * (num * 0.5f);
			transform.rotation = Quaternion.FromToRotation(Vector3.up, dir);
			transform.localScale = new Vector3(0.004f, num * 0.5f, 0.004f);
		}
	}

	public static void SetGrabHint(Hand h, bool grabbable, Vector3 point, bool haveHit)
	{
		if (h == null)
		{
			return;
		}
		h.OverGrabbable = grabbable;
		haveHit = false; // Hide decorative hit cubes without disabling interaction.
		Color color = (grabbable ? GrabCol : DotCol);
		if (h.RayObj != null && h.RayObj.activeSelf)
		{
			Renderer component = h.RayObj.GetComponent<Renderer>();
			if (component != null && component.material != null)
			{
				component.material.color = (grabbable ? GrabCol : RayCol);
			}
		}
		if (!(h.HitDot != null))
		{
			return;
		}
		if (h.HitDot.activeSelf != haveHit)
		{
			h.HitDot.SetActive(haveHit);
		}
		if (haveHit)
		{
			Transform transform = h.HitDot.transform;
			transform.position = point;
			transform.rotation = GemTilt;
			float num = (grabbable ? 0.03f : 0.013f);
			transform.localScale = new Vector3(num, num, num);
			Renderer component2 = h.HitDot.GetComponent<Renderer>();
			if (component2 != null && component2.material != null)
			{
				component2.material.color = color;
			}
		}
	}
}
public static class Hover
{
	private static GameObject _left;

	private static GameObject _right;

	private static int _log;

	private static readonly HashSet<string> _seen = new HashSet<string>();

	private static int _forcedId = 0;

	private static float _forcedUntil;

	public static void Set(bool isLeft, Collider col)
	{
		GameObject gameObject = null;
		try
		{
			if (col != null)
			{
				gameObject = col.gameObject;
			}
		}
		catch
		{
		}
		if (isLeft)
		{
			_left = gameObject;
		}
		else
		{
			_right = gameObject;
		}
	}

	public static bool IsAimed(GameObject watched)
	{
		if (watched == null)
		{
			return false;
		}
		if (!Related(watched, _right))
		{
			return Related(watched, _left);
		}
		return true;
	}

	private static bool Related(GameObject watched, GameObject aimed)
	{
		if (!Under(watched, aimed))
		{
			return Under(aimed, watched);
		}
		return true;
	}

	private static bool Under(GameObject watched, GameObject aimed)
	{
		if (aimed == null || watched == null)
		{
			return false;
		}
		try
		{
			Transform transform = aimed.transform;
			int num = 0;
			while (transform != null && num < 6)
			{
				if (transform.gameObject.GetInstanceID() == watched.GetInstanceID())
				{
					return true;
				}
				num++;
				transform = transform.parent;
			}
		}
		catch
		{
		}
		return false;
	}

	public static void Note(string what)
	{
		if (_log++ < 60)
		{
			MelonLogger.Msg("[hover] " + what);
		}
	}

	public static void NoteOwner(string owner, string aimedAt, bool matched)
	{
		if (_seen.Count <= 40 && _seen.Add(owner))
		{
			MelonLogger.Msg($"[hover] MousePickEvent owner='{owner}' beam on='{aimedAt}' matched={matched}");
		}
	}

	public static void Force(PlayMakerFSM fsm, float seconds)
	{
		if (!(fsm == null))
		{
			try
			{
				_forcedId = fsm.gameObject.GetInstanceID();
			}
			catch
			{
				return;
			}
			_forcedUntil = Time.unscaledTime + seconds;
		}
	}

	public static bool IsForced(GameObject go)
	{
		if (_forcedId == 0 || go == null)
		{
			return false;
		}
		if (Time.unscaledTime > _forcedUntil)
		{
			_forcedId = 0;
			return false;
		}
		try
		{
			return go.GetInstanceID() == _forcedId;
		}
		catch
		{
			return false;
		}
	}
}
[HarmonyPatch(typeof(MousePickEvent), "OnUpdate")]
internal static class P_MousePickEventUpdate
{
	private static float _memoTime = -1f;

	private static float _memoDist;

	private static int _memoMask;

	private static RaycastHit _memoHit;

	private static float _kh;

	private static void NoteKeyhole(MousePickEvent m)
	{
		try
		{
			GameObject gameObject = m.Fsm.GameObject;
			if (gameObject == null || gameObject.name != "Keyhole" || Time.unscaledTime < _kh)
			{
				return;
			}
			_kh = Time.unscaledTime + 1f;
			string value = "-";
			try
			{
				value = ((ActionHelpers.mousePickInfo.collider != null) ? ActionHelpers.mousePickInfo.collider.name : "nothing");
			}
			catch
			{
			}
			string value2 = "-";
			try
			{
				GameObject ownerDefaultTarget = m.Fsm.GetOwnerDefaultTarget(m.GameObject);
				if (ownerDefaultTarget != null)
				{
					value2 = ownerDefaultTarget.name;
				}
			}
			catch
			{
			}
			MelonLogger.Msg($"[keyhole] running, state='{m.Fsm.ActiveStateName}' | watches='{value2}' | cache stamp={ActionHelpers.mousePickRaycastTime:F3} time={Time.time:F3} frame={Time.frameCount} | cache holds '{value}' dist={ActionHelpers.mousePickDistanceUsed:F1} mask=0x{ActionHelpers.mousePickLayerMaskUsed:X}");
		}
		catch
		{
		}
	}

	private static bool Prefix(MousePickEvent __instance)
	{
		if(VRPlugin.OwnsTrunkHover(__instance.Fsm?.GameObject))return false;
		if(VRPlugin.OwnsDigHover(__instance.Fsm?.GameObject))return false;
		if(VRPlugin.ActivePumpTap!=null&&__instance.Fsm?.GameObject==VRPlugin.ActivePumpTap.gameObject)return false;
		if (!Beam.Enabled || !Beam.Valid)
		{
			return true;
		}
		try
		{
			float num = 0f;
			try
			{
				num = __instance.rayDistance.Value;
			}
			catch
			{
			}
			if (num <= 0f)
			{
				num = float.PositiveInfinity;
			}
			int num2;
			try
			{
				num2 = ActionHelpers.LayerArrayToLayerMask(__instance.layerMask, __instance.invertMask.Value);
			}
			catch
			{
				return true;
			}
			if (_memoTime != Time.time || _memoDist != num || _memoMask != num2)
			{
				Physics.Raycast(Beam.Origin, Beam.Direction, out var hitInfo, num, num2, QueryTriggerInteraction.Collide);
				_memoHit = hitInfo;
				_memoTime = Time.time;
				_memoDist = num;
				_memoMask = num2;
				if (hitInfo.collider != null)
				{
					Hover.Note("pointer is on '" + hitInfo.collider.name + "'");
				}
			}
			ActionHelpers.mousePickInfo = _memoHit;
			ActionHelpers.mousePickDistanceUsed = num;
			ActionHelpers.mousePickLayerMaskUsed = num2;
			ActionHelpers.mousePickRaycastTime = Time.time;
			GameObject gameObject = null;
			GameObject gameObject2 = null;
			try
			{
				gameObject = __instance.Fsm.GameObject;
			}
			catch
			{
			}
			try
			{
				gameObject2 = __instance.Fsm.GetOwnerDefaultTarget(__instance.GameObject);
			}
			catch
			{
			}
			if (gameObject2 == null)
			{
				gameObject2 = gameObject;
			}
			if (gameObject2 != null && (Hover.IsForced(gameObject) || Hover.IsForced(gameObject2) || Hover.IsAimed(gameObject2) || Hover.IsAimed(gameObject)))
			{
				FsmEvent mouseOver = __instance.mouseOver;
				if (mouseOver != null)
				{
					__instance.Fsm.Event(mouseOver);
					Hover.Note("holding hover open on '" + gameObject2.name + "'");
				}
				return false;
			}
		}
		catch
		{
		}
		return true;
	}
}
public static class HudStrip
{
	private class Piece
	{
		public GameObject Go;

		public Mesh Mesh;

		public float U0;

		public float U1;

		public float Side;
	}

	public static float Distance = 1.3f;

	public static float Spread = 1.05f;

	public static float Scale = 2.4f;

	public static float Rise = 0.3f;

	public static float StripTop = 0.093f;

	public static bool FullScreen = true;

	public static float FullWidth = 1.8f;

	public static float FullDistance = 1.6f;

	public static float FullDrop = 0.1f;

	private const float LeftU0 = 0f;

	private const float LeftU1 = 0.13f;

	private const float RightU0 = 0.48f;

	private const float RightU1 = 1f;

	private static readonly Piece[] _corners = new Piece[3]
	{
		new Piece
		{
			U0 = 0f,
			U1 = 0.13f,
			Side = -1f
		},
		new Piece
		{
			U0 = 0.48f,
			U1 = 1f,
			Side = 1f
		},
		new Piece
		{
			U0 = 0.15f,
			U1 = 0.85f,
			Side = 0f
		}
	};

	private static readonly Piece[] _whole = new Piece[1]
	{
		new Piece
		{
			U0 = 0f,
			U1 = 1f,
			Side = 0f
		}
	};

	private static Piece[] _piece;

	private static bool _built;

	private static float _shapedFor = -1f;

	public static bool Exists => _built;

	public static void Build(RenderTexture uiRT)
	{
		if (_built || uiRT == null)
		{
			return;
		}
		_built = true;
		_piece = (FullScreen ? _whole : _corners);
		Shader shader = Shader.Find("Unlit/Transparent") ?? Shader.Find("Sprites/Default") ?? Shader.Find("UI/Default") ?? Shader.Find("Unlit/Texture");
		Piece[] piece = _piece;
		foreach (Piece piece2 in piece)
		{
			piece2.Go = new GameObject("BPVR_Hud" + ((piece2.Side < 0f) ? "Left" : "Right"));
			UnityEngine.Object.DontDestroyOnLoad(piece2.Go);
			piece2.Mesh = new Mesh
			{
				name = piece2.Go.name + "Mesh"
			};
			piece2.Go.AddComponent<MeshFilter>().mesh = piece2.Mesh;
			MeshRenderer meshRenderer = piece2.Go.AddComponent<MeshRenderer>();
			if (shader != null)
			{
				meshRenderer.material = new Material(shader);
			}
			meshRenderer.material.mainTexture = uiRT;
			meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
			meshRenderer.receiveShadows = false;
		}
		Shape();
		MelonLogger.Msg("[hudstrip] built in two pieces, shader '" + ((shader != null) ? shader.name : "none") + "'");
	}

	private static void Shape()
	{
		float num = Mathf.Clamp(StripTop, 0.02f, 0.5f);
		_shapedFor = num * 1000f + Scale;
		Piece[] piece = _piece;
		foreach (Piece piece2 in piece)
		{
			float num2 = (FullScreen ? FullWidth : (Spread * (piece2.U1 - piece2.U0) * Scale));
			float num3 = (FullScreen ? (num2 * 0.5625f) : (Spread * num * Scale * 0.5625f));
			bool prompt = !FullScreen && piece2.Side == 0f;
			if(prompt){num2=FullWidth*.7f;num3=FullWidth*.85f*.5625f;}
			float num4 = num2 * 0.5f;
			float num5 = num3 * 0.5f;
			piece2.Mesh.Clear();
			piece2.Mesh.vertices = new Vector3[4]
			{
				new Vector3(0f - num4, 0f - num5, 0f),
				new Vector3(num4, 0f - num5, 0f),
				new Vector3(0f - num4, num5, 0f),
				new Vector3(num4, num5, 0f)
			};
			float y = (FullScreen ? 0f : (1f - num));
			if(prompt)y=0f;
			float top=prompt?.85f:1f;
			piece2.Mesh.uv = new Vector2[4]
			{
				new Vector2(piece2.U0, y),
				new Vector2(piece2.U1, y),
				new Vector2(piece2.U0, top),
				new Vector2(piece2.U1, top)
			};
			piece2.Mesh.triangles = new int[12]
			{
				0, 1, 2, 2, 1, 3, 2, 1, 0, 3,
				1, 2
			};
			piece2.Mesh.normals = new Vector3[4]
			{
				-Vector3.forward,
				-Vector3.forward,
				-Vector3.forward,
				-Vector3.forward
			};
			piece2.Mesh.RecalculateBounds();
		}
	}

	public static void Place(Vector3 eye, Quaternion look, bool show)
	{
		if (!_built)
		{
			return;
		}
		Piece[] piece = _piece;
		foreach (Piece piece2 in piece)
		{
			if (piece2.Go.activeSelf != show)
			{
				piece2.Go.SetActive(show);
			}
		}
		if (show)
		{
			if (!Mathf.Approximately(_shapedFor, StripTop * 1000f + Scale))
			{
				Shape();
			}
			Vector3 vector = look * Vector3.forward;
			Vector3 vector2 = look * Vector3.up;
			Vector3 vector3 = look * Vector3.right;
			Vector3 vector4 = eye + vector * (FullScreen ? FullDistance : Distance) + vector2 * (FullScreen ? (0f - FullDrop) : Rise);
			piece = _piece;
			foreach (Piece piece3 in piece)
			{
				float num = (FullScreen ? 0f : (Spread * (piece3.U1 - piece3.U0) * Scale));
				float num2 = piece3.Side * (Spread * 0.5f - num * 0.5f);
				piece3.Go.transform.position = vector4 + vector3 * num2;
				if(!FullScreen&&piece3.Side==0f)
					piece3.Go.transform.position=eye+vector*FullDistance-vector2*(FullDrop+FullWidth*.075f*.5625f);
				piece3.Go.transform.rotation = Quaternion.LookRotation(piece3.Go.transform.position - eye, vector2);
			}
		}
	}
}
public static class Intro
{
	public static bool Enabled = true;

	public static float Speed = 6f;

	private static float _until;

	private static float _next;

	private static bool _fast;

	private static bool _reported;

	private static int _warned;

	private static readonly string[] Words = new string[10] { "intro", "logo", "splash", "credit", "title", "opening", "cinematic", "cutscene", "prologue", "openingcredits" };

	private static bool Says(string n)
	{
		if (string.IsNullOrEmpty(n))
		{
			return false;
		}
		string[] words = Words;
		foreach (string value in words)
		{
			if (n.IndexOf(value, System.StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		return false;
	}

	public static void SceneLoaded(string sceneName)
	{
		Restore();
		_reported = false;
		_next = 0f;
		_until = ((!string.IsNullOrEmpty(sceneName) && sceneName.IndexOf("Holly", System.StringComparison.OrdinalIgnoreCase) >= 0) ? 0f : (Time.unscaledTime + 30f));
	}

	private static void Restore()
	{
		if (!_fast)
		{
			return;
		}
		_fast = false;
		try
		{
			Time.timeScale = 1f;
		}
		catch
		{
		}
	}

	public static void Tick()
	{
		if (!Enabled)
		{
			Restore();
		}
		else if (Time.unscaledTime > _until)
		{
			Restore();
		}
		else
		{
			if (Time.unscaledTime < _next)
			{
				return;
			}
			_next = Time.unscaledTime + 0.5f;
			try
			{
				Il2CppArrayBase<PlayMakerFSM> il2CppArrayBase = UnityEngine.Object.FindObjectsOfType<PlayMakerFSM>();
				if (il2CppArrayBase == null)
				{
					Restore();
					return;
				}
				bool flag = false;
				StringBuilder stringBuilder = new StringBuilder();
				int num = 0;
				for (int i = 0; i < il2CppArrayBase.Length; i++)
				{
					PlayMakerFSM playMakerFSM = il2CppArrayBase[i];
					if (playMakerFSM == null)
					{
						continue;
					}
					string name;
					string fsmName;
					try
					{
						name = playMakerFSM.gameObject.name;
						fsmName = playMakerFSM.FsmName;
					}
					catch
					{
						continue;
					}
					if (!Says(name) && !Says(fsmName))
					{
						continue;
					}
					flag = true;
					if (!_reported && num < 10)
					{
						num++;
						string value = "";
						try
						{
							value = playMakerFSM.Fsm.ActiveStateName;
						}
						catch
						{
						}
						StringBuilder stringBuilder2 = stringBuilder;
						StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(10, 3, stringBuilder2);
						handler.AppendLiteral("'");
						handler.AppendFormatted(name);
						handler.AppendLiteral("'/'");
						handler.AppendFormatted(fsmName);
						handler.AppendLiteral("'@'");
						handler.AppendFormatted(value);
						handler.AppendLiteral("'  ");
						stringBuilder2.Append(ref handler);
					}
					Nudge(playMakerFSM);
				}
				if (!_reported && num > 0)
				{
					_reported = true;
					MelonLogger.Msg("[intro] opening sequence: " + stringBuilder);
				}
				if (flag && !_fast)
				{
					_fast = true;
					try
					{
						Time.timeScale = Speed;
					}
					catch
					{
					}
					MelonLogger.Msg($"[intro] running the opening at {Speed:F0}x");
				}
				else if (!flag && _fast)
				{
					Restore();
					MelonLogger.Msg("[intro] opening done, speed back to normal");
				}
			}
			catch (System.Exception ex)
			{
				Restore();
				if (_warned++ < 3)
				{
					MelonLogger.Warning("[intro] " + ex.Message);
				}
			}
		}
	}

	private static void Nudge(PlayMakerFSM fsm)
	{
		try
		{
			FsmState activeState = fsm.Fsm.ActiveState;
			if (activeState == null)
			{
				return;
			}
			Il2CppReferenceArray<FsmTransition> transitions = activeState.Transitions;
			int num = 0;
			while (transitions != null && num < transitions.Length)
			{
				string text = ((transitions[num] != null) ? transitions[num].EventName : null);
				if (!string.IsNullOrEmpty(text) && text.IndexOf("skip", System.StringComparison.OrdinalIgnoreCase) >= 0)
				{
					fsm.SendEvent(text);
				}
				num++;
			}
		}
		catch
		{
		}
	}
}
public class PickEvents
{
	public PlayMakerFSM Fsm;

	public string Down;

	public string Up;

	public string Over;

	public string Off;

	public float Reach;

	public bool Any
	{
		get
		{
			if (Down == null && Up == null)
			{
				return Over != null;
			}
			return true;
		}
	}

	public bool CanClick
	{
		get
		{
			if (Down == null)
			{
				return Up != null;
			}
			return true;
		}
	}
}
public static class MousePick
{
	private static int _log;

	private static readonly PickEvents[] _hover = new PickEvents[2];

	private static string Name(FsmEvent e)
	{
		try
		{
			return (e != null && !string.IsNullOrEmpty(e.Name)) ? e.Name : null;
		}
		catch
		{
			return null;
		}
	}

	private static Il2CppReferenceArray<FsmStateAction> TryActions(FsmState st)
	{
		try
		{
			return st.Actions;
		}
		catch
		{
			return null;
		}
	}

	private static void ReadFrom(FsmState st, PickEvents into)
	{
		if (st == null || into.CanClick)
		{
			return;
		}
		Il2CppReferenceArray<FsmStateAction> il2CppReferenceArray = TryActions(st);
		if (il2CppReferenceArray == null)
		{
			return;
		}
		for (int i = 0; i < il2CppReferenceArray.Length; i++)
		{
			FsmStateAction fsmStateAction = il2CppReferenceArray[i];
			if (fsmStateAction == null)
			{
				continue;
			}
			MousePickEvent mousePickEvent;
			try
			{
				mousePickEvent = fsmStateAction.TryCast<MousePickEvent>();
			}
			catch
			{
				continue;
			}
			if (mousePickEvent == null)
			{
				continue;
			}
			try
			{
				if (into.Down == null)
				{
					into.Down = Name(mousePickEvent.mouseDown);
				}
				if (into.Up == null)
				{
					into.Up = Name(mousePickEvent.mouseUp);
				}
				if (into.Over == null)
				{
					into.Over = Name(mousePickEvent.mouseOver);
				}
				if (into.Off == null)
				{
					into.Off = Name(mousePickEvent.mouseOff);
				}
				if (into.Reach <= 0f && mousePickEvent.rayDistance != null)
				{
					into.Reach = mousePickEvent.rayDistance.Value;
				}
			}
			catch
			{
				continue;
			}
			if (into.CanClick)
			{
				break;
			}
		}
	}

	public static PickEvents For(Collider col)
	{
		if(col==null)return null;
		if(_pickFrame!=Time.frameCount){_pickFrame=Time.frameCount;_framePicks.Clear();}
		int id=col.GetInstanceID();
		if(_framePicks.TryGetValue(id,out var cached))return cached;
		var result=ReadFor(col);
		_framePicks[id]=result;
		return result;
	}
	private static int _pickFrame=-1;
	private static readonly System.Collections.Generic.Dictionary<int,PickEvents> _framePicks=new();
	private static PickEvents ReadFor(Collider col)
	{
		// Trunks can carry several FSMs: the first component is not necessarily
		// the lock's mouse-input FSM. Inspect all components inside this trunk only.
		Transform trunk=null;
		for(var t=col==null?null:col.transform;t!=null;t=t.parent) {
			if(t.name.StartsWith("TRUNK(Clone)",System.StringComparison.OrdinalIgnoreCase)){trunk=t;break;}
			if(t.name=="Pickup Spawn Pools"||t.name=="Room Spawn Pools")break;
		}
		if(trunk!=null) {
			PickEvents fallback=null;
			var inputRoot=col.transform==trunk?trunk.Find("Trunk Code"):col.transform;
			for(var t=inputRoot!=null?inputRoot:col.transform;t!=null;t=t.parent) {
				foreach(var f in t.GetComponents<PlayMakerFSM>()) {
					if(f==null||!f.isActiveAndEnabled)continue;
					var candidate=new PickEvents{Fsm=f,Reach=0f};
					ReadFrom(f.Fsm.ActiveState,candidate);
					if(candidate.CanClick)return candidate;
					foreach(var state in f.Fsm.States)ReadFrom(state,candidate);
					if(candidate.CanClick&&fallback==null)fallback=candidate;
				}
				if(t==trunk)break;
			}
			if(fallback!=null)return fallback;
		}
		if (col == null)
		{
			return null;
		}
		PickEvents pickEvents = null;
		Transform transform = col.transform;
		for (int i = 0; transform != null && i < 6; i++, transform = transform.parent)
		{
			PlayMakerFSM component;
			try
			{
				component = transform.gameObject.GetComponent<PlayMakerFSM>();
			}
			catch
			{
				continue;
			}
			if (component == null)
			{
				continue;
			}
			PickEvents pickEvents2 = new PickEvents
			{
				Fsm = component,
				Reach = 0f
			};
			try
			{
				Fsm fsm = component.Fsm;
				ReadFrom(fsm.ActiveState, pickEvents2);
				if (!pickEvents2.CanClick)
				{
					Il2CppReferenceArray<FsmState> states = fsm.States;
					int num = 0;
					while (states != null && num < states.Length && !pickEvents2.CanClick)
					{
						ReadFrom(states[num], pickEvents2);
						num++;
					}
				}
			}
			catch (System.Exception ex)
			{
				if (_log++ < 6)
				{
					MelonLogger.Warning("[pick] reading actions: " + ex.Message);
				}
			}
			if (pickEvents2.CanClick)
			{
				if (_log < 60)
				{
					_log++;
					MelonLogger.Msg($"[pick] '{transform.name}' fsm '{component.FsmName}' down='{pickEvents2.Down}' up='{pickEvents2.Up}' over='{pickEvents2.Over}' reach={pickEvents2.Reach:F2}");
				}
				return pickEvents2;
			}
			if (pickEvents2.Any && pickEvents == null)
			{
				pickEvents = pickEvents2;
			}
		}
		if (pickEvents != null && _log < 60)
		{
			_log++;
			MelonLogger.Msg($"[pick] hover-only on '{col.name}' (over='{pickEvents.Over}') - nothing clickable up the chain");
		}
		return pickEvents;
	}

	public static void Send(PlayMakerFSM fsm, string ev)
	{
		// A sent event can change eligibility within the same frame.
		_framePicks.Clear();
		if (fsm == null || string.IsNullOrEmpty(ev))
		{
			return;
		}
		try
		{
			Fsm fsm2 = fsm.Fsm;
			Il2CppReferenceArray<FsmTransition> il2CppReferenceArray = fsm2.ActiveState?.Transitions;
			for (int i = 0; il2CppReferenceArray != null && i < il2CppReferenceArray.Length; i++)
			{
				FsmTransition fsmTransition = il2CppReferenceArray[i];
				if (fsmTransition == null)
				{
					continue;
				}
				string eventName;
				try
				{
					eventName = fsmTransition.EventName;
				}
				catch
				{
					continue;
				}
				if (string.Equals(eventName, ev, System.StringComparison.Ordinal))
				{
					FsmEvent fsmEvent = fsmTransition.FsmEvent;
					if (fsmEvent != null)
					{
						fsm2.Event(fsmEvent);
						return;
					}
				}
			}
		}
		catch
		{
		}
		try
		{
			Fsm fsm3 = fsm.Fsm;
			Il2CppReferenceArray<FsmEvent> events = fsm3.Events;
			for (int j = 0; events != null && j < events.Length; j++)
			{
				FsmEvent fsmEvent2 = events[j];
				if (fsmEvent2 != null)
				{
					string name;
					try
					{
						name = fsmEvent2.Name;
					}
					catch
					{
						continue;
					}
					if (string.Equals(name, ev, System.StringComparison.Ordinal))
					{
						fsm3.Event(fsmEvent2);
						return;
					}
				}
			}
		}
		catch
		{
		}
		try
		{
			fsm.SendEvent(ev);
		}
		catch
		{
		}
	}

	public static void Hover(bool isLeft, PickEvents now)
	{
		int num = ((!isLeft) ? 1 : 0);
		PickEvents pickEvents = _hover[num];
		if (pickEvents == null || now == null || !(pickEvents.Fsm != null) || !(now.Fsm != null) || pickEvents.Fsm.Pointer != now.Fsm.Pointer)
		{
			if (pickEvents != null)
			{
				var other=_hover[1-num];
				if(other?.Fsm==null||pickEvents.Fsm==null||other.Fsm.Pointer!=pickEvents.Fsm.Pointer)
					Send(pickEvents.Fsm, pickEvents.Off);
			}
			_hover[num] = now;
			if (now != null)
			{
				Send(now.Fsm, now.Over);
			}
		}
	}

	public static void ClearHover(bool isLeft)
	{
		Hover(isLeft, null);
	}
}
public enum EVREye
{
	Left,
	Right
}
public enum ETextureType
{
	DirectX,
	OpenGL,
	Vulkan
}
public enum EColorSpace
{
	Auto,
	Gamma,
	Linear
}
public enum ETrackingUniverseOrigin
{
	Seated,
	Standing,
	RawAndUncalibrated
}
public enum EVRApplicationType
{
	Other,
	Scene,
	Overlay,
	Background
}
public struct HmdMatrix34_t
{
	public float m0;

	public float m1;

	public float m2;

	public float m3;

	public float m4;

	public float m5;

	public float m6;

	public float m7;

	public float m8;

	public float m9;

	public float m10;

	public float m11;
}
public struct HmdMatrix44_t
{
	public float m0;

	public float m1;

	public float m2;

	public float m3;

	public float m4;

	public float m5;

	public float m6;

	public float m7;

	public float m8;

	public float m9;

	public float m10;

	public float m11;

	public float m12;

	public float m13;

	public float m14;

	public float m15;
}
public struct HmdVector3_t
{
	public float v0;

	public float v1;

	public float v2;
}
public struct TrackedDevicePose_t
{
	public HmdMatrix34_t mDeviceToAbsoluteTracking;

	public HmdVector3_t vVelocity;

	public HmdVector3_t vAngularVelocity;

	public int eTrackingResult;

	[MarshalAs(UnmanagedType.I1)]
	public bool bPoseIsValid;

	[MarshalAs(UnmanagedType.I1)]
	public bool bDeviceIsConnected;
}
public struct Texture_t
{
	public System.IntPtr handle;

	public int eType;

	public int eColorSpace;
}
public struct VRTextureBounds_t
{
	public float uMin;

	public float vMin;

	public float uMax;

	public float vMax;
}
public enum ETrackedControllerRole
{
	Invalid,
	LeftHand,
	RightHand
}
public static class VRButton
{
	public const ulong ApplicationMenu = 2uL;

	public const ulong Grip = 4uL;

	public const ulong A = 128uL;

	public const ulong Touchpad = 4294967296uL;

	public const ulong Trigger = 8589934592uL;
}
public struct VRControllerAxis_t
{
	public float x;

	public float y;
}
public struct VRControllerState_t
{
	public uint unPacketNum;

	public ulong ulButtonPressed;

	public ulong ulButtonTouched;

	public VRControllerAxis_t rAxis0;

	public VRControllerAxis_t rAxis1;

	public VRControllerAxis_t rAxis2;

	public VRControllerAxis_t rAxis3;

	public VRControllerAxis_t rAxis4;
}
public struct IVRSystem_FnTable
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	public delegate void _GetRecommendedRenderTargetSize(ref uint w, ref uint h);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	public delegate HmdMatrix44_t _GetProjectionMatrix(EVREye eye, float nearZ, float farZ);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	public delegate void _GetProjectionRaw(EVREye eye, ref float l, ref float r, ref float t, ref float b);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	public delegate HmdMatrix34_t _GetEyeToHeadTransform(EVREye eye);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	public delegate uint _GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole role);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	[return: MarshalAs(UnmanagedType.I1)]
	public delegate bool _GetControllerState(uint deviceIndex, ref VRControllerState_t state, uint stateSize);

	public _GetRecommendedRenderTargetSize GetRecommendedRenderTargetSize;

	public _GetProjectionMatrix GetProjectionMatrix;

	public _GetProjectionRaw GetProjectionRaw;

	public System.IntPtr ComputeDistortion;

	public System.IntPtr ComputeDistortionSet;

	public _GetEyeToHeadTransform GetEyeToHeadTransform;

	public System.IntPtr GetTimeSinceLastVsync;

	public System.IntPtr GetD3D9AdapterIndex;

	public System.IntPtr GetDXGIOutputInfo;

	public System.IntPtr GetOutputDevice;

	public System.IntPtr IsDisplayOnDesktop;

	public System.IntPtr SetDisplayVisibility;

	public System.IntPtr GetDeviceToAbsoluteTrackingPose;

	public System.IntPtr GetSeatedZeroPoseToStandingAbsoluteTrackingPose;

	public System.IntPtr GetRawZeroPoseToStandingAbsoluteTrackingPose;

	public System.IntPtr GetSortedTrackedDeviceIndicesOfClass;

	public System.IntPtr GetTrackedDeviceActivityLevel;

	public System.IntPtr ApplyTransform;

	public _GetTrackedDeviceIndexForControllerRole GetTrackedDeviceIndexForControllerRole;

	public System.IntPtr GetControllerRoleForTrackedDeviceIndex;

	public System.IntPtr GetTrackedDeviceClass;

	public System.IntPtr IsTrackedDeviceConnected;

	public System.IntPtr GetBoolTrackedDeviceProperty;

	public System.IntPtr GetFloatTrackedDeviceProperty;

	public System.IntPtr GetInt32TrackedDeviceProperty;

	public System.IntPtr GetUint64TrackedDeviceProperty;

	public System.IntPtr GetMatrix34TrackedDeviceProperty;

	public System.IntPtr GetArrayTrackedDeviceProperty;

	public System.IntPtr GetStringTrackedDeviceProperty;

	public System.IntPtr GetPropErrorNameFromEnum;

	public System.IntPtr PollNextEvent;

	public System.IntPtr PollNextEventWithPose;

	public System.IntPtr PollNextEventWithPoseAndOverlays;

	public System.IntPtr GetEventTypeNameFromEnum;

	public System.IntPtr GetHiddenAreaMesh;

	public System.IntPtr GetEyeTrackedFoveationCenter;

	public System.IntPtr GetEyeTrackedFoveationCenterForProjection;

	public _GetControllerState GetControllerState;

	// Valve openvr_capi.h IVRSystem_026: preserve the intervening ABI slot.
	public System.IntPtr GetControllerStateWithPose;
	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	public delegate void _TriggerHapticPulse(uint deviceIndex,uint axisId,ushort durationMicroseconds);
	public _TriggerHapticPulse TriggerHapticPulse;
}
public struct IVRCompositor_FnTable
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	public delegate void _SetTrackingSpace(ETrackingUniverseOrigin origin);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	public delegate int _WaitGetPoses([In][Out] TrackedDevicePose_t[] renderPoses, uint renderCount, [In][Out] TrackedDevicePose_t[] gamePoses, uint gameCount);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	public delegate int _Submit(EVREye eye, ref Texture_t tex, ref VRTextureBounds_t bounds, int flags);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	public delegate void _PostPresentHandoff();

	public _SetTrackingSpace SetTrackingSpace;

	public System.IntPtr GetTrackingSpace;

	public _WaitGetPoses WaitGetPoses;

	public System.IntPtr GetLastPoses;

	public System.IntPtr GetLastPoseForTrackedDeviceIndex;

	public System.IntPtr GetSubmitTexture;

	public _Submit Submit;

	public System.IntPtr SubmitWithArrayIndex;

	public System.IntPtr ClearLastSubmittedFrame;

	public _PostPresentHandoff PostPresentHandoff;
}
public static class OpenVRNative
{
	public const string DLL = "openvr_api.dll";

	public const string IVRSystem_Version = "IVRSystem_026";

	public const string IVRCompositor_Version = "IVRCompositor_029";

	[DllImport("openvr_api.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern System.IntPtr VR_InitInternal(ref int err, EVRApplicationType type);

	[DllImport("openvr_api.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern void VR_ShutdownInternal();

	[DllImport("openvr_api.dll", CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.I1)]
	public static extern bool VR_IsHmdPresent();

	[DllImport("openvr_api.dll", CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.I1)]
	public static extern bool VR_IsRuntimeInstalled();

	[DllImport("openvr_api.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern System.IntPtr VR_GetGenericInterface([MarshalAs(UnmanagedType.LPStr)] string version, ref int err);

	public static T GetTable<T>(string version, out string error) where T : struct
	{
		int err = 0;
		System.IntPtr intPtr = VR_GetGenericInterface("FnTable:" + version, ref err);
		if (intPtr == System.IntPtr.Zero || err != 0)
		{
			error = $"{version} unavailable (err {err})";
			return default(T);
		}
		error = null;
		return Marshal.PtrToStructure<T>(intPtr);
	}
}
public static class SaveNow
{
	public const string Event = "SAVE DATA SAVE";

	private static float _held;

	private static bool _armed = true;

	private static int _log;

	public static void Tick(bool leftMenu, bool rightMenu, float dt)
	{
		if (leftMenu && rightMenu)
		{
			_held += dt;
			if (_held > 0.6f && _armed)
			{
				_armed = false;
				Fire();
			}
		}
		else
		{
			_held = 0f;
			_armed = true;
		}
	}

	public static void Fire()
	{
		try
		{
			PlayMakerFSM playMakerFSM = Find();
			if (playMakerFSM == null)
			{
				MelonLogger.Warning("[save] could not find SAVE SYSTEM");
				return;
			}
			playMakerFSM.SendEvent("SAVE DATA SAVE");
			MelonLogger.Msg($"[save] sent '{"SAVE DATA SAVE"}' to '{playMakerFSM.gameObject.name}' (fsm '{playMakerFSM.FsmName}')");
		}
		catch (System.Exception ex)
		{
			MelonLogger.Warning("[save] " + ex.Message);
		}
	}

	private static PlayMakerFSM Find()
	{
		try
		{
			Il2CppArrayBase<PlayMakerFSM> il2CppArrayBase = UnityEngine.Object.FindObjectsOfType<PlayMakerFSM>();
			for (int i = 0; il2CppArrayBase != null && i < il2CppArrayBase.Length; i++)
			{
				PlayMakerFSM playMakerFSM = il2CppArrayBase[i];
				if (!(playMakerFSM == null))
				{
					string name;
					try
					{
						name = playMakerFSM.gameObject.name;
					}
					catch
					{
						continue;
					}
					if (string.Equals(name, "SAVE SYSTEM", System.StringComparison.Ordinal))
					{
						return playMakerFSM;
					}
				}
			}
			if (_log++ < 3)
			{
				MelonLogger.Msg("[save] no object named 'SAVE SYSTEM' in this scene");
			}
		}
		catch
		{
		}
		return null;
	}
}
public static class Screens
{
	public static bool Enabled = true;

	public static float Delay = 1.6f;

	private static GameObject _waiting;

	private static float _since;

	private static float _nextScan;

	private static int _log;

	public static bool VideoUp;

	private static bool _wasUp;

	private static float _frozenSince = -1f;

	private static int _thawLog;

	private static string _seenState = "";

	private static bool _promptUp;

	private static string _thawEvent;

	private static readonly string[] Prompts = new string[4] { "CALL IT A DAY", "SAVE & QUIT", "ARE YOU SURE", "CONFIRM" };

	public static void NoteMenu(bool up)
	{
		if (up == _wasUp)
		{
			return;
		}
		_wasUp = up;
		if (!up)
		{
			MelonLogger.Msg("[screen] menu closed");
			return;
		}
		StringBuilder stringBuilder = new StringBuilder();
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(47, 1, stringBuilder2);
		handler.AppendLiteral("[screen] MENU OPENED cursor=");
		handler.AppendFormatted(Cursor.lockState);
		handler.AppendLiteral(" - active screens: ");
		stringBuilder3.Append(ref handler);
		try
		{
			Il2CppArrayBase<PlayMakerFSM> il2CppArrayBase = UnityEngine.Object.FindObjectsOfType<PlayMakerFSM>();
			int num = 0;
			for (int i = 0; il2CppArrayBase != null && i < il2CppArrayBase.Length && num < 12; i++)
			{
				PlayMakerFSM playMakerFSM = il2CppArrayBase[i];
				if (playMakerFSM == null)
				{
					continue;
				}
				GameObject gameObject;
				try
				{
					gameObject = playMakerFSM.gameObject;
				}
				catch
				{
					continue;
				}
				if (gameObject == null || !gameObject.activeInHierarchy)
				{
					continue;
				}
				Transform transform = gameObject.transform;
				bool flag = false;
				int num2 = 0;
				while (transform != null && num2 < 8)
				{
					if (transform.name.IndexOf("OVERLAY", System.StringComparison.OrdinalIgnoreCase) >= 0 || transform.name.IndexOf("UI ", System.StringComparison.OrdinalIgnoreCase) == 0)
					{
						flag = true;
						break;
					}
					num2++;
					transform = transform.parent;
				}
				if (flag)
				{
					string value = "?";
					try
					{
						value = playMakerFSM.Fsm.ActiveStateName;
					}
					catch
					{
					}
					stringBuilder2 = stringBuilder;
					StringBuilder stringBuilder4 = stringBuilder2;
					handler = new StringBuilder.AppendInterpolatedStringHandler(5, 2, stringBuilder2);
					handler.AppendLiteral("'");
					handler.AppendFormatted(gameObject.name);
					handler.AppendLiteral("'@");
					handler.AppendFormatted(value);
					handler.AppendLiteral("  ");
					stringBuilder4.Append(ref handler);
					num++;
				}
			}
		}
		catch (System.Exception ex)
		{
			stringBuilder.Append("scan failed: " + ex.Message);
		}
		MelonLogger.Msg(stringBuilder.ToString());
	}

	private static string ExitToUnfrozen(PlayMakerFSM fsm, string stateName)
	{
		try
		{
			Il2CppReferenceArray<FsmState> states = fsm.Fsm.States;
			if (states == null)
			{
				return null;
			}
			for (int i = 0; i < states.Length; i++)
			{
				FsmState fsmState = states[i];
				if (fsmState == null || fsmState.Name != stateName)
				{
					continue;
				}
				Il2CppReferenceArray<FsmTransition> transitions = fsmState.Transitions;
				for (int j = 0; transitions != null && j < transitions.Length; j++)
				{
					FsmTransition fsmTransition = transitions[j];
					if (fsmTransition == null)
					{
						continue;
					}
					string text;
					try
					{
						text = fsmTransition.ToState ?? "";
					}
					catch
					{
						continue;
					}
					if (text.IndexOf("UNFROZEN", System.StringComparison.OrdinalIgnoreCase) >= 0)
					{
						try
						{
							return fsmTransition.EventName;
						}
						catch
						{
							return null;
						}
					}
				}
				return null;
			}
		}
		catch
		{
		}
		return null;
	}

	private static void Thaw(PlayMakerFSM fps)
	{
		try
		{
			if (VideoUp || _waiting != null || _promptUp)
			{
				_frozenSince = -1f;
				return;
			}
			if (fps == null)
			{
				_frozenSince = -1f;
				return;
			}
			string text;
			try
			{
				text = fps.Fsm.ActiveStateName ?? "";
			}
			catch
			{
				return;
			}
			if (_thawLog < 3 && _seenState != text)
			{
				_seenState = text;
				MelonLogger.Msg("[screen] player controller state = '" + text + "'");
			}
			string text2 = ExitToUnfrozen(fps, text);
			if (string.IsNullOrEmpty(text2))
			{
				_frozenSince = -1f;
				return;
			}
			_thawEvent = text2;
			if (_frozenSince < 0f)
			{
				_frozenSince = Time.unscaledTime;
			}
			else if (!(Time.unscaledTime - _frozenSince < 1.2f))
			{
				_frozenSince = -1f;
				fps.SendEvent(text2);
				if (_thawLog++ < 8)
				{
					MelonLogger.Msg($"[screen] player was held in '{text}' - sent its own '{text2}'");
				}
			}
		}
		catch
		{
		}
	}

	public static void Tick()
	{
		if (!Enabled)
		{
			return;
		}
		try
		{
			VideoUp = false;
			Il2CppReferenceArray<Camera> allCameras = Camera.allCameras;
			int num = 0;
			while (allCameras != null && num < allCameras.Length)
			{
				Camera camera = allCameras[num];
				if (!(camera == null) && camera.isActiveAndEnabled && camera.name.IndexOf("VIDEO", System.StringComparison.OrdinalIgnoreCase) >= 0)
				{
					VideoUp = true;
					break;
				}
				num++;
			}
		}
		catch
		{
		}
		if (Time.unscaledTime < _nextScan)
		{
			return;
		}
		_nextScan = Time.unscaledTime + 0.25f;
		GameObject gameObject = null;
		PlayMakerFSM fps = null;
		try
		{
			gameObject = Sweep(out fps);
		}
		catch
		{
		}
		Thaw(fps);
		if (gameObject == null)
		{
			_waiting = null;
		}
		else if ((object)gameObject != _waiting)
		{
			_waiting = gameObject;
			_since = Time.unscaledTime;
			if (_log++ < 6)
			{
				MelonLogger.Msg("[screen] '" + gameObject.name + "' is up and holding you");
			}
		}
		else
		{
			if (Time.unscaledTime - _since < Delay)
			{
				return;
			}
			_since = Time.unscaledTime + 999f;
			try
			{
				PlayMakerFSM component = gameObject.GetComponent<PlayMakerFSM>();
				if (component != null)
				{
					MousePick.Send(component, "click");
					if (_log++ < 8)
					{
						MelonLogger.Msg("[screen] dismissed '" + gameObject.name + "'");
					}
				}
			}
			catch (System.Exception ex)
			{
				if (_log++ < 8)
				{
					MelonLogger.Warning("[screen] " + ex.Message);
				}
			}
		}
	}

	private static GameObject Sweep(out PlayMakerFSM fps)
	{
		fps = null;
		GameObject gameObject = null;
		_promptUp = false;
		Il2CppArrayBase<PlayMakerFSM> il2CppArrayBase = UnityEngine.Object.FindObjectsOfType<PlayMakerFSM>();
		if (il2CppArrayBase == null)
		{
			return null;
		}
		for (int i = 0; i < il2CppArrayBase.Length; i++)
		{
			PlayMakerFSM playMakerFSM = il2CppArrayBase[i];
			if (playMakerFSM == null)
			{
				continue;
			}
			GameObject gameObject2;
			try
			{
				gameObject2 = playMakerFSM.gameObject;
			}
			catch
			{
				continue;
			}
			if (!(gameObject2 == null))
			{
				string name = gameObject2.name;
				if (fps == null && name.IndexOf("FPSController", System.StringComparison.OrdinalIgnoreCase) >= 0)
				{
					fps = playMakerFSM;
				}
				if (gameObject == null && gameObject2.activeInHierarchy && (name.StartsWith("You Found", System.StringComparison.OrdinalIgnoreCase) || name.StartsWith("You Dug Up", System.StringComparison.OrdinalIgnoreCase)))
				{
					gameObject = gameObject2;
				}
				if (!_promptUp && gameObject2.activeInHierarchy && IsPrompt(name))
				{
					_promptUp = true;
				}
			}
		}
		return gameObject;
	}

	private static bool IsPrompt(string n)
	{
		for (int i = 0; i < Prompts.Length; i++)
		{
			if (n.IndexOf(Prompts[i], System.StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		return false;
	}
}
public static class Snapshot
{
	public static string Folder = ".";

	public static RenderTexture UiRT;

	public static RenderTexture EyeRT;

	public static RenderTexture EyeRT2;

	private static int _n;

	private static float _held;

	private static bool _armed = true;

	public static string TargetName = "-";

	public static float TargetDistance;

	public static string TargetEvents = "-";

	public static string TargetState = "-";

	public static bool TargetClickable;
	private static Collider _diagnosticTarget;

	public static void Note(Collider col, float dist, PickEvents pick)
	{
		_diagnosticTarget=col;
		TargetName = ((col != null) ? col.name : "-");
		TargetDistance = dist;
		if (pick != null)
		{
			TargetClickable = pick.CanClick;
			TargetEvents = $"down='{pick.Down}' up='{pick.Up}' over='{pick.Over}' reach={pick.Reach:F2}";
			try
			{
				TargetState = ((pick.Fsm != null) ? pick.Fsm.Fsm.ActiveStateName : "-");
				return;
			}
			catch
			{
				TargetState = "-";
				return;
			}
		}
		TargetClickable = false;
		TargetEvents = "no MousePickEvent";
		TargetState = "-";
	}

	public static void Tick(bool leftGrip, bool rightGrip, float dt)
	{
		if (leftGrip && rightGrip)
		{
			_held += dt;
			if (_held > 0.4f && _armed)
			{
				_armed = false;
				Take();
			}
		}
		else
		{
			_held = 0f;
			_armed = true;
		}
	}

	private static bool Save(RenderTexture rt, string path)
	{
		if (rt == null)
		{
			return false;
		}
		try
		{
			if (!rt.IsCreated())
			{
				return false;
			}
			RenderTexture active = RenderTexture.active;
			RenderTexture.active = rt;
			Texture2D texture2D = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, mipChain: false);
			texture2D.ReadPixels(new Rect(0f, 0f, rt.width, rt.height), 0, 0);
			texture2D.Apply(updateMipmaps: false);
			RenderTexture.active = active;
			File.WriteAllBytes(path, texture2D.EncodeToPNG());
			UnityEngine.Object.Destroy(texture2D);
			return true;
		}
		catch (System.Exception ex)
		{
			MelonLogger.Warning("[shot] " + Path.GetFileName(path) + ": " + ex.Message);
			return false;
		}
	}

	private static bool SaveFlat(string path)
	{
		try
		{
			Texture2D texture2D = ScreenCapture.CaptureScreenshotAsTexture();
			if (texture2D == null)
			{
				return false;
			}
			File.WriteAllBytes(path, texture2D.EncodeToPNG());
			UnityEngine.Object.Destroy(texture2D);
			return true;
		}
		catch (System.Exception ex)
		{
			MelonLogger.Warning("[shot] flat readback unavailable (" + ex.Message + ") - falling back to a queued capture");
			try
			{
				ScreenCapture.CaptureScreenshot(path);
				return true;
			}
			catch
			{
				return false;
			}
		}
	}

	private static void Resume()
	{
		if (_n > 0)
		{
			return;
		}
		try
		{
			string[] files = Directory.GetFiles(Folder, "BPVR_shot_*.txt");
			for (int i = 0; i < files.Length; i++)
			{
				if (int.TryParse(Path.GetFileNameWithoutExtension(files[i]).Substring(10), out var result) && result > _n)
				{
					_n = result;
				}
			}
		}
		catch
		{
		}
	}

	public static void Take()
	{
		try
		{
			Resume();
			_n++;
			string text = Path.Combine(Folder, $"BPVR_shot_{_n:00}");
			string path = text + ".txt";
			bool flag = Save(EyeRT, text + "_eye.png");
			bool flag2 = Save(EyeRT2, text + "_eyeR.png");
			bool flag3 = SaveFlat(text + "_flat.png");
			Save(UiRT, text + "_ui.png");
			StringBuilder stringBuilder = new StringBuilder();
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder3 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
			handler.AppendLiteral("time      ");
			handler.AppendFormatted(System.DateTime.Now, "HH:mm:ss");
			stringBuilder3.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder4 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
			handler.AppendLiteral("scene     ");
			handler.AppendFormatted(SceneManager.GetActiveScene().name);
			stringBuilder4.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder5 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(29, 3, stringBuilder2);
			handler.AppendLiteral("images    eyeL=");
			handler.AppendFormatted(flag ? "yes" : "NO");
			handler.AppendLiteral("  eyeR=");
			handler.AppendFormatted(flag2 ? "yes" : "NO");
			handler.AppendLiteral("  flat=");
			handler.AppendFormatted(flag3 ? "yes" : "NO");
			stringBuilder5.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder6 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(17, 2, stringBuilder2);
			handler.AppendLiteral("target    ");
			handler.AppendFormatted(TargetName);
			handler.AppendLiteral("  at ");
			handler.AppendFormatted(TargetDistance, "F2");
			handler.AppendLiteral(" m");
			stringBuilder6.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder7 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(29, 2, stringBuilder2);
			handler.AppendLiteral("ui world  canvases=");
			handler.AppendFormatted(UiWorld.Any);
			handler.AppendLiteral(" layers=0x");
			handler.AppendFormatted(UiWorld.Layers, "X");
			stringBuilder7.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder8 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(17, 2, stringBuilder2);
			handler.AppendLiteral("game aim  '");
			handler.AppendFormatted((Clickable.Target != null) ? Clickable.Target.name : "-");
			handler.AppendLiteral("' ");
			handler.AppendLiteral("has=");
			handler.AppendFormatted(Clickable.Has);
			stringBuilder8.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder9 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
			handler.AppendLiteral("clickable ");
			handler.AppendFormatted(TargetClickable);
			stringBuilder9.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder10 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
			handler.AppendLiteral("events    ");
			handler.AppendFormatted(TargetEvents);
			stringBuilder10.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder11 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
			handler.AppendLiteral("fsm state ");
			handler.AppendFormatted(TargetState);
			stringBuilder11.AppendLine(ref handler);
			try
			{
				Il2CppArrayBase<Camera> il2CppArrayBase = UnityEngine.Object.FindObjectsOfType<Camera>();
				stringBuilder.Append("cameras   ");
				int num = 0;
				while (il2CppArrayBase != null && num < il2CppArrayBase.Length && num < 64)
				{
					Camera camera = il2CppArrayBase[num];
					if (!(camera == null))
					{
						string value = "screen";
						try
						{
							value = ((camera.targetTexture != null) ? "RT" : "screen");
						}
						catch
						{
						}
						stringBuilder2 = stringBuilder;
						StringBuilder stringBuilder12 = stringBuilder2;
						handler = new StringBuilder.AppendInterpolatedStringHandler(13, 5, stringBuilder2);
						handler.AppendLiteral("'");
						handler.AppendFormatted(camera.name);
						handler.AppendLiteral("'[d");
						handler.AppendFormatted(camera.depth, "F0");
						handler.AppendLiteral("/");
						handler.AppendFormatted(value);
						handler.AppendLiteral("/");
						handler.AppendFormatted(camera.enabled ? "on" : "off");
						handler.AppendLiteral("/mask");
						handler.AppendFormatted(camera.cullingMask, "X");
						handler.AppendLiteral("] ");
						stringBuilder12.Append(ref handler);
					}
					num++;
				}
				stringBuilder.AppendLine();
			}
			catch
			{
			}
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder13 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(39, 5, stringBuilder2);
			handler.AppendLiteral("steps     ");
			handler.AppendFormatted(Steps.Value);
			handler.AppendLiteral("   ");
			handler.AppendLiteral("keys ");
			handler.AppendFormatted(Steps.Extra[0]);
			handler.AppendLiteral("  gems ");
			handler.AppendFormatted(Steps.Extra[1]);
			handler.AppendLiteral("  ");
			handler.AppendLiteral("gold ");
			handler.AppendFormatted(Steps.Extra[2]);
			handler.AppendLiteral("  dice ");
			handler.AppendFormatted(Steps.Extra[3]);
			stringBuilder13.AppendLine(ref handler);
			stringBuilder.Append(InteractionDiagnostics.Capture(_diagnosticTarget));
			File.WriteAllText(path, stringBuilder.ToString());
			MelonLogger.Msg($"[shot] {_n:00} eye={flag} flat={flag3} - target '{TargetName}' at {TargetDistance:F2}m, clickable={TargetClickable}");
		}
		catch (System.Exception ex)
		{
			MelonLogger.Warning("[shot] " + ex.Message);
		}
	}
}
public static class Steps
{
	private class Cand
	{
		public FsmInt V;

		public string Where;

		public int Score;

		public int First = int.MinValue;

		public bool Moved;
	}

	public static int Value = -1;

	public static string SourceName = "";

	public static bool HideHud = true;

	public const int Slots = 4;

	public static readonly int[] Extra = new int[4] { -1, -1, -1, -1 };

	public static readonly Color[] ExtraColor = new Color[4]
	{
		Color.white,
		Color.white,
		Color.white,
		Color.white
	};

	public static int ExtraCount;

	private static readonly TextMeshProUGUI[] _extra = new TextMeshProUGUI[4];

	private static float _rescan;

	private static bool _listed;

	private static bool _dumped;

	private static TextMeshProUGUI _label;

	private static float _next;

	private static int _warned;

	private static bool _hidden;

	private static StatsLogger _stats;

	private static bool _statsWork;

	private static bool _statsSaid;

	private static readonly System.Collections.Generic.List<Cand>[] _cand = new System.Collections.Generic.List<Cand>[5]
	{
		new System.Collections.Generic.List<Cand>(),
		new System.Collections.Generic.List<Cand>(),
		new System.Collections.Generic.List<Cand>(),
		new System.Collections.Generic.List<Cand>(),
		new System.Collections.Generic.List<Cand>()
	};

	private static readonly FsmInt[] _var = new FsmInt[5];

	private static bool _varsFound;

	private static float _varScan;

	private static float _liveLog;

	private static readonly string[][] Wanted = new string[5][]
	{
		new string[1] { "step" },
		new string[1] { "key" },
		new string[1] { "gem" },
		new string[2] { "coin", "gold" },
		new string[2] { "dice", "die" }
	};

	private static bool ReadStats()
	{
		try
		{
			if (_stats == null)
			{
				_stats = UnityEngine.Object.FindObjectOfType<StatsLogger>();
			}
			if (_stats == null)
			{
				return false;
			}
			DayStatsData currentDayStats = _stats.CurrentDayStats;
			if (currentDayStats == null)
			{
				return false;
			}
			Value = currentDayStats.StepsLeft;
			Extra[0] = currentDayStats.KeysLeft;
			Extra[1] = currentDayStats.GemsLeft;
			Extra[2] = currentDayStats.GoldLeft;
			Extra[3] = currentDayStats.IvDicesLeft;
			ExtraColor[0] = new Color(0.85f, 0.85f, 0.88f);
			ExtraColor[1] = new Color(0.45f, 0.8f, 0.95f);
			ExtraColor[2] = new Color(0.95f, 0.8f, 0.35f);
			ExtraColor[3] = new Color(0.92f, 0.92f, 0.86f);
			ExtraCount = 4;
			if (!_statsSaid)
			{
				_statsSaid = true;
				MelonLogger.Msg($"[hud] DayStatsData: steps={Value} keys={Extra[0]} gems={Extra[1]} gold={Extra[2]} dice={Extra[3]}");
			}
			return true;
		}
		catch (System.Exception ex)
		{
			if (_warned++ < 3)
			{
				MelonLogger.Warning("[hud] stats: " + ex.Message);
			}
			return false;
		}
	}

	private static int Rank(string owner, string name, string[] words)
	{
		int num = 0;
		if (owner != null)
		{
			if (owner.Equals("DAY", System.StringComparison.OrdinalIgnoreCase))
			{
				num += 120;
			}
			else if (owner.IndexOf("Global Manager", System.StringComparison.OrdinalIgnoreCase) >= 0)
			{
				num += 40;
			}
			if (owner.IndexOf("LAB", System.StringComparison.OrdinalIgnoreCase) >= 0)
			{
				num -= 80;
			}
			if (owner.IndexOf("CAM", System.StringComparison.OrdinalIgnoreCase) >= 0)
			{
				num -= 80;
			}
			if (owner.IndexOf("UI", System.StringComparison.OrdinalIgnoreCase) >= 0)
			{
				num -= 40;
			}
			if (owner.IndexOf("COST", System.StringComparison.OrdinalIgnoreCase) >= 0)
			{
				num -= 60;
			}
		}
		string[] array = words;
		foreach (string value in array)
		{
			if (name.Equals(value, System.StringComparison.OrdinalIgnoreCase))
			{
				num += 30;
			}
		}
		if (name.IndexOf("total", System.StringComparison.OrdinalIgnoreCase) >= 0)
		{
			num -= 50;
		}
		if (name.IndexOf("spent", System.StringComparison.OrdinalIgnoreCase) >= 0)
		{
			num -= 50;
		}
		if (name.IndexOf("end of", System.StringComparison.OrdinalIgnoreCase) >= 0)
		{
			num -= 50;
		}
		if (name.IndexOf("cost", System.StringComparison.OrdinalIgnoreCase) >= 0)
		{
			num -= 50;
		}
		array = new string[14]
		{
			"apple", "banana", "orange", "cupcake", "pizza", "salad", "soup", "steak", "sandwich", "quail",
			"salmon", "bacon", "stew", "turnip"
		};
		foreach (string value2 in array)
		{
			if (name.IndexOf(value2, System.StringComparison.OrdinalIgnoreCase) >= 0)
			{
				num -= 200;
			}
		}
		return num - name.Length;
	}

	private static int Pick(int slot)
	{
		System.Collections.Generic.List<Cand> list = _cand[slot];
		for (int i = 0; i < list.Count; i++)
		{
			Cand cand = list[i];
			int value;
			try
			{
				value = cand.V.Value;
			}
			catch
			{
				continue;
			}
			if (cand.First == int.MinValue)
			{
				cand.First = value;
			}
			else if (value != cand.First)
			{
				cand.Moved = true;
			}
		}
		for (int j = 0; j < list.Count; j++)
		{
			if (!list[j].Moved)
			{
				continue;
			}
			try
			{
				int value2 = list[j].V.Value;
				if (_var[slot] != list[j].V)
				{
					_var[slot] = list[j].V;
					MelonLogger.Msg($"[hud] slot {slot} settled on {list[j].Where} = {value2}");
				}
				return value2;
			}
			catch
			{
			}
		}
		for (int k = 0; k < list.Count; k++)
		{
			try
			{
				int value3 = list[k].V.Value;
				if (value3 > 0)
				{
					return value3;
				}
			}
			catch
			{
			}
		}
		if (list.Count > 0)
		{
			try
			{
				return list[0].V.Value;
			}
			catch
			{
			}
		}
		return -1;
	}

	private static bool Matches(string n, string[] words)
	{
		if (string.IsNullOrEmpty(n))
		{
			return false;
		}
		foreach (string value in words)
		{
			if (n.IndexOf(value, System.StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		return false;
	}

	private static bool ReadFsmVars()
	{
		try
		{
			if (_varsFound)
			{
				Value = Pick(0);
				for (int i = 1; i < 5; i++)
				{
					Extra[i - 1] = Pick(i);
				}
				if (Time.unscaledTime > _liveLog)
				{
					_liveLog = Time.unscaledTime + 60f;
					StringBuilder stringBuilder = new StringBuilder();
					System.Collections.Generic.List<Cand> list = _cand[0];
					for (int j = 0; j < list.Count && j < 14; j++)
					{
						try
						{
							StringBuilder stringBuilder2 = stringBuilder;
							StringBuilder stringBuilder3 = stringBuilder2;
							StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(2, 2, stringBuilder2);
							handler.AppendFormatted(list[j].Where);
							handler.AppendLiteral("=");
							handler.AppendFormatted(list[j].V.Value);
							handler.AppendLiteral(" ");
							stringBuilder3.Append(ref handler);
						}
						catch
						{
						}
					}
					MelonLogger.Msg("[hud] live step candidates: " + stringBuilder);
				}
				return true;
			}
			if (Time.unscaledTime < _varScan)
			{
				return false;
			}
			_varScan = Time.unscaledTime + 2f;
			Il2CppArrayBase<PlayMakerFSM> il2CppArrayBase = UnityEngine.Object.FindObjectsOfType<PlayMakerFSM>();
			if (il2CppArrayBase == null)
			{
				return false;
			}
			StringBuilder stringBuilder4 = new StringBuilder();
			int num = 0;
			for (int k = 0; k < il2CppArrayBase.Length; k++)
			{
				PlayMakerFSM playMakerFSM = il2CppArrayBase[k];
				if (playMakerFSM == null)
				{
					continue;
				}
				Il2CppReferenceArray<FsmInt> intVariables;
				string name;
				try
				{
					intVariables = playMakerFSM.Fsm.Variables.IntVariables;
					name = playMakerFSM.gameObject.name;
				}
				catch
				{
					continue;
				}
				if (intVariables == null)
				{
					continue;
				}
				for (int l = 0; l < intVariables.Length; l++)
				{
					FsmInt fsmInt = intVariables[l];
					if (fsmInt == null)
					{
						continue;
					}
					string name2;
					try
					{
						name2 = fsmInt.Name;
					}
					catch
					{
						continue;
					}
					if (string.IsNullOrEmpty(name2))
					{
						continue;
					}
					for (int m = 0; m < 5; m++)
					{
						if (Matches(name2, Wanted[m]))
						{
							int num2 = Rank(name, name2, Wanted[m]);
							if (num2 > -60)
							{
								_cand[m].Add(new Cand
								{
									V = fsmInt,
									Where = name + "." + name2,
									Score = num2
								});
							}
							if (m == 0 && num < 30)
							{
								num++;
								StringBuilder stringBuilder2 = stringBuilder4;
								StringBuilder stringBuilder5 = stringBuilder2;
								StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(3, 3, stringBuilder2);
								handler.AppendFormatted(name);
								handler.AppendLiteral(".");
								handler.AppendFormatted(name2);
								handler.AppendLiteral("=");
								handler.AppendFormatted(fsmInt.Value);
								handler.AppendLiteral(" ");
								stringBuilder5.Append(ref handler);
							}
						}
					}
				}
			}
			if (num > 0)
			{
				MelonLogger.Msg("[hud] step candidates: " + stringBuilder4);
			}
			for (int n = 0; n < 5; n++)
			{
				_cand[n].Sort((Cand a, Cand b) => b.Score.CompareTo(a.Score));
			}
			if (_cand[0].Count > 0)
			{
				StringBuilder stringBuilder6 = new StringBuilder();
				for (int num3 = 0; num3 < 5; num3++)
				{
					stringBuilder6.Append((_cand[num3].Count > 0) ? $"[{num3}]{_cand[num3][0].Where}({_cand[num3][0].Score}) " : $"[{num3}]none ");
				}
				MelonLogger.Msg("[hud] best guess per slot: " + stringBuilder6);
				_varsFound = true;
				ExtraCount = 4;
				ExtraColor[0] = new Color(0.85f, 0.85f, 0.88f);
				ExtraColor[1] = new Color(0.45f, 0.8f, 0.95f);
				ExtraColor[2] = new Color(0.95f, 0.8f, 0.35f);
				ExtraColor[3] = new Color(0.92f, 0.92f, 0.86f);
				MelonLogger.Msg("[hud] using FSM variables for the wrist panel");
				return true;
			}
		}
		catch (System.Exception ex)
		{
			if (_warned++ < 3)
			{
				MelonLogger.Warning("[hud] fsm vars: " + ex.Message);
			}
		}
		return false;
	}

	public static void Tick()
	{
		if (Time.unscaledTime < _next)
		{
			return;
		}
		_next = Time.unscaledTime + 0.2f;
		try
		{
			if (ReadFsmVars())
			{
				return;
			}
			if (ReadStats())
			{
				_statsWork = true;
			}
			else if (!_statsWork)
			{
				if (_label != null)
				{
					Read();
				}
				else
				{
					Search();
				}
				ReadExtras();
			}
		}
		catch (System.Exception ex)
		{
			if (_warned++ < 4)
			{
				MelonLogger.Warning("[steps] " + ex.Message);
			}
		}
	}

	private static void Read()
	{
		string text;
		try
		{
			text = _label.text;
		}
		catch
		{
			_label = null;
			_hidden = false;
			return;
		}
		if (Parse(text, out var v))
		{
			Value = v;
		}
		if (HideHud && !_hidden)
		{
			_hidden = true;
			HideCluster(_label.transform);
		}
	}

	private static bool Parse(string t, out int v)
	{
		v = 0;
		if (string.IsNullOrEmpty(t))
		{
			return false;
		}
		int num = 0;
		bool flag = false;
		foreach (char c in t)
		{
			if (c >= '0' && c <= '9')
			{
				num = num * 10 + (c - 48);
				flag = true;
				if (num > 999)
				{
					return false;
				}
				continue;
			}
			if (flag)
			{
				break;
			}
			if (c != ' ' && c != '\n' && c != '\r' && c != '\t')
			{
				return false;
			}
		}
		v = num;
		return flag;
	}

	private static void ReadExtras()
	{
		for (int i = 0; i < ExtraCount; i++)
		{
			TextMeshProUGUI textMeshProUGUI = _extra[i];
			if (!(textMeshProUGUI == null))
			{
				string text;
				try
				{
					text = textMeshProUGUI.text;
				}
				catch
				{
					_extra[i] = null;
					continue;
				}
				if (Parse(text, out var v))
				{
					Extra[i] = v;
				}
			}
		}
		if (ExtraCount > 0 && Time.unscaledTime < _rescan)
		{
			return;
		}
		_rescan = Time.unscaledTime + 3f;
		Il2CppArrayBase<TextMeshProUGUI> il2CppArrayBase = AllLabels();
		if (il2CppArrayBase == null)
		{
			return;
		}
		if (!_dumped)
		{
			_dumped = true;
			StringBuilder stringBuilder = new StringBuilder();
			int num = 0;
			for (int j = 0; j < il2CppArrayBase.Length && num < 30; j++)
			{
				TextMeshProUGUI textMeshProUGUI2 = il2CppArrayBase[j];
				if (!(textMeshProUGUI2 == null))
				{
					string text2;
					Vector3 position;
					try
					{
						text2 = textMeshProUGUI2.text;
						position = textMeshProUGUI2.transform.position;
					}
					catch
					{
						continue;
					}
					if (!string.IsNullOrEmpty(text2) && text2.Length <= 12)
					{
						StringBuilder stringBuilder2 = stringBuilder;
						StringBuilder stringBuilder3 = stringBuilder2;
						StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(8, 4, stringBuilder2);
						handler.AppendLiteral("'");
						handler.AppendFormatted(textMeshProUGUI2.gameObject.name);
						handler.AppendLiteral("'=\"");
						handler.AppendFormatted(text2.Trim());
						handler.AppendLiteral("\"@");
						handler.AppendFormatted(position.x, "F0");
						handler.AppendLiteral(",");
						handler.AppendFormatted(position.y, "F0");
						handler.AppendLiteral(" ");
						stringBuilder3.Append(ref handler);
						num++;
					}
				}
			}
			MelonLogger.Msg($"[hud] {il2CppArrayBase.Length} TMP labels, screen {Screen.width}x{Screen.height}: {stringBuilder}");
		}
		int num2 = 0;
		StringBuilder stringBuilder4 = (_listed ? null : new StringBuilder());
		for (int k = 0; k < il2CppArrayBase.Length && num2 < 4; k++)
		{
			TextMeshProUGUI textMeshProUGUI3 = il2CppArrayBase[k];
			if (textMeshProUGUI3 == null || textMeshProUGUI3 == _label)
			{
				continue;
			}
			string text3;
			try
			{
				text3 = textMeshProUGUI3.text;
			}
			catch
			{
				continue;
			}
			if (!Parse(text3, out var v2) || v2 < 0 || v2 > 999)
			{
				continue;
			}
			try
			{
				if (!textMeshProUGUI3.isActiveAndEnabled)
				{
					continue;
				}
			}
			catch
			{
				continue;
			}
			Vector3 position2;
			try
			{
				position2 = textMeshProUGUI3.transform.position;
			}
			catch
			{
				continue;
			}
			if (!(position2.y < (float)Screen.height * 0.75f))
			{
				Extra[num2] = v2;
				_extra[num2] = textMeshProUGUI3;
				try
				{
					ExtraColor[num2] = textMeshProUGUI3.color;
				}
				catch
				{
					ExtraColor[num2] = Color.white;
				}
				if (stringBuilder4 != null)
				{
					StringBuilder stringBuilder2 = stringBuilder4;
					StringBuilder stringBuilder5 = stringBuilder2;
					StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(6, 3, stringBuilder2);
					handler.AppendLiteral("'");
					handler.AppendFormatted(textMeshProUGUI3.gameObject.name);
					handler.AppendLiteral("'=");
					handler.AppendFormatted(v2);
					handler.AppendLiteral("@x");
					handler.AppendFormatted(position2.x, "F0");
					handler.AppendLiteral(" ");
					stringBuilder5.Append(ref handler);
				}
				num2++;
			}
		}
		ExtraCount = num2;
		if (stringBuilder4 != null && num2 > 0)
		{
			_listed = true;
			MelonLogger.Msg("[hud] counters: " + stringBuilder4);
		}
	}

	private static Il2CppArrayBase<TextMeshProUGUI> AllLabels()
	{
		try
		{
			return UnityEngine.Object.FindObjectsOfType<TextMeshProUGUI>();
		}
		catch
		{
			return null;
		}
	}

	private static bool NameSaysSteps(Transform t)
	{
		int num = 0;
		while (t != null && num < 4)
		{
			string name = t.name;
			if (!string.IsNullOrEmpty(name))
			{
				if (name.IndexOf("step", System.StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return true;
				}
				if (name.IndexOf("foot", System.StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return true;
				}
			}
			num++;
			t = t.parent;
		}
		return false;
	}

	private static void Search()
	{
		Il2CppArrayBase<TextMeshProUGUI> il2CppArrayBase = AllLabels();
		if (il2CppArrayBase == null || il2CppArrayBase.Length == 0)
		{
			return;
		}
		TextMeshProUGUI textMeshProUGUI = null;
		TextMeshProUGUI textMeshProUGUI2 = null;
		int num = -1;
		for (int i = 0; i < il2CppArrayBase.Length; i++)
		{
			TextMeshProUGUI textMeshProUGUI3 = il2CppArrayBase[i];
			if (textMeshProUGUI3 == null)
			{
				continue;
			}
			string text;
			try
			{
				text = textMeshProUGUI3.text;
			}
			catch
			{
				continue;
			}
			if (!Parse(text, out var v) || !textMeshProUGUI3.isActiveAndEnabled)
			{
				continue;
			}
			if (NameSaysSteps(textMeshProUGUI3.transform))
			{
				textMeshProUGUI = textMeshProUGUI3;
				break;
			}
			if (v <= 60 && v >= 1)
			{
				Vector3 position;
				try
				{
					position = textMeshProUGUI3.transform.position;
				}
				catch
				{
					continue;
				}
				if (!(position.x > (float)Screen.width * 0.4f) && !(position.y < (float)Screen.height * 0.6f) && textMeshProUGUI2 == null)
				{
					textMeshProUGUI2 = textMeshProUGUI3;
					num = v;
				}
			}
		}
		TextMeshProUGUI textMeshProUGUI4 = textMeshProUGUI ?? textMeshProUGUI2;
		if (!(textMeshProUGUI4 == null))
		{
			_label = textMeshProUGUI4;
			SourceName = textMeshProUGUI4.gameObject.name;
			MelonLogger.Msg($"[steps] reading the HUD label '{SourceName}' (parent '{((textMeshProUGUI4.transform.parent != null) ? textMeshProUGUI4.transform.parent.name : "-")}', {((textMeshProUGUI != null) ? "matched by name" : ("matched by shape, value " + num))})");
			Read();
		}
	}

	private static void HideCluster(Transform label)
	{
		try
		{
			Transform parent = label.parent;
			if (parent != null && parent.childCount <= 4 && NameSaysSteps(parent))
			{
				parent.gameObject.SetActive(value: false);
				MelonLogger.Msg("[steps] hid HUD group '" + parent.name + "'");
				return;
			}
			label.gameObject.SetActive(value: false);
			if (parent != null)
			{
				for (int i = 0; i < parent.childCount; i++)
				{
					Transform child = parent.GetChild(i);
					if (!(child == null) && !(child == label))
					{
						string text = child.name ?? "";
						if (text.IndexOf("step", System.StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("foot", System.StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("icon", System.StringComparison.OrdinalIgnoreCase) >= 0)
						{
							child.gameObject.SetActive(value: false);
						}
					}
				}
			}
			MelonLogger.Msg("[steps] hid HUD label '" + SourceName + "'");
		}
		catch (System.Exception ex)
		{
			if (_warned++ < 4)
			{
				MelonLogger.Warning("[steps] hiding: " + ex.Message);
			}
		}
	}
}
public static class UiWorld
{
	private class Adopted
	{
		public Canvas C;

		public RenderMode Was;

		public Camera WasCam;

		public Vector3 WasScale;

		public Vector2 WasSize;

		public bool HadScaler;
	}

	public static bool Enabled = true;

	public static float Width = 2.2f;

	public static float Distance = 1.7f;

	public static float Drop = 0.22f;

	public static Camera EventCamera;

	public static string Only = "Action canvas";

	private static readonly System.Collections.Generic.List<Adopted> _taken = new System.Collections.Generic.List<Adopted>();

	private static float _nextScan;

	private static int _log;

	private static int _layers;

	public static bool Any => _taken.Count > 0;

	public static int Layers => _layers;

	public static void Scan()
	{
		if (!Enabled)
		{
			Release();
		}
		else
		{
			if (Time.unscaledTime < _nextScan)
			{
				return;
			}
			_nextScan = Time.unscaledTime + 1f;
			Il2CppArrayBase<Canvas> il2CppArrayBase;
			try
			{
				il2CppArrayBase = UnityEngine.Object.FindObjectsOfType<Canvas>();
			}
			catch
			{
				return;
			}
			if (il2CppArrayBase == null)
			{
				return;
			}
			for (int i = 0; i < il2CppArrayBase.Length; i++)
			{
				Canvas canvas = il2CppArrayBase[i];
				if (canvas == null || canvas.renderMode != RenderMode.ScreenSpaceOverlay || Known(canvas) || (Only.Length > 0 && canvas.name.IndexOf(Only, System.StringComparison.OrdinalIgnoreCase) < 0))
				{
					continue;
				}
				try
				{
					if (!canvas.isRootCanvas)
					{
						continue;
					}
				}
				catch
				{
				}
				try
				{
					RectTransform component = canvas.GetComponent<RectTransform>();
					Adopted adopted = new Adopted
					{
						C = canvas,
						Was = canvas.renderMode,
						WasCam = canvas.worldCamera,
						WasScale = canvas.transform.localScale,
						WasSize = ((component != null) ? component.sizeDelta : Vector2.zero)
					};
					canvas.renderMode = RenderMode.WorldSpace;
					bool flag = false;
					try
					{
						flag = canvas.renderMode == RenderMode.WorldSpace;
					}
					catch
					{
					}
					if (!flag)
					{
						if (_log++ < 8)
						{
							MelonLogger.Warning("[uiworld] '" + canvas.name + "' refused the world-space switch - leaving it as an overlay");
						}
						continue;
					}
					canvas.worldCamera = ((EventCamera != null) ? EventCamera : Camera.main);
					try
					{
						CanvasScaler component2 = canvas.GetComponent<CanvasScaler>();
						if (component2 != null)
						{
							adopted.HadScaler = true;
							component2.enabled = false;
						}
					}
					catch
					{
					}
					if (component != null)
					{
						component.sizeDelta = new Vector2(1920f, 1080f);
						component.pivot = new Vector2(0.5f, 0.5f);
					}
					_taken.Add(adopted);
					_layers |= 1 << canvas.gameObject.layer;
					ForceAlwaysOnTop(canvas);
					try
					{
						Canvas.ForceUpdateCanvases();
					}
					catch
					{
					}
					if (_log++ < 8)
					{
						MelonLogger.Msg($"[uiworld] '{canvas.name}' (layer {canvas.gameObject.layer}) " + "is now real geometry in the room");
					}
				}
				catch (System.Exception ex)
				{
					if (_log++ < 8)
					{
						MelonLogger.Warning("[uiworld] " + canvas.name + ": " + ex.Message);
					}
				}
			}
		}
	}

	private static void ForceAlwaysOnTop(Canvas c)
	{
		try
		{
			Il2CppArrayBase<Graphic> componentsInChildren = c.GetComponentsInChildren<Graphic>(includeInactive: true);
			int num = 0;
			while (componentsInChildren != null && num < componentsInChildren.Length)
			{
				Graphic graphic = componentsInChildren[num];
				if (!(graphic == null))
				{
					Material materialForRendering = graphic.materialForRendering;
					if (!(materialForRendering == null) && materialForRendering.HasProperty("unity_GUIZTestMode"))
					{
						materialForRendering.SetInt("unity_GUIZTestMode", 8);
					}
				}
				num++;
			}
		}
		catch
		{
		}
	}

	private static bool Known(Canvas c)
	{
		for (int i = 0; i < _taken.Count; i++)
		{
			if (_taken[i].C != null && _taken[i].C.GetInstanceID() == c.GetInstanceID())
			{
				return true;
			}
		}
		return false;
	}

	public static void Place(Vector3 eye, Quaternion look)
	{
		if (_taken.Count == 0)
		{
			return;
		}
		Vector3 vector = look * Vector3.forward;
		Vector3 vector2 = look * Vector3.up;
		float num = Mathf.Max(0.0001f, Width / 1920f);
		Vector3 vector3 = eye + vector * Distance - vector2 * Drop;
		Quaternion rotation = Quaternion.LookRotation(vector3 - eye, vector2);
		for (int num2 = _taken.Count - 1; num2 >= 0; num2--)
		{
			Adopted adopted = _taken[num2];
			if (adopted.C == null)
			{
				_taken.RemoveAt(num2);
			}
			else
			{
				try
				{
					Transform transform = adopted.C.transform;
					transform.SetPositionAndRotation(vector3, rotation);
					transform.localScale = new Vector3(num, num, num);
				}
				catch
				{
					_taken.RemoveAt(num2);
				}
			}
		}
	}

	public static bool ScreenPoint(Vector3 origin, Vector3 dir, out Vector3 screen)
	{
		screen = Vector3.zero;
		if (_taken.Count == 0)
		{
			return false;
		}
		Transform transform = null;
		for (int i = 0; i < _taken.Count; i++)
		{
			if (!(transform == null))
			{
				break;
			}
			if (_taken[i].C != null)
			{
				transform = _taken[i].C.transform;
			}
		}
		if (transform == null)
		{
			return false;
		}
		try
		{
			Vector3 rhs = -transform.forward;
			float num = Vector3.Dot(dir, rhs);
			if (Mathf.Abs(num) < 1E-05f)
			{
				return false;
			}
			float num2 = Vector3.Dot(transform.position - origin, rhs) / num;
			if (num2 <= 0f)
			{
				return false;
			}
			Vector3 position = origin + dir * num2;
			Vector3 vector = transform.InverseTransformPoint(position);
			float num3 = Mathf.Clamp(vector.x + 960f, 1f, 1919f);
			float num4 = Mathf.Clamp(vector.y + 540f, 1f, 1079f);
			screen = new Vector3(num3 / 1920f * (float)Screen.width, num4 / 1080f * (float)Screen.height, num2);
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static void Release()
	{
		for (int i = 0; i < _taken.Count; i++)
		{
			Adopted adopted = _taken[i];
			if (adopted.C == null)
			{
				continue;
			}
			try
			{
				adopted.C.renderMode = adopted.Was;
				adopted.C.worldCamera = adopted.WasCam;
				adopted.C.transform.localScale = adopted.WasScale;
				RectTransform component = adopted.C.GetComponent<RectTransform>();
				if (component != null && adopted.WasSize != Vector2.zero)
				{
					component.sizeDelta = adopted.WasSize;
				}
				if (adopted.HadScaler)
				{
					CanvasScaler component2 = adopted.C.GetComponent<CanvasScaler>();
					if (component2 != null)
					{
						component2.enabled = true;
					}
				}
			}
			catch
			{
			}
		}
		_taken.Clear();
		_layers = 0;
	}
}
public static class VRInput
{
	public static bool Enabled;

	public static float MoveX;

	public static float MoveY;

	public static float LookX;

	public static float LookY;

	public static bool Interact;

	public static bool Cancel;

	public static bool Menu;

	public static bool NavUp;

	public static bool NavDown;

	public static bool NavLeft;

	public static bool NavRight;

	public static bool Select;

	public static bool NavUpEdge;

	public static bool NavDownEdge;

	public static bool NavLeftEdge;

	public static bool NavRightEdge;

	public static bool SelectEdge;

	public static int InteractDownFrames;

	public static string ActMoveX = "";

	public static string ActMoveY = "";

	public static string ActLookX = "";

	public static string ActLookY = "";

	public static string ActInteract = "";

	public static string ActCancel = "";

	public static string ActMenu = "";

	public static string ActNavUp = "";

	public static string ActNavDown = "";

	public static string ActNavLeft = "";

	public static string ActNavRight = "";

	public static string ActSelect = "";

	public static string ActDirectory = "room directory";

	public static string ActBlueprint = "blueprint menu";

	public static bool Directory;

	public static bool DirectoryEdge;

	public static bool Blueprint;

	public static bool BlueprintEdge;

	public static string ActUseItem = "use item";

	public static string ActInteract2 = "interact";

	public static string ActUICancel = "UICancel";
	public static bool NativeKeyboard;

	public static bool UseItem;

	public static bool UseItemEdge;

	public static bool CancelEdge;

	public static bool MenuEdge;

	public static string ActStroll = "stroll";

	public static bool Stroll;

	public static bool StrollEdge;

	public static string ActEnter = "enter";

	public static bool BlockLookY = true;

	public static bool MenuMode;

	private static readonly HashSet<string> _seenAxis = new HashSet<string>();

	private static readonly HashSet<string> _seenButton = new HashSet<string>();

	private static int _logged;

	public static int AxisHits;

	public static int ButtonHits;

	private static readonly HashSet<string> _live = new HashSet<string>();

	public static bool TakeInteractPress()
	{
		if (InteractDownFrames <= 0)
		{
			return false;
		}
		InteractDownFrames = 0;
		return true;
	}

	private static void Note(HashSet<string> set, string kind, string id)
	{
		if (_logged <= 200 && set.Add(id))
		{
			_logged++;
			MelonLogger.Msg($"[rewired] game polls {kind} '{id}'");
		}
	}

	private static bool IsNav(string id, string dir)
	{
		if (!string.Equals(id, dir, System.StringComparison.OrdinalIgnoreCase))
		{
			return string.Equals(id, dir + "_stick", System.StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	private static bool Match(string want, string id)
	{
		if (want.Length > 0)
		{
			return string.Equals(want, id, System.StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	public static bool AxisFor(string id, out float value)
	{
		value = 0f;
		if (NativeKeyboard) return false;
		if (!Enabled)
		{
			return false;
		}
		if (Match(ActMoveX, id))
		{
			value = MoveX;
			AxisHits++;
			return true;
		}
		if (Match(ActMoveY, id))
		{
			value = MoveY;
			AxisHits++;
			return true;
		}
		if (Match(ActLookX, id))
		{
			value = LookX;
			AxisHits++;
			return true;
		}
		if (Match(ActLookY, id))
		{
			value = (BlockLookY ? 0f : LookY);
			return true;
		}
		return false;
	}

	public static bool ButtonFor(string id, out bool value)
	{
		value = false;
		if (NativeKeyboard) return false;
		if (!Enabled)
		{
			return false;
		}
		if (Match(ActInteract, id))
		{
			value = Interact;
			ButtonHits++;
			return true;
		}
		if (Match(ActInteract2, id))
		{
			value = Interact;
			ButtonHits++;
			return true;
		}
		if (Match(ActUseItem, id))
		{
			value = UseItem;
			ButtonHits++;
			return true;
		}
		if (Match(ActStroll, id))
		{
			value = Stroll;
			ButtonHits++;
			return true;
		}
		if (Match(ActEnter, id))
		{
			value = Select;
			ButtonHits++;
			return true;
		}
		if (Match(ActCancel, id))
		{
			value = Cancel;
			return true;
		}
		if (Match(ActUICancel, id))
		{
			value = Cancel;
			return true;
		}
		if (Match(ActMenu, id))
		{
			value = Menu;
			return true;
		}
		if (IsNav(id, "up"))
		{
			value = NavUp;
			ButtonHits++;
			return true;
		}
		if (IsNav(id, "down"))
		{
			value = NavDown;
			ButtonHits++;
			return true;
		}
		if (IsNav(id, "left"))
		{
			value = NavLeft;
			ButtonHits++;
			return true;
		}
		if (IsNav(id, "right"))
		{
			value = NavRight;
			ButtonHits++;
			return true;
		}
		if (Match(ActNavUp, id))
		{
			value = NavUp;
			return true;
		}
		if (Match(ActNavDown, id))
		{
			value = NavDown;
			return true;
		}
		if (Match(ActNavLeft, id))
		{
			value = NavLeft;
			return true;
		}
		if (Match(ActNavRight, id))
		{
			value = NavRight;
			return true;
		}
		if (Match(ActSelect, id))
		{
			value = Select;
			return true;
		}
		if (Match(ActDirectory, id))
		{
			value = Directory;
			return true;
		}
		if (Match(ActBlueprint, id))
		{
			return false; // Native Tab input owns floor-plan toggling.
		}
		return false;
	}

	public static bool ButtonDownFor(string id, out bool value)
	{
		value = false;
		if (NativeKeyboard) return false;
		if (!Enabled)
		{
			return false;
		}
		if (Match(ActInteract, id))
		{
			value = TakeInteractPress();
			ButtonHits++;
			return true;
		}
		if (Match(ActInteract2, id))
		{
			value = TakeInteractPress();
			ButtonHits++;
			return true;
		}
		if (Match(ActUseItem, id))
		{
			value = UseItemEdge;
			ButtonHits++;
			return true;
		}
		if (Match(ActStroll, id))
		{
			value = StrollEdge;
			ButtonHits++;
			return true;
		}
		if (Match(ActEnter, id))
		{
			value = SelectEdge;
			ButtonHits++;
			return true;
		}
		if (Match(ActMenu, id))
		{
			value = MenuEdge;
			ButtonHits++;
			return true;
		}
		if (Match(ActCancel, id))
		{
			value = CancelEdge;
			return true;
		}
		if (Match(ActUICancel, id))
		{
			value = CancelEdge;
			return true;
		}
		if (IsNav(id, "up"))
		{
			value = NavUpEdge;
			ButtonHits++;
			return true;
		}
		if (IsNav(id, "down"))
		{
			value = NavDownEdge;
			ButtonHits++;
			return true;
		}
		if (IsNav(id, "left"))
		{
			value = NavLeftEdge;
			ButtonHits++;
			return true;
		}
		if (IsNav(id, "right"))
		{
			value = NavRightEdge;
			ButtonHits++;
			return true;
		}
		if (Match(ActNavUp, id))
		{
			value = NavUpEdge;
			return true;
		}
		if (Match(ActNavDown, id))
		{
			value = NavDownEdge;
			return true;
		}
		if (Match(ActNavLeft, id))
		{
			value = NavLeftEdge;
			return true;
		}
		if (Match(ActNavRight, id))
		{
			value = NavRightEdge;
			return true;
		}
		if (Match(ActSelect, id))
		{
			value = SelectEdge;
			return true;
		}
		if (Match(ActDirectory, id))
		{
			value = DirectoryEdge;
			return true;
		}
		if (Match(ActBlueprint, id))
		{
			return false; // Native Tab input owns floor-plan toggling.
		}
		return false;
	}

	public static void NoteAxis(string id)
	{
		Note(_seenAxis, "axis", id);
	}

	public static void NoteLive(string id, float v)
	{
		if (!(Mathf.Abs(v) < 0.25f) && _live.Add(id))
		{
			MelonLogger.Msg($"[rewired] axis '{id}' went LIVE with {v:F2}  <-- a real stick drives this");
		}
	}

	public static void NoteButton(string id)
	{
		Note(_seenButton, "button", id);
	}
}
[HarmonyPatch(typeof(Player), "GetAxis", new System.Type[] { typeof(int) })]
internal static class P_GetAxisInt
{
	private static void Postfix(int actionId, ref float __result)
	{
		string id = actionId.ToString();
		VRInput.NoteAxis(id);
		VRInput.NoteLive(id, __result);
		if (VRInput.AxisFor(id, out var value))
		{
			__result = value;
		}
	}
}
[HarmonyPatch(typeof(Player), "GetAxis", new System.Type[] { typeof(string) })]
internal static class P_GetAxisStr
{
	private static void Postfix(string actionName, ref float __result)
	{
		if (actionName != null)
		{
			VRInput.NoteAxis(actionName);
			VRInput.NoteLive(actionName, __result);
			if (VRInput.AxisFor(actionName, out var value))
			{
				__result = value;
			}
		}
	}
}
[HarmonyPatch(typeof(Player), "GetAxisRaw", new System.Type[] { typeof(int) })]
internal static class P_GetAxisRawInt
{
	private static void Postfix(int actionId, ref float __result)
	{
		string id = actionId.ToString();
		VRInput.NoteLive(id, __result);
		if (VRInput.AxisFor(id, out var value))
		{
			__result = value;
		}
	}
}
[HarmonyPatch(typeof(Player), "GetAxisRaw", new System.Type[] { typeof(string) })]
internal static class P_GetAxisRawStr
{
	private static void Postfix(string actionName, ref float __result)
	{
		if (actionName != null && VRInput.AxisFor(actionName, out var value))
		{
			__result = value;
		}
	}
}
[HarmonyPatch(typeof(Player), "GetButton", new System.Type[] { typeof(int) })]
internal static class P_GetButtonInt
{
	private static void Postfix(int actionId, ref bool __result)
	{
		string id = actionId.ToString();
		VRInput.NoteButton(id);
		if (VRInput.ButtonFor(id, out var value) && value)
		{
			__result = true;
		}
	}
}
[HarmonyPatch(typeof(Player), "GetButton", new System.Type[] { typeof(string) })]
internal static class P_GetButtonStr
{
	private static void Postfix(string actionName, ref bool __result)
	{
		if (actionName != null)
		{
			VRInput.NoteButton(actionName);
			if (VRInput.ButtonFor(actionName, out var value) && value)
			{
				__result = true;
			}
		}
	}
}
[HarmonyPatch(typeof(Player), "GetButtonDown", new System.Type[] { typeof(int) })]
internal static class P_GetButtonDownInt
{
	private static void Postfix(int actionId, ref bool __result)
	{
		string text = actionId.ToString();
		VRInput.NoteButton("down:" + text);
		if (VRInput.ButtonDownFor(text, out var value))
		{
			if (value)
			{
				__result = true;
			}
		}
		else if (VRInput.ButtonFor(text, out value) && value && VRPlugin.EdgeThisFrame)
		{
			__result = true;
		}
	}
}
[HarmonyPatch(typeof(Player), "GetButtonDown", new System.Type[] { typeof(string) })]
internal static class P_GetButtonDownStr
{
	private static void Postfix(string actionName, ref bool __result)
	{
		if (actionName == null)
		{
			return;
		}
		VRInput.NoteButton("down:" + actionName);
		if (VRInput.ButtonDownFor(actionName, out var value))
		{
			if (value)
			{
				__result = true;
			}
		}
		else if (VRInput.ButtonFor(actionName, out value) && value && VRPlugin.EdgeThisFrame)
		{
			__result = true;
		}
	}
}
public partial class VRPlugin : MelonMod
{
	private struct POINT
	{
		public int x;

		public int y;
	}

	private static IVRSystem_FnTable _sys;

	private static IVRCompositor_FnTable _comp;

	private bool _vrReady;

	private string _initError;

	private uint _eyeW;

	private uint _eyeH;

	private Camera[] _eyeCam = new Camera[2];

	private RenderTexture[] _eyeRT = new RenderTexture[2];

	private int[] _rtBuilds = new int[2];

	private System.IntPtr[] _eyeTexPtr = new System.IntPtr[2];

	private Matrix4x4[] _eyeProj = new Matrix4x4[2];

	private Vector3[] _eyeOffset = new Vector3[2];

	private GameObject _rig;

	private TrackedDevicePose_t[] _renderPoses = new TrackedDevicePose_t[64];

	private TrackedDevicePose_t[] _gamePoses = new TrackedDevicePose_t[64];

	private Quaternion _headRot = Quaternion.identity;
	private float _movementHeadYaw;

	private Vector3 _headPos = Vector3.zero;

	private Vector3 _origin = Vector3.zero;

	private bool _haveOrigin;

	private float _recenterHeld;

	private bool _recenterArmed = true;

	private bool _pacingApplied;

	private int _pacingResets;

	private bool _rendering;

	private int _skippedRT;

	private int _skippedPaused;

	private int _sceneGrace;

	private int _camMissingFrames;

	private int _framesRendered;

	private int _reportEvery = 600;

	private MelonPreferences_Category _cfg;

	private MelonPreferences_Entry<bool> _pEnabled;

	private MelonPreferences_Entry<float> _pResScale;

	private MelonPreferences_Entry<bool> _pPositional;

	private MelonPreferences_Entry<float> _pIpdScale;

	private MelonPreferences_Entry<bool> _pFlipY;

	private MelonPreferences_Entry<bool> _pUnlockFramerate;

	private MelonPreferences_Entry<int> _pDiagLevel;

	private MelonPreferences_Entry<int> _pColorSpace;

	private MelonPreferences_Entry<bool> _pForceClear;

	private MelonPreferences_Entry<int> _pEyeTexMode;

	private MelonPreferences_Entry<bool> _pEyeHDR;

	private MelonPreferences_Entry<bool> _pThroughGameCam;

	private MelonPreferences_Entry<string> _pEffects;

	private MelonPreferences_Entry<bool> _pHands;

	private MelonPreferences_Entry<bool> _pVRInput;

	private MelonPreferences_Entry<bool> _pBlockLookY;

	private MelonPreferences_Entry<string> _pActMoveX;

	private MelonPreferences_Entry<string> _pActMoveY;

	private MelonPreferences_Entry<string> _pActLookX;

	private MelonPreferences_Entry<string> _pActLookY;

	private MelonPreferences_Entry<string> _pActInteract;

	private MelonPreferences_Entry<string> _pActCancel;

	private MelonPreferences_Entry<string> _pActMenu;

	private MelonPreferences_Entry<string> _pNavUp;

	private MelonPreferences_Entry<string> _pNavDown;

	private MelonPreferences_Entry<string> _pNavLeft;

	private MelonPreferences_Entry<string> _pNavRight;

	private MelonPreferences_Entry<string> _pSelect;

	private MelonPreferences_Entry<bool> _pHandsInHouseOnly;

	private MelonPreferences_Entry<float> _pLookSens;

	private MelonPreferences_Entry<float> _pLookDead;

	private MelonPreferences_Entry<float> _pLookSmooth;

	private MelonPreferences_Entry<bool> _pDriveCursor;

	private MelonPreferences_Entry<int> _pTurnMode;

	private MelonPreferences_Entry<bool> _pTriggerClicks;

	private MelonPreferences_Entry<bool> _pUnlockToClick;

	private MelonPreferences_Entry<bool> _pDirectGrab;

	private MelonPreferences_Entry<bool> _pNamedEvents;

	private MelonPreferences_Entry<float> _pTurnPixels;

	private int _cursorLog;

	private float _lookXSmoothed;

	public static bool EdgeThisFrame;

	private MelonPreferences_Entry<float> _pHandScale;

	private MelonPreferences_Entry<bool> _pModelArms;

	private MelonPreferences_Entry<float> _pHandPitch;

	private MelonPreferences_Entry<float> _pHandYaw;

	private MelonPreferences_Entry<float> _pHandRoll;

	private MelonPreferences_Entry<float> _pJointThickness;

	private MelonPreferences_Entry<bool> _pArmsUnlit;

	private MelonPreferences_Entry<int> _pProjectionMode;

	private MelonPreferences_Entry<bool> _pSwapArmSides;

	private MelonPreferences_Entry<float> _pArmScale;

	private MelonPreferences_Entry<float> _pArmBrightness;

	private MelonPreferences_Entry<bool> _pWatch;

	private MelonPreferences_Entry<float> _pWatchSize;

	private MelonPreferences_Entry<bool> _pHideStepHud;

	private MelonPreferences_Entry<bool> _pSkipIntro;

	private MelonPreferences_Entry<bool> _pHudStrip;

	private MelonPreferences_Entry<float> _pAimRadius;

	private MelonPreferences_Entry<bool> _pAdoptCanvases;

	private MelonPreferences_Entry<bool> _pHudFull;

	private MelonPreferences_Entry<bool> _pUiWorld;

	private MelonPreferences_Entry<bool> _pMoveOsPointer;

	private MelonPreferences_Entry<float> _pUiWorldWidth;

	private MelonPreferences_Entry<float> _pUiWorldDist;

	private MelonPreferences_Entry<float> _pUiWorldDrop;

	private MelonPreferences_Entry<float> _pHudFullWidth;

	private MelonPreferences_Entry<float> _pHudFullDist;

	private MelonPreferences_Entry<float> _pHudFullDrop;

	private MelonPreferences_Entry<float> _pHudDistance;

	private MelonPreferences_Entry<float> _pHudWidth;

	private MelonPreferences_Entry<float> _pHudRise;

	private MelonPreferences_Entry<float> _pHudStripTop;

	private MelonPreferences_Entry<float> _pHudScale;

	private bool _leftGrip;

	private bool _rightGrip;

	private bool _leftA;

	private bool _leftB;

	private bool _forcePanel;

	private bool _canvasCensus;

	internal static float AimRadius = 0.1f;

	private int _aimWarn;

	private MelonPreferences_Entry<float> _pShoulderDrop;

	private MelonPreferences_Entry<bool> _pUIInVR;

	private MelonPreferences_Entry<float> _pUIDistance;

	private MelonPreferences_Entry<bool> _pShowCursor;

	private MelonPreferences_Entry<float> _pCursorDistance;

	private MelonPreferences_Entry<float> _pAimPitch;

	private MelonPreferences_Entry<float> _pCursorSmooth;

	private MelonPreferences_Entry<bool> _pCameraCensus;

	private MelonPreferences_Entry<float> _pUIParallax;

	private MelonPreferences_Entry<float> _pUIScale;

	private MelonPreferences_Entry<float> _pUITint;

	private MelonPreferences_Entry<float> _pCrouch;

	private MelonPreferences_Entry<bool> _pHudDirect;

	private MelonPreferences_Entry<bool> _pHeadUnstick;

	private MelonPreferences_Entry<float> _pHudDirectScale;

	private MelonPreferences_Entry<float> _pHudDirectParallax;

	private MelonPreferences_Entry<bool> _pShowBeam;

	private MelonPreferences_Entry<bool> _pUIPanel;

	private MelonPreferences_Entry<float> _pUIPanelWidth;

	private MelonPreferences_Entry<bool> _pSteerView;

	private MelonPreferences_Entry<float> _pSteerSpeed;

	private MelonPreferences_Entry<bool> _pSteerYawOnly;

	private MelonPreferences_Entry<float> _pSteerMaxDeg;

	private Hand _handL;

	private Hand _handR;

	private Camera _gameCamForCursor;

	private double _eyeMsAvg;

	private int _handLogged;

	private string _effectsFor;

	private int[] _submitFails = new int[2];

	private int[] _lastSubmitErr = new int[2];

	private int[] _prevSubmitErr = new int[2];

	private const uint MOUSEEVENTF_MOVE = 1u;

	private float _turnCarry;

	private bool _turning;

	private int _turnLog;

	private int _inputErr;

	private bool _prevInteract;

	private bool _prevUseItem;

	private bool _prevCancel;

	private bool _prevStroll;

	private bool _prevMenuBtn;

	private bool _crouchHeld;
	private readonly CrouchToggle _crouchToggle=new();
	private bool _crouchPressed,_crouchControllerValid;

	private float _crouch;

	private int _shoveLog;

	private float _navX;

	private float _navY;

	private bool _navUpHeld;

	private bool _navDownHeld;

	private bool _navLeftHeld;

	private bool _navRightHeld;

	private bool _selectHeld;

	private int _inputFrames;

	private int _ptrLog;

	private bool _windowLogged;

	private const uint MOUSEEVENTF_ABSOLUTE = 32768u;

	private bool _ptrCal;

	private int _ptrCalStage;

	private int _ptrCalWait;

	private Vector2 _ptrSample1;

	private float _kx = 1f;

	private float _ky = 1f;

	private float _cx;

	private float _cy;

	private const int CalA = 16000;

	private const int CalB = 48000;

	private GameObject _cursorMarker;

	private Renderer _cursorRenderer;

	private bool _steerCal;

	private bool _steerDead;

	private int _calStage;

	private int _calFrame;

	private int _calTries;

	private float _calYaw0;

	private float _calPitch0;

	private float _calNudge = 16f;

	private float _degPerPixX;

	private float _degPerPixY;

	private int _steerLog;

	private float _steerHeld;

	private float _lastErr;

	private int _worseFrames;

	private bool _steerAligned;

	private bool _steerLocked;

	private Vector3 _steerGoal;

	private int _panelAimLog;

	private string _panelThird = "?";

	private float _panelLocalX;

	private const uint MOUSEEVENTF_LEFTDOWN = 2u;

	private const uint MOUSEEVENTF_LEFTUP = 4u;

	private bool _mouseHeld;

	private bool _unlocked;

	private bool _clickFreeze;

	private Vector2 _frozenAim;

	private bool _haveFrozenAim;

	private int _unlockFrames;

	private int _unlockLog;

	private int _releaseFrames;

	private CursorLockMode _savedLock = CursorLockMode.Locked;

	private int _clickLog;

	private float _fpsWindowStart;

	private int _fpsWindowFrames;

	private Camera _uiCam;

	private RenderTexture _uiRT;

	private GameObject _uiQuad;

	private Renderer _uiQuadRenderer;

	private int _uiPanelLog;

	private float _uiDist;

	private Vector3 _panelAim = Vector3.forward;

	private bool _panelAimValid;

	private int _hudDirectLog;

	private int _hudAxisLog;

	private int _uiCamLog;

	private int _canvasLog;

	private int _movedLayers;

	private int _aimLog;

	private float _cursorSx;

	private float _cursorSy;

	private bool _cursorHave;

	private bool _lockNoted;

	private Vector3 _handTarget;

	private bool _handTargetValid;

	private bool _handTargetIsHit;

	private bool _armsTried;

	private int _armGoneLog;

	private static readonly string[] GrabMarkers = new string[6] { "IClickable", "UISelectable", "LabelObject", "Clickable", "Interactable", "Selectable" };

	private Texture2D _probeTex;

	private int _dumpsLeft = 1;

	private int _dumpAt = -1;

	private bool _projLogged;

	private int _lastCanvasScan = -999;

	private Camera _dumpCam;

	private Vector3 _dumpPos;

	private Quaternion _dumpRot;

	private Camera _fallbackCam;

	private string _lastCamName;

	private bool _inHouse;

	private bool UILogWanted
	{
		get
		{
			if (_uiCamLog >= 4)
			{
				return _framesRendered % 900 < 2;
			}
			return true;
		}
	}

	public override void OnInitializeMelon()
	{
		try
		{
			MelonLogger.Msg((AccessTools.Method(typeof(ClickableRaycaster), "GetCurrentScreenPoint") != null) ? "[pick] found ClickableRaycaster.GetCurrentScreenPoint - aim override armed" : "[pick] ClickableRaycaster has no GetCurrentScreenPoint; aim override is dead");
		}
		catch (System.Exception ex)
		{
			LogWarn("[pick] ClickableRaycaster unavailable: " + ex.Message);
		}
		try
		{
			MelonLogger.Msg((AccessTools.PropertyGetter(typeof(Mouse), "screenPosition") != null) ? "[pick] found Rewired Mouse.screenPosition - aim override armed" : "[pick] Rewired Mouse has no screenPosition getter");
		}
		catch (System.Exception ex2)
		{
			LogWarn("[pick] Rewired mouse unavailable: " + ex2.Message);
		}
		_cfg = MelonPreferences.CreateCategory("BluePrinceVR_Codex");
		_pEnabled = _cfg.CreateEntry("Enabled", default_value: true, null, "Master switch.");
		_pResScale = _cfg.CreateEntry("ResolutionScale", 0.5f, null, "Multiplier on the headset's recommended per-eye size. Lower if you drop frames.");
		_pFlipY = _cfg.CreateEntry("FlipY", default_value: true, null, "Flip the eye textures vertically on submit. Needed because assigning a custom projection matrix skips Unity's render-to-texture Y flip on D3D11.");
		_pPositional = _cfg.CreateEntry("PositionalTracking", default_value: true, null, "Let leaning move the view. Turn off if it feels wrong.");
		_pIpdScale = _cfg.CreateEntry("IpdScale", 1f, null, "Multiplier on eye separation. 1.0 is correct; lower makes the world feel bigger.");
		_pEyeTexMode = _cfg.CreateEntry("EyeTextureMode", 2, null, "How the eye texture is created. 0 = project default, 1 = force non-sRGB, 2 = force sRGB. The game renders in Gamma colour space, so if the texture's sRGB flag disagrees with what SteamVR is told, the whole image lands on the wrong gamma curve - washed out in bright scenes, pitch black in the forest.");
		_pColorSpace = _cfg.CreateEntry("SubmitColorSpace", 0, null, "How SteamVR should read the eye textures. 0 = Auto, 1 = Gamma, 2 = Linear. Try 1 then 2 if the image looks washed out or too dark.");
		_pVRInput = _cfg.CreateEntry("VRControllerInput", default_value: true, null, "Feed the Quest thumbsticks and buttons into the game through Rewired.");
		_pBlockLookY = _cfg.CreateEntry("BlockStickPitch", default_value: true, null, "Ignore up/down on the look stick. Pitch comes from your head; doing both at once is what makes people ill.");
		_pActMoveX = _cfg.CreateEntry("ActionMoveX", "", null, "Rewired action driven by left stick X. Take the name from the '[rewired] game polls' lines in the log.");
		_pActMoveY = _cfg.CreateEntry("ActionMoveY", "");
		_pActLookX = _cfg.CreateEntry("ActionLookX", "");
		_pActLookY = _cfg.CreateEntry("ActionLookY", "");
		_pActInteract = _cfg.CreateEntry("ActionInteract", "");
		_pActCancel = _cfg.CreateEntry("ActionCancel", "");
		_pActMenu = _cfg.CreateEntry("ActionMenu", "");
		_pNavUp = _cfg.CreateEntry("ActionNavUp", "up", null, "Menu navigation from the right stick. These are the game's own gamepad actions, taken from the '[rewired] game polls' lines in the log.");
		_pNavDown = _cfg.CreateEntry("ActionNavDown", "down");
		_pNavLeft = _cfg.CreateEntry("ActionNavLeft", "left");
		_pNavRight = _cfg.CreateEntry("ActionNavRight", "right");
		_pSelect = _cfg.CreateEntry("ActionSelect", "enter", null, "What the A button presses in menus.");
		_pHandsInHouseOnly = _cfg.CreateEntry("HandsOnlyInHouse", default_value: true, null, "Keep the arms, the beam and the pointer out of the menus. They belong in the house; a menu is navigated with the stick and A.");
		_pDriveCursor = _cfg.CreateEntry("HandDrivesCursor", default_value: true, null, "Move the mouse pointer to wherever your right hand is pointing. The game decides what you are selecting from the CURSOR, not from the camera, so without this the trigger clicks whatever the mouse was last left on.");
		_pModelArms = _cfg.CreateEntry("UseModelArms", default_value: true, null, "Use the rigged arm model from UserData/arms.bpvr. Off falls back to the old capsule arms.");
		_pHandPitch = _cfg.CreateEntry("HandPitch", 90f, null, "Rotation from the controller to the hand bone. Adjust if the hands sit at the wrong angle.");
		_pHandYaw = _cfg.CreateEntry("HandYaw", 0f);
		_pUIInVR = _cfg.CreateEntry("ShowUIInVR", default_value: true, null, "Move the game's screen-overlay canvases onto the world camera. Overlay canvases draw straight to the monitor and never to a render texture, which is why the room draft cards were missing in the headset.");
		_pUIDistance = _cfg.CreateEntry("UIDistance", 1.2f, null, "How far in front of you the UI sits, in metres.");
		_pShowCursor = _cfg.CreateEntry("ShowCursorMarker", default_value: true, null, "Draw a dot in the world where the mouse pointer is. The real pointer is drawn by Windows on the monitor and is not part of the headset image, so without this there is nothing in VR showing what you are about to click.");
		_pCursorDistance = _cfg.CreateEntry("CursorDistance", 1.5f, null, "How far away the cursor dot floats, in metres.");
		_pAimPitch = _cfg.CreateEntry("AimPitch", 30f, null, "Tilt of the aim ray, in degrees. POSITIVE aims higher, negative lower. Quest controllers sit well below the direction you feel you are pointing, hence the default. Nudge it until the beam lands where you mean it to.");
		_pCursorSmooth = _cfg.CreateEntry("CursorSmoothing", 0.55f, null, "How much the pointer is steadied, 0 to 1. Higher is calmer but lags more. Small items need a steady pointer.");
		_pSteerView = _cfg.CreateEntry("HandAimSteersView", default_value: false, null, "Off: nothing ever moves the view on its own - aim the dot with the sticks and your head, the way Alex asked. On: holding the trigger swings the view onto whatever the hand points at, then clicks, which is the only way the hand itself can choose what gets picked.");
		_pSteerYawOnly = _cfg.CreateEntry("SteerYawOnly", default_value: true, null, "Only turn left and right when the trigger drags the view, never up or down. Vertical is what causes motion sickness, and your head already handles looking up and down.");
		_pSteerMaxDeg = _cfg.CreateEntry("SteerMaxDegreesPerSecond", 90f, null, "Ceiling on how fast the drag may swing the view. Degrees per second, because that is what your inner ear measures.");
		_pSteerSpeed = _cfg.CreateEntry("SteerSpeed", 0.22f, null, "How fast the view swings to the hand's target, 0.05 to 1. Lower is gentler.");
		_pUIPanel = _cfg.CreateEntry("UIPanel", default_value: true, null, "Hang the game's UI on a screen standing in the room instead of painting it into both eyes. A flat image drawn into both eyes has no depth anywhere in it, which is why the cards would not line up; a panel is an object, so it gets real stereo.");
		_pUIPanelWidth = _cfg.CreateEntry("UIPanelWidth", 1.1f, null, "Width of that screen as a multiple of its distance. 1.1 is roughly a cinema screen; lower is smaller.");
		_pUIScale = _cfg.CreateEntry("UIScale", 1f, null, "How much of your view the game's overlays fill. Below 1 widens their frustum, which also brings their own black backdrop into view as boxes at the edges - so 1 unless you are willing to trade that.");
		_pUITint = _cfg.CreateEntry("UIPanelBrightness", 0.82f, "How bright the interface panel is drawn. The eyes render ungraded while the flat game colour-grades everything, so the cards arrive hotter in VR than on a monitor. 1.0 is raw; lower takes the glare off.");
		_pCrouch = _cfg.CreateEntry("CrouchDrop", 0.55f, "Crouch height in metres. Click the turn-hand stick once to crouch, again to stand.");
		_pHudDirect = _cfg.CreateEntry("HudDirect", default_value: true, "Draw the game's own HUD camera straight into the eye texture. This is the only way the key, gem, coin and step ICONS can appear - copying them through an offscreen texture drops every part that needs its own material, which is why the strip has only ever shown bare numbers.");
		_pHeadUnstick = _cfg.CreateEntry("HeadUnstick", default_value: false, "Step the view backwards when it ends up inside geometry, for scripted camera spots like the lab terminal. OFF by default: it also detects the player's own body collider, so it fires constantly, and it moves the eye without moving the shoulders - which leaves the arms floating out ahead of you.");
		_pHudDirectScale = _cfg.CreateEntry("HudDirectScale", 0.55f, "How much to shrink the game's overlay camera before drawing it into the eye, as a fraction of the width. It was built for a 60-degree monitor; drawn raw into a 100-degree headset its image lands in your face and its edges fall outside your vision. This shrinks the VIEWPORT, not the frustum - shrinking the frustum is what turned the whole view boxy in 1.92, because it brought the HUD's own black backdrop into frame.");
		_pHudDirectParallax = _cfg.CreateEntry("HudDirectParallax", 0.014f, "Sideways shift per eye for the HUD panel, in screen fractions. Drawn identically to both eyes the panel sits at infinity and will not fuse against a room two metres away - that is the double vision on the lab terminal. This gives it a distance. Zero for a flat HUD.");
		_pShowBeam = _cfg.CreateEntry("ShowAimBeam", default_value: false, "Draw the aiming beam out of each hand. Off by default: nothing in this game glows, and two blue lasers are the one thing on screen that reads as a VR tech demo rather than as Blue Prince. The beam still exists and still aims with it hidden - the raycast never looked at the drawn object - so turn it on when something needs debugging.");
		_pUIParallax = _cfg.CreateEntry("UIParallax", 0f, null, "Sideways shift per eye for the overlays. Anything above zero splits full-screen art such as the loading screen, because a picture that fills the view has no depth to give it. Zero keeps overlays flat, the way a HUD normally sits.");
		_pCameraCensus = _cfg.CreateEntry("LogCameras", default_value: true, null, "Every ten seconds, list the scene's cameras in the log. This is how the camera that draws the room cards gets identified rather than guessed at.");
		_pProjectionMode = _cfg.CreateEntry("ProjectionMode", 0, null, "How the eye projection is built. 0 = take the frustum out of OpenVR's own matrix and rebuild it with Unity's depth convention: correct colour and correct shape. 1 = build from OpenVR's raw tangents. 2 = OpenVR's matrix as-is, which writes the wrong depth and hazes everything.");
		_pSwapArmSides = _cfg.CreateEntry("SwapArmSides", default_value: false, null, "Which bone chain each controller drives. The rig's .L/.R names do not match the sides the bones sit on, so this decides it by result rather than by name.");
		_pShoulderDrop = _cfg.CreateEntry("ShoulderDrop", 0.08f, null, "Extra metres to drop the shoulders below your eyes. Raise it if the arms sit too high on you.");
		_pArmScale = _cfg.CreateEntry("ArmScale", 0.82f, null, "Overall size of the arms. The character is a boy, so under 1.");
		_pArmsUnlit = _cfg.CreateEntry("ArmsUnlit", default_value: true, null, "Draw the arms with an unlit shader so the cel-shaded texture shows as painted. Off lets the scene lighting shade them, which smooths the flat bands back into a gradient.");
		_pJointThickness = _cfg.CreateEntry("JointThickness", 1.05f, null, "Widens the forearm and wrist across their own axis. 1.0 is the model as authored; higher gives a chunkier wrist and elbow.");
		_pArmBrightness = _cfg.CreateEntry("ArmBrightness", 0.78f, null, "Dims the arm texture at load. The arms are unlit, so nothing in the room darkens them and a pale sleeve reads as glowing. Lower is darker.");
		_pAdoptCanvases = _cfg.CreateEntry("AdoptCameraCanvases", default_value: false, null, "Repoint UI that draws through its own private camera into the headset. Suspected of freezing the game in shops - off until that is proven either way.");
		_pAimRadius = _cfg.CreateEntry("AimRadius", 0.1f, null, "How wide the aiming beam is, in metres. The game's own hit boxes are very small; raise this if items are still hard to click.");
		_pHudFull = _cfg.CreateEntry("HudFullScreen", default_value: true, null, "Show the game's whole interface as a transparent overlay - counters, prompts, the USE KEY option and the cursor icon. Off shows only the two counter clusters.");
		_pMoveOsPointer = _cfg.CreateEntry("MoveWindowsPointer", default_value: false, null, "Also drag the real Windows pointer to match the beam. Off by default - the game already reads the beam directly, and moving the pointer was jumping the selection to another card.");
		_pUiWorld = _cfg.CreateEntry("UIInWorld", default_value: true, null, "Draw the game's interface as real geometry in the room instead of copying it into a texture. Keeps the icons and filled text that copying was losing.");
		_pUiWorldWidth = _cfg.CreateEntry("UIWorldWidth", 2.2f, null, "How wide the interface is, in metres.");
		_pUiWorldDist = _cfg.CreateEntry("UIWorldDistance", 1.7f, null, "How far in front of you the interface sits, in metres.");
		_pUiWorldDrop = _cfg.CreateEntry("UIWorldDrop", 0.22f, null, "How far below straight ahead the interface sits. Raise it to bring the HUD lower.");
		_pHudFullWidth = _cfg.CreateEntry("HudFullWidth", 1.8f, null, "How wide the full-screen interface overlay is, in metres.");
		_pHudFullDrop = _cfg.CreateEntry("HudFullDrop", 0.1f, null, "How far below straight ahead the interface overlay sits. Raise it to bring the HUD down further.");
		_pHudFullDist = _cfg.CreateEntry("HudFullDistance", 1.6f, null, "How far in front of you the full-screen interface overlay sits.");
		_pHudStrip = _cfg.CreateEntry("GameHud", default_value: true, null, "Show the game's own HUD strip - steps, dice, keys, gems, coins - floating in front of you.");
		_pHudDistance = _cfg.CreateEntry("HudDistance", 1.3f, null, "How far in front of your eyes the HUD strip sits, in metres.");
		_pHudWidth = _cfg.CreateEntry("HudWidth", 1.15f, null, "How far apart the two HUD clusters sit - steps on the left, resources on the right.");
		_pHudScale = _cfg.CreateEntry("HudScale", 2.4f, null, "How big the HUD icons are. Raise it if they are still small.");
		_pHudRise = _cfg.CreateEntry("HudRise", 0.3f, null, "How far above straight ahead the HUD strip sits. Lower it if you have to look up.");
		_pHudStripTop = _cfg.CreateEntry("HudStripHeight", 0.093f, null, "How much of the top of the screen the strip takes. Raise it if the icons are clipped.");
		_pSkipIntro = _cfg.CreateEntry("SkipIntro", default_value: true, null, "Run the opening sequence at speed instead of watching it every launch.");
		_pHideStepHud = _cfg.CreateEntry("HideStepHud", default_value: true, null, "Hide the game's own step counter once the watch is showing the number.");
		_pWatchSize = _cfg.CreateEntry("WatchSize", 1f, null, "How big the wrist watch is. Raise it if the number is hard to read in the headset.");
		_pWatch = _cfg.CreateEntry("StepWatch", default_value: true, null, "A wristwatch on the left hand showing the steps left in the day.");
		_pHandRoll = _cfg.CreateEntry("HandRoll", 0f);
		_pTriggerClicks = _cfg.CreateEntry("TriggerClicksMouse", default_value: true, null, "Send a real left mouse click when the right trigger is pulled. The menus are driven by Unity's event system, which only listens to the mouse button - without this the pointer moves over a menu entry and nothing happens.");
		_pNamedEvents = _cfg.CreateEntry("SendNamedFsmEvents", default_value: false, null, "Also fire an object's own named PlayMaker events, not just the mouse ones. Off by default: it forces the end of a sequence rather than the start, so a door opens onto a room that was never drafted.");
		_pDirectGrab = _cfg.CreateEntry("DirectGrab", default_value: true, null, "On the trigger, call the clicked object's own OnClick straight out, instead of going through the cursor, Rewired and the game's raycaster. Those are the layers that kept swallowing it.");
		_pUnlockToClick = _cfg.CreateEntry("UnlockPointerToClick", default_value: false, null, "Release the mouse pointer while the trigger is held. The game locks it during play and then picks whatever is dead ahead, ignoring the pointer entirely - which is why the beam can sit on a key and the click takes the table behind it.");
		_pTurnMode = _cfg.CreateEntry("TurnMode", 1, null, "0 = off, 1 = right stick moves the mouse (the game turns with the mouse).");
		_pTurnPixels = _cfg.CreateEntry("TurnPixelsPerSecond", 1400f, null, "Turn speed at full stick. Raise to turn faster.");
		_pLookSens = _cfg.CreateEntry("LookSensitivity", 1f, null, "Turn speed for the right stick.");
		_pLookDead = _cfg.CreateEntry("LookDeadzone", 0.15f, null, "Ignore small right-stick movement so the view does not creep.");
		_pLookSmooth = _cfg.CreateEntry("LookSmoothing", 0.35f, null, "0 = raw stick, higher = smoother ramp when you start and stop turning.");
		_pHands = _cfg.CreateEntry("ShowHands", default_value: true, null, "Draw tracked arms and hands at your Quest controllers, with an aim ray.");
		_pHandScale = _cfg.CreateEntry("HandScale", 1f, null, "Size of the arms. 1.0 is roughly life size.");
		_pEffects = _cfg.CreateEntry("CopyEffects", "FsGammaModScr1,Beautify", null, "Comma separated component names to clone from the game camera onto the eye cameras, with their tuned values. This is what puts the game's colour grading back into the headset. Clear it to render ungraded.");
		_pThroughGameCam = _cfg.CreateEntry("RenderThroughGameCamera", default_value: false, null, "Render each eye using the game's OWN camera instead of a copy, so its post-processing (Beautify, FsGammaModScr1 gamma grade, SSAO, bloom) actually runs on the headset image. Turn off to go back to plain clone cameras if this misbehaves.");
		_pEyeHDR = _cfg.CreateEntry("EyeCameraHDR", default_value: false, null, "Let the eye cameras render in HDR. Off by default: the game tonemaps HDR down to screen range in its post-processing stack, which our eye cameras do not have, so HDR values just blow out to white. That is the washed-out look.");
		_pForceClear = _cfg.CreateEntry("ForceOpaqueClear", default_value: true, null, "Always clear the eye textures to an opaque colour. The game switches its camera to Depth-only clear during drafting, which leaves the old frame behind - that is the card trails.");
		_pDiagLevel = _cfg.CreateEntry("DiagnosticLevel", 3, null, "Bisect switch. 0 = VR init only, nothing touched. 1 = eye cameras exist and are positioned but never render. 2 = eyes render to textures but nothing is sent to SteamVR (no picture in the headset - expected). 3 = full VR. Used to find which stage crashes.");
		_pUnlockFramerate = _cfg.CreateEntry("UnlockFramerate", default_value: true, null, "Turn off the game's vsync so the headset paces the frames instead of the monitor. Main cause of stutter if left on.");
		Log("=========================================");
		InitCodex();
		_pHaptics=_cfg.CreateEntry("InteractionHaptics",true,null,"Light target-ready and stronger accepted-input controller pulses. Set false to disable.");
		Log(" BluePrinceVR Codex 2.10.57 TEST - dev I/F shortcut compatibility");
		Log($" DiagnosticLevel = {_pDiagLevel.Value}  (3 = full VR)");
		Log("=========================================");
		VRInput.ActMoveX = _pActMoveX.Value ?? "";
		VRInput.ActMoveY = _pActMoveY.Value ?? "";
		VRInput.ActNavUp = _pNavUp.Value ?? "";
		VRInput.ActNavDown = _pNavDown.Value ?? "";
		VRInput.ActNavLeft = _pNavLeft.Value ?? "";
		VRInput.ActNavRight = _pNavRight.Value ?? "";
		VRInput.ActSelect = _pSelect.Value ?? "";
		VRInput.ActLookX = _pActLookX.Value ?? "";
		VRInput.ActLookY = _pActLookY.Value ?? "";
		VRInput.ActInteract = _pActInteract.Value ?? "";
		VRInput.ActCancel = _pActCancel.Value ?? "";
		VRInput.ActMenu = _pActMenu.Value ?? "";
		VRInput.BlockLookY = _pBlockLookY.Value;
		if (!_pEnabled.Value)
		{
			Log("Disabled in preferences.");
		}
		else
		{
			InitVR();
		}
	}

	private void InitVR()
	{
		try
		{
			if (!OpenVRNative.VR_IsRuntimeInstalled())
			{
				Fail("SteamVR runtime is not installed.");
				return;
			}
			if (!OpenVRNative.VR_IsHmdPresent())
			{
				Fail("No headset detected. Start SteamVR and make sure the headset is tracking before launching.");
				return;
			}
			int err = 0;
			OpenVRNative.VR_InitInternal(ref err, EVRApplicationType.Scene);
			if (err != 0)
			{
				Fail($"VR_InitInternal failed, error {err}");
				return;
			}
			_sys = OpenVRNative.GetTable<IVRSystem_FnTable>("IVRSystem_026", out var error);
			_comp = OpenVRNative.GetTable<IVRCompositor_FnTable>("IVRCompositor_029", out var error2);
			if (error != null || error2 != null)
			{
				Fail("interface lookup failed: " + error + " " + error2);
				OpenVRNative.VR_ShutdownInternal();
				return;
			}
			_sys.GetRecommendedRenderTargetSize(ref _eyeW, ref _eyeH);
			float num = Mathf.Clamp(_pResScale.Value, 0.3f, 2f);
			_eyeW = (uint)((float)_eyeW * num);
			_eyeH = (uint)((float)_eyeH * num);
			if (_eyeW < 256 || _eyeH < 256)
			{
				_eyeW = 1280u;
				_eyeH = 1440u;
			}
			for (int i = 0; i < 2; i++)
			{
				EVREye eye = (EVREye)i;
				_eyeProj[i] = ToUnity(_sys.GetProjectionMatrix(eye, 0.05f, 870f));
				HmdMatrix34_t hmdMatrix34_t = _sys.GetEyeToHeadTransform(eye);
				_eyeOffset[i] = new Vector3(hmdMatrix34_t.m3, hmdMatrix34_t.m7, 0f - hmdMatrix34_t.m11);
			}
			_comp.SetTrackingSpace(ETrackingUniverseOrigin.Seated);
			Log($"OpenVR up. Per-eye render target {_eyeW}x{_eyeH} (scale {num:F2}).");
			Log($"Eye offsets L={_eyeOffset[0]} R={_eyeOffset[1]} (metres)");
			Log($"Unity colour space: {QualitySettings.activeColorSpace}  (submit mode {_pColorSpace.Value})");
			_vrReady = true;
		}
		catch (System.DllNotFoundException)
		{
			Fail("openvr_api.dll not found next to BLUE PRINCE.exe.");
		}
		catch (System.Exception ex2)
		{
			Fail(ex2.GetType().Name + ": " + ex2.Message);
		}
	}

	private void ApplyFramePacing()
	{
		if (!_vrReady && !_pEnabled.Value)
		{
			return;
		}
		try
		{
			if (_pUnlockFramerate == null || !_pUnlockFramerate.Value)
			{
				return;
			}
			int vSyncCount = QualitySettings.vSyncCount;
			int targetFrameRate = Application.targetFrameRate;
			if (vSyncCount != 0 || targetFrameRate != -1)
			{
				QualitySettings.vSyncCount = 0;
				Application.targetFrameRate = -1;
				_pacingResets++;
				if (_pacingResets <= 5 || _pacingResets % 100 == 0)
				{
					Log($"frame pacing reset #{_pacingResets} (game had vsync={vSyncCount} targetFps={targetFrameRate})");
				}
			}
		}
		catch (System.Exception ex)
		{
			LogWarn("frame pacing: " + ex.Message);
		}
	}

	private void Fail(string why)
	{
		_initError = why;
		_vrReady = false;
		LogWarn("VR not starting: " + why);
		LogWarn("The game will run completely normally on the monitor.");
	}

	public override void OnUpdate()
	{
		try { UpdateDevShortcuts(); }
		catch(System.Exception ex) { if(_devShortcutLogs++<20)LogWarn("[dev shortcuts] "+ex.Message); }
		if (_codexBootScene) return;
		if (!_vrReady || !_pVRInput.Value)
		{
			return;
		}
		if (VRInput.InteractDownFrames > 0)
		{
			VRInput.InteractDownFrames--;
		}
		Grab.Tick();
		ParlorLegacyGrab.TickPending();
		if(!_comfortOpen)Snapshot.Tick(_leftGrip, _rightGrip, Time.deltaTime);
		SaveNow.Tick(VRInput.Menu, VRInput.Cancel, Time.deltaTime);
		Intro.Enabled = _pSkipIntro.Value;
		Intro.Tick();
		Steps.HideHud = false; // Wrist display samples the original HUD; keep its source rendered.
		Steps.Tick();
		if (Watch.Exists)
		{
			Watch.Set(Steps.Value);
			Watch.FaceViewer(HandRig.HeadPos);
		}
		try
		{
			PollControllers();
			VRInput.NativeKeyboard=false;
			PollFloorPlanButton();
			_crouchHeld=_crouchToggle.Update(_crouchPressed,
				_comfortOpen||_comfortChord||VRInput.Stroll||_journalOpen||_journalBlockUntilRelease||_computerKeyboardMode||NativePanelMenu,
				!_inHouse||!_crouchControllerValid||!Application.isFocused);
			MainMenuVRSettings();
			if(ComfortInput()){if(_quickMemo)CloseQuickMemo();BlockJournalInput();return;}
			JournalInput();
			if (_journalOpen || _journalBlockUntilRelease) return;
			UpdateComputerMode();
			UpdatePanelStickPointer();
			UpdateComputerKeys();
			DriveTurnFromStick();
		}
		catch (System.Exception ex)
		{
			if (_inputErr++ < 3)
			{
				LogWarn("controller poll: " + ex.Message);
			}
		}
	}

	[DllImport("user32.dll")]
	private static extern void mouse_event(uint flags, int dx, int dy, uint data, System.IntPtr extra);

	private void DriveTurnFromStick()
	{
		if(_comfort.Snap) {
			float x=VRInput.LookX;
			if(Mathf.Abs(x)<.2f)_snapLatched=false;
			if(!_snapLatched&&Mathf.Abs(x)>.65f&&_inHouse&&!TurnInputBlocked){
				_comfortYaw+=Mathf.Sign(x)*Mathf.Clamp(_comfort.SnapAngle,30f,60f);_snapLatched=true;
			}
			VRInput.LookX=0;_turnCarry=0;_turning=false;return;
		}
		if (_pTurnMode.Value != 1)
		{
			_turning = false;
			return;
		}
		if (TurnInputBlocked)
		{
			_turning = false;
			return;
		}
		float lookX = VRInput.LookX;
		if (Mathf.Abs(lookX) < 0.02f)
		{
			_turnCarry = 0f;
			_turning = false;
			return;
		}
		_turning = true;
		float num = lookX * Mathf.Max(1f, _pTurnPixels.Value) * Time.unscaledDeltaTime + _turnCarry;
		int num2 = (int)num;
		_turnCarry = num - (float)num2;
		if (num2 == 0)
		{
			return;
		}
		try
		{
			mouse_event(1u, num2, 0, 0u, System.IntPtr.Zero);
		}
		catch (System.Exception ex)
		{
			if (_turnLog < 3)
			{
				_turnLog++;
				LogWarn("mouse turn: " + ex.Message);
			}
		}
		if (_turnLog < 6 && _inputFrames % 60 == 1)
		{
			_turnLog++;
			Log($"[turn] stick={lookX:F2} -> {num2}px  cursorLock={Cursor.lockState} visible={Cursor.visible}");
		}
	}

	private void PollControllers()
	{
		_panelAdjusting = false;
		_leftPanelStick = Vector2.zero;
		_navX = _navY = 0f;
		VRInput.MoveX = VRInput.MoveY = VRInput.LookX = VRInput.LookY = 0f;
		VRInput.Interact = VRInput.Select = VRInput.Cancel = VRInput.Menu = false;
		VRInput.UseItem=VRInput.UseItemEdge=VRInput.Stroll=VRInput.StrollEdge=false;
		VRInput.CancelEdge=EdgeThisFrame=false;
		_crouchPressed = false;_crouchControllerValid=false;
		uint num = uint.MaxValue;
		uint num2 = uint.MaxValue;
		try
		{
			num = ControllerIndex(ETrackedControllerRole.LeftHand);
		}
		catch
		{
		}
		try
		{
			num2 = ControllerIndex(ETrackedControllerRole.RightHand);
		}
		catch
		{
		}
		uint stateSize = (uint)Marshal.SizeOf<VRControllerState_t>();
		bool flag = false;
		if (num != uint.MaxValue && num < 64)
		{
			VRControllerState_t state = default(VRControllerState_t);
			if (ReadControllerState(num, ref state, stateSize))
			{
				flag = true;
				VRInput.MoveX = Dead(state.rAxis0.x, 0.12f);
				VRInput.MoveY = Dead(state.rAxis0.y, 0.12f);
				_leftPanelStick = new Vector2(state.rAxis0.x, state.rAxis0.y);
				bool flag2 = (state.ulButtonPressed & 2) != 0;
				VRInput.MenuEdge = false;
				VRInput.Menu = false;
				_prevMenuBtn = flag2;
				bool flag3 = ((state.ulButtonPressed & 0x200000000L) != 0L || state.rAxis1.x > 0.6f) && (state.ulButtonPressed & 4UL) != 0;
				VRInput.UseItemEdge = flag3 && !_prevUseItem;
				VRInput.UseItem = flag3;
				_prevUseItem = flag3;
				bool flag4 = (state.ulButtonPressed & 0x100000000L) != 0;
				VRInput.StrollEdge = flag4 && !_prevStroll;
				VRInput.Stroll = flag4;
				_prevStroll = flag4;
			}
		}
		if (num2 != uint.MaxValue && num2 < 64)
		{
			VRControllerState_t state2 = default(VRControllerState_t);
			if (ReadControllerState(num2, ref state2, stateSize))
			{
				flag = true;
				float x = state2.rAxis0.x;
				float num3 = Mathf.Clamp(_pLookDead.Value, 0f, 0.9f);
				float num4 = Mathf.Abs(x);
				float b = ((num4 <= num3) ? 0f : (Mathf.Sign(x) * ((num4 - num3) / (1f - num3))));
				float num5 = Mathf.Clamp01(_pLookSmooth.Value);
				float t = ((num5 <= 0f) ? 1f : (1f - Mathf.Exp((0f - Time.unscaledDeltaTime) / Mathf.Max(0.001f, num5 * 0.25f))));
				_lookXSmoothed = Mathf.Lerp(_lookXSmoothed, b, t);
				if (VRInput.MenuMode)
				{
					VRInput.LookX = Dead(state2.rAxis0.x, 0.12f);
					VRInput.LookY = Dead(state2.rAxis0.y, 0.12f);
					VRInput.BlockLookY = false;
					_lookXSmoothed = 0f;
				}
				else
				{
					VRInput.LookX = _lookXSmoothed * Mathf.Clamp(_pLookSens.Value, 0.05f, 5f);
					VRInput.LookY = Dead(state2.rAxis0.y, 0.12f);
					VRInput.BlockLookY = _pBlockLookY.Value;
				}
				_navX = state2.rAxis0.x;
				_navY = state2.rAxis0.y;
				if (_pPanelAdjust.Value && (_computerKeyboardMode || NativePanelMenu) && (state2.ulButtonPressed & 4UL) != 0)
				{
					_panelAdjusting = true;
					if (_computerKeyboardMode)
						_computerViewingDistance = Mathf.Clamp(_computerViewingDistance + Dead(state2.rAxis0.y, 0.2f) * .4f * Mathf.Min(Time.unscaledDeltaTime, .05f), .55f, 1.2f);
					else
						_panelDistance = Mathf.Clamp(_panelDistance + Dead(state2.rAxis0.y, 0.2f) * 0.65f * Mathf.Min(Time.unscaledDeltaTime, 0.05f), 0.35f, 2.5f);
					_navX = _navY = VRInput.LookX = VRInput.LookY = 0f;
					VRInput.MoveX = VRInput.MoveY = 0f;
				}
				VRInput.Select = (state2.ulButtonPressed & 0x80) != 0;
				_crouchControllerValid=true;
				_crouchPressed = (state2.ulButtonPressed & 0x100000000L) != 0;
				// Tracked-hand clicks are dispatched once in PoseHand/ClickFromTrigger.
				// Do not also expose the same trigger as a Rewired interaction.
				bool handClickRoute = _pHands.Value && _pTriggerClicks.Value && (!_pHandsInHouseOnly.Value || _inHouse);
				VRInput.Interact = VRInput.Select || (!handClickRoute &&
					((state2.ulButtonPressed & 0x200000000L) != 0L || state2.rAxis1.x > 0.6f));
				bool flag5 = (state2.ulButtonPressed & 2) != 0;
				VRInput.CancelEdge = flag5 && !_prevCancel;
				VRInput.Cancel = flag5;
				_prevCancel = flag5;
				bool interact = VRInput.Interact;
				EdgeThisFrame = interact && !_prevInteract;
				if (EdgeThisFrame)
				{
					VRInput.InteractDownFrames = 8;
				}
				_prevInteract = interact;
			}
		}
		if (_mainMenuScene && _leftPanelStick.sqrMagnitude > 0.04f)
		{
			_navX = _leftPanelStick.x;
			_navY = _leftPanelStick.y;
		}
		if(_inHouse) {
			var headForward=_headRot*Vector3.forward;
			HeadRelativeMove.Rotate(VRInput.MoveX,VRInput.MoveY,headForward.x,headForward.z,_comfortYaw,
				ref _movementHeadYaw,out VRInput.MoveX,out VRInput.MoveY);
		}
		bool flag6 = _navY > 0.6f;
		bool flag7 = _navY < -0.6f;
		bool flag8 = _navX < -0.6f;
		bool flag9 = _navX > 0.6f;
		if (Mathf.Abs(_navY) < 0.35f)
		{
			_navUpHeld = (_navDownHeld = false);
		}
		if (Mathf.Abs(_navX) < 0.35f)
		{
			_navLeftHeld = (_navRightHeld = false);
		}
		VRInput.NavUpEdge = flag6 && !_navUpHeld;
		VRInput.NavDownEdge = flag7 && !_navDownHeld;
		VRInput.NavLeftEdge = flag8 && !_navLeftHeld;
		VRInput.NavRightEdge = flag9 && !_navRightHeld;
		if (flag6)
		{
			_navUpHeld = true;
		}
		if (flag7)
		{
			_navDownHeld = true;
		}
		if (flag8)
		{
			_navLeftHeld = true;
		}
		if (flag9)
		{
			_navRightHeld = true;
		}
		VRInput.NavUp = flag6;
		VRInput.NavDown = flag7;
		VRInput.NavLeft = flag8;
		VRInput.NavRight = flag9;
		VRInput.SelectEdge = VRInput.Select && !_selectHeld;
		_selectHeld = VRInput.Select;
		VRInput.Enabled = flag;
		if (flag && ++_inputFrames % 120 == 1)
		{
			Log($"[input] move=({VRInput.MoveX:F2},{VRInput.MoveY:F2}) lookX={VRInput.LookX:F2} interact={VRInput.Interact} edge={EdgeThisFrame} matched ax={VRInput.AxisHits} btn={VRInput.ButtonHits}");
		}
	}

	[DllImport("user32.dll")]
	private static extern bool SetCursorPos(int x, int y);

	[DllImport("user32.dll")]
	private static extern bool GetCursorPos(out POINT p);

	[DllImport("user32.dll")]
	private static extern System.IntPtr GetForegroundWindow();

	[DllImport("user32.dll")]
	private static extern System.IntPtr GetActiveWindow();

	[DllImport("user32.dll")]
	private static extern bool ClientToScreen(System.IntPtr hWnd, ref POINT p);

	private void ClientToDesktop(ref int x, ref int y)
	{
		try
		{
			System.IntPtr activeWindow = GetActiveWindow();
			if (activeWindow == System.IntPtr.Zero)
			{
				return;
			}
			POINT p = new POINT
			{
				x = x,
				y = y
			};
			if (ClientToScreen(activeWindow, ref p))
			{
				if (!_windowLogged)
				{
					_windowLogged = true;
					Log($"[pointer] window offset ({p.x - x},{p.y - y}); game area {Screen.width}x{Screen.height}");
				}
				x = p.x;
				y = p.y;
			}
		}
		catch
		{
		}
	}

	[DllImport("user32.dll")]
	private static extern int GetSystemMetrics(int index);

	private void SendAbsolute(int nx, int ny)
	{
		mouse_event(32769u, Mathf.Clamp(nx, 0, 65535), Mathf.Clamp(ny, 0, 65535), 0u, System.IntPtr.Zero);
	}

	private bool CalibratePointer()
	{
		if (_ptrCalWait > 0)
		{
			_ptrCalWait--;
			return false;
		}
		VRAim.Bypass = true;
		Vector3 mousePosition = Input.mousePosition;
		VRAim.Bypass = false;
		if (_ptrCalStage == 0)
		{
			SendAbsolute(16000, 16000);
			_ptrCalStage = 1;
			_ptrCalWait = 2;
			return false;
		}
		if (_ptrCalStage == 1)
		{
			_ptrSample1 = new Vector2(mousePosition.x, mousePosition.y);
			SendAbsolute(48000, 48000);
			_ptrCalStage = 2;
			_ptrCalWait = 2;
			return false;
		}
		float num = 32000f;
		float num2 = mousePosition.x - _ptrSample1.x;
		float num3 = mousePosition.y - _ptrSample1.y;
		if (Mathf.Abs(num2) < 1f || Mathf.Abs(num3) < 1f)
		{
			_ptrCalStage = 0;
			if (_ptrLog < 6)
			{
				_ptrLog++;
				LogWarn("[pointer] calibration saw no movement; retrying");
			}
			return false;
		}
		_kx = num2 / num;
		_cx = _ptrSample1.x - _kx * 16000f;
		_ky = num3 / num;
		_cy = _ptrSample1.y - _ky * 16000f;
		_ptrCal = true;
		Log($"[pointer] measured mapping: client_x = {_kx:F5}*n + {_cx:F1}, client_y = {_ky:F5}*n + {_cy:F1}  (game area {Screen.width}x{Screen.height})");
		return true;
	}

	private void MovePointerTo(int clientX, int clientYTopDown)
	{
		try
		{
			if (!_ptrCal)
			{
				CalibratePointer();
				return;
			}
			float num = clientX;
			float num2 = Screen.height - clientYTopDown;
			VRAim.Bypass = true;
			Vector3 mousePosition = Input.mousePosition;
			VRAim.Bypass = false;
			float f = num - mousePosition.x;
			float f2 = num2 - mousePosition.y;
			if (!(Mathf.Abs(f) < 1.5f) || !(Mathf.Abs(f2) < 1.5f))
			{
				int num3 = Mathf.RoundToInt((num - _cx) / _kx);
				int num4 = Mathf.RoundToInt((num2 - _cy) / _ky);
				SendAbsolute(num3, num4);
				if (_ptrLog < 20 && _inputFrames % 30 == 1)
				{
					_ptrLog++;
					Log($"[pointer] game sees ({mousePosition.x:F0},{mousePosition.y:F0}), want ({num:F0},{num2:F0}) -> absolute ({num3},{num4})");
				}
			}
		}
		catch (System.Exception ex)
		{
			if (_ptrLog++ < 3)
			{
				LogWarn("pointer move: " + ex.Message);
			}
		}
	}

	private void DriveCursorFromHand(Camera gameCam, bool haveHit, Vector3 hitPoint, Hand source=null)
	{
		if (_computerKeyboardMode) { VRAim.Valid=false; return; }
		if (_draftMenu || NativePanelMenu) { VRAim.Valid=false; return; }
		if (_panelStick.Active) { PublishPanelStickPointer(); return; }
		if (_turning || _clickFreeze || !_pDriveCursor.Value || gameCam == null)
		{
			return;
		}
		Hand handR = source??_handR;
		if (handR == null || !handR.Valid)
		{
			return;
		}
		if (_uiQuad != null && _uiQuad.activeInHierarchy && !ScreenPointFromPanel(handR, out _))
		{
			VRAim.Valid = false;
			return;
		}
		bool num = Cursor.lockState != CursorLockMode.None;
		Vector3 vector = (_handTarget = (haveHit ? hitPoint : (handR.AimOrigin + handR.AimDirection * 3f)));
		_handTargetValid = true;
		_handTargetIsHit = haveHit;
		Vector3 vector2 = vector;
		float num2 = Vector3.Distance(handR.AimOrigin, vector);
		if (ScreenPointFromPanel(handR, out var screenPoint))
		{
			num2 = screenPoint.z;
			vector2 = handR.AimOrigin + handR.AimDirection * num2;
		}
		// The marker is placed from the actual short ray hit in PoseHand.
		if (num && haveHit)
		{
			// Publish a stable hand target for native game interaction without
			// moving the OS cursor (which would rotate the locked camera).
			Vector3 projected = gameCam.WorldToScreenPoint(hitPoint);
			VRAim.Valid = projected.z > 0f;
			if (VRAim.Valid) VRAim.ScreenPoint = new Vector2(projected.x, projected.y);
			return;
		}
		_lockNoted = false;
		if (num) { VRAim.Valid = false; return; }
		Vector3 screen = Vector3.zero;
		bool flag = ScreenPointFromPanel(handR, out screen);
		if (!flag)
		{
			flag = UiWorld.Any && UiWorld.ScreenPoint(handR.AimOrigin, handR.AimDirection, out screen);
		}
		if (!flag && gameCam != null)
		{
			screen = (_handTargetIsHit ? gameCam.WorldToScreenPoint(vector) : gameCam.WorldToScreenPoint(gameCam.transform.position + handR.AimDirection * 10f));
			screen.x = Mathf.Clamp(screen.x, 1f, (float)Screen.width - 2f);
			screen.y = Mathf.Clamp(screen.y, 1f, (float)Screen.height - 2f);
			if (screen.z <= 0f)
			{
				screen.z = 1f;
			}
		}
		if (screen.z > 0f)
		{
			VRAim.ScreenPoint = new Vector2(screen.x, screen.y);
			VRAim.Valid = true;
		}
		if ((!flag && _uiQuad != null && _uiQuad.activeInHierarchy) || screen.z <= 0f || screen.x < 0f || screen.y < 0f || screen.x > (float)Screen.width || screen.y > (float)Screen.height)
		{
			return;
		}
		float t = Mathf.Clamp01(_pCursorSmooth.Value);
		if (_cursorHave)
		{
			_cursorSx = Mathf.Lerp(screen.x, _cursorSx, t);
			_cursorSy = Mathf.Lerp(screen.y, _cursorSy, t);
		}
		else
		{
			_cursorSx = screen.x;
			_cursorSy = screen.y;
			_cursorHave = true;
		}
		int num3 = Mathf.RoundToInt(_cursorSx);
		int num4 = Mathf.RoundToInt((float)Screen.height - _cursorSy);
		if (_pMoveOsPointer.Value)
		{
			MovePointerTo(num3, num4);
		}
		if (_aimLog >= 40 || _inputFrames % 120 != 1)
		{
			return;
		}
		_aimLog++;
		string text = "nothing";
		string value = "nothing";
		float num5 = -1f;
		try
		{
			if (Physics.Raycast(handR.AimOrigin, handR.AimDirection, out var hitInfo, 25f))
			{
				text = hitInfo.collider.name;
			}
			Il2CppStructArray<RaycastHit> il2CppStructArray = Physics.SphereCastAll(handR.AimOrigin, 0.25f, handR.AimDirection, 6f);
			float num6 = float.MaxValue;
			int num7 = 0;
			while (il2CppStructArray != null && num7 < il2CppStructArray.Length)
			{
				Collider collider = il2CppStructArray[num7].collider;
				if (!(collider == null) && !(collider.name == text))
				{
					Vector3 vector3 = collider.bounds.center - handR.AimOrigin;
					float num8 = Vector3.Dot(vector3, handR.AimDirection);
					if (!(num8 <= 0f))
					{
						float magnitude = (vector3 - handR.AimDirection * num8).magnitude;
						if (magnitude < num6)
						{
							num6 = magnitude;
							value = collider.name;
						}
					}
				}
				num7++;
			}
			if (num6 < float.MaxValue)
			{
				num5 = num6;
			}
		}
		catch
		{
		}
		Log($"[cursor] screen ({num3},{num4}) on '{text}'" + ((num5 >= 0f) ? $", nearest miss '{value}' {num5 * 100f:F0}cm off axis" : "") + $" lock={Cursor.lockState} visible={Cursor.visible} " + $"screen={Screen.width}x{Screen.height}");
	}

	private void PlaceCursorAt(Vector3 world, float dist, bool grabbable)
	{
		if (!_pShowCursor.Value)
		{
			return;
		}
		try
		{
			EnsureCursorMarker();
			if (!(_cursorMarker == null))
			{
				_cursorMarker.transform.position = world;
				float num = Mathf.Clamp(dist * 0.01f, 0.007f, 0.014f);
				if (PalmPickupAllowed && _handR != null && IsPickupTarget(_handR.AimCollider)) num=.045f;
				_cursorMarker.transform.localScale = new Vector3(num, num, num);
				if (_cursorRenderer != null && _cursorRenderer.material != null)
				{
					_cursorRenderer.material.color = (grabbable ? new Color(1f, 0.82f, 0.28f) : new Color(0.95f, 0.95f, 1f));
				}
				if (!_cursorMarker.activeSelf)
				{
					_cursorMarker.SetActive(value: true);
				}
			}
		}
		catch (System.Exception ex)
		{
			if (_cursorLog < 4)
			{
				_cursorLog++;
				LogWarn("[cursor] marker: " + ex.Message);
			}
		}
	}

	private void EnsureCursorMarker()
	{
		if (!(_cursorMarker != null))
		{
			_cursorMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			_cursorMarker.name = "BPVR_Cursor";
			Collider component = _cursorMarker.GetComponent<Collider>();
			if (component != null)
			{
				UnityEngine.Object.Destroy(component);
			}
			UnityEngine.Object.DontDestroyOnLoad(_cursorMarker);
			Shader shader = Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
			_cursorRenderer = _cursorMarker.GetComponent<Renderer>();
			if (shader != null && _cursorRenderer != null)
			{
				_cursorRenderer.material = new Material(shader);
				_cursorRenderer.shadowCastingMode = ShadowCastingMode.Off;
				_cursorRenderer.receiveShadows = false;
			}
		}
	}

	private void ShowCursorMarker(Camera gameCam, int x, int yTopDown, bool grabbable)
	{
		if (!_pShowCursor.Value || gameCam == null)
		{
			return;
		}
		try
		{
			if (_cursorMarker == null)
			{
				_cursorMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
				_cursorMarker.name = "BPVR_Cursor";
				Collider component = _cursorMarker.GetComponent<Collider>();
				if (component != null)
				{
					UnityEngine.Object.Destroy(component);
				}
				UnityEngine.Object.DontDestroyOnLoad(_cursorMarker);
				Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
				_cursorRenderer = _cursorMarker.GetComponent<Renderer>();
				if (shader != null && _cursorRenderer != null)
				{
					_cursorRenderer.material = new Material(shader);
				}
			}
			float num = Mathf.Clamp(_pCursorDistance.Value, 0.3f, 10f);
			Vector3 position = gameCam.ScreenToWorldPoint(new Vector3(x, Screen.height - yTopDown, num));
			_cursorMarker.transform.position = position;
			float num2 = 0.012f * num;
			_cursorMarker.transform.localScale = new Vector3(num2, num2, num2);
			if (_cursorRenderer != null && _cursorRenderer.material != null)
			{
				_cursorRenderer.material.color = (grabbable ? new Color(1f, 0.82f, 0.28f) : new Color(0.95f, 0.95f, 1f));
			}
			if (!_cursorMarker.activeSelf)
			{
				_cursorMarker.SetActive(value: true);
			}
		}
		catch (System.Exception ex)
		{
			if (_cursorLog < 4)
			{
				_cursorLog++;
				LogWarn("[cursor] marker: " + ex.Message);
			}
		}
	}

	private static float WrapDeg(float d)
	{
		while (d > 180f)
		{
			d -= 360f;
		}
		while (d < -180f)
		{
			d += 360f;
		}
		return d;
	}

	private void AbortSteering(string why)
	{
		_steerDead = true;
		_steerAligned = true;
		LogWarn("[steer] disabled: " + why);
	}

	private void SteerViewToHand(Camera gameCam, Hand h)
	{
		if (!_pSteerView.Value || _steerDead || gameCam == null)
		{
			return;
		}
		if (Cursor.lockState == CursorLockMode.None)
		{
			_steerHeld = 0f;
			return;
		}
		if (h == null || !h.Valid || !h.TriggerDown || !_handTargetValid || !_handTargetIsHit)
		{
			_steerHeld = 0f;
			_worseFrames = 0;
			_lastErr = -1f;
			_steerLocked = false;
			return;
		}
		_steerHeld += Time.unscaledDeltaTime;
		if (!_steerLocked)
		{
			_steerGoal = _handTarget;
			_steerLocked = true;
			_worseFrames = 0;
			_lastErr = -1f;
		}
		Vector3 eulerAngles = gameCam.transform.eulerAngles;
		if (!_steerCal)
		{
			if (_calStage == 0 || _calStage == 2)
			{
				_calYaw0 = eulerAngles.y;
				_calPitch0 = eulerAngles.x;
				int num = Mathf.RoundToInt(_calNudge);
				try
				{
					if (_calStage == 0)
					{
						mouse_event(1u, num, 0, 0u, System.IntPtr.Zero);
					}
					else
					{
						mouse_event(1u, 0, num, 0u, System.IntPtr.Zero);
					}
				}
				catch
				{
					AbortSteering("mouse_event failed");
					return;
				}
				_calStage++;
				_calFrame = 0;
			}
			else
			{
				if (++_calFrame < 4)
				{
					return;
				}
				float num2 = ((_calStage == 1) ? WrapDeg(eulerAngles.y - _calYaw0) : WrapDeg(eulerAngles.x - _calPitch0));
				if (Mathf.Abs(num2) < 0.5f)
				{
					_calTries++;
					_calNudge *= 2f;
					if (_calTries > 4 || _calNudge > 400f)
					{
						AbortSteering("the view does not respond to mouse motion here, so hand aiming cannot steer it");
					}
					else
					{
						_calStage--;
					}
					return;
				}
				float num3 = Mathf.Clamp(Mathf.Abs(num2 / _calNudge), 0.005f, 1f) * Mathf.Sign(num2);
				if (_calStage == 1)
				{
					_degPerPixX = num3;
					_calStage = 2;
					_calTries = 0;
					_calNudge = 16f;
					Log($"[steer] yaw responds {_degPerPixX:F4} deg per mouse pixel");
				}
				else
				{
					_degPerPixY = num3;
					_steerCal = true;
					Log($"[steer] pitch responds {_degPerPixY:F4} deg per mouse pixel - calibrated");
				}
			}
			return;
		}
		Vector3 vector = _steerGoal - gameCam.transform.position;
		if (vector.sqrMagnitude < 0.0001f)
		{
			return;
		}
		vector.Normalize();
		float num4 = Mathf.Atan2(vector.x, vector.z) * 57.29578f;
		float num5 = (0f - Mathf.Asin(Mathf.Clamp(vector.y, -1f, 1f))) * 57.29578f;
		float num6 = WrapDeg(num4 - eulerAngles.y);
		float num7 = WrapDeg(num5 - eulerAngles.x);
		float num8 = (_pSteerYawOnly.Value ? Mathf.Abs(num6) : Mathf.Sqrt(num6 * num6 + num7 * num7));
		if (_lastErr >= 0f && num8 > _lastErr + 0.5f)
		{
			if (++_worseFrames > 10)
			{
				AbortSteering($"the view moved away from the target for {_worseFrames} " + "frames running, so the correction is wrong");
				return;
			}
		}
		else
		{
			_worseFrames = 0;
		}
		_lastErr = num8;
		bool value = _pSteerYawOnly.Value;
		_steerAligned = (value ? (Mathf.Abs(num6) < 2f) : (num8 < 1.5f));
		if (_steerAligned)
		{
			return;
		}
		float num9 = Mathf.Clamp(_pSteerSpeed.Value, 0.05f, 1f);
		float num10 = Mathf.Max(1f, _pSteerMaxDeg.Value * Time.unscaledDeltaTime);
		float num11 = Mathf.Clamp(num6 * num9, 0f - num10, num10);
		float num12 = (value ? 0f : Mathf.Clamp(num7 * num9, 0f - num10, num10));
		int num13 = Mathf.Clamp(Mathf.RoundToInt(num11 / _degPerPixX), -60, 60);
		int num14 = Mathf.Clamp(Mathf.RoundToInt(num12 / _degPerPixY), -60, 60);
		if (num13 != 0 || num14 != 0)
		{
			try
			{
				mouse_event(1u, num13, num14, 0u, System.IntPtr.Zero);
			}
			catch
			{
				AbortSteering("mouse_event failed");
				return;
			}
		}
		if (_steerLog < 30 && _inputFrames % 15 == 1)
		{
			_steerLog++;
			Log($"[steer] err yaw={num6:F1} pitch={num7:F1} total={num8:F1} -> mouse ({num13},{num14})");
		}
	}

	private bool ScreenPointFromPanel(Hand h, out Vector3 screenPoint)
	{
		screenPoint = Vector3.zero;
		if (_uiQuad == null || !_uiQuad.activeInHierarchy || h == null)
		{
			return false;
		}
		Transform transform = _uiQuad.transform;
		Vector3 rhs = -transform.forward;
		float num = Vector3.Dot(h.AimDirection, rhs);
		if (Mathf.Abs(num) < 1E-05f)
		{
			return false;
		}
		float num2 = Vector3.Dot(transform.position - h.AimOrigin, rhs) / num;
		if (num2 <= 0f)
		{
			return false;
		}
		Vector3 position = h.AimOrigin + h.AimDirection * num2;
		Vector3 vector = transform.InverseTransformPoint(position);
		if (Mathf.Abs(vector.x) > 0.5f || Mathf.Abs(vector.y) > 0.5f) return false;
		vector.x = Mathf.Clamp(vector.x, -0.4999f, 0.4999f);
		vector.y = Mathf.Clamp(vector.y, -0.4999f, 0.4999f);
		screenPoint = new Vector3((vector.x + 0.5f) * (float)Screen.width, (vector.y + 0.5f) * (float)Screen.height, num2);
		_panelThird = ((vector.x < -0.1667f) ? "LEFT" : ((vector.x > 0.1667f) ? "RIGHT" : "MIDDLE"));
		_panelLocalX = vector.x;
		if (_panelAimLog < 10 && _inputFrames % 30 == 1)
		{
			_panelAimLog++;
			Log($"[pointer] beam crosses the panel at ({vector.x:F2},{vector.y:F2}) {_panelThird} -> screen ({screenPoint.x:F0},{screenPoint.y:F0}) of {Screen.width}x{Screen.height}");
		}
		return true;
	}

	private void ClearClickState()
	{
		_clickFreeze = false;
		_haveFrozenAim = false;
		if (_mouseHeld)
		{
			try
			{
				mouse_event(4u, 0, 0, 0u, System.IntPtr.Zero);
			}
			catch
			{
			}
			_mouseHeld = false;
		}
		if (_unlocked)
		{
			_unlocked = false;
			if (_savedLock != CursorLockMode.None)
			{
				Cursor.lockState = _savedLock;
			}
		}
	}

	private void ClickFromTrigger(Hand h)
	{
		if(_quickMemo && h!=null && !h.IsLeft){ClearClickState();return;}
		if (_journalOpen || _journalBlockUntilRelease) { ClearClickState(); return; }
		if (_computerKeyboardMode) { ClearClickState(); return; }
		if (_draftMenu || NativePanelMenu) { ClearClickState(); return; }
		if (h == null || !h.TriggerDown) { _clickFreeze = false; _haveFrozenAim = false; }
		if (!_panelStick.Active && (h == null || h.AimCollider == null)) { ClearClickState(); return; }
		if (h != null && h.DirectClickConsumed) { ClearClickState(); return; }
		if (_panelStick.Active) { ClickFromPanelStick(h); return; }
		if (h != null && IsPickupTarget(h.AimCollider)) { ClearClickState(); return; }
		if (_panelAdjusting) { ClearClickState(); return; }
		if (h != null && _uiQuad != null && _uiQuad.activeInHierarchy && !ScreenPointFromPanel(h, out _)) { ClearClickState(); return; }
		if (!_pTriggerClicks.Value)
		{
			return;
		}
		if (_pHandsInHouseOnly.Value && !_inHouse)
		{
			_clickFreeze = false;
			ClearClickState();
			return;
		}
		bool flag = h != null && h.Valid && h.TriggerDown;
		if (h != null && h.TriggerDown && !h.TriggerWasDown && VRAim.Valid)
		{
			_frozenAim = VRAim.ScreenPoint;
			_haveFrozenAim = true;
			_clickFreeze = true;
		}
		if (_pUnlockToClick.Value)
		{
			if (flag)
			{
				if (!_unlocked)
				{
					_savedLock = Cursor.lockState;
					_unlocked = true;
					_unlockFrames = 0;
					_releaseFrames = 0;
					if (_unlockLog < 6)
					{
						_unlockLog++;
						Log($"[click] trigger held - releasing the pointer (was {_savedLock})");
					}
				}
				if (Cursor.lockState != CursorLockMode.None)
				{
					Cursor.lockState = CursorLockMode.None;
				}
				if (_unlockFrames++ < 4)
				{
					return;
				}
				if (!_clickFreeze)
				{
					_frozenAim = VRAim.ScreenPoint;
					_haveFrozenAim = VRAim.Valid;
					Hand handR = _handR;
					Log($"[card] pressed in the {_panelThird} third (x={_panelLocalX:F3}) -> click at ({_frozenAim.x:F0},{_frozenAim.y:F0}) of {Screen.width}x{Screen.height}, OS pointer at ({Input.mousePosition.x:F0},{Input.mousePosition.y:F0})" + ((handR != null) ? $", beam from {handR.AimOrigin} dir {handR.AimDirection}" : ", no right hand"));
				}
				_clickFreeze = true;
				if (_haveFrozenAim)
				{
					VRAim.ScreenPoint = _frozenAim;
				}
			}
			else if (_unlocked)
			{
				if (Cursor.lockState != CursorLockMode.None)
				{
					Cursor.lockState = CursorLockMode.None;
				}
				if (!_mouseHeld && ++_releaseFrames >= 3)
				{
					_unlocked = false;
					_clickFreeze = false;
					_haveFrozenAim = false;
					if (_savedLock != CursorLockMode.None)
					{
						Cursor.lockState = _savedLock;
					}
				}
			}
		}
		bool flag2 = h != null && h.Valid && h.TriggerDown;
		if (flag2 == _mouseHeld)
		{
			return;
		}
		try
		{
			mouse_event(flag2 ? 2u : 4u, 0, 0, 0u, System.IntPtr.Zero);
		}
		catch (System.Exception ex)
		{
			if (_clickLog < 3)
			{
				_clickLog++;
				LogWarn("mouse click: " + ex.Message);
			}
			return;
		}
		_mouseHeld = flag2;
		if (_clickLog >= 40)
		{
			return;
		}
		_clickLog++;
		string value = "nothing";
		try
		{
			Camera gameCamForCursor = _gameCamForCursor;
			if (gameCamForCursor != null)
			{
				VRAim.Bypass = true;
				Vector3 mousePosition = Input.mousePosition;
				VRAim.Bypass = false;
				if (Physics.Raycast(gameCamForCursor.ScreenPointToRay(new Vector3(mousePosition.x, mousePosition.y, 0f)), out var hitInfo, 25f))
				{
					value = $"{hitInfo.collider.name} at {hitInfo.distance:F2}m";
				}
			}
		}
		catch
		{
		}
		string value2 = "nothing";
		try
		{
			Camera gameCamForCursor2 = _gameCamForCursor;
			if (gameCamForCursor2 != null && Physics.Raycast(gameCamForCursor2.ScreenPointToRay(new Vector3((float)Screen.width * 0.5f, (float)Screen.height * 0.5f, 0f)), out var hitInfo2, 25f))
			{
				value2 = $"{hitInfo2.collider.name} at {hitInfo2.distance:F2}m";
			}
		}
		catch
		{
		}
		Log($"[click] mouse {(flag2 ? "down" : "up")} from trigger; pointer={Cursor.lockState}; under the pointer: '{value}'; dead ahead: '{value2}'");
	}

	private static float Dead(float v, float dz)
	{
		if (!(Mathf.Abs(v) <= dz))
		{
			return Mathf.Sign(v) * ((Mathf.Abs(v) - dz) / (1f - dz));
		}
		return 0f;
	}

	public override void OnLateUpdate()
	{
		if (_codexBootScene) return;
		if (!_vrReady)
		{
			return;
		}
		long perfStart=Stopwatch.GetTimestamp();
		_lastPoseWaitMs=0;
		try
		{
			Frame();
		}
		catch (System.Exception ex)
		{
			LogErr("frame failed, disabling VR: " + ex);
			_vrReady = false;
		}
		finally { RecordPerformance(perfStart,Stopwatch.GetTimestamp(),_lastPoseWaitMs); }
	}

	private void Frame()
	{
		BeginPerformanceStages();
		CodexHotkeys();
		TraceStartupFrame("before WaitGetPoses");
		long waitStart=Stopwatch.GetTimestamp();
		_comp.WaitGetPoses(_renderPoses, 64u, _gamePoses, 64u);
		_lastPoseWaitMs=Milliseconds(Stopwatch.GetTimestamp()-waitStart);
		TraceStartupFrame("after WaitGetPoses");
		TrackedDevicePose_t trackedDevicePose_t = _renderPoses[0];
		if (trackedDevicePose_t.bPoseIsValid)
		{
			HmdMatrix34_t mDeviceToAbsoluteTracking = trackedDevicePose_t.mDeviceToAbsoluteTracking;
			_headRot = ToUnity(mDeviceToAbsoluteTracking);
			_headPos = new Vector3(mDeviceToAbsoluteTracking.m3, mDeviceToAbsoluteTracking.m7, 0f - mDeviceToAbsoluteTracking.m11);
			if (!_haveOrigin)
			{
				_origin = _headPos;
				_haveOrigin = true;
			}
			if (VRInput.Menu)
			{
				_recenterHeld += Time.unscaledDeltaTime;
				if (_recenterHeld > 1f && _recenterArmed)
				{
					_recenterArmed = false;
					_origin = _headPos;
					Log($"[recenter] origin moved to {_headPos} - view re-centred");
				}
			}
			else
			{
				_recenterHeld = 0f;
				_recenterArmed = true;
			}
		}
		if (_sceneGrace > 0)
		{
			_sceneGrace--;
			return;
		}
		if ((_framesRendered & 0x1F) == 0)
		{
			ApplyFramePacing();
		}
		// A paused simulation still needs a fresh head-tracked image.
		Camera camera = ResolveGameCamera();
		if (camera == null)
		{
			if (++_camMissingFrames == 1 || _camMissingFrames % 300 == 0)
			{
				Log($"no world camera this frame (x{_camMissingFrames}), skipping render");
			}
			return;
		}
		_camMissingFrames = 0;
		if (_pDiagLevel.Value <= 0)
		{
			if (_framesRendered++ % 120 == 1)
			{
				Log($"L0 f{_framesRendered} cam='{camera.name}' - not rendering");
			}
			return;
		}
		if (camera.targetTexture != null)
		{
			if (++_skippedRT % 120 == 1)
			{
				Log("'" + camera.name + "' is rendering to a RenderTexture - skipping VR frame");
			}
			return;
		}
		_inHouse = camera.name == "FirstPersonCharacter" || string.Equals(SceneManager.GetActiveScene().name, "Mount Holly Estate", System.StringComparison.OrdinalIgnoreCase);
		EnsureEyeRigs();
		SyncEyeEffects(camera);
		if (_pUiWorld.Value)
		{
			UiWorld.Enabled = true;
			UiWorld.Width = Mathf.Clamp(_pUiWorldWidth.Value, 0.5f, 6f);
			UiWorld.Distance = Mathf.Clamp(_pUiWorldDist.Value, 0.4f, 6f);
			UiWorld.Drop = _pUiWorldDrop.Value;
			UiWorld.EventCamera = camera;
			UiWorld.Scan();
			UiWorld.Place(camera.transform.position + camera.transform.rotation * (_headPos - _origin), camera.transform.rotation * _headRot);
		}
		else
		{
			UiWorld.Enabled = false;
			AttachOverlayCanvases(camera);
		}
		TraceStartupFrame("before UI render");
		PerformanceStage(0);
		HideOverlayHands(out var overlayArms,out var overlayArmsEnabled,out var overlayOutlineEnabled);
		try { RenderUIPanel(camera); }
		finally { RestoreOverlayHands(overlayArms,overlayArmsEnabled,overlayOutlineEnabled); }
		TraceStartupFrame("after UI render");
		PerformanceStage(1);
		Snapshot.EyeRT = _eyeRT[0];
		Snapshot.EyeRT2 = _eyeRT[1];
		Snapshot.UiRT = _uiRT;
		Screens.Tick();
		Grab.TickPending();
		bool flag = _pUIPanel.Value && _pHudDirect.Value;
		AimRadius = Mathf.Clamp(_pAimRadius.Value, 0f, 0.5f);
		HandRig.ShowBeam = _pShowBeam.Value;
		Snapshot.UiRT = _uiRT;
		if (_pHudStrip.Value && !flag)
		{
			HudStrip.Distance = Mathf.Clamp(_pHudDistance.Value, 0.4f, 5f);
			HudStrip.Spread = Mathf.Clamp(_pHudWidth.Value, 0.2f, 4f);
			HudStrip.Scale = Mathf.Clamp(_pHudScale.Value, 0.5f, 8f);
			HudStrip.Rise = _pHudRise.Value;
			HudStrip.StripTop = Mathf.Clamp(_pHudStripTop.Value, 0.02f, 0.5f);
			HudStrip.FullScreen = _pHudFull.Value;
			HudStrip.FullWidth = Mathf.Clamp(_pHudFullWidth.Value, 0.5f, 8f);
			HudStrip.FullDistance = Mathf.Clamp(_pHudFullDist.Value, 0.4f, 6f);
			HudStrip.FullDrop = _pHudFullDrop.Value;
			HudStrip.Build(_uiRT);
			Quaternion hudAnchor = _inHouse ? Quaternion.Euler(0f, camera.transform.eulerAngles.y, 0f) : camera.transform.rotation;
			Quaternion look = hudAnchor * _headRot;
			Vector3 eye = camera.transform.position + hudAnchor * (_headPos - _origin);
			bool flag2 = _uiQuad != null && _uiQuad.activeInHierarchy;
			HudStrip.Place(eye, look, _inHouse && !flag2);
		}
		else if (HudStrip.Exists)
		{
			HudStrip.Place(Vector3.zero, Quaternion.identity, show: false);
		}
		CameraCensus();
		HideRoomCaption();
		Vector3 position = camera.transform.position;
		Quaternion rotation = camera.transform.rotation;
		position += ComputerViewOffset(position);
		// Once back in first-person, mouse pitch from inspection must not tilt
		// the tracked playspace. Head pitch remains supplied by the headset.
		if (_inHouse && camera.name.IndexOf("FirstPerson", System.StringComparison.OrdinalIgnoreCase)>=0)
			rotation = Quaternion.Euler(0f, rotation.eulerAngles.y, 0f);
		Vector3 vector = (_pPositional.Value ? (_headPos - TrackingOrigin) : Vector3.zero);
		_crouch = Mathf.MoveTowards(_crouch, _crouchHeld ? Mathf.Clamp(_pCrouch.Value, 0f, 1.2f) : 0f, 1.8f * Time.unscaledDeltaTime);
		if (_crouch > 0.001f)
		{
			vector += Vector3.down * _crouch;
		}
		if(_computerKeyboardMode && _activeComputer!=null)
			rotation*=Quaternion.Euler(0,-_computerTrackingYaw,0);
		else if(_inHouse&&Mathf.Abs(_comfortYaw)>.001f){var before=rotation;rotation*=Quaternion.Euler(0,_comfortYaw,0);position+=before*vector-rotation*vector;}
		if (_pHeadUnstick.Value)
		{
			try
			{
				Vector3 vector2 = -(rotation * _headRot * Vector3.forward);
				Vector3 vector3 = position + rotation * vector;
				if (Physics.CheckSphere(vector3, 0.16f, -1, QueryTriggerInteraction.Ignore))
				{
					for (float num = 0.1f; num <= 0.6f; num += 0.1f)
					{
						if (!Physics.CheckSphere(vector3 + vector2 * num, 0.16f, -1, QueryTriggerInteraction.Ignore))
						{
							vector += Quaternion.Inverse(rotation) * (vector2 * num);
							if (_shoveLog++ < 6)
							{
								Log($"[view] head was inside geometry - stepped back {num:F1}m");
							}
							break;
						}
					}
				}
			}
			catch
			{
			}
		}
		_gameCamForCursor = camera;
		if (_pUIInVR.Value && _pUIPanel.Value && _uiRT != null)
			PlaceUIPanel(rotation, position + rotation * vector);
		TraceStartupFrame("before hands");
		PerformanceStage(2);
		UpdateHands(position, rotation);
		ShowCelHands();
		if (_pWatch.Value && HandRig.Model?.HandL != null && !Watch.Exists)
			Watch.Build(_celHands?.Wrists[0]??HandRig.Model.HandL, _celHands?.PalmLocal(true)??Fingers.PalmLocalL, Shader.Find("Unlit/Texture"),_celHands?.Forearms[0],_celHands?.Renderer,_celHands!=null?.012f:-.026f);
		Watch.SetVisible(_pWatch.Value && _inHouse && _handL != null && _handL.Valid && !_journalOpen && _visionCamera == null && !_heldToolVisible);
		Watch.SetSource(_uiRT);
		if(_handR==null || !_handR.Valid || _journalOpen || _visionCamera!=null)HideHeldTools();
		JournalFrame(camera,position+rotation*vector,rotation*_headRot);
		ShowTypingHint(position + rotation * vector, rotation * _headRot);
		ComfortFrame(position + rotation * vector, rotation * _headRot);
		if (_armOutline != null) _armOutline.enabled = HandRig.Model != null && HandRig.Model.Renderer != null && HandRig.Model.Renderer.enabled;
		// Hide only the visual mesh, not the tracked skeleton driving the pencil.
		if (_heldToolVisible || (_visionCamera!=null && !_journalOpen) || (_journalOpen && !_journalFraming && _journalBoard!=null)) {
			if(HandRig.Model?.Renderer!=null)HandRig.Model.Renderer.enabled=false;
			if(_armOutline!=null)_armOutline.enabled=false;
		}
		TraceStartupFrame("after hands");
		PerformanceStage(3);
		float num2 = Mathf.Clamp(_pIpdScale.Value, 0.1f, 3f);
		float num3 = Mathf.Max(0.02f, camera.nearClipPlane);
		float num4 = Mathf.Max(num3 + 1f, camera.farClipPlane);
		if (_pThroughGameCam.Value)
		{
			long timestamp = Stopwatch.GetTimestamp();
			RenderEyesThroughGameCamera(camera, position, rotation, vector, num2, num3, num4);
			double num5 = (double)(Stopwatch.GetTimestamp() - timestamp) * 1000.0 / (double)Stopwatch.Frequency;
			_eyeMsAvg = ((_eyeMsAvg <= 0.0) ? num5 : (_eyeMsAvg * 0.95 + num5 * 0.05));
		}
		else
		{
			for (int i = 0; i < 2; i++)
			{
				Camera camera2 = _eyeCam[i];
				if (camera2 == null)
				{
					continue;
				}
				camera2.CopyFrom(camera);
				camera2.enabled = false;
				camera2.targetTexture = _eyeRT[i];
				camera2.rect = new Rect(0f, 0f, 1f, 1f);
				camera2.allowHDR = _pEyeHDR.Value;
				camera2.allowMSAA = false;
				if (_pForceClear.Value)
				{
					if (camera2.clearFlags == CameraClearFlags.Depth || camera2.clearFlags == CameraClearFlags.Nothing)
					{
						camera2.clearFlags = CameraClearFlags.Color;
					}
					Color backgroundColor = camera2.backgroundColor;
					camera2.backgroundColor = new Color(backgroundColor.r, backgroundColor.g, backgroundColor.b, 1f);
				}
				camera2.nearClipPlane = num3;
				camera2.farClipPlane = num4;
				camera2.projectionMatrix = EyeProjection((EVREye)i, num3, num4);
				Quaternion rotation2 = rotation * _headRot;
				Vector3 vector4 = position + rotation * (vector + _headRot * (_eyeOffset[i] * num2));
				camera2.transform.SetPositionAndRotation(vector4, rotation2);
				if (_pDiagLevel.Value >= 2)
				{
					if (_rendering)
					{
						return;
					}
					_rendering = true;
					try
					{
						RenderCodexEye(camera2, _eyeRT[i], i);
					}
					finally
					{
						_rendering = false;
					}
					if (_framesRendered <= 3)
					{
						Log($"eye {i} rendered: rt={((camera2.targetTexture == null) ? "NULL" : (camera2.targetTexture.width + "x" + camera2.targetTexture.height))} created={_eyeRT[i].IsCreated()} pos={vector4} rot={rotation2.eulerAngles}");
					}
				}
			}
		}
		if (_pDiagLevel.Value < 3)
		{
			_framesRendered++;
			if (_framesRendered % 120 == 1)
			{
				Log($"L{_pDiagLevel.Value} f{_framesRendered} cam='{camera.name}' pos={position} - rendered, not submitted");
			}
			return;
		}
		if (_pGpuProbe.Value && _framesRendered % 180 == 5)
		{
			SampleEyeTexture();
		}
		TraceStartupFrame("eyes rendered; before flush/submit");
		PerformanceStage(4);
		GL.Flush();
		SubmitEye(EVREye.Left, 0);
		SubmitEye(EVREye.Right, 1);
		TraceStartupFrame("before PostPresentHandoff");
		_comp.PostPresentHandoff();
		CaptureVisualDiagnostics();
		TraceStartupFrame("frame completed");
		PerformanceStage(5);
		_framesRendered++;
		if (_framesRendered % 60 == 1)
		{
			Log($"f{_framesRendered} cam='{camera.name}' pos={position} near={num3:F2} far={num4:F0} ts={Time.timeScale:F1} submitErr={_lastSubmitErr[0]}/{_lastSubmitErr[1]} eyeRender={_eyeMsAvg:F1}ms rt={_eyeW}x{_eyeH} vsync={QualitySettings.vSyncCount} resets={_pacingResets}");
		}
	}

	private void FpsLog()
	{
		_fpsWindowFrames++;
		float unscaledTime = Time.unscaledTime;
		if (_fpsWindowStart <= 0f)
		{
			_fpsWindowStart = unscaledTime;
			_fpsWindowFrames = 0;
			return;
		}
		float num = unscaledTime - _fpsWindowStart;
		if (!(num < 5f))
		{
			float value = (float)_fpsWindowFrames / num;
			_fpsWindowStart = unscaledTime;
			_fpsWindowFrames = 0;
			Log($"[fps] {value:F1}  eye {_eyeW}x{_eyeH} (scale {_pResScale.Value:F2})  eye render {_eyeMsAvg:F1} ms");
		}
	}

	private void CameraCensus()
	{
		FpsLog();
		if (!_pCameraCensus.Value || _framesRendered % 600 != 7)
		{
			return;
		}
		try
		{
			Il2CppReferenceArray<Camera> allCameras = Camera.allCameras;
			if (allCameras == null)
			{
				return;
			}
			Log($"[cams] {allCameras.Length} active:");
			for (int i = 0; i < allCameras.Length; i++)
			{
				Camera camera = allCameras[i];
				if (!(camera == null))
				{
					Log($"[cams]   '{camera.name}' depth={camera.depth} ortho={camera.orthographic} mask=0x{camera.cullingMask:X} clear={camera.clearFlags} target={((camera.targetTexture == null) ? "screen" : "texture")} pos={camera.transform.position}");
				}
			}
		}
		catch (System.Exception ex)
		{
			LogWarn("[cams] " + ex.Message);
		}
	}

	private Camera EnsureUICamera(Camera like)
	{
		if (_uiCam != null)
		{
			return _uiCam;
		}
		GameObject gameObject = new GameObject("BPVR_UICam");
		UnityEngine.Object.DontDestroyOnLoad(gameObject);
		gameObject.transform.position = new Vector3(0f, -3000f, 0f);
		_uiCam = gameObject.AddComponent<Camera>();
		_uiCam.enabled = false;
		_uiCam.clearFlags = CameraClearFlags.Depth;
		_uiCam.fieldOfView = ((like != null) ? like.fieldOfView : 60f);
		_uiCam.nearClipPlane = 0.05f;
		_uiCam.farClipPlane = 50f;
		_uiCam.depth = 100f;
		Log("[ui] created a dedicated camera for the game's overlay canvases");
		return _uiCam;
	}

	private static bool IsNativeMapCamera(Camera camera) => camera!=null &&
		(camera.name=="Blue Print Map" || camera.name=="Map Camera Drafting");

	private void RenderUIPanel(Camera world)
	{
		if (!_pUIInVR.Value || !_pUIPanel.Value || world == null)
		{
			return;
		}
		try
		{
			ScanMovieTexture();
			if (_uiRT == null || !_uiRT.IsCreated())
			{
				_uiRT = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
				_uiRT.hideFlags = HideFlags.HideAndDontSave;
				_uiRT.Create();
				UnityEngine.Object.DontDestroyOnLoad(_uiRT);
				Log("[ui] panel texture 1920x1080 created");
			}
			RenderTexture active = RenderTexture.active;
			RenderTexture.active = _uiRT;
			GL.Clear(clearDepth: true, clearColor: true, new Color(0f, 0f, 0f, 0f));
			RenderTexture.active = active;
			if(_movieTexture!=null) {
				// Same decoded frame for both eyes; no second decoder or CPU readback.
				Graphics.Blit(_movieTexture,_uiRT);
				return;
			}
			Il2CppReferenceArray<Camera> allCameras = Camera.allCameras;
			if (allCameras == null)
			{
				return;
			}
			System.Collections.Generic.List<Camera> list = new System.Collections.Generic.List<Camera>();
			for (int i = 0; i < allCameras.Length; i++)
			{
				Camera camera = allCameras[i];
				if(IsInspectionSceneCamera(camera)&&camera!=_roomPanelCamera)continue;
				if (!(camera == null) && !(camera == world) && !(camera == _uiCam) && !(camera == _eyeCam[0]) && !(camera == _eyeCam[1]) && camera.isActiveAndEnabled && !(camera.targetTexture != null) && !(camera.depth < world.depth))
				{
					list.Add(camera);
				}
			}
			if (_uiCam != null)
			{
				list.Add(_uiCam);
			}
			list.Sort(ComparePresentationCameras);
			int num = 0;
			foreach (Camera item in list)
			{
				if(_moviePresentationEnded && item==_visionCamera)continue;
				RenderTexture targetTexture = item.targetTexture;
				CameraClearFlags clearFlags = item.clearFlags;
				Rect rect = item.rect;
				int cullingMask = item.cullingMask;
				Matrix4x4 nativeProjection=item.projectionMatrix;
				try
				{
					if (item != _uiCam && !IsNativeMapCamera(item) && item.name.IndexOf("UI OVERLAY",System.StringComparison.OrdinalIgnoreCase)<0)
					{
						item.cullingMask = cullingMask & ~_movedLayers;
					}
					if (item.cullingMask != 0)
					{
						num++;
						item.clearFlags = item==_visionCamera || item==_roomPanelCamera ? CameraClearFlags.Color : CameraClearFlags.Depth;
						// Native map uses a sub-viewport within the pause/draft panel.
						// Keep it there and don't mask its shared UI layer away.
						item.rect = IsNativeMapCamera(item) ? rect : new Rect(0f, 0f, 1f, 1f);
						item.targetTexture = _uiRT;
						item.projectionMatrix=nativeProjection;
						RenderWithoutBeautify(item);
					}
				}
				finally
				{
					item.targetTexture = targetTexture;
					item.clearFlags = clearFlags;
					item.rect = rect;
					item.cullingMask = cullingMask;
					item.projectionMatrix=nativeProjection;
				}
			}
			if (_uiQuad != null && _uiPanelLog < 6 && _framesRendered % 600 < 2)
			{
				_uiPanelLog++;
				Log($"[ui] panel: {num} of {list.Count} overlay cameras drew into it; quad at {_uiQuad.transform.position} scale {_uiQuad.transform.localScale} shader '{((_uiQuadRenderer != null && _uiQuadRenderer.material != null) ? _uiQuadRenderer.material.shader.name : "none")}' visible={_uiQuad.activeInHierarchy && (_uiQuadRenderer == null || _uiQuadRenderer.enabled)}");
			}
		}
		catch (System.Exception ex)
		{
			if (_uiPanelLog < 5)
			{
				_uiPanelLog++;
				LogWarn("[ui] panel: " + ex.Message);
			}
		}
	}

	private void PlaceUIPanel(Quaternion playspaceRotation, Vector3 eyePosition)
	{
		if (_uiQuad == null)
		{
			_uiQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
			_uiQuad.name = "BPVR_UIPanel";
			Collider component = _uiQuad.GetComponent<Collider>();
			if (component != null)
			{
				UnityEngine.Object.Destroy(component);
			}
			UnityEngine.Object.DontDestroyOnLoad(_uiQuad);
			Shader shader = Shader.Find("Unlit/Transparent") ?? Shader.Find("Sprites/Default") ?? Shader.Find("UI/Default") ?? Shader.Find("Unlit/Texture") ?? Shader.Find("Standard");
			Log("[ui] panel shader: " + ((shader != null) ? shader.name : "NONE FOUND"));
			_uiQuadRenderer = _uiQuad.GetComponent<Renderer>();
			if (shader != null && _uiQuadRenderer != null)
			{
				_uiQuadRenderer.material = new Material(shader);
			}
			if (_uiQuadRenderer != null)
			{
				_uiQuadRenderer.material.mainTexture = _uiRT;
				_uiQuadRenderer.shadowCastingMode = ShadowCastingMode.Off;
				_uiQuadRenderer.receiveShadows = false;
				Material material = _uiQuadRenderer.material;
				float num = Mathf.Clamp01(_pUITint.Value);
				try
				{
					material.color = new Color(num, num, num, 1f);
				}
				catch
				{
				}
				int value = 8;
				if (material.HasProperty("unity_GUIZTestMode"))
				{
					material.SetInt("unity_GUIZTestMode", value);
				}
				if (material.HasProperty("_ZTest"))
				{
					material.SetInt("_ZTest", value);
				}
				material.renderQueue = 4000;
			}
			Log("[ui] panel quad created");
		}
		// X/Y already send the game's directory/blueprint actions. Do not also
		// toggle a second empty panel on top of the native menu.
		if (VRInput.Cancel) _forcePanel = false;
		bool flag = (Cursor.lockState == CursorLockMode.None && !_unlocked) || _forcePanel || Screens.VideoUp || _movieTexture!=null || _anteCinematic || _roomPanelCamera!=null;
		if(_moviePresentationEnded && (Screens.VideoUp || _visionCamera!=null) && _roomPanelCamera==null && !_forcePanel && !_draftMenu && !NativePanelMenu && !_computerKeyboardMode)
			flag=false;
		if (_computerKeyboardMode && _activeComputer!=null && _gameCamForCursor!=null)
		{
			// Reproject the native desktop UI at the monitor depth. A head-relative
			// reading panel floats above the physical CRT after the view backs away.
			var desktop=_gameCamForCursor;
			float depth=Vector3.Dot(_activeComputer.TransformPoint(_computerScreenLocal)-desktop.transform.position,desktop.transform.forward);
			var projection=desktop.projectionMatrix;
			if(depth>.05f && Mathf.Abs(projection.m00)>.01f && Mathf.Abs(projection.m11)>.01f)
			{
				_uiQuad.transform.SetPositionAndRotation(desktop.transform.TransformPoint(new Vector3(projection.m02*depth/projection.m00,projection.m12*depth/projection.m11,depth)),desktop.transform.rotation);
				_uiQuad.transform.localScale=new Vector3(2f*depth/Mathf.Abs(projection.m00),2f*depth/Mathf.Abs(projection.m11),1f);
				_uiQuad.SetActive(true);_panelAimValid=false;
				return;
			}
			// Never flash a head-locked full-size panel when the native camera
			// crosses the monitor plane during an inspection transition.
			_uiQuad.SetActive(false);_panelAimValid=false;
			return;
		}
		// Re-anchor at opening, never reuse an aim left behind by the previous menu.
		if (!flag || !_uiQuad.activeSelf || _lastPanelRoomCamera!=_roomPanelCamera) _panelAimValid = false;
		_lastPanelRoomCamera=_roomPanelCamera;
		Vector3 vector;
		if (_panelAimValid)
		{
			vector = _panelAim;
		}
		else
		{
			vector = playspaceRotation * _headRot * Vector3.forward;
			if (vector.sqrMagnitude < 1E-06f)
			{
				vector = playspaceRotation * Vector3.forward;
			}
			vector.Normalize();
			if (vector.y > 0.85f || vector.y < -0.85f)
			{
				vector.y = Mathf.Clamp(vector.y, -0.85f, 0.85f);
				vector.Normalize();
			}
		}
		float num2 = _panelDistance;
		Vector3 vector2 = eyePosition;
		// The overlay already bypasses wall depth. Collision resizing here made the
		// panel jump between nearby door colliders and its requested reading distance.
		_uiDist = ((!_panelAimValid || _uiDist <= 0f) ? num2 : Mathf.Lerp(_uiDist, num2, 1f - Mathf.Exp(-12f * Mathf.Min(Time.unscaledDeltaTime, 0.05f))));
		num2 = _uiDist;
		_uiQuad.transform.position = vector2 + vector * num2;
		_uiQuad.transform.rotation = Quaternion.LookRotation(vector, Vector3.up);
		_panelAim = vector;
		float num3 = Mathf.Clamp(_pUIPanelWidth.Value, 0.3f, 10f) * num2;
		float panelAspect=_movieTexture!=null ? (float)_movieTexture.height/Mathf.Max(1,_movieTexture.width) : (float)Screen.height/Mathf.Max(1,Screen.width);
		_uiQuad.transform.localScale = new Vector3(num3, num3 * panelAspect, 1f);
		// Actual panel is rendered after the world, with its depth buffer cleared.
		// This works even when a wide card panel intersects a wall at its edges.
		if (_uiQuad.activeSelf != flag)
		{
			_uiQuad.SetActive(flag);
			if (!flag)
			{
				_uiDist = 0f;
				_panelAimValid = false;
			}
			else
			{
				_panelAimValid = true;
			}
		}
		_panelAimValid = flag;
	}

	private void RenderUICameras(RenderTexture rt, Camera world, int eye)
	{
		if (!_pUIInVR.Value || rt == null)
		{
			return;
		}
		Il2CppReferenceArray<Camera> allCameras = Camera.allCameras;
		if (allCameras == null)
		{
			return;
		}
		if (UILogWanted)
		{
			_uiCamLog++;
		}
		System.Collections.Generic.List<Camera> list = new System.Collections.Generic.List<Camera>();
		for (int i = 0; i < allCameras.Length; i++)
		{
			Camera camera = allCameras[i];
			if (!(camera == null) && !IsInspectionSceneCamera(camera) && !(camera == world) && !(camera == _eyeCam[0]) && !(camera == _eyeCam[1]) && camera.isActiveAndEnabled && !(camera.targetTexture != null) && !(camera.depth < world.depth))
			{
				list.Add(camera);
			}
		}
		list.Sort(ComparePresentationCameras);
		foreach (Camera item in list)
		{
			RenderTexture targetTexture = item.targetTexture;
			CameraClearFlags clearFlags = item.clearFlags;
			Rect rect = item.rect;
			Vector3 position = item.transform.position;
			int cullingMask = item.cullingMask;
			try
			{
				float num = Mathf.Clamp(_pUIParallax.Value, 0f, 0.2f);
				float num2 = Mathf.Clamp(_pUIScale.Value, 0.2f, 1f);
				if (num > 0f || num2 < 0.999f)
				{
					Matrix4x4 projectionMatrix = item.projectionMatrix;
					projectionMatrix.m00 *= num2;
					projectionMatrix.m11 *= num2;
					projectionMatrix.m02 += ((eye == 0) ? num : (0f - num));
					item.projectionMatrix = projectionMatrix;
				}
				item.cullingMask = IsNativeMapCamera(item) ? cullingMask : cullingMask & ~_movedLayers;
				if (item.cullingMask != 0)
				{
					item.clearFlags = CameraClearFlags.Depth;
					item.rect = IsNativeMapCamera(item) ? rect : new Rect(0f, 0f, 1f, 1f);
					item.targetTexture = rt;
					item.Render();
					if (UILogWanted)
					{
						Log($"[ui] drew '{item.name}' depth={item.depth} into eye {eye}");
					}
				}
			}
			catch (System.Exception ex)
			{
				if (_uiCamLog < 20)
				{
					_uiCamLog++;
					LogWarn("[ui] '" + item.name + "': " + ex.Message);
				}
			}
			finally
			{
				item.targetTexture = targetTexture;
				item.clearFlags = clearFlags;
				item.rect = rect;
				item.transform.position = position;
				item.cullingMask = cullingMask;
				item.ResetProjectionMatrix();
			}
		}
	}

	private void RenderHudDirect(RenderTexture rt, Camera world, int eye)
	{
		if (_uiQuad != null && _uiQuad.activeInHierarchy)
		{
			return;
		}
		try
		{
			var allCameras = new System.Collections.Generic.List<Camera>();
			foreach(var activeCamera in Camera.allCameras)allCameras.Add(activeCamera);
			// Adopted action prompts live on this disabled, manually-rendered
			// camera. Camera.allCameras omits it, so the direct HUD lost them.
			if(_uiCam!=null)allCameras.Add(_uiCam);
			int num = 0;
			while (num < allCameras.Count)
			{
				Camera camera = allCameras[num];
				if (!(camera == null) && !(camera == world) && (camera.isActiveAndEnabled||camera==_uiCam) && !(camera.targetTexture != null) && (camera==_uiCam||camera.name.IndexOf("UI OVERLAY", System.StringComparison.OrdinalIgnoreCase) >= 0))
				{
					RenderTexture targetTexture = camera.targetTexture;
					CameraClearFlags clearFlags = camera.clearFlags;
					Rect rect = camera.rect;
					Matrix4x4 hudProjection = camera.projectionMatrix;
					try
					{
						float num2 = Mathf.Clamp(_pHudDirectScale.Value, 0.2f, 1f);
						float num3 = camera.aspect;
						if (num3 <= 0.01f || float.IsNaN(num3))
						{
							num3 = 1.7777778f;
						}
						float num4 = num2;
						float num5 = num2 * (float)rt.width / num3 / (float)Mathf.Max(1, rt.height);
						if (num5 > 1f)
						{
							num4 /= num5;
							num5 = 1f;
						}
						float num6 = 0.5f;
						try
						{
							float m = world.projectionMatrix.m02;
							if (!float.IsNaN(m) && Mathf.Abs(m) < 0.9f)
							{
								num6 = (1f - m) * 0.5f;
							}
						}
						catch
						{
						}
						float num7 = Mathf.Clamp(_pHudDirectParallax.Value, 0f, 0.06f);
						float num8 = ((eye == 0) ? num7 : (0f - num7));
						float num9 = Mathf.Clamp(num6 - num4 * 0.5f + num8, 0f, 1f - num4);
						float y = (1f - num5) * 0.5f;
						if (_hudAxisLog < 2)
						{
							_hudAxisLog++;
							Log($"[hud] eye {eye} optical axis at {num6:F3} of width (texture centre is 0.500), box x={num9:F3} w={num4:F3}");
						}
						// Keep the HUD's native framing: changing its aspect clipped
						// the left icon / first digit on letterboxed desktop windows.
						camera.projectionMatrix = hudProjection;
						camera.clearFlags = CameraClearFlags.Depth;
						camera.rect = new Rect(num9, y, num4, num5);
						camera.targetTexture = rt;
						camera.Render();
					}
					finally
					{
						camera.targetTexture = targetTexture;
						camera.clearFlags = clearFlags;
						camera.rect = rect;
						try
						{
							camera.projectionMatrix = hudProjection;
						}
						catch
						{
						}
					}
				}
				num++;
			}
		}
		catch (System.Exception ex)
		{
			if (_hudDirectLog++ < 3)
			{
				LogWarn("[hud] direct: " + ex.Message);
			}
		}
	}

	private void AttachOverlayCanvases(Camera cam)
	{
		if (!_pUIInVR.Value || cam == null || Time.frameCount - _lastCanvasScan < 60)
		{
			return;
		}
		_lastCanvasScan = Time.frameCount;
		try
		{
			Il2CppArrayBase<Canvas> il2CppArrayBase = UnityEngine.Object.FindObjectsOfType<Canvas>();
			if (il2CppArrayBase == null)
			{
				return;
			}
			if (!_canvasCensus)
			{
				_canvasCensus = true;
				StringBuilder stringBuilder = new StringBuilder();
				for (int i = 0; i < il2CppArrayBase.Length && i < 30; i++)
				{
					Canvas canvas = il2CppArrayBase[i];
					if (!(canvas == null))
					{
						string value = "-";
						try
						{
							value = ((canvas.worldCamera != null) ? canvas.worldCamera.name : "none");
						}
						catch
						{
						}
						StringBuilder stringBuilder2 = stringBuilder;
						StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(17, 4, stringBuilder2);
						handler.AppendLiteral("'");
						handler.AppendFormatted(canvas.name);
						handler.AppendLiteral("'[");
						handler.AppendFormatted(canvas.renderMode);
						handler.AppendLiteral("/cam:");
						handler.AppendFormatted(value);
						handler.AppendLiteral("/order:");
						handler.AppendFormatted(canvas.sortingOrder);
						handler.AppendLiteral("] ");
						stringBuilder2.Append(ref handler);
					}
				}
				Log($"[ui] {il2CppArrayBase.Length} canvases: {stringBuilder}");
			}
			for (int j = 0; j < il2CppArrayBase.Length; j++)
			{
				Canvas canvas2 = il2CppArrayBase[j];
				if (canvas2 == null)
				{
					continue;
				}
				bool flag = canvas2.renderMode == RenderMode.ScreenSpaceOverlay;
				bool flag2 = false;
				if (!flag && canvas2.renderMode == RenderMode.ScreenSpaceCamera)
				{
					try
					{
						Camera worldCamera = canvas2.worldCamera;
						// Main-camera screen-space UI was skipped, leaving menu map
						// content in the stereo world instead of the readable panel.
						// Restrict main-camera adoption to the UI layer; do not move
						// world-space computer/document canvases.
						flag2 = worldCamera == null || (worldCamera != _uiCam && (worldCamera != cam || canvas2.gameObject.layer==5));
					}
					catch
					{
						flag2 = false;
					}
				}
                // The main menu creates the Profiles page as a private
                // ScreenSpaceCamera canvas.  It is visible on the desktop,
                // but that canvas was left on the game's monitor camera when
                // the optional adoption preference was off, so the headset
                // saw only the underlying menu.  Main-menu canvases are safe
                // to route through our dedicated UI camera; keep the old
                // opt-in behavior for in-house canvases.
                if (flag || (flag2 && (_pAdoptCanvases.Value || _mainMenuScene)))
				{
					if(canvas2.renderMode==RenderMode.ScreenSpaceCamera && canvas2.worldCamera==cam)
						Log("[menu-map] Adopting main-camera UI canvas: "+canvas2.name);
					canvas2.renderMode = RenderMode.ScreenSpaceCamera;
					canvas2.worldCamera = EnsureUICamera(cam);
					canvas2.planeDistance = 1f;
					_movedLayers |= 1 << canvas2.gameObject.layer;
					if (_canvasLog < 12)
					{
						_canvasLog++;
						Log($"[ui] canvas '{canvas2.name}' moved onto '{cam.name}' so it reaches the headset");
					}
				}
			}
		}
		catch (System.Exception ex)
		{
			if (_canvasLog < 3)
			{
				_canvasLog++;
				LogWarn("[ui] " + ex.Message);
			}
		}
	}

	private void RenderEyesThroughGameCamera(Camera cam, Vector3 anchorPos, Quaternion anchorRot, Vector3 headLocal, float ipd, float near, float far)
	{
		if (_rendering)
		{
			return;
		}
		RenderTexture targetTexture = cam.targetTexture;
		Rect rect = cam.rect;
		int cullingMask = cam.cullingMask;
		bool flag = false;
		try
		{
			_rendering = true;
			cam.rect = new Rect(0f, 0f, 1f, 1f);
			if (UiWorld.Any)
			{
				cam.cullingMask = cullingMask | UiWorld.Layers;
			}
			for (int i = 0; i < 2; i++)
			{
				if (!(_eyeRT[i] == null))
				{
					Quaternion q = anchorRot * _headRot;
					Vector3 pos = anchorPos + anchorRot * (headLocal + _headRot * (_eyeOffset[i] * ipd));
					cam.worldToCameraMatrix = Matrix4x4.TRS(pos, q, new Vector3(1f, 1f, -1f)).inverse;
					cam.projectionMatrix = EyeProjection((EVREye)i, near, far);
					cam.targetTexture = _eyeRT[i];
					RenderCodexEye(cam, _eyeRT[i], i);
					if (!_pUIPanel.Value)
					{
						RenderUICameras(_eyeRT[i], cam, i);
					}
					else if (_pHudDirect.Value)
					{
						RenderHudDirect(_eyeRT[i], cam, i);
					}
				}
			}
		}
		finally
		{
			cam.targetTexture = targetTexture;
			cam.rect = rect;
			cam.cullingMask = cullingMask;
			cam.ResetWorldToCameraMatrix();
			cam.ResetProjectionMatrix();
			// Assigning cullingMatrix pins it, even when assigning a saved automatic
			// value. Release the eye override so native camera motion updates culling.
			cam.ResetCullingMatrix();
			cam.ResetAspect();
			_rendering = false;
			flag = true;
		}
		if (!flag)
		{
			LogErr("failed to restore the game camera");
		}
	}

	private void UpdateHands(Vector3 anchorPos, Quaternion anchorRot)
	{
		HideDigCues();
		if (!_pHands.Value || (_pHandsInHouseOnly.Value && !_inHouse))
		{
			if (_cursorMarker != null && _cursorMarker.activeSelf)
			{
				_cursorMarker.SetActive(value: false);
			}
			VRAim.Valid = false;
			HandRig.SetVisible(_handL, on: false);
			HandRig.SetVisible(_handR, on: false);
			if (HandRig.Model != null && HandRig.Model.Renderer != null)
			{
				HandRig.Model.Renderer.enabled = false;
			}
			ClickFromTrigger(null);
			return;
		}
		EnsureArmsModel();
		if (_handL == null)
		{
			_handL = HandRig.Build("BPVR_ArmL", leftHand: true);
		}
		if (_handR == null)
		{
			_handR = HandRig.Build("BPVR_ArmR", leftHand: false);
		}
		Vector3 vector = (_pPositional.Value ? (_headPos - TrackingOrigin) : Vector3.zero);
		if (_crouch > 0.001f)
		{
			vector += Vector3.down * _crouch;
		}
		Vector3 headPos = anchorPos + anchorRot * vector;
		HandRig.ShoulderDrop = _pShoulderDrop.Value;
		HandRig.PlaceBody(headPos, anchorRot);
		if (HandRig.Model != null && HandRig.Model.Renderer != null)
		{
			HandRig.Model.Renderer.enabled = true;
		}
		PoseHand(_handL, ETrackedControllerRole.LeftHand, anchorPos, anchorRot);
		PoseHand(_handR, ETrackedControllerRole.RightHand, anchorPos, anchorRot);
		// Trigger is interaction only, regardless of legacy HandAimSteersView settings.
		UpdateInteractionHand();
		TickDoorHaptics();
	}

	private void EnsureArmsModel()
	{
		if (_armsTried || !_pModelArms.Value)
		{
			return;
		}
		_armsTried = true;
		string path = Path.Combine(CodexAssetDirectory, "arms.bpvr");
		string importedPath = Path.Combine(CodexAssetDirectory, "fps-arms-rigged.bpvr");
		_importedArms = File.Exists(importedPath);
		if (_importedArms) path = importedPath;
		ArmsAsset.IncludeLegacyAccessories = !_importedArms;
		Log("[arms] Selected " + Path.GetFileName(path));
		if (!File.Exists(path)) path = Path.Combine(Application.dataPath, "..", "UserData", "arms.bpvr");
		Shader shader = null;
		if (_pArmsUnlit.Value && !_pRoundedArms.Value && !_importedArms)
		{
			shader = Shader.Find("Unlit/Texture") ?? Shader.Find("Mobile/Unlit (Supports Lightmap)") ?? Shader.Find("Sprites/Default");
		}
		shader = shader ?? Shader.Find("Standard") ?? Shader.Find("Diffuse");
		if (shader == null)
		{
			LogWarn("[arms] no usable shader");
			return;
		}
		ArmsAsset.JointThickness = Mathf.Clamp(_pJointThickness.Value, 0.5f, 3f);
		ArmsAsset.Brightness = (_pRoundedArms.Value || _importedArms) ? 1f : Mathf.Clamp(_pArmBrightness.Value, 0.2f, 1f);
		ArmsAsset.Scale = Mathf.Clamp(_pArmScale.Value, 0.3f, 2f);
		ArmsAsset.SwapSides = _pSwapArmSides.Value;
		Grab.SendNamedEvents = _pNamedEvents.Value;
		ArmsModel armsModel = ArmsAsset.Load(path, shader);
		if (armsModel == null && _importedArms)
		{
			LogWarn("[arms] Imported asset failed: " + ArmsAsset.LastError + "; trying original asset");
			_importedArms = false;
			ArmsAsset.IncludeLegacyAccessories = true;
			armsModel = ArmsAsset.Load(Path.Combine(CodexAssetDirectory, "arms.bpvr"), shader);
		}
		if (armsModel == null)
		{
			LogWarn("[arms] not used (" + ArmsAsset.LastError + ") - falling back to capsules");
			return;
		}
		HandRig.Model = armsModel;
		FitArmReach(armsModel);
		ApplyCodexArms(armsModel);
		HandRig.HandEuler = new Vector3(_pHandPitch.Value, _pHandYaw.Value, _pHandRoll.Value);
		HandRig.ImportedRig = _importedArms;
		Fingers.Bind(armsModel, _importedArms);
		LoadCelHands();
		Snapshot.Folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "UserData"));
		Watch.Size = Mathf.Clamp(_pWatchSize.Value, 0.4f, 4f);
	}

	private bool AimHit(Hand h, out RaycastHit best, out bool clickable)
	{
		best = default;
		clickable = false;
		if (h == null || !h.Valid) return false;
		// Query far enough for ground-level dirt piles, but retain the short
		// per-target limit for everything else and stop at ALL solid occluders.
		float nearest = ShovelReach;
		foreach (var hit in Physics.RaycastAll(h.AimOrigin, h.AimDirection, nearest, -1, QueryTriggerInteraction.Collide))
		{
			var col = hit.collider;
			if (col == null || col.name.StartsWith("BPVR_") || hit.distance > nearest) continue;
			if(OpenTrunkShell(col))continue;
			var pick = MousePick.For(col);
			// Ignore non-interactive volume triggers, never solid walls/box surfaces.
			if (col.isTrigger && pick == null) continue;
			nearest = hit.distance;
			best = hit;
			clickable = pick != null && pick.CanClick && WithinInteractionReach(col,pick,hit.distance);
		}
		return best.collider != null;
	}

	private bool NativeParlorTarget(Hand source=null)
	{
		var h = source??_handR;
		return h != null && h.Valid && h.AimCollider != null &&
			h.AimCollider.name.StartsWith("puzshog", System.StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsPickupName(string name)
	{
		string n = (name ?? "").ToUpperInvariant();
		return System.Text.RegularExpressions.Regex.IsMatch(n, @"\b(COINS?|APPLES?|FRUIT|BANANAS?|ORANGES?|GEMS?|DIAMONDS?|KEYS?|DICE|DIE|PICKUPS?|COLLECTIBLES?)\b");
	}
	private static bool IsPickupTarget(Collider collider)
	{
		// Generic "Click Trigger" colliders inherit the item's identity from parents.
		for (var t = collider == null ? null : collider.transform; t != null; t = t.parent)
		{
			if(t.name=="Pickup Spawn Pools")break;
			if (t.name.Equals("_Pickup Items", System.StringComparison.OrdinalIgnoreCase) || IsPickupName(t.name)) return true;
			if (t.name == "_GAMEPLAY" || t.name == "_COLLIDERS" || t.name == "Room Spawn Pools") break;
		}
		return false;
	}
	private void HideHand(Hand h, ETrackedControllerRole role, string why)
	{
		_palmReady[h.IsLeft?0:1]=false;
		h.RayFilterReady = false;
		if (role == ETrackedControllerRole.RightHand && _cursorMarker != null) _cursorMarker.SetActive(false);
		if (h.Root != null && h.Root.activeSelf)
		{
			h.HiddenSince = Time.unscaledTime;
			if (_armGoneLog < 24)
			{
				_armGoneLog++;
				Log($"[arm] {role} hidden at frame {Time.frameCount}: {why}");
			}
		}
		HandRig.SetVisible(h, on: false);
	}

	private void PoseHand(Hand h, ETrackedControllerRole role, Vector3 anchorPos, Quaternion anchorRot)
	{
		if (h == null)
		{
			return;
		}
		uint num;
		try
		{
			num = ControllerIndex(role);
		}
		catch (System.Exception ex)
		{
			HideHand(h, role, "role lookup threw: " + ex.Message);
			return;
		}
		if (num == uint.MaxValue || num >= 64)
		{
			HideHand(h, role, $"no device for role (idx={num})");
			h.Valid = false;
			return;
		}
		TrackedDevicePose_t trackedDevicePose_t = _renderPoses[num];
		if (!trackedDevicePose_t.bPoseIsValid)
		{
			HideHand(h, role, $"pose not valid (device {num}, result {trackedDevicePose_t.eTrackingResult})");
			h.Valid = false;
			return;
		}
		h.DeviceIndex = num;
		h.Valid = true;
		if (h.Root != null && !h.Root.activeSelf && _armGoneLog < 24)
		{
			_armGoneLog++;
			Log($"[arm] {role} back after {Time.unscaledTime - h.HiddenSince:F2}s");
		}
		HandRig.SetVisible(h, on: true);
		HmdMatrix34_t mDeviceToAbsoluteTracking = trackedDevicePose_t.mDeviceToAbsoluteTracking;
		Vector3 vector = new Vector3(mDeviceToAbsoluteTracking.m3, mDeviceToAbsoluteTracking.m7, 0f - mDeviceToAbsoluteTracking.m11) - TrackingOrigin;
		Quaternion quaternion = ToUnity(mDeviceToAbsoluteTracking);
		if (_crouch > 0.001f)
		{
			vector += Vector3.down * _crouch;
		}
		Vector3 handPos = anchorPos + anchorRot * vector;
		Quaternion quaternion2 = anchorRot * quaternion;
		float num2 = ((role == ETrackedControllerRole.LeftHand) ? (-1f) : 1f);
		Vector3 shoulder = anchorPos + anchorRot * new Vector3(0.18f * num2, -0.22f, -0.04f);
		Vector3 pole = anchorRot * new Vector3(0.55f * num2, -1f, -0.25f).normalized;
		HandRig.Solve(h, shoulder, handPos, quaternion2, pole);
		UpdatePalmRay(h,handPos,quaternion2);
		Transform transform = ((HandRig.Model == null) ? null : (h.IsLeft ? HandRig.Model.HandL : HandRig.Model.HandR));
		if (transform != null)
		{
			ArmsModel model = HandRig.Model;
			string text = (h.IsLeft ? ".L" : ".R");
			Transform transform2 = model.Find("f_index.01" + text);
			Transform transform3 = model.Find("f_index.03" + text);
			Vector3 vector2;
			Vector3 aimOrigin;
			if (Fingers.AimKnown)
			{
				vector2 = transform.TransformDirection(h.IsLeft ? Fingers.AimDirLocalL : Fingers.AimDirLocalR).normalized;
				aimOrigin = transform.TransformPoint(h.IsLeft ? Fingers.AimFromLocalL : Fingers.AimFromLocalR);
			}
			else if (transform2 != null && transform3 != null && (transform3.position - transform2.position).sqrMagnitude > 1E-06f)
			{
				vector2 = (transform3.position - transform2.position).normalized;
				aimOrigin = transform3.position + vector2 * 0.03f;
			}
			else
			{
				vector2 = transform.up;
				aimOrigin = transform.position + vector2 * 0.05f;
			}
			if (Mathf.Abs(_pAimPitch.Value) > 0.01f)
			{
				Vector3 vector3 = Vector3.Cross(Vector3.up, vector2);
				if (vector3.sqrMagnitude < 1E-06f)
				{
					vector3 = transform.right;
				}
				vector2 = Quaternion.AngleAxis(0f - _pAimPitch.Value, vector3.normalized) * vector2;
			}
			h.AimDirection = vector2.normalized;
			h.AimOrigin = aimOrigin;
		}
		else
		{
			Quaternion quaternion3 = ((h.HandNode != null) ? h.HandNode.transform.rotation : quaternion2);
			h.AimOrigin = h.Tip.position;
			h.AimDirection = quaternion3 * Quaternion.Euler(_pAimPitch.Value - 40f, 0f, 0f) * Vector3.forward;
		}
		// Imported finger pose must never change the interaction ray.
		if (_importedArms)
		{
			Vector3 aim = quaternion2 * Vector3.forward;
			float blend = h.RayFilterReady ? 1f - Mathf.Exp(-Time.unscaledDeltaTime / .065f) : 1f;
			h.FilteredRayDirection = Vector3.Slerp(h.FilteredRayDirection, aim, blend).normalized;
			h.FilteredRayOrigin = Vector3.Lerp(h.FilteredRayOrigin, handPos, blend);
			h.RayFilterReady = true;
			h.AimDirection = h.FilteredRayDirection;
			h.AimOrigin = h.FilteredRayOrigin + h.AimDirection * .04f;
		}
		VRControllerState_t state = default(VRControllerState_t);
		bool flag;
		try
		{
			flag = _sys.GetControllerState(num, ref state, (uint)Marshal.SizeOf<VRControllerState_t>());
		}
		catch
		{
			flag = false;
		}
		h.TriggerWasDown = h.TriggerDown;
		h.GripWasDown = h.GripDown;
		if (h.IsLeft)
		{
			_leftGrip = h.GripDown;
		}
		else
		{
			_rightGrip = h.GripDown;
		}
		if (h.IsLeft && flag)
		{
			bool flag2 = (state.ulButtonPressed & 0x80) != 0;
			bool flag3 = (state.ulButtonPressed & 2) != 0;
			VRInput.DirectoryEdge = false; // X belongs to the journal now.
			VRInput.BlueprintEdge = false;
			VRInput.Directory = false;
			VRInput.Blueprint = false;
			_leftA = flag2;
			_leftB = flag3;
		}
		h.TriggerDown = flag && ((state.ulButtonPressed & 0x200000000L) != 0L || state.rAxis1.x > 0.6f);
		if (!h.TriggerDown) h.DirectClickConsumed = false;
		h.GripDown = flag && (state.ulButtonPressed & 4) != 0;
		float a = (flag ? Mathf.Clamp01(state.rAxis2.x) : 0f);
		float a2 = (flag ? Mathf.Clamp01(state.rAxis1.x) : 0f);
		h.GripAmount = a > 0f ? a : (h.GripDown ? 1f : 0f);
		h.TriggerAmount = a2 > 0f ? a2 : (h.TriggerDown ? 1f : 0f);
		bool flag4 = ((flag ? state.ulButtonTouched : 0) & 0x100000082L) != 0L || (state.ulButtonPressed & 0x100000080L) != 0;
		h.ThumbAmount = (flag4 ? 1f : 0f);
		_celHands?.Pose(h.IsLeft ^ _comfort.LeftHanded,handPos,quaternion2,h.GripAmount,h.TriggerAmount,h.ThumbAmount,false,1f,Mathf.Clamp(_comfort.Span*.03f,.035f,.075f),recorderHold:_quickMemo && !h.IsLeft);
		if (_comfortOpen || _comfortChord || _journalOpen || _journalBlockUntilRelease || (!h.IsLeft&&RightXShortcut()) || (_quickMemo && !h.IsLeft)) {
			if(!h.IsLeft)HideHeldTools();
			Fingers.Drive(h.IsLeft,h.IsLeft?h.GripAmount:1f,0f,1f,Time.unscaledDeltaTime,false,false);
			h.AimCollider=null;MousePick.Hover(h.IsLeft,null);Hover.Set(h.IsLeft,null);
			if(h.RayObj!=null)h.RayObj.SetActive(false);
			return;
		}
		float num3 = 3f;
		bool flag5 = false;
		bool grabbable = false;
		Vector3 vector4 = Vector3.zero;
		bool haveAimHit = AimHit(h, out var best, out var clickable);
		if(!clickable && ToolAimAssist(h,out var assisted,out var assistedReady)) {
			best=assisted;clickable=assistedReady;haveAimHit=true;
			h.AimDirection=(best.point-h.AimOrigin).normalized;
		}
		// Grip/reaching uses the broad palm query; a held trigger keeps the
		// forward pointing target, including collectibles. Both retain reach/occlusion.
		if (PalmPickupHit(h, out var palmHit, out var palmClickable, out var palmOrigin) && (!h.TriggerDown||IsKeyOrGemPickup(palmHit.collider))) {
			best=palmHit;clickable=palmClickable;haveAimHit=true;
			h.AimOrigin=palmOrigin;
			h.AimDirection=(best.point-palmOrigin).normalized;
		}
		if ((!clickable || InteractionBoxRoot(best.collider)!=null) && BoxInteractionAssist(h,out var boxHit)) {
			best=boxHit;clickable=true;haveAimHit=true;
			h.AimDirection=(best.point-h.AimOrigin).normalized;
		}
		if (haveAimHit)
		{
			num3 = best.distance;
			flag5 = true;
			vector4 = best.point;
			grabbable = clickable || IsGrabbable(best.collider);
			h.AimCollider = best.collider;
		}
		else
		{
			h.AimCollider = null;
		}
		PickEvents pickEvents = (flag5 ? MousePick.For(h.AimCollider) : null);
		if (pickEvents != null && pickEvents.Any)
		{
			grabbable = true;
		}
		if (flag5)
		{
			DoorProbe.Look(h.AimCollider);
		}
		if (role == ETrackedControllerRole.RightHand)
		{
			Snapshot.Note(h.AimCollider, num3, pickEvents);
			ProbeKeyboardForCapture(h);
		}
		if(_heldPump==null)MousePick.Hover(h.IsLeft, _inHouse ? pickEvents : null);
		bool flag6 = (VRInput.MenuMode = _uiQuad != null && _uiQuad.activeInHierarchy);
		Screens.NoteMenu(flag6);
		if(_heldPump==null)Hover.Set(h.IsLeft, _inHouse ? h.AimCollider : null);
		if (role == ETrackedControllerRole.RightHand)
		{
			Beam.LastInHouse = _inHouse;
			Beam.LastMenuUp = flag6;
			Beam.Set(h.AimOrigin, h.AimDirection, _inHouse && !_panelStick.Active && flag5);
			Beam.Report();
		}
		HandRig.UpdateRay(h, h.AimOrigin, h.AimDirection, num3, show: true);
		HandRig.SetGrabHint(h, grabbable, vector4, flag5);
		ShowPalmPickupBeam(h, vector4, PalmPickupAllowed);
		// Open-grab pose now matches the target squeezing grip will actually use.
		bool pickupReady = _inHouse && PalmPickupAllowed && flag5 && clickable && num3 <= PickupHoverReach && IsPickupTarget(h.AimCollider);
		bool typing = (_computerKeyboardMode && _activeComputer != null) || PumpPointPose(h);
		bool holdingTool=UpdateHeldTool(h,clickable);
		if(!typing&&!holdingTool&&!pickupReady)pickupReady=PumpGrabReady(h);
		bool triggerReady = FingerControls.PointReady(
			_inHouse && PalmPickupAllowed && flag5 && num3<=WorldInteractionReach(h.AimCollider) && !IsPickupTarget(h.AimCollider) &&
			((clickable && (IsDigTarget(h.AimCollider)?DigReady(pickEvents):Grab.Ready(pickEvents))) || (NativeParlorTarget(h) && ParlorLegacyGrab.Ready(h.AimCollider,pickEvents))),
			pickupReady,typing||holdingTool||_panelAdjusting||_heldPump!=null,h.GripAmount,h.TriggerAmount);
		Fingers.Drive(h.IsLeft, typing || holdingTool || triggerReady ? 1f : h.GripAmount, holdingTool ? 1f : typing || triggerReady ? 0f : h.TriggerAmount, typing || holdingTool || triggerReady ? 1f : h.ThumbAmount, Time.unscaledDeltaTime, !typing && !holdingTool && pickupReady, !holdingTool && !triggerReady && !flag4 && (state.ulButtonTouched & 0x200000000UL) == 0 && !pickupReady);
		_celHands?.Pose(h.IsLeft ^ _comfort.LeftHanded,handPos,quaternion2,typing?1f:h.GripAmount,typing?0f:h.TriggerAmount,h.ThumbAmount,!typing&&pickupReady,1f,Mathf.Clamp(_comfort.Span*.03f,.035f,.075f),triggerReady||typing,computerTyping:typing);
		h.AimPoint=vector4;h.AimNormal=best.normal;h.AimDistance=num3;h.AimClickable=clickable;
		UpdateTrunkHover(h,pickEvents);
		UpdateDigHover(h,pickEvents);
		UpdateDigCue(h,pickEvents,clickable);
		TargetFeedback(h,pickEvents,clickable && (pickupReady || triggerReady || holdingTool));
		h.DirectClickConsumed |= UpdatePumpButtons(h);
		h.DirectClickConsumed |= UpdatePumpHand(h);
		bool flag7 = _uiQuad != null && _uiQuad.activeInHierarchy && BeamCrossesPanel(h.AimOrigin, h.AimDirection, num3);
		bool gripPress=h.GripDown && !h.GripWasDown && !OtherGripHeld(h) && !_panelAdjusting;
		bool worldPress=(h.TriggerDown && !h.TriggerWasDown) || gripPress;
		if (!h.DirectClickConsumed && NativeParlorTarget(h) && PalmPickupAllowed && !_panelStick.Active && !flag7 && worldPress)
		{
			h.DirectClickConsumed = DirectHandClick(h,true);
		}
		if (!h.DirectClickConsumed && !_forcePanel && !_panelStick.Active && !_panelAdjusting && clickable && Cursor.lockState != CursorLockMode.None && worldPress && _pDirectGrab.Value && _inHouse && !flag7 && h.AimCollider != null)
		{
			h.DirectClickConsumed = DirectHandClick(h,false);
			if (h.DirectClickConsumed) NoteComputerOpened(h.AimCollider);
		}
		if (h.TriggerDown && !h.TriggerWasDown)
		{
			string value = "nothing";
			try
			{
				if (Physics.Raycast(h.AimOrigin, h.AimDirection, out best, 25f))
				{
					value = best.collider.name;
				}
			}
			catch
			{
			}
			Log($"{role} trigger -> aiming at '{value}' at {num3:F2}m");
		}
		if (_handLogged < 2)
		{
			_handLogged++;
			Log($"{role} tracked, device index {num}");
		}
	}

	private static bool LooksLikeOverlay(Camera c)
	{
		try
		{
			if (c.orthographic)
			{
				return true;
			}
			if (c.targetTexture != null)
			{
				return true;
			}
			if (c.clearFlags == CameraClearFlags.Depth || c.clearFlags == CameraClearFlags.Nothing)
			{
				return true;
			}
			if (c.transform.position.y < -500f)
			{
				return true;
			}
			string text = c.name.ToUpperInvariant();
			if (text.Contains("UI") || text.Contains("OVERLAY") || text.Contains("BRIGHT") || text.Contains("MAG") || text.Contains("DRAFT") || text.Contains("CURSOR"))
			{
				return true;
			}
		}
		catch
		{
			return true;
		}
		return false;
	}

	private static bool IsGrabbable(Collider col)
	{
		if (col == null)
		{
			return false;
		}
		try
		{
			Transform transform = col.transform;
			int num = 0;
			while (num < 4 && transform != null)
			{
				foreach (Component component in transform.GetComponents<Component>())
				{
					if (component == null)
					{
						continue;
					}
					string name = component.GetIl2CppType().Name;
					for (int i = 0; i < GrabMarkers.Length; i++)
					{
						if (name.IndexOf(GrabMarkers[i], System.StringComparison.OrdinalIgnoreCase) >= 0)
						{
							return true;
						}
					}
				}
				num++;
				transform = transform.parent;
			}
		}
		catch
		{
		}
		return false;
	}

	private void SyncEyeEffects(Camera gameCam)
	{
		string text = _pEffects.Value ?? "";
		string text2 = gameCam.name + "|" + text;
		if (_effectsFor == text2)
		{
			return;
		}
		_effectsFor = text2;
		if (text.Trim().Length == 0)
		{
			Log("CopyEffects empty - eyes stay ungraded");
			return;
		}
		string[] array = text.Split(',');
		for (int i = 0; i < 2; i++)
		{
			GameObject gameObject = ((_eyeCam[i] == null) ? null : _eyeCam[i].gameObject);
			if (gameObject == null)
			{
				continue;
			}
			foreach (Component component2 in gameObject.GetComponents<Component>())
			{
				if (!(component2 == null) && !(component2.TryCast<Camera>() != null) && !(component2.TryCast<Transform>() != null))
				{
					UnityEngine.Object.DestroyImmediate(component2);
				}
			}
			foreach (Component component3 in gameCam.GetComponents<Component>())
			{
				if (component3 == null)
				{
					continue;
				}
				Il2CppSystem.Type il2CppType = component3.GetIl2CppType();
				string name = il2CppType.Name;
				bool flag = false;
				for (int j = 0; j < array.Length; j++)
				{
					if (string.Equals(array[j].Trim(), name, System.StringComparison.OrdinalIgnoreCase))
					{
						flag = true;
					}
				}
				if (!flag)
				{
					continue;
				}
				try
				{
					Component component = gameObject.AddComponent(il2CppType);
					int value = CopyFields(component3, component, il2CppType);
					Behaviour behaviour = component.TryCast<Behaviour>();
					bool value2 = false;
					if (behaviour != null)
					{
						Behaviour behaviour2 = component3.TryCast<Behaviour>();
						behaviour.enabled = false;
						behaviour.enabled = behaviour2 == null || behaviour2.enabled;
						value2 = true;
					}
					Log($"eye {i}: added '{name}' ({value} values copied, reinit={value2}, enabled={((behaviour == null) ? "n/a" : behaviour.enabled.ToString())})");
				}
				catch (System.Exception ex)
				{
					LogWarn($"eye {i}: could not add '{name}': {ex.Message}");
				}
			}
		}
	}

	private static int CopyFields(Component src, Component dst, Il2CppSystem.Type t)
	{
		int num = 0;
		Il2CppReferenceArray<Il2CppSystem.Reflection.FieldInfo> fields = t.GetFields(Il2CppSystem.Reflection.BindingFlags.Instance | Il2CppSystem.Reflection.BindingFlags.Public | Il2CppSystem.Reflection.BindingFlags.NonPublic);
		for (int i = 0; i < fields.Length; i++)
		{
			Il2CppSystem.Reflection.FieldInfo fieldInfo = fields[i];
			if (!(fieldInfo == null) && !fieldInfo.IsStatic)
			{
				try
				{
					fieldInfo.SetValue(dst, fieldInfo.GetValue(src));
					num++;
				}
				catch
				{
				}
			}
		}
		return num;
	}

	private void SampleEyeTexture()
	{
		try
		{
			RenderTexture renderTexture = _eyeRT[0];
			if (renderTexture == null || !renderTexture.IsCreated())
			{
				Log("[probe] eye RT missing");
				return;
			}
			if (_probeTex == null)
			{
				_probeTex = new Texture2D(8, 8, TextureFormat.RGBA32, mipChain: false);
				UnityEngine.Object.DontDestroyOnLoad(_probeTex);
			}
			RenderTexture active = RenderTexture.active;
			RenderTexture.active = renderTexture;
			int num = Mathf.Max(0, renderTexture.width / 2 - 4);
			int num2 = Mathf.Max(0, renderTexture.height / 2 - 4);
			_probeTex.ReadPixels(new Rect(num, num2, 8f, 8f), 0, 0, recalculateMipMaps: false);
			_probeTex.Apply(updateMipmaps: false);
			RenderTexture.active = active;
			float num3 = 0f;
			float num4 = 0f;
			float num5 = 0f;
			float num6 = 0f;
			for (int i = 0; i < 8; i++)
			{
				for (int j = 0; j < 8; j++)
				{
					Color pixel = _probeTex.GetPixel(i, j);
					num3 += pixel.r;
					num4 += pixel.g;
					num5 += pixel.b;
					num6 += pixel.a;
				}
			}
			Log($"[probe] eye centre rgba=({num3 / 64f:F3},{num4 / 64f:F3},{num5 / 64f:F3},{num6 / 64f:F3}) rt={renderTexture.width}x{renderTexture.height} sRGB={renderTexture.sRGB}");
		}
		catch (System.Exception ex)
		{
			LogWarn("[probe] " + ex.Message);
		}
	}

	private void SaveRT(RenderTexture rt, string path)
	{
		Texture2D texture2D = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, mipChain: false);
		RenderTexture active = RenderTexture.active;
		RenderTexture.active = rt;
		texture2D.ReadPixels(new Rect(0f, 0f, rt.width, rt.height), 0, 0, recalculateMipMaps: false);
		texture2D.Apply(updateMipmaps: false);
		RenderTexture.active = active;
		File.WriteAllBytes(path, texture2D.EncodeToPNG());
		UnityEngine.Object.Destroy(texture2D);
	}

	private void DumpFrame()
	{
		try
		{
			string text = Path.Combine(Application.dataPath, "..", "UserData");
			Directory.CreateDirectory(text);
			RenderTexture renderTexture = _eyeRT[0];
			if (renderTexture == null || !renderTexture.IsCreated())
			{
				LogWarn("[dump] no eye texture");
				return;
			}
			SaveRT(renderTexture, Path.Combine(text, "eye_dump.png"));
			Log($"[dump] wrote eye_dump.png ({renderTexture.width}x{renderTexture.height}, sRGB={renderTexture.sRGB})");
			Camera dumpCam = _dumpCam;
			if (!(dumpCam == null))
			{
				RenderTexture targetTexture = dumpCam.targetTexture;
				Rect rect = dumpCam.rect;
				try
				{
					_rendering = true;
					dumpCam.rect = new Rect(0f, 0f, 1f, 1f);
					dumpCam.targetTexture = renderTexture;
					float num = Mathf.Max(0.02f, dumpCam.nearClipPlane);
					float far = Mathf.Max(num + 1f, dumpCam.farClipPlane);
					Matrix4x4 inverse = Matrix4x4.TRS(_dumpPos, _dumpRot, new Vector3(1f, 1f, -1f)).inverse;
					Matrix4x4 projectionMatrix = EyeProjection(EVREye.Left, num, far);
					dumpCam.worldToCameraMatrix = inverse;
					dumpCam.projectionMatrix = projectionMatrix;
					dumpCam.Render();
					SaveRT(renderTexture, Path.Combine(text, "dump_A_view_vrproj.png"));
					dumpCam.worldToCameraMatrix = inverse;
					dumpCam.ResetProjectionMatrix();
					dumpCam.Render();
					SaveRT(renderTexture, Path.Combine(text, "dump_B_view_gameproj.png"));
					dumpCam.ResetWorldToCameraMatrix();
					dumpCam.projectionMatrix = projectionMatrix;
					dumpCam.Render();
					SaveRT(renderTexture, Path.Combine(text, "dump_C_gameview_vrproj.png"));
					dumpCam.ResetWorldToCameraMatrix();
					dumpCam.ResetProjectionMatrix();
					dumpCam.Render();
					SaveRT(renderTexture, Path.Combine(text, "dump_D_untouched.png"));
					Log("[dump] wrote variants A-D");
				}
				finally
				{
					dumpCam.targetTexture = targetTexture;
					dumpCam.rect = rect;
					dumpCam.ResetWorldToCameraMatrix();
					dumpCam.ResetProjectionMatrix();
					dumpCam.ResetAspect();
					_rendering = false;
				}
				ScreenCapture.CaptureScreenshot(Path.Combine(text, "screen_dump.png"));
				Log("[dump] requested screen_dump.png");
			}
		}
		catch (System.Exception ex)
		{
			LogWarn("[dump] " + ex.Message);
		}
	}

	private void SubmitEye(EVREye eye, int i)
	{
		RenderTexture renderTexture = _eyeRT[i];
		if (renderTexture == null)
		{
			return;
		}
		if (!renderTexture.IsCreated())
		{
			renderTexture.Create();
			_eyeTexPtr[i] = System.IntPtr.Zero;
			LogWarn($"eye {i} RenderTexture had been released - recreated");
			return;
		}
		// Preserve the working baseline's render-thread synchronization before
		// handing the texture to the external compositor. Do not cache this call
		// until the multithreaded Unity/OpenVR submission path has been profiled.
		System.IntPtr nativeTexturePtr = renderTexture.GetNativeTexturePtr();
		if (nativeTexturePtr != System.IntPtr.Zero)
		{
			if (nativeTexturePtr != _eyeTexPtr[i])
			{
				Log($"eye {i} native texture moved 0x{_eyeTexPtr[i].ToInt64():X} -> 0x{nativeTexturePtr.ToInt64():X}");
				_eyeTexPtr[i] = nativeTexturePtr;
			}
			Texture_t tex = new Texture_t
			{
				handle = nativeTexturePtr,
				eType = 0,
				eColorSpace = _pColorSpace.Value
			};
			VRTextureBounds_t bounds = (_pFlipY.Value ? new VRTextureBounds_t
			{
				uMin = 0f,
				vMin = 1f,
				uMax = 1f,
				vMax = 0f
			} : new VRTextureBounds_t
			{
				uMin = 0f,
				vMin = 0f,
				uMax = 1f,
				vMax = 1f
			});
			int num = _comp.Submit(eye, ref tex, ref bounds, 0);
			_lastSubmitErr[i] = num;
			if (num != 0)
			{
				_submitFails[i]++;
			}
			if (_framesRendered <= 5)
			{
				Log($"Submit({eye}) tex=0x{nativeTexturePtr.ToInt64():X} err={num} flipY={_pFlipY.Value}");
			}
			else if (num != _prevSubmitErr[i])
			{
				Log($"Submit({eye}) result changed {_prevSubmitErr[i]} -> {num} ({CompositorError(num)}) at frame {_framesRendered}");
			}
			else if (num != 0 && _framesRendered % 300 == 0)
			{
				Log($"Submit({eye}) still failing: {num} ({CompositorError(num)}), {_submitFails[i]} total");
			}
			_prevSubmitErr[i] = num;
		}
	}

	private Camera ResolveGameCamera()
	{
		var visionAnchor=VisionAnchor();
		if(visionAnchor!=null)return visionAnchor;
		var coatView=CoatCheckView();
		if(coatView!=null)return coatView;
		Camera main = Camera.main;
		if (main != null && !LooksLikeOverlay(main))
		{
			if (_lastCamName != main.name)
			{
				_lastCamName = main.name;
				Log("world camera: '" + main.name + "'");
				try
				{
					Log($"   allowHDR={main.allowHDR} allowMSAA={main.allowMSAA} clear={main.clearFlags} bg={main.backgroundColor}");
					Il2CppArrayBase<Component> components = main.GetComponents<Component>();
					string text = "";
					for (int i = 0; i < components.Length; i++)
					{
						if (!(components[i] == null))
						{
							if (text.Length > 0)
							{
								text += ", ";
							}
							text += components[i].GetIl2CppType().Name;
						}
					}
					Log("   effects on it (we do not inherit these): " + text);
				}
				catch (System.Exception ex)
				{
					LogWarn("cam report: " + ex.Message);
				}
			}
			return main;
		}
		Il2CppReferenceArray<Camera> allCameras = Camera.allCameras;
		Camera stableWorld=null;
		for (int j = 0; j < allCameras.Length; j++)
		{
			Camera camera = allCameras[j];
			if (camera!=null && camera.isActiveAndEnabled && camera.targetTexture==null && !LooksLikeOverlay(camera))
			{
				if(stableWorld==null || camera.depth>stableWorld.depth ||
					(camera.depth==stableWorld.depth && camera.GetInstanceID()<stableWorld.GetInstanceID()))stableWorld=camera;
			}
		}
		if(stableWorld!=null) {
			if(_lastCamName!=stableWorld.name){_lastCamName=stableWorld.name;Log("world camera (stable fallback): '"+stableWorld.name+"'");}
			return stableWorld;
		}
		if (_fallbackCam != null && _fallbackCam.isActiveAndEnabled && _fallbackCam.targetTexture == null)
		{
			return _fallbackCam;
		}
		Camera camera2 = null;
		for (int k = 0; k < allCameras.Length; k++)
		{
			Camera camera3 = allCameras[k];
			if (!(camera3 == null) && camera3.isActiveAndEnabled && !(camera3.targetTexture != null))
			{
				if (camera2 == null)
				{
					camera2 = camera3;
				}
				else if (camera3.depth < camera2.depth)
				{
					camera2 = camera3;
				}
				else if (camera3.depth == camera2.depth && string.CompareOrdinal(camera3.name, camera2.name) < 0)
				{
					camera2 = camera3;
				}
			}
		}
		_fallbackCam = camera2;
		if (camera2 != null && _lastCamName != camera2.name)
		{
			_lastCamName = camera2.name;
			Log("no world camera - drawing the menu through '" + camera2.name + "'");
		}
		return camera2;
	}

	private bool BeamCrossesPanel(Vector3 origin, Vector3 dir, float targetDist)
	{
		if (_uiQuad == null)
		{
			return false;
		}
		try
		{
			Transform transform = _uiQuad.transform;
			Vector3 rhs = -transform.forward;
			float num = Vector3.Dot(dir, rhs);
			if (Mathf.Abs(num) < 1E-06f)
			{
				return false;
			}
			float num2 = Vector3.Dot(transform.position - origin, rhs) / num;
			if (num2 <= 0f || (targetDist > 0f && num2 > targetDist))
			{
				return false;
			}
			Vector3 vector = transform.InverseTransformPoint(origin + dir * num2);
			return Mathf.Abs(vector.x) <= 0.5f && Mathf.Abs(vector.y) <= 0.5f;
		}
		catch
		{
			return false;
		}
	}

	private void EnsureEyeRigs()
	{
		if (_rig != null && _eyeCam[0] != null && _eyeCam[1] != null && _eyeRT[0] != null && _eyeRT[1] != null && _eyeRT[0].IsCreated() && _eyeRT[1].IsCreated())
		{
			return;
		}
		if (_rig == null)
		{
			_rig = new GameObject("BluePrinceVR_Rig");
			UnityEngine.Object.DontDestroyOnLoad(_rig);
		}
		for (int i = 0; i < 2; i++)
		{
			if (_eyeRT[i] != null && !_eyeRT[i].IsCreated())
			{
				LogWarn($"eye {i} texture lost its GPU surface - recreating");
				_eyeRT[i].Create();
				_eyeTexPtr[i] = _eyeRT[i].GetNativeTexturePtr();
			}
			if (_eyeRT[i] == null)
			{
				if (_rtBuilds[i] > 0)
				{
					LogWarn($"eye {i} texture was destroyed (build #{_rtBuilds[i] + 1}) - " + "a scene load collected it; rebuilding");
				}
				_rtBuilds[i]++;
				int value = _pEyeTexMode.Value;
				RenderTexture renderTexture = new RenderTexture((int)_eyeW, (int)_eyeH, 24, RenderTextureFormat.ARGB32, value switch
				{
					2 => RenderTextureReadWrite.sRGB, 
					1 => RenderTextureReadWrite.Linear, 
					_ => RenderTextureReadWrite.Default, 
				});
				renderTexture.antiAliasing = 1;
				renderTexture.hideFlags = HideFlags.HideAndDontSave;
				renderTexture.Create();
				UnityEngine.Object.DontDestroyOnLoad(renderTexture);
				Log($"eye {i} texture: mode={value} rt.sRGB={renderTexture.sRGB} projectColourSpace={QualitySettings.activeColorSpace} submitAs={_pColorSpace.Value}");
				_eyeRT[i] = renderTexture;
				_eyeTexPtr[i] = renderTexture.GetNativeTexturePtr();
				Log($"eye {i} RT {_eyeW}x{_eyeH} native=0x{_eyeTexPtr[i].ToInt64():X}");
			}
			if (_eyeCam[i] == null)
			{
				GameObject gameObject = new GameObject((i == 0) ? "BPVR_EyeL" : "BPVR_EyeR");
				gameObject.transform.SetParent(_rig.transform, worldPositionStays: false);
				Camera camera = gameObject.AddComponent<Camera>();
				camera.enabled = false;
				_eyeCam[i] = camera;
				Log($"eye camera {i} created");
			}
		}
	}

	public override void OnSceneWasLoaded(int buildIndex, string sceneName)
	{
		ReleasePump();
		if(_quickMemo)CloseQuickMemo();
		_floorPlanButton.Update(true,true,Time.unscaledTime);
		_floorPlanTabUntil=_floorPlanPauseUntil=0;
		_crouchToggle.Update(false,false,true);_crouchHeld=false;
		if(_journalOpen) CloseJournal();
		_codexBootScene = string.Equals(sceneName, "Initialize", System.StringComparison.OrdinalIgnoreCase);
		_mainMenuScene = string.Equals(sceneName, "Main Menu", System.StringComparison.OrdinalIgnoreCase);
		_panelStick.Reset();
		_magnifierPointer=false;
		ReleaseComputerKeys();
		Log("[Codex startup] " + sceneName + (_codexBootScene ? ": VR camera/input work suspended until the next scene." : ": VR camera/input work resumed."));
		_sceneGrace = 30;
		_lastCamName = null;
		ClickFromTrigger(null);
		_pacingApplied = false;
		Log($"scene '{sceneName}' - pausing VR render for {_sceneGrace} frames");
		Intro.SceneLoaded(sceneName);
	}

	public override void OnApplicationQuit()
	{
		ReleaseComputerKeys();
		CancelMemo();
		SaveJournal();
		ReleaseCodex();
		if (_vrReady)
		{
			try
			{
				OpenVRNative.VR_ShutdownInternal();
			}
			catch
			{
			}
			_vrReady = false;
		}
	}

	private static Quaternion ToUnity(HmdMatrix34_t m)
	{
		Quaternion result = default(Quaternion);
		result.w = Mathf.Sqrt(Mathf.Max(0f, 1f + m.m0 + m.m5 + m.m10)) * 0.5f;
		result.x = Mathf.Sqrt(Mathf.Max(0f, 1f + m.m0 - m.m5 - m.m10)) * 0.5f;
		result.y = Mathf.Sqrt(Mathf.Max(0f, 1f - m.m0 + m.m5 - m.m10)) * 0.5f;
		result.z = Mathf.Sqrt(Mathf.Max(0f, 1f - m.m0 - m.m5 + m.m10)) * 0.5f;
		result.x = CopySign(result.x, 0f - (m.m9 - m.m6));
		result.y = CopySign(result.y, 0f - (m.m2 - m.m8));
		result.z = CopySign(result.z, m.m4 - m.m1);
		return result;
	}

	private Matrix4x4 EyeProjection(EVREye eye, float near, float far)
	{
		Matrix4x4 result = ToUnity(_sys.GetProjectionMatrix(eye, near, far));
		if (_pProjectionMode.Value == 2)
		{
			return result;
		}
		float m = result.m00;
		float m2 = result.m02;
		float m3 = result.m11;
		float m4 = result.m12;
		if (_pProjectionMode.Value == 1 || Mathf.Abs(m) < 1E-06f || Mathf.Abs(m3) < 1E-06f)
		{
			float l = 0f;
			float r = 0f;
			float t = 0f;
			float b = 0f;
			_sys.GetProjectionRaw(eye, ref l, ref r, ref t, ref b);
			float num = 0f - t;
			float num2 = 0f - b;
			if (num < num2)
			{
				num = t;
				num2 = b;
			}
			LogProj(eye, l * near, r * near, num2 * near, num * near, near, far, "raw tangents");
			return Matrix4x4.Frustum(l * near, r * near, num2 * near, num * near, near, far);
		}
		float num3 = 2f * near / m;
		float num4 = m2 * num3;
		float num5 = (num4 + num3) * 0.5f;
		float num6 = (num4 - num3) * 0.5f;
		float num7 = 2f * near / m3;
		float num8 = m4 * num7;
		float num9 = (num8 + num7) * 0.5f;
		float num10 = (num8 - num7) * 0.5f;
		if (num9 < num10)
		{
			float num11 = num9;
			num9 = num10;
			num10 = num11;
		}
		LogProj(eye, num6, num5, num10, num9, near, far, "from OpenVR matrix");
		return Matrix4x4.Frustum(num6, num5, num10, num9, near, far);
	}

	private void LogProj(EVREye eye, float l, float r, float b, float t, float near, float far, string how)
	{
		if (!_projLogged)
		{
			_projLogged = true;
			Log($"[proj] {eye} via {how}: l={l:F4} r={r:F4} b={b:F4} t={t:F4} near={near:F3} far={far:F1}");
			Log($"[proj] half-FOV deg: left={Mathf.Atan2(0f - l, near) * 57.29578f:F1} right={Mathf.Atan2(r, near) * 57.29578f:F1} up={Mathf.Atan2(t, near) * 57.29578f:F1} down={Mathf.Atan2(0f - b, near) * 57.29578f:F1}");
			Log($"[proj] frustum aspect={(r - l) / (t - b):F4} eye texture aspect={(float)_eyeW / (float)_eyeH:F4} ({_eyeW}x{_eyeH})");
		}
	}

	private static Matrix4x4 ToUnity(HmdMatrix44_t m)
	{
		return new Matrix4x4
		{
			m00 = m.m0,
			m01 = m.m1,
			m02 = m.m2,
			m03 = m.m3,
			m10 = m.m4,
			m11 = m.m5,
			m12 = m.m6,
			m13 = m.m7,
			m20 = m.m8,
			m21 = m.m9,
			m22 = m.m10,
			m23 = m.m11,
			m30 = m.m12,
			m31 = m.m13,
			m32 = m.m14,
			m33 = m.m15
		};
	}

	private static string CompositorError(int e)
	{
		return e switch
		{
			0 => "None", 
			1 => "RequestFailed", 
			100 => "IncompatibleVersion", 
			101 => "DoNotHaveFocus", 
			102 => "InvalidTexture", 
			103 => "IsNotSceneApplication", 
			104 => "TextureIsOnWrongDevice", 
			105 => "TextureUsesUnsupportedFormat", 
			106 => "SharedTexturesNotSupported", 
			107 => "IndexOutOfRange", 
			108 => "AlreadySubmitted", 
			109 => "InvalidBounds", 
			110 => "AlreadySet", 
			_ => "unknown", 
		};
	}

	private static float CopySign(float size, float sign)
	{
		if (!(sign >= 0f))
		{
			return 0f - Mathf.Abs(size);
		}
		return Mathf.Abs(size);
	}

	private void Log(string s)
	{
		MelonLogger.Msg(s);
	}

	private void LogWarn(string s)
	{
		MelonLogger.Warning(s);
	}

	private void LogErr(string s)
	{
		MelonLogger.Error(s);
	}
}
