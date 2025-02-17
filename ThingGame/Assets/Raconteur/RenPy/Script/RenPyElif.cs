﻿using UnityEngine;

using DPek.Raconteur.RenPy.State;
using DPek.Raconteur.Util.Parser;

namespace DPek.Raconteur.RenPy.Script
{
	/// <summary>
	/// Ren'Py elif statement.
	/// </summary>
	public class RenPyElif : RenPyStatement
	{
		[SerializeField]
		private Expression m_expression;
		public Expression Expression
		{
			get
			{
				return m_expression;
			}
		}

		/// <summary>
		/// Whether or not last time this statement was executed that it
		/// or some if statement before it evaluated as true.
		/// </summary>
		private bool m_wasSuccessful;
		public bool WasSuccessful
		{
			get
			{
				return m_wasSuccessful;
			}
		}

		public RenPyElif() : base(RenPyStatementType.ELIF)
		{
			m_wasSuccessful = false;
		}

		public override void Parse(ref Scanner tokens)
		{
			tokens.Seek("elif");
			tokens.Next();

			// Get the expression
			string expressionString = tokens.Seek(":").Trim();
			tokens.Next();

			var parser = ExpressionParserFactory.GetRenPyParser();
			m_expression = parser.ParseExpression(expressionString);
		}

		public override void Execute(RenPyState state)
		{
			// Check if evaluation is necessary
			var prev = state.Execution.GetPreviousStatement() as RenPyIf;
			if (prev == null) {
				var elif = state.Execution.GetPreviousStatement() as RenPyElif;
				if (elif == null) {
					var msg = "elif expression has no preceding if statement";
					UnityEngine.Debug.LogError(msg);
					return;
				}
				else if (elif.WasSuccessful) {
					m_wasSuccessful = true;
					return;
				}
			}
			else if (prev.WasSuccessful) {
				m_wasSuccessful = true;
				return;
			}

			// If evaluation succeeds, push back this block
			if (m_expression.Evaluate(state).GetValue(state).AsString(state) == "True") {
				string msg = "elif " + m_expression + " evaluated to true";
				Static.Log(msg);

				m_wasSuccessful = true;
				state.Execution.PushStackFrame(NestedBlocks);
			}
			// If evaluation fails, skip this block
			else {
				string msg = "elif " + m_expression + " evaluated to false";
				Static.Log(msg);

				m_wasSuccessful = false;
			}
		}

		public override string ToDebugString()
		{
			string str = "elif " + m_expression + ":";
			return str;
		}
	}
}
