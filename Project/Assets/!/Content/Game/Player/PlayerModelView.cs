using UnityEngine;

namespace Game.Player
{
	public sealed class PlayerModelView : MonoBehaviour
	{
		public void PrintEvent(string s) {
			Debug.Log("PrintEvent: " + s + " called at: " + Time.time);
		}

		// Receiver for the 'Land' AnimationEvent on the Death clip
		public void Land() { }
	}
}