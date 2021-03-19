using MaxyGames.uNode;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace MaxyGames {
	public static partial class CodeGenerator {
		public const string KEY_INFORMATION_HEAD = "@";
		public const string KEY_INFORMATION_TAIL = "#";
		public const string KEY_INFORMATION_VARIABLE = "V:";

		/// <summary>
		/// Wrap 'input' string with information of 'obj' so uNode can suggest what is the object that generates the code.
		/// </summary>
		/// <param name="input"></param>
		/// <param name="obj"></param>
		/// <returns></returns>
		public static string WrapWithInformation(string input, object obj) {
			if(!string.IsNullOrWhiteSpace(input)) {
				int firstIndex = 0;
				int lastIndex = input.Length;
				for (int i = 0; i < input.Length;i++) {
					if(!char.IsWhiteSpace(input[i])) {
						firstIndex = i;
						break;
					}
				}
				for (int i = input.Length - 1; i > 0; i--) {
					if(!char.IsWhiteSpace(input[i])) {
						lastIndex = i + 1;
						break;
					}
				}
				return input.Add(lastIndex, EndGenerateInformation(obj)).Add(firstIndex, BeginGenerateInformation(obj));
			}
			return null;
			// return input.AddFirst(BeginGenerateInformation(obj)).Add(EndGenerateInformation(obj));
		}

		static string BeginGenerateInformation(object obj) {
			if(obj is UnityEngine.Object) {
				return GenerateComment((obj as UnityEngine.Object).GetInstanceID().ToString().AddFirst(KEY_INFORMATION_HEAD));
			} else if(obj is VariableData) {
				return GenerateComment((obj as VariableData).Name.AddFirst(KEY_INFORMATION_HEAD + KEY_INFORMATION_VARIABLE));
			}
			return null;
		}

		static string EndGenerateInformation(object obj) {
			if(obj is UnityEngine.Object) {
				return GenerateComment((obj as UnityEngine.Object).GetInstanceID().ToString().AddFirst(KEY_INFORMATION_TAIL));
			} else if(obj is VariableData) {
				return GenerateComment((obj as VariableData).Name.AddFirst(KEY_INFORMATION_TAIL + KEY_INFORMATION_VARIABLE));
			}
			return null;
		}

		/// <summary>
		/// Generate debug code.
		/// </summary>
		/// <param name="comp"></param>
		/// <param name="state"></param>
		/// <returns></returns>
		public static string GenerateDebugCode(NodeComponent comp, StateType state) {
			string data = setting.debugPreprocessor ? "\n#if UNITY_EDITOR" : "";
			string s = state == StateType.Success ? "true" : (state == StateType.Failure ? "false" : "null");
			data += GenerateInvokeCode(typeof(uNodeUtility), "InvokeNode",
				"this",
				ParseValue(uNodeUtility.GetObjectID(graph)),
				ParseValue(uNodeUtility.GetObjectID(comp)),
				s).AddLineInFirst();
			if(setting.debugPreprocessor)
				data += "#endif".AddLineInFirst();
			return data;
		}

		/// <summary>
		/// Generate debug code.
		/// </summary>
		/// <param name="valueNode"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public static string GenerateDebugCode(Node valueNode, string value) {
			string data = setting.debugPreprocessor ? "\n#if UNITY_EDITOR" : "";
			data += GenerateInvokeCode(typeof(uNodeUtility), "InvokeValueNode",
				"this",
				ParseValue(uNodeUtility.GetObjectID(graph)),
				ParseValue(uNodeUtility.GetObjectID(valueNode)),
				value).AddLineInFirst();
			if(setting.debugPreprocessor)
				data += "#endif".AddLineInFirst();
			return data;
		}

		/// <summary>
		/// Generate debug code.
		/// </summary>
		/// <param name="member"></param>
		/// <returns></returns>
		public static string GenerateDebugCode(MemberData member) {
			if(!member.isAssigned)
				return null;
			if(member.targetType != MemberData.TargetType.FlowNode &&
				member.targetType != MemberData.TargetType.ValueNode &&
				member.targetType != MemberData.TargetType.FlowInput)
				return null;
			string data = setting.debugPreprocessor ? "\n#if UNITY_EDITOR" : "";
			if(member.targetType == MemberData.TargetType.FlowNode) {
				data += GenerateInvokeCode(typeof(uNodeUtility), "InvokeFlowTransition",
					"this",
					ParseValue(uNodeUtility.GetObjectID(graph)),
					ParseValue(uNodeUtility.GetObjectID(member.GetInstance() as UnityEngine.Object)),
					ParseValue(int.Parse(member.startName))).AddLineInFirst();
			} else if(member.targetType == MemberData.TargetType.FlowInput) {
				data += GenerateInvokeCode(typeof(uNodeUtility), "InvokeFlowTransition",
					"this",
					ParseValue(uNodeUtility.GetObjectID(graph)),
					ParseValue(uNodeUtility.GetObjectID(member.GetInstance() as UnityEngine.Object)),
					ParseValue(member.startName)).AddLineInFirst();
			} else {
				throw new System.NotSupportedException($"Target type:{member.targetType} is not supported to generate debug code");
			}
			if(setting.debugPreprocessor)
				data += "#endif".AddLineInFirst();
			return data;
		}

		/// <summary>
		/// Generate debug code.
		/// </summary>
		/// <param name="member"></param>
		/// <param name="value"></param>
		/// <param name="isSet"></param>
		/// <returns></returns>
		public static string GenerateDebugCode(MemberData member, string value, bool isSet = false) {
			if(!member.isAssigned)
				return null;
			if(member.targetType != MemberData.TargetType.FlowNode &&
				member.targetType != MemberData.TargetType.ValueNode &&
				member.targetType != MemberData.TargetType.FlowInput)
				return null;
			string data = setting.debugPreprocessor ? "\n#if UNITY_EDITOR" : "";
			if(member.targetType == MemberData.TargetType.FlowNode) {
				data += GenerateInvokeCode(typeof(uNodeUtility), nameof(uNodeUtility.InvokeFlowTransition),
					"this",
					ParseValue(uNodeUtility.GetObjectID(graph)),
					ParseValue(uNodeUtility.GetObjectID(member.GetInstance() as UnityEngine.Object)),
					ParseValue(int.Parse(member.startName))).AddLineInFirst();
			} else if(member.targetType == MemberData.TargetType.ValueNode) {
				data += GenerateInvokeCode(typeof(uNodeUtility), nameof(uNodeUtility.InvokeValueTransition),
					"this",
					ParseValue(uNodeUtility.GetObjectID(graph)),
					ParseValue(uNodeUtility.GetObjectID(member.GetInstance() as UnityEngine.Object)),
					ParseValue(int.Parse(member.startName)),
					value,
					ParseValue(isSet)).AddLineInFirst();
			} else {
				throw new System.NotSupportedException("Target type is not supported to generate debug code");
			}
			if(setting.debugPreprocessor)
				data += "#endif".AddLineInFirst();
			return data;
		}

		/// <summary>
		/// Generate debug code.
		/// </summary>
		/// <param name="comp"></param>
		/// <param name="transition"></param>
		/// <returns></returns>
		public static string GenerateDebugCode(NodeComponent comp, TransitionEvent transition) {
			string data = setting.debugPreprocessor ? "\n#if UNITY_EDITOR" : "";
			data += GenerateInvokeCode(typeof(uNodeUtility), "InvokeTransition",
				"this",
				ParseValue(uNodeUtility.GetObjectID(graph)),
				ParseValue(uNodeUtility.GetObjectID(transition))).AddLineInFirst();
			if(setting.debugPreprocessor)
				data += "#endif".AddLineInFirst();
			return data;
		}
	}
}