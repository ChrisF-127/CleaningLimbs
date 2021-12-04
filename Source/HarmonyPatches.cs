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
						case "VaccuumHand":
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

						// vaccuum hand'ing
						for (int i = hands; i > 0; i--)
						{
							filth.ThinFilth();
						}
						// mop feet'ing
						if (feet > 0)
						{
							CleanAdjacent(toil, filth, __instance.Map, feet);
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
			void CleanAdjacent(Toil toil, Filth filth, Map map, int feet)
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
	}
}
