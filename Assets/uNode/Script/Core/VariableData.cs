using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MaxyGames.uNode {
	public abstract class BaseVariable : IVariable {
		/// <summary>
		/// The name of this variable.
		/// </summary>
		public string Name;
		/// <summary>
		/// The type of this variable.
		/// </summary>
		public MemberData type = new MemberData(typeof(object), MemberData.TargetType.Type);
		/// <summary>
		/// The runtime type of this variable.
		/// </summary>
		public System.Type Type {
			get {
				return type.SafeGet<System.Type>();
			}
			set {
				if(value == null) return;
				type.CopyFrom(MemberData.CreateFromType(value));
			}
		}

		/// <summary>
		/// The real variable value.
		/// </summary>
		[System.NonSerialized]
		public object variable;
		
		/// <summary>
		/// Get/Set the variable value.
		/// </summary>
		public object value {
			get {
				return Get();
			}
			set {
				Set(value);
			}
		}

		/// <summary>
		/// Get variable value
		/// </summary>
		/// <returns></returns>
		public virtual object Get() {
			return variable;
		}

		/// <summary>
		/// Generic Get variable value
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public T Get<T>() {
			return (T)Get();
		}

		/// <summary>
		/// Set a variable value
		/// </summary>
		/// <param name="value"></param>
		public virtual void Set(object value) {
			if(Type != null) {
				if(Type.IsValueType) {
					if(value == null) {
						throw new ArgumentNullException("value", $"Cannot set variable:{Name} because the value is null which is not allowed for struct type");
					} else if(!value.GetType().IsCastableTo(Type)) {
						throw new Exception("Cannot convert type '" + value.GetType().PrettyName() + "' to '" + Type.PrettyName() + "'");
					}
				} else {
					if(value != null && !value.GetType().IsCastableTo(Type)) {
						throw new Exception("Cannot convert type '" + value.GetType().PrettyName() + "' to '" + Type.PrettyName() + "'");
					}
				}
			}
			variable = value;
		}
	}

	/// <summary>
	/// Class to save instanced value data used in runtime and script generation.
	/// </summary>
	[System.Serializable]
	public class VariableData : SerializedVariable, IVariableModifier, IAttributeSystem, ISummary {
		/// <summary>
		/// The summary of this Variable.
		/// </summary>
		[TextArea]
		public string summary;
		/// <summary>
		/// Reset the value on enter ( only local variable )
		/// </summary>
		public bool resetOnEnter = true;
		/// <summary>
		/// Are this variable only can be get.
		/// </summary>
		public bool onlyGet { get; set; }
		/// <summary>
		/// Are this variable has been intialized
		/// </summary>
		[NonSerialized]
		public bool isInitialize;

		#region Functions
		/// <summary>
		/// Used for intialization.
		/// </summary>
		public void Initialize() {
			if(!isInitialize) {
				if(object.ReferenceEquals(variable, null) && Type != null) {
					//If type is Value Type/Struct will auto create new instance.
					if(Type.IsValueType) {
						variable = ReflectionUtils.CreateInstance(Type);
					}
				}
				isInitialize = true;
			}
		}
		#endregion
		
		#region Editor
		/// <summary>
		/// The attribute data for script generation
		/// </summary>
		public AttributeData[] attributes = new AttributeData[0];

		/// <summary>
		/// The variable modifier for script generation
		/// </summary>
		//[FullSerializer.fsIgnore]
		public FieldModifier modifier = new FieldModifier();

		public bool showInInspector {
			get {
				if(modifier != null && modifier.Public) {
					return true;
				}
				return false;
			}
		}

		public IList<AttributeData> Attributes { get => attributes; set => attributes = uNodeUtility.CreateArrayFrom(value); }

		public FieldModifier GetModifier() {
			return modifier;
		}

		public string GetSummary() {
			return summary;
		}
		#endregion

		#region Constructors
		/// <summary>
		/// Create new Variable
		/// </summary>
		public VariableData() {

		}

		/// <summary>
		/// Create new Variable
		/// </summary>
		/// <param name="name">The variable name</param>
		public VariableData(string name) {
			Name = name;
			type = null;
		}

		/// <summary>
		/// Create new Variable
		/// </summary>
		/// <param name="name">The variable name</param>
		/// <param name="type">The variable type</param>
		public VariableData(string name, MemberData type) {
			Name = name;
			this.type = type;
		}

		/// <summary>
		/// Create new Variable
		/// </summary>
		/// <param name="name">The variable name</param>
		/// <param name="type">The variable type</param>
		public VariableData(string name, System.Type type) {
			Name = name;
			this.Type = type;
		}

		/// <summary>
		/// Create new Variable and mark if this variable has been intialized.
		/// </summary>
		/// <param name="name">The variable name</param>
		/// <param name="type">The variable type</param>
		/// <param name="variableValue">The variable value</param>
		public VariableData(string name, System.Type type, object variableValue) {
			Name = name;
			this.Type = type;
			variable = variableValue;
			isInitialize = true;
		}

		/// <summary>
		/// Copy Variable and make new variable
		/// </summary>
		/// <param name="variable">The variable to copy</param>
		public VariableData(VariableData variable) {
			this.CopyFrom(variable);
		}

		/// <summary>
		/// Copy variable from variable.
		/// </summary>
		/// <param name="variable">The variable to copy</param>
		public void CopyFrom(VariableData variable) {
			this.Name = variable.Name;
			this.type = new MemberData(variable.type);

			this.isInitialize = false;
			this.variable = variable.variable;
			this.attributes = uNodeUtility.CloneObject(variable.attributes);
			this.modifier = uNodeUtility.CloneObject(variable.modifier);
			this.Serialize();
			this.Deserialize();
		}
		#endregion

	}

	/// <summary>
	/// The variable data that saves the value in the editor.
	/// </summary>
	public abstract class SerializedVariable : BaseVariable, ISerializationCallbackReceiver {
		[SerializeField]
		protected OdinSerializedData odinSerializedData;
		[SerializeField]
		protected List<UnityEngine.Object> references;
		[SerializeField]
		protected string serializedData;

		/// <summary>
		/// Serialize and save the current value for persistence
		/// </summary>
		public void Serialize() {
			odinSerializedData = SerializerUtility.SerializeValue(variable);
			serializedData = string.Empty;
			references = null;
		}

		/// <summary>
		/// Load the variable values
		/// </summary>
		public void Deserialize() {
			variable = SerializerUtility.DeserializeMigration(ref odinSerializedData, ref serializedData, ref references);
		}
		
		void ISerializationCallbackReceiver.OnAfterDeserialize() {
			Deserialize();
		}

		void ISerializationCallbackReceiver.OnBeforeSerialize() {
			// #if UNITY_EDITOR
			// Event e = Event.current;
			// if(e != null && e.type != UnityEngine.EventType.Used &&
			// 	(e.type == UnityEngine.EventType.Repaint ||
			// 	e.type == UnityEngine.EventType.MouseDrag ||
			// 	e.type == UnityEngine.EventType.Layout ||
			// 	e.type == UnityEngine.EventType.ScrollWheel)) {
			// 	return;
			// }
			// #endif
			Serialize();
		}
	}

	/// <summary>
	/// Bind variable to c# field (only for runtime).
	/// </summary>
	public class BindVariable : BaseVariable {
		private string fieldName;

		private System.Reflection.FieldInfo field;

		public override object Get() {
			if(variable == null) {
				throw new System.Exception("The owner of binder variable is null.");
			}
			if(field == null) {
				field = variable.GetType().GetField(fieldName);
			}
			return field.GetValueOptimized(variable);
		}

		public override void Set(object value) {
			if(variable == null) {
				throw new System.Exception("The owner of binder variable is null.");
			}
			if(field == null) {
				field = variable.GetType().GetField(fieldName);
			}
			field.SetValueOptimized(variable, value);
		}

		public BindVariable(string fieldName, object owner) {
			this.fieldName = fieldName;
			variable = owner;
			if(owner != null)
				field = owner.GetType().GetField(fieldName);
		}
	}
}