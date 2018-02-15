using UnityEngine;

namespace ProGroups
{
	/**
	 *	Utility functions for ProGroups.
	 */
	public static class ProGroups_Util
	{
		/**
		 *	Set hide flags for "Frozen" state (NotEditable, but visibility doesn't change).
		 */
		public static void Freeze(this GameObject go)
		{
			HideFlags flags = go.hideFlags;
			go.hideFlags = flags | HideFlags.NotEditable;
		}

		/**
		 *	Set hide flags for not "Frozen" state (clear NotEditable flag; visibility doesn't change).
		 */
		public static void Thaw(this GameObject go)
		{
			HideFlags flags = go.hideFlags;
			go.hideFlags = flags & (~HideFlags.NotEditable);
		}

		/**
		 *	Freeze and set to inactive.
		 */
		public static void Hide(this GameObject go)
		{
			go.SetActive(false);
		}

		/**
		 *	Thaw and set to active.
		 */
		public static void Show(this GameObject go)
		{
			go.SetActive(true);
		}
	}
}
