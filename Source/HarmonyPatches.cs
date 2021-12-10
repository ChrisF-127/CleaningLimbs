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
		}

		static IEnumerable<Toil> JobDriver_CleanFilth_MakeNewToils_Postfix(IEnumerable<Toil> toils, JobDriver_CleanFilth __instance)
		{
			int c = toils.Count();
			foreach (var toil in toils)
			{
				if (--c == 1) // hopefully the cleaning job will always be the second to last job in the list...
					ReplaceCleanToilAction(toil);
				yield return toil;
			}

			// ---

			// function to replace the cleaning action
			void ReplaceCleanToilAction(Toil toil)
			{
				// count number of parts ... there are probably more efficient ways to handle this
				int hands = 0;
				int feet = 0;
				foreach (var part in __instance.pawn.health.hediffSet.GetHediffs<Hediff_AddedPart>())
				{
					var name = part.def.defName;
					switch (name)
					{
						case "VacuumHand":
							hands++;
							break;
						case "MopFoot":
							feet++;
							break;
					}
				}

				// replace tick action
				toil.tickAction = delegate
				{
					__instance.cleaningWorkDone += 1f + hands * 0.25f;
					__instance.totalCleaningWorkDone += 1f;

					Filth filth = __instance.Filth;
					if (__instance.cleaningWorkDone > filth.def.filth.cleaningWorkToReduceThickness)
					{
						// base cleaning
						filth.ThinFilth();

						// vacuum hand'ing
						for (int i = hands; i > 0; i--)
						{
							filth.ThinFilth();
						}
						// mop feet'ing
						if (feet > 0)
						{
							CleanAdjacent(toil, __instance.Map, filth, feet);
						}

						// base functionality
						__instance.cleaningWorkDone = 0f;
						if (filth.Destroyed)
						{
							toil.actor.records.Increment(RecordDefOf.MessesCleaned);
							__instance.ReadyForNextToil();
						}
					}
				};
			}

			// function to clean adjacent tiles
			void CleanAdjacent(Toil toil, Map map, Filth filth, int feet)
			{
				var cleans = feet * 2;
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
				// count number of parts ... there are probably more efficient ways to handle this
				int hands = 0;
				int feet = 0;
				foreach (var part in __instance.pawn.health.hediffSet.GetHediffs<Hediff_AddedPart>())
				{
					var name = part.def.defName;
					switch (name)
					{
						case "VacuumHand":
							hands++;
							break;
						case "MopFoot":
							feet++;
							break;
					}
				}

				// replace tick action
				toil.tickAction = delegate
				{
					float statValue = toil.actor.GetStatValue(StatDefOf.GeneralLaborSpeed);
					__instance.workDone += statValue + hands * 0.50f;
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
				var clears = feet * 2;
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
	}
}
