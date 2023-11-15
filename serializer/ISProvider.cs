using System;
using System.Collections;
using System.Collections.Generic;
using Unigine;

/// <summary>
/// Интерфейс провайдера сериализации
/// </summary>
public interface ISProvider
{
	/// <summary>
	/// Инициализация сериализатора по месту требования
	/// </summary>
	public void SerializerInit();

	/// <summary>
	/// Вызывает сериализацию состояния текущего игрового мира с укзанным именем стадии. Возникшие исключения вывалятся в консоль
	/// </summary>
	/// <param name="stage"></param>
	/// <param name="rewrite"></param>
	public void UnsafeSaveStage(string stage, bool rewrite = false);

	/// <summary>
	/// Выполняет попытку записать указанную стадию с установленным флагом. Выполнит запись только в случае если записи в данных нет или установлен флаг перезаписи
	/// </summary>
	/// <param name="stage"></param>
	/// <param name="rewrite"></param>
	public void TrySaveStage(string stage, bool rewrite = false);

	/// <summary>
	/// Загрузка данных из ранее сохраненного состояния по указанному названию стадии
	/// </summary>
	/// <param name="stage"></param>
	public void UnsafeLoadStage(string stage);

	/// <summary>
	/// Загрузка данных из ранее сохраненного состояния по указанному названию стадии. Исключения будут выведены в консоль
	/// </summary>
	/// <param name="stage"></param>
	public void TryLoadStage(string stage);
}