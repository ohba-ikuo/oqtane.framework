using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using Oqtane.Shared;
using System;
using Microsoft.AspNetCore.Hosting;
using Oqtane.Infrastructure;
using Microsoft.AspNetCore.Http.Features;
using System.IO;

namespace Oqtane.Controllers
{
    [Route(ControllerRoutes.ApiRoute)]
    public class SystemController : Controller
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IConfigManager _configManager;

        public SystemController(IWebHostEnvironment environment, IConfigManager configManager)
        {
            _environment = environment;
            _configManager = configManager;
        }

        // GET: api/<controller>?type=x
        [HttpGet]
        [Authorize(Roles = RoleNames.Host)]
        public Dictionary<string, object> Get(string type)
        {
            Dictionary<string, object> systeminfo = new Dictionary<string, object>();

            switch (type.ToLower())
            {
                case "environment":
                    systeminfo.Add("CLRVersion", Environment.Version.ToString());
                    systeminfo.Add("OSVersion", Environment.OSVersion.ToString());
                    systeminfo.Add("Process", (Environment.Is64BitProcess) ? "64 Bit" : "32 Bit");
                    systeminfo.Add("MachineName", Environment.MachineName);
                    systeminfo.Add("WorkingSet", Environment.WorkingSet.ToString());
                    systeminfo.Add("TickCount", Environment.TickCount64.ToString());
                    systeminfo.Add("ContentRootPath", _environment.ContentRootPath);
                    systeminfo.Add("WebRootPath", _environment.WebRootPath);
                    systeminfo.Add("Environment", _environment.EnvironmentName);
                    systeminfo.Add("ServerTime", DateTime.UtcNow.ToString());
                    var feature = HttpContext.Features.Get<IHttpConnectionFeature>();
                    systeminfo.Add("IPAddress", feature?.LocalIpAddress?.ToString());
                    break;
                case "configuration":
                    systeminfo.Add("InstallationId", _configManager.GetInstallationId());
                    systeminfo.Add("RenderMode", _configManager.GetSetting("RenderMode", RenderModes.Interactive));
                    systeminfo.Add("Runtime", _configManager.GetSetting("Runtime", Runtimes.Server));
                    systeminfo.Add("DetailedErrors", _configManager.GetSetting("DetailedErrors", "false"));
                    systeminfo.Add("Logging:LogLevel:Default", _configManager.GetSetting("Logging:LogLevel:Default", "Information"));
                    systeminfo.Add("Logging:LogLevel:Notify", _configManager.GetSetting("Logging:LogLevel:Notify", "Error"));
                    systeminfo.Add("UseSwagger", _configManager.GetSetting("UseSwagger", "true"));
                    systeminfo.Add("CacheControl", _configManager.GetSetting("CacheControl", ""));
                    systeminfo.Add("PackageRegistryUrl", _configManager.GetSetting("PackageRegistryUrl", Constants.PackageRegistryUrl));
                    systeminfo.Add("PackageRegistryEmail", _configManager.GetSetting("PackageRegistryEmail", ""));
                    break;
                case "log":
                    string log = "";
                    string path = Path.Combine(_environment.ContentRootPath, "Content", "Log", "error.log");
                    if (System.IO.File.Exists(path))
                    {
                        log = System.IO.File.ReadAllText(path);
                    }
                    systeminfo.Add("Log", log);
                    break;
                case "connectionstrings":
                    foreach (var kvp in _configManager.GetSettings(SettingKeys.ConnectionStringsSection))
                    {
                        systeminfo.Add(kvp.Key, kvp.Value);
                    }
                    break;
            }

            return systeminfo;
        }

        // GET: api/<controller>
        [HttpGet("{key}/{value}")]
        [Authorize(Roles = RoleNames.Host)]
        public object Get(string key, object value)
        {
            return _configManager.GetSetting(key, value);
        }

        // POST: api/<controller>
        [HttpPost]
        [Authorize(Roles = RoleNames.Host)]
        public void Post([FromBody] Dictionary<string, object> settings)
        {
            foreach(KeyValuePair<string, object> kvp in settings)
            {
                UpdateSetting(kvp.Key, kvp.Value);
            }
        }

        // GET: api/<controller>/icons
        [HttpGet("icons")]
        public Dictionary<string, string> Get()
        {
            var icons = new Dictionary<string, string>();

            // use reflection to get list of icons from Icons class
            Type iconsType = typeof(Icons);
            System.Reflection.FieldInfo[] fields = iconsType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.GetField);
            foreach (System.Reflection.FieldInfo field in fields)
            {
                if (field.FieldType == typeof(string))
                {
                    icons.Add((string)field.GetValue(null), field.Name); // ie. ("oi oi-home", "Home")
                }
            }

            return icons;
        }

        private void UpdateSetting(string key, object value)
        {
            switch (key.ToLower())
            {
                case "clearlog":
                    string path = Path.Combine(_environment.ContentRootPath, "Content", "Log", "error.log");
                    if (System.IO.File.Exists(path))
                    {
                        System.IO.File.Delete(path);
                    }
                    break;
                default:
                    _configManager.AddOrUpdateSetting(key, value, false);
                    break;
            }
        }
    }
}
