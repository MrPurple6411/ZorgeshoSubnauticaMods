namespace CustomHotkeys;

using System;

using UnityEngine;
using Nautilus.Options;

using Common;
using Common.Reflection;
using Common.Configuration;
using Nautilus.Utility;
using static Nautilus.Utility.KeyCodeUtils;
using BindCallback = System.Action<GameInput.Device, GameInput.Button, GameInput.BindingSet, string>;

class KeyWModBindOption: Options.ModOption
{
	public class Tag: MonoBehaviour { }
	uGUI_Binding bind1, bind2;

	public KeyWModBindOption(Config.Field cfgField, string label) : base(cfgField, label) { }

	public override void addOption(Options options)
	{
		options.AddItem(ModKeybindOption.Create(id, label, GameInput.Device.Keyboard, KeyCode.A));
	}

	public override void onValueChange(EventArgs e) { }

	void onValueChange()
	{
		static KeyCode _getKeyCode(uGUI_Binding bind)
		{
			if (bind.value.isNullOrEmpty())
				return default;

			var keyCode = StringToKeyCode(bind.value);

			if (keyCode == KeyCode.AltGr)
			{
				keyCode = KeyCode.RightAlt;
				bind.value = keyCode.ToString(); // will resend event (field action will run once anyway)
			}
			else if (keyCode == KeyCode.None)
			{
				bind.value = ""; // in case of unsupported binds (e.g. mouse wheel)
			}

			return keyCode;
		}

		cfgField.value = new KeyWithModifier(_getKeyCode(bind1), _getKeyCode(bind2));
	}

	public override void onGameObjectChange(GameObject go)
	{
		uGUI_Bindings bindings = go.GetComponentInChildren<uGUI_Bindings>();
		bind1 = bindings.bindings[0];

		GameObject bind2GO = bindings.gameObject.createChild(bind1.gameObject);
		bind2 = bindings.bindings[1] = bind2GO.GetComponent<uGUI_Binding>();
		bind1.action = bind2.action = GameInput.Button.None;
		var keyValue = cfgField.value.cast<KeyWithModifier>();

		if (keyValue.modifier != KeyCode.None)
		{
			bind1.value = KeyCodeUtils.KeyCodeToString(keyValue.modifier);
			bind2.value = KeyCodeUtils.KeyCodeToString(keyValue.key);
		}
		else
		{
			bind1.value = keyValue.key != KeyCode.None ? KeyCodeUtils.KeyCodeToString(keyValue.key) : "";
			bind2.value = "";
		}
		bind1.gameObject.AddComponent<Tag>();
		bind2.gameObject.AddComponent<Tag>();

		BindCallback _getCallback(uGUI_Binding bind) => new((_, _, _, s) =>
		{
			bind.value = s;
			onValueChange();
			bind.RefreshValue();
		});

		bind1.bindCallback = _getCallback(bind1);
		bind2.bindCallback = _getCallback(bind2);
		base.onGameObjectChange(go);
	}
}