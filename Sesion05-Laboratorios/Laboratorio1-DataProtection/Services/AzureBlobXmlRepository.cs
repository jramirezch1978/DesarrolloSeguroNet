using Microsoft.AspNetCore.DataProtection.Repositories;
using Azure.Storage.Blobs;
using System.Xml.Linq;
using System.Text;

namespace DevSeguroWebApp.Services
{
    public class AzureBlobXmlRepository : IXmlRepository
    {
        private readonly BlobClient _blobClient;
        private readonly ILogger<AzureBlobXmlRepository> _logger;

        public AzureBlobXmlRepository(BlobClient blobClient, ILogger<AzureBlobXmlRepository> logger)
        {
            _blobClient = blobClient ?? throw new ArgumentNullException(nameof(blobClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IReadOnlyCollection<XElement> GetAllElements()
        {
            try
            {
                _logger.LogDebug("Retrieving Data Protection keys from Azure Blob Storage");

                if (!_blobClient.Exists())
                {
                    _logger.LogInformation("Data Protection keys blob does not exist yet");
                    return Array.Empty<XElement>();
                }

                using var stream = _blobClient.OpenRead();
                using var reader = new StreamReader(stream, Encoding.UTF8);
                var content = reader.ReadToEnd();

                if (string.IsNullOrWhiteSpace(content))
                {
                    _logger.LogInformation("Data Protection keys blob is empty");
                    return Array.Empty<XElement>();
                }

                var document = XDocument.Parse(content);
                var elements = document.Root?.Elements().ToList() ?? new List<XElement>();

                _logger.LogInformation("Retrieved {Count} Data Protection keys from Azure Blob Storage", elements.Count);
                return elements;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Data Protection keys from Azure Blob Storage");
                return Array.Empty<XElement>();
            }
        }

        public void StoreElement(XElement element, string friendlyName)
        {
            try
            {
                _logger.LogDebug("Storing Data Protection key to Azure Blob Storage: {FriendlyName}", friendlyName);

                // Obtener elementos existentes
                var existingElements = GetAllElements().ToList();

                // Agregar el nuevo elemento
                existingElements.Add(element);

                // Crear documento XML completo
                var document = new XDocument(
                    new XDeclaration("1.0", "utf-8", null),
                    new XElement("repository", existingElements)
                );

                // Subir a Azure Blob Storage
                using var stream = new MemoryStream();
                using var writer = new StreamWriter(stream, Encoding.UTF8);
                document.Save(writer);
                writer.Flush();
                stream.Position = 0;

                _blobClient.Upload(stream, overwrite: true);

                _logger.LogInformation("Successfully stored Data Protection key to Azure Blob Storage: {FriendlyName}", friendlyName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing Data Protection key to Azure Blob Storage: {FriendlyName}", friendlyName);
                throw;
            }
        }
    }
} 