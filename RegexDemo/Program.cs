using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using RE;
namespace RegexDemo
{
	class Program
	{
		static void Main(string[] args) {
			_RunLexer();
			_RunMatch();
			_RunDom();
		}
		static void _RunCompiledLexCodeGen() {
			// create our expressions
			var digits = CharFA<string>.Repeat(
				CharFA<string>.Set("0123456789"),
				1, -1
				, "Digits");
			var word = CharFA<string>.Repeat(
				CharFA<string>.Set(new CharRange[] { new CharRange('A', 'Z'), new CharRange('a', 'z') }),
				1, -1
				, "Word");
			var whitespace = CharFA<string>.Repeat(
				CharFA<string>.Set(" \t\r\n\v\f"),
				1, -1
				, "Whitespace");
			// initialize our lexer
			var lexer = CharFA<string>.ToLexer(digits, word, whitespace);
			// create the symbol table (include the error symbol at index/id 3)
			var symbolTable = new string[] { "Digits", "Word", "Whitespace", "#ERROR" };
			// create the DFA table we'll use to generate code
			var dfaTable = lexer.ToDfaStateTable(symbolTable);
			// create our new class
			var compClass = new CodeTypeDeclaration("RegexGenerated");
			compClass.TypeAttributes = System.Reflection.TypeAttributes.Class;
			compClass.Attributes = MemberAttributes.Final | MemberAttributes.Static;
			// add the symbol table field - in production we'll set the name
			// to something more appropriate
			var symtblField = new CodeMemberField(typeof(string[]), "LexSymbols");
			symtblField.Attributes = MemberAttributes.Static |  MemberAttributes.Public;
			// generate the symbol table init code
			symtblField.InitExpression = CharFA<string>.GenerateSymbolTableInitializer(symbolTable);
			compClass.Members.Add(symtblField);
			// Generate and add the compiled lex method code
			compClass.Members.Add(CharFA<string>.GenerateLexMethod(dfaTable, 3));
			// in production we'd change the name of the returned method
			// above
			// add the DFA table field - in production we'd change the name
			var dfatblField = new CodeMemberField(typeof(CharDfaEntry[]), "LexDfaTable");
			dfatblField.Attributes = MemberAttributes.Static | MemberAttributes.Public;
			// generate the DFA state table init code
			dfatblField.InitExpression = CharFA<string>.GenerateDfaStateTableInitializer(dfaTable);
			compClass.Members.Add(dfatblField);
			// create the C# provider and generate the code
			// we'll usually want to put this in a namespace
			// but we haven't here
			var prov = CodeDomProvider.CreateProvider("cs");
			prov.GenerateCodeFromType(compClass, Console.Out, new CodeGeneratorOptions());
		}
		static void _RunDom() { 
			var test = "(ABC|DEF)*";
			var dom = RegexExpression.Parse(test);
			Console.WriteLine(dom.ToString());
			var rep = dom as RegexRepeatExpression;
			rep.MinOccurs = 1;
			Console.WriteLine(dom.ToString());
			dom.ToFA("Accept");
		}
		static void _RunMatch() { 
			var test = "foo123_ _bar";

			var word = CharFA<string>.Repeat(
				CharFA<string>.Set(new CharRange[] { new CharRange('A', 'Z'), new CharRange('a', 'z') }),
				1, -1
				, "Word");
			var dfaWord = word.ToDfa();
			var dfaTableWord = word.ToDfaStateTable(); 
			RegexMatch match;
			var pc = ParseContext.Create(test);
			Console.WriteLine("Matching words with an NFA:");
			while (null!=(match=word.Match(pc)))
				Console.WriteLine("Found match at {0}: {1}", match.Position, match.Value);
			pc = ParseContext.Create(test);
			Console.WriteLine("Matching words with a DFA:");
			while (null != (match = dfaWord.MatchDfa(pc)))
				Console.WriteLine("Found match at {0}: {1}", match.Position, match.Value);
			pc = ParseContext.Create(test);
			Console.WriteLine("Matching words with a DFA state table:");
			while (null != (match = CharFA<string>.MatchDfa(dfaTableWord,pc)))
				Console.WriteLine("Found match at {0}: {1}", match.Position, match.Value);

		}
		static void _RunLexer()
		{
			var digits = CharFA<string>.Repeat(
				CharFA<string>.Set("0123456789"),
				1, -1
				, "Digits");
			var word = CharFA<string>.Repeat(
				CharFA<string>.Set(new CharRange[] { new CharRange('A', 'Z'), new CharRange('a', 'z') }),
				1, -1
				, "Word");
			var whitespace = CharFA<string>.Repeat(
				CharFA<string>.Set(" \t\r\n\v\f"),
				1, -1
				, "Whitespace");
			var lexer = new CharFA<string>();
			lexer.EpsilonTransitions.Add(digits);
			lexer.EpsilonTransitions.Add(word);
			lexer.EpsilonTransitions.Add(whitespace);
			var lexerDfa = lexer.ToDfa();
			lexerDfa.TrimDuplicates();
			// we use a symbol table with the DFA state table to map ids back to strings
			var symbolTable = new string[] { "Digits", "Word", "Whitespace","#ERROR" };
			// make sure to pass the symbol table if you're using one
			var dfaTable = lexer.ToDfaStateTable(symbolTable);
			var test = "foo123_ _bar";
			Console.WriteLine("Lex using the NFA");
			// create a parse context over our test string
			var pc = ParseContext.Create(test);
			// while not end of input
			while (-1 != pc.Current)
			{
				// clear the capture so that we don't keep appending the token data
				pc.ClearCapture();
				// lex the next token
				var acc = lexer.Lex(pc, "#ERROR");
				// write the result
				Console.WriteLine("{0}: {1}",acc, pc.GetCapture());
			}
			Console.WriteLine();
			Console.WriteLine("Lex using the DFA");
			// create a new parse context over our test string
			// because our old parse context is now past the end
			pc = ParseContext.Create(test);
			while (-1 != pc.Current)
			{
				pc.ClearCapture();
				// lex using the DFA. This works exactly like 
				// the previous Lex method except that it's
				// optimized for DFA traversal.
				// DO NOT use this with an NFA. It won't work
				// but won't error (can't check for perf reasons)
				var acc = lexerDfa.LexDfa(pc, "#ERROR");
				// write the result
				Console.WriteLine("{0}: {1}", acc, pc.GetCapture());
			}
			Console.WriteLine();
			Console.WriteLine("Lex using the DFA state table");
			pc = ParseContext.Create(test);
			while (-1 != pc.Current)
			{
				pc.ClearCapture();
				// Lex using our DFA table. This is a little different
				// because it's a static method that takes CharDfaEntry[]
				// as its first parameter. It also uses symbol ids instead
				// of the actual symbol. You must map them back using the
				// symbol table you created earlier.
				var acc = CharFA<string>.LexDfa(dfaTable, pc, 3);
				// when we write this, we map our symbol id back to the
				// symbol using our symbol table
				Console.WriteLine("{0}: {1}", symbolTable[acc], pc.GetCapture());
			}
			Console.WriteLine();
			Console.WriteLine("Lex using our compiled lex method");
			pc = ParseContext.Create(test);
			while (-1 != pc.Current)
			{
				pc.ClearCapture();
				// Lex using our compiledDFA. Like the table driven lex
				// this also uses symbol ids instead of the actual symbol.
				var acc = Lex(pc);
				// when we write this, we map our symbol id back to the
				// symbol using our symbol table
				Console.WriteLine("{0}: {1}", symbolTable[acc], pc.GetCapture());
			}
		}
		static void _BuildArticleImages()
		{
			// this generates the figures used in the code project article
			// at https://www.codeproject.com/Articles/5251476/How-to-Build-a-Regex-Engine-in-Csharp
			var litA = CharFA<string>.Literal("ABC", "Accept");
			litA.RenderToFile(@"..\..\..\literal.jpg");
			var litAa = CharFA<string>.CaseInsensitive(litA, "Accept");
			litAa.RenderToFile(@"..\..\..\literal_ci.jpg");
			var opt = CharFA<string>.Optional(litA, "Accept");
			opt.RenderToFile(@"..\..\..\optional.jpg");
			var litB = CharFA<string>.Literal("DEF");
			var or = CharFA<string>.Or(new CharFA<string>[] { litA, litB }, "Accept");
			or.RenderToFile(@"..\..\..\or.jpg");
			var set = CharFA<string>.Set("ABC", "Accept");
			set.RenderToFile(@"..\..\..\set.jpg");
			var loop = CharFA<string>.Repeat(litA, 1, -1, "Accept");
			loop.RenderToFile(@"..\..\..\repeat.jpg");
			var concat = CharFA<string>.Concat(new CharFA<string>[] { litA, litB }, "Accept");
			concat.RenderToFile(@"..\..\..\concat.jpg");
			var foobar = CharFA<string>.Or(new CharFA<string>[] { CharFA<string>.Literal("foo"), CharFA<string>.Literal("bar") }, "Accept");
			foobar.RenderToFile(@"..\..\..\foobar_nfa.jpg");
			var rfoobar = foobar.Reduce();
			rfoobar.RenderToFile(@"..\..\..\foobar.jpg");
			var lfoobar = CharFA<string>.Repeat(foobar, 1, -1, "Accept");
			lfoobar.RenderToFile(@"..\..\..\foobar_loop_nfa.jpg");
			var rlfoobar = lfoobar.Reduce();
			rlfoobar.RenderToFile(@"..\..\..\foobar_loop.jpg");

			var digits = CharFA<string>.Repeat(
				CharFA<string>.Set("0123456789"),
				1, -1
				, "Digits");
			var word = CharFA<string>.Repeat(
				CharFA<string>.Set(new CharRange[] { new CharRange('A', 'Z'), new CharRange('a', 'z') }),
				1, -1
				, "Word");
			var whitespace = CharFA<string>.Repeat(
				CharFA<string>.Set(" \t\r\n\v\f"),
				1, -1
				, "Whitespace");
			var lexer = CharFA<string>.ToLexer(digits, word, whitespace);
			lexer.RenderToFile(@"..\..\..\lexer.jpg");
			var dopt = new CharFA<string>.DotGraphOptions();
			dopt.DebugSourceNfa = lexer;
			var dlexer = lexer.ToDfa();
			dlexer.RenderToFile(@"..\..\..\dlexer.jpg", dopt
				);
			dlexer.RenderToFile(@"..\..\..\dlexer2.jpg");
			var dom = RegexExpression.Parse("(ABC|DEF)+");
			var fa = dom.ToFA("Accept");
			fa.RenderToFile(@"..\..\..\ABCorDEFloop.jpg");
			
		}
		internal static int Lex(RE.ParseContext context)
		{
			context.EnsureStarted();
			// q0
			if (((context.Current >= '0')
						&& (context.Current <= '9')))
			{
				context.CaptureCurrent();
				context.Advance();
				goto q1;
			}
			if ((((context.Current >= 'A')
						&& (context.Current <= 'Z'))
						|| ((context.Current >= 'a')
						&& (context.Current <= 'z'))))
			{
				context.CaptureCurrent();
				context.Advance();
				goto q2;
			}
			if (((((context.Current == '\t')
						|| ((context.Current >= '\n')
						&& (context.Current <= '')))
						|| (context.Current == '\r'))
						|| (context.Current == ' ')))
			{
				context.CaptureCurrent();
				context.Advance();
				goto q3;
			}
			goto error;
		q1:
			if (((context.Current >= '0')
						&& (context.Current <= '9')))
			{
				context.CaptureCurrent();
				context.Advance();
				goto q1;
			}
			return 0;
		q2:
			if ((((context.Current >= 'A')
						&& (context.Current <= 'Z'))
						|| ((context.Current >= 'a')
						&& (context.Current <= 'z'))))
			{
				context.CaptureCurrent();
				context.Advance();
				goto q2;
			}
			return 1;
		q3:
			if (((((context.Current == '\t')
						|| ((context.Current >= '\n')
						&& (context.Current <= '')))
						|| (context.Current == '\r'))
						|| (context.Current == ' ')))
			{
				context.CaptureCurrent();
				context.Advance();
				goto q3;
			}
			return 2;
		error:
			context.CaptureCurrent();
			context.Advance();
			return 3;
		}
	}
}
