using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ProGroups
{
	/**
	 * Container class for a group.  Consists of name, objects in group, and frozen / hidden status.
	 */
	[System.Serializable]
	public class Group
	{
		public string name;
		public GameObject[] objects;
		public bool frozen;
		public bool hidden;

		public Group(string InName, GameObject[] InObjects, bool InFrozen, bool InHidden)
		{
			this.name = InName;
			this.objects = InObjects.Distinct().ToArray();
			this.frozen = InFrozen;
			this.hidden = InHidden;
		}

		/**
		 * Remove null or missing objects from this group.
		 */
		public void RemoveNullOrEmpty()
		{
			if( objects == null )
			{
				objects = new GameObject[0];
			}
			else
			{
				IEnumerable<GameObject> valid = objects.Where(x => x != null);
				objects = valid == null ? new GameObject[0] : valid.ToArray();
			}
		}
		
		/**
		 * Add @InGameObjects to the list of objects in this group.
		 */
		public void AddObjects(GameObject[] InObjects)
		{
			ArrayExt.AddRange(ref objects, InObjects);
			objects = objects.Distinct().ToArray();
		}

		/**
		 * Remove @InObject from objects.
		 */
		public void RemoveObject(GameObject InObject)
		{
			ArrayExt.Remove(ref objects, InObject);
		
		}

		/**
		 * Remove @InObjects from objects.
		 */
		public void RemoveObjects(GameObject[] InObjects)
		{
			ArrayExt.Remove(ref objects, InObjects);
		}
	}

	/**
	 * Extension methods for working with arrays.
	 */
	static class ArrayExt
	{
		public static void Add<T>(ref T[] array, T val)
		{
			T[] tmp = new T[array.Length + 1];
			System.Array.Copy(array, tmp, array.Length);
			tmp[array.Length] = val;
			array = tmp;
		}

		/**
		 * Concatenate @values with @array.
		 */
		public static void AddRange<T>(ref T[] array, T[] values)
		{
			T[] tmp = new T[array.Length + values.Length];
			System.Array.Copy(array, tmp, array.Length);
			System.Array.Copy(values, 0, tmp, array.Length, values.Length);
			array = tmp;
		}

		/**
		 * Remove all instances of @value from @array.
		 */
		public static void Remove<T>(ref T[] array, T value)
		{
			array = array.Where(x => !value.Equals(x)).ToArray();
		}

		public static void Remove<T>(ref T[] array, T[] values)
		{
			array = array.Where(x => !values.Contains(x)).ToArray();
		}

		public static void RemoveAt<T>(ref T[] array, int index)
		{
			T[] tmp = new T[array.Length-1];
			for(int i = 0; i < array.Length; i++)
				if(i != index)
					tmp[i > index ? i-1 : i] = array[i];
			array = tmp;
		}
	}

	/**
	 * Component that holds array of user groups.
	 */
	public class GroupContainer : MonoBehaviour
	{
		public Group[] sceneGroups = new Group[0] {};

		/**
		 * Add a new group.
		 */
		public void NewGroup(string InName, GameObject[] InObjects)
		{
			Group newGroup = new Group(InName, InObjects, false, false);

			if(sceneGroups != null)
			{
				ArrayExt.Add<Group>(ref sceneGroups, newGroup);
			}
			else
			{
				sceneGroups = new Group[] { newGroup };
			}
		}

		/**
		 * Freeze all objects in @InGroup.
		 */
		public void ToggleFreeze(Group InGroup)
		{
			InGroup.RemoveNullOrEmpty();

			InGroup.frozen = !InGroup.frozen;

			foreach(GameObject obj in InGroup.objects)
			{
				if(InGroup.frozen)
					obj.Freeze();
				else
					obj.Thaw();
			}
		}

		/**
		 * Hide or show all items in @InGroup.
		 */
		public void ToggleVis(Group InGroup)
		{
			if(InGroup.hidden)
				ShowGroup(InGroup);
			else
				HideGroup(InGroup);
		}
		
		/**
		 * Hide all items in @InGroup.
		 */
		public void HideGroup(Group InGroup)
		{
			InGroup.RemoveNullOrEmpty();

			foreach(GameObject obj in InGroup.objects)
				obj.Hide();

			InGroup.hidden = true;
		}

		/**
		 * Show all items in @InGroup.
		 */
		public void ShowGroup(Group InGroup)
		{
			InGroup.RemoveNullOrEmpty();

			foreach(GameObject obj in InGroup.objects)
				obj.Show();

			InGroup.hidden = false;
		}

		/**
		 * Hide all groups save for group at index @i.
		 */
		public void Isolate(int i )
		{
			for(int j = 0; j < sceneGroups.Length; j++)
			{
				if(j != i)
				{
					HideGroup(sceneGroups[j]);
				}
			}

			ShowGroup(sceneGroups[i]);
		}

		public void RemoveGroups(IEnumerable<int> indices)
		{
			List<int> sorted = new List<int>(indices);
			sorted.Sort();

			int offset = 0;

			for(int i = 0; i < sorted.Count; i++)
			{
				int ind = sorted[i] - offset++;
				RemoveGroup(ind);
			}
		}

		/**
		 * Remove group at index @i from the container list.
		 */
		public void RemoveGroup(int i )
		{
			sceneGroups[i].RemoveNullOrEmpty();

			foreach(GameObject obj in sceneGroups[i].objects)
			{
			#if UNITY_3_5
				obj.active = true;
			#else
				obj.SetActive(true);
			#endif
				obj.hideFlags = 0;
			}

			ArrayExt.RemoveAt(ref sceneGroups, i);
		}

		/**
		 * Rebuild @InGroup with @InObjects, and reset hidden and frozen flags.
		 */
		public void UpdateGroup(Group InGroup, GameObject[] InObjects)
		{
			InGroup.RemoveNullOrEmpty();

			foreach(GameObject obj in InGroup.objects)
			{
			#if UNITY_3_5
				obj.active = true;
			#else
				obj.SetActive(true);
			#endif
				obj.hideFlags = 0;
			}

			InGroup.objects = InObjects;

			InGroup.hidden = false;
			InGroup.frozen = false;
		}

		/**
		 * Move group at @InShiftIndex up one index.
		 */
		public void MoveGroupUp(int InShiftIndex)
		{
			if( InShiftIndex < 1 || InShiftIndex > sceneGroups.Length-1)
				return;

			Group tmp = sceneGroups[InShiftIndex-1];
			sceneGroups[InShiftIndex-1] = sceneGroups[InShiftIndex];
			sceneGroups[InShiftIndex] = tmp;
		}

		/**
		 * Move group at @InShiftIndex down one index.
		 */
		public void MoveGroupDown(int InShiftIndex)
		{
			if( InShiftIndex < 0 || InShiftIndex > sceneGroups.Length-2)
				return;

			Group tmp = sceneGroups[InShiftIndex+1];
			sceneGroups[InShiftIndex+1] = sceneGroups[InShiftIndex];
			sceneGroups[InShiftIndex] = tmp;
		}

		/**
		 * Remove null or empty groups.
		 */
		public void RemoveNullOrEmpty()
		{
			sceneGroups = sceneGroups.Where(x => x != null && x.objects.Length > 0).ToArray();
		}

		/**
		 * Make sure there aren't any null objects in groups.
		 */
		public void Clean()
		{
			for(int i = 0; i < sceneGroups.Length; i++) {
				sceneGroups[i].RemoveNullOrEmpty();
			}
		}
	}
}
