using System;
using System.Collections;
using System.Collections.Generic;
using Unigine;

/// <summary>
/// Сериализует название стадии и её подстадии, если они указаны в редакторе
/// </summary>
[Component(PropertyGuid = "c536b84e0d43635575bbdcb09e17484f9a241962")]
public class SerializeStageDesricpion : SerializeBaseItem
{

	public enum MODE
	{
		DIRECT, HANDLER, NONE
	}

	[ShowInEditor][Parameter(Tooltip = "Режим сериализации. DIRECT - самостоятельно, HANDLER - в обработчике")]
	private MODE mode = MODE.HANDLER;

	private bool IsLoad = false;               // флаг уже загруженных настроек
	private bool IsSaved = false;              // флаг сохраненных настроек

	[ShowInEditor][Parameter(Tooltip = "Название стадии работы проекта")]
	private string stageName;
	[ShowInEditor][Parameter(Tooltip = "Названия шагов на стадии работы проекта")]
	private string[] stageStepNames;

	/// <summary>
	/// Проверяет, что переданый индекс валиден
	/// </summary>
	/// <param name="index"></param>
	/// <returns></returns>
	private bool IsValidIndex(int index) { return index >=0 && index < GetStageStepsCount(); }

	/// <summary>
	/// Возвращает название стадии
	/// </summary>
	/// <returns></returns>
	public string GetStageName() { return stageName; }

	/// <summary>
	/// Возвращает количество шагов в стадии
	/// </summary>
	/// <returns></returns>
	public int GetStageStepsCount() { return stageStepNames.Length; }

	/// <summary>
	/// Возвращает название стадии по индексу
	/// </summary>
	/// <param name="index"></param>
	/// <returns></returns>
	public string GetStageStep(int index)
	{
		if (IsValidIndex(index)) 
		{
			return stageStepNames[index];
		}
		else 
		{
			throw new System.Exception("GetStageStep(int index::index is out of range)");
		}
	}

	/// <summary>
	/// Непосредственная имплементация сериализации. Вынесен отдельно чтобы запускать из-под обработчика
	/// </summary>
	/// <param name="fileSource"></param>
	public void SerializeDescription(Unigine.File fileSource)
	{
		if (!IsSaved) 
		{
			WriteTypeLabel(fileSource, DATA_BEGIN_SUFFIX);

			fileSource.WriteString(stageName);                                        // записываем название стадии
			int stageStepsCount = stageStepNames.Length;
			fileSource.WriteInt(stageStepsCount);                                     // записываем количество шагов на стадии

			for (int i = 0; i != stageStepsCount; ++i) 
			{
				fileSource.WriteString(stageStepNames[i]);                            // записываем названия шагов на стадии
			}

			WriteTypeLabel(fileSource, DATA_END_SUFFIX);
			IsSaved = true;
		}
	}

	/// <summary>
	/// Общий метод сериализации. В данном классе он смотрит режим, и если режим DIRECT то запусает процесс
	/// </summary>
	/// <param name="fileSource"></param>
	public override void SerializeNodeData(Unigine.File fileSource)
	{
		switch (mode)
		{
			case MODE.DIRECT:
				SerializeDescription(fileSource);
				break;
		}
	}

	/// <summary>
	/// Непосредственная имплементация восстановления данных. Вынесен отдельно чтобы запускать из-под обработчика
	/// </summary>
	/// <param name="fileSource"></param>
	/// <exception cref="System.Exception"></exception>
	public void RestoreDescription(Unigine.File fileSource) 
	{
		try 
		{
			if (!IsLoad) 
			{
				// считывание объекта начнется в том случае если корректно считывается хеддер и флаг тела объекта
				if (ReadTypeLabel(fileSource, DATA_BEGIN_SUFFIX)) 
				{
					stageName = fileSource.ReadString();                              // получаем название стадии из потока
					int stageStepsCount = fileSource.ReadInt();                       // получаем количество шагов на стадии

					stageStepNames = new string[stageStepsCount];                     // пересоздаём массив с наименованиями шагов
					for (int i = 0; i != stageStepsCount; ++i) 
					{
						stageStepNames.SetValue(fileSource.ReadString(), i);          // записываем названия шагов на стадии
					}
		
					if (!ReadTypeLabel(fileSource, DATA_END_SUFFIX)) throw new System.Exception("Node END_label reading is corrupt");
				}

				IsLoad = true;
			}
		}
		catch (System.Exception e)
		{
			throw new System.Exception(e.Message);
		}
	}

	/// <summary>
	/// Общий метод восстановления. В данном классе он смотрит режим, и если режим DIRECT то запусает восстановление
	/// </summary>
	/// <param name="fileSource"></param>
	public override void RestoreData(Unigine.File fileSource) 
	{
		switch (mode)
		{
			case MODE.DIRECT:
				RestoreDescription(fileSource);
				break;
		}
	}
}