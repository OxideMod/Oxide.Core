using Rebex.Net;
using Rebex.Security.Certificates;
using Rebex.Security.Cryptography;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using uMod.Plugins;

namespace uMod.Libraries
{
    /// <summary>
    /// Request methods for web requests
    /// </summary>
    public enum RequestMethod
    {
        /// <summary>
        /// Requsts deletion of the specified resource
        /// </summary>
        DELETE,

        /// <summary>
        /// Requests data from the specified resource
        /// </summary>
        GET,

        /// <summary>
        /// Applies partial modifications to the specified resource
        /// </summary>
        PATCH,

        /// <summary>
        /// Submits data to be processed to the specified resource
        /// </summary>
        POST,

        /// <summary>
        /// Uploads a representation of the specified URI
        /// </summary>
        PUT,

        /// <summary>
        /// Grabs only HTTP headers and no document body
        /// </summary>
        HEAD
    };

    /// <summary>
    /// The WebRequests library
    /// </summary>
    public class WebRequests : Library
    {
        private static readonly Covalence covalence = Interface.uMod.GetLibrary<Covalence>();

        private readonly AutoResetEvent workevent = new AutoResetEvent(false);
        private readonly Queue<WebRequest> queue = new Queue<WebRequest>();
        private readonly Thread workerthread;
        private readonly int maxCompletionPortThreads;
        private readonly int maxWorkerThreads;
        private readonly object syncRoot = new object();

        private bool shutdown;

        /// <summary>
        /// Specifies the default HTTP request timeout in seconds
        /// </summary>
        public static float DefaultTimeout = 30f;

        /// <summary>
        /// Specifies the HTTP request decompression support
        /// </summary>
        public static bool AllowDecompression = false;

        /// <summary>
        /// Represents a single WebRequest instance
        /// </summary>
        public class WebRequest
        {
            /// <summary>
            /// Gets the callback delegate
            /// </summary>
            public Action<int, string> Callback { get; }

            /// <summary>
            /// Gets the v2 callback delegate
            /// </summary>
            public Action<WebResponse> CallbackV2 { get; }

            /// <summary>
            /// Gets or sets the request timeout
            /// </summary>
            public float Timeout { get; set; }

            /// <summary>
            /// Gets or sets the web request method
            /// </summary>
            public string Method { get; set; }

            /// <summary>
            /// Gets the destination URL
            /// </summary>
            public string Url { get; }

            /// <summary>
            /// Gets or sets the request body
            /// </summary>
            public string Body { get; set; }

            /// <summary>
            /// Gets the HTTP response code
            /// </summary>
            public int ResponseCode { get; protected set; }

            /// <summary>
            /// Gets the HTTP response text
            /// </summary>
            public string ResponseText { get; protected set; }

            /// <summary>
            /// Gets the HTTP response object
            /// </summary>
            public WebResponse Response { get; protected set; }

            /// <summary>
            /// Gets the plugin to which this web request belongs, if any
            /// </summary>
            public Plugin Owner { get; protected set; }

            /// <summary>
            /// Gets or sets the web request headers
            /// </summary>
            public Dictionary<string, string> RequestHeaders { get; set; }

            private HttpRequest request;
            private WaitHandle waitHandle;
            private RegisteredWaitHandle registeredWaitHandle;
            private Event.Callback<Plugin, PluginManager> removedFromManager;

            /// <summary>
            /// Initializes a new instance of the WebRequest class
            /// </summary>
            /// <param name="url"></param>
            /// <param name="callback"></param>
            /// <param name="owner"></param>
            public WebRequest(string url, Action<int, string> callback, Plugin owner)
            {
                Url = url;
                Callback = callback;
                Owner = owner;
                removedFromManager = Owner?.OnRemovedFromManager.Add(owner_OnRemovedFromManager);
            }

            /// <summary>
            /// Initializes a new instance of the WebRequest class
            /// </summary>
            /// <param name="url"></param>
            /// <param name="owner"></param>
            /// <param name="callback"></param>
            public WebRequest(string url, Plugin owner, Action<WebResponse> callback) : this(url, null, owner)
            {
                CallbackV2 = callback;
            }

            /// <summary>
            /// Validates certificates for the web request
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="args"></param>
            private void ValidatingCertificate(object sender, SslCertificateValidationEventArgs args)
            {
                X509Chain chain = new X509Chain();

                // Alter how the chain is built/validated
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                chain.ChainPolicy.VerificationFlags = X509VerificationFlags.IgnoreWrongUsage;
                chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;

                // Build the new certificate chain
                Certificate primaryCert = args.CertificateChain[0];
#if DEBUG
                Interface.uMod.LogDebug($"{args.CertificateChain.Count} certificates in chain");
                Interface.uMod.LogDebug($"Primary common name: {primaryCert.GetCommonName()}");
                Interface.uMod.LogDebug($"  Thumbprint:  {primaryCert.Thumbprint}");
                Interface.uMod.LogDebug($"  Expires on:  {primaryCert.GetExpirationDate():d}");
                Interface.uMod.LogDebug($"  Key algorithm: {primaryCert.KeyAlgorithm}");
#endif

                // Add extra certificates to the chain
                foreach (Certificate cert in args.CertificateChain.Skip(1))
                {
#if DEBUG
                    Interface.uMod.LogDebug($"Extra common name: {cert.GetCommonName()}");
                    Interface.uMod.LogDebug($"  Thumbprint:  {cert.Thumbprint}");
                    Interface.uMod.LogDebug($"  Expires on:  {cert.GetExpirationDate():d}");
                    Interface.uMod.LogDebug($"  Key algorithm: {cert.KeyAlgorithm}");
#endif
                    chain.ChainPolicy.ExtraStore.Add(new X509Certificate2(cert.GetRawCertData()));
                }

                // Blindly accept ECDSA as they aren't able to be validated yet
                if (primaryCert.KeyAlgorithm == KeyAlgorithm.ECDsa || args.CertificateChain.Skip(1).Any(c => c.KeyAlgorithm == KeyAlgorithm.ECDsa))
                {
                    //Interface.uMod.LogWarning("Web request could not be completed as remote server certficiate only supports ECDSA");
                    args.Accept(); // TODO: We really should be validating ALL... hopefully we can fix this soon
                    return;
                }

                bool isValid = chain.Build(new X509Certificate2(primaryCert.GetRawCertData()));
                if (isValid)
                {
#if DEBUG
                    Interface.uMod.LogDebug("Certificate is valid, accepting");
#endif
                    args.Accept();
                    return;
                }

                // Reject certificate if not valid
                args.Reject();
            }

            /// <summary>
            /// Used by the worker thread to start the request
            /// </summary>
            public void Start()
            {
                try
                {
                    Rebex.Licensing.Key = "==Afeyns36vcis9o4KOHrZ0ZnCmXanJ7AoMz8O6rpvR3oc=="; // Expires: 11-25-18, TODO: Obfuscate production key

                    // Override the web request creator
                    HttpRequestCreator creator = new HttpRequestCreator();
                    creator.Register();

                    // Import NIST and Brainpool curves crypto
                    AsymmetricKeyAlgorithm.Register(EllipticCurveAlgorithm.Create);

                    // Import Curve25519 crypto
                    AsymmetricKeyAlgorithm.Register(Curve25519.Create);

                    // Import Ed25519 crypto
                    AsymmetricKeyAlgorithm.Register(Ed25519.Create);

                    // Override certificate validation (necessary for Mono)
                    creator.ValidatingCertificate += ValidatingCertificate;
                    //creator.ValidatingCertificate += (sender, args) => args.Accept();

                    // Create the web request
                    request = creator.Create(Url);
                    request.Method = Method;
                    request.Credentials = CredentialCache.DefaultCredentials;
                    request.KeepAlive = false;
                    request.Timeout = (int)Math.Round((Timeout.Equals(0f) ? DefaultTimeout : Timeout) * 1000f);
                    request.AutomaticDecompression = AllowDecompression ? DecompressionMethods.GZip | DecompressionMethods.Deflate : DecompressionMethods.None;

                    // Exclude loopback requests from IP address binding
                    if (!request.RequestUri.IsLoopback)
                    {
                        // Assign server's assigned IP address, not primary network adapter address
                        creator.SetSocketFactory(SimpleSocket.GetFactory(covalence.Server.LocalAddress ?? covalence.Server.Address));
                    }

                    // Optional request body for POST requests
                    byte[] data = new byte[0];
                    if (Body != null && !request.Method.Equals("HEAD"))
                    {
                        data = Encoding.UTF8.GetBytes(Body);
                        request.ContentLength = data.Length;
                        request.ContentType = "application/x-www-form-urlencoded";
                    }

                    if (RequestHeaders != null)
                    {
                        request.SetRawHeaders(RequestHeaders);
                    }

                    // Perform DNS lookup and connect (blocking)
                    if (data.Length > 0)
                    {
                        request.BeginGetRequestStream(result =>
                        {
                            if (request != null)
                            {
                                try
                                {
                                    // Write request body
                                    using (Stream stream = request.EndGetRequestStream(result))
                                    {
                                        stream.Write(data, 0, data.Length);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    ResponseText = FormatWebException(ex, ResponseText ?? string.Empty);
                                    if (Response == null)
                                    {
                                        Response = new WebResponse(ResponseText,
                                            Uri.TryCreate(Url, UriKind.Absolute, out Uri uri) ? uri : null,
                                            (RequestMethod)Enum.Parse(typeof(RequestMethod), request.Method));
                                    }

                                    request?.Abort();
                                    OnComplete();
                                    return;
                                }

                                WaitForResponse();
                            }
                        }, null);
                    }
                    else
                    {
                        WaitForResponse();
                    }
                }
                catch (Exception ex)
                {
                    ResponseText = FormatWebException(ex, ResponseText ?? string.Empty);
                    string message = $"Web request produced exception (Url: {Url})";
                    if (Owner)
                    {
                        message += $" in '{Owner.Name} v{Owner.Version}' plugin";
                    }

                    Interface.uMod.LogException(message, ex);

                    if (Response == null)
                    {
                        Response = new WebResponse(ResponseText,
                            Uri.TryCreate(Url, UriKind.Absolute, out Uri uri) ? uri : null,
                            (RequestMethod)Enum.Parse(typeof(RequestMethod), request.Method));
                    }

                    request?.Abort();
                    OnComplete();
                }
            }

            private void WaitForResponse()
            {
                IAsyncResult result = request.BeginGetResponse(res =>
                {
                    try
                    {
                        using (HttpResponse response = (HttpResponse)request.EndGetResponse(res))
                        {
                            Response = new WebResponse(response, Owner);
                            ResponseText = Response.ReadAsString();
                            ResponseCode = Response.StatusCode;
                        }
                    }
                    catch (WebException ex)
                    {
                        ResponseText = FormatWebException(ex, ResponseText ?? string.Empty);
                        HttpResponse response = ex.Response as HttpResponse;
                        if (response != null)
                        {
                            try
                            {
                                Response = new WebResponse(response, Owner);
                                ResponseCode = Response.StatusCode;
                                ResponseText = Response.ReadAsString();
                            }
                            catch (Exception)
                            {
                                if (Response == null)
                                {
                                    Response = new WebResponse(ResponseText,
                                        Uri.TryCreate(Url, UriKind.Absolute, out Uri uri) ? uri : null,
                                        (RequestMethod)Enum.Parse(typeof(RequestMethod), Method));
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ResponseText = FormatWebException(ex, ResponseText ?? string.Empty);
                        string message = $"Web request produced exception (Url: {Url})";
                        if (Owner)
                        {
                            message += $" in '{Owner.Name} v{Owner.Version}' plugin";
                        }

                        Interface.uMod.LogException(message, ex);
                        if (Response == null)
                        {
                            Response = new WebResponse(ResponseText, Uri.TryCreate(Url,
                                UriKind.Absolute, out Uri uri) ? uri : null,
                                (RequestMethod)Enum.Parse(typeof(RequestMethod), Method));
                        }
                    }

                    if (request != null)
                    {
                        request.Abort();
                        OnComplete();
                    }
                }, null);

                waitHandle = result.AsyncWaitHandle;
                registeredWaitHandle = ThreadPool.RegisterWaitForSingleObject(waitHandle, OnTimeout, null, request.Timeout, true);
            }

            private void OnTimeout(object state, bool timedOut)
            {
                if (timedOut)
                {
                    request?.Abort();
                }

                if (Owner != null)
                {
                    Event.Remove(ref removedFromManager);
                    Owner = null;
                }
            }

            private void OnComplete()
            {
                Event.Remove(ref removedFromManager);
                registeredWaitHandle?.Unregister(waitHandle);
                Interface.uMod.NextTick(() =>
                {
                    if (request != null)
                    {
                        request = null;
                        Owner?.TrackStart();
                        try
                        {
                            Callback?.Invoke(ResponseCode, ResponseText);
                            CallbackV2?.Invoke(Response);
                        }
                        catch (Exception ex)
                        {
                            string message = "Web request callback raised an exception";
                            if (Owner && Owner != null)
                            {
                                message += $" in '{Owner.Name} v{Owner.Version}' plugin";
                            }

                            Interface.uMod.LogException(message, ex);
                        }

                        Owner?.TrackEnd();
                        Owner = null;
                    }
                });
            }

            /// <summary>
            /// Called when the owner plugin was unloaded
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="manager"></param>
            private void owner_OnRemovedFromManager(Plugin sender, PluginManager manager)
            {
                if (request != null)
                {
                    HttpRequest outstandingRequest = request;
                    request = null;
                    outstandingRequest.Abort();
                }
            }
        }

        /// <summary>
        /// Represents a Response from a WebRequest
        /// </summary>
        public class WebResponse : IDisposable
        {
            // Holds the response data if any
            private MemoryStream responseStream;

            /// <summary>
            /// The Headers from the response
            /// </summary>
            public IDictionary<string, string> Headers { get; protected set; }

            /// <summary>
            /// The Content-Length returned from the response
            /// </summary>
            public virtual long ContentLength => responseStream?.Length ?? 0;

            /// <summary>
            /// Content-Type set by the Content-Type Header of the response
            /// </summary>
            public string ContentType { get; protected set; }

            /// <summary>
            /// Gets the Content-Encoding from the response
            /// </summary>
            public string ContentEncoding { get; protected set; }

            /// <summary>
            /// Gets the status code returned from the responding server
            /// </summary>
            public int StatusCode { get; protected set; }

            /// <summary>
            /// Gets information on the returned status code
            /// </summary>
            public string StatusDescription { get; protected set; }

            /// <summary>
            /// The original method used to get this response
            /// </summary>
            public RequestMethod Method { get; protected set; }

            /// <summary>
            /// Gets the Uri of the responding server
            /// </summary>
            public Uri ResponseUri { get; protected set; }

            /// <summary>
            /// Gets the HTTP protocol version
            /// </summary>
            public VersionNumber ProtocolVersion { get; protected set; }

            protected WebResponse(HttpResponse response, Plugin Owner = null)
            {
                // Make sure we aren't creating an empty response
                if (response == null)
                {
                    throw new ArgumentNullException(nameof(response), "A WebResponse cannot be created from an null HttpResponse");
                }

                // Verify the original Request Method
                switch (response.Method.ToUpper())
                {
                    case "DELETE":
                        Method = RequestMethod.DELETE;
                        break;

                    case "GET":
                        Method = RequestMethod.GET;
                        break;

                    case "HEAD":
                        Method = RequestMethod.HEAD;
                        break;

                    case "PATCH":
                        Method = RequestMethod.PATCH;
                        break;

                    case "POST":
                        Method = RequestMethod.POST;
                        break;

                    case "PUT":
                        Method = RequestMethod.PUT;
                        break;

                    default:
                        throw new InvalidOperationException($"Unknown request method was defined '{response.Method}'");
                }

                ContentType = response.ContentType;
                ResponseUri = response.ResponseUri;
                StatusCode = (int)response.StatusCode;
                StatusDescription = response.StatusDescription;
                ProtocolVersion = new VersionNumber(response.ProtocolVersion?.Major ?? 1, response.ProtocolVersion?.Minor ?? 1, response.ProtocolVersion?.Revision ?? 0);
                ContentEncoding = response.ContentEncoding;

                if (response.Headers != null)
                {
                    Headers = new Dictionary<string, string>();

                    for (int h = 0; h < response.Headers.Count; h++)
                    {
                        string key = response.Headers.GetKey(h);
                        string[] values = response.Headers.GetValues(h);

                        if (values != null && values.Length > 0 && !Headers.ContainsKey(key))
                        {
                            Headers.Add(key, string.Join(";", values));
                        }
                    }
                }

                if (Method != RequestMethod.HEAD)
                {
                    try
                    {
                        using (Stream stream = response.GetResponseStream())
                        {
                            responseStream = new MemoryStream();

                            byte[] responseCache = new byte[256];
                            int currentBytes;

                            while (stream != null && (currentBytes = stream.Read(responseCache, 0, 256)) != 0)
                            {
                                responseStream.Write(responseCache, 0, currentBytes);
                                responseCache = new byte[256];
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        string message = $"Web request produced exception (Url: {ResponseUri?.AbsoluteUri})";
                        if (Owner)
                        {
                            message += $" in '{Owner.Name} v{Owner.Version}' plugin";
                        }

                        Interface.uMod.LogException(message, ex);
                    }
                }
            }

            internal WebResponse(System.Net.WebResponse response, Plugin Owner = null) : this((HttpResponse)response, Owner)
            {
            }

            internal WebResponse(string errorText, Uri originalUri, RequestMethod method)
            {
                StatusDescription = errorText;
            }

            /// <summary>
            /// Reads the Response in it's raw data
            /// </summary>
            /// <returns></returns>
            public byte[] ReadAsBytes() => ContentLength != 0 ? responseStream.ToArray() : new byte[0];

            /// <summary>
            /// Reads the response as a string
            /// </summary>
            /// <param name="encoding"></param>
            /// <returns></returns>
            public string ReadAsString(Encoding encoding = null) => ContentLength != 0 ? encoding?.GetString(ReadAsBytes()) ?? Encoding.UTF8.GetString(ReadAsBytes()) : null;

            /// <summary>
            /// Converts the response string from Json to a .NET Object
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="encoding"></param>
            /// <returns></returns>
            public T ConvertFromJson<T>(Encoding encoding = null) => ContentLength != 0 ? Utility.ConvertFromJson<T>(ReadAsString(encoding)) : default(T);

            /// <summary>
            /// Safely disposes this object
            /// </summary>
            public virtual void Dispose()
            {
                responseStream?.Dispose();
                responseStream = null;
                Headers?.Clear();
                Headers = null;

                ContentType = null;
                ContentEncoding = null;
                ResponseUri = null;
                StatusDescription = null;
            }
        }

        /// <summary>
        /// Formats given WebException to string
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        public static string FormatWebException(Exception exception, string response)
        {
            if (!string.IsNullOrEmpty(response))
            {
                response += Environment.NewLine;
            }

            response += exception.Message; // TODO: Fix duplicate messages

            if (exception.InnerException != null && !response.Equals(exception.InnerException.Message))
            {
                response = FormatWebException(exception.InnerException, response);
            }

            return response;
        }

        /// <summary>
        /// Initializes a new instance of the WebRequests library
        /// </summary>
        public WebRequests()
        {
            // Initialize SSL
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            ServicePointManager.DefaultConnectionLimit = 200;

            ThreadPool.GetMaxThreads(out maxWorkerThreads, out maxCompletionPortThreads);
            maxCompletionPortThreads = (int)(maxCompletionPortThreads * 0.6);
            maxWorkerThreads = (int)(maxWorkerThreads * 0.75);

            // Start worker thread
            workerthread = new Thread(Worker);
            workerthread.Start();
        }

        /// <summary>
        /// Shuts down the worker thread
        /// </summary>
        public override void Shutdown()
        {
            if (!shutdown)
            {
                shutdown = true;
                workevent.Set();
                Thread.Sleep(250);
                workerthread.Abort();
            }
        }

        /// <summary>
        /// The worker thread method
        /// </summary>
        private void Worker()
        {
            try
            {
                while (!shutdown)
                {
                    int workerThreads, completionPortThreads;
                    ThreadPool.GetAvailableThreads(out workerThreads, out completionPortThreads);
                    if (workerThreads <= maxWorkerThreads || completionPortThreads <= maxCompletionPortThreads)
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    WebRequest request = null;
                    lock (syncRoot)
                    {
                        if (queue.Count > 0)
                        {
                            request = queue.Dequeue();
                        }
                    }

                    if (request != null)
                    {
                        request.Start();
                    }
                    else
                    {
                        workevent.WaitOne();
                    }
                }
            }
            catch (Exception ex)
            {
                Interface.uMod.LogException("WebRequests worker: ", ex);
            }
        }

        /// <summary>
        /// Enqueues a DELETE, GET, PATCH, POST, HEAD, or PUT web request
        /// </summary>
        /// <param name="url"></param>
        /// <param name="body"></param>
        /// <param name="callback"></param>
        /// <param name="owner"></param>
        /// <param name="method"></param>
        /// <param name="headers"></param>
        /// <param name="timeout"></param>
        [LibraryFunction("Enqueue")]
        public void Enqueue(string url, string body, Action<int, string> callback, Plugin owner, RequestMethod method = RequestMethod.GET, Dictionary<string, string> headers = null, float timeout = 0f)
        {
            WebRequest request = new WebRequest(url, callback, owner) { Method = method.ToString(), RequestHeaders = headers, Timeout = timeout, Body = body };
            lock (syncRoot)
            {
                queue.Enqueue(request);
            }
            workevent.Set();
        }

        /// <summary>
        /// Enqueues a DELETE, GET, PATCH, POST, HEAD, or PUT web request
        /// </summary>
        /// <param name="url"></param>
        /// <param name="body"></param>
        /// <param name="callback"></param>
        /// <param name="owner"></param>
        /// <param name="method"></param>
        /// <param name="headers"></param>
        /// <param name="timeout"></param>
        [LibraryFunction("EnqueueV2")]
        public void Enqueue(string url, string body, Action<WebResponse> callback, Plugin owner, RequestMethod method = RequestMethod.GET, Dictionary<string, string> headers = null, float timeout = 0f)
        {
            WebRequest request = new WebRequest(url, owner, callback) { Method = method.ToString(), RequestHeaders = headers, Timeout = timeout, Body = body };
            lock (syncRoot)
            {
                queue.Enqueue(request);
            }
            workevent.Set();
        }

        /// <summary>
        /// Returns the current queue length
        /// </summary>
        /// <returns></returns>
        [LibraryFunction("GetQueueLength")]
        public int GetQueueLength() => queue.Count;
    }

    // HttpRequest extensions to add raw header support
    public static class HttpRequestExtensions
    {
        /// <summary>
        /// Headers that require modification via a property
        /// </summary>
        private static readonly string[] RestrictedHeaders = {
            "Accept",
            "Connection",
            "Content-Length",
            "Content-Type",
            "Date",
            "Expect",
            "Host",
            "If-Modified-Since",
            "Keep-Alive",
            "Proxy-Connection",
            "Range",
            "Referer",
            "Transfer-Encoding",
            "User-Agent"
        };

        /// <summary>
        /// Dictionary of all of the header properties
        /// </summary>
        private static readonly Dictionary<string, PropertyInfo> HeaderProperties = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initialize the HeaderProperties dictionary
        /// </summary>
        static HttpRequestExtensions()
        {
            Type type = typeof(HttpRequest);
            foreach (string header in RestrictedHeaders)
            {
                HeaderProperties[header] = type.GetProperty(header.Replace("-", ""));
            }
        }

        /// <summary>
        /// Sets raw HTTP request headers
        /// </summary>
        /// <param name="request">Request object</param>
        /// <param name="headers">Dictionary of headers to set</param>
        public static void SetRawHeaders(this WebRequest request, Dictionary<string, string> headers)
        {
            foreach (KeyValuePair<string, string> keyValPair in headers)
            {
                request.SetRawHeader(keyValPair.Key, keyValPair.Value);
            }
        }

        /// <summary>
        /// Sets a raw HTTP request header
        /// </summary>
        /// <param name="request">Request object</param>
        /// <param name="name">Name of the header</param>
        /// <param name="value">Value of the header</param>
        public static void SetRawHeader(this WebRequest request, string name, string value)
        {
            if (HeaderProperties.ContainsKey(name))
            {
                PropertyInfo property = HeaderProperties[name];
                if (property.PropertyType == typeof(DateTime))
                {
                    property.SetValue(request, DateTime.Parse(value), null);
                }
                else if (property.PropertyType == typeof(bool))
                {
                    property.SetValue(request, bool.Parse(value), null);
                }
                else if (property.PropertyType == typeof(long))
                {
                    property.SetValue(request, long.Parse(value), null);
                }
                else
                {
                    property.SetValue(request, value, null);
                }
            }
            else
            {
                request.Headers[name] = value;
            }
        }
    }
}

// SimpleSocket implementation (c) Rebex and does not fall under this project's license
// TODO: Clean up SimpleSocket to more closely match project, maybe move?
namespace Rebex.Net
{
    public class SimpleSocket : ISocket
    {
        private class SimpleSocketFactory : ISocketFactory
        {
            private readonly IPEndPoint _localEndPoint;

            public SimpleSocketFactory(IPEndPoint localEndPoint)
            {
                _localEndPoint = localEndPoint;
            }

            public ISocket CreateSocket()
            {
                return new SimpleSocket(this, _localEndPoint);
            }
        }

        public static ISocketFactory GetFactory(IPAddress localEndPoint)
        {
            return new SimpleSocketFactory(new IPEndPoint(localEndPoint, 0));
        }

        private class SyncResult : IAsyncResult
        {
            private readonly ManualResetEvent _resetEvent = new ManualResetEvent(true);

            public SyncResult(object state, object result)
            {
                AsyncState = state;
                AsyncResult = result;
            }

            public object AsyncState { get; }

            public object AsyncResult { get; }

            public WaitHandle AsyncWaitHandle => _resetEvent;

            public bool CompletedSynchronously => true;

            public bool IsCompleted => true;
        }

        private readonly Socket _socket;
        private readonly SimpleSocketFactory _factory;

        private SimpleSocket(SimpleSocketFactory factory, Socket socket)
        {
            _factory = factory;
            _socket = socket;
        }

        private SimpleSocket(SimpleSocketFactory factory, IPEndPoint localEndPoint)
        {
            _factory = factory;
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.Bind(localEndPoint);
        }

        public ISocketFactory Factory => _factory;

        public int Timeout
        {
            get => 0;
            set { }
        }

        public int Available => _socket.Available;

        public bool Connected => _socket.Connected;

        public EndPoint LocalEndPoint => _socket.LocalEndPoint;

        public EndPoint RemoteEndPoint => _socket.RemoteEndPoint;

        public SocketState GetConnectionState()
        {
            if (!Connected)
            {
                return SocketState.NotConnected;
            }

            if (!_socket.Poll(100, SelectMode.SelectRead))
            {
                return SocketState.Connected;
            }

            if (_socket.Available > 0)
            {
                return SocketState.Connected;
            }

            return SocketState.NotConnected;
        }

        public bool Poll(int microSeconds, SocketSelectMode mode)
        {
            return _socket.Poll(microSeconds, (SelectMode)mode);
        }

        public void Connect(EndPoint remoteEP)
        {
            _socket.Connect(remoteEP);
        }

        public void Connect(string serverName, int serverPort)
        {
            IPHostEntry hostEntry = Dns.GetHostEntry(serverName);
            IPEndPoint endPoint = ProxySocket.ToEndPoint(hostEntry, serverPort);
            _socket.Connect(endPoint);
        }

        public IAsyncResult BeginConnect(EndPoint remoteEP, AsyncCallback callback, object state)
        {
            return _socket.BeginConnect(remoteEP, callback, state);
        }

        public IAsyncResult BeginConnect(string serverName, int serverPort, AsyncCallback callback, object state)
        {
            IPHostEntry hostEntry = Dns.GetHostEntry(serverName);
            IPEndPoint endPoint = ProxySocket.ToEndPoint(hostEntry, serverPort);
            return _socket.BeginConnect(endPoint, callback, state);
        }

        public void EndConnect(IAsyncResult asyncResult)
        {
            _socket.EndConnect(asyncResult);
        }

        public EndPoint Listen(ISocket controlSocket)
        {
            _socket.Listen(0);
            return _socket.LocalEndPoint;
        }

        public IAsyncResult BeginListen(ISocket controlSocket, AsyncCallback callback, object state)
        {
            IPEndPoint ep = (IPEndPoint)controlSocket.LocalEndPoint;
            _socket.Bind(new IPEndPoint(ep.Address, 0));
            _socket.Listen(0);

            SyncResult result = new SyncResult(state, _socket.LocalEndPoint);

            callback?.Invoke(result);

            return result;
        }

        public EndPoint EndListen(IAsyncResult asyncResult)
        {
            SyncResult result = asyncResult as SyncResult;
            if (result == null)
            {
                throw new ArgumentException("The IAsyncResult object supplied to EndListen was not returned from the corresponding BeginListen method on this class.", nameof(asyncResult));
            }

            return (EndPoint)result.AsyncResult;
        }

        public ISocket Accept()
        {
            Socket socket = _socket.Accept();
            return new SimpleSocket(_factory, socket);
        }

        public IAsyncResult BeginAccept(AsyncCallback callback, object state)
        {
            return _socket.BeginAccept(callback, state);
        }

        public ISocket EndAccept(IAsyncResult asyncResult)
        {
            Socket socket = _socket.EndAccept(asyncResult);
            return new SimpleSocket(_factory, socket);
        }

        public int Send(byte[] buffer, int offset, int count, SocketFlags socketFlags)
        {
            return _socket.Send(buffer, offset, count, socketFlags);
        }

        public IAsyncResult BeginSend(byte[] buffer, int offset, int count, SocketFlags socketFlags, AsyncCallback callback, object state)
        {
            return _socket.BeginSend(buffer, offset, count, socketFlags, callback, state);
        }

        public int EndSend(IAsyncResult asyncResult)
        {
            return _socket.EndSend(asyncResult);
        }

        public int Receive(byte[] buffer, int offset, int count, SocketFlags socketFlags)
        {
            return _socket.Receive(buffer, offset, count, socketFlags);
        }

        public IAsyncResult BeginReceive(byte[] buffer, int offset, int count, SocketFlags socketFlags, AsyncCallback callback, object state)
        {
            return _socket.BeginReceive(buffer, offset, count, socketFlags, callback, state);
        }

        public int EndReceive(IAsyncResult asyncResult)
        {
            return _socket.EndReceive(asyncResult);
        }

        public void Shutdown(SocketShutdown how)
        {
            _socket.Shutdown(how);
        }

        public void Close()
        {
            _socket.Close();
        }
    }
}
