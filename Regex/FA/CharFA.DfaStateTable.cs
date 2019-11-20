using System;
using System.Collections.Generic;

namespace RE
{
	partial class CharFA<TAccept>
	{
		/// <summary>
		/// Returns a DFA table that can be used to lex or match
		/// </summary>
		/// <param name="symbolTable">The symbol table to use, or null to just implicitly tag symbols with integer ids</param>
		/// <param name="progress">The progress object used to report the progress of the task</param>
		/// <returns>A DFA table that can be used to efficiently match or lex input</returns>
		public CharDfaEntry[] ToDfaStateTable(IList<TAccept> symbolTable = null, IProgress<CharFAProgress> progress=null)
		{
			var dfa = ToDfa(progress);
			var closure = dfa.FillClosure();
			var symbolLookup = new ListDictionary<TAccept, int>();
			if (null == symbolTable)
			{
				var i = 0;
				for (int jc = closure.Count, j = 0; j < jc; ++j)
				{
					var fa = closure[j];
					if (fa.IsAccepting && !symbolLookup.ContainsKey(fa.AcceptSymbol))
					{
						symbolLookup.Add(fa.AcceptSymbol, i);
						++i;
					}
				}
			}
			else
				for (int ic = symbolTable.Count, i = 0; i < ic; ++i)
					if (null != symbolTable[i])
						symbolLookup.Add(symbolTable[i], i);

			var result = new CharDfaEntry[closure.Count];
			for (var i = 0; i < result.Length; i++)
			{
				var fa = closure[i];
				var trgs = fa.FillInputTransitionRangesGroupedByState();
				var trns = new CharDfaTransitionEntry[trgs.Count];
				var j = 0;

				foreach (var trg in trgs)
				{
					trns[j] = new CharDfaTransitionEntry(
						CharRange.ToPackedChars(trg.Value),
						closure.IndexOf(trg.Key));

					++j;
				}
				result[i] = new CharDfaEntry(
					fa.IsAccepting ? symbolLookup[fa.AcceptSymbol] : -1,
					trns);

			}
			return result;
		}
	}
}
