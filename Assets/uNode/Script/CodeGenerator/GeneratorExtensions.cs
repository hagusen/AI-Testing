using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace MaxyGames.uNode {
	/// <summary>
	/// Privides usefull extensions functions for code generation.
	/// </summary>
	public static class GeneratorExtensions {
		/// <summary>
		/// Split the members
		/// </summary>
		/// <param name="strs"></param>
		/// <returns></returns>
		public static IList<string> SplitMember(this string member) {
			if(string.IsNullOrEmpty(member))
				return member == null ? new List<string>() : new List<string>() { member };
			List<string> strs = new List<string>();
			int deep = 0;
			string current = "";
			for(int i=0;i<member.Length;i++) {
				var c = member[i];
				 if(c == '.') {
					if(deep == 0) {
						strs.Add(current);
						current = "";
					} else {
						current += c;
					}
				} else {
					current += c;
					if(c == '<' || c == '(') {
						deep++;
					} else if(c == '>' || c == ')') {
						deep--;
					}
				}
			}
			strs.Add(current);
			return strs;
		}

		/// <summary>
		/// Generate code for values.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static string ToCode(this object value) {
			return CodeGenerator.ParseValue(value);
		}

		/// <summary>
		/// Generate code for flow.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="from"></param>
		/// <param name="waitTarget"></param>
		/// <returns></returns>
		public static string ToFlowCode(this MemberData value, uNode.Node from, bool waitTarget = true) {
			return CodeGenerator.GenerateFlowCode(value, from, waitTarget);
		}

		/// <summary>
		/// Convert a string into valid variable name.
		/// </summary>
		/// <param name="str"></param>
		/// <param name="owner"></param>
		/// <returns></returns>
		public static string ToVariableName(this string str, object owner) {
			return CodeGenerator.GenerateVariableName(str, owner);
		}

		/// <summary>
		/// Generate code for values.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static string ParseValue(this object value) {
			return CodeGenerator.ParseValue(value);
		}

		/// <summary>
		/// Generate code for type.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static string ParseType(this Type value) {
			return CodeGenerator.ParseType(value);
		}

		/// <summary>
		/// Generate access element code.
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="index"></param>
		/// <returns></returns>
		public static string AccessElement(this string instance, string index) {
			return CodeGenerator.GenerateAccessElementCode(instance, index);
		}

		/// <summary>
		/// Generate access element code.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="index"></param>
		/// <returns></returns>
		public static string AccessElement(this Type type, string index) {
			return CodeGenerator.GenerateAccessElementCode(type, index);
		}

		/// <summary>
		/// Generate access element code.
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="index"></param>
		/// <returns></returns>
		public static string AccessElement(this object instance, string index) {
			return CodeGenerator.GenerateAccessElementCode(instance, index);
		}

		/// <summary>
		/// Generate access code.
		/// </summary>
		/// <param name="first"></param>
		/// <param name="members"></param>
		/// <returns></returns>
		public static string Access(this string first, params string[] members) {
			return CodeGenerator.GenerateAccessCode(first, members);
		}

		/// <summary>
		/// Generate access code.
		/// </summary>
		/// <param name="first"></param>
		/// <param name="members"></param>
		/// <returns></returns>
		public static string Access(this Type first, params string[] members) {
			return CodeGenerator.GenerateAccessCode(first, members);
		}

		/// <summary>
		/// Generate access code.
		/// </summary>
		/// <param name="first"></param>
		/// <param name="members"></param>
		/// <returns></returns>
		public static string Access(this object first, params string[] members) {
			return CodeGenerator.GenerateAccessCode(first, members);
		}

		/// <summary>
		/// Generate invoke code.
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="functionName"></param>
		/// <param name="paramObject"></param>
		/// <returns></returns>
		public static string InvokeCode(this string instance, string functionName, params string[] paramObject) {
			return CodeGenerator.GenerateInvokeCode(instance, functionName, paramObject);
		}

		/// <summary>
		/// Generate type convert code.
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="convertType"></param>
		/// <returns></returns>
		public static string ConvertCode(this string instance, Type convertType, bool extension = false) {
			if(extension) {
				CodeGenerator.RegisterUsingNamespace("MaxyGames");
				return CodeGenerator.GenerateGenericInvokeCode(convertType, instance, nameof(Extensions.ConvertTo), null).RemoveSemicolon();
			}
			return CodeGenerator.ParseType(convertType).Wrap() + instance;
		}

		/// <summary>
		/// Generate invoke code.
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="functionName"></param>
		/// <param name="paramObject"></param>
		/// <returns></returns>
		public static string InvokeCode(this object instance, string functionName, params string[] paramObject) {
			return CodeGenerator.GenerateInvokeCode(instance, functionName, paramObject);
		}

		/// <summary>
		/// Generate invoke code.
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="functionName"></param>
		/// <param name="paramObject"></param>
		/// <returns></returns>
		public static string InvokeCode(this object instance, string functionName, Type[] genericType, params string[] paramObject) {
			return CodeGenerator.GenerateInvokeCode(instance, functionName, genericType, paramObject);
		}

		/// <summary>
		/// Generate invoke code.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="functionName"></param>
		/// <param name="paramObject"></param>
		/// <returns></returns>
		public static string InvokeCode(this Type type, string functionName, params string[] paramObject) {
			return CodeGenerator.GenerateInvokeCode(type, functionName, paramObject);
		}

		/// <summary>
		/// Generate invoke code.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="functionName"></param>
		/// <param name="paramObject"></param>
		/// <returns></returns>
		public static string InvokeCode(this Type type, string functionName, Type[] genericType, params string[] paramObject) {
			return CodeGenerator.GenerateInvokeCode(type, functionName, genericType, paramObject);
		}

		/// <summary>
		/// Generate a new set code.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <param name="setType"></param>
		/// <returns></returns>
		public static string Set(this string left, string right, SetType setType = SetType.Change) {
			return CodeGenerator.GenerateSetCode(left, right, setType);
		}

		/// <summary>
		/// Generate a new set code.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <param name="setType"></param>
		/// <returns></returns>
		public static string Set(this string left, string right, Type leftType, Type rightType, SetType setType = SetType.Change) {
			return CodeGenerator.GenerateSetCode(left, right, leftType, rightType, setType);
		}

		/// <summary>
		/// Generate a new set code.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <param name="storeType"></param>
		/// <param name="setType"></param>
		/// <returns></returns>
		public static string Set(this string left, string right, Type storeType, SetType setType = SetType.Change) {
			return CodeGenerator.GenerateSetCode(left, right, storeType, storeType, setType);
		}

		/// <summary>
		/// Generate arithmetic operation.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <param name="arithmeticType"></param>
		/// <returns></returns>
		public static string Arithmetic(this string left, string right, ArithmeticType arithmeticType) {
			return CodeGenerator.GenerateArithmetiCode(left, right, arithmeticType);
		}

		/// <summary>
		/// Generate compare code.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <param name="compareType"></param>
		/// <returns></returns>
		public static string Compare(this string left, string right, ComparisonType compareType = ComparisonType.Equal) {
			return CodeGenerator.GenerateCompareCode(left, right, compareType);
		}

		/// <summary>
		/// Generate + operation.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <param name="arithmeticType"></param>
		/// <returns></returns>
		public static string AddOperation(this string left, string right) {
			return CodeGenerator.GenerateArithmetiCode(left, right, ArithmeticType.Add);
		}

		/// <summary>
		/// Generate * operation.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <param name="arithmeticType"></param>
		/// <returns></returns>
		public static string MultiplyOperation(this string left, string right) {
			return CodeGenerator.GenerateArithmetiCode(left, right, ArithmeticType.Multiply);
		}

		/// <summary>
		/// Generate * operation.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <param name="arithmeticType"></param>
		/// <returns></returns>
		public static string SubtractOperation(this string left, string right) {
			return CodeGenerator.GenerateArithmetiCode(left, right, ArithmeticType.Subtract);
		}

		/// <summary>
		/// Generate / operation.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <param name="arithmeticType"></param>
		/// <returns></returns>
		public static string DivideOperation(this string left, string right) {
			return CodeGenerator.GenerateArithmetiCode(left, right, ArithmeticType.Divide);
		}

		/// <summary>
		/// Generate and code.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static string AndOperation(this string left, string right) {
			return CodeGenerator.GenerateAndCode(left, right);
		}

		/// <summary>
		/// Generate or code.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static string OrOperation(this string left, string right) {
			return CodeGenerator.GenerateOrCode(left, right);
		}

		/// <summary>
		/// Generate not code.
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public static string NotOperation(this string str) {
			if(string.IsNullOrEmpty(str)) {
				return str;
			}
			return "!" + str;
		}

		/// <summary>
		/// Generate not code.
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public static string NotOperation(this string str, bool wrapCode) {
			if(!wrapCode)
				return NotOperation(str);
			if(string.IsNullOrEmpty(str)) {
				return str;
			}
			return "!(" + str + ")";
		}

		/// <summary>
		/// Generate negate code.
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public static string NegateOperation(this string str) {
			if(string.IsNullOrEmpty(str)) {
				return str;
			}
			return "-" + str;
		}

		/// <summary>
		/// Generate negate code.
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public static string NegateOperation(this string str, bool wrapCode) {
			if(!wrapCode)
				return NegateOperation(str);
			if(string.IsNullOrEmpty(str)) {
				return str;
			}
			return "-(" + str + ")";
		}

		/// <summary>
		/// Wrap a string value with brackets
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public static string Wrap(this string str) {
			return CodeGenerator.WrapCode(str);
		}

		/// <summary>
		/// Wrap a string value so the string will be generated without quotes.
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public static CodeGenerator.StringWrapper WrapString(this string str) {
			return CodeGenerator.WrapString(str);
		}
	}
}