using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;

namespace MaxyGames.uNode {
	public abstract class GenericRuntimeParameter<T> : RuntimeParameter {
		public readonly T target;

		public override Type ParameterType => typeof(T);

        public GenericRuntimeParameter(RuntimeMethod owner, T target) : base(owner) {
			this.target = target;
		}
	}

	public class RuntimeGraphParameter : GenericRuntimeParameter<ParameterData> {
		public RuntimeGraphParameter(RuntimeMethod owner, ParameterData target) : base(owner, target) { }

		public override string Name => target.name;

		public override Type ParameterType => target.Type;

		public override ParameterAttributes Attributes {
			get {
				if (target.refKind == ParameterData.RefKind.Out) {
					return ParameterAttributes.Out;
				} else if (target.refKind == ParameterData.RefKind.Ref) {
					return ParameterAttributes.Retval;
				}
				return ParameterAttributes.None;
			}
		}
	}

	public class RuntimeParameterInfo : RuntimeParameter {
		private readonly string name;
		private readonly Type type;

		public RuntimeParameterInfo(Type type, RuntimeMethod owner) : base(owner) {
			this.name = type.Name.ToLower();
			this.type = type;
		}

		public RuntimeParameterInfo(string name, Type type, RuntimeMethod owner) : base(owner) {
			this.name = name;
			this.type = type;
		}

		public override string Name => name;
		public override Type ParameterType => type;
	}

	public abstract class RuntimeParameter : ParameterInfo {
		public readonly RuntimeMethod owner;

		public RuntimeParameter(RuntimeMethod owner) {
			this.owner = owner;
		}

		public override ParameterAttributes Attributes => ParameterAttributes.None;

		public override object DefaultValue => null;

		public override bool HasDefaultValue => false;

		public override MemberInfo Member => base.Member;

		public override int MetadataToken => 0;

		public override IEnumerable<CustomAttributeData> CustomAttributes => base.CustomAttributes;

		public override int Position => base.Position;

		public override object RawDefaultValue => base.RawDefaultValue;

		public override object[] GetCustomAttributes(bool inherit) {
			return GetCustomAttributes(inherit);
		}

		public override object[] GetCustomAttributes(Type attributeType, bool inherit) {
			return base.GetCustomAttributes(attributeType, inherit);
		}

		public override IList<CustomAttributeData> GetCustomAttributesData() {
			return base.GetCustomAttributesData();
		}

		public override bool IsDefined(Type attributeType, bool inherit) {
			return base.IsDefined(attributeType, inherit);
		}

		public override string ToString() {
			return Name;
		}
	}
}