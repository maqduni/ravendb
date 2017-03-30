using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Raven.Abstractions.Data;
using Raven.Abstractions.MEF;
using Raven.Bundles.Compression.Plugin;
using Raven.Bundles.Encryption.Plugin;
using Raven.Bundles.Encryption.Settings;
using Raven.Database;
using Raven.Tests.Storage;
using Raven.Database.Config;
using Raven.Database.Plugins;
using Raven.Database.Storage;
using Raven.Database.Util;
using Raven.StorageExporter;
using Raven.Json.Linq;
using System.Configuration;
using System.Timers;
using Voron;
using System.Diagnostics;
using Raven.Database.Impl;
using Raven.Database.Server;
using Raven.Imports.Newtonsoft.Json;
using Microsoft.VisualBasic.FileIO;

namespace RunUnitTests
{
    class Program
    {
        static void MainAttempt1(string[] args)
        {
            var ravenConfiguration = new RavenConfiguration
            {
                DataDirectory = @"D:\raven3\Server\Databases\MyDatabase",
                CacheDocumentsInMemory = false,
                RunInMemory = false,
                Storage =
                {
                    PreventSchemaUpdate = true,
                    SkipConsistencyCheck = true
                }
            };

            var systemDatabase = new DocumentDatabase(ravenConfiguration, null);
            var database = new DocumentDatabase(ravenConfiguration, systemDatabase);
        }

        static void MainAttempt3(string[] args)
        {
            var ravenConfiguration = new RavenConfiguration
            {
                DataDirectory = ConfigurationManager.AppSettings["DataDirectory"],
                CacheDocumentsInMemory = false,
                RunInMemory = false,
                Storage =
                {
                    PreventSchemaUpdate = true,
                    SkipConsistencyCheck = true
                }
            };
            Console.WriteLine("Configuration initialized.");

            var ravendDbOptions = new RavenDBOptions(ravenConfiguration);
        }

        static void Main(string[] args)
        {
            var ravenConfiguration = new RavenConfiguration
            {
                DataDirectory = ConfigurationManager.AppSettings["DataDirectory"],
                CacheDocumentsInMemory = false,
                RunInMemory = false,
                Storage =
                {
                    PreventSchemaUpdate = true,
                    SkipConsistencyCheck = true
                }
            };
            Console.WriteLine("Configuration initialized.");

            var encryptionKey = ConfigurationManager.AppSettings["Raven/Encryption/Key"];
            if (!string.IsNullOrWhiteSpace(encryptionKey))
            {
                Encryption = new EncryptionConfiguration()
                {
                    EncryptIndexes = ConfigurationManager.AppSettings["Raven/Encryption/EncryptIndexes"].Equals("True")
                };

                if (Encryption.TrySavingEncryptionKey(encryptionKey) == false)
                {
                    ConsoleUtils.ConsoleWriteLineWithColor(ConsoleColor.Red, "Encryption key should be in base64 string format, we got {0}.\n", encryptionKey);
                }

                var algorithmType = ConfigurationManager.AppSettings["Raven/Encryption/Algorithm"];
                if (Encryption.TrySavingAlgorithmType(algorithmType) == false)
                {
                    ConsoleUtils.ConsoleWriteLineWithColor(ConsoleColor.Red, "Unknown encryption algorithm type, we got {0}.\n", algorithmType);
                }

                var preferedEncryptionKeyBitsSize = ConfigurationManager.AppSettings["Raven/Encryption/KeyBitsPreference"];
                if (Encryption.SavePreferedEncryptionKeyBitsSize(preferedEncryptionKeyBitsSize) == false)
                {
                    ConsoleUtils.ConsoleWriteLineWithColor(ConsoleColor.Red, "Encryption key bit size should be in an int, we got {0}.\n", preferedEncryptionKeyBitsSize);
                }
                Console.WriteLine("Encryption configured");
            }

            CreateTransactionalStorage(ravenConfiguration);
            Console.WriteLine("Transactional storage created.");

            try
            {
                Etag seekAfterEtag = ParseEtag(ConfigurationManager.AppSettings["DocumentTouchCorruptSeekAfterEtag"]),
                    preTouchEtag,
                    afterTouchEtag;

                // Action info
                string[] actionAndTimes = ConfigurationManager.AppSettings["ActionAndTimes"].Split(':');
                string action = actionAndTimes[0];
                int actionTimes = int.TryParse(actionAndTimes.Length > 1 ? actionAndTimes[1] : "1", out actionTimes) ? actionTimes : 1;

                // Document key and tag info
                Dictionary<string, Dictionary<string, string>> documentKeyAndEtagJson = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText(ConfigurationManager.AppSettings["DocumentKeyAndEtagJsonPath"]));
                Dictionary<string, List<KeyValuePair<string, Etag>>> documentKeyAndEtagCasted = new Dictionary<string, List<KeyValuePair<string, Etag>>>();

                foreach (var keyAndEtagDic in documentKeyAndEtagJson)
                {
                    documentKeyAndEtagCasted.Add(keyAndEtagDic.Key, keyAndEtagDic.Value.Select(k =>
                    {
                        string documentKey = k.Value;
                        Etag etag = ParseEtag(k.Key);

                        return new KeyValuePair<string, Etag>(documentKey, etag);
                    }).ToList());
                }

                var documentKeyAndEtag = documentKeyAndEtagCasted[action];

                switch (action)
                {
                    case "doc_json":
                        /**
                         * Try json document
                         */

                        var jsonDocumentDic = new Dictionary<string, JsonDocument>();
                        var metadataOnly = ConfigurationManager.AppSettings["DocumentsJsonMetadataOnly"] == "true";
                        var outputFields = ConfigurationManager.AppSettings["DocumentsJsonOutputFields"].Split(';');

                        foreach (var keyValuePair in documentKeyAndEtag)
                        {
                            storage.Batch(a =>
                            {
                                try
                                {
                                    JsonDocument jsonDocument;
                                    if (metadataOnly)
                                    {
                                        var metadata = a.Documents.DocumentMetadataByKey(keyValuePair.Key);

                                        jsonDocument = new JsonDocument()
                                        {
                                            DataAsJson = RavenJObject.FromObject(new object()),
                                            Metadata = metadata.Metadata,
                                            Etag = metadata.Etag,
                                            LastModified = metadata.LastModified,
                                            Key = metadata.Key
                                        };
                                    }
                                    else
                                    {
                                        jsonDocument = a.Documents.DocumentByKey(keyValuePair.Key);
                                    }
                                    
                                    Console.WriteLine($"Key: {jsonDocument}, Etag: {jsonDocument.Etag}");
                                    Console.WriteLine($"Metadata: {jsonDocument.Metadata}");
                                    Console.WriteLine("");

                                    jsonDocumentDic.Add(keyValuePair.Key, jsonDocument);
                                }
                                catch (Exception ex)
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine($"Key: {keyValuePair.Key}, Exception: {ex}");
                                    Console.ResetColor();

                                    jsonDocumentDic.Add(keyValuePair.Key, new JsonDocument()
                                    {
                                        DataAsJson = RavenJObject.FromObject(ex),
                                        Metadata = new RavenJObject()
                                    });
                                }
                            });
                        }

                        var jsonDocumentDicSerialized = jsonDocumentDic.Select(d =>
                        {
                            var key = d.Key;
                            //var dataAsJson = d.Value.DataAsJson.ToString();
                            //var metadata = d.Value.Metadata.ToString();
                            //var lastModified = d.Value.LastModified.ToString();
                            var objDictionary = new Dictionary<string, string>()
                            {
                                { "DataAsJson", d.Value.DataAsJson.ToString() },
                                { "Metadata",  d.Value.Metadata.ToString() },
                                { "LastModified", d.Value.LastModified.ToString() }
                            }
                            .Where(f => outputFields.Contains(f.Key))
                            .ToDictionary(f => f.Key, f => f.Value);

                            return new KeyValuePair<string, Dictionary<string, string>>(key, objDictionary);
                        }).ToDictionary(d => d.Key, d => d.Value);


                        var newJsonName = string.Format(ConfigurationManager.AppSettings["DocumentsJsonPath"], DateTime.Now.ToString("s").Replace(':', '_'));
                        File.WriteAllText(newJsonName, JsonConvert.SerializeObject(jsonDocumentDicSerialized));

                        break;
                    case "doc_touch_corrupt":
                        /**
                         * Try deleting the document
                         */

                        foreach (var keyValuePair in documentKeyAndEtag)
                        {
                            for (int i = 0; i < actionTimes; i++)
                            {
                                storage.Batch(a =>
                                {
                                    a.Documents.TouchCorruptDocumentPub(keyValuePair.Key, out preTouchEtag, out afterTouchEtag, seekAfterEtag);
                                    Console.WriteLine($"preTouchEtag: {preTouchEtag}, afterTouchEtag: {afterTouchEtag}");
                                });
                            }
                        }

                        break;
                    case "doc_touch":
                        /**
                         * Try deleting the document
                         */

                        foreach (var keyValuePair in documentKeyAndEtag)
                        {
                            for (int i = 0; i < actionTimes; i++)
                            {
                                storage.Batch(a =>
                                {
                                    a.Documents.TouchDocument(keyValuePair.Key, out preTouchEtag, out afterTouchEtag);
                                    Console.WriteLine($"preTouchEtag: {preTouchEtag}, afterTouchEtag: {afterTouchEtag}");
                                });
                            }
                        }

                        break;
                    case "doc_delete":
                        /**
                         * Try deleting the document
                         */
                        RavenJObject outMetadata;
                        Etag outEtag;

                        foreach (var keyValuePair in documentKeyAndEtag)
                        {
                            for (int i = 0; i < actionTimes; i++)
                            {
                                storage.Batch(a =>
                                {
                                    Console.WriteLine($"Deleting document by key: '{keyValuePair.Key}'");
                                    a.Documents.DeleteDocument(keyValuePair.Key, null, out outMetadata, out outEtag);
                                    Console.WriteLine($"outEtag: {outEtag}, outMetadata: {outMetadata}");
                                });
                            }
                        }

                        break;
                    case "doc_update_metadata":
                        /**
                         * Trying to overwrite the metadata with empty metadata
                         */

                        foreach (var keyValuePair in documentKeyAndEtag)
                        {
                            JsonDocumentMetadata metadata = new JsonDocumentMetadata()
                            {
                                Etag = keyValuePair.Value,
                                Key = keyValuePair.Key,
                                Metadata = new RavenJObject()
                            };
                            storage.Batch(a => a.Documents.WriteDocumentMetadataPub(metadata, metadata.Key, true));
                        }

                        break;
                    case "documents_key_etag":
                        /**
                         * Get the table of keys and tags for each document
                         */
                        storage.Batch(a =>
                        {
                            var writeCount = 0;

                            var newFileName = string.Format(ConfigurationManager.AppSettings["DocumentsKeyEtagPath"], DateTime.Now.ToString("s").Replace(':', '_'));
                            var outputCorruptOnly = ConfigurationManager.AppSettings["DocumentsKeyEtagCorruptOnly"] == "true";
                            var outputCorruptIndicators = ConfigurationManager.AppSettings["DocumentsKeyEtagOutputCorruptIndicators"] == "true";
                            Etag startAtEtag = ParseEtag(ConfigurationManager.AppSettings["DocumentsKeyEtagStartAtEtag"]);

                            int take = ParseInt(ConfigurationManager.AppSettings["DocumentsKeyEtagTake"], int.MaxValue, "Max");

                            var timer = Stopwatch.StartNew();
                            using (var writer = new StreamWriter(newFileName))
                            {
                                var templateHeader = outputCorruptIndicators
                                    ? "Etag, Metadata, Document, DocumentKey, Exception"
                                    : "Etag, Document key";

                                // Write column headers
                                writer.WriteLine(templateHeader);

                                var keyEtags = a.Documents.GetKeysAfterWithIdStartingWithPub(startAtEtag, take, includeMetadataCanBeReadFlag: outputCorruptIndicators, includeDocumentCanBeReadFlag: outputCorruptIndicators);
                                foreach (var etagTuple in keyEtags)
                                {
                                    if (writeCount >= 10000)
                                    {
                                        Console.Write(".");
                                        writeCount = 0;
                                    }
                                    writeCount++;

                                    //Console.WriteLine($"{keyValuePair.Key} - {keyValuePair.Value}");
                                    if (outputCorruptOnly && etagTuple.Item5 == null)
                                        continue;

                                    var templateLine = outputCorruptIndicators
                                        ? $"{etagTuple.Item1}, {etagTuple.Item3}, {etagTuple.Item4}, {etagTuple.Item2}, {JsonConvert.SerializeObject(etagTuple.Item5)}"
                                        : $"{etagTuple.Item1}, {etagTuple.Item2}";

                                    // Write lines
                                    writer.WriteLine(templateLine);
                                }

                                Console.WriteLine("");
                                Console.WriteLine($"Completed in {timer.Elapsed.ToString()}");
                                Console.WriteLine($"Generated file '{newFileName}'");
                            }
                        });

                        break;
                    case "compare_document_key_csvs":
                        var timer1 = Stopwatch.StartNew();

                        // Load the sets
                        List<HashSet<string>> hashSets = new List<HashSet<string>>();
                        for (int i = 0; i < 2; i++)
                        {
                            var fileConfig = ConfigurationManager.AppSettings[$"CompareDocumentsKeyCsvsPath{i+1}"].Split(',');
                            int columnNumber = int.TryParse(fileConfig.Length > 1 ? fileConfig[1] : "0", out columnNumber) ? columnNumber : 0;

                            var set = GetDocumentKeysFromCsv(fileConfig[0], columnNumber);
                            hashSets.Add(set);
                        }

                        // Compare the sets
                        List<IEnumerable<string>> diffs = new List<IEnumerable<string>>
                        {
                            hashSets[0].Except(hashSets[1]),
                            hashSets[1].Except(hashSets[0])
                        };

                        // Save the diffs
                        for (int i = 0; i < 2; i++)
                        {
                            var newFileName = string.Format(ConfigurationManager.AppSettings["CompareDocumentsKeyPath"], DateTime.Now.ToString("s").Replace(':', '_'), i == 0 ? "1to2" : "2to1");
                            var writeCount = 0;
                            using (var writer = new StreamWriter(newFileName))
                            {
                                // Write column headers
                                writer.WriteLine("Document key");

                                foreach (var diff in diffs[i])
                                {
                                    if (writeCount >= 10000)
                                    {
                                        Console.Write(".");
                                        writeCount = 0;
                                    }
                                    writeCount++;

                                    // Write lines
                                    writer.WriteLine(diff);
                                }
                            }

                            Console.WriteLine($"Generated file '{newFileName}'");
                        }
                        Console.WriteLine($"Completed in {timer1.Elapsed}");

                        break;
                    default:
                        Console.WriteLine("Allowed actions are: doc_delete, doc_update_metadata, documents_key_etag");
                        break;
                }

                Console.WriteLine("Operation succeeded.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                storage.Dispose();
            }

            Console.Read();
        }

        private static ITransactionalStorage storage;
        private static bool HasCompression = false;
        private static EncryptionConfiguration Encryption = null;
        private static SequentialUuidGenerator uuidGenerator;


        private static void CreateTransactionalStorage(InMemoryRavenConfiguration ravenConfiguration)
        {
            if (string.IsNullOrEmpty(ravenConfiguration.DataDirectory) == false && Directory.Exists(ravenConfiguration.DataDirectory))
            {
                try
                {
                    if (TryToCreateTransactionalStorage(ravenConfiguration, HasCompression, Encryption, out storage) == false)
                        ConsoleUtils.PrintErrorAndFail("Failed to create transactional storage");
                }
                catch (UnauthorizedAccessException uae)
                {
                    ConsoleUtils.PrintErrorAndFail(string.Format("Failed to initialize the storage it is probably been locked by RavenDB.\nError message:\n{0}", uae.Message), uae.StackTrace);
                }
                catch (InvalidOperationException ioe)
                {
                    ConsoleUtils.PrintErrorAndFail(string.Format("Failed to initialize the storage it is probably been locked by RavenDB.\nError message:\n{0}", ioe.Message), ioe.StackTrace);
                }
                catch (Exception e)
                {
                    ConsoleUtils.PrintErrorAndFail(e.Message, e.StackTrace);
                    return;
                }

                return;
            }

            ConsoleUtils.PrintErrorAndFail(string.Format("Could not detect storage file under the given directory:{0}", ravenConfiguration.DataDirectory));
        }

        public static bool TryToCreateTransactionalStorage(InMemoryRavenConfiguration ravenConfiguration,
            bool hasCompression, EncryptionConfiguration encryption, out ITransactionalStorage storage)
        {
            storage = null;
            if (File.Exists(Path.Combine(ravenConfiguration.DataDirectory, Voron.Impl.Constants.DatabaseFilename)))
                storage = ravenConfiguration.CreateTransactionalStorage(InMemoryRavenConfiguration.VoronTypeName, () => { }, () => { });
            else if (File.Exists(Path.Combine(ravenConfiguration.DataDirectory, "Data")))
                storage = ravenConfiguration.CreateTransactionalStorage(InMemoryRavenConfiguration.EsentTypeName, () => { }, () => { });

            if (storage == null)
                return false;

            var orderedPartCollection = new OrderedPartCollection<AbstractDocumentCodec>();
            if (encryption != null)
            {
                var documentEncryption = new DocumentEncryption();
                documentEncryption.SetSettings(new EncryptionSettings(encryption.EncryptionKey, encryption.SymmetricAlgorithmType,
                    encryption.EncryptIndexes, encryption.PreferedEncryptionKeyBitsSize));
                orderedPartCollection.Add(documentEncryption);
            }
            if (hasCompression)
            {
                orderedPartCollection.Add(new DocumentCompression());
            }

            uuidGenerator = new SequentialUuidGenerator();
            storage.Initialize(uuidGenerator, orderedPartCollection);

            /*
             * Added configuration steps
             */
            storage.Batch(actions =>
            {
                var nextIdentityValue = actions.General.GetNextIdentityValue("Raven/Etag");
                uuidGenerator.EtagBase = nextIdentityValue;
            });

            return true;
        }

        private static Etag ParseEtag(string strEtag)
        {
            Etag parsedEtag = strEtag == "Empty"
                ? Etag.Empty :
                Etag.TryParse(strEtag, out parsedEtag)
                    ? parsedEtag : null;
            return parsedEtag;
        }

        private static int ParseInt(string strInt, int defaultTo = 0, string deafultStr = "0")
        {
            int parsedInt = strInt == deafultStr
                ? defaultTo :
                int.TryParse(strInt, out parsedInt)
                    ? parsedInt : defaultTo;
            return parsedInt;
        }

        public static HashSet<string> GetDocumentKeysFromCsv(string path, int columnNumber = 0)
        {
            Console.WriteLine($"Reading file '{path}'");
            var documentKeys = new HashSet<string>();

            using (TextFieldParser csvParser = new TextFieldParser(path))
            {
                var writeCount = 0;

                csvParser.CommentTokens = new string[] { "#" };
                csvParser.SetDelimiters(new string[] { "," });
                csvParser.HasFieldsEnclosedInQuotes = true;

                // Skip the row with the column names
                csvParser.ReadLine();

                while (!csvParser.EndOfData)
                {
                    if (writeCount >= 10000)
                    {
                        Console.Write(".");
                        writeCount = 0;
                    }
                    writeCount++;

                    // Read current line fields, pointer moves to the next line.
                    var fields = csvParser.ReadFields();
                    if (fields != null)
                        documentKeys.Add(fields[columnNumber]);
                }
            }

            Console.WriteLine("");
            return documentKeys;
        }
    }
}
