using System;
using System.Collections;
using System.Collections.Generic;
using Unigine;

/// <summary>
/// Базовый класс-индикатор доступности и необходимости сериализации. Содержит базовые поля ноды к которой привязан.
/// НЕИЗСПОЛЬЗОВАТЬ САМОСТОЯТЕЛЬНО!
/// </summary>
[Component(PropertyGuid = "df976ac05867df1fe6a2a1f28fd998a925d009f4")]
public class SerializeBaseItem : Component
{
	/// <summary>
	/// Тип сериализуемого объекта, устанавливается автоматически при создании экземляра класса или наследника
	/// </summary>
	public enum SERIAL_OBJECT_TYPE {
		NO_SERIAL_OBJECT,
		STAGE_DESCRIPTION,
		SERIAL_HANDLER,
		ACTION_HANDLER,
		GUI_TEXT_FIELD, 
		GUI_IMAGE_WIDGET,
		OBJECT_NODE, 
		GUI_STEP_BUTTON_SETTINGS,
		GUI_LIST_BOX_SETTINGS,

		COMMON_OBJECT, 
		CAMERA_OBJECT, 
		ANIMATION, 
		OBJECT_DUMMY, 
		ACTION_BUTTON, 
		GUI_BOX_LIST
	}

	protected SERIAL_OBJECT_TYPE _type = SERIAL_OBJECT_TYPE.NO_SERIAL_OBJECT;

	static readonly Dictionary<SERIAL_OBJECT_TYPE, string> SERIAL_OBJECT_TYPE_STRING = new Dictionary<SERIAL_OBJECT_TYPE, string>()
	{
		{SERIAL_OBJECT_TYPE.NO_SERIAL_OBJECT, "NO_SERIALIZE_OBJECT"},
		{SERIAL_OBJECT_TYPE.STAGE_DESCRIPTION, "STAGE_DESCRIPTION"},
		{SERIAL_OBJECT_TYPE.SERIAL_HANDLER, "SERIAL_HANDLER"},
		{SERIAL_OBJECT_TYPE.ACTION_HANDLER, "ACTION_HANDLER"},
		{SERIAL_OBJECT_TYPE.GUI_TEXT_FIELD, "GUI_TEXT_FIELD"},
		{SERIAL_OBJECT_TYPE.GUI_IMAGE_WIDGET, "GUI_IMAGE_WIDGET"},
		{SERIAL_OBJECT_TYPE.OBJECT_NODE, "OBJECT_NODE"},
		{SERIAL_OBJECT_TYPE.GUI_STEP_BUTTON_SETTINGS, "GUI_STEP_BUTTON_SETTINGS"},
		{SERIAL_OBJECT_TYPE.GUI_LIST_BOX_SETTINGS, "GUI_LIST_BOX_SETTINGS"}
	};

	static readonly protected string DATA_BEGIN_SUFFIX = ".BEGIN"; 
	static readonly protected string DATA_END_SUFFIX = ".END"; 

	/// <summary>
	/// Возвращает тип текущего экземпляра класса
	/// </summary>
	/// <returns></returns>
	public SERIAL_OBJECT_TYPE GetSerialItemType() { return _type; }

	/// <summary>
	/// Возвращает комбинированную строку содержащею название типа + переданный суффикс. Не должна выбрасывать исключение. Если выкинула - в проге нарушенны данные.
	/// </summary>
	/// <param name="suffix"></param>
	/// <returns></returns>
	/// <exception cref="System.Exception"></exception>
	private string GetCombinedTitleLabel(string suffix)
	{
		if (SERIAL_OBJECT_TYPE_STRING.TryGetValue(_type, out string type_label)) 
		{
			return type_label + suffix;
		}
		else 
		{
			throw new System.Exception("Unknow type or static dada is corrupt");
		}	
	}

	/// <summary>
	/// Записывает в поток метку объекта с приложенным суффиксом
	/// </summary>
	/// <param name="fileSource"></param>
	/// <param name="suffix"></param>
	public void WriteTypeLabel(Unigine.File fileSource, string suffix) 
	{
		fileSource.WriteString(GetCombinedTitleLabel(suffix));
	}

	/// <summary>
	/// Записывает в поток булевой флаг что тело записи отсутствет
	/// </summary>
	/// <param name="fileSource"></param>
	public void WriteNullBodyFlag(Unigine.File fileSource) 
	{
		WriteBodyFlag(fileSource, false);
	}

	/// <summary>
	/// Записывает в поток булевой флаг наличия тела записи
	/// </summary>
	/// <param name="fileSource"></param>
	/// <param name="flag"></param>
	public void WriteBodyFlag(Unigine.File fileSource, bool flag)
	{
		fileSource.WriteBool(flag);
	}

	/// <summary>
	/// Берет из предложенного потока строку и сверяет по указанному суффиксу
	/// </summary>
	/// <param name="fileSource"></param>
	/// <param name="suffix"></param>
	/// <returns></returns>
	public bool ReadTypeLabel(Unigine.File fileSource, string suffix) 
	{
		// string from_stream = fileSource.ReadString();
		// string combine = GetCombinedTitleLabel(suffix); 
		// return from_stream == combine ? true : false;
		return fileSource.ReadString() == GetCombinedTitleLabel(suffix) ? true : false;
	}

	/// <summary>
	/// Берет из предложенного потока строку и сверяет по указанному суффиксу, также возвращает значение полученной записи
	/// </summary>
	/// <param name="fileSource"></param>
	/// <param name="suffix"></param>
	/// <param name="title"></param>
	/// <returns></returns>
	public bool ReadTypeLabel(Unigine.File fileSource, string suffix, out string title) 
	{
		title = fileSource.ReadString();
		return title == GetCombinedTitleLabel(suffix) ? true : false;
	} 

	/// <summary>
	/// Читает и возвращает булевой флаг наличия тела записи
	/// </summary>
	/// <param name="fileSource"></param>
	/// <returns></returns>
	public bool ReadBodyFlag(Unigine.File fileSource) 
	{
		return fileSource.ReadBool();
	}

	/// <summary>
	/// Базовый вызов сериализации ноды
	/// </summary>
	/// <param name="fileSource"></param>
	public virtual void SerializeNodeData(Unigine.File fileSource) 
	{
		WriteTypeLabel(fileSource, DATA_BEGIN_SUFFIX);
		WriteNullBodyFlag(fileSource);
		WriteTypeLabel(fileSource, DATA_END_SUFFIX);
	}
	
	/// <summary>
	/// Базовый вызов восстановления данных
	/// </summary>
	/// <param name="fileSource"></param>
	public virtual void RestoreData(Unigine.File fileSource) 
	{
		if (ReadTypeLabel(fileSource, DATA_BEGIN_SUFFIX) 
			&& !ReadBodyFlag(fileSource) && ReadTypeLabel(fileSource, DATA_END_SUFFIX)) return;

		throw new System.Exception("Restore data from stream is corrupt");
	}

	// ------------------------------------------- блок булевых проверок базового класса ------------------------------------------------

	/// <summary>
	/// Флаг отрицания IsNode()
	/// </summary>
	/// <returns></returns>
	public bool IsEmpty() 
	{
		return !IsNode();
	}

	/// <summary>
	/// Возвращает флаг того, что скрипт привязан к Unigine.Node
	/// </summary>
	/// <returns></returns>
	public bool IsNode()
	{
		return node != null;
	}

	/// <summary>
	/// Возвращает флаг того, что скрипт привязан к Unigine.Node.Object
	/// </summary>
	/// <returns></returns>
	public bool IsObject()
	{
		return IsNode() && node.IsObject;
	}

	/// <summary>
	/// Возвращает флаг того, что объект имеет наследников
	/// </summary>
	/// <returns></returns>
	public bool IsParent() 
	{
		return IsNode() && node.NumChildren > 0;
	}

	/// <summary>
	/// Возвращает флаг того, что объект имеет родителя
	/// </summary>
	/// <returns></returns>
	public bool IsChild() 
	{
		return IsNode() && node.Parent != null;
	}
}