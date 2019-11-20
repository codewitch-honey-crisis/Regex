using System;
using System.Collections.Generic;
using System.Text;

namespace RE
{
	/// <summary>
	/// Represents a regular expression match
	/// </summary>
	/// <remarks>Returned from the Match() and MatchDfa() methods</remarks>
	public sealed class RegexMatch
	{
		public int Line { get; }
		public int Column { get; }
		public long Position { get; }
		public string Value { get; }
		public RegexMatch(int line,int column,long position,string value)
		{
			Line = line;
			Column = column;
			Position = position;
			Value = value;
		}
	}
}
