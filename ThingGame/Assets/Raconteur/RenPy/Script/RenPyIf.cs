﻿using UnityEngine;

using DPek.Raconteur.RenPy.State;
using DPek.Raconteur.Util.Parser;

namespace DPek.Raconteur.RenPy.Script
{
	/// <summary>
	/// Ren'Py if statement.
	/// </summary>
	public class RenPyIf : RenPyStatement
	{
		[SerializeField]
		private Expression m_expression;
		public Expression Expression
		{
			get {
				return m_expression;
			}
		}

		/// <summary>
		/// Whether or not last time this statement was executed that it
		/// evaluated as true.
		/// </summary>
		private bool m_wasSuccessful;
		public bool WasSuccessful
		{
			get
			{
				return m_wasSuccessful;
			}
		}

		public RenPyIf() : base(RenPyStatementType.IF)
		{
			m_wasSuccessful = false;
		}
		
		public override void Parse(ref Scanner tokens)
		{
			tokens.Seek("if");
			tokens.Next();

			// Get the expression
			string expressionString = tokens.Seek(":").Trim();
			tokens.Next();

			var parser = ExpressionParserFactory.GetRenPyParser();
			m_expression = parser.ParseExpression(expressionString);
		}

		public override void Execute(RenPyState state)
		{
			// If evaluation succeeds, push back this block
			if (m_expression.Evaluate(state).GetValue(state).AsString(state) == "True") {
				string msg = "if " + m_expression + " evaluated to true";
				Static.Log(msg);

				m_wasSuccessful = true;
				state.Execution.PushStackFrame(NestedBlocks);
			}
			// If evaluation fails, skip this block
			else {
				string msg = "if " + m_expression + " evaluated to false";
				Static.Log(msg);

				m_wasSuccessful = false;
			}
		}

		public override string ToDebugString()
		{
			string str = "if " + m_expression + ":";
			return str;
		}
	}
}
