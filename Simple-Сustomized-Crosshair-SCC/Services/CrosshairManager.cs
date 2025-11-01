using Newtonsoft.Json;
using Simple_Customized_Crosshair_SCC.Models;
using System.Collections.ObjectModel;
using System.IO;

namespace Simple_Customized_Crosshair_SCC.Services
{
    /// <summary>
    /// Менеджер для работы с прицелами
    /// </summary>
    public class CrosshairManager
    {
        private readonly string _appDataPath;
        private readonly string _crosshairsFolder;

        public CrosshairManager()
        {
            _appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _crosshairsFolder = Path.Combine(_appDataPath, "SimpleCustomizedCrosshair", "Crosshairs");

            if (!Directory.Exists(_crosshairsFolder))
            {
                Directory.CreateDirectory(_crosshairsFolder);
            }
        }

        /// <summary>
        /// Сохранить прицел
        /// </summary>
        public void SaveCrosshair(Crosshair crosshair)
        {
            var fileName = $"{SanitizeFileName(crosshair.Name)}.json";
            var filePath = Path.Combine(_crosshairsFolder, fileName);

            var json = JsonConvert.SerializeObject(crosshair, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// Загрузить все прицелы
        /// </summary>
        public ObservableCollection<Crosshair> LoadCrosshairs()
        {
            var crosshairs = new ObservableCollection<Crosshair>();

            if (!Directory.Exists(_crosshairsFolder))
                return crosshairs;

            var files = Directory.GetFiles(_crosshairsFolder, "*.json");

            foreach (var file in files)
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var crosshair = JsonConvert.DeserializeObject<Crosshair>(json);
                    if (crosshair != null)
                    {
                        crosshairs.Add(crosshair);
                    }
                }
                catch (System.Exception ex)
                {
                    // Логирование ошибки
                    System.Diagnostics.Debug.WriteLine($"Error loading crosshair {file}: {ex.Message}");
                }
            }

            return crosshairs;
        }

        /// <summary>
        /// Удалить прицел
        /// </summary>
        public void DeleteCrosshair(Crosshair crosshair)
        {
            var fileName = $"{SanitizeFileName(crosshair.Name)}.json";
            var filePath = Path.Combine(_crosshairsFolder, fileName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        /// <summary>
        /// Экспортировать прицел
        /// </summary>
        public void ExportCrosshair(Crosshair crosshair, string filePath)
        {
            var json = JsonConvert.SerializeObject(crosshair, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// Импортировать прицел
        /// </summary>
        public Crosshair? ImportCrosshair(string filePath)
        {
            try
            {
                var json = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<Crosshair>(json);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error importing crosshair: {ex.Message}");
                return null;
            }
        }

        private string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            return string.Join("_", fileName.Split(invalidChars));
        }
    }
}