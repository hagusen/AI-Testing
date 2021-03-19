using UnityEngine;
using UnityEngine.UIElements;

namespace MaxyGames.uNode.Editors {
	public class AutoHideGraphElement : ImmediateModeElement {
		public readonly UGraphView graphView;

		public AutoHideGraphElement(UGraphView graphView) {
			this.graphView = graphView;
		}

		protected override void ImmediateRepaint() {
			if (Event.current.isMouse) {
				UpdateElements();
			}
		}

		private void UpdateElements() {
			var graphView = this.graphView;
			UnityEngine.Profiling.Profiler.BeginSample("Update Element Visibility");
			Rect contentRect = graphView.layout;
			//Auto hide nodes
			graphView.nodeViews.ForEach((node) => {
				Rect nodeRect = node.ChangeCoordinatesTo(graphView, node.GetRect());
				// if(!node.visible) {
				// 	nodeRect = graphView.contentViewContainer.ChangeCoordinatesTo(graphView, node.hidingRect);
				// }
				nodeRect.x -= 200;
				nodeRect.y -= 50;
				nodeRect.width += 250;
				nodeRect.height += 100;
				if(nodeRect.Overlaps(contentRect)) {
					if (node.resolvedStyle.opacity == 0) {
						node.SetOpacity(true);
						// node.visible = true;
						// node.SetDisplay(true);
					}
				} else {
					if(node.resolvedStyle.opacity != 0) {
						node.SetOpacity(false);
						// node.visible = false;
						// node.SetDisplay(false);
						// node.hidingRect = node.ChangeCoordinatesTo(graphView.contentViewContainer, node.GetRect());
					}
				}
			});
			//Auto hide edges
			graphView.edgeViews.ForEach(edge => {
				if(edge.isProxy) return;//skip if the edge is proxy because it is hide by default
				var edgeControl = edge.edgeControl;
				Rect edgeRect = edgeControl.ChangeCoordinatesTo(graphView, edgeControl.GetRect());
				if(edgeRect.Overlaps(contentRect)) {
					if (edgeControl.resolvedStyle.opacity == 0) {
						edgeControl.SetOpacity(true);
						//edgeControl.visible = true;
						edgeControl.SetDisplay(true);
					}
				} else {
					if(edgeControl.resolvedStyle.opacity != 0) {
						edgeControl.SetOpacity(false);
						//edgeControl.visible = false;
						edgeControl.SetDisplay(false);
					}
				}
			});
			UnityEngine.Profiling.Profiler.EndSample();
		}
	}
}