using System;
using System.Collections.Generic;
using System.Text;

namespace RE
{
	public abstract class RegexUnaryExpression : RegexExpression
	{
		public RegexExpression Expression { get; set; }

	}
}
