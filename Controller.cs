using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace RevAI
{
    public class Controller
    {
        NameValueCollection _headers = new NameValueCollection();
        /// <summary>
        /// 提交任务
        /// </summary>
        const string SubmitJobUrl = "https://api.rev.ai/speechtotext/v1/jobs";
        /// <summary>
        /// 获取列表
        /// </summary>
        const string GetJobListUrl = "https://api.rev.ai/speechtotext/v1/jobs?limit=1000";
        /// <summary>
        /// 获取列表
        /// </summary>
        const string GetJobUrl = "https://api.rev.ai/speechtotext/v1/jobs/";
        public Controller(string _token)
        {
            _headers["Authorization"] = $"Bearer {_token}";
        }
        
        public JObject SubmitJob(string _fileName,string _metaData)
        {
            NameValueCollection _formData = new NameValueCollection();
            _formData["options"] = new JObject() { ["metadata"] = _metaData }.ToString(Newtonsoft.Json.Formatting.None);
            Tuple<bool, string> _result = HttpUtility.PostFormData(SubmitJobUrl, new string[] { _fileName }, _headers, _formData);
            if (!_result.Item1)
                throw new Exception(_result.Item2);
            return JObject.Parse(_result.Item2);
        }

        public JArray JobList()
        {
            Tuple<bool, string> _result = HttpUtility.Get(GetJobListUrl, "application/json", null, _headers);
            if (!_result.Item1)
                throw new Exception(_result.Item2);
           return JArray.Parse(_result.Item2);
        }

        public JObject GetJob(string _jobId)
        {
            Tuple<bool, string> _result = HttpUtility.Get($"{GetJobUrl}{_jobId}", "application/json", null, _headers);
            if (!_result.Item1)
                throw new Exception(_result.Item2);
            return JObject.Parse(_result.Item2);
        }

        public string GetJobSubtitle(string _jobId)
        {
            NameValueCollection _headerWithAccept = new NameValueCollection();
            _headerWithAccept["Authorization"] = _headers["Authorization"];
            _headerWithAccept["accept"] = "application/x-subrip";
          
            Tuple<bool, string> _result = HttpUtility.Get($"{GetJobUrl}{_jobId}/captions", "application/json", null, _headerWithAccept);
            if (!_result.Item1)
                throw new Exception(_result.Item2);
            return _result.Item2;
        }

        public bool DeleteJob(string _jobId)
        {
            Tuple<bool, string> _result = HttpUtility.Delete($"{GetJobUrl}{_jobId}", "application/json", null, _headers, _proxy);
            return _result.Item1;
        }
    }
}
