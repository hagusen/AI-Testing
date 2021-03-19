using System;
using UnityEngine.UIElements;

namespace MaxyGames.uNode.Editors {
	[System.Serializable]
	public class PortData {
		/// <summary>
		/// The ID of the port, this is used to identify the port and connections are created by looking this ID.
		/// </summary>
		public string portID;
		/// <summary>
		/// The owner of the port.
		/// </summary>
		public UNodeView owner;
		/// <summary>
		/// The owner of port data.
		/// </summary>
		public PortView port;
		/// <summary>
		/// Optionally filter for value ports.
		/// </summary>
		public FilterAttribute filter;

		/// <summary>
		/// The port name
		/// </summary>
		/// <value></value>
		public Func<string> getPortName { private get; set; }
		public Func<string> getPortTooltip { private get; set; }
		/// <summary>
		/// The port type. Required for value port but not when filter is assigned.
		/// </summary>
		public Func<Type> getPortType { private get; set; }
		/// <summary>
		/// The port value. Required for input value and output flow ports.
		/// </summary>
		public Func<object> getPortValue { private get; set; }
		/// <summary>
		/// Required for output value and input flow ports.
		/// </summary>
		public Func<MemberData> getConnection { private get; set; }
		/// <summary>
		/// Required for input value and output flow ports.
		/// </summary>
		public Action<object> onValueChanged { private get; set; }

		public MemberControl InstantiateControl(bool autoLayout = false) {
			ControlConfig config = new ControlConfig() {
				owner = owner,
				value = GetPortValue(),
				type = GetPortType(),
				filter = GetFilter(),
				onValueChanged = onValueChanged,
			};
			return new MemberControl(config, autoLayout);
		}

		/// <summary>
		/// Get the filter of this port or create new if none.
		/// </summary>
		/// <returns></returns>
		public FilterAttribute GetFilter() {
			if(filter == null) {
				Type t = getPortType();
				if(t != null) {
					filter = new FilterAttribute(t);
				} else {
					filter = new FilterAttribute(typeof(object));
				}
			}
			return filter;
		}

		public void OnValueChanged(object value) {
			onValueChanged?.Invoke(value);
			owner?.OnValueChanged();
		}

		public string GetPortName() {
			return getPortName?.Invoke();
		}

		public string GetPortTooltip() {
			return getPortTooltip?.Invoke();
		}

		public Type GetPortType() {
			return getPortType?.Invoke() ?? GetFilter().GetActualType();
		}

		public object GetPortValue() {
			return getPortValue?.Invoke();
		}

		public MemberData GetConnection() {
			return getConnection?.Invoke();
		}
	}
}