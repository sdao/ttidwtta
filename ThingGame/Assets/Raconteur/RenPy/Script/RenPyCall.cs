﻿using UnityEngine;
using System.Collections.Generic;

using DPek.Raconteur.RenPy.State;
using DPek.Raconteur.Util.Parser;

namespace DPek.Raconteur.RenPy.Script
{
	public class RenPyCall : RenPyStatement
	{
		[SerializeField]
		private string m_label;

		public RenPyCall() : base(RenPyStatementType.CALL)
		{
			// Nothing to do
		}

		public override void Parse(ref Scanner tokens)
		{
			tokens.Seek("call");
			tokens.Next();
			tokens.Skip(new string[]{" ","\t"});

			// Get the label that we want to call
			m_label = tokens.Next();
		}

		public override void Execute(RenPyState state)
		{
			var frame = state.Execution.InitialStackFrame.Blocks;
			state.Execution.PushStackFrame(frame);
			state.Execution.GoToLabel(m_label);
		}
		
		public override string ToDebugString()
		{
			string str = "call " + m_label;
			return str;
		}
	}
}
