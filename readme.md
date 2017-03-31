# RavenDB Data Correction Tool

The source code of the console application which uses thies functions is in Raven.DataCorrector project.

// Problem #1 and the exception
Not so long ago, when I was worked at TransPerfect we encountered a serious problem on one of our RavenDB instances. Some records couldn't be read from the Voron store and the logs contained the following exception,

```
System.IO.InvalidDataException: Failed to de-serialize metadata of document documentCollection/3c93ae96-aa62-42b0-afa3-4bb693cbcdd4 ---> System.IO.EndOfStreamException: Attempted to read past the end of the stream.
   at Raven.Abstractions.Extensions.StreamExtensions.ReadEtag(Stream stream) in c:\Builds\RavenDB-Stable-3.0\Raven.Abstractions\Extensions\StreamExtensions.cs:line 162
   at Raven.Database.Storage.Voron.StorageActions.DocumentsStorageActions.ReadDocumentMetadata(String normalizedKey, Slice sliceKey, Int32& size)
   --- End of inner exception stack trace ---
   at Raven.Database.Storage.Voron.StorageActions.DocumentsStorageActions.ReadDocumentMetadata(String normalizedKey, Slice sliceKey, Int32& size)
   at Raven.Database.Storage.Voron.StorageActions.DocumentsStorageActions.DocumentByKey(String key)
   at Raven.Database.Storage.Voron.StorageActions.DocumentsStorageActions.<GetDocumentsWithIdStartingWith>d__15.MoveNext()
   at Raven.Database.Actions.DocumentActions.<>c__DisplayClassf.<GetDocumentsWithIdStartingWith>b__d(IStorageActionsAccessor actions)
   at Raven.Storage.Voron.TransactionalStorage.ExecuteBatch(Action`1 action)
   at Raven.Storage.Voron.TransactionalStorage.Batch(Action`1 action)
   at Raven.Database.Actions.DocumentActions.GetDocumentsWithIdStartingWith(String idPrefix, String matches, String exclude, Int32 start, Int32 pageSize, CancellationToken token, Int32& nextStart, Action`1 addDoc, String transformer, Dictionary`2 transformerParameters, String skipAfter)
   at Raven.Database.Actions.DocumentActions.GetDocumentsWithIdStartingWith(String idPrefix, String matches, String exclude, Int32 start, Int32 pageSize, CancellationToken token, Int32& nextStart, String transformer, Dictionary`2 transformerParameters, String skipAfter)
   at Raven.Database.Server.Controllers.DocumentsController.DocsGet()
   at lambda_method(Closure , Object , Object[] )
   at System.Web.Http.Controllers.ReflectedHttpActionDescriptor.ActionExecutor.<>c__DisplayClass10.<GetExecutor>b__9(Object instance, Object[] methodParameters)
   at System.Web.Http.Controllers.ReflectedHttpActionDescriptor.ExecuteAsync(HttpControllerContext controllerContext, IDictionary`2 arguments, CancellationToken cancellationToken)
--- End of stack trace from previous location where exception was thrown ---
   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
   at System.Web.Http.Controllers.ApiControllerActionInvoker.<InvokeActionAsyncCore>d__0.MoveNext()
--- End of stack trace from previous location where exception was thrown ---
   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
   at System.Web.Http.Controllers.ActionFilterResult.<ExecuteAsync>d__2.MoveNext()
--- End of stack trace from previous location where exception was thrown ---
   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
   at System.Web.Http.Controllers.ExceptionFilterResult.<ExecuteAsync>d__0.MoveNext()
   at Raven.Client.Connection.Implementation.HttpJsonRequest.<CheckForErrorsAndReturnCachedResultIfAnyAsync>d__20.MoveNext() in c:\Builds\RavenDB-Stable-3.0\Raven.Client.Lightweight\Connection\Implementation\HttpJsonRequest.cs:line 451
--- End of stack trace from previous location where exception was thrown ---
   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
   at Raven.Client.Connection.Implementation.HttpJsonRequest.<>c__DisplayClasse.<<SendRequestInternal>b__d>d__10.MoveNext() in c:\Builds\RavenDB-Stable-3.0\Raven.Client.Lightweight\Connection\Implementation\HttpJsonRequest.cs:line 231
--- End of stack trace from previous location where exception was thrown ---
   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
   at Raven.Client.Connection.Implementation.HttpJsonRequest.<RunWithAuthRetry>d__15`1.MoveNext() in c:\Builds\RavenDB-Stable-3.0\Raven.Client.Lightweight\Connection\Implementation\HttpJsonRequest.cs:line 295
--- End of stack trace from previous location where exception was thrown ---
   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
   at Raven.Client.Connection.Implementation.HttpJsonRequest.<ReadResponseJsonAsync>d__9.MoveNext() in c:\Builds\RavenDB-Stable-3.0\Raven.Client.Lightweight\Connection\Implementation\HttpJsonRequest.cs:line 204
--- End of stack trace from previous location where exception was thrown ---
   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
   at Raven.Client.Connection.Async.AsyncServerClient.<>c__DisplayClass1a7.<<StartsWithAsync>b__1a2>d__1ad.MoveNext() in c:\Builds\RavenDB-Stable-3.0\Raven.Client.Lightweight\Connection\Async\AsyncServerClient.cs:line 1271
--- End of stack trace from previous location where exception was thrown ---
   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
   at Raven.Client.Connection.ReplicationInformerBase`1.<TryOperationAsync>d__29`1.MoveNext() in c:\Builds\RavenDB-Stable-3.0\Raven.Client.Lightweight\Connection\ReplicationInformerBase.cs:line 454
--- End of stack trace from previous location where exception was thrown ---
   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
   at Raven.Client.Connection.ReplicationInformerBase`1.<ExecuteWithReplicationAsync>d__19`1.MoveNext() in c:\Builds\RavenDB-Stable-3.0\Raven.Client.Lightweight\Connection\ReplicationInformerBase.cs:line 343
--- End of stack trace from previous location where exception was thrown ---
   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
   at Raven.Client.Connection.Async.AsyncServerClient.<ExecuteWithReplication>d__2c9`1.MoveNext() in c:\Builds\RavenDB-Stable-3.0\Raven.Client.Lightweight\Connection\Async\AsyncServerClient.cs:line 0
--- End of stack trace from previous location where exception was thrown ---
   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
   at Raven.Abstractions.Util.AsyncHelpers.<>c__DisplayClassb`1.<<RunSync>b__8>d__d.MoveNext() in c:\Builds\RavenDB-Stable-3.0\Raven.Abstractions\Util\AsyncHelpers.cs:line 82
--- End of stack trace from previous location where exception was thrown ---
   at System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()
   at Raven.Abstractions.Util.AsyncHelpers.RunSync[T](Func`1 task) in c:\Builds\RavenDB-Stable-3.0\Raven.Abstractions\Util\AsyncHelpers.cs:line 90
   at Raven.Client.Document.DocumentSession.LoadStartingWith[T](String keyPrefix, String matches, Int32 start, Int32 pageSize, String exclude, RavenPagingInformation pagingInformation, String skipAfter) in c:\Builds\RavenDB-Stable-3.0\Raven.Client.Lightweight\Document\DocumentSession.cs:line 1001
```

Thus indexes for the collection in subject where skipping the corrupt records and replication completely stopped, it was an operational database and we had to bring back up as soon as we could.

First thing we opened an issue in Raven google groups. Oren Eini responded, he immediately assumed that it was a hard disk failure (which we also think was the case) due to which the object was written to Voron store incompletely. He suggested to export all data and import it into a fresh RavenDB instance. Unfortunately exporting of data wasn't an option, since the exporter reads all records in the etag order and on failing to read a record it comes to halt. I had to come up with a way to make our database exportable so I started digging into the dark depths of RavenDB source code.

// Solution

By following the chain of calls in the exception stack I ended up in DocumentsStorageActions.ReadDocumentMetadata() method. It turned out that metadata weren't present for those documents, or were present partially and couldn't be deserialized. The only option that I had in mind then is to somehow force the metadata into the document and make it readable, DocumentsStorageActions.TouchDocument() turned out to be just that function that I needed. Normally it simply updates the documents etag, i.e. "touches" it without really updating anything. But in order to make it work for my case I had to slightly modify it, so that in case the metadata turned out to be unreadable my function would overwrite them with a new metadata object. I called my modification TouchCorruptDocumentPub(), where ending Pub signifies that the function is publicly available.

```
public void TouchCorruptDocumentPub(string key, out Etag preTouchEtag, out Etag afterTouchEtag, Etag seekAfterEtag)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException("key");

            var normalizedKey = CreateKey(key);
            var normalizedKeySlice = (Slice)normalizedKey;

            if (!tableStorage.Documents.Contains(Snapshot, normalizedKeySlice, writeBatch.Value))
            {
                if (logger.IsDebugEnabled)
                    logger.Debug("Document with dataKey='{0}' was not found", key);
                preTouchEtag = null;
                afterTouchEtag = null;
                return;
            }

            int _;
            JsonDocumentMetadata metadata;
            try
            {
                metadata = ReadDocumentMetadata(normalizedKey, normalizedKeySlice, out _);

                Console.WriteLine($"Metadata loaded for key '{key}', {metadata.Etag}");
            }
            catch (Exception)
            {
                Console.WriteLine($"Metadata failed to load for key '{key}'");
                Console.WriteLine($"Looking for an etag for key '{key}', starting at {seekAfterEtag ?? Etag.Empty}");

                metadata = new JsonDocumentMetadata()
                {
                    Etag = FindEtagByKey(normalizedKey, seekAfterEtag),
                    Key = key,
                    Metadata = new RavenJObject()
                };

                Console.WriteLine($"Found etag {metadata.Etag}");
            }

            var newEtag = uuidGenerator.CreateSequentialUuid(UuidType.Documents);
            Console.WriteLine($"Generated new etag {newEtag}");

            afterTouchEtag = newEtag;
            preTouchEtag = metadata.Etag;
            metadata.Etag = newEtag;

            WriteDocumentMetadata(metadata, normalizedKeySlice, shouldIgnoreConcurrencyExceptions: true);

            var keyByEtagIndex = tableStorage.Documents.GetIndex(Tables.Documents.Indices.KeyByEtag);

            keyByEtagIndex.Delete(writeBatch.Value, preTouchEtag);
            keyByEtagIndex.Add(writeBatch.Value, newEtag, normalizedKey);

            documentCacher.RemoveCachedDocument(normalizedKey, preTouchEtag);
            etagTouches.Add(preTouchEtag, afterTouchEtag);

            if (logger.IsDebugEnabled) { logger.Debug("TouchDocument() - document with key = '{0}'", key); }
        }
```

This fixed the problem for individual documents. Now I had to figure out how to fix all of the affected documents, so here comes the function that traverses the etag tree and attempts to load each corresponding document and it's metadata,

```
public IEnumerable<Tuple<Etag, string, bool, bool, Exception>> GetKeysAfterWithIdStartingWithPub(
            Etag etag, 
            int take = int.MaxValue,
            Etag untilEtag = null,
            TimeSpan? timeout = null,
            Reference<bool> earlyExit = null,
            Action<List<DocumentFetchError>> failedToGetHandler = null,
            bool includeMetadataCanBeReadFlag = false,
            bool includeDocumentCanBeReadFlag = false)
        {
            if (earlyExit != null)
                earlyExit.Value = false;
            if (take < 0)
                throw new ArgumentException("must have zero or positive value", "take");

            if (take == 0)
                yield break;

            if (string.IsNullOrEmpty(etag))
                throw new ArgumentNullException("etag");

            Stopwatch duration = null;
            if (timeout != null)
                duration = Stopwatch.StartNew();

            Etag lastDocEtag = null;
            using (var iterator = tableStorage.Documents.GetIndex(Tables.Documents.Indices.KeyByEtag)
                .Iterate(Snapshot, writeBatch.Value))
            {
                var slice = (Slice)etag.ToString();
                if (iterator.Seek(slice) == false)
                    yield break;

                if (iterator.CurrentKey.Equals(slice)) // need gt, not ge
                {
                    if (iterator.MoveNext() == false)
                        yield break;
                }
                int fetchedDocumentCount = 0;

                Etag docEtag = etag;

                var errors = new List<DocumentFetchError>();
                var skipDocumentGetErrors = failedToGetHandler != null;
                
                do
                {
                    docEtag = Etag.Parse(iterator.CurrentKey.ToString());

                    // We can skip many documents so the timeout should be at the start of the process to be executed.
                    if (timeout != null)
                    {
                        if (duration.Elapsed > timeout.Value)
                        {
                            if (earlyExit != null)
                                earlyExit.Value = true;
                            break;
                        }
                    }

                    if (untilEtag != null)
                    {
                        // This is not a failure, we are just ahead of when we expected to. 
                        if (EtagUtil.IsGreaterThan(docEtag, untilEtag))
                            break;
                    }

                    var key = GetKeyFromCurrent(iterator);

                    int metadataSize = -1;
                    int documentSize = -1;
                    var sliceKey = (Slice)key;
                    Exception exception = null;
                    if (includeDocumentCanBeReadFlag || includeMetadataCanBeReadFlag)
                    {
                        try
                        {
                            var metadata = ReadDocumentMetadata(key, sliceKey, out metadataSize);
                            if (includeDocumentCanBeReadFlag)
                            {
                                var @object = ReadDocumentData(key, sliceKey, metadata.Etag, metadata.Metadata, out documentSize);
                            }
                        }
                        catch (Exception ex)
                        {
                            exception = ex;
                            // ignored
                        }
                    }

                    yield return Tuple.Create(docEtag, key, metadataSize > 0, documentSize > 0, exception);

                    fetchedDocumentCount++;
                    if (fetchedDocumentCount >= take)
                    {
                        if (untilEtag != null && earlyExit != null)
                            earlyExit.Value = true;
                        break;
                    }
                } while (iterator.MoveNext());

                if (skipDocumentGetErrors && errors.Count > 0)
                {
                    failedToGetHandler(errors);
                }
            }
        }
```

it outputs a Tuple object with Item1 = Document etag, Item2 = Indication that metadata are present,  Item3 = Idication that document data are present, Item4 = The excpetion in case of an error.
The results could be stored in a csv or any other human readable document format. That helped to identify all broken records and later pass them into TouchCorruptDocumentPub() for correction.

This helped us to extract all data and then import them into a fresh instance like Oren suggested.

// Problem #2 and the exception

// Solution
