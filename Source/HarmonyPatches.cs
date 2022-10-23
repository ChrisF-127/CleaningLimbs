using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace CleaningLimbs
{
	[StaticConstructorOnStartup]
	public static class HarmonyPatches
	{
		static HarmonyPatches()
		{
			Harmony harmony = new Harmony("syrus.cleaninglimbs");

			harmony.Patch(
				typeof(JobDriver_CleanFilth).GetMethod("MakeNewToils", BindingFlags.Instance | BindingFlags.NonPublic),
				postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.JobDriver_CleanFilth_MakeNewToils_Postfix)));

			harmony.Patch(
				typeof(JobDriver_ClearSnow).GetMethod("MakeNewToils", BindingFlags.Instance | BindingFlags.NonPublic),
				postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.JobDriver_ClearSnow_MakeNewToils_Postfix)));

			// CleaningArea patch
			var typeCleaningArea = Type.GetType("CleaningArea.JobDriver_CleanFilth_CleaningArea, CleaningArea");
			if (typeCleaningArea != null)
			{
				Log.Message("CleaningLimbs: applying CleaningArea.JobDriver_CleanFilth_CleaningArea patch");
				harmony.Patch(
					typeCleaningArea.GetMethod("MakeNewToils", BindingFlags.Instance | BindingFlags.NonPublic),
					postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.JobDriver_CleanFilth_CleaningArea_MakeNewToils_Postfix)));
			}
		}

		static IEnumerable<Toil> JobDriver_CleanFilth_MakeNewToils_Postfix(IEnumerable<Toil> toils, JobDriver_CleanFilth __instance)
		{
			int c = toils.Count();
			foreach (var toil in toils)
			{
				if (--c == 1) // hopefully the cleaning job will always be the second to last job in the list...
					ReplaceCleanToilAction(toil, __instance);
				yield return toil;
			}
		}
		static IEnumerable<Toil> JobDriver_CleanFilth_CleaningArea_MakeNewToils_Postfix(IEnumerable<Toil> toils, JobDriver __instance)
		{
			int c = toils.Count();
			foreach (var toil in toils)
			{
				if (--c == 2) // once again, hoping the jobs stays at that position
					ReplaceCleanToilAction(toil, __instance);
				yield return toil;
			}
		}

		// function to replace the cleaning action
		static void ReplaceCleanToilAction(Toil toil, JobDriver __instance)
		{
			// count number of parts
			(int hands, int feet) = CountHandsAndFeet(__instance);

			// dirty fucking reflection because CleaningArea screws up how cleaning jobs are generated...
			var instanceType = __instance.GetType();
			var cleaningWorkDoneFieldInfo = instanceType.GetField("cleaningWorkDone", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
			var totalCleaningWorkDoneFieldInfo = instanceType.GetField("totalCleaningWorkDone", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
			var filthFieldInfo = instanceType.GetProperty("Filth", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

			// replace tick action
			toil.tickAction = delegate
			{
				var cleaninWorkDone = (float)cleaningWorkDoneFieldInfo.GetValue(__instance);
				var totalCleaningWorkDone = (float)totalCleaningWorkDoneFieldInfo.GetValue(__instance);

				cleaninWorkDone += 1f + hands * CleaningLimbs.HandAdditionalSpeed;
				totalCleaningWorkDone += 1f;

				var filth = (Filth)filthFieldInfo.GetValue(__instance);
				if (cleaninWorkDone > filth.def.filth.cleaningWorkToReduceThickness)
				{
					// base cleaning
					filth.ThinFilth();

					// vacuum hand'ing
					for (int i = hands * CleaningLimbs.HandAdditionalCleans; i > 0; i--)
					{
						filth.ThinFilth();
					}
					// mop feet'ing
					if (feet > 0)
					{
						CleanAdjacent(toil, __instance.Map, filth, feet);
					}

					// base functionality
					cleaninWorkDone = 0f;
					if (filth.Destroyed)
					{
						toil.actor.records.Increment(RecordDefOf.MessesCleaned);
						__instance.ReadyForNextToil();
					}
				}

				cleaningWorkDoneFieldInfo.SetValue(__instance, cleaninWorkDone);
				totalCleaningWorkDoneFieldInfo.SetValue(__instance, totalCleaningWorkDone);
			};
		}
		// function to clean adjacent tiles
		static void CleanAdjacent(Toil toil, Map map, Filth filth, int feet)
		{
			var cleans = feet * CleaningLimbs.FootAdjacentCleans;
			foreach (var cell in GenAdj.AdjacentCellsAround)
			{
				var things = (filth.positionInt + cell).GetThingList(map);
				for (int i = things.Count - 1; i >= 0; i--)
				{
					// search for cleanable filth
					if (things[i] is Filth adjacentFilth)
					{
						// clean until filth is cleaned or number of cleans is used up
						while (!adjacentFilth.Destroyed)
						{
							// clean adjacent filth
							adjacentFilth.ThinFilth();

							// increase number of messes cleaned for pawn
							if (adjacentFilth.Destroyed)
								toil.actor.records.Increment(RecordDefOf.MessesCleaned);

							// limit to max number of cleans
							if (--cleans == 0)
								return;
						}
					}
				}
			}
			cleans /= 2;

			// clean filth at center
			while (!filth.Destroyed && cleans-- > 0)
			{
				filth.ThinFilth();
			}
		}


		static IEnumerable<Toil> JobDriver_ClearSnow_MakeNewToils_Postfix(IEnumerable<Toil> toils, JobDriver_ClearSnow __instance)
		{
			int c = toils.Count();
			foreach (var toil in toils)
			{
				if (--c == 0) // hopefully the clearing snow job will always be the last job in the list...
					ReplaceCleanToilAction(toil);
				yield return toil;
			}

			void ReplaceCleanToilAction(Toil toil)
			{
				// count number of parts
				(int hands, int feet) = CountHandsAndFeet(__instance);

				// replace tick action
				toil.tickAction = delegate
				{
					float statValue = toil.actor.GetStatValue(StatDefOf.GeneralLaborSpeed);
					__instance.workDone += statValue + hands * CleaningLimbs.HandAdditionalSpeed * 2;
					if (__instance.workDone >= __instance.TotalNeededWork)
					{
						var map = __instance.Map;
						var loc = __instance.TargetLocA;
						map.snowGrid.SetDepth(loc, 0f);

						if (feet > 0)
							CleanAdjacent(toil, map, loc, feet);

						__instance.ReadyForNextToil();
					}
				};
			}

			// function to clean adjacent tiles
			void CleanAdjacent(Toil toil, Map map, IntVec3 center, int feet)
			{
				var clears = feet * CleaningLimbs.FootAdjacentCleans;
				foreach (var cell in GenAdj.AdjacentCellsAround)
				{
					var loc = center + cell;
					// search for snow
					if (map.snowGrid.GetDepth(loc) > 0f)
					{
						// clear snow
						map.snowGrid.SetDepth(loc, 0f);

						// limit to max number of cleans
						if (--clears == 0)
							return;
					}
				}
			}
		}

		private static (int hands, int feet) CountHandsAndFeet(JobDriver jobDriver)
		{
			var list = new List<Hediff_AddedPart>();
			jobDriver.pawn.health.hediffSet.GetHediffs(ref list);
			int hands = 0, feet = 0;
			foreach (var part in list)
			{
				var name = part.def.defName;
				if (part.def == HediffDefOfCleaningLimbs.VacuumHand)
					hands++;
				else if (part.def == HediffDefOfCleaningLimbs.MopFoot)
					feet++;
			}
			return (hands, feet);
		}
	}
}
