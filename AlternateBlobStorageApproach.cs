using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.Linq;
using System.Collections.Generic;

namespace PlaceholderNamespace
{
    class Program
    {
        static void Alternative(string[] args)
        {
            Resume_Template_A();
        }

        public static void Resume_Template_A()
        {
            string sasToken = "PLACEHOLDER_SAS_TOKEN";
            string storageAccount = "PLACEHOLDER_STORAGE_ACCOUNT";
            string containerName = "PLACEHOLDER_CONTAINER_NAME";
            string blobName = "PLACEHOLDER_BLOB_NAME";

            string[] oldStoryMappings = {
                "PLACEHOLDER_OLD_MAPPING_1", "PLACEHOLDER_OLD_MAPPING_2",
                "PLACEHOLDER_OLD_MAPPING_3", "PLACEHOLDER_OLD_MAPPING_4"
            };

            string[] newStoryMappings = {
                "PLACEHOLDER_NEW_MAPPING_1", "PLACEHOLDER_NEW_MAPPING_2",
                "PLACEHOLDER_NEW_MAPPING_3", "PLACEHOLDER_NEW_MAPPING_4"
            };

            Idml_Generator idml_Generator = new Idml_Generator();
            string[] imageBlobNames = { "PLACEHOLDER_IMAGE_NAME" };
            string[] spreadImageBase64 = idml_Generator.DownloadImagesAsBase64(sasToken, storageAccount, containerName, imageBlobNames);

            if (spreadImageBase64.Length > 0)
            {
                Console.WriteLine("Images downloaded successfully as base64.");
            }
            else
            {
                Console.WriteLine("Failed to download images.");
            }

            byte[] originalIdmlContent = idml_Generator.DownloadBlobContent(sasToken, storageAccount, containerName, blobName);
            if (originalIdmlContent != null)
            {
                byte[] updatedIdmlContent = idml_Generator.UpdateIdmlContentInMemory(originalIdmlContent, spreadImageBase64, oldStoryMappings, newStoryMappings);
                if (updatedIdmlContent != null)
                {
                    string updatedBlobName = "PLACEHOLDER_UPDATED_BLOB_NAME";
                    idml_Generator.UploadBlobContent(sasToken, storageAccount, containerName, updatedBlobName, updatedIdmlContent);
                    Console.WriteLine("Updated IDML file uploaded successfully.");
                }
                else
                {
                    Console.WriteLine("Failed to update IDML content.");
                }
            }
            else
            {
                Console.WriteLine("Failed to download the original IDML file.");
            }
        }
    }

    class Idml_Generator
    {
        public byte[] DownloadBlobContent(string sasToken, string storageAccount, string containerName, string blobName)
        {
            string requestUri = $"https://{storageAccount}.blob.core.windows.net/{containerName}/{blobName}?{sasToken}";

            try
            {
                using (WebClient webClient = new WebClient())
                {
                    return webClient.DownloadData(requestUri);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while downloading blob content: {ex.Message}");
                return null;
            }
        }

        public string[] DownloadImagesAsBase64(string sasToken, string storageAccount, string containerName, string[] blobNames)
        {
            List<string> base64Images = new List<string>();

            foreach (string blobName in blobNames)
            {
                string requestUri = $"https://{storageAccount}.blob.core.windows.net/{containerName}/{blobName}?{sasToken}";

                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requestUri);
                    request.Method = "GET";

                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    using (Stream responseStream = response.GetResponseStream())
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        responseStream.CopyTo(memoryStream);
                        byte[] imageContent = memoryStream.ToArray();
                        string base64Image = Convert.ToBase64String(imageContent);
                        base64Images.Add(base64Image);
                    }
                }
                catch (WebException ex)
                {
                    HttpWebResponse errorResponse = (HttpWebResponse)ex.Response;
                    if (errorResponse != null && errorResponse.StatusCode == HttpStatusCode.NotFound)
                    {
                        Console.WriteLine($"Blob '{blobName}' not found.");
                    }
                    else
                    {
                        Console.WriteLine($"An error occurred while downloading blob '{blobName}': {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred while downloading blob '{blobName}': {ex.Message}");
                }
            }

            return base64Images.ToArray();
        }

        public byte[] UpdateIdmlContentInMemory(byte[] originalIdmlContent, string[] spreadImageFilePaths, string[] oldStoryMappings, string[] newStoryMappings)
        {
            try
            {
                using (MemoryStream updatedIdmlZip = new MemoryStream())
                {
                    updatedIdmlZip.Write(originalIdmlContent, 0, originalIdmlContent.Length);
                    using (ZipArchive updatedZipFile = new ZipArchive(updatedIdmlZip, ZipArchiveMode.Update, true))
                    {
                        UpdateSpreadXml(updatedZipFile, spreadImageFilePaths);
                        string[] storyPaths = ExtractStoryPaths(updatedZipFile);

                        foreach (string storyPath in storyPaths)
                        {
                            UpdateStoryXml(updatedZipFile, storyPath, oldStoryMappings, newStoryMappings);
                        }
                    }

                    return updatedIdmlZip.ToArray();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while updating IDML content: {ex.Message}");
                return null;
            }
        }

        public void UploadBlobContent(string sasToken, string storageAccount, string containerName, string blobName, byte[] content)
        {
            string requestUri = $"https://{storageAccount}.blob.core.windows.net/{containerName}/{blobName}?{sasToken}";

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requestUri);
                request.Method = "PUT";
                request.ContentType = "application/octet-stream";
                request.ContentLength = content.Length;
                request.Headers.Add("x-ms-blob-type", "BlockBlob");

                using (Stream requestStream = request.GetRequestStream())
                {
                    requestStream.Write(content, 0, content.Length);
                }

                using (HttpWebResponse resp = (HttpWebResponse)request.GetResponse())
                {
                    if (resp.StatusCode == HttpStatusCode.OK)
                    {
                        Console.WriteLine("Blob content uploaded successfully.");
                    }
                    else
                    {
                        Console.WriteLine($"Failed to upload blob content. Status code: {resp.StatusCode}");
                    }
                }
            }
            catch (WebException ex)
            {
                HttpWebResponse response = (HttpWebResponse)ex.Response;
                if (response != null)
                {
                    Console.WriteLine($"Failed to upload blob content. Status code: {response.StatusCode}");
                }
                else
                {
                    Console.WriteLine($"An error occurred while uploading blob content: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while uploading blob content: {ex.Message}");
            }
        }


        private void UpdateSpreadXml(ZipArchive updatedZipFile, string[] imageFilePaths)
        {
            int imageIndex = 0; // Index to track the current image path being used

            // Iterate through all entries in the zip archive
            foreach (var entry in updatedZipFile.Entries.ToList())
            {
                // Check if the entry is a spread file
                if (entry.FullName.StartsWith("Spreads/") && entry.FullName.EndsWith(".xml"))
                {
                    using (MemoryStream spreadMemoryStream = new MemoryStream())
                    {
                        // Read the contents of the spread entry into a memory stream
                        using (Stream spreadStream = entry.Open())
                        {
                            spreadStream.CopyTo(spreadMemoryStream);
                        }

                        spreadMemoryStream.Seek(0, SeekOrigin.Begin);
                        XDocument doc = XDocument.Load(spreadMemoryStream);
                        var contentElements = doc.Descendants("Contents").ToArray();

                        // Assign image file paths to content elements
                        for (int i = 0; i < contentElements.Length && imageIndex < imageFilePaths.Length; i++)
                        {
                            //string base64Content = ConvertImageToBase64(imageFilePaths[imageIndex]);
                            string base64Content = imageFilePaths[imageIndex];
                            contentElements[i].Value = base64Content;
                            imageIndex++; // Move to the next image path
                        }

                        // Save changes to spread XML
                        spreadMemoryStream.SetLength(0);
                        spreadMemoryStream.Position = 0;
                        doc.Save(spreadMemoryStream);

                        // Delete the original entry
                        entry.Delete();

                        // Create a new entry with the same name
                        var newEntry = updatedZipFile.CreateEntry(entry.FullName); // Create new entry
                        using (Stream entryStream = newEntry.Open())
                        {
                            spreadMemoryStream.Seek(0, SeekOrigin.Begin);
                            spreadMemoryStream.CopyTo(entryStream);
                        }
                    }
                }
            }
        }

        private void UpdateStoryXml(ZipArchive updatedZipFile, string storyPath, string[] oldMappings, string[] newMappings)
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

                        // Replace each old mapping with the corresponding new mapping
                        for (int j = 0; j < oldMappings.Length; j++)
                        {
                            string oldValue = oldMappings[j];
                            string newValue = newMappings[j];

                            // Check if the contentValue contains the oldValue
                            // before attempting to replace it
                            if (contentValue != null && contentValue.Contains(oldValue))
                            {
                                // Perform the replacement for all occurrences of oldValue
                                Console.WriteLine(contentValue);
                                contentValue = contentValue.Replace(oldValue, newValue);
                            }
                        }

                        // Update the contentElement with the modified contentValue
                        contentElement.Value = contentValue;
                    }
                }

                xmlStream.SetLength(0); // Clear existing content
                xmlStream.Position = 0; // Reset stream position

                xmlDoc.Save(xmlStream, SaveOptions.DisableFormatting);
            }
        }



        private string[] ExtractStoryPaths(ZipArchive idmlFile)
        {
            return idmlFile.Entries
                .Where(entry => entry.FullName.StartsWith("Stories/") && entry.FullName.EndsWith(".xml"))
                .Select(entry => entry.FullName)
                .ToArray();
        }

        private string ConvertImageToBase64(string imagePath)
        {
            byte[] imageBytes = File.ReadAllBytes(imagePath);
            return Convert.ToBase64String(imageBytes);
        }
    }
}
