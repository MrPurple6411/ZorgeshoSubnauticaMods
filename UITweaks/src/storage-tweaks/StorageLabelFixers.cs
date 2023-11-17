﻿namespace UITweaks.StorageTweaks;

using System.Collections;
using System.Text.RegularExpressions;

using UnityEngine;
using UnityEngine.UI;

using Common;
using Text = TMPro.TMP_Text;

static partial class StorageLabelFixers
{
	public static bool tweakEnabled => Main.config.storageTweaks.enabled && Main.config.storageTweaks.multilineLabels;

	abstract class Fixer: MonoBehaviour
	{
		static readonly Regex spaceRemover = new ("\\s+", RegexOptions.Compiled);

		protected Text text;
		protected RectTransform textTransform;
		protected uGUI_InputField inputField;

		protected IStorageLabelInfo labelInfo;

		void valueListener(string str)
		{
			if (str != null)
			{
				str = str.Replace("\n", "");
				str = spaceRemover.Replace(str, " "); // consecutive spaces are buggy, replacing them with one space

				if (inputField.text != str)
				{																						"StorageLabelFixers.Fixer: changing label in valueListener".logDbg();
					inputField.text = str;
					return;
				}
			}

			using var _ = Common.Debug.profiler("StorageLabelFixer");

			fixLabel();
			ensureMaxLineCount(labelInfo.maxLineCount);
		}

		protected abstract void fixLabel();

		protected virtual void ensureMaxLineCount(int maxLineCount)
		{
			// SN approach doesn't work here because of the bug in TMP_Text (see 'Utils.forceRedraw')
			// will remove entire last word instead of just extra chars
			// also, can't use 'inputField.lineLimit', need more control over this
			if (text.getLineCount() > maxLineCount)
				inputField.text = text.text.Remove(text.getFirstCharIndexAtLine(maxLineCount));
		}

		void Awake()
		{
			if (!tweakEnabled)
				Destroy(this);
		}

		protected virtual void Start()
		{																								$"StorageLabelFixers.Fixer: Start (type: {GetType()})".logError();
			inputField = GetComponent<IStorageLabel>()?.label.signInput.inputField;
			labelInfo = GetComponent<IStorageLabelInfo>();

			if (!inputField)
			{
				Destroy(this);
				return;
			}

			text = inputField.textComponent;
			textTransform = text.transform as RectTransform;

			if (textTransform == null)
			{
				DestroyImmediate(this);
				return;
			}

			inputField.characterLimit = labelInfo.maxCharCount;
			text.GetComponent<ContentSizeFitter>().enabled = false;

			RectTransformExtensions.SetSize(textTransform, labelInfo.size.x, labelInfo.size.y);
			text.alignment = TMPro.TextAlignmentOptions.Midline;
			inputField.lineType = TMPro.TMP_InputField.LineType.MultiLineSubmit;
			inputField.onValueChanged.AddListener(valueListener);
			UWE.CoroutineHost.StartCoroutine(_updateLabel());

			IEnumerator _updateLabel()
			{
				// skip two frames to wait for initialization
				yield return null;
				yield return null;

				valueListener(null);
			}
		}

		void OnDestroy()
		{
			inputField?.onValueChanged.RemoveListener(valueListener);
		}
	}


	[StorageHandler(TechType.SmallLocker)]
	class SmallLockerLabelFixer: Fixer
	{
		protected override void fixLabel()
		{
			text.forceRedraw(inputField.text);
			text.lineSpacing = text.getLineCount() > 3? (Mod.Consts.isGameSN? 0.7f: -43f): (Mod.Consts.isGameSN? 1f: 0f);
		}
	}


	[StorageHandler(TechType.SmallStorage)]
	class SmallStorageLabelFixer: Fixer
	{
		static readonly Vector3 defaultScale = new (1.0f, 1.7f, 1.0f);
		static readonly Vector3 tightScale = new (1.0f, 1.2f, 1.0f);

		protected override void fixLabel()
		{
			RectTransformExtensions.SetSize(textTransform, labelInfo.size.x, textTransform.rect.height);
			textTransform.localScale = defaultScale;
			text.forceRedraw(inputField.text);
			if (text.getLineCount() > 2)
			{
				textTransform.localScale = tightScale;
			}
		}

		protected override void ensureMaxLineCount(int maxLineCount)
		{
			const int maxSteps = 10;
			const float scaleStep = 0.05f;

			// first, we'll try to make symbols narrower
			for (int i = 0; text.getLineCount() > maxLineCount && i < maxSteps; i++)
			{
				textTransform.localScale = textTransform.localScale.setX(textTransform.localScale.x - scaleStep);
				RectTransformExtensions.SetSize(textTransform, labelInfo.size.x / textTransform.localScale.x, textTransform.rect.height);
				text.forceRedraw(inputField.text);
			}

			// remove extra symbols if we still have too much lines
			if (text.getLineCount() > maxLineCount)
				base.ensureMaxLineCount(maxLineCount);
		}

		protected override void Start()
		{
#if SUBNAUTICA
			if (inputField) // in case we already called 'Start' from 'OnDisable'
				return;
#endif
			base.Start();

			if (!inputField)
				return;

			text.lineSpacing = Mod.Consts.isGameSN? 0.7f: -35f;
#if SUBNAUTICA
			storageModel = gameObject.getChild("../3rd_person_model");
			fixLabelPos();
#endif
		}

#if SUBNAUTICA // additional fixing for label's position in SN
		Animator animator;
		GameObject storageModel;

		void fixLabelPos()
		{
			if (textTransform)
				textTransform.localPosition = storageModel.activeSelf? Vector3.zero: new (25f, -35f);
		}

		void OnEnable()
		{
			fixLabelPos();
		}

		void OnDisable()
		{
			if (!tweakEnabled)
				return;

			// 'OnDisable' can be called before 'Start' and nothing will be initialized
			// so we need this minor hack to do it ourselves
			if (!text)
				UWE.CoroutineHost.StartCoroutine(_startAndFix());
			else
				fixLabelPos();

			animator ??= gameObject.getChild("../3rd_person_model/floating_storage_cube_tp").GetComponent<Animator>();
			animator.Rebind();

			IEnumerator _startAndFix()
			{
				// just in case, to be sure that other components are initialized
				yield return null;

				Start();
				fixLabelPos();
			}
		}
#endif // SUBNAUTICA
	}

#if BELOWZERO
	abstract class SeaTruckLabelFixer: Fixer
	{
		protected abstract int lineTight { get; }
		protected virtual Vector2 labelPos => Vector2.zero;
		protected abstract Vector2 colorSelectorPos { get; }

		protected override void fixLabel()
		{
			text.forceRedraw(inputField.text);
			text.lineSpacing = text.getLineCount() > lineTight? -20f: 0f;
		}

		protected override void Start()
		{
			base.Start();

			if (inputField)
			{
				inputField.transform.localPosition = labelPos;
				inputField.transform.Find("../ColorSelector").localPosition = colorSelectorPos;
			}
		}
	}

	[StorageHandler(TechType.SeaTruckFabricatorModule)]
	class SeaTruckFabricatorFixer: SeaTruckLabelFixer
	{
		protected override int lineTight => 2;
		protected override Vector2 labelPos => new (110f, -30f);
		protected override Vector2 colorSelectorPos => new (-230f, -30f);
	}

	[StorageHandler(TechType.SeaTruckStorageModule)]
	class SeaTruckStorageFixer: MonoBehaviour
	{
		class FixerVertical: SeaTruckLabelFixer
		{
			protected override int lineTight => 4;
			protected override Vector2 colorSelectorPos => new (0f, -270f);
		}

		class FixerHorizontalBig: SeaTruckLabelFixer
		{
			protected override int lineTight => 2;
			protected override Vector2 labelPos => new (0f, -30f);
			protected override Vector2 colorSelectorPos => new (-400f, -30f);
		}

		class FixerHorizontalSmallLeft: SeaTruckLabelFixer
		{
			protected override int lineTight => 3;
			protected override Vector2 labelPos => new (-12f, 80f);
			protected override Vector2 colorSelectorPos => new (-245f, -50f);
		}

		class FixerHorizontalSmallRight: SeaTruckLabelFixer
		{
			protected override int lineTight => 3;
			protected override Vector2 labelPos => new (7f, 80f);
			protected override Vector2 colorSelectorPos => new (-225f, -50f);
		}

		void Start()
		{
			var fixerType = GetComponent<IStorageLabelInfo>()?.type switch
			{
				"Vertical" => typeof(FixerVertical),
				"HorizontalBig" => typeof(FixerHorizontalBig),
				"HorizontalSmallLeft" => typeof(FixerHorizontalSmallLeft),
				"HorizontalSmallRight" => typeof(FixerHorizontalSmallRight),
				_ => null
			};

			Common.Debug.assert(fixerType != null);

			if (fixerType != null)
				gameObject.AddComponent(fixerType);
		}
	}
#endif // BELOWZERO
}