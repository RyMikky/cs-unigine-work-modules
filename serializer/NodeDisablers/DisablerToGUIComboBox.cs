using System;
using System.Collections;
using System.Collections.Generic;
using Unigine;

/// <summary>
/// Даёт доступ к включению/отключению комбобокса
/// </summary>
[Component(PropertyGuid = "fc6579c2600b6e396e7f424405f068d390035816")]
public class DisablerToGUIComboBox : Component, ISDisabler
{
	Combobox script;

	/// <summary>
	/// Пытается получить скрипт управления комбобоксом и возвращает флаг успешности
	/// </summary>
	/// <returns></returns>
	private bool TryLoadResource()
	{
		if (node != null && script == null) script = node.GetComponent<Combobox>();
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