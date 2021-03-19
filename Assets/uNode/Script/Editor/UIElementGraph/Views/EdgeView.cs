using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;

namespace MaxyGames.uNode.Editors {
	public class EdgeView : Edge {
		public bool isProxy;
		private IMGUIContainer iMGUIContainer;

		//private UGraphView graphView;

		public PortView Output => output as PortView;
		public PortView Input => input as PortView;

		public EdgeView() {
			ReloadView();
		}

		public EdgeView(PortView input, PortView output) {
			this.input = input;
			this.output = output;
			var port = input;
			if(port == null) {
				port = output;
			}
			if(port != null) {
				if(port.orientation == Orientation.Vertical) {
					AddToClassList("flow");
				} else {
					AddToClassList("value");
				}
			}
			//if(input != null) {
			//	graphView = input.owner?.owner;
			//} else if(output != null) {
			//	graphView = output.owner?.owner;
			//}
			ReloadView();
		}

		public void ReloadView() {
			if(input == null || output == null) return;
			if(input.orientation == Orientation.Horizontal && input.direction == Direction.Input) {
				PortView port = input as PortView;
				MemberData member = port.portData.GetPortValue() as MemberData;
				isProxy = member != null && member.IsProxy();
			} else if(output.orientation == Orientation.Vertical && output.direction == Direction.Output) {
				PortView port = output as PortView;
				MemberData member = port.portData.GetPortValue() as MemberData;
				isProxy = member != null && member.IsProxy();
			}
			if(isProxy) {
				edgeControl.visible = false;
				edgeControl.SetEnabled(false);
			}
			#region Debug
			if(Application.isPlaying && uNodeUtility.useDebug) {
				//if(graphView != null) {
				//	graphView.RegisterIMGUI(this, DebugGUI);
				//}
				//iMGUIContainer = graphView.IMGUIContainer;
				if(iMGUIContainer == null) {
					iMGUIContainer = new IMGUIContainer(DebugGUI);
					iMGUIContainer.style.flexGrow = 1;
					iMGUIContainer.style.flexShrink = 0;
					iMGUIContainer.pickingMode = PickingMode.Ignore;
					edgeControl.Add(iMGUIContainer);
				}
			} else if(iMGUIContainer != null) {
				iMGUIContainer.RemoveFromHierarchy();
				iMGUIContainer = null;
			}
			#endregion
		}

		void DebugGUI() {
			if(isProxy && !visible)
				return;
			if(Application.isPlaying && uNodeUtility.useDebug) {
				PortView port = input as PortView ?? output as PortView;
				if(port != null && edgeControl.controlPoints != null && edgeControl.controlPoints.Length == 4) {
					uNodeUtility.DebugData debugData = port.owner.owner.graph.GetDebugInfo();
					if(debugData != null) {
						if(port.orientation == Orientation.Vertical) {
							PortView portView = output as PortView;
							if(portView.GetPortID() == UGraphView.SelfPortID) {

							} else {
								MemberData member = portView.portData.GetPortValue() as MemberData;
								if(member != null) {
									float times = -1;
									if(member.targetType == MemberData.TargetType.FlowInput) {
										int ID = uNodeUtility.GetObjectID(member.startTarget as MonoBehaviour);
										if(debugData != null && debugData.flowInputDebug.ContainsKey(ID)) {
											if(debugData.flowInputDebug[ID].ContainsKey(member.startName)) {
												times = Time.unscaledTime - debugData.flowInputDebug[ID][member.startName];
												times = times / 2;
											}
										}
									} else {
										int ID = uNodeUtility.GetObjectID(member.startTarget as MonoBehaviour);
										if(debugData != null && debugData.flowTransitionDebug.ContainsKey(ID)) {
											if(debugData.flowTransitionDebug[ID].ContainsKey(int.Parse(member.startName))) {
												times = Time.unscaledTime - debugData.flowTransitionDebug[ID][int.Parse(member.startName)];
												times = times / 2;
											}
										}
									}
									if(times >= 0) {
										if(Mathf.Abs(edgeControl.controlPoints[0].x - edgeControl.controlPoints[3].x) <= 4) {
											Vector2 v1 = this.ChangeCoordinatesTo(iMGUIContainer, edgeControl.controlPoints[0]);
											Vector2 v4 = this.ChangeCoordinatesTo(iMGUIContainer, edgeControl.controlPoints[3]);
											DrawDebug(v1, v4, edgeControl.inputColor, edgeControl.outputColor, times, true);
										} else {
											Vector2 v1 = this.ChangeCoordinatesTo(iMGUIContainer, edgeControl.controlPoints[0]);
											Vector2 v2 = this.ChangeCoordinatesTo(iMGUIContainer, edgeControl.controlPoints[1]);
											Vector2 v3 = this.ChangeCoordinatesTo(iMGUIContainer, edgeControl.controlPoints[2]);
											Vector2 v4 = this.ChangeCoordinatesTo(iMGUIContainer, edgeControl.controlPoints[3]);
											DrawDebug(new Vector2[] { v1, v2, v3, v4 }, edgeControl.inputColor, edgeControl.outputColor, times, true);
										}
									}
								}
							}
						} else {
							PortView portView = input as PortView;
							MemberData member = portView.portData.GetPortValue() as MemberData;
							if(member != null) {
								bool isSet = false;
								float times = 0;
								GUIContent debugContent = null;
								switch(member.targetType) {
									case MemberData.TargetType.ValueNode: {
										int ID = uNodeUtility.GetObjectID(member.startTarget as MonoBehaviour);
										if(debugData != null && debugData.valueTransitionDebug.ContainsKey(ID)) {
											if(debugData.valueTransitionDebug[ID].ContainsKey(int.Parse(member.startName))) {
												var vData = debugData.valueTransitionDebug[ID][int.Parse(member.startName)];
												isSet = vData.isSetValue;
												times = (Time.unscaledTime - vData.calledTime) / 2;
												if(vData.value != null) {
													debugContent = new GUIContent
														(uNodeUtility.GetDebugName(vData.value),
														uNodeEditorUtility.GetTypeIcon(vData.value.GetType()));
												} else {
													debugContent = new GUIContent("null");
												}
											}
										}
										break;
									}
									case MemberData.TargetType.NodeField: {

										break;
									}
									case MemberData.TargetType.NodeFieldElement: {

										break;
									}
								}
								if(debugContent != null) {
									if(Mathf.Abs(edgeControl.controlPoints[0].y - edgeControl.controlPoints[3].y) <= 4) {
										Vector2 v1 = this.ChangeCoordinatesTo(iMGUIContainer, edgeControl.controlPoints[0]);
										Vector2 v4 = this.ChangeCoordinatesTo(iMGUIContainer, edgeControl.controlPoints[3]);
										if(isSet) {
											DrawDebug(v4, v1, edgeControl.outputColor, edgeControl.inputColor, times, true);
										} else {
											DrawDebug(v1, v4, edgeControl.inputColor, edgeControl.outputColor, times, true);
										}
										{//Debug label
											Vector2 vec = (v1 + v4) / 2;
											Vector2 size = EditorStyles.helpBox.CalcSize(new GUIContent(debugContent.text));
											size.x += 25;
											GUI.Box(
												new Rect(vec.x - (size.x / 2), vec.y - 10, size.x - 10, 20),
												debugContent,
												EditorStyles.helpBox);
										}
									} else {
										Vector2 v1 = this.ChangeCoordinatesTo(iMGUIContainer, edgeControl.controlPoints[0]);
										Vector2 v2 = this.ChangeCoordinatesTo(iMGUIContainer, edgeControl.controlPoints[1]);
										Vector2 v3 = this.ChangeCoordinatesTo(iMGUIContainer, edgeControl.controlPoints[2]);
										Vector2 v4 = this.ChangeCoordinatesTo(iMGUIContainer, edgeControl.controlPoints[3]);
										if(isSet) {
											DrawDebug(new Vector2[] { v4, v3, v2, v1 }, edgeControl.outputColor, edgeControl.inputColor, times, true);
										} else {
											DrawDebug(new Vector2[] { v1, v2, v3, v4 }, edgeControl.inputColor, edgeControl.outputColor, times, true);
										}
										{//Debug label
											Vector2 vec = (v2 + v3) / 2;
											Vector2 size = EditorStyles.helpBox.CalcSize(new GUIContent(debugContent.text));
											size.x += 25;
											GUI.Box(
												new Rect(vec.x - (size.x / 2), vec.y - 10, size.x - 10, 20),
												debugContent,
												EditorStyles.helpBox);
										}
									}
								}
							}
						}
					}
				}
			}
		}

		private static void DrawDebug(Vector2[] vectors, Color inColor, Color outColor, float time, bool isFlow) {
			float timer = Mathf.Lerp(1, 0, time * 2f);//The debug timer speed.
			float distance = 0;
			for(int i = 0; i + 1 < vectors.Length; i++) {
				distance += Vector2.Distance(vectors[i], vectors[i + 1]);
			}
			float size = 15 * timer;
			float pointDist = 0;
			int currentSegment = 0;
			if(isFlow) {
				for(float i = -1; i < 1; i += 50f / distance) {
					float t = i + uNodeUtility.debugLinesTimer * (50f / distance);
					if(!(t < 0f || t > 1)) {
						if(currentSegment + 1 >= vectors.Length) break;
						float seqmentDistance = Vector2.Distance(vectors[currentSegment], vectors[currentSegment + 1]);
						while(Mathf.Lerp(0, distance, t) > pointDist + seqmentDistance && currentSegment + 2 < vectors.Length) {
							pointDist += seqmentDistance;
							currentSegment++;
							seqmentDistance = Vector2.Distance(vectors[currentSegment], vectors[currentSegment + 1]);
						}
						var vec = Vector2.Lerp(
							vectors[currentSegment],
							vectors[currentSegment + 1],
							(Mathf.Lerp(0, distance, t) - pointDist) / seqmentDistance);
						GUI.color = new Color(
							Mathf.Lerp(outColor.r, inColor.r, t),
							Mathf.Lerp(outColor.g, inColor.g, t),
							Mathf.Lerp(outColor.b, inColor.b, t), 1);
						GUI.DrawTexture(new Rect(vec.x - size / 2, vec.y - size / 2, size, size), uNodeUtility.DebugPoint);
					}
				}
			} else {
				for(float i = -1; i < 1; i += 50f / distance) {
					float t = i + uNodeUtility.debugLinesTimer * (50f / distance);
					if(!(t < 0f || t > 1)) {
						if(currentSegment + 1 >= vectors.Length) break;
						float seqmentDistance = Vector2.Distance(vectors[currentSegment], vectors[currentSegment + 1]);
						while(Mathf.Lerp(0, distance, t) > pointDist + seqmentDistance && currentSegment + 2 < vectors.Length) {
							pointDist += seqmentDistance;
							currentSegment++;
							seqmentDistance = Vector2.Distance(vectors[currentSegment], vectors[currentSegment + 1]);
						}
						var vec = Vector2.Lerp(
							vectors[currentSegment + 1],
							vectors[currentSegment],
							(Mathf.Lerp(0, distance, t) - pointDist) / seqmentDistance);
						GUI.color = new Color(
							Mathf.Lerp(inColor.r, outColor.r, t),
							Mathf.Lerp(inColor.g, outColor.g, t),
							Mathf.Lerp(inColor.b, outColor.b, t), 1);
						GUI.DrawTexture(new Rect(vec.x - size / 2, vec.y - size / 2, size, size), uNodeUtility.DebugPoint);
					}
				}
			}
			GUI.color = Color.white;
		}

		private static void DrawDebug(Vector2 start, Vector2 end, Color inColor, Color outColor, float time, bool isFlow) {
			float timer = Mathf.Lerp(1, 0, time * 2f);//The debug timer speed.
			float dist = Vector2.Distance(start, end);
			float size = 15 * timer;
			if(isFlow) {
				for(float i = -1; i < 1; i += 50f / dist) {
					float t = i + uNodeUtility.debugLinesTimer * (50f / dist);
					if(!(t < 0f || t > 1)) {
						GUI.color = new Color(
							Mathf.Lerp(outColor.r, inColor.r, t),
							Mathf.Lerp(outColor.g, inColor.g, t),
							Mathf.Lerp(outColor.b, inColor.b, t), 1);
						Vector2 vec = Vector2.Lerp(start, end, t);
						GUI.DrawTexture(new Rect(vec.x - size / 2, vec.y - size / 2, size, size), uNodeUtility.DebugPoint);
					}
				}
			} else {
				for(float i = -1; i < 1; i += 50f / dist) {
					float t = i + uNodeUtility.debugLinesTimer * (50f / dist);
					if(!(t < 0f || t > 1)) {
						GUI.color = new Color(
							Mathf.Lerp(inColor.r, outColor.r, t),
							Mathf.Lerp(inColor.g, outColor.g, t),
							Mathf.Lerp(inColor.b, outColor.b, t), 1);
						Vector2 vec = Vector2.Lerp(end, start, t);
						GUI.DrawTexture(new Rect(vec.x - size / 2, vec.y - size / 2, size, size), uNodeUtility.DebugPoint);
					}
				}
			}
			GUI.color = Color.white;
		}

		public PortView GetSenderPort() {
			if(input.orientation == Orientation.Horizontal) {
				if(input.direction == Direction.Input) {
					return input as PortView;
				} else {
					return output as PortView;
				}
			} else {
				if(input.direction == Direction.Input) {
					return output as PortView;
				} else {
					return input as PortView;
				}
			}
		}

		public PortView GetReceiverPort() {
			if(input.orientation == Orientation.Horizontal) {
				if(input.direction == Direction.Input) {
					return output as PortView;
				} else {
					return input as PortView;
				}
			} else {
				if(input.direction == Direction.Input) {
					return input as PortView;
				} else {
					return output as PortView;
				}
			}
		}

		public bool isValid => parent != null && !isGhostEdge && this.IsVisible();
	
		#region Overrides
		public override bool Overlaps(Rect rectangle)
        {
            if (isProxy) return false;
			return base.Overlaps(rectangle);
        }

		public override bool ContainsPoint(Vector2 localPoint) {
			if(isProxy) return false;
			return base.ContainsPoint(localPoint);
		}
		#endregion
	}
}