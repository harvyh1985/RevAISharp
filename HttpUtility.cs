using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace RevAI
{
    public class HttpUtility
    {
        internal static Tuple<bool, string> PostFormData(string _url, string[] _fileNames, NameValueCollection _headers = null, NameValueCollection _formDatas = null, WebProxy _proxy = null, int _timeOut = 60000)
        {
            List<string> _files = new List<string>();
            foreach (string _fileName in _fileNames)
            {
                if (File.Exists(_fileName))
                {
                    _files.Add(_fileName);
                }
            }

            string _boundary = $"WebKit{Guid.NewGuid().ToString().Replace("-", "")}";
            byte[] _beginBoundary = Encoding.UTF8.GetBytes("--" + _boundary + "\r\n");
            byte[] _endBoundary = Encoding.UTF8.GetBytes("\r\n--" + _boundary + "--\r\n");

            HttpWebRequest _request = null;
            try
            {
                using (MemoryStream _memoryStream = new MemoryStream())
                {
                    _request = WebRequest.Create(_url) as HttpWebRequest;
                    _request.ContentType = string.Format("multipart/form-data; boundary={0}", _boundary);
                    _request.Method = "POST";
                    _request.KeepAlive = true;
                    _request.Timeout = _timeOut;
                    _request.Accept = "application/json";
                    if (_proxy != null)
                        _request.Proxy = _proxy;
                    if (_headers != null)
                    {
                        _headers.AllKeys.ToList().ForEach(t =>
                        {
                            switch (t)
                            {
                                case "user-agent":
                                case "useragent":
                                    _request.UserAgent = _headers[t];
                                    break;
                                default:
                                    _request.Headers.Add(t, _headers[t]);
                                    break;
                                case "accept":
                                    _request.Accept = _headers[t];
                                    break;
                            }
                        });
                    }

                    if (_formDatas != null)
                    {
                        string _template = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}\r\n";

                        foreach (string _key in _formDatas.Keys)
                        {
                            byte[] _formItemBytes = Encoding.UTF8.GetBytes(string.Format(_template, _key, _formDatas[_key]));
                            _memoryStream.Write(_beginBoundary, 0, _beginBoundary.Length);
                            _memoryStream.Write(_formItemBytes, 0, _formItemBytes.Length);
                        }
                    }

                    string _filesTemplate = "Content-Disposition: form-data; name=\"media\"; filename=\"{1}\"\r\n" +
                              "Content-Type: application/octet-stream\r\n\r\n";

                    int _fileCount = 0;
                    foreach (var _file in _files)
                    {
                        FileInfo _fi = new FileInfo(_file);
                        byte[] _fileInBoundary = Encoding.UTF8.GetBytes(string.Format(_filesTemplate, Path.GetFileNameWithoutExtension(_fi.Name), _fi.Name));
                        if (_fileCount > 0)
                        {
                            byte[] _emptyLineBytes = Encoding.UTF8.GetBytes("\r\n");
                            _memoryStream.Write(_emptyLineBytes, 0, _emptyLineBytes.Length);
                        }
                        _memoryStream.Write(_beginBoundary, 0, _beginBoundary.Length);
                        _memoryStream.Write(_fileInBoundary, 0, _fileInBoundary.Length);

                        int bytesRead;
                        byte[] _fileTempBuffer = new byte[1024];
                        FileStream fileStream = new FileStream(_file, FileMode.Open, FileAccess.Read);
                        while ((bytesRead = fileStream.Read(_fileTempBuffer, 0, _fileTempBuffer.Length)) != 0)
                        {
                            _memoryStream.Write(_fileTempBuffer, 0, bytesRead);
                        }
                        _fileCount++;
                    }
                    _memoryStream.Write(_endBoundary, 0, _endBoundary.Length);
                    _request.ContentLength = _memoryStream.Length;

                    Stream _writeStream = _request.GetRequestStream();
                    _memoryStream.Position = 0;
                    byte[] _tempBuffer = new byte[_memoryStream.Length];
                    _memoryStream.Read(_tempBuffer, 0, _tempBuffer.Length);
                    _memoryStream.Close();

                    _writeStream.Write(_tempBuffer, 0, _tempBuffer.Length);
                    _writeStream.Close();

                    HttpWebResponse _response = _request.GetResponse() as HttpWebResponse;
                    if (_response != null)
                    {
                        string _result = new StreamReader(_response.GetResponseStream(), Encoding.GetEncoding("utf-8")).ReadToEnd();
                        _response.Close();
                        return new Tuple<bool, string>(true, _result);
                    }
                }
            }
            catch (Exception _ex)
            {
                return new Tuple<bool, string>(false, _ex.Message);
            }
            finally
            {
                if (_request != null)
                {
                    _request.Abort();
                }
            }
            return new Tuple<bool, string>(false, "UNKNOW error");
        }

        internal static string PostData(string _url, string _contentType = "application/json", string _postValue = "", CookieContainer _cookieContainer = null, NameValueCollection _headers = null, WebProxy _proxy = null,
            int _timeout = 60000, bool _expect100Continue = false)
        {
            var _request = (HttpWebRequest)WebRequest.Create(_url);
            _request.Method = "POST";
            _request.ContentType = _contentType;
            _request.ServicePoint.Expect100Continue = _expect100Continue;
            if (_headers != null)
            {
                _headers.AllKeys.ToList().ForEach(t =>
                {
                    switch (t)
                    {
                        case "user-agent":
                        case "useragent":
                            _request.UserAgent = _headers[t];
                            break;
                        default:
                            _request.Headers.Add(t, _headers[t]);
                            break;
                        case "accept":
                            _request.Accept = _headers[t];
                            break;
                        case "contenttype":
                        case "content-type":
                            _request.ContentType = _headers[t];
                            break;
                    }
                });
            }
            if (_cookieContainer == null)
                _cookieContainer = new CookieContainer();


            _request.Timeout = _timeout;

            _request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            _request.CookieContainer = _cookieContainer;
            if (_proxy != null)
                _request.Proxy = _proxy;
            if (!string.IsNullOrWhiteSpace(_postValue))
            {
                var _valueArray = System.Text.Encoding.UTF8.GetBytes(_postValue);
                _request.ContentLength = _valueArray.Length;
                using (Stream _writeStream = _request.GetRequestStream())
                {
                    _writeStream.Write(_valueArray, 0, _valueArray.Length);
                }
            }
            var _response = (HttpWebResponse)_request.GetResponse();
            string _result = new StreamReader(_response.GetResponseStream(), Encoding.GetEncoding("utf-8")).ReadToEnd();
            _response.Close();
            _request.Abort();
            return _result;
        }

        internal static Tuple<bool, string> Get(string _url, string _contentType = "application/json", CookieContainer _cookieContainer = null, NameValueCollection _headers = null, WebProxy _proxy = null,
            int _timeout = 60000)
        {
            return RequestUrl("GET", _url, _contentType, _cookieContainer, _headers, _proxy, _timeout);
        }

        internal static Tuple<bool, string> Delete(string _url, string _contentType = "application/json", CookieContainer _cookieContainer = null, NameValueCollection _headers = null, WebProxy _proxy = null,
       int _timeout = 60000)
        {
            return RequestUrl("DELETE", _url, _contentType, _cookieContainer, _headers, _proxy, _timeout);
        }

        static Tuple<bool,string> RequestUrl(string _method,string _url, string _contentType , CookieContainer _cookieContainer, NameValueCollection _headers, WebProxy _proxy,int _timeout)
        {
            try
            {
                var _request = (HttpWebRequest)WebRequest.Create(_url);
                _request.Method = _method;
                _request.ContentType = _contentType;
                if (_headers != null)
                {
                    _headers.AllKeys.ToList().ForEach(t =>
                    {
                        switch (t)
                        {
                            case "user-agent":
                            case "useragent":
                                _request.UserAgent = _headers[t];
                                break;
                            default:
                                _request.Headers.Add(t, _headers[t]);
                                break;
                            case "accept":
                                _request.Accept = _headers[t];
                                break;
                            case "contenttype":
                            case "content-type":
                                _request.ContentType = _headers[t];
                                break;
                        }
                    });
                }
                if (_cookieContainer == null)
                    _cookieContainer = new CookieContainer();


                _request.Timeout = _timeout;

                _request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                _request.CookieContainer = _cookieContainer;
                if (_proxy != null)
                    _request.Proxy = _proxy;

                var _response = (HttpWebResponse)_request.GetResponse();
                string _result = new StreamReader(_response.GetResponseStream(), Encoding.GetEncoding("utf-8")).ReadToEnd();
                _response.Close();
                _request.Abort();
                return new Tuple<bool, string>(true, _result);
            }
            catch (Exception _ex)
            {
                return new Tuple<bool, string>(false, _ex.Message);
            }
        }
    }
}
