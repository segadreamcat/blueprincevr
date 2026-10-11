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
using Il2CppSystem.Collections.Generic;
using Il2CppSystem.Reflection;
using Il2CppTMPro;
using MelonLoader;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


namespace BluePrinceVR;

// Original 2.2 interaction implementation, invoked ONLY for observed Parlor box colliders.
internal static class ParlorLegacyGrab
{
	internal static bool Ready(Collider col,PickEvents pick)
	{
		if(Grab.Ready(pick))return true;
		try {
			var fsm=pick?.Fsm??Find(col);
			if(fsm==null||!fsm.isActiveAndEnabled||fsm.Fsm.ActiveState==null)return false;
			var names=new System.Collections.Generic.List<string>();
			foreach(var transition in fsm.Fsm.ActiveState.Transitions) {
				if(transition==null||string.IsNullOrEmpty(transition.ToState))continue;
				string name=transition.EventName;
				if(name=="MOUSE DOWN"||name=="MOUSE UP AS BUTTON")return true;
				if(!string.IsNullOrEmpty(name))names.Add(name);
			}
			return !string.IsNullOrEmpty(BestClickEvent(names));
		}catch{return false;}
	}
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

	private static readonly System.Collections.Generic.HashSet<string> _dumped = new System.Collections.Generic.HashSet<string>();

	private static PickEvents _pending;

	private static int _pendingFrames;

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
		if (_pending == null || --_pendingFrames > 0)
		{
			return;
		}
		PickEvents pending = _pending;
		_pending = null;
		try
		{
			string text = SafeState(pending.Fsm);
			MousePick.Send(pending.Fsm, pending.Down);
			string text2 = SafeState(pending.Fsm);
			if (text2 != text)
			{
				if (_log++ < 40)
				{
					MelonLogger.Msg($"[grab] deferred click: {text} -('{pending.Down}')-> {text2}");
				}
				return;
			}
			Fsm fsm = pending.Fsm.Fsm;
			if (!IsPointerState(fsm, text))
			{
				if (_log++ < 40)
				{
					MelonLogger.Msg($"[grab] '{pending.Fsm.FsmName}' is busy in '{text}' - leaving it alone");
				}
				return;
			}
			string text3 = ClickDestination(fsm, pending.Down);
			if (text3 == null)
			{
				if (_log++ < 40)
				{
					MelonLogger.Msg($"[grab] nothing in '{pending.Fsm.FsmName}' goes anywhere on '{pending.Down}'");
				}
				return;
			}
			fsm.SetState(text3);
			try
			{
				fsm.UpdateStateChanges();
			}
			catch
			{
			}
			if (_log++ < 40)
			{
				MelonLogger.Msg($"[grab] drove '{pending.Fsm.gameObject.name}' from '{text}' to '{text3}' - now '{SafeState(pending.Fsm)}'");
			}
		}
		catch (System.Exception ex)
		{
			if (_log++ < 40)
			{
				MelonLogger.Warning("[grab] pending: " + ex.Message);
			}
		}
	}

	public static bool Click(Collider col)
	{
		try
		{
			PickEvents pickEvents = MousePick.For(col);
			if (pickEvents != null && pickEvents.CanClick)
			{
				string text = SafeState(pickEvents.Fsm);
				MousePick.Send(pickEvents.Fsm, pickEvents.Over);
				string value = SafeState(pickEvents.Fsm);
				MousePick.Send(pickEvents.Fsm, pickEvents.Down);
				string value2 = SafeState(pickEvents.Fsm);
				MousePick.Send(pickEvents.Fsm, pickEvents.Up);
				string text2 = SafeState(pickEvents.Fsm);
				if (_log < 40)
				{
					_log++;
					MelonLogger.Msg($"[grab] CLICK '{col.name}': {text} -over('{pickEvents.Over}')-> {value} -down('{pickEvents.Down}')-> {value2} -up('{pickEvents.Up}')-> {text2}");
				}
				if (text2 == text)
				{
					DumpGraph(pickEvents.Fsm, col.name + "@" + text);
					DumpAncestors(col);
					Hover.Force(pickEvents.Fsm, 0.6f);
					_pending = pickEvents;
					_pendingFrames = 4;
				}
				return true;
			}
			if (pickEvents != null)
			{
				if (_log < 40)
				{
					_log++;
					MelonLogger.Msg("[grab] CLICK '" + col.name + "' - hover-only, nothing to send");
				}
				DumpGraph(pickEvents.Fsm, col.name);
			}
			PlayMakerFSM playMakerFSM = Find(col);
			if (playMakerFSM == null)
			{
				if (_log < 12)
				{
					_log++;
					MelonLogger.Msg("[grab] no state machine on '" + col?.name + "'");
				}
				return false;
			}
			System.Collections.Generic.List<string> list = Listening(playMakerFSM);
			bool flag = false;
			foreach (string item in list)
			{
				if (IsMouse(item))
				{
					flag = true;
					break;
				}
			}
			if (_log < 20)
			{
				_log++;
				StringBuilder stringBuilder = new StringBuilder();
				foreach (string item2 in list)
				{
					stringBuilder.Append('\'').Append(item2).Append("' ");
				}
				MelonLogger.Msg($"[grab] no MousePickEvent on '{playMakerFSM.FsmName}'; falling back, listens: {stringBuilder}");
			}
			if (flag)
			{
				string[] clickEvents = ClickEvents;
				foreach (string eventName in clickEvents)
				{
					try
					{
						playMakerFSM.SendEvent(eventName);
					}
					catch
					{
					}
				}
				return true;
			}
			string text3 = BestClickEvent(list);
			if (!string.IsNullOrEmpty(text3))
			{
				playMakerFSM.SendEvent(text3);
				return true;
			}
			int num = 0;
			foreach (string item3 in list)
			{
				if (IsSafeEvent(item3))
				{
					playMakerFSM.SendEvent(item3);
					num++;
				}
			}
			if (num == 0)
			{
				string[] clickEvents = ClickEvents;
				foreach (string eventName2 in clickEvents)
				{
					try
					{
						playMakerFSM.SendEvent(eventName2);
					}
					catch
					{
					}
				}
			}
			return true;
		}
		catch (System.Exception ex)
		{
			if (_log++ < 6)
			{
				MelonLogger.Warning("[grab] " + ex.Message);
			}
			return false;
		}
	}

	public static void Tick()
	{
	}
}
