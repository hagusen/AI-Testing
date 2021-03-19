using UnityEngine;

namespace MaxyGames.Events {
	[BlockMenu("UnityEngine", "Audio.PlayOneShot")]
	public class AudioPlayOneShot : Action {
		[ObjectType(typeof(AudioSource))]
		public MemberData audio;
		public bool playRandomSound;
		[Hide("playRandomSound", true)]
		[ObjectType(typeof(AudioClip))]
		public MemberData clip;
		[Hide("playRandomSound", false)]
		[ObjectType(typeof(AudioClip))]
		public MemberData[] clips;
		[ObjectType(typeof(float))]
		public MemberData volumeScale;

		protected override void OnExecute() {
				if(audio == null) {
					Debug.LogError("No Audio Source");
					return;
				}
				AudioSource source = audio.GetValue<AudioSource>();
				if(source != null) {
					if(!playRandomSound) {
						source.PlayOneShot(clip.GetValue<AudioClip>(), volumeScale.GetValue<float>());
						return;
					}
					if(clips.Length > 0) {
						source.PlayOneShot(clips[Random.Range(0, clips.Length - 1)].GetValue<AudioClip>(), volumeScale.GetValue<float>());
					}
				}
		}

		public override string GenerateCode(Object obj) {
			string audioName = null;
			if(audio.isAssigned) {
				audioName = CodeGenerator.ParseValue((object)audio);
			} else {
				return null;
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
					data += CodeGenerator.GenerateInvokeCode(audioName, "PlayOneShot",
						CodeGenerator.GetInvokeCode(varName + "[]", 
						CodeGenerator.GetInvokeCode(typeof(Random), "Range", 0, clips.Length - 1)).RemoveSemicolon(),
						CodeGenerator.ParseValue(volumeScale)).AddLineInFirst();
					return data;
				}
			}
			return CodeGenerator.GetInvokeCode(audioName, "PlayOneShot", clip, volumeScale).ToString();
		}

		public override void CheckError(Object owner) {
			base.CheckError(owner);
			uNode.uNodeUtility.CheckError(audio, owner, Name + " - audio");
			uNode.uNodeUtility.CheckError(volumeScale, owner, Name + " - volumeScale");
			if(!playRandomSound) {
				uNode.uNodeUtility.CheckError(clip, owner, Name + " - clip");
			} else {
				uNode.uNodeUtility.CheckError(clips, owner, Name + " - clip");
			}
		}

		public override string GetDescription() {
			return "Plays an AudioClip, and scales the AudioSource volume by volumeScale.";
		}
	}
}