using System;
using System.Collections.Generic;
using System.Text;

namespace RE
{
	/// <summary>
	/// Represents a regular expression match
	/// </summary>
	/// <remarks>Returned from the Match() and MatchDfa() methods</remarks>
	public sealed class CharFAMatch
	{
		/// <summary>
		/// Indicates the 1 based line where the match was found
		/// </summary>
		public int Line { get; }
		/// <summary>
		/// Indicates the 1 based column where the match was found
		/// </summary>
		public int Column { get; }
		/// <summary>
		/// Indicates the 0 based position where the match was found
		/// </summary>
		public long Position { get; }
		/// <summary>
		/// Indicates the value of the match
		/// </summary>
		public string Value { get; }
		public CharFAMatch(int line,int column,long position,string value)
		{
			Line = line;
			Column = column;
			Position = position;
			Value = value;
		}
	}
}
