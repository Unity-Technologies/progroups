using UnityEngine;
using UnityEditor;
using System.Collections;

/**
 * JS compiled in Editor pass doesn't know about CS compiled in Editor pass.
 */
public class ProGroups_About : Editor
{
	[MenuItem("Tools/ProGroups/About", false, 0)]
	public static void MenuAbout ()
	{
		groups_AboutWindow.Init("Assets/ProCore/ProGroups/About/pc_AboutEntry_ProGroups.txt", true);
	}
}