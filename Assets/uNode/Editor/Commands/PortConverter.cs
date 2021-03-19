using System;
using UnityEngine;
using System.Reflection;
using MaxyGames.uNode.Nodes;

namespace MaxyGames.uNode.Editors.PortConverter {
	class CastConverter : AutoConvertPort {
		public override Node CreateNode() {
			Node node = leftNode;
			NodeEditorUtility.AddNewNode<ASNode>(
				NodeGraph.openedGraph.editorData,
					new Vector2(rightNode.editorRect.x - 250, rightNode.editorRect.y),
					(nod) => {
						nod.compactDisplay = true;
						nod.type = new MemberData(rightType);
						nod.target = new MemberData(node, MemberData.TargetType.ValueNode);
						node = nod;
					});
			return node;
		}

		public override bool IsValid() {
			return !(rightType is RuntimeType) /*&& (rightType.IsCastableTo(leftType) || rightType == typeof(string))*/;
		}

		public override int order {
			get {
				return int.MaxValue;
			}
		}
	}

	class ElementToArray : AutoConvertPort {
		public override Node CreateNode() {
			Node node = leftNode;
			NodeEditorUtility.AddNewNode<MakeArrayNode>(
				NodeGraph.openedGraph.editorData,
					new Vector2(rightNode.editorRect.x - 250, rightNode.editorRect.y),
					(nod) => {
						nod.elementType = new MemberData(rightType.GetElementType());
						nod.values[0] = new MemberData(node, MemberData.TargetType.ValueNode);
						node = nod;
					});
			return node;
		}

		public override bool IsValid() {
			return rightType.IsArray && leftType.IsCastableTo(rightType.GetElementType());
		}

		public override int order {
			get {
				return -1;
			}
		}
	}

	// class StringConverter : AutoConvertPort {
	// 	public override Node CreateNode() {
	// 		Node node = leftNode;
	// 		NodeEditorUtility.AddNewNode<MultipurposeNode>(
	// 			NodeGraph.openedGraph.editorData,
	// 				new Vector2(rightNode.editorRect.x - 250, rightNode.editorRect.y),
	// 				(nod) => {
	// 					nod.target.target = new MemberData(typeof(object).GetMethod("ToString", Type.EmptyTypes));
	// 					nod.target.target.instance = new MemberData(node, MemberData.TargetType.ValueNode);
	// 					node = nod;
	// 				});
	// 		return node;
	// 	}

	// 	public override bool IsValid() {
	// 		return rightType == typeof(string);
	// 	}
	// }

	// class GameObjectConverter : AutoConvertPort {
	// 	public override Node CreateNode() {
	// 		Node node = leftNode;
	// 		if(rightType is RuntimeType) {
	// 			return node;
	// 		}
	// 		if(rightType.IsCastableTo(typeof(Component))) {
	// 			NodeEditorUtility.AddNewNode<MultipurposeNode>(
	// 				NodeGraph.openedGraph.editorData,
	// 				new Vector2(rightNode.editorRect.x - 250, rightNode.editorRect.y),
	// 				(nod) => {
	// 					nod.target.target = new MemberData(
	// 						typeof(GameObject).GetMethod("GetComponent", Type.EmptyTypes).MakeGenericMethod(rightType)
	// 					);
	// 					nod.target.target.instance = new MemberData(node, MemberData.TargetType.ValueNode);
	// 					node = nod;
	// 				});
	// 		}
	// 		return node;
	// 	}

	// 	public override bool IsValid() {
	// 		return leftType == typeof(GameObject);
	// 	}
	// }

	class ComponentConverter : AutoConvertPort {
		public override Node CreateNode() {
			Node node = leftNode;
			if(leftType == typeof(Transform)) {
				if(rightType == typeof(Vector3)) {
					NodeEditorUtility.AddNewNode<MultipurposeNode>(
						NodeGraph.openedGraph.editorData,
						new Vector2(rightNode.editorRect.x - 250, rightNode.editorRect.y),
						(nod) => {
							nod.target.target = new MemberData(typeof(Transform).GetProperty("position"));
							nod.target.target.instance = new MemberData(node, MemberData.TargetType.ValueNode);
							node = nod;
						});
					return node;
				} else if(rightType == typeof(Quaternion)) {
					NodeEditorUtility.AddNewNode<MultipurposeNode>(
						NodeGraph.openedGraph.editorData,
						new Vector2(rightNode.editorRect.x - 250, rightNode.editorRect.y),
						(nod) => {
							nod.target.target = new MemberData(typeof(Transform).GetProperty("rotation"));
							nod.target.target.instance = new MemberData(node, MemberData.TargetType.ValueNode);
							node = nod;
						});
					return node;
				}
			}
			if(rightType == typeof(GameObject)) {
				NodeEditorUtility.AddNewNode<MultipurposeNode>(
					NodeGraph.openedGraph.editorData,
					new Vector2(rightNode.editorRect.x - 250, rightNode.editorRect.y),
					(nod) => {
						nod.target.target = new MemberData(typeof(Component).GetProperty("gameObject"));
						nod.target.target.instance = new MemberData(node, MemberData.TargetType.ValueNode);
						node = nod;
					});
			} else if(rightType == typeof(Transform)) {
				NodeEditorUtility.AddNewNode<MultipurposeNode>(
						NodeGraph.openedGraph.editorData,
						new Vector2(rightNode.editorRect.x - 250, rightNode.editorRect.y),
						(nod) => {
							nod.target.target = new MemberData(typeof(Component).GetProperty("transform"));
							nod.target.target.instance = new MemberData(node, MemberData.TargetType.ValueNode);
							node = nod;
						});
			} else if(rightType.IsCastableTo(typeof(Component))) {
				NodeEditorUtility.AddNewNode<MultipurposeNode>(
					NodeGraph.openedGraph.editorData,
					new Vector2(rightNode.editorRect.x - 250, rightNode.editorRect.y),
					(nod) => {
						nod.target.target = new MemberData(
							typeof(Component).GetMethod("GetComponent", Type.EmptyTypes).MakeGenericMethod(rightType)
						);
						nod.target.target.instance = new MemberData(node, MemberData.TargetType.ValueNode);
						node = nod;
					});
			}
			return node;
		}

		public override bool IsValid() {
			if(leftType == typeof(Transform)) {
				if(rightType == typeof(Vector3)) {
					return true;
				} else if(rightType == typeof(Quaternion)) {
					return true;
				}
			}
			return false;
			// return leftType.IsCastableTo(typeof(Component));
		}
	}

	class QuaternionConverter : AutoConvertPort {
		public override Node CreateNode() {
			Node node = leftNode;
			if(rightType.IsCastableTo(typeof(Vector3))) {
				NodeEditorUtility.AddNewNode<MultipurposeNode>(
					NodeGraph.openedGraph.editorData,
					new Vector2(rightNode.editorRect.x - 250, rightNode.editorRect.y),
					(nod) => {
						nod.target.target = new MemberData(typeof(Quaternion).GetProperty("eulerAngles"));
						nod.target.target.instance = new MemberData(node, MemberData.TargetType.ValueNode);
						node = nod;
					});
			}
			return node;
		}

		public override bool IsValid() {
			return leftType == typeof(Quaternion);
		}
	}

	class Vector3Converter : AutoConvertPort {
		public override Node CreateNode() {
			Node node = leftNode;
			if(rightType == typeof(Quaternion)) {
				NodeEditorUtility.AddNewNode<MultipurposeNode>(
					NodeGraph.openedGraph.editorData,
					new Vector2(rightNode.editorRect.x - 250, rightNode.editorRect.y),
					(nod) => {
						nod.target.target = new MemberData(
							typeof(Quaternion).GetMethod("Euler", new Type[] { typeof(Vector3) })
						);
						nod.target.parameters = new MemberData[] {
							new MemberData(node, MemberData.TargetType.ValueNode)
						};
						node = nod;
					});
			}
			return node;
		}

		public override bool IsValid() {
			return leftType == typeof(Vector3) && rightType == typeof(Quaternion);
		}
	}

	class RaycastHitConverter : AutoConvertPort {
		public override Node CreateNode() {
			Node node = leftNode;
			#region RaycastHit
			if(rightType == typeof(Collider)) {
				NodeEditorUtility.AddNewNode<MultipurposeNode>(
					NodeGraph.openedGraph.editorData,
					new Vector2(rightNode.editorRect.x - 250, rightNode.editorRect.y),
					(nod) => {
						nod.target.target = new MemberData(
							typeof(RaycastHit).GetProperty("collider")
						);
						nod.target.target.instance = new MemberData(node, MemberData.TargetType.ValueNode);
						node = nod;
					});
			} else if(rightType == typeof(Transform)) {
				NodeEditorUtility.AddNewNode<MultipurposeNode>(
					NodeGraph.openedGraph.editorData,
					new Vector2(rightNode.editorRect.x - 250, rightNode.editorRect.y),
					(nod) => {
						nod.target.target = new MemberData(
							typeof(RaycastHit).GetProperty("transform")
						);
						nod.target.target.instance = new MemberData(node, MemberData.TargetType.ValueNode);
						node = nod;
					});
			} else if(rightType == typeof(Rigidbody)) {
				NodeEditorUtility.AddNewNode<MultipurposeNode>(
					NodeGraph.openedGraph.editorData,
					new Vector2(rightNode.editorRect.x - 250, rightNode.editorRect.y),
					(nod) => {
						nod.target.target = new MemberData(
							typeof(RaycastHit).GetProperty("rigidbody")
						);
						nod.target.target.instance = new MemberData(node, MemberData.TargetType.ValueNode);
						node = nod;
					});
			} else if(rightType == typeof(GameObject)) {
				NodeEditorUtility.AddNewNode<MultipurposeNode>(
					NodeGraph.openedGraph.editorData,
					new Vector2(rightNode.editorRect.x - 250, rightNode.editorRect.y),
					(nod) => {
						nod.target.target = new MemberData(
							new MemberInfo[] {
								typeof(RaycastHit),
								typeof(RaycastHit).GetProperty("collider"),
								typeof(Collider).GetProperty("gameObject"),
							}
						);
						nod.target.target.instance = new MemberData(node, MemberData.TargetType.ValueNode);
						node = nod;
					});
			} else if(rightType.IsCastableTo(typeof(Component))) {
				NodeEditorUtility.AddNewNode<MultipurposeNode>(
					NodeGraph.openedGraph.editorData,
					new Vector2(rightNode.editorRect.x - 250, rightNode.editorRect.y),
					(nod) => {
						nod.target.target = new MemberData(
							new MemberInfo[] {
								typeof(RaycastHit),
								typeof(RaycastHit).GetProperty("collider"),
								typeof(Collider).GetMethod("GetComponent", Type.EmptyTypes).MakeGenericMethod(rightType),
							}
						);
						nod.target.target.instance = new MemberData(node, MemberData.TargetType.ValueNode);
						node = nod;
					});
			} else if(rightType.IsCastableTo(typeof(float))) {
				NodeEditorUtility.AddNewNode<MultipurposeNode>(
					NodeGraph.openedGraph.editorData,
					new Vector2(rightNode.editorRect.x - 250, rightNode.editorRect.y),
					(nod) => {
						nod.target.target = new MemberData(
							typeof(RaycastHit).GetProperty("distance")
						);
						nod.target.target.instance = new MemberData(node, MemberData.TargetType.ValueNode);
						node = nod;
					});
			} else if(rightType == typeof(Vector3)) {
				NodeEditorUtility.AddNewNode<MultipurposeNode>(
					NodeGraph.openedGraph.editorData,
					new Vector2(rightNode.editorRect.x - 250, rightNode.editorRect.y),
					(nod) => {
						nod.target.target = new MemberData(
							typeof(RaycastHit).GetProperty("point")
						);
						nod.target.target.instance = new MemberData(node, MemberData.TargetType.ValueNode);
						node = nod;
					});
			}
			#endregion
			return node;
		}

		public override bool IsValid() {
			return leftType == typeof(RaycastHit) && (
				rightType == typeof(Collider) ||
				rightType == typeof(Transform) ||
				rightType == typeof(Rigidbody) ||
				rightType == typeof(GameObject) ||
				rightType == typeof(Vector3) ||
				rightType == typeof(Rigidbody) ||
				rightType.IsCastableTo(typeof(float)) ||
				rightType.IsCastableTo(typeof(Component))
			);
		}
	}

	class RaycastHit2DConverter : AutoConvertPort {
		public override Node CreateNode() {
			Node node = leftNode;
			#region RaycastHit2D
			if(rightType == typeof(Collider2D)) {
				NodeEditorUtility.AddNewNode<MultipurposeNode>(
					NodeGraph.openedGraph.editorData,
					new Vector2(rightNode.editorRect.x - 250, rightNode.editorRect.y),
					(nod) => {
						nod.target.target = new MemberData(
							typeof(RaycastHit2D).GetProperty("collider")
						);
						nod.target.target.instance = new MemberData(node, MemberData.TargetType.ValueNode);
						node = nod;
					});
			} else if(rightType == typeof(Transform)) {
				NodeEditorUtility.AddNewNode<MultipurposeNode>(
					NodeGraph.openedGraph.editorData,
					new Vector2(rightNode.editorRect.x - 250, rightNode.editorRect.y),
					(nod) => {
						nod.target.target = new MemberData(
							typeof(RaycastHit2D).GetProperty("transform")
						);
						nod.target.target.instance = new MemberData(node, MemberData.TargetType.ValueNode);
						node = nod;
					});
			} else if(rightType == typeof(Rigidbody2D)) {
				NodeEditorUtility.AddNewNode<MultipurposeNode>(
					NodeGraph.openedGraph.editorData,
					new Vector2(rightNode.editorRect.x - 250, rightNode.editorRect.y),
					(nod) => {
						nod.target.target = new MemberData(
							typeof(RaycastHit2D).GetProperty("rigidbody")
						);
						nod.target.target.instance = new MemberData(node, MemberData.TargetType.ValueNode);
						node = nod;
					});
			} else if(rightType == typeof(GameObject)) {
				NodeEditorUtility.AddNewNode<MultipurposeNode>(
					NodeGraph.openedGraph.editorData,
					new Vector2(rightNode.editorRect.x - 250, rightNode.editorRect.y),
					(nod) => {
						nod.target.target = new MemberData(
							new MemberInfo[] {
								typeof(RaycastHit2D),
								typeof(RaycastHit2D).GetProperty("collider"),
								typeof(Collider).GetProperty("gameObject"),
							}
						);
						nod.target.target.instance = new MemberData(node, MemberData.TargetType.ValueNode);
						node = nod;
					});
			} else if(rightType.IsCastableTo(typeof(Component))) {
				NodeEditorUtility.AddNewNode<MultipurposeNode>(
					NodeGraph.openedGraph.editorData,
					new Vector2(rightNode.editorRect.x - 250, rightNode.editorRect.y),
					(nod) => {
						nod.target.target = new MemberData(
							new MemberInfo[] {
								typeof(RaycastHit2D),
								typeof(RaycastHit2D).GetProperty("collider"),
								typeof(Collider).GetMethod("GetComponent", Type.EmptyTypes).MakeGenericMethod(rightType),
							}
						);
						nod.target.target.instance = new MemberData(node, MemberData.TargetType.ValueNode);
						node = nod;
					});
			} else if(rightType == typeof(float)) {
				NodeEditorUtility.AddNewNode<MultipurposeNode>(
					NodeGraph.openedGraph.editorData,
					new Vector2(rightNode.editorRect.x - 250, rightNode.editorRect.y),
					(nod) => {
						nod.target.target = new MemberData(
							typeof(RaycastHit2D).GetProperty("distance")
						);
						nod.target.target.instance = new MemberData(node, MemberData.TargetType.ValueNode);
						node = nod;
					});
			} else if(rightType == typeof(Vector3)) {
				NodeEditorUtility.AddNewNode<MultipurposeNode>(
					NodeGraph.openedGraph.editorData,
					new Vector2(rightNode.editorRect.x - 250, rightNode.editorRect.y),
					(nod) => {
						nod.target.target = new MemberData(
							typeof(RaycastHit2D).GetProperty("point")
						);
						nod.target.target.instance = new MemberData(node, MemberData.TargetType.ValueNode);
						node = nod;
					});
			}
			#endregion
			return node;
		}

		public override bool IsValid() {
			return leftType == typeof(RaycastHit2D) && (
				rightType == typeof(Collider2D) ||
				rightType == typeof(Transform) ||
				rightType == typeof(Rigidbody2D) ||
				rightType == typeof(GameObject) ||
				rightType == typeof(Vector3) ||
				rightType.IsCastableTo(typeof(float)) ||
				rightType.IsCastableTo(typeof(Component))
			);
		}
	}
}