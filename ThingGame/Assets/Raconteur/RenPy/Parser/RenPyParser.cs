﻿using UnityEngine;
using System.Collections.Generic;
using System.Linq;

using DPek.Raconteur.RenPy.Script;
using DPek.Raconteur.RenPy.State;
using DPek.Raconteur.Util.Parser;

namespace DPek.Raconteur.RenPy.Parser
{
	public class RenPyParser
	{
		public static List<RenPyBlock> Parse(string[] lines)
		{
			var tokenizer = new Tokenizer(false);
			string[] parseTokens;
			parseTokens = "[ ] ( ) # \\\" \" ' , ; = + - * / \\ : $".Split(' ');
			tokenizer.SetupTokens(parseTokens);

			// Create the scanner
			var tokens = new LinkedList<string>();
			for (int line = 0; line < lines.Length; line++) {
				string[] arr = tokenizer.Tokenize(ref lines[line]);
				for (int i = 0; i < arr.Length; i++) {
					tokens.AddLast(arr[i]);
				}
				tokens.AddLast("\n");
			}
			var scanner = new Scanner(ref tokens);

			// Parse the statements
			var statements = new List<RenPyStatement>();
			var levels = new List<int>();
			scanner.SkipEmptyLines();
			while (scanner.HasNext()) {

				int level = scanner.Skip(new string[]{" ","\t"});

				RenPyStatement statement = ParseStatement(ref scanner);
				if (statement != null) {
					statements.Add(statement);
					levels.Add(level);
				}

				scanner.SkipEmptyLines();
			}

			// Create blocks out of the statements
			int startIndex = 0;
			var blocks = ParseBlocks(ref statements, ref levels, ref startIndex);

			// Get init statements in order from low to high priority
			RenPyInitPhase initPhase = ExtractInitPhase(blocks);
			var initBlocks = new List<RenPyBlock>();
			foreach(var kvp in initPhase.Statements.OrderBy(i => i.Key)) {
				RenPyBlock block;
				block = ScriptableObject.CreateInstance<RenPyBlock>();
				block.Statements = kvp.Value;
				initBlocks.Add(block);
			}

			// Combine init blocks with the rest of the blocks
			initBlocks.AddRange(blocks);

			return initBlocks;
		}

		/// <summary>
		/// Extracts and removes the init phase from the passed list of
		/// RenPyBlocks.
		/// </summary>
		/// <returns>
		/// The init phase for the passed blocks.
		/// </returns>
		/// <param name="blocks">
		/// A list of RenPyBlocks to extract the init phase from.
		/// </param>
		private static RenPyInitPhase ExtractInitPhase(List<RenPyBlock> blocks) {
			RenPyInitPhase initPhase = new RenPyInitPhase();

			// Read every statement in this block
			for (int i = 0 ; i < blocks.Count; ++i) {
				var block = blocks[i];
				for(int j = 0; j < block.StatementCount; ++j) {
					var statement = block[j];
					bool remove = false;

					// Look for statements that must be part of the init phase
					if (statement is RenPyInit) {
						int priority = (statement as RenPyInit).Priority;
						initPhase.AddStatement(priority, statement);
						remove = true;
					} else if (statement is RenPyCharacter) {
						initPhase.AddStatement(0, statement);
						remove = true;
					} else if (statement is RenPyImage) {
						initPhase.AddStatement(990, statement);
						remove = true;
					}
					// Look for init phase statements inside nested blocks
					else if (statement.NestedBlocks != null) {
						var addPhase = ExtractInitPhase(statement.NestedBlocks);
						initPhase.AddPhase(ref addPhase);
					}
					
					// Remove the statement if needed
					if(remove) {
						block.RemoveAt(j);
						--j;
					}
				}
			}
			return initPhase;
		}

		private static List<RenPyBlock> ParseBlocks(ref List<RenPyStatement> statements,
		                                            ref List<int> levels,
		                                            ref int index)
		{
			var blocks = new List<RenPyBlock>();
			int currentLevel = levels[index];
			var statementBlock = new List<RenPyStatement>();

			for (; index < statements.Count; ++index)
			{
				// Belongs to this block
				if (levels[index] == currentLevel) {
					statementBlock.Add(statements[index]);
				}

				// Reached the end of this block
				else if (levels[index] < currentLevel) {
					if (statementBlock.Count > 0) {
						RenPyBlock block;
						block = ScriptableObject.CreateInstance<RenPyBlock>();
						block.Statements = statementBlock;
						blocks.Add(block);
					}
					--index;
					return blocks;
				}

				// Belongs to a nested block
				else {
					var previousStatement = statementBlock[statementBlock.Count - 1];
					var nestedBlocks = ParseBlocks(ref statements, ref levels, ref index);
					previousStatement.NestedBlocks = nestedBlocks;
				}
			}

			if (statementBlock.Count > 0) {
				RenPyBlock block;
				block = ScriptableObject.CreateInstance<RenPyBlock>();
				block.Statements = statementBlock;
				blocks.Add(block);
			}
			return blocks;
		}

		private static RenPyStatement ParseStatement(ref Scanner scanner)
		{
			switch (scanner.Peek()) {
				case "$":
					return NewStatement<RenPyVariable>(ref scanner);
				case "#":
					var statement = NewStatement<RenPyComment>(ref scanner);
					ScriptableObject.DestroyImmediate(statement, true);
					return null;
				case "call":
					return NewStatement<RenPyCall>(ref scanner);
				case "define":
					return NewStatement<RenPyCharacter>(ref scanner);
				case "elif":
					return NewStatement<RenPyElif>(ref scanner);
				case "else":
					return NewStatement<RenPyElse>(ref scanner);
				case "hide":
					return NewStatement<RenPyHide>(ref scanner);
				case "if":
					return NewStatement<RenPyIf>(ref scanner);
				case "init":
					return NewStatement<RenPyInit>(ref scanner);
				case "image":
					return NewStatement<RenPyImage>(ref scanner);
				case "jump":
					return NewStatement<RenPyJump>(ref scanner);
				case "label":
					return NewStatement<RenPyLabel>(ref scanner);
				case "menu":
					return NewStatement<RenPyMenu>(ref scanner);
				case "pass":
					return NewStatement<RenPyPass>(ref scanner);
				case "pause":
					return NewStatement<RenPyPause>(ref scanner);
				case "play":
					return NewStatement<RenPyPlay>(ref scanner);
				case "queue":
					return NewStatement<RenPyQueue>(ref scanner);
				case "return":
					return NewStatement<RenPyReturn>(ref scanner);
				case "scene":
					return NewStatement<RenPyScene>(ref scanner);
				case "show":
					return NewStatement<RenPyShow>(ref scanner);
				case "stop":
					return NewStatement<RenPyStop>(ref scanner);
				case "while":
					return NewStatement<RenPyWhile>(ref scanner);
				case "window":
					return NewStatement<RenPyWindow>(ref scanner);
				default:
					if(scanner.PeekEnd() == ":") {
						return NewStatement<RenPyMenuChoice>(ref scanner);
					} else {
						return NewStatement<RenPySay>(ref scanner);
					}
			}
		}

		public static RenPyStatement NewStatement<T>(ref Scanner scanner)
			where T : RenPyStatement
		{
			var statement = ScriptableObject.CreateInstance<T>();
			statement.Parse(ref scanner);
			return statement;
		}
	}
}
