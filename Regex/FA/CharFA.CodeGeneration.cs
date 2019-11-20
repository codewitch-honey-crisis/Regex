using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Reflection;

namespace RE
{
	partial class CharFA<TAccept>
	{
		public static CodeExpression GenerateSymbolTableInitializer(params TAccept[] symbols)
			=> _Serialize(symbols);
		public static CodeExpression GenerateDfaStateTableInitializer(CharDfaEntry[] dfaTable)
			=> _Serialize(dfaTable);
		public static CodeMemberMethod GenerateLexMethod(CharDfaEntry[] dfaTable, int errorSymbol)
		{
			var result = new CodeMemberMethod();
			result.Name = "Lex";
			result.Attributes = MemberAttributes.FamilyAndAssembly | MemberAttributes.Static;
			result.Parameters.Add(new CodeParameterDeclarationExpression(typeof(ParseContext), "context"));
			result.ReturnType = new CodeTypeReference(typeof(int));
			result.Statements.Add(new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(new CodeArgumentReferenceExpression("context"), "EnsureStarted")));
			// we generate labels for each state except maybe the first.
			// we only generate a label for the first state if any of the
			// states (including itself) reference it. This is to prevent
			// a compiler warning in the case of an unreferenced label
			var isRootLoop = false;
			// we also need to see if any states do not accept
			// if they don't we'll have to generate an error condition
			var hasError = false;
			for (var i = 0;i<dfaTable.Length;i++)
			{
				var trns = dfaTable[i].Transitions;
				for (var j=0;j<trns.Length;j++)
				{
					if(0==trns[j].Destination)
					{
						isRootLoop = true;
						break;
					}
				}
			}
			var pcr = new CodeArgumentReferenceExpression(result.Parameters[0].Name);
			var pccr = new CodePropertyReferenceExpression(pcr, "Current");
			var pccc = new CodeExpressionStatement(new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(pcr, "CaptureCurrent")));
			var exprs = new CodeExpressionCollection();
			var stmts = new CodeStatementCollection();
			
			for (var i = 0;i<dfaTable.Length;i++)
			{
				stmts.Clear();
				var se = dfaTable[i];
				var trns = se.Transitions;
				for(var j = 0;j<trns.Length;j++)
				{
					var cif = new CodeConditionStatement();
					stmts.Add(cif);
					exprs.Clear();

					var trn = trns[j];
					var pr = trn.PackedRanges;
					for(var k =0;k<pr.Length; k++)
					{
						var first = pr[k];
						++k; // advance an extra place
						var last = pr[k];
						if (first!=last)
						{
							exprs.Add(
								new CodeBinaryOperatorExpression(
									new CodeBinaryOperatorExpression(
										pccr,
										CodeBinaryOperatorType.GreaterThanOrEqual,
										new CodePrimitiveExpression(first)
										),
									CodeBinaryOperatorType.BooleanAnd,
									new CodeBinaryOperatorExpression(
										pccr,
										CodeBinaryOperatorType.LessThanOrEqual,
										new CodePrimitiveExpression(last)
										)
									)
								);
						}
						else
						{
							exprs.Add(
								new CodeBinaryOperatorExpression(
									pccr,
									CodeBinaryOperatorType.ValueEquality,
									new CodePrimitiveExpression(first)
									)
								);
						}
					}
					cif.Condition = _MakeBinOps(exprs, CodeBinaryOperatorType.BooleanOr);
					cif.TrueStatements.Add(pccc);
					cif.TrueStatements.Add(new CodeExpressionStatement(new CodeMethodInvokeExpression(pcr, "Advance")));
					cif.TrueStatements.Add(new CodeGotoStatement(string.Concat("q", trn.Destination.ToString())));

				}
				if(-1!=se.AcceptSymbolId) // is accepting
					stmts.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(se.AcceptSymbolId)));
				else
				{
					hasError = true;
					stmts.Add(new CodeGotoStatement("error"));
				}
				if (0 < i || isRootLoop)
				{
					result.Statements.Add(new CodeLabeledStatement(string.Concat("q", i.ToString()), stmts[0]));
					for (int jc = stmts.Count, j = 1; j < jc; ++j)
						result.Statements.Add(stmts[j]);
				}
				else
				{
					result.Statements.Add(new CodeCommentStatement("q0"));
					result.Statements.AddRange(stmts);
				}
			}
			if(hasError)
			{
				result.Statements.Add(new CodeLabeledStatement("error", pccc));
				result.Statements.Add(new CodeExpressionStatement(new CodeMethodInvokeExpression(pcr, "Advance")));
				result.Statements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(errorSymbol)));
			}
			return result;
		}
		static CodeExpression _MakeBinOps(IEnumerable exprs, CodeBinaryOperatorType type)
		{
			var result = new CodeBinaryOperatorExpression();
			foreach (CodeExpression expr in exprs)
			{
				result.Operator = type;
				if (null == result.Left)
				{
					result.Left = expr;
					continue;
				}
				if (null == result.Right)
				{
					result.Right = expr;
					continue;
				}
				result = new CodeBinaryOperatorExpression(result, type, expr);
			}
			if (null == result.Right)
				return result.Left;
			return result;
		}
		#region Type serialization
		static CodeExpression _SerializeArray(Array arr)
		{
			if (1 == arr.Rank && 0 == arr.GetLowerBound(0))
			{
				var result = new CodeArrayCreateExpression(arr.GetType());
				foreach (var elem in arr)
					result.Initializers.Add(_Serialize(elem));
				return result;
			}
			throw new NotSupportedException("Only SZArrays can be serialized to code.");
		}
		static CodeExpression _Serialize(object val)
		{
			if (null == val)
				return new CodePrimitiveExpression(null);
			if (val is char) // special case for unicode nonsense
			{
				// console likes to cook unicode characters
				// so we render them as ints cast to the character
				if (((char)val) > 0x7E)
					return new CodeCastExpression(typeof(char), new CodePrimitiveExpression((int)(char)val));
				return new CodePrimitiveExpression((char)val);
			}
			else
			if (val is bool ||
				val is string ||
				val is short ||
				val is ushort ||
				val is int ||
				val is uint ||
				val is ulong ||
				val is long ||
				val is byte ||
				val is sbyte ||
				val is float ||
				val is double ||
				val is decimal)
			{
				// TODO: mess with strings to make them console safe.
				return new CodePrimitiveExpression(val);
			}
			if (val is Array && 1 == ((Array)val).Rank && 0 == ((Array)val).GetLowerBound(0))
			{
				return _SerializeArray((Array)val);
			}
			var conv = TypeDescriptor.GetConverter(val);
			if (null != conv)
			{
				if (conv.CanConvertTo(typeof(InstanceDescriptor)))
				{
					var desc = conv.ConvertTo(val, typeof(InstanceDescriptor)) as InstanceDescriptor;
					if (!desc.IsComplete)
						throw new NotSupportedException(
							string.Format(
								"The type \"{0}\" could not be serialized.",
								val.GetType().FullName));
					var ctor = desc.MemberInfo as ConstructorInfo;
					if (null != ctor)
					{
						var result = new CodeObjectCreateExpression(ctor.DeclaringType);
						foreach (var arg in desc.Arguments)
							result.Parameters.Add(_Serialize(arg));
						return result;
					}
					throw new NotSupportedException(
						string.Format(
							"The instance descriptor for type \"{0}\" is not supported.",
							val.GetType().FullName));
				}
				else
				{
					// we special case for KeyValuePair types.
					var t = val.GetType();
					if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
					{
						// TODO: Find a workaround for the bug with VBCodeProvider
						// may need to modify the reference source
						var kvpType = new CodeTypeReference(typeof(KeyValuePair<,>));
						foreach (var arg in val.GetType().GetGenericArguments())
							kvpType.TypeArguments.Add(arg);
						var result = new CodeObjectCreateExpression(kvpType);
						for (int ic = kvpType.TypeArguments.Count, i = 0; i < ic; ++i)
						{
							var prop = val.GetType().GetProperty(0 == i ? "Key" : "Value");
							result.Parameters.Add(_Serialize(prop.GetValue(val)));
						}
						return result;
					}
					throw new NotSupportedException(
						string.Format("The type \"{0}\" could not be serialized.",
						val.GetType().FullName));
				}
			}
			else
				throw new NotSupportedException(
					string.Format(
						"The type \"{0}\" could not be serialized.",
						val.GetType().FullName));
		}
		#endregion
	}
}
