using System;
using System.Collections;
using System.Collections.Generic;
using Unigine;

/// <summary>
/// Сериалайзер-наследник от SerialHandler.cs, работающий статически из AppSystemLogic.cs, сохраняющий только элементы отмеченные SerializeItem.cs или его наследниками
/// </summary>
[Component(PropertyGuid = "773070c299c3f93977405144dab82bd300da3f16")]
public class StaticItemSerializer : SerializeHandler
{
	private void Init()
	{

	}
	
	private void Update()
	{
		// write here code to be called before updating each render frame
		
	}
}