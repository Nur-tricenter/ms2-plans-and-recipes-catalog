using Joseco.Communication.External.RabbitMQ.Services;

namespace Catalog.Tests.IntegrationTest.RabbitMq
{
    /// <summary>
    /// Builds the <see cref="RabbitMqSettings"/> the broker integration tests hand to the real
    /// <c>AddRabbitMQ</c> registration. The values match the project-level broker published by the
    /// container <c>nurtricenter-rabbitmq</c>.
    /// </summary>
    public static class RabbitMqTestSettings
    {
        public const string UserName = "catalogUser";
        public const string Password = "catalogUserPassword";
        public const string VirtualHost = "/";

        public static RabbitMqSettings Create(int maxRetryAttempts = 3, int retryBaseDelayMilliseconds = 300)
        {
            return new RabbitMqSettings
            {
                Host = RabbitMqProbe.Host,
                Port = RabbitMqProbe.Port,
                UserName = UserName,
                Password = Password,
                VirtualHost = VirtualHost,
                UseSsl = false,
                MaxRetryAttempts = maxRetryAttempts,
                RetryBaseDelayMilliseconds = retryBaseDelayMilliseconds
            };
        }
    }
}