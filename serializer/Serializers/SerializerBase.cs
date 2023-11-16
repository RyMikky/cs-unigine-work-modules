using System.Collections.Generic;
using Unigine;
using System.IO;
using System.Text.RegularExpressions;
using System;

/// <summary>
/// Основной класс обеспечивающий сериализацию данных. 
/// Может работать как самостоятельный модуль для одного игрового мира, так и как общий модуль определенный статически в AppSystemLogic.cs
/// </summary>
[Component(PropertyGuid = "5dfaf545f3a3e5e05c38dfbf6e1f6a12b40c8249")]
public class SerializerBase : Component
{
	protected static readonly string STREAM_MODE_READ_ONLY = "r";
	protected static readonly string STREAM_MODE_READ_BINARY = "rb";
	protected static readonly string STREAM_MODE_WRITE = "w";
	protected static readonly string STREAM_MODE_WRITE_BINARY = "wb";
	protected static readonly string STREAM_MODE_APPEND = "a";
	protected static readonly string STREAM_MODE_APPEND_BINARY = "ab";

	protected static readonly string DATA_FOLDER = "../data/";
	protected static readonly string BINARY_FOLDER = "../bin/";

	public enum ROOT {
		DATA, BINARY
	}

	protected static readonly string SHUTDOWN_SUFFIX = "_sd_backup";                // суффикс файла сохранения при выклчюении
	protected static readonly string DEFAULT_SUFFIX = "_ds_backup";                 // суффикс сохраненных базовых настроек мира
	protected static readonly string REGULAR_SUFFIX = "_rt_backup";                 // обычный суффикс файла сохранения по времени
	protected static readonly string PATH_LIST_SUFFIX = "_pl_backup";               // суффикс для сохранения листа с данными
	protected static readonly string STAGE_SUFFIX = "_st_backup";                   // суффикс для сохранения стадии

	static readonly string DATA_STAMP = @"[0-9]{4}-[0-9]{2}-[0-9]{2}-[0-9]{2}-[0-9]{2}-[0-9]{2}";     // штамп для времени
	static readonly string META_STAMP = @"meta";

	/// <summary>
	/// Флаг первичного запуска, выбор режима чтения записи
	/// </summary>
	public enum MODE 
	{
		WRITE_NEW_DATA,                     // режим запуска "с чистого листа"
		READ_SHDN_DATA                      // запуск с чтением информации с прошлого выключения
	}

	/// <summary>
	/// Статус потока чтения/сохранения данных
	/// </summary>
	public enum STATUS {                    // статус выбранного файла сохранения
		OPEN, CLOSE, ERROR
	}

	/// <summary>
	/// Тип файла в потоке, зависит от его состояния
	/// </summary>
	public enum TYPE {                      // тип выбранного файла сохранения
		NONE, ERROR, SHDN_FILE, PATH_LIST, REGULAR, DEFAULT, STAGE
	}

	[ShowInEditor][Parameter(Tooltip = "Базовый режим работы модуля сериализации")]
	protected MODE mode = MODE.WRITE_NEW_DATA;

	[ShowInEditor][Parameter(Tooltip = "Корневая папка, в которую будет происходить сохранение")]
	protected ROOT folderRoot = ROOT.DATA;

	[ShowInEditor][Parameter(Tooltip = "Путь к папке сохранений")]
	protected string saveStateFolder = "serialization";

	[ShowInEditor][Parameter(Tooltip = "Включение использвоания в имени записи времени и даты")]
	protected bool useTimeStamp = false;

	[ShowInEditor][Parameter(Tooltip = "Включение сохранения при первичной загрузке")]
	protected bool saveDefaultDataState = false;

	[ShowInEditor][Parameter(Tooltip = "Включение сохранения при завершении работы программы")]
	protected bool saveDataOnShutdown = false;

	// -------------------------- блок внутренних полей класса ---------------------------------

	protected Regex shutdownSaveRegExp;                             // регулярка для формирования и поиска сейвов по выключению
	protected Regex defaultStateRegExp;                             // регулярка для формирования базового сейва
	protected Regex regularSaveRegExp;                              // общая регулярка сохранения по времени
	protected Regex pathListSaveRegExp;                             // регулярка для сохранения листа сейвов
	protected Regex stageStateRegExp;                               // регулярка для сохранения стейжа
	protected Regex fileExtMetaException;                           // регулярка исключения ".meta" (для файлов unigine)
	
	protected string[] initFolderFiles;                             // массив под хранение путей к файлам в рабочей папке

	protected string worldName;                                     // название текущего игрового мира
	protected FileInfo fileInfo;                                    // заглушка для проверки наличия файла
	protected string filePath;                                      // путь для работы файлового потока
	protected Unigine.File fileSource = new Unigine.File();         // главный поток сохранения/загрузки
	protected STATUS fileStatus = STATUS.CLOSE;                     // статус потока сохранения/загрузки
	protected TYPE fileType = TYPE.NONE;                            // тип файла-потока сохранения/загрузки
	protected string fileMode = "";                                 // режим открытия файла-потока
                        
	protected List<Node> worldRootNodes = new List<Node>();         // список всех корневых нод загруженного мира

	private Dictionary<string, SWorldDataStruct> saveData = new Dictionary<string, SWorldDataStruct>();

	// ------------------------------------------- блок булевых проверок базового класса ------------------------------------------------

	/// <summary>
	/// Возвращает флаг состояния потока, что он открыт или закрыт
	/// </summary>
	/// <returns></returns>
	protected bool FileSourceIsOpen() { return fileStatus == STATUS.OPEN ? true : false;}

	// ------------------------------------------- блок методов инициализации класса ----------------------------------------------------

	/// <summary>
	/// Инициализирует файл с информацией о раннее сохраненных при выключении данных
	/// </summary>
	protected void InitShutDownData() 
	{
		if (mode == MODE.READ_SHDN_DATA)
		{
			// в папке будет находится только один полноценный файл состояния после предыдущего выключения
			foreach (string file in initFolderFiles)
			{
				if (!fileExtMetaException.IsMatch(file) && shutdownSaveRegExp.IsMatch(file))
				{
					// запрашиваем инициализацию файла по условию наличия суффикса и не имеющего разрешения
					OpenRecentFile(file, STREAM_MODE_READ_BINARY, TYPE.SHDN_FILE);
					LoadWorldData();                             // выполняем загрузку данных в игровой мир
				}
			}
		}
	}
	
	/// <summary>
	/// Инициализирует названия файлов из указанной папки
	/// </summary>
	protected void InitSaveFiles()
	{
		Directory.CreateDirectory(GetSaveFolder());
		initFolderFiles = Directory.GetFiles(GetSaveFolder());
	}

	/// <summary>
	/// Инициализирует создание новой записи по данных игрового мира
	/// </summary>
	protected void InitNewWorldData(out SWorldDataStruct worldData)
	{
		saveData.Add(worldName, 
			worldData = new SWorldDataStruct(GetNewFilePath(TYPE.PATH_LIST)));
	}

	/// <summary>
	/// Инициализирует запись в saveData по текущему игровому миру
	/// </summary>
	protected void InitSaveData() 
	{
		// создаём запись в хеше по данному игровому миру
		if (!saveData.TryGetValue(worldName, out SWorldDataStruct worldData)) {
			InitNewWorldData(out worldData);
		}

		// бежим по полученному списку файлов
		foreach (string file in initFolderFiles) {

			if (!fileExtMetaException.IsMatch(file) && stageStateRegExp.IsMatch(file))
			{
				string stage = System.IO.Path.GetFileNameWithoutExtension(file);
				stage = stage.Substring(0, stage.Length - STAGE_SUFFIX.Length);

				if (!worldData.CheckDataByStage(stage) && !worldData.CheckDataByPath(file)) worldData.AddData(stage, file);
			}
		}
	}

	/// <summary>
	/// Сортировка всех листов с нодами, чтобы сохранения и загрузки выполнялись корректно
	/// </summary>
	protected virtual void SortAllNodeLists() 
	{
		Comparison<Unigine.Node> nodeCompararor = (lhs, rhs) => lhs.ID.CompareTo(rhs.ID);
		worldRootNodes.Sort(nodeCompararor);
	}

	/// <summary> 
	/// Получает от игрового мира информацию по всем корневым нодам
	/// </summary>
	protected virtual void InitWorldData() 
	{
		World.GetRootNodes(worldRootNodes);
		SortAllNodeLists();
	}

	/// <summary>
	/// Получает название текущего игрового мира
	/// </summary>
	protected void InitWorldName() 
	{
		worldName = System.IO.Path.GetFileNameWithoutExtension(World.Path);
		worldName = GetFileNameByPath(World.Path);
	}

	/// <summary>
	/// Инициализирует регулярные выражения
	/// </summary>
	protected void InitRegExps() 
	{
		shutdownSaveRegExp = new Regex((useTimeStamp ? DATA_STAMP : "") + SHUTDOWN_SUFFIX);
		defaultStateRegExp = new Regex((useTimeStamp ? DATA_STAMP : "")  + DEFAULT_SUFFIX);
		regularSaveRegExp = new Regex((useTimeStamp ? DATA_STAMP : "")  + REGULAR_SUFFIX);
		pathListSaveRegExp = new Regex((useTimeStamp ? DATA_STAMP : "")  + PATH_LIST_SUFFIX);
		stageStateRegExp = new Regex((useTimeStamp ? DATA_STAMP : "")  + STAGE_SUFFIX);
		fileExtMetaException = new Regex(META_STAMP);
	}

	/// <summary>
	/// Метод общей инициализации класса
	/// </summary>
	protected void SerializerInit()
	{
		InitWorldName();
		InitWorldData();
		InitRegExps();
		InitSaveFiles();
		InitSaveData();

		switch (mode) 
		{
			case MODE.READ_SHDN_DATA:
				InitShutDownData();
				break;
		}

		if (saveDefaultDataState) SerializeDefaultState();
	} 

	/// <summary>
	/// Метод внешней инициализации. Используется для внешнего вызова, если сериализатор находится, например, в AppSystemLogic.cs
	/// </summary>
	public virtual SerializerBase ExternanInit()
	{
		SerializerInit(); return this;
	}

	private void Init()
	{
		SerializerInit();
	}

	private void Shutdown()
    {
		if (saveDataOnShutdown) SerilizeOnShutDown();
    }

	/// <summary>
	/// Проверяем доступность файла по указанному пути
	/// </summary>
	/// <param name="path"></param>
	/// <returns></returns>
	private bool CheckFilePath(string path)
	{
		fileInfo = new FileInfo(path);
		return fileInfo.Exists;
	}

	/// <summary>
	/// Возвращает наличие сохраненного файла по имени стадии
	/// </summary>
	/// <param name="stage"></param>
	/// <returns></returns>
	private bool CheckStageByName(string stage)
	{
		return saveData.TryGetValue(worldName, out SWorldDataStruct data) && data.CheckDataByStage(stage);
	}

	/// <summary>
	/// Возвращает наличие сохраненного файла по пути к файлу стадии
	/// </summary>
	/// <param name="path"></param>
	/// <returns></returns>
	private bool CheckStageBypath(string path)
	{
		return saveData.TryGetValue(worldName, out SWorldDataStruct data) && data.CheckDataByPath(path);
	}

	// ------------------------------------------- блок внутренних общих прикладных геттеров --------------------------------------------

	/// <summary>
	/// Возвращает имя файла по пути до него. Использует System.IO.Path
	/// </summary>
	/// <param name="path"></param>
	/// <returns></returns>
	private string GetFileNameByPath(string path)
	{
		return System.IO.Path.GetFileNameWithoutExtension(path);
	}

	/// <summary>
	/// Возвращает выбранный файловый корень, куда будут происходить сохранения и создания доп папок
	/// </summary>
	/// <returns></returns>
	private string GetFolderRoot() 
	{
		switch (folderRoot)
		{
			case ROOT.DATA:
				return DATA_FOLDER;
			
			case ROOT.BINARY:
				return BINARY_FOLDER;
		}

		return "";
	}

	/// <summary>
	/// Возвращает путь к папке сохранения
	/// </summary>
	/// <returns></returns>
	private string GetSaveFolder() 
	{
		if (saveStateFolder.Length != 0)
		{
			// сохраняет в папку "{рут}/{папка сохранения}/{название игрового мира}/"
			return GetFolderRoot() + saveStateFolder + "/" + worldName + "/";
		}
		else 
		{
			return GetFolderRoot() + worldName + "/";
		}
	}

	/// <summary>
	/// Возвращает текущую дату и время
	/// </summary>
	/// <returns></returns>
	private string GetTimeStamp() 
	{
		string year = System.DateTime.Now.Year.ToString();
		string month = System.DateTime.Now.Month.ToString();
		if (month.Length == 1) month = "0" + month;
		string day = System.DateTime.Now.Day.ToString();
		if (day.Length == 1) day = "0" + day;

		string hour = System.DateTime.Now.Hour.ToString();
		if (hour.Length == 1) hour = "0" + hour;
		string min = System.DateTime.Now.Minute.ToString();
		if (min.Length == 1) min = "0" + min;
		string sec = System.DateTime.Now.Second.ToString();
		if (sec.Length == 1) sec = "0" + sec;

		return year + "-" + month + "-" + day + "-" + hour + "-" + min + "-" + sec;
	}

	/// <summary>
	/// Возвращает имя текущего игрового мира
	/// </summary>
	/// <returns></returns>
	public string GetCurrentWorldName() { return worldName; }

	// ------------------------------------------- блок методов для работы с файлами ----------------------------------------------------

	/// <summary>
	/// Вовзаращает путь к файлу сохранения по переданному типу сохранения и названию стадии
	/// </summary>
	/// <param name="type"></param>
	/// <param name="stage"></param>
	/// <returns></returns>
	private string GetNewFilePath(TYPE type, string stage = "")
	{
		switch (type)
		{
			case TYPE.SHDN_FILE:
				return new string(GetSaveFolder() + worldName + (useTimeStamp ? GetTimeStamp() : "") + SHUTDOWN_SUFFIX);

			case TYPE.DEFAULT:
				return new string(GetSaveFolder() + worldName + (useTimeStamp ? GetTimeStamp() : "") + DEFAULT_SUFFIX);

			case TYPE.PATH_LIST:
				return new string(GetSaveFolder() + worldName + (useTimeStamp ? GetTimeStamp() : "") + PATH_LIST_SUFFIX);

			case TYPE.STAGE:
				return new string(GetSaveFolder() + stage + (useTimeStamp ? GetTimeStamp() : "") + STAGE_SUFFIX);
		}

		return "";
	}

	/// <summary>
	/// Создаёт новый файл согласно переданному типу и режиму открытия. Режим открытия по умолчанию "wb". Название стейжа = ""
	/// </summary>
	/// <param name="type"></param>
	/// <param name="mode"></param>
	private void WriteNewFile(TYPE type, string stage = "")
	{
		CloseOpenedFile();
		filePath = GetNewFilePath(type, stage);

		if (fileSource.Open(filePath, STREAM_MODE_WRITE_BINARY))
		{
			fileStatus = STATUS.OPEN;
			fileType = type; fileMode = STREAM_MODE_WRITE_BINARY;
		}
		else 
		{
			fileStatus = STATUS.ERROR;
			fileType = TYPE.ERROR; fileMode = "error"; filePath = "error";
			throw new System.Exception("SERIALIZE_HANDLER::WriteNewFile(TYPE, string)::Open file error");
		}
	}

	/// <summary>
	/// Загружает в поток файл по переданному пути, в соответствующем режиме  
	/// </summary>
	/// <param name="path"></param>
	/// <param name="mode"></param>
	/// <param name="type"></param>
	private void OpenRecentFile(string path, string mode, TYPE type)
	{
		if (CheckFilePath(path)) 
		{
			if (fileStatus == STATUS.OPEN && fileSource.IsOpened) CloseOpenedFile();

			if (fileSource.Open(path, mode)) 
			{
				fileStatus = STATUS.OPEN;
				fileType = type;
			}
			else 
			{
				fileStatus = STATUS.ERROR;
				fileType = TYPE.ERROR;
			}
		}
		else 
		{
			fileStatus = STATUS.ERROR;
			fileType = TYPE.ERROR;
		}
	}

	/// <summary>
	/// Закрывает открытый файл потока
	/// </summary>
	private void CloseOpenedFile()
	{
		if (fileStatus != STATUS.CLOSE) {
			fileStatus = STATUS.CLOSE;
			fileType = TYPE.NONE;
			fileSource.Close();
			filePath = "";
		}
	}




	// ------------------------------------------- блок методов для вызова сериализации -------------------------------------------------

	/// <summary>
	/// Коды возвращаемых операций работы по (де)сериализации данных
	/// </summary>
	public enum SERIAL_RESULT 
	{
		SAVE_STAGE_COMPLETE,
		SAVE_ERROR_SAVE_EXIST,
		SAVE_ERROR_EXCEPTION,
		LOAD_STAGE_COMPLETE,
		LOAD_ERROR_NOT_EXIST,
		LOAD_ERROR_EXCEPTION
	}

	/// <summary>
	/// Выполняет сериализацию базового сотояния всех нод игрового мира
	/// </summary>
	protected void SerializeDefaultState() 
	{
		SerializeData(TYPE.DEFAULT);
	}

	/// <summary>
	/// Удаляет ранее созданные файлы сохранения при выключении
	/// </summary>
	protected void ClearShutDownSaves() 
	{
		InitSaveFiles();     // пердварительно по новой инициализируем имеющиеся в папке файлы

		foreach (string file in initFolderFiles)
		{
			if (shutdownSaveRegExp.IsMatch(file))
			{
				FileInfo delete = new FileInfo(file);
				if (delete.Exists) delete.Delete();
			}
		}
	}

	/// <summary>
	/// Вызов сериализации в файл перед выключением игры
	/// </summary>
	protected void SerilizeOnShutDown()
	{
		ClearShutDownSaves();                                       // удаляет файлы с суфиксом сохранений при выключении
		SerializeData(TYPE.SHDN_FILE);                              // вызывает сериализацию в соответствующем режиме
	}

	/// <summary>
	/// Сохраняет записи по стейжу в структуру
	/// </summary>
	/// <param name="stage"></param>
	/// <exception cref="System.Exception"></exception>
	private void AddWorldSavePathData(string stage) 
	{
		if (fileStatus == STATUS.OPEN && saveData.TryGetValue(worldName, out SWorldDataStruct worldData))
		{
			worldData.AddData(stage, filePath);
		}
		else 
		{
			throw new System.Exception("SERIALIZE_HANDLER::AddWorldSavePathData(string)::World data is not exist");
		}
	}

	/// <summary>
	/// Базовый, комплексный метод сериализации данных в различных режимах
	/// </summary>
	/// <param name="type"></param>
	/// <param name="stage"></param>
	/// <param name="exc_throw"></param>
	private SERIAL_RESULT SerializeData(TYPE type, string stage = "", bool exc_throw = true)
	{
		try
		{
			WriteNewFile(type, stage);                         // создаёт и открывает для записи новый файл сохранений
			SerializeWorldData();                              // записывает данные о состоянии игрового мира
			AddWorldSavePathData(stage);                       // сохраняет стадию и путь к файлу созранения в хеш
			CloseOpenedFile();                                 // закрывает файл записанный файл

			return SERIAL_RESULT.SAVE_STAGE_COMPLETE;
		}
		catch (System.Exception e)
		{
			if (exc_throw) {
				// в зависимости от флага или пробрасывает исключение дальше или возвращает результат обработки
				throw new System.Exception("SERIALIZE_HANDLER::SerializeData(TYPE, string, exc_throw == true)::Exception::" + e.Message);
			}
			else {
				Log.Message("SERIALIZE_HANDLER::SerializeData(TYPE, string, exc_throw == false)::Exception::Captured::\n" + e.Message);
				return SERIAL_RESULT.SAVE_ERROR_EXCEPTION;
			}
		}
		
	}

	/// <summary>
	/// Сохраняет настройки объектов игрового мира, выбрасывает исключение если файл сохранения уже существет и не установлен флаг перезаписи
	/// </summary>
	/// <param name="stage"></param>
	/// <param name="rewrite"></param>
	/// <exception cref="System.Exception"></exception>
	public void UnsafeSaveStage(string stage, bool rewrite = false)
	{
		// проверяем наличие сохранений по текущему игровому миру
		if (!saveData.TryGetValue(worldName, out SWorldDataStruct worldData)) InitNewWorldData(out worldData);
		// проверяем наличие сохранений по заданной стадии и флагу перезаписи
		if (worldData.CheckDataByStage(stage) && !rewrite)
		{
			throw new System.Exception("SERIALIZE_HANDLER::SaveStage(string)::Serialization error::Stage name already exist");
		}
		else 
		{
			try
			{
				SerializeData(TYPE.STAGE, stage);
			}
			catch (System.Exception e)
			{	
				throw new System.Exception("SERIALIZE_HANDLER::SaveStage(string)::Serialization error::" + e.Message); 
			}
		}
	}

	/// <summary>
	/// Делает попытку сохранить настройки объектов игрового мира, НЕ выбрасывает исключений и не останавливает работу модуля. Возвращает результат работы операции
	/// </summary>
	/// <param name="stage"></param>
	/// <param name="rewrite"></param>
	/// <exception cref="System.Exception"></exception>
	public SERIAL_RESULT TrySaveStage(string stage, bool rewrite = false) 
	{
		// Если данных изначально не было, то создаётся запись о сохранении
		if (!saveData.TryGetValue(worldName, out SWorldDataStruct worldData)) InitNewWorldData(out worldData);
		// Если данные уже содержат упоминания о сохраненной стадии и не выбран флаг перезаписи, то система возвращает код ошибки
		if (worldData.CheckDataByStage(stage) && !rewrite) return SERIAL_RESULT.SAVE_ERROR_SAVE_EXIST;

		// Возвращаем значение результат сериализации с отловом возможных исключений
		return SerializeData(TYPE.STAGE, stage, false);
	}


	/// <summary>
	/// Базовый, комплексный метод десериализации данных в различных режимах
	/// </summary>
	/// <param name="stage"></param>
	/// <param name="exc_throw"></param>
	private SERIAL_RESULT DeSerializeData(string stage, bool exc_throw = true) 
	{	
		if (!saveData.TryGetValue(worldName, out SWorldDataStruct worldData)|| !worldData.CheckDataByStage(stage)) 
		{
			if (exc_throw) {
				// если данных по стадии нет в базе то кидаем соответствующее исключение
				throw new System.Exception("SERIALIZE_HANDLER::DeSerializeData(string)::Load error::Stage not exist"); 
			}
			else {
				Log.Message("SERIALIZE_HANDLER::DeSerializeData(string)::Load error::Stage not exist\n");
				return SERIAL_RESULT.LOAD_ERROR_NOT_EXIST;
			}
		}

		OpenRecentFile(worldData.GetPathByStage(stage), STREAM_MODE_READ_BINARY, TYPE.STAGE);

		try
		{
			LoadWorldData();
			CloseOpenedFile();

			return SERIAL_RESULT.LOAD_STAGE_COMPLETE;
		}
		catch (System.Exception e)
		{
			if (exc_throw) {
				throw new System.Exception("SERIALIZE_HANDLER::DeSerializeData(string)::Loading error::" + e.Message);
			}
			else {
				Log.Message("SERIALIZE_HANDLER::DeSerializeData(string)::Exception::Captured::\n" + e.Message);
				return SERIAL_RESULT.LOAD_ERROR_EXCEPTION;
			}
		}
	}

	/// <summary>
	/// Загружает параметры объектов игрового мира их сохраненного ранее файла стейжа, в случае отсуствия выбрасывает исключение
	/// </summary>
	/// <param name="stage"></param>
	/// <exception cref="System.Exception"></exception>
	public SERIAL_RESULT UnsafeLoadStage(string stage)
	{
		return DeSerializeData(stage);
	}

	/// <summary>
	/// Безопасная попытка загрузить данные, вернет результат работы, в случае ошибок или исключений не пробросит их дальше
	/// </summary>
	/// <param name="stage"></param>
	/// <returns></returns>
	public SERIAL_RESULT TryLoadStage(string stage)
	{
		return DeSerializeData(stage, false);
	}




	// ------------------------------------------- блок методов сохранения информации в файл --------------------------------------------

	/// <summary>
	/// Записывает в файл информаию о предоставленной ноде, также выставляет предварительную шапку по переданному Label
	/// </summary>
	/// <param name="node"></param>
	/// <param name="label"></param>
	private void SerializeNode(Node node, string label) 
	{
		fileSource.WriteString(label + ". Begin\n");
		fileSource.WriteInt(node.ID);                       // записываем ID текущей ноды
		fileSource.WriteInt(node.Parent.ID);                // записываем ID родителя
		fileSource.WriteString(node.Name);                  // записываем название текущей ноды
		fileSource.WriteMat4(node.WorldTransform);          // матрицу трансформации
		fileSource.WriteQuat(node.GetWorldRotation());      // кватернион поворота
		fileSource.WriteVec3(node.WorldPosition);           // мировую позицию
		fileSource.WriteVec3(node.WorldScale);              // мировой масштаб

		if (node.IsObject) {
			fileSource.WriteBool(true);                     // флаг объекта
			Unigine.Object nodeObject = node as Unigine.Object;

			fileSource.WriteInt(nodeObject.NumSurfaces);    // записываем количество поверхностей
			
			for (int i = 0; i != nodeObject.NumSurfaces; i++) {
				Material mat = nodeObject.GetMaterial(i);
				fileSource.WriteVec4(mat.GetParameterFloat4("auxiliary_color"));
			}
		}
		else 
		{
			fileSource.WriteBool(false);                    // флаг объекта
		}

		fileSource.WriteString("\n" + label + ". End\n");
	}

	/// <summary>
	/// Базовая рекурсивная функция сериализации узлов. Проходит по всем наследникам каждого из переданных узлов включая переданный.
	/// Если у узла нет наследников, он считается дочерним, если есть, то родительским.
	/// </summary>
	/// <param name="node"></param>
	private void SerializeNodeData(Node node)
	{
		int num = node.NumChildren;                         // получаем количество наследников

		if (num == 0) 
		{
			SerializeNode(node, "ChildNode");               // если наследников нет, записываем дочерний узел и выходим из рекурсии
		}
		else 
		{
			if (node.Parent != null) SerializeNode(node, "ParentNode");   // записываем родительский узел, если у него самого есть родитель
			for (int i = 0; i != num; i++)
			{
				SerializeNodeData(node.GetChild(i));        // записываем всех наследников родительского узла
			}
		}
	}

	/// <summary>
	/// Сериализация корневого узла
	/// </summary>
	/// <param name="node"></param>
	private void SerializeRootNode(Node node) 
	{
		fileSource.WriteString("Root Node. Begin\n");
		fileSource.WriteInt(node.ID);                       // записываем ID текущей ноды
		fileSource.WriteString(node.Name);                  // записываем название текущей ноды
		fileSource.WriteMat4(node.WorldTransform);          // матрицу трансформации
		fileSource.WriteQuat(node.GetWorldRotation());      // кватернион поворота
		fileSource.WriteVec3(node.WorldPosition);           // мировую позицию
		fileSource.WriteVec3(node.WorldScale);              // мировой масштаб

		if (node.IsObject) {
			fileSource.WriteBool(true);                     // флаг объекта
			Unigine.Object nodeObject = node as Unigine.Object;

			fileSource.WriteInt(nodeObject.NumSurfaces);    // записываем количество поверхностей
			
			for (int i = 0; i != nodeObject.NumSurfaces; i++) {
				Material mat = nodeObject.GetMaterial(i);
				fileSource.WriteVec4(mat.GetParameterFloat4("auxiliary_color"));
			}
		}
		else 
		{
			fileSource.WriteBool(false);                    // флаг объекта
		}

		fileSource.WriteString("\nRoot Node. End\n");
	}

	/// <summary>
	/// Вызов сериализации игровых данных в заранее открытый файл
	/// </summary>
	protected virtual void SerializeWorldData()
	{
		if (fileStatus == STATUS.OPEN) 
		{
			foreach (Node node in worldRootNodes) 
			{	
				SerializeRootNode(node);
				if (node.NumChildren > 0) SerializeNodeData(node);
			}		
		}
		else 
		{
			throw new System.ArgumentException("SERIALIZE_HANDLER::SerializeWorldData()::fileStatus != STATUS.OPEN");
		}
	}




	// ------------------------------------------- блок методов загрузки информации из файла --------------------------------------------

	/// <summary>
	/// Загружает информаию о предоставленной ноде из файла
	/// </summary>
	/// <param name="node"></param>
	private void LoadNode(Node node) 
	{
		try 
		{
			string nodeHeader = fileSource.ReadString();        // получаем хеддер "... Node. Begin\n"
			int nodeId = fileSource.ReadInt();                  // получаем ID текущей ноды         
			int nodeParentId = fileSource.ReadInt();            // получаем ID родителя текущей ноды        
			string nodeName = fileSource.ReadString();          // получаем название текущей ноды      
			mat4 nodeWorldTransform = fileSource.ReadMat4();    // матрицу трансформации     
			quat nodeWorldRotation = fileSource.ReadQuat();     // кватернион поворота
			vec3 nodeWorldPosition = fileSource.ReadVec3();     // мировую позицию
			vec3 nodeWorldScale = fileSource.ReadVec3();        // мировой масштаб

			if (fileSource.ReadBool()) {
				// если нода является объектом, то записываем её параметры материалов
				Unigine.Object nodeObject = node as Unigine.Object;
				// количество поверхностей должно быть сериализировано, потому берем из потока
				int nodeNumSurfaces = fileSource.ReadInt();
				for (int i = 0; i != nodeNumSurfaces; i++) {
					Material mat = nodeObject.GetMaterial(i);
					// сразу же из потока берем цвет засветки
					mat.SetParameterFloat4("auxiliary_color", fileSource.ReadVec4());
				}
			}

			string nodeEnd = fileSource.ReadString();           // хеддер "... Node. End\n"

			if (node.ID == nodeId && node.Parent.ID == nodeParentId)    // в случае совпадения по ID записываем параметры в корневую ноду
			{
				node.WorldTransform = nodeWorldTransform;
				node.SetWorldRotation(nodeWorldRotation);
				node.WorldPosition = nodeWorldPosition;
				node.WorldScale = nodeWorldScale;
			}
		}
		catch
		{
			// TODO сделать выбрасывание исключения
		}
	}

	/// <summary>
	/// Базовая рекурсивная функция получения информации по узлам. Проходит по всем наследникам каждого из переданных узлов включая переданный.
	/// Если у узла нет наследников, он считается дочерним, если есть, то родительским.
	/// </summary>
	/// <param name="node"></param>
	private void LoadNodeData(Node node)
	{
		int num = node.NumChildren;                         // получаем количество наследников

		if (num == 0) 
		{
			LoadNode(node);               // если наследников нет, получаем дочерний узел и выходим из рекурсии
		}
		else 
		{
			if (node.Parent != null) LoadNode(node);   // получаем родительский узел, если у него самого есть родитель
			for (int i = 0; i != num; i++)
			{
				LoadNodeData(node.GetChild(i));        // получаем всех наследников родительского узла
			}
		}
	}

	/// <summary>
	/// Загружает параметры корневого узла
	/// </summary>
	/// <param name="node"></param>
	private void LoadRootNode(Node node) 
	{
		try 
		{
			string rootNodeHeader = fileSource.ReadString();        // получаем хеддер "Root Node. Begin\n"
			int rootNodeId = fileSource.ReadInt();                  // получаем ID текущей ноды         
			string rootNodeName = fileSource.ReadString();          // получаем название текущей ноды      
			mat4 rootNodeWorldTransform = fileSource.ReadMat4();    // матрицу трансформации     
			quat rootNodeWorldRotation = fileSource.ReadQuat();     // кватернион поворота
			vec3 rootNodeWorldPosition = fileSource.ReadVec3();     // мировую позицию
			vec3 rootNodeWorldScale = fileSource.ReadVec3();        // мировой масштаб

			if (fileSource.ReadBool()) {
				// если нода является объектом, то записываем её параметры материалов
				Unigine.Object rootNodeObject = node as Unigine.Object;
				// количество поверхностей должно быть сериализировано, потому берем из потока
				int rootNodeNumSurfaces = fileSource.ReadInt();
				for (int i = 0; i != rootNodeNumSurfaces; i++) {
					Material mat = rootNodeObject.GetMaterial(i);
					// сразу же из потока берем цвет засветки
					mat.SetParameterFloat4("auxiliary_color", fileSource.ReadVec4());
				}
			}

			string rootNodeEnd = fileSource.ReadString();           // хеддер "Root Node. End\n"

			if (node.ID == rootNodeId)    // в случае совпадения по ID записываем параметры в корневую ноду
			{
				node.WorldTransform = rootNodeWorldTransform;
				node.SetWorldRotation(rootNodeWorldRotation);
				node.WorldPosition = rootNodeWorldPosition;
				node.WorldScale = rootNodeWorldScale;
			}
		}
		catch
		{
			// TODO сделать выбрасывание исключения
		}
		
	}

	/// <summary>
	/// Загружает параметры из cериализованных данных
	/// </summary>
	protected virtual void LoadWorldData()
	{
		if (fileStatus == STATUS.OPEN) 
		{
			foreach (Node node in worldRootNodes) 
			{	
				LoadRootNode(node);
				if (node.NumChildren > 0) LoadNodeData(node);
			}
		}
	}
}