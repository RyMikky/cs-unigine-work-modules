using System;
using System.Collections;
using System.Collections.Generic;
using Unigine;

/// <summary>
/// Даёт доступ к включению/отключению абстрактной кнопки и её наследников
/// </summary>
[Component(PropertyGuid = "4e2a664c16c46d3b52b1015e182579253937d840")]
public class DisablerToAbstractButton : Component, ISDisabler
{
	AbstractButton script;

	/// <summary>
	/// Пытается получить скрипт управления кнопкой и возвращает флаг успешности
	/// </summary>
	/// <returns></returns>
	private bool TryLoadResource()
	{
		if (node != null && script == null) script = node.GetComponent<AbstractButton>();
		return script != null ? true : false;
	}

	public void DisableWorldElement()
	{
		if (TryLoadResource()) script.SetEnable(false);
	}

	public void EnableWorldElement()
	{
		if (TryLoadResource()) script.SetEnable(true);
	}
}