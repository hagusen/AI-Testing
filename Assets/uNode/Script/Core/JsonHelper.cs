using UnityEngine;
using FullSerializer;
using System.Collections.Generic;

namespace MaxyGames {
	/// <summary>
	/// Helper class for Deserialize or Serialize to/from json.
	/// </summary>
	public static class JsonHelper {
		static readonly fsSerializer serializer;
		static readonly fsSerializer editorSerializer;

		static JsonHelper() {
			serializer = new fsSerializer();
			serializer.Config.EnablePropertySerialization = false;

			editorSerializer = new fsSerializer();
			editorSerializer.AddConverter(new UnityObjectConverter());
			editorSerializer.Config.SerializeEnumsAsInteger = true;
			editorSerializer.Config.EnablePropertySerialization = false;
		}

		/// <summary>
		/// Make a copy of object by serializing and deserialize.
		/// Note: this operation is slow.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="value"></param>
		/// <returns></returns>
		public static T Duplicate<T>(T value) {
			List<Object> references = new List<Object>();
			string json = Serialize(value, false, references);
			return Deserialize<T>(json, references);
		}

		/// <summary>
		/// Serialize object without Indented.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public static string Serialize(object obj) {
			return Serialize(obj, false);
		}

		/// <summary>
		/// Serialize object.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="Indented"></param>
		/// <returns></returns>
		public static string Serialize(object obj, bool Indented) {
			fsData data;
			serializer.TrySerialize(obj, out data);
			if(Indented) {
				return fsJsonPrinter.PrettyJson(data);
			}
			return fsJsonPrinter.CompressedJson(data);
		}

		/// <summary>
		/// Deserialize object.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="json"></param>
		/// <param name="instance"></param>
		public static void Deserialize<T>(string json, ref T instance) {
			fsData fsData;
			fsJsonParser.Parse(json, out fsData);
			if(fsData != null)
				serializer.TryDeserialize<T>(fsData, ref instance);
		}

		/// <summary>
		/// Deserialize object.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="json"></param>
		/// <returns></returns>
		public static T Deserialize<T>(string json) {
			T result = default(T);
			fsData data;
			fsJsonParser.Parse(json, out data);
			if(data != null)
				serializer.TryDeserialize<T>(data, ref result);
			return result;
		}

		/// <summary>
		/// Deserialize object with support for unity reference.
		/// Unity reference will be handle from references
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="json"></param>
		/// <param name="references"></param>
		public static T Deserialize<T>(string json, List<Object> references) {
			T result = default(T);
			try {
				fsData data;
				fsJsonParser.Parse(json, out data);
				if(data != null) {
					fsSerializer fsS;
					if(UnityObjectConverter.listObject != null) {
						fsS = new fsSerializer();
						fsS.AddConverter(new UnityObjectConverter() { reference = references });
					} else {
						UnityObjectConverter.listObject = references;
						fsS = editorSerializer;
					}
					fsS.TryDeserialize<T>(data, ref result);
					if(UnityObjectConverter.listObject != null && references != null) {
						UnityObjectConverter.listObject = null;
					}
				}
			}
			catch(System.Exception ex) {
				Debug.LogException(ex);
			}
			return result;
		}

		/// <summary>
		/// Deserialize object.
		/// </summary>
		/// <param name="json"></param>
		/// <param name="t"></param>
		/// <returns></returns>
		public static object Deserialize(string json, System.Type t) {
			object result = null;
			fsData data;
			fsJsonParser.Parse(json, out data);
			if(data != null)
				serializer.TryDeserialize(data, t, ref result);
			return result;
		}

		/// <summary>
		/// Serialize object with support for unity reference.
		/// Unity reference will saved to reference.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="Indented"></param>
		/// <param name="references"></param>
		/// <returns></returns>
		public static string Serialize(object obj, bool Indented, List<Object> references) {
			//Clear reference if its not null.
			if(references != null)
				references.Clear();
			fsData data;
			//Check if currently is serializing mostly this will false.
			//This only to handle unity reference serialization.
			if(UnityObjectConverter.listObject != null) {
				//Make new serializer.
				fsSerializer s = new fsSerializer();
				//Add new UnityObjectConverter so unity reference object will serialize and saved to list.
				s.AddConverter(new UnityObjectConverter() { reference = references });
				//Make sure Enum type serialze to int so any changed made to enum will not lose.
				s.Config.SerializeEnumsAsInteger = true;
				//Disable property serialization so this save more performance and memory.
				s.Config.EnablePropertySerialization = false;
				//Try serializing object
				s.TrySerialize(obj, out data);
			} else {
				if(references != null)
					UnityObjectConverter.listObject = references;
				editorSerializer.TrySerialize(obj, out data);
				if(references != null)
					UnityObjectConverter.listObject = null;
			}
			if(Indented) {
				return fsJsonPrinter.PrettyJson(data);
			}
			return fsJsonPrinter.CompressedJson(data);
		}

		/// <summary>
		/// Deserialize object with support for unity reference.
		/// Unity reference will be handle from references
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="json"></param>
		/// <returns></returns>
		public static void Deserialize<T>(string json, ref T instance, List<Object> references) {
			try {
				fsData data;
				fsJsonParser.Parse(json, out data);
				if(data != null) {
					fsSerializer fsS;
					if(UnityObjectConverter.listObject != null) {
						fsS = new fsSerializer();
						fsS.AddConverter(new UnityObjectConverter() { reference = references });
					} else {
						UnityObjectConverter.listObject = references;
						fsS = editorSerializer;
					}
					fsS.TryDeserialize<T>(data, ref instance);
					if(UnityObjectConverter.listObject != null && references != null) {
						UnityObjectConverter.listObject = null;
					}
				}
			}
			catch(System.Exception ex) {
				Debug.LogException(ex);
			}
		}

		/// <summary>
		/// Deserialize object with support for unity reference and type converter if default type is missing.
		/// Unity reference will be handle from references.
		/// </summary>
		/// <param name="json"></param>
		/// <param name="type"></param>
		/// <param name="references"></param>
		/// <returns></returns>
		public static object Deserialize(string json, System.Type type, List<Object> references) {
			object result = null;
			try {
				fsData data;
				fsJsonParser.Parse(json, out data);
				if(data != null) {
					fsSerializer fsS;
					if(UnityObjectConverter.listObject != null) {
						fsS = new fsSerializer();
						fsS.AddConverter(new UnityObjectConverter() { reference = references });
					} else {
						UnityObjectConverter.listObject = references;
						fsS = editorSerializer;
					}
					if(type != null) {
						if(data.IsDictionary) {
							var d = data.AsDictionary;
							if(d.ContainsKey("$type")) {
								fsData fsD = d["$type"];
								if(fsD.IsString) {
									System.Type t = TypeSerializer.Deserialize(fsD.AsString, false);
									if(t == null) {
										d["$type"] = new fsData(type.FullName);
									}
								}
							}
						}
						//fsS.TryDeserialize(data, type, ref result);
						fsS.TryDeserialize(data, ref result);
					} else {
						fsS.TryDeserialize(data, ref result);
					}
					if(UnityObjectConverter.listObject != null && references != null) {
						UnityObjectConverter.listObject = null;
					}
				}
			}
			catch(System.Exception ex) {
				Debug.LogException(ex);
			}
			return result;
		}

		/// <summary>
		/// Custom Converter to handle unity reference
		/// </summary>
		private class UnityObjectConverter : fsConverter {
			public static List<Object> listObject;
			public List<Object> reference;

			private List<Object> actualReferece {
				get {
					if(reference != null) {
						return reference;
					}
					return listObject;
				}
			}

			public override bool RequestCycleSupport(System.Type storageType) {
				return false;
			}

			public override bool RequestInheritanceSupport(System.Type storageType) {
				return false;
			}

			public override object CreateInstance(fsData data, System.Type storageType) {
				if(typeof(Object).IsAssignableFrom(storageType)) {
					return null;
				}
				return System.Activator.CreateInstance(storageType);
			}

			public override bool CanProcess(System.Type type) {
				if(typeof(Object).IsAssignableFrom(type)) {
					return true;
				}
				return false;
			}

			public override fsResult TrySerialize(object instance, out fsData serialized, System.Type storageType) {
				if(actualReferece != null) {
					actualReferece.Add(instance as Object);
					if(instance != null) {
						serialized = new fsData(actualReferece.Count - 1);
					} else {
						serialized = new fsData();
					}
				} else {
					serialized = new fsData();
				}
				return fsResult.Success;
			}

			public override fsResult TryDeserialize(fsData data, ref object instance, System.Type storageType) {
				if(actualReferece != null && data.IsInt64) {
					int index = (int)data.AsInt64;
					if(actualReferece.Count > index) {
						if(instance as Object == actualReferece[index]) {
							return fsResult.Fail("");
						}
						instance = actualReferece[index];
						return fsResult.Success;
					}
				}
				return fsResult.Fail("");
			}
		}
	}
}