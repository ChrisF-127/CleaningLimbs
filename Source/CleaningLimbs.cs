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
		private SettingHandle<int> _footAdjacentCleans;
		public static int FootAdjacentCleans = 2;

		private SettingHandle<bool> _warCrimesMode;
		public static bool WarCrimesMode = false;
		private SettingHandle<float> _partMoodEffect;

		private SettingHandle<float> _handPartEfficiency;
		private SettingHandle<float> _handMovementSpeed;

		private SettingHandle<float> _footPartEfficiency;
		private SettingHandle<float> _footMovementSpeed;


		public override string ModIdentifier => "CleaningLimbs";

		public override void DefsLoaded()
		{
			MakeSettingWithValueChanged(
				ref _handAdditionalCleans,
				nameof(HandAdditionalCleans), 
				1, 
				Validators.IntRangeValidator(0, 10));
			MakeSettingWithValueChanged(
				ref _handAdditionalSpeed,
				nameof(HandAdditionalSpeed),
				0.25f,
				Validators.FloatRangeValidator(0f, 10f));
			MakeSettingWithValueChanged(
				ref _footAdjacentCleans,
				nameof(FootAdjacentCleans),
				2,
				Validators.IntRangeValidator(0, 10));

			MakeSettingWithValueChanged(
				ref _warCrimesMode,
				nameof(WarCrimesMode),
				false);
			MakeSetting(
				ref _partMoodEffect,
				"HorribleLimbsMoodEffect",
				ThoughtDefOfCleaningLimbs.HorribleCleaningLimbs.stages[0].baseMoodEffect,
				Validators.FloatRangeValidator(-100f, 100f));
			_partMoodEffect.ValueChanged += (value) => SetMoodEffect(ThoughtDefOfCleaningLimbs.HorribleCleaningLimbs, (SettingHandle<float>)value); 
			SetMoodEffect(ThoughtDefOfCleaningLimbs.HorribleCleaningLimbs, _partMoodEffect);

			MakeSetting(
				ref _handPartEfficiency,
				"HandPartEfficiency",
				HediffDefOfCleaningLimbs.VacuumHand.addedPartProps.partEfficiency,
				Validators.FloatRangeValidator(0f, 1000f));
			_handPartEfficiency.ValueChanged += (value) => SetPartEfficiency(HediffDefOfCleaningLimbs.VacuumHand, (SettingHandle<float>)value);
			SetPartEfficiency(HediffDefOfCleaningLimbs.VacuumHand, _handPartEfficiency);
			MakeSetting(
				ref _handMovementSpeed,
				"HandMovementSpeed",
				GetMovementSpeedCapacityModifier(HediffDefOfCleaningLimbs.VacuumHand).offset,
				Validators.FloatRangeValidator(-1f, 1f));
			_handMovementSpeed.ValueChanged += (value) => SetMovementSpeed(HediffDefOfCleaningLimbs.VacuumHand, (SettingHandle<float>)value);
			SetMovementSpeed(HediffDefOfCleaningLimbs.VacuumHand, _handMovementSpeed);

			MakeSetting(
				ref _footPartEfficiency,
				"FootPartEfficiency",
				HediffDefOfCleaningLimbs.MopFoot.addedPartProps.partEfficiency,
				Validators.FloatRangeValidator(0f, 1000f));
			_footPartEfficiency.ValueChanged += (value) => SetPartEfficiency(HediffDefOfCleaningLimbs.MopFoot, (SettingHandle<float>)value);
			SetPartEfficiency(HediffDefOfCleaningLimbs.MopFoot, _footPartEfficiency);
			MakeSetting(
				ref _footMovementSpeed,
				"FootMovementSpeed",
				GetMovementSpeedCapacityModifier(HediffDefOfCleaningLimbs.MopFoot).offset,
				Validators.FloatRangeValidator(-1f, 1f));
			_footMovementSpeed.ValueChanged += (value) => SetMovementSpeed(HediffDefOfCleaningLimbs.MopFoot, (SettingHandle<float>)value);
			SetMovementSpeed(HediffDefOfCleaningLimbs.MopFoot, _footMovementSpeed);
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
