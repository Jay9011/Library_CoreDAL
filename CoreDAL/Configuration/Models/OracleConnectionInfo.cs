using System;
using System.Collections.Generic;
using CoreDAL.Configuration.Interface;
using SECUiDEA.CoreDAL;

namespace CoreDAL.Configuration.Models
{
    public class OracleConnectionInfo : IDbConnectionInfo
    {
        public DatabaseType DbType => DatabaseType.ORACLE;
        virtual public string Host { get; set; }
        virtual public int Port { get; set; } = 1521;
        virtual public string ServiceName { get; set; }
        virtual public string UserId { get; set; }
        virtual public string Password { get; set; }
        virtual public string Protocol { get; set; } = "TCP";

        public string ToConnectionString()
        {
            return $"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL={Protocol})(HOST={Host})(PORT={Port}))(CONNECT_DATA=(SERVICE_NAME={ServiceName})));User Id={UserId};Password={Password};";
        }

        public bool Validate(out string errorMessage)
        {
            errorMessage = string.Empty;
            if (string.IsNullOrWhiteSpace(Host))
            {
                errorMessage = "Host is required.";
                return false;
            }

            if (Port <= 0)
            {
                errorMessage = "Port is required.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(ServiceName))
            {
                errorMessage = "ServiceName is required.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(UserId))
            {
                errorMessage = "UserId is required.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                errorMessage = "Password is required.";
                return false;
            }

            return true;
        }

        public IDbConnectionInfo LoadFromSettings(Dictionary<string, string> settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            Host = settings.TryGetValue(Consts.HostKey, out var host) ? host : "";
            Port = settings.TryGetValue(Consts.PortKey, out var port) ? int.Parse(port) : Port;
            ServiceName = settings.TryGetValue(Consts.ServiceNameKey, out var serviceName) ? serviceName : "";
            UserId = settings.TryGetValue(Consts.UserIdKey, out var userId) ? userId : "";
            Password = settings.TryGetValue(Consts.PasswordKey, out var password) ? password : "";
            Protocol = settings.TryGetValue(Consts.ProtocolKey, out var protocol) ? protocol : Protocol;

            return this;
        }

        public Dictionary<string, string> ToSettings()
        {
            return new Dictionary<string, string>
            {
                [Consts.DBTypeKey] = DbType.ToString(),
                [Consts.HostKey] = Host,
                [Consts.PortKey] = Port.ToString(),
                [Consts.ServiceNameKey] = ServiceName,
                [Consts.UserIdKey] = UserId,
                [Consts.PasswordKey] = Password,
                [Consts.ProtocolKey] = Protocol
            };
        }
    }
}
