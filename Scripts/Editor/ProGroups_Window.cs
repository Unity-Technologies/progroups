using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ProGroups
{

	/**
	 * Editor interface for ProGroups.
	 */
	public class ProGroups_Window : EditorWindow
	{
#region Class Variables

		const string GROUPS_GAMEOBJECT_NAME = "Progroups2_Groups_Container_Object";

		static readonly Color EVEN_ROW_COLOR = new Color(.1f, .1f, .1f, .2f);
		static readonly Color ODD_ROW_COLOR = new Color(.35f, .35f, .35f, .2f);

		// A reference to the scene's current groupContainer component.
		static GroupContainer _groupContainer;

		private Group nextRepaintSetGroup = null;
		private Dictionary<Group, bool> groupExpandos = new Dictionary<Group, bool>();

		static GUIStyle _backgroundColorStyle = null;

		public static GUIStyle backgroundColorStyle
		{
			get
			{
				if(_backgroundColorStyle == null)
				{
					_backgroundColorStyle = new GUIStyle();
					_backgroundColorStyle.margin = new RectOffset(4,4,4,4);
					_backgroundColorStyle.padding = new RectOffset(4,4,4,4);
					_backgroundColorStyle.normal.background = EditorGUIUtility.whiteTexture;
				}

				return _backgroundColorStyle;
			}
		}

		static GroupContainer groupContainer
		{
			get
			{
				if(_groupContainer == null)
				{
					GameObject go = GameObject.Find(GROUPS_GAMEOBJECT_NAME);

					if(go == null)
					{
						go = new GameObject();
						go.name = GROUPS_GAMEOBJECT_NAME;
						go.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
						_groupContainer = go.AddComponent<GroupContainer>();
					}
					else
					{
						_groupContainer = go.GetComponent<GroupContainer>();
					}
				}

				return _groupContainer;
			}

			set
			{
				_groupContainer = value;
			}
		}

		// Indices of groups that contains items in the current selection
		List<int> selectedGroupIndices = new List<int>();

		// Path to the icons used in the editor.
		string guiPath = "Assets/ProCore/ProGroups/GUI/";

		// Default name for new groups.
		public static string newGroupName = "New Group";

		// Icons
		Texture2D 	icon_Rect,
					icon_SnowFlake,
					icon_Eye,
					icon_Select,
					// icon_UpdateGroup,
					icon_MoveUp,
					icon_Delete,
					icon_Gear,
					icon_Add,
					icon_freeze,
					icon_vis,
					icon_Drag;
					// icon_MultiPlus;

		// Scrollbar position.
		Vector2 scrollPos;

		// If the editor is in 'modification' mode - meaning editing the group position / deleting / renaming groups.
		bool modMode = false;
		readonly Color LightGray = new Color(1f, 1f, 1f, .2f);

		static ProGroups_Window instance;
#endregion

#region Menu

		[MenuItem("Tools/ProGroups/ProGroups Window")]
		static void MenuInitProGroupsWindow()
		{
			bool floating = EditorPrefs.HasKey("groups_floatingWindow") ? EditorPrefs.GetBool("groups_floatingWindow") : false;
			EditorWindow.GetWindow<ProGroups_Window>(floating, "ProGroups", false);
		}

		[MenuItem("Tools/ProGroups/New Group From Selection %g")]
		 static void NewGroupFromSelection()
		 {
		 	ProGroups_Window.NewGroupWithObjects( Selection.gameObjects, Selection.gameObjects.Length > 0 ? Selection.gameObjects[0].name : "New Group" );

			if(ProGroups_Window.instance != null)
			{
				ProGroups_Window.instance.Repaint();
				ProGroups_Window.instance.OnSelectionChange();
			}
		 }

		 [MenuItem("Tools/ProGroups/Clear All Groups in Scene")]
		 static void ClearAll()
		 {
		 	GameObject go = GameObject.Find(GROUPS_GAMEOBJECT_NAME);
		 	int count = 0;

		 	if( !EditorUtility.DisplayDialog("Remove All Groups", "This will remove all groups in this scene.  It is not undo-able.  Continue?", "Yes", "No") )
		 		return;

		 	while(go != null)
		 	{
		 		count ++;
		 		GameObject.DestroyImmediate(go);
		 		go = GameObject.Find(GROUPS_GAMEOBJECT_NAME);
		 	}

		 	SceneView.lastActiveSceneView.ShowNotification(new GUIContent("Removed " + count + " Group Container" + (count == 1 ? "" : "s"), ""));
		 }

		 void OpenContextMenu()
		 {
		 	GenericMenu menu = new GenericMenu();

	 		menu.AddItem (new GUIContent("Window/Open as Floating Window", ""), EditorPrefs.GetBool("groups_floatingWindow", false), ContextMenu_OpenFloatingWindow);
			menu.AddItem (new GUIContent("Window/Open as Dockable Window", ""), !EditorPrefs.GetBool("groups_floatingWindow", false), ContextMenu_OpenDockableWindow);

			menu.AddSeparator("");

			menu.AddItem( new GUIContent("Collapse Selected Groups"), false, () =>
			{
				instance.CollapseSelectedGroups();
			});

			menu.ShowAsContext();
		}

		void CollapseSelectedGroups()
		{
			if(selectedGroupIndices.Count < 1)
				return;

			IEnumerable<Group> selectedGroups = groupContainer.sceneGroups.Where((x,i) => selectedGroupIndices.Contains(i));
			GameObject[] all = selectedGroups.SelectMany(x => x.objects).ToArray();
			groupContainer.RemoveGroups(selectedGroupIndices);
			NewGroupWithObjects(all, string.Join("+", selectedGroups.Select(x => x.name).ToArray()));
		}

		static void ContextMenu_OpenFloatingWindow()
		{
			EditorPrefs.SetBool("groups_floatingWindow", true);

			EditorWindow.GetWindow<ProGroups_Window>().Close();
			EditorWindow.GetWindow<ProGroups_Window>(true, "ProGroups", true);
		}

		static void ContextMenu_OpenDockableWindow()
		{
			EditorPrefs.SetBool("groups_floatingWindow", false);

			EditorWindow.GetWindow<ProGroups_Window>().Close();
			EditorWindow.GetWindow<ProGroups_Window>(false, "ProGroups", true);
		}
#endregion

#region Initialization

		/**
		 *	Find a directory in the Assets folder by searching for a partial path.
		 */
		static string FindFolder(string folder, bool exactMatch = false)
		{
			string single = folder.Replace("\\", "/").Substring(folder.LastIndexOf('/') + 1);

			string[] matches = Directory.GetDirectories("Assets/", single, SearchOption.AllDirectories);

			foreach(string str in matches)
			{
				string path = str.Replace("\\", "/");

				if( path.Contains(folder) )
				{
					if(exactMatch)
					{
						string found = path.Substring(str.LastIndexOf('/') + 1);

						if(!found.Equals(single))
							continue;
					}

					if(!path.EndsWith("/"))
						path += "/";

					return path;
				}
			}

			return null;
		}

		void OnEnable()
		{
			this.autoRepaintOnSceneChange = true;
			this.wantsMouseMove = true;
			instance = this;

			guiPath = FindFolder("ProGroups/GUI");

			icon_Rect 			= (Texture2D) AssetDatabase.LoadAssetAtPath(guiPath + "ProGroupsIcons_Rect.png", typeof(Texture2D));
			icon_SnowFlake 		= (Texture2D) AssetDatabase.LoadAssetAtPath(guiPath + "ProGroupsIcons_SnowFlake.png", typeof(Texture2D));
			icon_Eye 			= (Texture2D) AssetDatabase.LoadAssetAtPath(guiPath + "ProGroupsIcons_Eye.png", typeof(Texture2D));
			icon_Select 		= (Texture2D) AssetDatabase.LoadAssetAtPath(guiPath + "ProGroupsIcons_Select.png", typeof(Texture2D));
			// icon_UpdateGroup 	= (Texture2D) AssetDatabase.LoadAssetAtPath(guiPath + "ProGroupsIcons_UpdateGroup.png", typeof(Texture2D));
			icon_MoveUp  		= (Texture2D) AssetDatabase.LoadAssetAtPath(guiPath + "ProGroupsIcons_MoveUp.png", typeof(Texture2D));
			icon_Delete  		= (Texture2D) AssetDatabase.LoadAssetAtPath(guiPath + "ProGroupsIcons_Delete.png", typeof(Texture2D));
			icon_Gear  			= (Texture2D) AssetDatabase.LoadAssetAtPath(guiPath + "ProGroupsIcons_Gear.png", typeof(Texture2D));
			icon_Drag  			= (Texture2D) AssetDatabase.LoadAssetAtPath(guiPath + "ProGroupsIcons_Drag.png", typeof(Texture2D));
			icon_Add  			= (Texture2D) AssetDatabase.LoadAssetAtPath(guiPath + "ProGroupsIcons_Add.png", typeof(Texture2D));
			// icon_MultiPlus 		= (Texture2D) AssetDatabase.LoadAssetAtPath(guiPath + "ProGroupsIcons_MultiPlus.png", typeof(Texture2D));
		}
#endregion

		/**
		 * On a selection change, update the currently selected display.
		 */
		public void OnSelectionChange()
		{
			selectedGroupIndices.Clear();

			for(int i = 0; i < groupContainer.sceneGroups.Length; i++)
			{
				if( groupContainer.sceneGroups[i].objects.Intersect(Selection.gameObjects).Any() )
					selectedGroupIndices.Add(i);
			}

			Repaint();
		}

		void OnLostFocus()
		{
			editingTitle = -1;
		}

		private int dragIndex = -1;
		private int editingTitle = -1;
		private bool wantsRepaint = false;
		private bool guiInitialied = false;
		private bool dragInProgress = false;
		private bool dragImageReady = false;
		private GUIStyle iconButtonStyle, labelStyle, textFieldStyle;
		private GUIContent groupTooltip = new GUIContent();

		void OnGUI()
		{
			Event curEvent = Event.current;

			if(curEvent.type == EventType.ContextClick)
				OpenContextMenu();

			if(!guiInitialied)
			{
				iconButtonStyle = new GUIStyle(GUI.skin.button);
				iconButtonStyle.stretchWidth = false;
				iconButtonStyle.margin = new RectOffset(2,2,2,2);
				iconButtonStyle.padding.left = 2;
				iconButtonStyle.padding.right = 2;
				iconButtonStyle.normal.background = null;
				iconButtonStyle.hover.background = null;
				iconButtonStyle.active.background = null;
				iconButtonStyle.focused.background = null;
				iconButtonStyle.onNormal.background = null;
				iconButtonStyle.onHover.background = null;
				iconButtonStyle.onActive.background = null;
				iconButtonStyle.onFocused.background = null;

				labelStyle = new GUIStyle(GUI.skin.label);
				labelStyle.padding.top = 3;
				guiInitialied = true;

				textFieldStyle = new GUIStyle(EditorStyles.textField);
				textFieldStyle.margin.top = 4;
				textFieldStyle.padding.top = 1;
			}

			GUILayout.Label(modMode ? "Edit Groups" : "Create New", EditorStyles.boldLabel);

			EditorGUILayout.BeginHorizontal();

				if(!modMode)
				{
					newGroupName = EditorGUI.TextField(new Rect(3, 27, this.position.width-50, 17), newGroupName);

					GUILayout.FlexibleSpace();

					if(newGroupName == "")
						newGroupName = "New Group";

					if(GUILayout.Button(new GUIContent(icon_Add, "Create New Group from Selected Objects"), iconButtonStyle))
					{
						GUIUtility.keyboardControl = 0;

		 				NewGroupWithObjects( Selection.gameObjects, newGroupName );
					}

					if(GUILayout.Button (new GUIContent(icon_Gear, "Modify Groups"), iconButtonStyle))
					{
						modMode = true;
					}

				}
				else
				{
					if(GUILayout.Button("Done"))
					{
						modMode = false;
					}
				}

			EditorGUILayout.EndHorizontal();

			GUILayout.Label("Groups", EditorStyles.boldLabel);

			groupContainer.Clean();

			GameObject[] dragItems;
			bool dragging = ListenForDragAndDrop(curEvent, out dragItems);

			if(dragging && !dragInProgress)
				dragInProgress = true;

			if(groupContainer.sceneGroups != null)
			{
				scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
				EditorGUILayout.BeginVertical();

				for(int i = 0; i < groupContainer.sceneGroups.Length; i++)
				{
					Group group = groupContainer.sceneGroups[i];

					if(group.frozen)
						icon_freeze = icon_SnowFlake;
					else
						icon_freeze = icon_Rect;

					if(group.hidden)
						icon_vis = icon_Rect;
					else
						icon_vis = icon_Eye;

					Rect r = i > 0 ? GUILayoutUtility.GetLastRect() : new Rect(2, 0, 0, 0);
					Rect rowRect = new Rect(r.x, r.y + r.height + 2, this.position.width-8, 32);
					bool doExpandGroups = false;

					GUI.backgroundColor = i % 2 == 0 ? EVEN_ROW_COLOR : ODD_ROW_COLOR;
					GUILayout.BeginVertical(backgroundColorStyle);
					GUI.backgroundColor = Color.white;

					GUILayout.BeginHorizontal();

					if(modMode)
					{
						//move up
						GUI.enabled = i != 0;
						if(GUILayout.Button(new GUIContent(icon_MoveUp, "Move Group Up"), iconButtonStyle))
						{
							GUIUtility.keyboardControl = 0;
							groupContainer.MoveGroupUp(i);
							Repaint();
						}

						// would it have been easier to just create another icon?  probably.  would it have
						// been as much fun?  probably not.
						Rect mbr = GUILayoutUtility.GetLastRect();
						Vector2 pivot = new Vector2(mbr.x + mbr.width + mbr.width/2f + 2, mbr.y + mbr.height/2f - 1);
						GUIUtility.RotateAroundPivot(180f, pivot);
						GUI.enabled = i != groupContainer.sceneGroups.Length-1;
						if(GUILayout.Button(new GUIContent(icon_MoveUp, "Move Group Down"), iconButtonStyle))
						{
							GUIUtility.keyboardControl = 0;
							groupContainer.MoveGroupDown(i);
							Repaint();
						}
						GUIUtility.RotateAroundPivot(-180f, pivot);
						GUI.enabled = true;

						//delete
						if(GUILayout.Button(new GUIContent(icon_Delete, "Remove Group (does not delete objects)"), iconButtonStyle))
						{
							if(EditorUtility.DisplayDialog("Remove This Group?", "All objects from the group will become visible and un-frozen. No objects will be deleted.", "Confirm","Cancel"))
							{
								GUIUtility.keyboardControl = 0;
								Undo.RecordObject(groupContainer, "Remove Group");
								groupContainer.RemoveGroup(i);
								Repaint();
							}
						}

						// //add selected to group
						// if(GUILayout.Button(new GUIContent(icon_MultiPlus, "Add Selected Objects to this Group"), iconButtonStyle))
						// {
						// 	GUIUtility.keyboardControl = 0;
						// 	AddToGroup(Selection.gameObjects, group);
						// 	Repaint();
						// }

						// //update
						// if(GUILayout.Button(new GUIContent(icon_UpdateGroup, "Rebuild Group from Selection"), iconButtonStyle))
						// {
						// 	if(EditorUtility.DisplayDialog("Replace Objects in the Group With Selected Objects", "Note: all objects from the old group will become visible and un-frozen.", "Confirm", "Cancel"))
						// 	{
						// 		GUIUtility.keyboardControl = 0;

						// 		groupContainer.UpdateGroup(group, Selection.gameObjects);

						// 		Repaint();
						// 	}
						// }

						//group name
						group.name = EditorGUILayout.TextField(group.name);
					}
					else
					{
						if( selectedGroupIndices.Contains(i) || dragIndex == i )
						{
							GUI.backgroundColor = i == dragIndex ? Color.green : Color.white;
							GUI.Box(rowRect, "");
							GUI.backgroundColor = Color.white;
						}

						if(!groupExpandos.TryGetValue(group, out doExpandGroups))
							groupExpandos.Add(group, doExpandGroups);

						EditorGUI.BeginChangeCheck();

						doExpandGroups = EditorGUILayout.Toggle(doExpandGroups, EditorStyles.foldout, GUILayout.MaxWidth(14));

						if(EditorGUI.EndChangeCheck())
							groupExpandos[group] = doExpandGroups;

						//select
						if(GUILayout.Button(new GUIContent(icon_Select, "Select Group Objects"), iconButtonStyle))
						{
							GUIUtility.keyboardControl = 0;
							nextRepaintSetGroup = group;
						}

						//vis toggle
						if(GUILayout.Button(icon_vis, iconButtonStyle))
						{
							if(Event.current.alt)
							{
								groupContainer.Isolate(i);
							}
							else
							{
								groupContainer.ToggleVis(group);
							}

							GUIUtility.keyboardControl = 0;
						}

						//freeze toggle
						if(GUILayout.Button(icon_freeze, iconButtonStyle))
						{
							GUIUtility.keyboardControl = 0;
							groupContainer.ToggleFreeze(group);
							SceneView.RepaintAll();
						}

						//group name
						if( editingTitle == i )
						{
							group.name = EditorGUILayout.TextField(group.name, textFieldStyle);
						}
						else
						{
							groupTooltip.text = group.name;
							groupTooltip.tooltip = ConcatString(group.objects);
							GUILayout.Label(groupTooltip, labelStyle);
						}

						GUILayout.FlexibleSpace();

						Color c = GUI.color;
						GUI.color = EditorGUIUtility.isProSkin ? Color.gray : LightGray;
						GUILayout.Label(string.Format("({0})", group.objects.Length),labelStyle);
						GUI.color = c;
					}

					EditorGUILayout.EndHorizontal();

					if( !modMode && doExpandGroups && group.objects.Length > 0 )
					{
						GUILayout.BeginVertical();

						GUILayout.Label("Objects in Group", EditorStyles.boldLabel);

						foreach(GameObject go in group.objects)
						{
							GUILayout.BeginHorizontal();
								if(GUILayout.Button(icon_Delete, iconButtonStyle))
								{
									Undo.RecordObject(groupContainer, "Remove from Group");
									group.RemoveObject(go);
									break;
								}

								GUILayout.Label(go.name);

								if(curEvent.type == EventType.MouseDown && GUILayoutUtility.GetLastRect().Contains(curEvent.mousePosition))
								{
									Selection.objects = new UnityEngine.Object[] { go };
									curEvent.Use();
								}

							GUILayout.EndHorizontal();
						}

						GUILayout.EndVertical();
					}

					GUILayout.EndVertical();

					Rect groupRect = GUILayoutUtility.GetLastRect();

					// GUILayoutUtility.GetLastRect()
					if( groupRect.Contains(curEvent.mousePosition) )
					{
						if(dragging)
						{
							dragIndex = i;
							wantsRepaint = true;
						}

						switch(curEvent.type)
						{
							case EventType.DragPerform:
								if(dragging)
								{
									AddToGroup(dragItems, group);
									curEvent.Use();
									dragIndex = -1;
									dragInProgress = false;
									dragImageReady = false;
								}
								break;

							case EventType.MouseDown:

								nextRepaintSetGroup = group;

								editingTitle = -1;

								if( curEvent.clickCount > 1 )
								{
									editingTitle = i;
									curEvent.Use();
								}
								break;
						}
					}
				}

				EditorGUILayout.EndVertical();
				EditorGUILayout.EndScrollView();
			}

			if(dragInProgress)
			{
				if(!dragImageReady && curEvent.type == EventType.Layout)
					dragImageReady = true;

				if(dragImageReady)
				{
					GUILayout.FlexibleSpace();
						GUILayout.BeginHorizontal();
							GUILayout.FlexibleSpace();
								GUILayout.Label(icon_Drag);
							GUILayout.FlexibleSpace();
						GUILayout.EndHorizontal();
					GUILayout.FlexibleSpace();
				}
			}
			else if(groupContainer.sceneGroups == null || groupContainer.sceneGroups.Length < 1)
			{
				GUILayout.FlexibleSpace();
					GUILayout.BeginHorizontal();
						GUILayout.FlexibleSpace();
							GUILayout.Label("Drag GameObjects Here!", EditorStyles.largeLabel);
						GUILayout.FlexibleSpace();
					GUILayout.EndHorizontal();
				GUILayout.FlexibleSpace();
			}

			// if( curEvent.type != EventType.Repaint &&
			// 	curEvent.type != EventType.Layout &&
			// 	curEvent.type != EventType.MouseMove )
			// 	Debug.Log(curEvent.type + " " + curEvent.clickCount);

			if( curEvent.type == EventType.MouseDown ||
				(curEvent.type == EventType.KeyDown &&
					(curEvent.keyCode == KeyCode.Return ||
					curEvent.keyCode == KeyCode.KeypadEnter ||
					curEvent.keyCode == KeyCode.Clear ||
					curEvent.keyCode == KeyCode.Escape ||
					curEvent.keyCode == KeyCode.Tab)
				)
			)
			{
				editingTitle = -1;
				wantsRepaint = true;
			}

			if( curEvent.type == EventType.Ignore ||
				curEvent.type == EventType.DragPerform ||
				curEvent.type == EventType.DragExited ||
				curEvent.type == EventType.Repaint)
			{
				// Dragged into the void.  Create a new group.
				if( dragIndex < 0 && curEvent.type == EventType.DragPerform )
				{
					NewGroupWithObjects( dragItems, dragItems[0].name );
					wantsRepaint = true;
				}

				if(curEvent.type != EventType.Repaint)
				{
					dragInProgress = false;
					dragImageReady = false;
				}

				if(curEvent.type != EventType.Repaint)
					dragIndex = -1;
			}

			if( nextRepaintSetGroup != null && curEvent.type == EventType.Repaint )
				SelectGroup(nextRepaintSetGroup);

			if(wantsRepaint)
				Repaint();
		}

		string ConcatString(GameObject[] array)
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();

			for(int i = 0; i < array.Length - 1; i++)
			{
				if(array[i] != null)
					sb.AppendLine(array[i].name);
			}

			if(array.Length > 0 && array[array.Length-1] != null)
				sb.Append( array[array.Length-1].name );

			return sb.ToString();
		}

		bool ListenForDragAndDrop(Event curEvent, out GameObject[] dragItems)
		{
			dragItems = null;

			if( (curEvent.type == EventType.DragUpdated || curEvent.type == EventType.DragPerform) && DragAndDrop.objectReferences.Length > 0)
			{
				dragItems = (GameObject[]) DragAndDrop.objectReferences.Where(x => x is GameObject).Cast<GameObject>().ToArray();

				if(dragItems != null && dragItems.Length > 0)
					DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
			}

			return dragItems != null && dragItems.Length > 0;
		}

		/**
		 * Set Selection.gameObjects to match @InGroup gameObjects.
		 */
		void SelectGroup(Group InGroup)
		{
			nextRepaintSetGroup = null;
			Selection.objects = InGroup.objects;
		}

		/**
		 * Create a new group with @InObjects and @name.
		 */
		public static void NewGroupWithObjects(GameObject[] InObjects, string name)
		{
			Undo.RecordObject(groupContainer, "New Group from Selection");

			// Uncomment to restrict objects to a single group.
			// GameObject[] cleanObjects = DuplicateCheckObjects(InObjects);
			GameObject[] cleanObjects = InObjects;

			groupContainer.NewGroup(name, cleanObjects);

			// groupContainer.RemoveNullOrEmpty();

			if( ProGroups_Window.instance != null )
				ProGroups_Window.instance.OnSelectionChange();
		}

		/**
		 * Add @InObjectsToAdd to @InGroup, checking for existing entries
		 * in other groups.
		 */
		void AddToGroup(GameObject[] InObjectsToAdd, Group InGroup)
		{
			Undo.RecordObject(groupContainer, "Add objects to group");

			InGroup.AddObjects( InObjectsToAdd );

			foreach(GameObject go in InObjectsToAdd)
			{
				if(InGroup.frozen)
					go.Freeze();
				else
					go.Thaw();

				if(InGroup.hidden)
					go.Hide();
				else
					go.Show();
			}

			// Uncomment to restrict objects to a single group.
			// InGroup.AddObjects( DuplicateCheckObjects(InObjectsToAdd, InGroup) );
			// groupContainer.RemoveNullOrEmpty();

			OnSelectionChange();
		}

		/**
		 * Checks for existing entries in the GroupContainer arrays, and prompts user
		 * to either remove them from the existing group or removes them from the
		 * current selection.
		 * @GroupMask is an optional parameter that tells the duplicate check to ignore
		 * that particular group.
		 */
		static GameObject[] DuplicateCheckObjects(GameObject[] InObjects)
		{
			return DuplicateCheckObjects(InObjects, null);
		}

		static GameObject[] DuplicateCheckObjects(GameObject[] InObjects, Group InGroupMask)
		{
			List<GameObject> appendObjects = new List<GameObject>(InObjects);

			// check for entries in existing groups
			int removeFromExistingGroup = -1;

			for(int i = 0; i < groupContainer.sceneGroups.Length; i++)
			{
				Group group = groupContainer.sceneGroups[i];

				if( group == InGroupMask )
					continue;

				GameObject[] dup = group.objects.Intersect(appendObjects).ToArray();

				if( dup != null && dup.Length > 0 )
				{
					foreach(GameObject duplicate in dup)
					{
						if( removeFromExistingGroup != 2 )
							removeFromExistingGroup = EditorUtility.DisplayDialogComplex("Remove from current group?", duplicate.name + " already belongs to a group.  Would you like to remove it from it's current groups and place it in the new one?", "Yes", "No", "Yes to All");

						switch(removeFromExistingGroup)
						{
							case 0:	// yes
							case 2:	// yes to all
								group.RemoveObject(duplicate);
								break;

							case 1:	// no
								appendObjects.Remove(duplicate);
								break;

							default:
								// ehu?
								break;
						}
					}
				}
			}

			return appendObjects.ToArray();
		}
	}
}
