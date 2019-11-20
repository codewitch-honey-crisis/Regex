using System;
using System.Collections.Generic;
using System.Text;

namespace RE
{
	public abstract class RegexBinaryExpression : RegexExpression
	{
		public RegexExpression Left { get; set; }
		public RegexExpression Right { get; set; }
	}
}
