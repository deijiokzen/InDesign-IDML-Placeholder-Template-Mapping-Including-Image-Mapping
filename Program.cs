using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace PlaceholderNamespace
{
    class EntryPoint
    {
        static void Main(string[] args)
        {
            string basePath = "<BASE_PATH_PLACEHOLDER>";
            string imagePath = "<IMAGE_PATH_PLACEHOLDER>";
            TemplateHandler templateHandler = new TemplateHandler();
            templateHandler.ProcessTemplate(basePath, imagePath);
        }
    }

    class TemplateHandler
    {
        public void ProcessTemplate(string basePath, string imagePath)
        {
            IDMLProcessor idmlProcessor = new IDMLProcessor();

            string originalIdmlFilePath = basePath + "<ORIGINAL_IDML_FILE_PATH_PLACEHOLDER>";
            string updatedIdmlFilePath = basePath + "<UPDATED_IDML_FILE_PATH_PLACEHOLDER>";

            string[] imageFilePaths = { imagePath };

            string[] oldStoryMappings = {
                "<OLD_MAPPING_1>", "<OLD_MAPPING_2>", "<OLD_MAPPING_3>"
            };

            string[] newStoryMappings = {
                "<NEW_MAPPING_1>", "<NEW_MAPPING_2>", "<NEW_MAPPING_3>"
            };

            idmlProcessor.UpdateIdmlFile(originalIdmlFilePath, updatedIdmlFilePath, imageFilePaths, oldStoryMappings, newStoryMappings);

            Console.WriteLine($"Original IDML file path: {originalIdmlFilePath}");
            Console.WriteLine($"Updated IDML file path: {updatedIdmlFilePath}");
            Console.WriteLine("New .idml file created successfully.");
        }
    }

    class IDMLProcessor
    {
        public void UpdateIdmlFile(string originalFilePath, string updatedFilePath, string[] imageFilePaths, string[] oldStoryMappings, string[] newStoryMappings)
        {
            byte[] originalIdmlContent;
            using (FileStream originalFileStream = new FileStream(originalFilePath, FileMode.Open))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    originalFileStream.CopyTo(ms);
                    originalIdmlContent = ms.ToArray();
                }
            }

            using (MemoryStream updatedIdmlZip = new MemoryStream())
            {
                updatedIdmlZip.Write(originalIdmlContent, 0, originalIdmlContent.Length);
                using (ZipArchive updatedZipFile = new ZipArchive(updatedIdmlZip, ZipArchiveMode.Update, true))
                {
                    UpdateSpreadXml(updatedZipFile, imageFilePaths);

                    string[] storyPaths = ExtractStoryPaths(updatedZipFile);

                    foreach (string storyPath in storyPaths)
                    {
                        UpdateStoryXml(updatedZipFile, storyPath, oldStoryMappings, newStoryMappings);
                    }
                }

                File.WriteAllBytes(updatedFilePath, updatedIdmlZip.ToArray());
            }
        }

        public void UpdateSpreadXml(ZipArchive updatedZipFile, string[] imageFilePaths)
        {
            int imageIndex = 0;

            foreach (var entry in updatedZipFile.Entries.ToList())
            {
                if (entry.FullName.StartsWith("Spreads/") && entry.FullName.EndsWith(".xml"))
                {
                    using (MemoryStream spreadMemoryStream = new MemoryStream())
                    {
                        using (Stream spreadStream = entry.Open())
                        {
                            spreadStream.CopyTo(spreadMemoryStream);
                        }

                        spreadMemoryStream.Seek(0, SeekOrigin.Begin);
                        XDocument doc = XDocument.Load(spreadMemoryStream);
                        var contentElements = doc.Descendants("Contents").ToArray();

                        for (int i = 0; i < contentElements.Length && imageIndex < imageFilePaths.Length; i++)
                        {
                            string base64Content = ConvertImageToBase64(imageFilePaths[imageIndex]);
                            contentElements[i].Value = base64Content;
                            imageIndex++;
                        }

                        spreadMemoryStream.SetLength(0);
                        spreadMemoryStream.Position = 0;
                        doc.Save(spreadMemoryStream);

                        entry.Delete();

                        var newEntry = updatedZipFile.CreateEntry(entry.FullName);
                        using (Stream entryStream = newEntry.Open())
                        {
                            spreadMemoryStream.Seek(0, SeekOrigin.Begin);
                            spreadMemoryStream.CopyTo(entryStream);
                        }
                    }
                }
            }
        }

        public void UpdateStoryXml(ZipArchive updatedZipFile, string storyPath, string[] oldMappings, string[] newMappings)
        {
            ZipArchiveEntry xmlEntry = updatedZipFile.GetEntry(storyPath);
            using (Stream xmlStream = xmlEntry.Open())
            {
                XDocument xmlDoc = XDocument.Load(xmlStream);

                foreach (var charStyleRange in xmlDoc.Descendants("CharacterStyleRange"))
                {
                    foreach (var contentElement in charStyleRange.Elements("Content"))
                    {
                        string contentValue = (string)contentElement;

                        for (int j = 0; j < oldMappings.Length; j++)
                        {
                            string oldValue = oldMappings[j];
                            string newValue = newMappings[j];

                            if (contentValue != null && contentValue.Contains(oldValue))
                            {
                                contentValue = contentValue.Replace(oldValue, newValue);
                            }
                        }

                        contentElement.Value = contentValue;
                    }
                }

                xmlStream.SetLength(0);
                xmlStream.Position = 0;

                xmlDoc.Save(xmlStream, SaveOptions.DisableFormatting);
            }
        }

        public string ConvertImageToBase64(string imagePath)
        {
            byte[] imageBytes = File.ReadAllBytes(imagePath);
            return Convert.ToBase64String(imageBytes);
        }

        private string[] ExtractStoryPaths(ZipArchive idmlFile)
        {
            return idmlFile.Entries
                .Where(entry => entry.FullName.StartsWith("Stories/") && entry.FullName.EndsWith(".xml"))
                .Select(entry => entry.FullName)
                .ToArray();
        }
    }
}
