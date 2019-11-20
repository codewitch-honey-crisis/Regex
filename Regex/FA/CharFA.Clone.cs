using System;
using System.Collections.Generic;
using System.Text;

namespace RE
{
	partial class CharFA<TAccept> : ICloneable
	{
		public CharFA<TAccept> Clone()
		{
			var closure = FillClosure();
			var nclosure = new CharFA<TAccept>[closure.Count];
			for (var i = 0; i < nclosure.Length; i++)
			{
				nclosure[i] = new CharFA<TAccept>(closure[i].IsAccepting, closure[i].AcceptSymbol);
				nclosure[i].Tag = closure[i].Tag;
			}
			for (var i = 0; i < nclosure.Length; i++)
			{
				var t = nclosure[i].InputTransitions;
				var e = nclosure[i].EpsilonTransitions;
				foreach (var trns in closure[i].InputTransitions)
				{
					var id = closure.IndexOf(trns.Value);
					t.Add(trns.Key, nclosure[id]);
				}
				foreach (var trns in closure[i].EpsilonTransitions)
				{
					var id = closure.IndexOf(trns);
					e.Add(nclosure[id]);
				}
			}
			return nclosure[0];
		}
		object ICloneable.Clone() => Clone();
	}
}
