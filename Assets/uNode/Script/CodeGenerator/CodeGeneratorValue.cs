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
		#region Generate Set Code
		/// <summary>
		/// Function for get code for set.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <param name="leftType"></param>
		/// <param name="rightType"></param>
		/// <returns></returns>
		public static string GenerateSetCode(object left, object right, Type leftType, Type rightType = null) {
			return GenerateSetCode(left, right, SetType.Change, leftType, rightType);
		}

		/// <summary>
		/// Function for generate code for set a value.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <param name="setType"></param>
		/// <param name="leftType"></param>
		/// <param name="rightType"></param>
		/// <returns></returns>
		public static string GenerateSetCode(object left, object right, SetType setType = SetType.Change, Type leftType = null, Type rightType = null) {
			if(left == null || right == null)
				return null;
			object firstVal = left;
			string set;
			if(right is string) {
				set = right as string;
			} else if(right is char[]) {
				set = new string(right as char[]);
			} else {
				set = ParseValue(right, autoConvert:true);
			}
			string result = null;
			if(left is MemberData) {
				MemberData source = left as MemberData;
				if(source.type != null && source.type.IsValueType) {
					MemberInfo[] memberInfo = source.GetMembers(false);
					// if(memberInfo[0] is RuntimeType && memberInfo[memberInfo.Length - 1] is IRuntimeMember){
					// 	throw null;
					// } else 
					if(memberInfo != null && memberInfo.Length > 1 && ReflectionUtils.GetMemberType(memberInfo[memberInfo.Length - 2]).IsValueType) {
						string varName = GenerateVariableName("tempVar");
						string data = ParseType(ReflectionUtils.GetMemberType(memberInfo[memberInfo.Length - 2])) + " " + varName + " = ";
						var pVal = ParseValue((object)source);
						var pVal2 = pVal.Remove(pVal.IndexOf(ParseStartValue(source)), ParseStartValue(source).Length + 1);
						if(pVal.LastIndexOf(".") >= 0) {
							pVal = pVal.Remove(pVal.LastIndexOf("."));
						}
						data += pVal + ";\n";
						switch(setType) {
							case SetType.Subtract:
								data = data + varName + "." + pVal2.SplitMember().Last() + " -= " + set + ";";
								break;
							case SetType.Divide:
								data = data + varName + "." + pVal2.SplitMember().Last() + " /= " + set + ";";
								break;
							case SetType.Add:
								data = data + varName + "." + pVal2.SplitMember().Last() + " += " + set + ";";
								break;
							case SetType.Multiply:
								data = data + varName + "." + pVal2.SplitMember().Last() + " *= " + set + ";";
								break;
							case SetType.Modulo:
								data = data + varName + "." + pVal2.SplitMember().Last() + " %= " + set + ";";
								break;
							default:
								data = data + varName + "." + pVal2.SplitMember().Last() + " = " + set + ";";
								break;
						}
						if(leftType != null && !leftType.IsCastableTo(typeof(Delegate)) && !(leftType is RuntimeType)) {
							if(rightType == null || !rightType.IsCastableTo(leftType) && !rightType.IsValueType && rightType != typeof(string)) {
								if(leftType.IsValueType) {
									varName = varName.Insert(0, "(" + ParseType(leftType) + ")");
								} else if(set != "null") {
									varName = varName + " as " + ParseType(leftType);
								}
							}
						}
						return data + "\n" + pVal + " = " + varName + ";";
					} else {
						result = ParseValue(left, setVariable:true);
					}
				} else {
					result = ParseValue(left, setVariable:true);
					if(source.type is RuntimeGraphType && right is MemberData) {
						MemberData mVal = right as MemberData;
						if(mVal.type != source.type) {
							set = set.Access(GenerateGetGeneratedComponent(null, source.type as RuntimeGraphType));
						}
					}
				}
			} else if(left is string) {
				result = left.ToString();
			}
			if(leftType != null && !leftType.IsCastableTo(typeof(Delegate)) && !(leftType is RuntimeType)) {
				if(rightType == null || !rightType.IsCastableTo(leftType) && !rightType.IsValueType && rightType != typeof(string)) {
					if(leftType.IsValueType) {
						set = set.Insert(0, "(" + ParseType(leftType) + ")");
					} else if(set != "null") {
						set = set + " as " + ParseType(leftType);
					}
				}
			}
			bool flag = !generatePureScript && result.EndsWith("\")");
			if(includeGraphInformation && firstVal is MemberData && !result.EndsWith("*/")) {
				var member = firstVal as MemberData;
				var node = member.GetTargetNode();
				if(member.targetType == MemberData.TargetType.ValueNode && node != null) {
					result = WrapWithInformation(result, node);
				}
			}
			if(flag) {
				var strs = result.SplitMember();
				var lastStr = strs[strs.Count - 1];
				string setCode = null;
				if(lastStr.StartsWith(nameof(RuntimeComponent.GetVariable) + "<")) {
					setCode = nameof(RuntimeComponent.SetVariable);
					if(set != "null" && leftType.IsCastableTo(typeof(Delegate))) {
						set = set.Wrap().Insert(0, "(" + ParseType(leftType) + ")");
					}
				} else if(lastStr.StartsWith(nameof(RuntimeComponent.GetProperty) + "<")) {
					setCode = nameof(RuntimeComponent.SetProperty);
					if(set != "null" && leftType.IsCastableTo(typeof(Delegate))) {
						set = set.Wrap().Insert(0, "(" + ParseType(leftType) + ")");
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
					{//for change the operator code
						int firstIndex = lastStr.IndexOf("\"");
						string vName = lastStr.Substring(firstIndex, lastStr.LastIndexOf("\"") - firstIndex + 1);
						if(code != null) {
							strs[strs.Count - 1] = DoGenerateInvokeCode(setCode, new string[] { vName, set, code.ParseValue() });
						} else {
							strs[strs.Count - 1] = DoGenerateInvokeCode(setCode, new string[] { vName, set });
						}
					}
					result = string.Join(".", strs);
					if(debugScript && setting.debugValueNode && firstVal is MemberData) {
						result += GenerateDebugCode(firstVal as MemberData, result, true).AddLineInFirst();
					}
					return result;
				}
			}
			switch(setType) {
				case SetType.Subtract:
					result = result + " -= " + set + ";";
					break;
				case SetType.Divide:
					result = result + " /= " + set + ";";
					break;
				case SetType.Add:
					result = result + " += " + set + ";";
					break;
				case SetType.Multiply:
					result = result + " *= " + set + ";";
					break;
				case SetType.Modulo:
					result = result + " %= " + set + ";";
					break;
				default:
					result = result + " = " + set + ";";
					break;
			}
			if(debugScript && setting.debugValueNode && firstVal is MemberData) {
				result += GenerateDebugCode(firstVal as MemberData, result.RemoveLast(), true).AddLineInFirst();
			}
			return result;
		}
		#endregion

        #region InvokeCode
        /// <summary>
		/// Get invoke code for target.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="functionName"></param>
		/// <param name="paramObject"></param>
		/// <returns></returns>
		public static string GetInvokeCode(object target, string functionName, params object[] paramObject) {
			if(object.ReferenceEquals(target, null)) {
				return GetInvokeCode(functionName, paramObject);
			}
			string data = ParseValue(target);
			if(string.IsNullOrEmpty(data))
				return null;
			data = data.Replace("\"", "");
			if(data.EndsWith("[]")) {
				return GetInvokeCode(data.Replace("[]", "") + "[" + functionName + "]", false, paramObject);
			}
			return GetInvokeCode(data + "." + functionName, false, paramObject);
		}

		/// <summary>
		/// Get invoke code from type.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="functionName"></param>
		/// <param name="paramObject"></param>
		/// <returns></returns>
		public static string GetInvokeCode(Type type, string functionName, params object[] paramObject) {
			string data = ParseType(type);
			if(string.IsNullOrEmpty(data))
				return null;
			if(data.EndsWith("[]")) {
				return GetInvokeCode(data.Replace("[]", "") + "[" + functionName + "]", false, paramObject);
			}
			return GetInvokeCode(data + "." + functionName, false, paramObject);
		}

        /// <summary>
		/// Get the local invoke code.
		/// </summary>
		/// <param name="functionName"></param>
		/// <param name="paramObject"></param>
		/// <param name="removeInvokeSymbolOnNoParameter"></param>
		/// <returns></returns>
		public static string GetInvokeCode(string functionName, object[] paramObject, bool removeInvokeSymbolOnNoParameter = false) {
			if(functionName.Contains(';')) {
				functionName = functionName.RemoveSemicolon();
			}
			string paramName = "";
			int index = 0;
			foreach(object o in paramObject) {
				if(index != 0) {
					paramName += ", ";
				}
				paramName += ParseValue(o);
				index++;
			}
			if(string.IsNullOrEmpty(paramName) && removeInvokeSymbolOnNoParameter) {
				return functionName + ";";
			}
			if(functionName.EndsWith("[]")) {
				return functionName.RemoveLast(2) + "[" + paramName + "];";
			} else if(functionName.EndsWith("]") && string.IsNullOrEmpty(paramName)) {
				return functionName + ";";
			}
			return functionName + "(" + paramName + ");";
		}

		/// <summary>
		/// Get the local invoke code.
		/// </summary>
		/// <param name="functionName"></param>
		/// <param name="removeInvokeSymbolOnNoParameter"></param>
		/// <param name="paramObject"></param>
		/// <returns></returns>
		public static string GetInvokeCode(string functionName, bool removeInvokeSymbolOnNoParameter, params object[] paramObject) {
			return GetInvokeCode(functionName, paramObject, removeInvokeSymbolOnNoParameter);
		}
        #endregion

        #region CompareCode
        /// <summary>
		/// Function for get code for comparing
		/// </summary>
		/// <param name="compareType"></param>
		/// <returns></returns>
		public static string GetCompareCode(ComparisonType compareType = ComparisonType.Equal) {
			switch(compareType) {
				case ComparisonType.Equal:
					return "==";
				case ComparisonType.GreaterThan:
					return ">";
				case ComparisonType.GreaterThanOrEqual:
					return ">=";
				case ComparisonType.LessThan:
					return "<";
				case ComparisonType.LessThanOrEqual:
					return "<=";
				case ComparisonType.NotEqual:
					return "!=";
			}
			return null;
		}

		/// <summary>
		/// Function for get code for compare 2 object
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <param name="compareType"></param>
		/// <returns></returns>
		public static string GetCompareCode(object left, object right, ComparisonType compareType = ComparisonType.Equal) {
			if(left == null && right == null)
				throw new System.Exception();
			if(left != null && left.GetType().IsValueType && right == null)
				return null;
			if(right != null && right.GetType().IsValueType && left == null)
				return null;
			if(left is MemberData) {
				left = ParseValue(left);
			}
			string data2 = ParseValue(right);
			if(right is string) {
				data2 = (string)right;
			}
			switch(compareType) {
				case ComparisonType.Equal:
					return left.ToString() + " == " + data2;
				case ComparisonType.GreaterThan:
					return left.ToString() + " > " + data2;
				case ComparisonType.GreaterThanOrEqual:
					return left.ToString() + " >= " + data2;
				case ComparisonType.LessThan:
					return left.ToString() + " < " + data2;
				case ComparisonType.LessThanOrEqual:
					return left.ToString() + " <= " + data2;
				case ComparisonType.NotEqual:
					return left.ToString() + " != " + data2;
			}
			throw new InvalidCastException();
		}
        #endregion

        #region Generate ConstuctorCode
		/// <summary>
		/// Function to generate code for creating new object 
		/// </summary>
		/// <param name="type"></param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		public static string GenerateConstuctorCode(Type type, params object[] parameters) {
			ConstructorInfo[] ctor = type.GetConstructors();
			if(ctor != null) {
				if(ctor.Length == 0) {
					throw new System.Exception("Not found supported constructor");
				} else {
					if(parameters == null) {
						parameters = new object[0];
					}
					for(int i = 0; i < ctor.Length; i++) {
						ParameterInfo[] pinfo = ctor[i].GetParameters();
						if(pinfo.Length == parameters.Length) {
							bool isValid = true;
							int index = 0;
							foreach(ParameterInfo info in pinfo) {
								object obj = parameters[index];
								if(info.ParameterType.IsValueType) {
									if(obj == null) {
										isValid = false;
										break;
									}
								}
								if(!(obj is MemberData)) {
									if(obj != null && !obj.GetType().IsCastableTo(type)) {
										isValid = false;
										break;
									}
								}
								index++;
							}
							if(!isValid)
								continue;
							string str = "new " + ParseType(type) + "(";
							index = 0;
							foreach(object obj in parameters) {
								if(index != 0) {
									str += ", ";
								}
								str += ParseValue(obj);
								index++;
							}
							return str + ")";
						}
					}
				}
			}
			return null;
		}
		#endregion
		
		#region Generate YieldReturn
		/// <summary>
		/// Get yield return value code.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static string GetYieldReturn(object value) {
			return "yield return " + ParseValue(value) + ";";
		}
		#endregion

		#region Generate Return
		/// <summary>
		/// Get return value code.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static string GetReturnValue(object value) {
			return "return " + ParseValue(value) + ";";
		}
		#endregion

		#region String Wrap
		/// <summary>
		/// Wrap a string value so the string will be generated without quotes.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static StringWrapper WrapString(string value) {
			return new StringWrapper(value);
		}

		/// <summary>
		/// Wrap a string value with brackets "( code )"
		/// </summary>
		/// <param name="code"></param>
		/// <param name="onlyOnContainSpace"></param>
		/// <returns></returns>
		public static string WrapCode(string code, bool onlyOnContainSpace = false) {
			if(string.IsNullOrEmpty(code)) {
				return code;
			}
			if(onlyOnContainSpace && !code.Contains(" ")) {
				return code;
			}
			return "(" + code + ")";
		}
		#endregion
    }
}
