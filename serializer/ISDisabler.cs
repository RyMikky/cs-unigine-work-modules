using System;
using System.Collections;
using System.Collections.Generic;
using Unigine;

/// <summary>
/// Интерфейс для создания компонентов отключающих элементы в модели игрового мира
/// </summary>
public interface ISDisabler
{
	/// <summary>
	/// Отключает выбранный элемент игрового мира
	/// </summary>
	public void DisableWorldElement();
	/// <summary>
	/// Подключает выбранный элемент игрового мира
	/// </summary>
	public void EnableWorldElement();
}