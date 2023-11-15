using System;
using System.Collections;
using System.Collections.Generic;
using Unigine;

/// <summary>
/// Структура хранящая в себе данные по названиям и путям к файлам сохраненных стадий для конкретного игрового мира
/// </summary>
public struct SWorldDataStruct
{
	
	private string worldDataListFileName;                             // назвние файла с сохраненным листом стадий
	private string worldDataListFilePath;                             // путь к файлу с сохраненным листом стадий

	public string GetWorldDataListFileName() { return worldDataListFileName; }
	public SWorldDataStruct SetWorldDataListFileName(string fileName)
	{
		worldDataListFileName = fileName;
		return this;
	} 
	public string GetWorldDataListFilePath() { return worldDataListFilePath; }
	public SWorldDataStruct SetWorldDataListFilePath(string filePath)
	{
		worldDataListFilePath = filePath;
		return this;
	} 
	
	private Dictionary<string, string> saveStagePaths;                // хеш, сохраняющий названия стадий и пути к файлам сохранения стадий
	private Dictionary<string, string> savePathStages;                // обратный хеш, сохраняющий пути к файлам сохранения и название стадий 

	public Dictionary<string, string> GetStageToPathDictonary() { return saveStagePaths; }
	public SWorldDataStruct SetStageToPathDictonary(Dictionary<string, string> newData) 
	{
		saveStagePaths = newData;
		return this;
	}
	public Dictionary<string, string> GetPathToStageDictonary() { return savePathStages; }
	public SWorldDataStruct SetPathToStageDictonary(Dictionary<string, string> newData) 
	{
		savePathStages = newData;
		return this;
	}

	/// <summary>
	/// Конструктор без параметров НЕ рекомендуется!
	/// </summary>
	public SWorldDataStruct() 
	{
		saveStagePaths = new Dictionary<string, string>();
		savePathStages = new Dictionary<string, string>();
		worldDataListFileName = "";
		worldDataListFilePath = "";
	}

	public SWorldDataStruct(string filePath) 
	{
		saveStagePaths = new Dictionary<string, string>();
		savePathStages = new Dictionary<string, string>();
		worldDataListFileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
		worldDataListFilePath = filePath;
	}

	public SWorldDataStruct(string fileName, string filePath) 
	{
		saveStagePaths = new Dictionary<string, string>();
		savePathStages = new Dictionary<string, string>();
		worldDataListFileName = fileName;
		worldDataListFilePath = filePath;
	}

	public SWorldDataStruct(SWorldDataStruct other) 
	{
		saveStagePaths = other.saveStagePaths;
		savePathStages = other.savePathStages;
		worldDataListFileName = other.worldDataListFileName;
		worldDataListFilePath = other.worldDataListFilePath;
	}

	/// <summary>
	/// Сохраняет данные по стадии
	/// </summary>
	/// <param name="stage"></param>
	/// <param name="path"></param>
	/// <returns></returns>
	public SWorldDataStruct AddData(string stage, string path)
	{
		if (saveStagePaths.ContainsKey(stage)) saveStagePaths.Remove(stage);
		if (savePathStages.ContainsKey(path)) savePathStages.Remove(path);

		saveStagePaths.Add(stage, path);
		savePathStages.Add(path, stage);
		
		return this;
	}

	/// <summary>
	/// Сверяет переданное имя файла с имеющимся в экземпляре данных
	/// </summary>
	/// <param name="fileName"></param>
	/// <returns></returns>
	public bool CheckWorldDataListFileName(string fileName) { return fileName == worldDataListFileName; }

	/// <summary>
	/// Сверяет переданный путь к файлу с имеющимся в экземпляре
	/// </summary>
	/// <param name="filePath"></param>
	/// <returns></returns>
	public bool CheckWorldDataListFilePath(string filePath) { return filePath == worldDataListFilePath; }

	/// <summary>
	/// Возвращает флаг того, что есть упоминания о списке сохраненных данных
	/// </summary>
	/// <returns></returns>
	public bool CheckWorldDataList() { return worldDataListFileName != "" && worldDataListFilePath != ""; }

	/// <summary>
	/// Проверка наличия записи по указанной стадии
	/// </summary>
	/// <param name="stage"></param>
	/// <returns></returns>
	public bool CheckDataByStage(string stage) { return saveStagePaths.ContainsKey(stage); }
	
	/// <summary>
	/// Проверка наличия записи по заданному пути к файлу
	/// </summary>
	/// <param name="path"></param>
	/// <returns></returns>
	public bool CheckDataByPath(string path) { return savePathStages.ContainsKey(path); }

	/// <summary>
	/// Возвращает путь к сохраненному стейжу, если такой имеется, иначе возвращает пустую строку
	/// </summary>
	/// <param name="stage"></param>
	/// <returns></returns>
	public string GetPathByStage(string stage) 
	{ 
		string result = "";

		if (CheckDataByStage(stage))
		{
			saveStagePaths.TryGetValue(stage, out result);
		}

		return result;
	}

	/// <summary>
	/// Возвращает название стейжа по пути, если такой имеется, иначе возвращает пустую строку
	/// </summary>
	/// <param name="path"></param>
	/// <returns></returns>
	public string GetStageByPath(string path) 
	{ 
		string result = "";

		if (CheckDataByPath(path))
		{
			savePathStages.TryGetValue(path, out result);
		}

		return result;
	}

	/// <summary>
	/// Удаляет данные по имени стейжа, или возвращает false
	/// </summary>
	/// <param name="stage"></param>
	/// <returns></returns>
	public bool RemoveDataByStage(string stage) 
	{ 	
		bool result = false;

		if (CheckDataByStage(stage) && saveStagePaths.Remove(stage, out string path))
		{
			result = savePathStages.Remove(path);
		}

		return result;
	}

	/// <summary>
	/// Удаляет данные по пути к файлу или возвращает false
	/// </summary>
	/// <param name="path"></param>
	/// <returns></returns>
	public bool RemoveDataByPath(string path) 
	{
		bool result = false;

		if (CheckDataByPath(path) && savePathStages.Remove(path, out string stage))
		{
			result = saveStagePaths.Remove(stage);
		}

		return result;
	}

}