using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Data;
using System.Data.SqlClient;


namespace SpargoTest
{
    class Program
    {

        private static Program instance;

        string SQL_server = "";
        string SQL_port = "";
        string SQL_user = "";
        string SQL_passw = "";
        string SQL_database = "";
        SqlConnection SQL_connection;


        static void Main(string[] args)
        {
            // Кодировка текста:
            // TODO: работает только английский
            Console.OutputEncoding = Encoding.GetEncoding(866); //Encoding.UTF8;
            //Console.InputEncoding = Encoding.GetEncoding(1251);

            // Создать единый экземпляр программы (синглтон):
            Program App = new Program();

            // Чтение настроек подключения:
            App.ReadConfig();

            // Запрос пароля БД от пользователя:
            Console.Write("Enter password: ");
            App.SQL_passw = Console.ReadLine();


            // Попытка подключения к БД:
            if (App.ConnectServer() == 0)
            {
                Console.WriteLine("Connection failed. Press any key..");
                Console.ReadKey();
                return; // выйти из программы
            }

            // Цикл перехода (возврата) к основному меню:
            var ExitLoop = false;
            while (!ExitLoop)
            {
                Console.WriteLine("\nSelect:");
                Console.WriteLine("1. Create product");
                Console.WriteLine("2. Delete product");
                Console.WriteLine("3. Create store");
                Console.WriteLine("4. Delete store");
                Console.WriteLine("5. Create warehouse");
                Console.WriteLine("6. Delete warehouse");
                Console.WriteLine("7. Create batch");
                Console.WriteLine("8. Delete batch");
                Console.WriteLine("9. Show items");
                Console.WriteLine("0. exit");

                // Запрос номера пункта у пользователя:
                Console.WriteLine("\nYour choice:");

                var answer = Console.ReadLine();

                switch (answer)
                {
                    // Товар:
                    case "1":
                        App.CreateProduct();
                        break;
                    case "2":
                        App.InterfaceForDelete("tovar");
                        break;

                    // Аптека:
                    case "3":
                        App.CreateStore();
                        break;
                    case "4":
                        App.InterfaceForDelete("apteka");
                        break;

                    // Склад:
                    case "5":
                        App.CreateWarehouse();
                        break;
                    case "6":
                        App.InterfaceForDelete("sklad");
                        break;

                    // Партия:
                    case "7":
                        App.CreateBatch();
                        break;
                    case "8":
                        App.InterfaceForDelete("batch");
                        break;

                    // Показать товары в аптеке:
                    case "9":
                        App.ShowAllItemsInStore();
                        break;

                    // Выход:
                    case "0":
                        ExitLoop = true;
                        break;
                } // switch(answer)

            } // while

            // Отключение от БД:
            App.DisconnectServer();

            // EXIT PROGRAM
        } // Main()



        public static Program getInstance()
        {
            if (instance == null)
                instance = new Program();
            return instance;
        }



        // Чтение настроек:
        public void ReadConfig()
        {
            var ConfigData = new IniReader("config.ini");
            SQL_server = ConfigData.GetValue("server", "Connection");
            SQL_port = ConfigData.GetValue("port", "Connection");
            SQL_user = ConfigData.GetValue("user", "Connection");
            //SQL_passw = ConfigData.GetValue("passw", "Connection");
            SQL_database = ConfigData.GetValue("database", "Connection");
        }



        // Соединение к БД:
        // возвращает 0 в случае ошибки
        public int ConnectServer()
        {
            string connetionString = null;

            // Порт может быть пустым, тогда игнорируется

            connetionString = "Data Source=" + SQL_server + ((SQL_port=="")?"":("," + SQL_port)) + ";Initial Catalog=" + SQL_database + ";User ID=" + SQL_user + ";Password=" + SQL_passw;
            Console.WriteLine("Connection=" + connetionString);

            // Попытка соединения:
            SQL_connection = new SqlConnection(connetionString);
            try
            {
                SQL_connection.Open();
                Console.WriteLine("Connection OK");
                return 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Connection error");
                return 0;
            }
        } // ConnectServer()



        // Отключение от БД:
        public void DisconnectServer()
        {
            SQL_connection.Close();
        }



        // Чтение таблицы, по произвольному SQL запросу
        // возвращает список строк, в виде словаря <ключ(имя поля), значение>
        // null в случае ошибки
        public List<Dictionary<string, string>> ReadTable(string Query)
        {
            if (SQL_connection.State == ConnectionState.Open) // если есть соединение
            {
                Dictionary<string, string> RowData;
                List<Dictionary<string, string>> result = new List<Dictionary<string, string>>(); // пустой список с результатами
                SqlCommand cmd = new SqlCommand(Query, SQL_connection);
                SqlDataReader dataReader;

                // Запрос к БД:
                dataReader = cmd.ExecuteReader();
                while (dataReader.Read()) // чтение строки
                {
                    // выделение памяти под новую строку:
                    RowData = new Dictionary<string, string>();

                    // заполнение полей:
                    for(int i=0;i<dataReader.FieldCount;i++){
                        RowData.Add(dataReader.GetName(i), dataReader.GetValue(i).ToString());
                    }

                    // добавление строки в итоговый список:
                    result.Add(RowData);
                } // while

                // де-инициализация:
                dataReader.Close();
                cmd.Dispose();
                return result;
            }
            else
            {
                return null; // вернуть null в случае ошибки
            }
        } // ReadTable()



        // проверка наличия строки в таблице по id
        // возвращает true, если найдено
        public bool CheckItemExists(string Table, string id)
        {
            if (SQL_connection.State == ConnectionState.Open) // если есть соединение
            {
                SqlCommand cmd = new SqlCommand("SELECT COUNT(*) AS n FROM " + Table, SQL_connection);

                // чтение единственного поля с результатом:
                int count = (int)cmd.ExecuteScalar();

                return ((count>0) ? true : false);
            }
            else
            {
                // TODO: решить что делать в случае ошибки
                return false;
            }
        } // CheckItemExists()



        // Добавление строки в таблицу
        // RowData - словарь формата <ключ(имя поля), значение>
        public void WriteTable(string Table, Dictionary<string, string> RowData)
        {
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = SQL_connection;
            cmd.CommandText = "INSERT INTO " + Table + " (" + String.Join(",", RowData.Keys.ToArray()) + ") VALUES ('" + String.Join("','", RowData.Values.ToArray()) + "')";

            if (SQL_connection.State == ConnectionState.Open) // если есть соединение
            {
                // Запрос к БД:
                cmd.ExecuteScalar();
                Console.WriteLine("Succesfully inserted");
            }
        }



        // Удаление строки из таблицы по id
        public void DeleteItem(string Table, int id)
        {
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = SQL_connection;
            cmd.CommandText = "DELETE FROM " + Table + " WHERE id=" + id;

            if (SQL_connection.State == ConnectionState.Open) // если есть соединение
            {
                // запрос к БД:
                cmd.ExecuteScalar();
                
                Console.WriteLine("Succesfully deleted");

                int numberOfRecords = cmd.ExecuteNonQuery();
                // TODO: show error if none deleted

            }
        } // DeleteItem()



        // Произвольный запрос к БД:
        public void RunQuery(string Query)
        {
            SqlCommand cmd = new SqlCommand(Query, SQL_connection);

            if (SQL_connection.State == ConnectionState.Open) // если есть соединение
            {
                cmd.ExecuteScalar();
            }
        }



        // Валидация телефона:
        // удаляет все символы, кроме цифр.
        // если длина строки менее 10 или более 15 знаков, то возвращает "" (ошибка)
        public string ValidatePhone(string str)
        {
            string result = Regex.Replace(str, "[^0-9]+", "");
            if (result.Length < 10 || result.Length > 15) result = "";
            return result;
        } // ValidatePhone()



        // интерактивная процедура создания товара:
        public void CreateProduct()
        {
            var ExitLoop = false;
            while (!ExitLoop) // цикл возврата к следующей попытке
            {
                // пустой словарь с данными для новой строки:
                Dictionary<string, string> RowData = new Dictionary<string, string>();

                // Спросить пользователя: название товара
                Console.Write("\nEnter name: ");
                var ProductName = Console.ReadLine();
                RowData.Add("name", ProductName.Replace("'", "''"));

                // Запись в БД:
                WriteTable("tovar", RowData);

                // Следующая попытка?
                Console.Write("\nAdd another item? (y/n): ");
                var answer = Console.ReadLine();
                if (answer!="y")
                {
                    ExitLoop = true;
                }
                  
            } // while

        } // CreateProduct()



        // интерактивная процедура создания аптеки:
        public void CreateStore()
        {
            var ExitLoop = false;
            var ErrorMsg = "";
            while (!ExitLoop) // цикл возврата к следующей попытке
            {
                // пустой словарь с данными для новой строки:
                Dictionary<string, string> RowData = new Dictionary<string, string>();

                // Спросить пользователя: название аптеки
                Console.Write("\nEnter name: ");
                var StoreName = Console.ReadLine();
                RowData.Add("name", StoreName.Replace("'", "''"));

                // Спросить пользователя: адрес аптеки
                Console.Write("\nEnter address: ");
                var StoreAddress = Console.ReadLine();
                RowData.Add("address", StoreAddress.Replace("'", "''"));

                // Спросить пользователя: телефон аптеки
                Console.Write("\nEnter phone: ");
                var StorePhone = ValidatePhone(Console.ReadLine());
                RowData.Add("phone", StorePhone.Replace("'", "''"));

                // Проверка ошибок:
                if (StoreAddress == "") ErrorMsg = "Empty address";
                if (StorePhone == "") ErrorMsg = "Wrong phone number";


                if (ErrorMsg == "") // если нет ошибок
                {
                    // Запись в БД:
                    WriteTable("apteka", RowData);
                }
                else
                {
                    Console.WriteLine("Error: " + ErrorMsg);
                }


                // Следующая попытка?
                Console.Write("\nAdd another item? (y/n): ");
                var answer = Console.ReadLine();
                if (answer != "y")
                {
                    ExitLoop = true;
                }

            } // while

        } // CreateStore()



        // интерактивная процедура создания склада:
        public void CreateWarehouse()
        {
            var ExitLoop = false;
            var ErrorMsg = "";
            string id;
            while (!ExitLoop) // цикл возврата к следующей попытке
            {
                // пустой словарь с данными для новой строки:
                Dictionary<string, string> RowData = new Dictionary<string, string>();

                // Спросить пользователя: id аптеки
                // TODO: interface for store selection
                Console.Write("\nEnter store id: ");
                id = Console.ReadLine();
                int StoreId = 0;
                // валидация числа id:
                if (int.TryParse(id, out StoreId))
                {
                    // Проверка что существует в БД:
                    if (!CheckItemExists("apteka", StoreId.ToString()))
                    {
                        ErrorMsg = "store not found";
                    }
                }
                else
                {
                    ErrorMsg = "wrong store id";
                }
                RowData.Add("apteka_id", StoreId.ToString());

                // Спросить пользователя: название склада
                Console.Write("\nEnter name: ");
                var StoreName = Console.ReadLine();
                RowData.Add("name", StoreName.Replace("'", "''"));


                if (ErrorMsg == "") // если нет ошибок
                {
                    // Запись в БД:
                    WriteTable("sklad", RowData);
                }
                else
                {
                    Console.WriteLine("Error: " + ErrorMsg);
                }


                // Следующая попытка?
                Console.Write("\nAdd another item? (y/n): ");
                var answer = Console.ReadLine();
                if (answer != "y")
                {
                    ExitLoop = true;
                }

            } // while

        } // CreateWarehouse()



        // интерактивная процедура создания партии:
        public void CreateBatch()
        {
            var ExitLoop = false;
            var ErrorMsg = "";
            string id;
            while (!ExitLoop) // цикл возврата к следующей попытке
            {
                // пустой словарь с данными для новой строки:
                Dictionary<string, string> RowData = new Dictionary<string, string>();

                // Спросить пользователя: id товара
                // TODO: interface for product selection
                Console.Write("\nEnter product id: ");
                id = Console.ReadLine();
                int ProductId = 0;
                // валидация числа id:
                if (int.TryParse(id, out ProductId))
                {
                    // Проверка наличия товара в БД:
                    if (!CheckItemExists("tovar", ProductId.ToString()))
                    {
                        ErrorMsg = "product not found";
                    }
                }
                else
                {
                    ErrorMsg = "wrong product id";
                }
                RowData.Add("tovar_id", ProductId.ToString());


                // Спросить пользователя: id склада
                // TODO: interface for store selection
                Console.Write("\nEnter sklad id: ");
                id = Console.ReadLine();
                int SkladId = 0;
                // валидация числа id:
                if (int.TryParse(id, out SkladId))
                {
                    // Проверка наличия этого склада в БД:
                    if (!CheckItemExists("sklad", SkladId.ToString()))
                    {
                        ErrorMsg = "sklad not found";
                    }
                }
                else
                {
                    ErrorMsg = "wrong sklad id";
                }
                RowData.Add("sklad_id", SkladId.ToString());


                // Спросить пользователя: кол-во товара:
                Console.Write("\nEnter number of items: ");
                id = Console.ReadLine();
                int Num = 0;
                // валидация числа:
                if (!int.TryParse(id, out Num))
                {
                    ErrorMsg = "wrong number";
                }
                RowData.Add("num", Num.ToString());


                if (ErrorMsg == "") // если нет ошибок
                {
                    // Запись в БД:
                    WriteTable("batch", RowData);
                }
                else
                {
                    Console.WriteLine("Error: " + ErrorMsg);
                }


                // Следующая попытка?
                Console.Write("\nAdd another item? (y/n): ");
                var answer = Console.ReadLine();
                if (answer != "y")
                {
                    ExitLoop = true;
                }

            } // while

        } // CreateBatch()



        // интерактивная процедура удаления чего-либо:
        public void InterfaceForDelete(string Table)
        {
            var ExitLoop = false;
            while (!ExitLoop) // цикл возврата к следующей попытке
            {
                // Спросить пользователя: id объекта:
                Console.Write("\nEnter id: ");
                var id = Console.ReadLine();
                int id_int = 0;
                // валидация числа id:
                if (int.TryParse(id, out id_int))
                {
                    // В зависимости от типа данных,
                    // Сначала удалить зависимые объекты:
                    switch (Table)
                    {
                        case "tovar":
                            // Удалить все партии во всех аптеках, связанные с этим товаром:
                            RunQuery("DELETE FROM batch WHERE tovar_id=" + id_int);
                            break;

                        case "apteka":
                            // Удалить все склады аптеки и партии в складах:
                            RunQuery("DELETE FROM batch WHERE sklad_id IN(SELECT DISTINCT id FROM sklad WHERE sklad.apteka_id=" + id_int + ")");
                            RunQuery("DELETE FROM sklad WHERE apteka_id=" + id_int);
                            // TODO: make transaction
                            break;

                        case "sklad":
                            // Удалить все данные о партиях в этом складе:
                            RunQuery("DELETE FROM batch WHERE sklad_id=" + id_int);
                            break;
                    } // switch (Table)

                    // Удалить целевой объект:
                    DeleteItem(Table, id_int);
                }
                else
                {
                    Console.WriteLine("Error: wrong number");
                }

                // Следующая попытка?
                Console.Write("\nDelete another item? (y/n): ");
                var answer = Console.ReadLine();
                if (answer != "y")
                {
                    ExitLoop = true;
                }

            } // while

        } // InterfaceForDelete()



        // Вывести на экран весь список товаров и его количество в выбранной аптеке (количество товара во всех складах аптеки)
        public void ShowAllItemsInStore()
        {
            var ExitLoop = false;
            while (!ExitLoop) // цикл возврата к следующей попытке
            {
                Console.WriteLine("\n========== Store list: ===========");

                // Прочитать из БД список всех аптек:
                List<Dictionary<string, string>> StoreList = ReadTable("SELECT * FROM apteka ORDER BY id");
                foreach (var Store in StoreList)
                {
                    Console.WriteLine("id=" + Store["id"] + "  " + Store["name"].Trim() + " (" + Store["address"].Trim() + "; tel:" + Store["phone"].Trim() + ")");
                }

                // Спросить пользователя: id аптеки:
                Console.Write("\nEnter store id: ");
                var id = Console.ReadLine();
                int id_int = 0;
                // валидация числа id:
                if (int.TryParse(id, out id_int))
                {
                    // Если аптека с этим id есть в БД?:
                    if (CheckItemExists("apteka", id_int.ToString()))
                    {
                        // SQL запрос:
                        List<Dictionary<string, string>> RowData = ReadTable(
                            "SELECT    SUM(batch.num) AS total_num,    tovar.name AS tovar_name " + 
                            "FROM batch " + 
                            "INNER JOIN tovar ON tovar.id = batch.tovar_id " + 
                            "INNER JOIN sklad ON sklad.id = batch.sklad_id " +
                            "WHERE (sklad.apteka_id = " + id_int + ") " +
                            "GROUP BY batch.tovar_id, tovar.name " +
                            "ORDER BY tovar_name");

                        // Вывод результатов на экран:
                        Console.WriteLine("\n  result:");
                        foreach (var Row in RowData)
                        {
                            Console.WriteLine(Row["total_num"] + "x  " + Row["tovar_name"]);
                        }

                    }
                    else
                    {
                        Console.WriteLine("Error: store not found");
                    }
                }
                else
                {
                    Console.WriteLine("Error: wrong number");
                }

                // Следующая попытка?
                Console.Write("\nTry again? (y/n): ");
                var answer = Console.ReadLine();
                if (answer != "y")
                {
                    ExitLoop = true;
                }

            } // while

        } // ShowAllItemsInStore()



    } // class Program
} // namespace SpargoTest
