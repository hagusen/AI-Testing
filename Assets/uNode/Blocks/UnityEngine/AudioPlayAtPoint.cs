using UnityEngine;

namespace MaxyGames.Events {
	[BlockMenu("UnityEngine", "Audio.PlayAtPoint")]
	public class AudioPlayAtPoint : Action {
		public bool useTransform = true;
		[Hide("useTransform", false)]
		[ObjectType(typeof(Transform))]
		public MemberData transform;
		[Hide("useTransform", true)]
		[ObjectType(typeof(Vector3))]
		public MemberData position;
		public bool playRandomSound;
		[Hide("playRandomSound", true)]
		[ObjectType(typeof(AudioClip))]
		public MemberData clip;
		[Hide("playRandomSound", false)]
		[ObjectType(typeof(AudioClip))]
		public MemberData[] clips;
		[ObjectType(typeof(float))]
		public MemberData volumeScale = new MemberData(1);

		protected override void OnExecute() {
			if(useTransform) {
				if(!playRandomSound) {
					AudioSource.PlayClipAtPoint(clip.GetValue<AudioClip>(), transform.GetValue<Transform>().position, volumeScale.GetValue<float>());
				} else if(clips.Length > 0) {
					AudioSource.PlayClipAtPoint(clips[Random.Range(0, clips.Length - 1)].GetValue<AudioClip>(), transform.GetValue<Transform>().position, volumeScale.GetValue<float>());
				}
			} else {
				if(!playRandomSound) {
					AudioSource.PlayClipAtPoint(clip.GetValue<AudioClip>(), position.GetValue<Vector3>(), volumeScale.GetValue<float>());
				} else if(clips.Length > 0) {
					AudioSource.PlayClipAtPoint(clips[Random.Range(0, clips.Length - 1)].GetValue<AudioClip>(), position.GetValue<Vector3>(), volumeScale.GetValue<float>());
				}
			}
		}

		public override string GenerateCode(Object obj) {
			string position;
			if(useTransform) {
				position = CodeGenerator.ParseValue((object)transform).Add(".position");
			} else {
				position = CodeGenerator.ParseValue((object)this.position);
			}
			if(playRandomSound) {
				string data = null;
				if(clips.Length > 0) {
					string varName = CodeGenerator.GenerateVariableName("random", this);
					string randomData = CodeGenerator.GenerateVariableDeclaration(varName, typeof(System.Collections.Generic.List<AudioClip>));
					data += randomData;
					foreach(var var in clips) {
						data += CodeGenerator.GetInvokeCode(varName, "Add", var).AddLineInFirst();
					}
					data += CodeGenerator.GenerateInvokeCode(typeof(AudioSource), "PlayClipAtPoint",
						CodeGenerator.GetInvokeCode(varName + "[]", 
						CodeGenerator.GetInvokeCode(typeof(Random), "Range", 0, clips.Length - 1)).RemoveSemicolon(),
						position,
						CodeGenerator.ParseValue(volumeScale)).AddLineInFirst();
					return data;
				} else {
					throw new System.Exception("The clips is empty");
				}
			}
			return CodeGenerator.GetInvokeCode(typeof(AudioSource), "PlayClipAtPoint", clip, CodeGenerator.WrapString(position), volumeScale);
		}

		public override void CheckError(Object owner) {
			base.CheckError(owner);
			uNode.uNodeUtility.CheckError(volumeScale, owner, Name + " - volumeScale");
			if(!playRandomSound) {
				uNode.uNodeUtility.CheckError(clip, owner, Name + " - clip");
			} else {
				uNode.uNodeUtility.CheckError(clips, owner, Name + " - clip");
			}
			if(useTransform) {
				uNode.uNodeUtility.CheckError(transform, owner, Name + " - transform");
			} else {
				uNode.uNodeUtility.CheckError(position, owner, Name + " - position");
			}
		}

		public override string GetDescription() {
			return "Plays an AudioClip at a given position in world space.";
		}
	}
}