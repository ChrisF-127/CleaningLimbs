using HugsLib.Settings;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using static HugsLib.Settings.SettingHandle;

namespace CleaningLimbs
{
	public class CleaningLimbs : HugsLib.ModBase
	{
		private SettingHandle<int> _handAdditionalCleans;
		public static int HandAdditionalCleans = 1;
		private SettingHandle<float> _handAdditionalSpeed;
		public static float HandAdditionalSpeed = 0.25f;
		private SettingHandle<float> _handPartEfficiency;
		private SettingHandle<float> _handMovementSpeed;

		private SettingHandle<int> _footAdjacentCleans;
		public static int FootAdjacentCleans = 2;
		private SettingHandle<float> _footAdditionalSpeed;
		public static float FootAdditionalSpeed = 0.25f;
		private SettingHandle<float> _footPartEfficiency;
		private SettingHandle<float> _footMovementSpeed;

		private SettingHandle<bool> _warCrimesMode;
		public static bool WarCrimesMode = false;
		private SettingHandle<float> _partMoodEffect;

		public override string ModIdentifier => "CleaningLimbs";

		public override void DefsLoaded()
		{
			// Vacuum arm / hand cleaning settings
			MakeSettingWithValueChanged(
				ref _handAdditionalCleans,
				nameof(HandAdditionalCleans), 
				1, 
				Validators.IntRangeValidator(0, 20));
			MakeSettingWithValueChanged(
				ref _handAdditionalSpeed,
				nameof(HandAdditionalSpeed),
				0.25f,
				Validators.FloatRangeValidator(0f, 10f));

			// Vacuum arm / hand efficiency
			MakeSetting(
				ref _handPartEfficiency,
				"HandPartEfficiency",
				HediffDefOfCleaningLimbs.VacuumHand.addedPartProps.partEfficiency, // uses hand as default
				Validators.FloatRangeValidator(0f, 1000f));
			_handPartEfficiency.ValueChanged += value =>
			{
				var v = (SettingHandle<float>)value;
				SetPartEfficiency(HediffDefOfCleaningLimbs.VacuumArm, v);
				SetPartEfficiency(HediffDefOfCleaningLimbs.VacuumHand, v);
			};
			SetPartEfficiency(HediffDefOfCleaningLimbs.VacuumArm, _handPartEfficiency);
			SetPartEfficiency(HediffDefOfCleaningLimbs.VacuumHand, _handPartEfficiency);

			// Vacuum arm / hand movement speed
			MakeSetting(
				ref _handMovementSpeed,
				"HandMovementSpeed",
				GetMovementSpeedCapacityModifier(HediffDefOfCleaningLimbs.VacuumHand).offset, // uses hand as default
				Validators.FloatRangeValidator(-1f, 1f));
			_handMovementSpeed.ValueChanged += value =>
			{
				var v = (SettingHandle<float>)value;
				SetMovementSpeed(HediffDefOfCleaningLimbs.VacuumArm, v);
				SetMovementSpeed(HediffDefOfCleaningLimbs.VacuumHand, v);
			};
			SetMovementSpeed(HediffDefOfCleaningLimbs.VacuumArm, _handMovementSpeed);
			SetMovementSpeed(HediffDefOfCleaningLimbs.VacuumHand, _handMovementSpeed);


			// Mop leg / foot cleaning settings
			MakeSettingWithValueChanged(
				ref _footAdjacentCleans,
				nameof(FootAdjacentCleans),
				2,
				Validators.IntRangeValidator(0, 20));
			MakeSettingWithValueChanged(
				ref _footAdditionalSpeed,
				nameof(FootAdditionalSpeed),
				0.25f,
				Validators.FloatRangeValidator(0f, 10f));

			// Mop leg / foot efficiency
			MakeSetting(
				ref _footPartEfficiency,
				"FootPartEfficiency",
				HediffDefOfCleaningLimbs.MopFoot.addedPartProps.partEfficiency, // uses foot as default
				Validators.FloatRangeValidator(0f, 1000f));
			_footPartEfficiency.ValueChanged += value =>
			{
				var v = (SettingHandle<float>)value;
				SetPartEfficiency(HediffDefOfCleaningLimbs.MopLeg, v);
				SetPartEfficiency(HediffDefOfCleaningLimbs.MopFoot, v);
			};
			SetPartEfficiency(HediffDefOfCleaningLimbs.MopLeg, _footPartEfficiency);
			SetPartEfficiency(HediffDefOfCleaningLimbs.MopFoot, _footPartEfficiency);

			// Mop leg / foot movement speed
			MakeSetting(
				ref _footMovementSpeed,
				"FootMovementSpeed",
				GetMovementSpeedCapacityModifier(HediffDefOfCleaningLimbs.MopFoot).offset, // uses foot as default
				Validators.FloatRangeValidator(-1f, 1f));
			_footMovementSpeed.ValueChanged += value =>
			{
				var v = (SettingHandle<float>)value;
				SetMovementSpeed(HediffDefOfCleaningLimbs.MopLeg, v);
				SetMovementSpeed(HediffDefOfCleaningLimbs.MopFoot, v);
			};
			SetMovementSpeed(HediffDefOfCleaningLimbs.MopLeg, _footMovementSpeed);
			SetMovementSpeed(HediffDefOfCleaningLimbs.MopFoot, _footMovementSpeed);


			// War crimes mode & settings
			MakeSettingWithValueChanged(
				ref _warCrimesMode,
				nameof(WarCrimesMode),
				false);
			MakeSetting(
				ref _partMoodEffect,
				"HorribleLimbsMoodEffect",
				ThoughtDefOfCleaningLimbs.HorribleCleaningLimbs.stages[0].baseMoodEffect,
				Validators.FloatRangeValidator(-100f, 100f));
			_partMoodEffect.ValueChanged += value => SetMoodEffect(ThoughtDefOfCleaningLimbs.HorribleCleaningLimbs, (SettingHandle<float>)value);
			SetMoodEffect(ThoughtDefOfCleaningLimbs.HorribleCleaningLimbs, _partMoodEffect);
		}

		private static void SetMoodEffect(ThoughtDef thoughtDef, float value)
		{
			for (int i = 0; i < thoughtDef.stages.Count; i++)
				thoughtDef.stages[i].baseMoodEffect = value * (i + 1);
		}
		private static void SetPartEfficiency(HediffDef hediffDef, float value) =>
			hediffDef.addedPartProps.partEfficiency = value;
		private static PawnCapacityModifier GetMovementSpeedCapacityModifier(HediffDef hediffDef) =>
			hediffDef.stages[0].capMods.First((capMod) => capMod.capacity == PawnCapacityDefOf.Moving);
		private static void SetMovementSpeed(HediffDef hediffDef, float value) =>
			GetMovementSpeedCapacityModifier(hediffDef).offset = value;

		private void MakeSettingWithValueChanged<T>(ref SettingHandle<T> setting, string fieldName, T defaultValue, ValueIsValid validator = null)
		{
			var propInfo = GetType().GetField(fieldName);
			void action(SettingHandle handle) => propInfo.SetValue(this, (T)(SettingHandle<T>)handle);

			MakeSetting(ref setting, fieldName, defaultValue, validator);

			setting.ValueChanged += action;
			action(setting);
		}
		private void MakeSetting<T>(ref SettingHandle<T> setting, string name, T defaultValue, ValueIsValid validator = null)
		{
			setting = Settings.GetHandle(
				name.ToLower(),
				("SY_CL." + name).Translate(),
				("SY_CL.Tooltip" + name).Translate(),
				defaultValue,
				validator);
		}
	}
}
