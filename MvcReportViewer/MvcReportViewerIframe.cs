﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace MvcReportViewer
{
    /// <summary>
    /// HTML iframe rengering engine for MvcReportViewer HTML extension.
    /// </summary>
    public class MvcReportViewerIframe : IMvcReportViewerOptions
    {
        private const string JsPostForm = @"
document.addEventListener('DOMContentLoaded', function(event) {{
    var form = document.getElementById('{0}');
    if (form) {{
        form.submit();
    }}
}});
";
        private string _reportPath;

        private string _reportServerUrl;

        private string _username;

        private string _password;

        private FormMethod _method;

        private IDictionary<string, object> _reportParameters;

        private bool? _showParameterPrompts;

        private IDictionary<string, object> _htmlAttributes;

        private readonly string _aspxViewer;

        /// <summary>
        /// Creates an instance of MvcReportViewerIframe class.
        /// </summary>
        /// <param name="reportPath">The path to the report on the server.</param>
        public MvcReportViewerIframe(string reportPath)
            : this(reportPath, null, null, null, null, null, null, FormMethod.Get)
        {
        }

        /// <summary>
        /// Creates an instance of MvcReportViewerIframe class.
        /// </summary>
        /// <param name="reportPath">The path to the report on the server.</param>
        /// <param name="htmlAttributes">An object that contains the HTML attributes to set for the element.</param>
        public MvcReportViewerIframe(
            string reportPath,
            IDictionary<string, object> htmlAttributes)
            : this(reportPath, null, null, null, null, null, htmlAttributes, FormMethod.Get)
        {
        }

        /// <summary>
        /// Creates an instance of MvcReportViewerIframe class.
        /// </summary>
        /// <param name="reportPath">The path to the report on the server.</param>
        /// <param name="reportParameters">The report parameter properties for the report.</param>
        /// <param name="htmlAttributes">An object that contains the HTML attributes to set for the element.</param>
        public MvcReportViewerIframe(
            string reportPath,
            IDictionary<string, object> reportParameters,
            IDictionary<string, object> htmlAttributes)
            : this(reportPath, null, null, null, reportParameters, null, htmlAttributes, FormMethod.Get)
        {
        }

        /// <summary>
        /// Creates an instance of MvcReportViewerIframe class.
        /// </summary>
        /// <param name="reportPath">The path to the report on the server.</param>
        /// <param name="reportServerUrl">The URL for the report server.</param>
        /// <param name="username">The report server username.</param>
        /// <param name="password">The report server password.</param>
        /// <param name="reportParameters">The report parameter properties for the report.</param>
        /// <param name="showParameterPrompts">The value that indicates wether parameter prompts are dispalyed.</param>
        /// <param name="htmlAttributes">An object that contains the HTML attributes to set for the element.</param>
        /// <param name="method">Method for sending parameters to the iframe, either GET or POST.</param>
        public MvcReportViewerIframe(
            string reportPath,
            string reportServerUrl,
            string username,
            string password,
            IDictionary<string, object> reportParameters,
            bool? showParameterPrompts,
            IDictionary<string, object> htmlAttributes,
            FormMethod method)
        {
            _reportPath = reportPath;
            _reportServerUrl = reportServerUrl;
            _username = username;
            _password = password;
            _showParameterPrompts = showParameterPrompts;
            _reportParameters = reportParameters;
            _htmlAttributes = htmlAttributes;
            _method = method;
            _aspxViewer = ConfigurationManager.AppSettings[WebConfigSettings.AspxViewer];
            if (string.IsNullOrEmpty(_aspxViewer))
            {
                throw new MvcReportViewerException("ASP.NET Web Forms viewer is not set. Make sure you have MvcReportViewer.AspxViewer in your Web.config.");
            }
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return RenderIframe();
        }

        /// <summary>
        /// Returns an HTML-encoded string.
        /// </summary>
        /// <returns>An HTML-encoded string.</returns>
        public string ToHtmlString()
        {
            return ToString();
        }

        private string RenderIframe()
        {
            switch (_method)
            {
                case FormMethod.Get:
                    return GetIframeUsingGetMethod();
                    
                case FormMethod.Post:
                    return GetIframeUsingPostMethod();
            }
            
            throw new InvalidOperationException();
        }

        private string GetIframeUsingPostMethod()
        {
            var iframeId = GenerateId();
            var formId = GenerateId();

            // <form method="POST" action="/MvcReportViewer.aspx">...</form>
            var form = new TagBuilder("form");
            form.MergeAttribute("method", "POST");
            form.MergeAttribute("action", _aspxViewer);
            form.MergeAttribute("target", iframeId);
            form.GenerateId(formId);
            form.InnerHtml = BuildIframeFormFields();

            // <iframe />
            var iframe = new TagBuilder("iframe");
            iframe.MergeAttributes(_htmlAttributes);
            iframe.MergeAttribute("name", iframeId);
            iframe.GenerateId(iframeId);
            
            // <script>...</script>
            var script = new StringBuilder("<script>");
            script.AppendFormat(JsPostForm, formId);
            script.Append("</script>");

            var html = new StringBuilder();
            html.Append(form);
            html.Append(iframe);
            html.Append(script);
         
            return html.ToString();
        }

        private string GenerateId()
        {
            return "mvc-report-viewer-" + Guid.NewGuid().ToString("N");
        }

        private string BuildIframeFormFields()
        {
            var html = new StringBuilder();

            if (!string.IsNullOrEmpty(_reportPath))
            {
                html.Append(CreateHiddenField(UriParameters.ReportPath, _reportPath));
            }

            if (!string.IsNullOrEmpty(_reportServerUrl))
            {
                html.Append(CreateHiddenField(UriParameters.ReportServerUrl, _reportServerUrl));
            }

            if (!string.IsNullOrEmpty(_username) || !string.IsNullOrEmpty(_password))
            {
                html.Append(CreateHiddenField(UriParameters.Username, _username));
                html.Append(CreateHiddenField(UriParameters.Password, _password));
            }

            if (_showParameterPrompts != null)
            {
                html.Append(CreateHiddenField(UriParameters.ShowParameterPrompts, _showParameterPrompts));
            }

            if (_reportParameters != null)
            {
                foreach (var parameter in _reportParameters)
                {
                    var value = parameter.Value == null ? string.Empty : parameter.Value.ToString();
                    html.Append(CreateHiddenField(parameter.Key, value));
                }
            }

            return html.ToString();
        }

        private string CreateHiddenField<T>(string name, T value)
        {
            var tag = new TagBuilder("input");
            tag.MergeAttribute("type", "hidden");
            tag.MergeAttribute("name", name);
            tag.MergeAttribute("value", value.ToString());

            return tag.ToString();
        }

        private string GetIframeUsingGetMethod()
        {
            var iframe = new TagBuilder("iframe");
            var uri = PrepareViewerUri();
            iframe.MergeAttribute("src", uri);
            iframe.MergeAttributes(_htmlAttributes);
            return iframe.ToString();
        }

        private string PrepareViewerUri()
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            if (!string.IsNullOrEmpty(_reportPath))
            {
                query[UriParameters.ReportPath] = _reportPath;
            }

            if (!string.IsNullOrEmpty(_reportServerUrl))
            {
                query[UriParameters.ReportServerUrl] = _reportServerUrl;
            }

            if (!string.IsNullOrEmpty(_username) || !string.IsNullOrEmpty(_password))
            {
                query[UriParameters.Username] = _username;
                query[UriParameters.Password] = _password;
            }

            if (_showParameterPrompts != null)
            {
                query[UriParameters.ShowParameterPrompts] = _showParameterPrompts.ToString();
            }

            if (_reportParameters != null)
            {
                foreach (var parameter in _reportParameters)
                {
                    var value = parameter.Value == null ? string.Empty : parameter.Value.ToString();
                    query[parameter.Key] = value;
                }
            }

            var uri = query.Count == 0 ? 
                _aspxViewer : 
                _aspxViewer + "?" + query;

            return uri;
        }

        /// <summary>
        /// Sets the path to the report on the server.
        /// </summary>
        /// <param name="reportPath">The path to the report on the server.</param>
        /// <returns>An instance of MvcViewerOptions class.</returns>
        public IMvcReportViewerOptions ReportPath(string reportPath)
        {
            _reportPath = reportPath;
            return this;
        }

        /// <summary>
        /// Sets the URL for the report server.
        /// </summary>
        /// <param name="reportServerUrl">The URL for the report server.</param>
        /// <returns>An instance of MvcViewerOptions class.</returns>
        public IMvcReportViewerOptions ReportServerUrl(string reportServerUrl)
        {
            _reportServerUrl = reportServerUrl;
            return this;
        }

        /// <summary>
        /// Sets the report server username.
        /// </summary>
        /// <param name="username">The report server username.</param>
        /// <returns>An instance of MvcViewerOptions class.</returns>
        public IMvcReportViewerOptions Username(string username)
        {
            _username = username;
            return this;
        }

        /// <summary>
        /// Sets the report server password.
        /// </summary>
        /// <param name="password">The report server password.</param>
        /// <returns>An instance of MvcViewerOptions class.</returns>
        public IMvcReportViewerOptions Password(string password)
        {
            _password = password;
            return this;
        }

        /// <summary>
        /// Sets the report parameter properties for the report.
        /// </summary>
        /// <param name="reportParameters">The report parameter properties for the report.</param>
        /// <returns>An instance of MvcViewerOptions class.</returns>
        public IMvcReportViewerOptions ReportParameters(object reportParameters)
        {
            var parameters = reportParameters == null ? 
                null : 
                HtmlHelper.AnonymousObjectToHtmlAttributes(reportParameters);
            _reportParameters = parameters;
            return this;
        }

        /// <summary>
        /// Sets the value that indicates whether parameter prompts are displayed.
        /// </summary>
        /// <param name="showParameterPrompts">The value that indicates wether parameter prompts are dispalyed.</param>
        /// <returns>An instance of MvcViewerOptions class.</returns>
        public IMvcReportViewerOptions ShowParameterPrompts(bool showParameterPrompts)
        {
            _showParameterPrompts = showParameterPrompts;
            return this;
        }

        /// <summary>
        /// Sets an object that contains the HTML attributes to set for the element.
        /// </summary>
        /// <param name="htmlAttributes">An object that contains the HTML attributes to set for the element.</param>
        /// <returns>An instance of MvcViewerOptions class.</returns>
        public IMvcReportViewerOptions Attributes(object htmlAttributes)
        {
            var attributes = htmlAttributes == null ?
                null :
                HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
            _htmlAttributes = attributes;
            return this;
        }

        /// <summary>
        /// Sets the method for sending parametes to the iframe, either GET or POST.
        /// POST should be used to send long arguments, etc. Use GET otherwise.
        /// </summary>
        /// <param name="method">The HTTP method for sending parametes to the iframe, either GET or POST.</param>
        /// <returns>An instance of MvcViewerOptions class.</returns>
        public IMvcReportViewerOptions Method(FormMethod method)
        {
            _method = method;
            return this;
        }
    }
}
