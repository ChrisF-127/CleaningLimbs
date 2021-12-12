using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace CleaningLimbs
{
	public class ThoughtWorker_CleaningHediff : ThoughtWorker
	{
		public override ThoughtState CurrentStateInternal(Pawn p)
		{
			if (!CleaningLimbs.WarCrimesMode)
				return ThoughtState.Inactive;

			int count = 0;
			foreach (var hediff in p.health.hediffSet.hediffs)
			{
				if (hediff.def == HediffDefOfCleaningLimbs.VacuumHand
					|| hediff.def == HediffDefOfCleaningLimbs.MopFoot)
					count++;
			}
			if (count == 0)
				return ThoughtState.Inactive;

			return ThoughtState.ActiveAtStage(Mathf.Min(count - 1, def.stages.Count - 1));
		}
	}
}
