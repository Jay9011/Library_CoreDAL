using System;
using System.Collections.Generic;
using System.Text;
using CoreDAL.Configuration.Interface;
using SECUiDEA.CoreDAL;

namespace CoreDAL.Configuration.Models
{
    public class MsSqlConnectionInfo : IDbConnectionInfo
    {
        public DatabaseType DbType => DatabaseType.MSSQL;
        virtual public string Server { get; set; }
        virtual public string Database { get; set; }
        virtual public string UserId { get; set; }
        virtual public string Password { get; set; }
        virtual public bool IntegratedSecurity { get; set; }
        virtual public int? Port { get; set; } = 1433;

        public string ToConnectionString()
        {
            var builder = new StringBuilder();

            if (Port.HasValue)
            {
                builder.Append($"Server={Server},{Port};");
            }
            else
            {
                builder.Append($"Server={Server};");
            }

            builder.Append($"Database={Database};");

            if (IntegratedSecurity)
            {
                builder.Append("Integrated Security=True;");
            }
            else
            {
                builder.Append($"User Id={UserId};Password={Password};");
            }

            builder.Append($"{Consts.TrustServerCertificateKey}=True;");

            return builder.ToString();
        }

        public bool Validate(out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(Server))
            {
                errorMessage = "Server Name is required";
                return false;
            }

            if (string.IsNullOrWhiteSpace(Database))
            {
                errorMessage = "Database Name is required";
                return false;
            }

            if (!IntegratedSecurity)
            {
                if (string.IsNullOrWhiteSpace(UserId))
                {
                    errorMessage = "User Id is required";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(Password))
                {
                    errorMessage = "Password is required";
                    return false;
                }
            }

            errorMessage = string.Empty;
            return true;
        }

        public IDbConnectionInfo LoadFromSettings(Dictionary<string, string> settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            Server = settings.TryGetValue(Consts.ServerKey, out var server) ? server : "";
            Database = settings.TryGetValue(Consts.DatabaseKey, out var database) ? database : "";
            UserId = settings.TryGetValue(Consts.UserIdKey, out var userId) ? userId : "";
            Password = settings.TryGetValue(Consts.PasswordKey, out var password) ? password : "";
            IntegratedSecurity = settings.TryGetValue(Consts.IntegratedSecurityKey, out var integratedSecurity) && bool.Parse(integratedSecurity);
            Port = settings.TryGetValue(Consts.PortKey, out var setting) ? int.Parse(setting) : Port;

            return this;
        }

        public Dictionary<string, string> ToSettings()
        {
            return new Dictionary<string, string>
            {
                [Consts.DBTypeKey] = DbType.ToString(),
                [Consts.ServerKey] = Server,
                [Consts.PortKey] = Port.ToString(),
                [Consts.DatabaseKey] = Database,
                [Consts.UserIdKey] = UserId,
                [Consts.PasswordKey] = Password,
                [Consts.IntegratedSecurityKey] = IntegratedSecurity.ToString()
            };
        }
    }
}
