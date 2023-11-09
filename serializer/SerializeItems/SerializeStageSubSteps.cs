using System;
using System.Collections;
using System.Collections.Generic;
using Unigine;

/// <summary>
/// Компонент отвечающий за формирование подшагов стадии
/// </summary>
[Component(PropertyGuid = "9a33c7932fefa7d68c5fbd3dfda6dd783ade0277")]
public class SerializeStageSubSteps : Component
{

	[ShowInEditor][Parameter(Tooltip = "Индекс основной стадии в SerializeStageList.cs")]
	private int stageIndex;
	[ShowInEditor][Parameter(Tooltip = "Название основной стадии в SerializeStageList.cs")]
	private string stageName;

	[ShowInEditor][Parameter(Tooltip = "Названия подшагов основной стадии")]
	private string[] stageSteps;

	private void Init()
	{
		// write here code to be called on component initialization
		
	}
	
	private void Update()
	{
		// write here code to be called before updating each render frame
		
	}
}