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
		#region Others
		/// <summary>
		/// Generate a flow statements code
		/// </summary>
		/// <param name="statements"></param>
		/// <returns></returns>
		public static string GenerateFlowStatement(params string[] statements) {
			string result = null;
			for (int i = 0; i < statements.Length;i++) {
				if(string.IsNullOrEmpty(result)) {
					result += statements[i];
				} else {
					result += statements[i].AddLineInFirst();
				}
			}
			return result;
		}

		/// <summary>
		/// Generate this keyword.
		/// To do: return the class name on class is static
		/// </summary>
		/// <returns></returns>
		public static string GenerateThis() {
			return "this";
		}
		#endregion

        #region If Statement
		/// <summary>
		/// Generate a new if statement code.
		/// </summary>
		/// <param name="conditions"></param>
		/// <param name="contents"></param>
		/// <param name="isAnd"></param>
		/// <returns></returns>
		public static string GenerateIfStatement(string[] conditions, string contents, bool isAnd = true) {
			string condition = null;
			for(int i = 0; i < conditions.Length; i++) {
				if(i > 0) {
					condition += isAnd ? " && " : " || ";
				}
				condition += conditions[i];
			}
			return GenerateCondition("if", condition, contents);
		}

		/// <summary>
		/// Generate a new if statement code.
		/// </summary>
		/// <param name="condition"></param>
		/// <param name="contents"></param>
		/// <returns></returns>
		public static string GenerateIfStatement(string condition, string contents) {
			return GenerateCondition("if", condition, contents);
		}

		/// <summary>
		/// Generate a new if statement code.
		/// </summary>
		/// <param name="condition"></param>
		/// <param name="contents"></param>
		/// <param name="elseContents"></param>
		/// <returns></returns>
		public static string GenerateIfStatement(string condition, string contents, string elseContents) {
			if(!string.IsNullOrEmpty(elseContents)) {
				if(string.IsNullOrEmpty(contents)) {
					return GenerateCondition("if", condition.AddFirst("!(").Add(")"), elseContents);
				}
				string data = GenerateCondition("if", condition, contents);
				data += " else {" + elseContents.AddLineInFirst().AddTabAfterNewLine(1) + "\n}";
				return data;
			}
			return GenerateCondition("if", condition, contents);
		}

		/// <summary>
		/// Generate a new if statement code.
		/// </summary>
		/// <param name="conditions"></param>
		/// <param name="contents"></param>
		/// <param name="elseContents"></param>
		/// <param name="isAnd"></param>
		/// <returns></returns>
		public static string GenerateIfStatement(string[] conditions, string contents, string elseContents, bool isAnd = true) {
			string condition = null;
			for(int i = 0; i < conditions.Length; i++) {
				if(i > 0) {
					condition += isAnd ? " && " : " || ";
				}
				condition += conditions[i];
			}
			if(!string.IsNullOrEmpty(elseContents)) {
				if(string.IsNullOrEmpty(contents)) {
					return GenerateCondition("if", condition.AddFirst("!(").Add(")"), elseContents);
				}
				string data = GenerateCondition("if", condition, contents);
				data += " else {" + elseContents.AddLineInFirst().AddTabAfterNewLine(1) + "\n}";
				return data;
			}
			return GenerateCondition("if", condition, contents);
		}
		#endregion

        #region For Statement
		/// <summary>
		/// Generate a new for statement code.
		/// </summary>
		/// <param name="variableName"></param>
		/// <param name="condition"></param>
		/// <param name="firstValue"></param>
		/// <param name="setVariable"></param>
		/// <param name="setType"></param>
		/// <param name="contents"></param>
		/// <returns></returns>
		public static string GenerateForStatement(string variableName, string condition, object firstValue, object setVariable, SetType setType, string contents) {
			string data = "for(" +
				ParseType(typeof(int)) + " " + variableName + " = " + ParseValue(firstValue) + ";" +
				condition + ";" + GenerateSetCode(variableName, setVariable, setType) + ") {";
			if(!string.IsNullOrEmpty(contents)) {
				data += ("\n" + contents).AddTabAfterNewLine(1) + "\n";
			}
			data += "}";
			return data;
		}

		/// <summary>
		/// Generate a new for statement code.
		/// </summary>
		/// <param name="initializer"></param>
		/// <param name="condition"></param>
		/// <param name="iterator"></param>
		/// <param name="contents"></param>
		/// <returns></returns>
		public static string GenerateForStatement(string initializer, string condition, string iterator, string contents) {
			string data = "for(" + initializer + "; " + condition + "; " + iterator + ") {";
			if(!string.IsNullOrEmpty(contents)) {
				data += ("\n" + contents).AddTabAfterNewLine(1) + "\n";
			}
			data += "}";
			return data;
		}
		#endregion

        #region Set Code
		/// <summary>
		/// Generate a new set code.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <param name="setType"></param>
		/// <returns></returns>
		public static string GenerateSetCode(string left, string right, SetType setType = SetType.Change) {
			switch(setType) {
				case SetType.Change:
					return left + " = " + right + ";";
				case SetType.Add:
					return left + " += " + right + ";";
				case SetType.Subtract:
					return left + " -= " + right + ";";
				case SetType.Multiply:
					return left + " *= " + right + ";";
				case SetType.Divide:
					return left + " /= " + right + ";";
				case SetType.Modulo:
					return left + " %= " + right + ";";
			}
			return null;
		}

		public static string GenerateSetCode(string left, string right, Type leftType, Type rightType, SetType setType = SetType.Change) {
			if(leftType != null && !leftType.IsCastableTo(typeof(Delegate)) && !(leftType is RuntimeType)) {
				if(rightType == null || !rightType.IsCastableTo(leftType) && !rightType.IsValueType && rightType != typeof(string)) {
					if(leftType.IsValueType) {
						right = right.Insert(0, "(" + ParseType(leftType) + ")");
					} else if(right != "null") {
						right = right + " as " + ParseType(leftType);
					}
				}
			}
			bool flag = !generatePureScript && left.EndsWith("\")");
			if(flag) {
				var strs = left.SplitMember();
				var lastStr = strs[strs.Count - 1];
				string setCode = null;
				if(lastStr.StartsWith(nameof(RuntimeComponent.GetVariable) + "<")) {
					setCode = nameof(RuntimeComponent.SetVariable);
					if(right != "null" && leftType.IsCastableTo(typeof(Delegate))) {
						right = right.Wrap().Insert(0, "(" + ParseType(leftType) + ")");
					}
				} else if(lastStr.StartsWith(nameof(RuntimeComponent.GetProperty) + "<")) {
					setCode = nameof(RuntimeComponent.GetProperty);
					if(right != "null" && leftType.IsCastableTo(typeof(Delegate))) {
						right = right.Wrap().Insert(0, "(" + ParseType(leftType) + ")");
					}
				}
				if(setCode != null) {
					string code = null;
					switch(setType) {
						case SetType.Subtract:
							code = "-";
							break;
						case SetType.Divide:
							code = "/";
							break;
						case SetType.Add:
							code = "+";
							break;
						case SetType.Multiply:
							code = "*";
							break;
						case SetType.Modulo:
							code = "%";
							break;
					}
					int firstIndex = lastStr.IndexOf("\"");
					string vName = lastStr.Substring(firstIndex, lastStr.LastIndexOf("\"") - firstIndex + 1);
					if(code != null) {
						strs[strs.Count - 1] = DoGenerateInvokeCode(setCode, new string[] { vName, right, code.ParseValue() });
					} else {
						strs[strs.Count - 1] = DoGenerateInvokeCode(setCode, new string[] { vName, right });
					}
					left = string.Join(".", strs);
					return left;
				}
			}
			return GenerateSetCode(left, right, setType);
		}
		#endregion

		#region Access Code
		/// <summary>
		/// Generate access code.
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="members"></param>
		/// <returns></returns>
		public static string GenerateAccessElementCode(string instance, string index) {
			return instance.Add("[").Add(index).Add("]");
		}

		/// <summary>
		/// Generate access code.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="members"></param>
		/// <returns></returns>
		public static string GenerateAccessElementCode(Type type, string index) {
			return GenerateAccessElementCode(ParseType(type), index);
		}

		/// <summary>
		/// Generate access code.
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="members"></param>
		/// <returns></returns>
		public static string GenerateAccessElementCode(object instance, string index) {
			return GenerateAccessElementCode(ParseValue(instance), index);
		}

		/// <summary>
		/// Generate access code.
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="members"></param>
		/// <returns></returns>
		public static string GenerateAccessCode(string instance, params string[] members) {
			string result = instance;
			if(members.Length > 0 && (string.IsNullOrEmpty(result) || result == "null")) {
				throw new Exception("The generated instance is null");
			}
			foreach(var m in members) {
				result += m.AddFirst(".");
			}
			return result;
		}

		/// <summary>
		/// Generate access code.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="members"></param>
		/// <returns></returns>
		public static string GenerateAccessCode(Type type, params string[] members) {
			string result = ParseType(type);
			return GenerateAccessCode(result, members);
		}

		/// <summary>
		/// Generate access code.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="members"></param>
		/// <returns></returns>
		public static string GenerateAccessCode(object instance, params string[] members) {
			string result = ParseValue(instance);
			return GenerateAccessCode(result, members);
		}
		#endregion

        #region New Object Code
		/// <summary>
		/// Generate a new object creation code.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		public static string GenerateNewObjectCode(System.Type type, params string[] parameters) {
			return GenerateNewObjectCode(ParseType(type), parameters);
		}

		/// <summary>
		/// Generate a new object creation code.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		public static string GenerateNewObjectCode(string type, params string[] parameters) {
			string paramName = "";
			int index = 0;
			foreach(string o in parameters) {
				if(index != 0) {
					paramName += ", ";
				}
				paramName += o;
				index++;
			}
			if(string.IsNullOrEmpty(paramName)) {
				return "new " + type + "()";
			}
			return "new " + type + "(" + paramName + ")";
		}
		#endregion

		#region Generic InvokeCode
		/// <summary>
		/// Generate generic invoke code.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="variable"></param>
		/// <param name="functionName"></param>
		/// <param name="paramObject"></param>
		/// <returns></returns>
		public static string GenerateGenericInvokeCode<T>(string variable, string functionName, params string[] paramObject) {
			return GenerateGenericInvokeCode(typeof(T), variable, functionName, paramObject);
		}

		/// <summary>
		/// Generate generic invoke code.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="variable"></param>
		/// <param name="functionName"></param>
		/// <param name="paramObject"></param>
		/// <returns></returns>
		public static string GenerateGenericInvokeCode<T>(object variable, string functionName, params string[] paramObject) {
			return GenerateGenericInvokeCode(typeof(T), variable, functionName, paramObject);
		}

		/// <summary>
		/// Generate generic invoke code.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="type"></param>
		/// <param name="functionName"></param>
		/// <param name="paramObject"></param>
		/// <returns></returns>
		public static string GenerateGenericInvokeCode<T>(Type type, string functionName, params string[] paramObject) {
			return GenerateGenericInvokeCode(typeof(T), type, functionName, paramObject);
		}

		/// <summary>
		/// Generate generic invoke code.
		/// </summary>
		/// <param name="genericType"></param>
		/// <param name="variable"></param>
		/// <param name="functionName"></param>
		/// <param name="paramObject"></param>
		/// <returns></returns>
		public static string GenerateGenericInvokeCode(Type genericType, string variable, string functionName, params string[] paramObject) {
			if(string.IsNullOrEmpty(variable))
				return DoGenerateInvokeCode(functionName, paramObject, new Type[1] { genericType });
			return DoGenerateInvokeCode(variable + "." + functionName, paramObject, new Type[1] { genericType });
		}

		/// <summary>
		/// Generate generic invoke code.
		/// </summary>
		/// <param name="genericType"></param>
		/// <param name="variable"></param>
		/// <param name="functionName"></param>
		/// <param name="paramObject"></param>
		/// <returns></returns>
		public static string GenerateGenericInvokeCode(Type genericType, object variable, string functionName, params string[] paramObject) {
			string data = ParseValue(variable).Replace("\"", "");
			if(string.IsNullOrEmpty(data))
				return null;
			return DoGenerateInvokeCode(data + "." + functionName, paramObject, new Type[1] { genericType });
		}

		/// <summary>
		/// Generate generic invoke code.
		/// </summary>
		/// <param name="genericType"></param>
		/// <param name="type"></param>
		/// <param name="functionName"></param>
		/// <param name="paramObject"></param>
		/// <returns></returns>
		public static string GenerateGenericInvokeCode(Type genericType, Type type, string functionName, params string[] paramObject) {
			string data = ParseType(type);
			if(string.IsNullOrEmpty(data))
				return null;
			return DoGenerateInvokeCode(data + "." + functionName, paramObject, new Type[1] { genericType });
		}

		/// <summary>
		/// Generate generic invoke code.
		/// </summary>
		/// <param name="genericType"></param>
		/// <param name="variable"></param>
		/// <param name="functionName"></param>
		/// <param name="paramObject"></param>
		/// <returns></returns>
		public static string GenerateGenericInvokeCode(Type[] genericType, string variable, string functionName, params string[] paramObject) {
			if(string.IsNullOrEmpty(variable))
				return DoGenerateInvokeCode(functionName, paramObject, genericType);
			return DoGenerateInvokeCode(variable + "." + functionName, paramObject, genericType);
		}

		/// <summary>
		/// Generate generic invoke code.
		/// </summary>
		/// <param name="genericType"></param>
		/// <param name="variable"></param>
		/// <param name="functionName"></param>
		/// <param name="paramObject"></param>
		/// <returns></returns>
		public static string GenerateGenericInvokeCode(Type[] genericType, object variable, string functionName, params string[] paramObject) {
			string data = ParseValue(variable).Replace("\"", "");
			if(string.IsNullOrEmpty(data))
				return null;
			return DoGenerateInvokeCode(data + "." + functionName, paramObject, genericType);
		}

		/// <summary>
		/// Generate generic invoke code.
		/// </summary>
		/// <param name="genericType"></param>
		/// <param name="type"></param>
		/// <param name="functionName"></param>
		/// <param name="paramObject"></param>
		/// <returns></returns>
		public static string GenerateGenericInvokeCode(Type[] genericType, Type type, string functionName, params string[] paramObject) {
			string data = ParseType(type);
			if(string.IsNullOrEmpty(data))
				return null;
			return DoGenerateInvokeCode(data + "." + functionName, paramObject, genericType);
		}
		#endregion

		#region Break
		/// <summary>
		/// Get break code.
		/// </summary>
		/// <returns></returns>
		public static string GenerateBreak() {
			return "break;";
		}

		/// <summary>
		/// Get yield break code.
		/// </summary>
		/// <returns></returns>
		public static string GenerateYieldBreak() {
			return "yield break;";
		}
		#endregion

		#region Invoke Code
        /// <summary>
		/// Generate invoke code.
		/// </summary>
		/// <param name="variable"></param>
		/// <param name="functionName"></param>
		/// <param name="paramObject"></param>
		/// <returns></returns>
		public static string GenerateInvokeCode(string variable, string functionName, params string[] paramObject) {
			return GenerateInvokeCode(variable, functionName, null, paramObject);
		}

		/// <summary>
		/// Generate invoke code.
		/// </summary>
		/// <param name="variable"></param>
		/// <param name="functionName"></param>
		/// <param name="genericType"></param>
		/// <param name="paramObject"></param>
		/// <returns></returns>
		public static string GenerateInvokeCode(string variable, string functionName, Type[] genericType, params string[] paramObject) {
			if (string.IsNullOrEmpty(variable)) {
				return DoGenerateInvokeCode(functionName, paramObject, genericType);
			} else if(string.IsNullOrEmpty(functionName)) {
				return DoGenerateInvokeCode(variable, paramObject, genericType);
			}
			if(variable.EndsWith("[]")) {
				return DoGenerateInvokeCode(variable.RemoveLast(2) + "[" + functionName + "]", paramObject);
			}
			return DoGenerateInvokeCode(variable + "." + functionName, paramObject, genericType);
		}

		/// <summary>
		/// Generate invoke code.
		/// </summary>
		/// <param name="variable"></param>
		/// <param name="functionName"></param>
		/// <param name="paramObject"></param>
		/// <returns></returns>
		public static string GenerateInvokeCode(object variable, string functionName, params string[] paramObject) {
			return GenerateInvokeCode(variable, functionName, null, paramObject);
		}

		/// <summary>
		/// Generate invoke code.
		/// </summary>
		/// <param name="variable"></param>
		/// <param name="functionName"></param>
		/// <param name="paramObject"></param>
		/// <returns></returns>
		public static string GenerateInvokeCode(object variable, string functionName, Type[] genericType, params string[] paramObject) {
			string data = ParseValue(variable).Replace("\"", "");
			if(string.IsNullOrEmpty(data))
				return null;
			if(data.EndsWith("[]")) {
				return DoGenerateInvokeCode(data.RemoveLast(2) + "[" + functionName + "]", paramObject);
			}
			return DoGenerateInvokeCode(data + "." + functionName, paramObject, genericType);
		}

		/// <summary>
		/// Generate invoke code.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="functionName"></param>
		/// <param name="paramObject"></param>
		/// <returns></returns>
		public static string GenerateInvokeCode(Type type, string functionName, params string[] paramObject) {
			return GenerateInvokeCode(type, functionName, null, paramObject);
		}

		/// <summary>
		/// Generate invoke code.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="functionName"></param>
		/// <param name="paramObject"></param>
		/// <returns></returns>
		public static string GenerateInvokeCode(Type type, string functionName, Type[] genericType, params string[] paramObject) {
			string data = ParseType(type);
			if(string.IsNullOrEmpty(data))
				return null;
			if(data.EndsWith("[]")) {
				return DoGenerateInvokeCode(data.RemoveLast(2) + "[" + functionName + "]", paramObject);
			}
			return DoGenerateInvokeCode(data + "." + functionName, paramObject, genericType);
		}
		
		private static string DoGenerateInvokeCode(string functionName, string[] paramObject, Type[] genericType = null) {
			if(functionName.Contains(';')) {
				functionName = functionName.RemoveSemicolon();
			}
			string paramName = "";
			if (paramObject != null) {
				for (int i = 0; i < paramObject.Length;i++) {
					if (i != 0) {
						paramName += ", ";
					}
					paramName += paramObject[i];
				}
			}
			string genericData = null;
			if(genericType != null && genericType.Length > 0) {
				genericData += "<";
				for(int i = 0; i < genericType.Length; i++) {
					if(i > 0) {
						genericData += ",";
					}
					genericData += ParseType(genericType[i]);
				}
				genericData += ">";
			}
			if(string.IsNullOrEmpty(paramName)) {
				return functionName + genericData + "();";
			}
			if(functionName.EndsWith("[]")) {
				return functionName.RemoveLast(2) + "[" + paramName + "];";
			} else if(functionName.EndsWith("]") && string.IsNullOrEmpty(paramName)) {
				return functionName.AddSemicolon();
			}
			return functionName + genericData + "(" + paramName + ");";
		}
		
		private static string DoGenerateInvokeCode(string functionName, string[] paramValues, string[] genericTypes) {
			if(functionName.Contains(';')) {
				functionName = functionName.RemoveSemicolon();
			}
			string paramName = "";
			if (paramValues != null) {
				for (int i = 0; i < paramValues.Length;i++) {
					if (i != 0) {
						paramName += ", ";
					}
					paramName += paramValues[i];
				}
			}
			string genericData = null;
			if(genericTypes != null && genericTypes.Length > 0) {
				genericData += "<";
				for(int i = 0; i < genericTypes.Length; i++) {
					if(i > 0) {
						genericData += ",";
					}
					genericData += genericTypes[i];
				}
				genericData += ">";
			}
			if(string.IsNullOrEmpty(paramName)) {
				return functionName + genericData + "();";
			}
			if(functionName.EndsWith("[]")) {
				return functionName.RemoveLast(2) + "[" + paramName + "];";
			} else if(functionName.EndsWith("]") && string.IsNullOrEmpty(paramName)) {
				return functionName.AddSemicolon();
			}
			return functionName + genericData + "(" + paramName + ");";
		}
		#endregion

        #region Foreach Statement
		/// <summary>
		/// Generate a new foreach statement code.
		/// </summary>
		/// <param name="elementType"></param>
		/// <param name="variableName"></param>
		/// <param name="iteration"></param>
		/// <param name="contents"></param>
		/// <returns></returns>
		public static string GenerateForeachStatement(Type elementType, string variableName, string iteration, string contents) {
			string data = "foreach(" + ParseType(elementType) + " " + variableName + " in " + iteration + ") {";
			if(!string.IsNullOrEmpty(contents)) {
				data += ("\n" + contents).AddTabAfterNewLine(1) + "\n";
			}
			data += "}";
			return data;
		}
		#endregion

        #region Make Array
		/// <summary>
		/// Generate a new array creation code.
		/// </summary>
		/// <param name="elementType"></param>
		/// <param name="values"></param>
		/// <returns></returns>
		public static string GenerateMakeArray(Type elementType, params string[] values) {
			string elementObject = "[0]";
			if(values != null && values.Length > 0) {
				int index = 0;
				elementObject = "[" + //values.Length + 
					"] {";
				foreach(object o in values) {
					if(index != 0) {
						elementObject += ",";
					}
					elementObject += " " + o;
					index++;
				}
				elementObject += " }";
			}
			return "new " + ParseType(elementType) + elementObject;
		}

		/// <summary>
		/// Generate a new array creation code.
		/// </summary>
		/// <param name="elementType"></param>
		/// <param name="arrayLength"></param>
		/// <param name="values"></param>
		/// <returns></returns>
		public static string GenerateMakeArray(Type elementType, MemberData arrayLength, params string[] values) {
			string length = arrayLength.isTargeted ? ParseValue((object)arrayLength) : string.Empty;
			string elementObject = "[" + length + "]";
			if(values != null && values.Length > 0) {
				int index = 0;
				elementObject = "[" + length + "] {";
				foreach(object o in values) {
					if(index != 0) {
						elementObject += ",";
					}
					elementObject += " " + o;
					index++;
				}
				elementObject += " }";
			}
			return "new " + ParseType(elementType) + elementObject;
		}
		#endregion

        #region Anonymous Method
		/// <summary>
		/// Generate new anonymous method code.
		/// </summary>
		/// <param name="types"></param>
		/// <param name="parameterNames"></param>
		/// <param name="contents"></param>
		/// <returns></returns>
		public static string GenerateAnonymousMethod(IList<Type> types, IList<string> parameterNames, string contents) {
			if(types != null && parameterNames != null && types.Count != parameterNames.Count)
				return null;
			string parameters = null;
			if(types != null && parameterNames != null) {
				for(int i = 0; i < types.Count; i++) {
					if(i != 0) {
						parameters += ", ";
					}
					parameters += ParseType(types[i]) + " " + parameterNames[i];
				}
			}
			string data = "(" + parameters + ") => {";
			if(!string.IsNullOrEmpty(contents)) {
				data += contents.AddLineInFirst().AddTabAfterNewLine(1).AddLineInEnd();
			}
			data += "}";
			return data;
		}
		#endregion

        #region Condition
		/// <summary>
		/// Generate a new condition code.
		/// </summary>
		/// <param name="conditionKey"></param>
		/// <param name="conditionContent"></param>
		/// <param name="contents"></param>
		/// <returns></returns>
		public static string GenerateCondition(string conditionKey, string conditionContent, string contents) {
			string data = conditionKey + "(" + conditionContent + ") {";
			if(conditionKey.Equals("do")) {
				data = conditionKey + " {";
			}
			if(!string.IsNullOrEmpty(contents)) {
				data += ("\n" + contents).AddTabAfterNewLine(1) + "\n";
			}
			data += "}";
			if(conditionKey.Equals("do")) {
				data += " while(" + conditionContent + ");";
			}
			return data;
		}
		#endregion

		#region Arithmeti Code
		/// <summary>
		/// Generate arithmetic operation.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <param name="compareType"></param>
		/// <returns></returns>
		public static string GenerateArithmetiCode(string left, string right, ArithmeticType arithmeticType = ArithmeticType.Add) {
			if(left == null && right == null)
				return null;
			switch(arithmeticType) {
				case ArithmeticType.Add:
					return left + " + " + right;
				case ArithmeticType.Divide:
					return left + " / " + right;
				case ArithmeticType.Modulo:
					return left + " % " + right;
				case ArithmeticType.Multiply:
					return left + " * " + right;
				case ArithmeticType.Subtract:
					return left + " - " + right;
			}
			throw new System.InvalidCastException();
		}
		#endregion

		#region Operator Code
		/// <summary>
		/// Generate And (left && right) code.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <param name="compareType"></param>
		/// <returns></returns>
		public static string GenerateAndCode(string left, string right) {
			return left + " && " + right;
		}

		/// <summary>
		/// Generate Convert code.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public static string GenerateConvertCode(MemberData value, Type type) {
			if(type is RuntimeType && value.type.IsCastableTo(typeof(IRuntimeClass))) {
				RegisterUsingNamespace("MaxyGames");
				if(generatePureScript) {
					return ParseValue((object)value).Access(
							DoGenerateInvokeCode(
								nameof(Extensions.ToRuntimeInstance),
								new string[0],
								new Type[] { type }
							).RemoveSemicolon()
						);
				} else {
					return ParseValue(value).Access(
							DoGenerateInvokeCode(
								nameof(Extensions.ToRuntimeInstance),
								new string[] { type.Name.AddFirst(_runtimeInterfaceKey, type.IsInterface).ParseValue() }
							).RemoveSemicolon()
						);
				}
			}
			return GenerateConvertCode(ParseValue((object)value), ParseType(type));
		}

		/// <summary>
		/// Generate Convert code.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public static string GenerateConvertCode(string value, string type) {
			return "((" + type + ")" + value + ")";
		}

		/// <summary>
		/// Get Generated unique type name
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static string GetUniqueNameForComponent(RuntimeType type) {
			return type.Name.AddFirst(_runtimeInterfaceKey, type.IsInterface).ParseValue();
		}

		/// <summary>
		/// Generate As code.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public static string GenerateAsCode(MemberData value, Type type) {
			if(type is RuntimeType && value.type.IsCastableTo(typeof(IRuntimeClass))) {
				RegisterUsingNamespace("MaxyGames");
				if(generatePureScript) {
					return ParseValue((object)value).Access(
							DoGenerateInvokeCode(
								nameof(Extensions.ToRuntimeInstance),
								new string[0],
								new Type[] { type }
							).RemoveSemicolon()
						);
				} else {
					return ParseValue(value).Access(
							DoGenerateInvokeCode(
								nameof(Extensions.ToRuntimeInstance),
								new string[] { GetUniqueNameForComponent(type as RuntimeType) }
							).RemoveSemicolon()
						);
				}
			}
			if(type.IsValueType) {
				return GenerateConvertCode(ParseValue((object)value), ParseType(type));
			}
			return GenerateAsCode(ParseValue((object)value), ParseType(type));
		}

		/// <summary>
		/// Generate As code.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public static string GenerateAsCode(string value, string type) {
			return "(" + value + " as " + type + ")";
		}

		/// <summary>
		/// Generate Is code.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public static string GenerateIsCode(MemberData value, Type type) {
			if(type is RuntimeType && value.type.IsCastableTo(typeof(IRuntimeClass))) {
				RegisterUsingNamespace("MaxyGames");
				return ParseValue((object)value).Access(
						DoGenerateInvokeCode(
							nameof(Extensions.IsTypeOf),
							new string[0],
							new Type[] { type }
						).RemoveSemicolon()
					);
			}
			return GenerateIsCode(ParseValue((object)value), type);
		}

		/// <summary>
		/// Generate Is code.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public static string GenerateIsCode(string value, Type type) {
			return GenerateIsCode(value, ParseType(type));
		}

		/// <summary>
		/// Generate Is code.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public static string GenerateIsCode(string value, string type) {
			return "(" + value + " is " + type + ")";
		}

		/// <summary>
		/// Generate Or (left || right) code.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <param name="compareType"></param>
		/// <returns></returns>
		public static string GenerateOrCode(string left, string right) {
			return left + " || " + right;
		}

		public static string GetOperatorCode(string left, string right, BitwiseType operatorType) {
			switch(operatorType) {
				case BitwiseType.And:
					return left + " & " + right;
				case BitwiseType.Or:
					return left + " | " + right;
				case BitwiseType.ExclusiveOr:
					return left + " ^ " + right;
				default:
					throw new System.InvalidCastException();
			}
		}

		/// <summary>
		/// Function for get correctly operator code
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <param name="operatorType"></param>
		/// <returns></returns>
		public static string GetOperatorCode(string left, string right, ShiftType operatorType) {
			switch(operatorType) {
				case ShiftType.LeftShift:
					return left + " << " + right;
				case ShiftType.RightShift:
					return left + " >> " + right;
				default:
					throw new System.InvalidCastException();
			}
		}
		#endregion

		#region Compare Code
		/// <summary>
		/// Generate compare code.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <param name="compareType"></param>
		/// <returns></returns>
		public static string GenerateCompareCode(string left, string right, ComparisonType compareType = ComparisonType.Equal) {
			if(left == null && right == null)
				return null;
			switch(compareType) {
				case ComparisonType.Equal:
					return left + " == " + right;
				case ComparisonType.GreaterThan:
					return left + " > " + right;
				case ComparisonType.GreaterThanOrEqual:
					return left + " >= " + right;
				case ComparisonType.LessThan:
					return left + " < " + right;
				case ComparisonType.LessThanOrEqual:
					return left + " <= " + right;
				case ComparisonType.NotEqual:
					return left + " != " + right;
			}
			throw new InvalidCastException();
		}
		#endregion

		#region Return
		/// <summary>
		/// Generate return value code.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static string GenerateReturn(string value = null) {
			return "return " + value + ";";
		}
		#endregion

		#region Yield Return
		/// <summary>
		/// Generate yield return value code.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static string GenerateYieldReturn(string value) {
			if(value == null) {
				return "yield return null;";
			}
			return "yield return " + value + ";";
		}
		#endregion

		#region Commentaries
		/// <summary>
		/// Generate a single line comments
		/// </summary>
		/// <param name="contents"></param>
		/// <returns></returns>
		public static string GenerateComment(string contents) {
			return contents.AddFirst("/*").Add("*/");
		}
		#endregion

        #region Block
		/// <summary>
		/// Generate a new block of codes.
		/// </summary>
		/// <param name="contents"></param>
		/// <returns></returns>
		public static string GenerateBlock(string contents) {
			string data = "{";
			if(!string.IsNullOrEmpty(contents)) {
				data += ("\n" + contents).AddTabAfterNewLine(1) + "\n";
			}
			data += "}";
			return data;
		}
		#endregion
	}
}