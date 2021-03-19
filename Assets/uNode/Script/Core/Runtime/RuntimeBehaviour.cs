using System;

namespace MaxyGames {
	public abstract class RuntimeBehaviour : RuntimeComponent {
		public virtual void OnAwake() { }

		public override void SetVariable(string Name, object value) {
			var field = this.GetType().GetField(Name, MemberData.flags);
			if (field == null) {
				throw new Exception($"Variable with name:{Name} not found from type {this.GetType().FullName}." + 
					"\nIt may because of outdated generated script, try to generate the script again.");
			}
			value = uNodeHelper.GetActualRuntimeValue(value);
			field.SetValueOptimized(this, value);
		}

		public override void SetVariable(string Name, object value, string @operator) {
			var field = this.GetType().GetField(Name, MemberData.flags);
			if (field == null) {
				throw new Exception($"Variable with name:{Name} not found from type {this.GetType().FullName}." + 
					"\nIt may because of outdated generated script, try to generate the script again.");
			}
			value = uNodeHelper.GetActualRuntimeValue(value);
			switch(@operator) {
				case "+":
				case "-":
				case "/":
				case "*":
				case "%":
					var val = field.GetValueOptimized(this);
					value = uNodeHelper.ArithmeticOperator(val, value, @operator, field.FieldType, value?.GetType());
					break;
			}
			field.SetValueOptimized(this, value);
		}

		public override object GetVariable(string Name) {
			var field = this.GetType().GetField(Name, MemberData.flags);
			if (field == null) {
				throw new Exception($"Variable with name:{Name} not found from type {this.GetType().FullName}." + 
					"\nIt may because of outdated generated script, try to generate the script again.");
			}
			return field.GetValueOptimized(this);
		}

		public override T GetVariable<T>(string Name) {
			var obj = GetVariable(Name);
			if (obj != null) {
				return (T)obj;
			}
			return default;
		}

		public override object GetProperty(string Name) {
			var property = this.GetType().GetProperty(Name, MemberData.flags);
			if (property == null) {
				throw new Exception($"Property with name:{Name} not found from type {this.GetType().FullName}." + 
					"\nIt may because of outdated generated script, try to generate the script again.");
			}
			return property.GetValueOptimized(this);
		}

		public override T GetProperty<T>(string Name) {
			var obj = GetProperty(Name);
			if (obj != null) {
				return (T)obj;
			}
			return default;
		}

		public override void SetProperty(string Name, object value) {
			var property = this.GetType().GetProperty(Name, MemberData.flags);
			if (property == null) {
				throw new Exception($"Property with name:{Name} not found from type {this.GetType().FullName}." + 
					"\nIt may because of outdated generated script, try to generate the script again.");
			}
			value = uNodeHelper.GetActualRuntimeValue(value);
			property.SetValueOptimized(this, value);
		}
		
		public override void SetProperty(string Name, object value, string @operator) {
			var property = this.GetType().GetProperty(Name, MemberData.flags);
			if (property == null) {
				throw new Exception($"Property with name:{Name} not found from type {this.GetType().FullName}." + 
					"\nIt may because of outdated generated script, try to generate the script again.");
			}
			switch(@operator) {
				case "+":
				case "-":
				case "/":
				case "*":
				case "%":
					var val = property.GetValueOptimized(this);
					value = uNodeHelper.ArithmeticOperator(val, value, @operator, property.PropertyType, value?.GetType());
					break;
			}
			value = uNodeHelper.GetActualRuntimeValue(value);
			property.SetValueOptimized(this, value);
		}

		/// <summary>
		/// Execute function without parameters and without throwing an exception
		/// </summary>
		/// <param name="Name"></param>
		public void ExecuteFunction(string Name) {
			var func = this.GetType().GetMethod(Name, Type.EmptyTypes);
			if(func != null) {
				func.InvokeOptimized(this, null);
			}
		}

		public override object InvokeFunction(string Name, object[] values) {
			Type[] types = new Type[values != null ? values.Length : 0];
			if (values != null) {
				for (int i = 0; i < types.Length; i++) {
					types[i] = values[i] != null ? values[i].GetType() : typeof(object);
				}
				for (int i = 0; i < values.Length;i++) {
					values[i] = uNodeHelper.GetActualRuntimeValue(values[i]);
				}
			}
			var func = this.GetType().GetMethod(Name, types);
			if (func == null) {
				throw new Exception($"Function with name:{Name} not found from type {this.GetType().FullName}." + 
					"\nIt may because of outdated generated script, try to generate the script again.");
			}
			return func.InvokeOptimized(this, values);
		}

		public override object InvokeFunction(string Name, Type[] parameters, object[] values) {
			var func = this.GetType().GetMethod(Name, parameters);
			if (func == null) {
				throw new Exception($"Function with name:{Name} not found from type {this.GetType().FullName}." + 
					"\nIt may because of outdated generated script, try to generate the script again.");
			}
			if(values != null) {
				for (int i = 0; i < values.Length;i++) {
					values[i] = uNodeHelper.GetActualRuntimeValue(values[i]);
				}
			}
			return func.InvokeOptimized(this, values);
		}
	}
}