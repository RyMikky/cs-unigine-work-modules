using System;
using System.Collections;
using System.Collections.Generic;
using Unigine;

/// <summary>
/// Включает/отключает ActionManager.cs и ноду, к которой прикреплен
/// </summary>
[Component(PropertyGuid = "e8565dd79c34614a8a156021270afb0c1c307afb")]
public class DisablerToActionManager : Component, ISDisabler
{
	ActionManager script;

	/// <summary>
	/// Пытается получить скрипт управления кнопкой и возвращает флаг успешности
	/// </summary>
	/// <returns></returns>
	private bool TryLoadResource()
	{
		if (node != null && script == null) script = node.GetComponent<ActionManager>();
		return script != null ? true : false;
	}

	public void DisableWorldElement()
	{
		if (TryLoadResource()) 
		{
			node.Enabled = false;
		}
	}

	public void EnableWorldElement()
	{
		if (TryLoadResource())  
		{
			node.Enabled = true;
		}
	}
}