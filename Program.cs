using RabbitMQ.Client;
using System;
using System.Threading;

namespace RabbitMQCrash
{
    class Program
    {
        static void Main(string[] args)
        {
            IConnection _connection = null;
            IModel _channel = null;

            ulong someInvalidTag = 1000;
            bool closeConnection = true; // Change this trigger to see another scenario

            _connection = Connect();
            _channel = _connection.CreateModel();

            _channel.ModelShutdown += (s, e) =>
            {
                Console.WriteLine("Model shutdown");
                if (_connection != null && _connection.IsOpen)
                {
                    if (closeConnection)
                    {
                        Console.WriteLine("Connection seems to be open, try to close it");

                        // This will deadlock the handler.
                        _connection.Close();
                    }
                    else
                    {
                        Console.WriteLine("Connection seems to be open, try to create a channel through it");

                        // This will result in a timeout exception.
                        var newModel = _connection.CreateModel();
                    }

                    // We will never reach the following line.
                    Console.WriteLine("Handler finished successfully!");

                }
                else
                {
                    // This will actually never be the case if being triggered by an invalid tag
                    Console.WriteLine("Connection is broken also!");
                }
            };

            _channel.ModelShutdown += (s, e) =>
            {
                Console.WriteLine("Model shutdown");
                // Here I would like to create a new model from an existing connection
                if (_connection != null && _connection.IsOpen)
                {
                }
                else
                {
                    // This will actually never be the case if being triggered by an invalid tag
                    Console.WriteLine("Connection is broken also!");
                }
            };


            // This one will be fired
            _connection.ConnectionShutdown += (s, e) =>
            {
                if (!_connection.IsOpen)
                    Console.WriteLine("Connection closed.");
            };

            Console.WriteLine("Trying to ack with some invalid tag");
            // This will trigger _channel.ModelShutdown
            _channel.BasicAck(someInvalidTag, false);
        }

        private static IConnection Connect()
        {
            IConnection _connection = null;

            var _host = "localhost";
            var _user = "guest";
            var _pass = "guest";
            var _timeout = 1000;

            while (!(_connection != null && _connection.IsOpen))
            {
                try
                {
                    var connectionFactory = new ConnectionFactory
                    {
                        HostName = _host,
                        UserName = _user,
                        Password = _pass,
                    };

                    _connection = connectionFactory.CreateConnection();
                }
                catch
                {
                    Console.WriteLine($"Could not connect to RabbitMQ at {_host}");
                }

                if (!(_connection != null && _connection.IsOpen))
                    Thread.Sleep(_timeout);
                else
                    Console.WriteLine($"Using RabbitMQ at {_host}");

            }

            return _connection;
        }
    }
}
