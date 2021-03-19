namespace MaxyGames {
	/// <summary>
	/// Allow you to invoking method or get field, property or even create new object instance and much more.
	/// This class can invoke method, constructor or event with parameter.
	/// </summary>
	[System.Serializable]
	public class MultipurposeMember {
		public MemberData target = new MemberData();
		public MemberData[] parameters = new MemberData[0];
		public uNode.ValueData initializer;

		public object Get() {
			object obj = null;
			if(target.isAssigned) {
				if(parameters != null && parameters.Length > 0) {
					target.GetMembers();
					object[] paramsValue = null;
					if(!target.hasRefOrOut) {
						if(parameters.Length > 0) {
							paramsValue = new object[parameters.Length];
							for(int i = 0; i < paramsValue.Length; i++) {
								paramsValue[i] = parameters[i].Get();
							}
						}
						obj = target.Invoke(paramsValue);
					} else {
						object[] paramsValue2 = null;
						if(parameters.Length > 0) {
							paramsValue = new object[parameters.Length];
							paramsValue2 = new object[parameters.Length];
							for(int i = 0; i < paramsValue.Length; i++) {
								paramsValue[i] = parameters[i].Get();
								paramsValue2[i] = paramsValue[i];
							}
						}
						obj = target.Invoke(paramsValue);
						if(paramsValue != null) {
							for(int i = 0; i < paramsValue.Length; i++) {
								if(paramsValue2[i] != paramsValue[i] && parameters[i].CanSetValue()) {
									parameters[i].Set(paramsValue[i]);
								}
							}
						}
					}
				} else {
					obj = target.Get();
				}
				if(target.targetType == MemberData.TargetType.Values || target.targetType == MemberData.TargetType.Constructor) {
					if(initializer != null && initializer.value != null) {
						uNode.ConstructorValueData ctor = initializer.value as uNode.ConstructorValueData;
						if(ctor != null) {
							ctor.ApplyInitializer(ref obj);
						}
					}
				}
			}
			return obj;
		}

		public void Set(object value) {
			if(target.isAssigned) {
				if(parameters != null && parameters.Length > 0) {
					target.GetMembers();
					object[] paramsValue = null;
					if(!target.hasRefOrOut) {
						if(parameters.Length > 0) {
							paramsValue = new object[parameters.Length];
							for(int i = 0; i < paramsValue.Length; i++) {
								paramsValue[i] = parameters[i].Get();
							}
						}
						target.Set(value, paramsValue);
					} else {
						object[] paramsValue2 = null;
						if(parameters.Length > 0) {
							paramsValue = new object[parameters.Length];
							paramsValue2 = new object[parameters.Length];
							for(int i = 0; i < paramsValue.Length; i++) {
								paramsValue[i] = parameters[i].Get();
								paramsValue2[i] = paramsValue[i];
							}
						}
						target.Set(value, paramsValue);
						if(paramsValue != null) {
							for(int i = 0; i < paramsValue.Length; i++) {
								if(paramsValue2[i] != paramsValue[i]) {
									parameters[i].Set(paramsValue[i]);
								}
							}
						}
					}
				} else {
					target.Set(value);
				}
			}
		}

		public bool CanSetValue() {
			return target.CanSetValue();
		}

		public bool CanGetValue() {
			return target.CanGetValue();
		}

		#region Ctor
		public MultipurposeMember() {

		}

		public MultipurposeMember(MemberData target) {
			this.target = new MemberData(target);
			MemberDataUtility.UpdateMultipurposeMember(this);
		}

		public MultipurposeMember(MemberData target, MemberData[] parameters) {
			this.target = new MemberData(target);
			if(parameters != null) {
				this.parameters = new MemberData[parameters.Length];
				for (int i = 0; i < parameters.Length;i++) {
					this.parameters[i] = new MemberData(parameters[i]);
				}
			}
			MemberDataUtility.UpdateMultipurposeMember(this);
		}

		public MultipurposeMember(MultipurposeMember member) {
			if(member == null) return;
			target = new MemberData(member.target);
			parameters = new MemberData[member.parameters.Length];
			for (int i = 0; i < parameters.Length;i++) {
				parameters[i] = new MemberData(member.parameters[i]);
			}
			initializer = SerializerUtility.Duplicate(member.initializer);
		}
		#endregion
	}
}