using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unigine;

[Component(PropertyGuid = "673fd8f31ad8c9b6ac95b59d62baba58d8c179d7")]
public class SerializeUnit : Component
{
	//SerializeHandler handler;
	StaticItemSerializer serializer;

	private void Init()
	{
		// handler = UnigineApp.AppSystemLogic
		// 	.GetSerializer()
		// 	.ExternanInit();

		serializer = UnigineApp.AppSystemLogic
			.GetStaticSerializer()
			.ExternanInit();
	}

	public SerializeUnit ExternalInit() {
		this.Init();
		return this;
	}

	/// <summary>
	/// Вызывает сериализацию состояния текущего игрового мира с укзанным именем стадии. Возникшие исключения вывалятся в консоль
	/// </summary>
	/// <param name="stage"></param>
	/// <param name="rewrite"></param>
	public void UnsafeSaveStage(string stage, bool rewrite = false)
	{
		if (serializer != null) serializer.UnsafeSaveStage(stage, rewrite);
	}

	/// <summary>
	/// Выполняет попытку записать указанную стадию с установленным флагом. Выполнит запись только в случае если записи в данных нет или установлен флаг перезаписи
	/// </summary>
	/// <param name="stage"></param>
	/// <param name="rewrite"></param>
	public void TrySaveStage(string stage, bool rewrite = false) 
	{
		if (serializer != null) 
		{
			SerializeHandler.SERIAL_RESULT result = serializer.TrySaveStage(stage, rewrite);

			if (result != SerializeHandler.SERIAL_RESULT.SAVE_STAGE_COMPLETE) {
				Log.Message("Stage {0} serialization is fail\n", stage);

				switch (result)
				{
					case SerializeHandler.SERIAL_RESULT.SAVE_ERROR_SAVE_EXIST:
						Log.Message("Serialization is already exist\n", stage);
						break;
					case SerializeHandler.SERIAL_RESULT.SAVE_ERROR_EXCEPTION:
						Log.Message("Serialization process an exception capture\n");
						break;
				}
			}
		}
	}

	
	/// <summary>
	/// Загрузка данных из ранее сохраненного состояния по указанному названию стадии
	/// </summary>
	/// <param name="stage"></param>
	public void UnsafeLoadStage(string stage)
	{
		if (serializer != null) serializer.UnsafeLoadStage(stage);
	}

	/// <summary>
	/// Загрузка данных из ранее сохраненного состояния по указанному названию стадии. Исключения будут выведены в консоль
	/// </summary>
	/// <param name="stage"></param>
	public void TryLoadStage(string stage)
	{
		if (serializer != null) 
		{
			SerializeHandler.SERIAL_RESULT result = serializer.TryLoadStage(stage);

			if (result != SerializeHandler.SERIAL_RESULT.LOAD_STAGE_COMPLETE) {
				Log.Message("Stage {0} deserialization is fail\n", stage);

				switch (result)
				{
					case SerializeHandler.SERIAL_RESULT.LOAD_ERROR_NOT_EXIST:
						Log.Message("Deserialization is not exist\n", stage);
						break;
					case SerializeHandler.SERIAL_RESULT.LOAD_ERROR_EXCEPTION:
						Log.Message("Deserialization process an exception capture\n");
						break;
				}
			}
		}
	}
}