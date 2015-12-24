////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
using System;


using System.Net;
using System.IO;
using System.Text;

namespace Microsoft.Zelig.Test
{
    public class FunctionalTests : TestBase, ITestInterface
    {
        [SetUp]
        public InitializeResult Initialize()
        {
            Log.Comment("Adding set up for the tests.");
            try
            {
                // Check networking - we need to make sure we can reach our proxy server
                Dns.GetHostEntry(HttpTests.Proxy);
            }
            catch (Exception ex)
            {
                Log.Exception("Unable to get address for " + HttpTests.Proxy, ex);
                return InitializeResult.Skip;
            }
            return InitializeResult.ReadyToGo;
        }

        [TearDown]
        public void CleanUp()
        {
            Log.Comment("Cleaning up after the tests.");

            // TODO: Add your clean up steps here.
        }

        [TestMethod]
        public TestResult VisitMicrosoft()
        {
            try
            {
                Log.Comment("Small web page - redirect");
                // Print for now, Parse later
                string data = new string(Encoding.UTF8.GetChars(GetRequested("http://www.microsoft.com", "IIS")));
            }
            catch (Exception ex)
            {
                Log.Exception("Unexpected Exception", ex);
                return TestResult.Fail;
            }
            return TestResult.Pass;
        }

        [TestMethod]
        public TestResult VisitNYTimes()
        {
            try
            {
                Log.Comment("SUN web page");
                // Print for now, Parse later
                string data = new string(Encoding.UTF8.GetChars(GetRequested("http://www.nytimes.com", "SUN", "APACHE")));
            }
            catch (Exception ex)
            {
                Log.Exception("Unexpected Exception", ex);
                return TestResult.Fail;
            }
            return TestResult.Pass;
        }

        [TestMethod]
        public TestResult VisitApache()
        {
            try
            {
                Log.Comment("Apache Web server");
                // Print for now, Parse later
                string data = new string(Encoding.UTF8.GetChars(GetRequested("http://www.apache.org", "Apache")));
            }
            catch (Exception ex)
            {
                Log.Exception("Unexpected Exception", ex);
                return TestResult.Fail;
            }
            return TestResult.Pass;
        }

        [TestMethod]
        public TestResult VisitGoogle()
        {
            try
            {
                Log.Comment("Google Web server");
                // Print for now, Parse later
                string data = new string(Encoding.UTF8.GetChars(GetRequested("http://www.google.com", "GWS")));
            }
            catch (ArgumentException) { /* Don't care if google doesn't return wrong header, happens at major 'holidays' like april 1 */ }
            catch (Exception ex)
            {
                Log.Exception("Unexpected Exception", ex);
                return TestResult.Fail;
            }
            return TestResult.Pass;
        }

        [TestMethod]
        public TestResult VisitLighttpd()
        {
            try
            {
                Log.Comment("Lighttpd Web server");
                // Print for now, Parse later
                string data = new string(Encoding.UTF8.GetChars(GetRequested("http://redmine.lighttpd.net", "Lighttpd")));
            }
            catch (Exception ex)
            {
                Log.Exception("Unexpected Exception", ex);
                return TestResult.Fail;
            }
            return TestResult.Pass;
        }
        
        private byte[] GetRequested(string uri, params string[] servers)
        {
            byte[] page = null;

            // Create request.
            HttpWebRequest request = HttpWebRequest.Create(uri) as HttpWebRequest;
            // Set proxy information
            WebProxy itgProxy = new WebProxy(HttpTests.Proxy, true);
            request.Proxy = itgProxy;
            // Get response from server.
            WebResponse resp = null;
            try
            {
                resp = request.GetResponse();
            }
            catch (Exception e)
            {
                Log.Exception("GetResponse Exception", e);
                throw e;
            }

            try
            {
                // Get Network response stream
                if (resp != null)
                {
                    Log.Comment("Headers - ");
                    foreach (string header in resp.Headers.AllKeys)
                    {
                        Log.Comment("    " + header + ": " + resp.Headers[header]);
                    }

                    using (Stream respStream = resp.GetResponseStream())
                    {
                        // Get all data:
                        if (resp.ContentLength != -1)
                        {
                            int respLength = (int)resp.ContentLength;
                            page = new byte[respLength];

                            // Now need to read all data. We read in the loop until resp.ContentLength or zero bytes read.
                            // Zero bytes read means there was error on server and it did not send all data.
                            for (int totalBytesRead = 0; totalBytesRead < respLength; )
                            {
                                int bytesRead = respStream.Read(page, totalBytesRead, respLength - totalBytesRead);
                                // If nothing is read - means server closed connection or timeout. In this case no retry.
                                if (bytesRead == 0)
                                {
                                    break;
                                }
                                // Adds number of bytes read on this iteration.  
                                totalBytesRead += bytesRead;
                            } 
                        }
                        else
                        {
                            byte[] byteData = new byte[4096];
                            char[] charData = new char[4096];
                            string data = null;
                            int bytesRead = 0;
                            Decoder UTF8decoder = System.Text.Encoding.UTF8.GetDecoder();
                            int totalBytes = 0;
                            while ((bytesRead = respStream.Read(byteData, 0, byteData.Length)) > 0)
                            {
                                int byteUsed, charUsed;
                                bool completed = false;
                                totalBytes += bytesRead;
                                UTF8decoder.Convert(byteData, 0, bytesRead, charData, 0, bytesRead, true, out byteUsed, out charUsed, out completed);
                                data = data + new String(charData, 0, charUsed);
                                Log.Comment("Bytes Read Now: " + bytesRead + " Total: " + totalBytes);
                            }
                            Log.Comment("Total bytes downloaded in message body : " + totalBytes);
                            page = Encoding.UTF8.GetBytes(data);
                        }

                        Log.Comment("Page downloaded");

                        respStream.Close();
                    }

                    bool fFoundExpectedServer = false;
                    string httpServer = resp.Headers["server"].ToLower();
                    foreach(string server in servers)
                    {
                        if (httpServer.IndexOf(server.ToLower()) >= 0)
                        {
                            fFoundExpectedServer = true;
                            break;
                        }
                    }
                    if(!fFoundExpectedServer)
                    {
                        Log.Exception("Expected server: " + servers[0] + ", but got server: " + resp.Headers["Server"]);
                        throw new ArgumentException("Unexpected Server type");
                    }

                    resp.Close();
                }
            }
            catch (Exception ex)
            {
                Log.Exception("Unexpected exception processing response", ex);
                throw ex;
            }
            return page;
        }
    }
}
