using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SpargoTest
{
    class IniReader
    {
        // результат чтения параметров (словарь <название, значение> для каждого раздела)
        Dictionary<string, Dictionary<string, string>> ini = new Dictionary<string, Dictionary<string, string>>(StringComparer.InvariantCultureIgnoreCase);

        // Инициализация + чтение файла:
        public IniReader(string file)
        {
            // прочитать в переменную:
            var txt = File.ReadAllText(file);

            // изначальный раздел (секция) файла конфигурации:
            Dictionary<string, string> currentSection = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            ini[""] = currentSection;

            // Проход по всем строкам: с удалением пробелов
            foreach (var line in txt.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries)
                                   .Where(t => (!string.IsNullOrEmpty(t) && t.Trim().Length != 0))
                                   .Select(t => t.Trim()))
            {
                // пропуск комментов:
                if (line.StartsWith(";"))
                    continue;

                // раздел\секция:
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    currentSection = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
                    ini[line.Substring(1, line.LastIndexOf("]") - 1)] = currentSection;
                    continue;
                }

                // параметр ("имя=значение"):
                var idx = line.IndexOf("="); // строка содержит = ?
                if (idx == -1)
                    currentSection[line] = ""; // если не найдено
                else
                    currentSection[line.Substring(0, idx)] = line.Substring(idx + 1); // если найдено, добавить параметр со значением
            }
        }



        // чтение по названию параметра, без указания секции:
        public string GetValue(string key)
        {
            return GetValue(key, "", "");
        }


        // Чтение параметра в указанной секции:
        public string GetValue(string key, string section)
        {
            return GetValue(key, section, "");
        }


        // Чтение со значением по умолчанию
        public string GetValue(string key, string section, string @default)
        {
            if (!ini.ContainsKey(section))
                return @default;

            if (!ini[section].ContainsKey(key))
                return @default;

            return ini[section][key];
        }


        // Список всех параметров в секции:
        public string[] GetKeys(string section)
        {
            if (!ini.ContainsKey(section))
                return new string[0];

            return ini[section].Keys.ToArray();
        }


        // Список всех секций:
        public string[] GetSections()
        {
            return ini.Keys.Where(t => t != "").ToArray();
        }


    } // class IniReader
} // namespace SpargoTest
