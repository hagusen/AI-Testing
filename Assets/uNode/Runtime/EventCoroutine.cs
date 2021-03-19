using System.Collections;
using UnityEngine;

namespace MaxyGames.Runtime {
	/// <summary>
	/// EventCoroutine is a class to Start coroutine with return data and some function that useful for making event based coroutine.
	/// </summary>
	public class EventCoroutine {
		private int rawState;

		/// <summary>
		/// The owner of coroutine to start running coroutine
		/// </summary>
		public MonoBehaviour owner;
		/// <summary>
		/// The target IEnumerable.
		/// </summary>
		private IEnumerable target;
		/// <summary>
		/// The currently IEnumerator.
		/// </summary>
		private IEnumerator iterator;
		/// <summary>
		/// The action to invoke on event stop.
		/// </summary>
		private System.Action onStop;
		/// <summary>
		/// The current coroutine, null indicate coroutine not running
		/// </summary>
		public Coroutine coroutine { get; private set; }
		/// <summary>
		/// The result object of current return of running coroutine
		/// </summary>
		public object result;

		/// <summary>
		/// Indicate state of Coroutine, 
		/// "Success" indicate state is success, 
		/// "Failure" indicate state is failure, 
		/// otherwise indicate state is running or never running
		/// </summary>
		public string state {
			get {
				switch(rawState) {
					case 1:
						return "Success";
					case 2:
						return "Failure";
					default:
						return null;
				}
			}
		}


		/// <summary>
		/// Indicate coroutine is finished running when its has running before
		/// </summary>
		public bool IsFinished {
			get {
				return coroutine == null && rawState != 0;
			}
		}

		/// <summary>
		/// Indicate coroutine is finished running or never running
		/// </summary>
		public bool IsFinishedOrNeverRun {
			get {
				return rawState != 0;
			}
		}

		/// <summary>
		/// True if the state is "Success"
		/// </summary>
		public bool IsSuccess => rawState == 1;
		/// <summary>
		/// Try if the state is "Failure"
		/// </summary>
		public bool IsFailure => rawState == 2;
		/// <summary>
		/// True if the state is "Running"
		/// </summary>
		public bool IsRunning => rawState == 0;

		/// <summary>
		/// Create new Event Coroutine.
		/// </summary>
		/// <param name="owner">The coroutine owner</param>
		/// <param name="target">The coroutine function target</param>
		public EventCoroutine(MonoBehaviour owner, IEnumerable target) {
			this.target = target;
			this.owner = owner;
		}

		/// <summary>
		/// Create new Event Coroutine
		/// </summary>
		/// <param name="owner">The coroutine owner</param>
		/// <param name="target">The coroutine function target</param>
		/// <param name="onStop"></param>
		public EventCoroutine(MonoBehaviour owner, IEnumerable target, System.Action onStop) {
			this.target = target;
			this.owner = owner;
			this.onStop = onStop;
		}

		/// <summary>
		/// Create new Event Coroutine without owner.
		/// </summary>
		/// <param name="target"></param>
		public EventCoroutine(IEnumerable target) {
			this.target = target;
			this.owner = RuntimeSMHelper.Instance;
		}

		private IEnumerator RunCoroutine() {
			var resultState = 1;
#if UNITY_EDITOR
			if(debug) {
				uNodeDEBUG.InvokeEvent(this, eventUID, debugUID);
			}
#endif
			while(iterator.MoveNext()) {
				result = iterator.Current;
				if(result is bool) {
					bool r = (bool)result;
					resultState = r ? 1 : 2;
					break;
				} else if(result is string) {
					string r = result as string;
					if(r == "Success") {
						resultState = 1;
						break;
					} else if(r == "Failure") {
						resultState = 2;
						break;
					}
				} else if(result is EventCoroutine) {
					EventCoroutine nestedCoroutine = result as EventCoroutine;
					if(!nestedCoroutine.IsFinished) {
						//Wait the nestedCoroutine until finish
						yield return nestedCoroutine.coroutine;
					}
					continue;
				}
				yield return result;
			}
			rawState = resultState;
			coroutine = null;
#if UNITY_EDITOR
			if(debug) {
				uNodeDEBUG.InvokeEvent(this, eventUID, debugUID);
			}
#endif
			if(onStop != null) {
				onStop();
			}
		}

		/// <summary>
		/// Run the coroutine if not running
		/// </summary>
		/// <returns></returns>
		public EventCoroutine Run() {
#if UNITY_EDITOR
			if(!Application.isPlaying) {
				throw new System.Exception("You need be in play mode to use EventCoroutine");
			}
#endif
			if(coroutine == null) {
				rawState = 0;
				iterator = target.GetEnumerator();
				coroutine = owner.StartCoroutine(RunCoroutine());
			}
			return this;
		}

		/// <summary>
		/// Stop Running Coroutine.
		/// </summary>
		public void Stop(bool state = false) {
			if(coroutine != null && owner != null) {
				owner.StopCoroutine(coroutine);
				rawState = state ? 1 : 2;
				coroutine = null;
				if(onStop != null) {
					onStop();
				}
			}
		}
		#region Debug
#if UNITY_EDITOR
		private bool debug;
		private int debugUID;
		private int eventUID;
#endif

		/// <summary>
		/// Call this to implement debugging in editor.
		/// </summary>
		/// <param name="nodeUID"></param>
		public void Debug(int eventSystemUID, int nodeUID) {
#if UNITY_EDITOR
			debug = true;
			eventUID = eventSystemUID;
			debugUID = nodeUID;
#endif
		}
		#endregion
	}
}