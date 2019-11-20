using System.Collections.Generic;
namespace RE
{
	partial class CharFA<TAccept>
	{
		/// <summary>
		/// Indicates whether or not the state is neutral
		/// </summary>
		public bool IsNeutral {
			get { return 0 == InputTransitions.Count && 1 == EpsilonTransitions.Count; }
		}

		/// <summary>
		/// Retrieves all the states reachable from this state that are neutral.
		/// </summary>
		/// <param name="result">The list of neutral states. Will be filled after the call.</param>
		/// <returns>The resulting list of neutral states. This is the same value as the result parameter, if specified.</returns>
		public IList<CharFA<TAccept>> FillNeutralStates(IList<CharFA<TAccept>> result = null)
			=> FillNeutralStates(FillClosure(), result);
		/// <summary>
		/// Retrieves all the states in this closure that are neutral
		/// </summary>
		/// <param name="closure">The closure to examine</param>
		/// <param name="result">The list of neutral states. Will be filled after the call.</param>
		/// <returns>The resulting list of neutral states. This is the same value as the result parameter, if specified.</returns>
		public static IList<CharFA<TAccept>> FillNeutralStates(IList<CharFA<TAccept>> closure, IList<CharFA<TAccept>> result = null)
		{
			if (null == result)
				result = new List<CharFA<TAccept>>();
			for (int ic = closure.Count, i = 0; i < ic; ++i)
			{
				var fa = closure[i];
				if (fa.IsNeutral)
					if (!result.Contains(fa))
						result.Add(fa);
			}
			return result;
		}

	}
}
