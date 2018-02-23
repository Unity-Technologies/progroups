# ** IMPORTANT **

ProGroups is deprecated and no longer under development- use at your own risk! These files are made available as-is with zero guarantees or promises of support.

In time, we (World Building team) do hope to re-create similar functionality in Unity, scene organization is important to us! 

---

# Overview

ProGroups allows you to organize objects in your scene into “selection sets”, **without modifying the objects or hierarchy**. Then, use the interface to **hide, freeze, and select** entire groups at once. 

These features help keep your large, complex scenes nicely organized and easy to work with.

# Installation and Setup

1. Open Unity
1. Download ProGroups (Click the green "Clone or Download" button or use this [Direct Link](https://github.com/Unity-Technologies/progroups/archive/master.zip))
1. Unzip and place the ProGroups files into Assets folder of your Unity project

**Or, via command line:**
```
cd YourProject/Assets
git clone https://github.com/Unity-Technologies/progroups.git ProGroups
```

# Getting Started with ProGroups

ProGroups works differently than simple hierarchy organization. ProGroups does not alter or modify your objects or hierarchy in any way. This allows you to have any object in multiple Groups, and to keep your hierarchy clean and efficient.

1. From the top menu, choose `Tools  > ProGroups > ProGroups Window`
1. The ProGroups window will now appear as a dock-able panel. You can change this by right clicking (Option Click on Mac) anywhere in the ProGroups window and selecting the Window menu.
1. The ProGroups window has two modes: **Normal** and **Modify**.  Normal mode is where you will typically stay. You can create new groups, view and edit group names, toggle visibility and frozen status, and select all objects in each group.
1. Modify mode lets you change group names, remove groups, and re-order your groups.

# The ProGroups GUI

## Normal Mode

![normal mode](http://www.procore3d.com/docs/progroups/progroups_MainPanel.jpg)

1. Before creating a new group, you can specify a group name here.
1. Click the plus icon to create a new group from the current selection.
1. Click the gear icon to modify the order or delete groups.
1. Click the pointer icon to select all items in this group.
1. Click the eye icon to hide or show objects in this group.  Hold `Alt` to hide all other groups (isolate).
1. Click the snowflake icon to freeze or un-freeze the current selection.
	- Freezing a group means that the objects may not be edited in any way.
1. Toggle the drop-down of child items in this group.
1. The group name.  Double-click to edit this value, or single click to select objects in this group.  The number of items currently in this group will be displayed to the far right.

When in **Normal** mode, you may drag objects from the Hierarchy window to create new groups.  Dragging objects over existing group entries will add those objects to that group.  Dragging objects into an empty space of the window will create a new group.

As you're working ProGroups will highlight groups that contain objects that are in your current selection.

## Modify Mode

![modify mode](http://www.procore3d.com/docs/progroups/progroups_ModifyPanel.jpg)

**Modify** mode makes it easy to edit many groups simultaneously.

1. Exit Modify mode
1. Move the group up one level in the list.
1. Move the group down one in the list.
1. Remove the Group
	- All objects will be un-hidden and un-frozen.
	- No objects will be deleted!
1. Text field for renaming the Group

---

