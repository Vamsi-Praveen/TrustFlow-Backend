using Confluent.Kafka;
using Newtonsoft.Json;
using Org.BouncyCastle.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using TrustFlow.Core.Communication;
using TrustFlow.Core.Models;

namespace TrustFlow.Core.Services
{
    public class ActivityService
    {
        public ActivityService()
        {
            

        }


        public async Task<ServiceResult> SendActivity(ActivityLog log)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = "localhost:9092"
            };

            using var producer = new ProducerBuilder<Null, string>(config).Build();

            var json = JsonConvert.SerializeObject(log);

            try
            {
                var result = await producer.ProduceAsync(
                    "test-topic",
                    new Message<Null, string> { Value = json });

                return new ServiceResult(true, $"Message sent to {result.TopicPartitionOffset}");
            }
            catch (ProduceException<Null, string> e)
            {
                Console.WriteLine($"Delivery failed: {e.Error.Reason}");
                return new ServiceResult(false, "Failed to Send activity to Kafka");
            }

        }

        public async Task<ServiceResult> RecieveActivity(int count = 10)
        {
            try
            {
                var logs = new List<ActivityLog>();
                var config = new ConsumerConfig
                {
                    GroupId = "test-group",
                    BootstrapServers = "localhost:9092",
                    AutoOffsetReset = AutoOffsetReset.Latest,
                    EnableAutoCommit = false
                };
                using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
                string topic = "test-topic";
                var partition = new TopicPartition(topic, new Partition(0));

                // Assign instead of Subscribe so we control where we read from
                consumer.Assign(partition);

                // Find the latest offset
                var offsets = consumer.QueryWatermarkOffsets(partition, TimeSpan.FromSeconds(3));

                long latest = offsets.High.Value;       // Next offset to be written
                long earliest = offsets.Low.Value;      // Oldest retained offset
                long start = Math.Max(latest - count, earliest);

                // Jump directly to where we want to start
                consumer.Seek(new TopicPartitionOffset(partition, new Offset(start)));

                // Read up to 'count' messages
                for (int i = 0; i < count; i++)
                {
                    var cr = consumer.Consume(TimeSpan.FromMilliseconds(500));
                    if (cr?.Message?.Value == null) continue;

                    if (string.IsNullOrWhiteSpace(cr.Message.Value) || !cr.Message.Value.TrimStart().StartsWith("{"))
                    {
                        Console.WriteLine($"Skipping non-JSON message: {cr.Message.Value}");
                        continue;
                    }

                    var log = JsonConvert.DeserializeObject<ActivityLog>(cr.Message.Value);
                    logs.Add(log);
                }

                return new ServiceResult(true, "Succesfully Recieved logs", logs);
            }
            catch(Exception ex)
            {
                return new ServiceResult(false, "An Internal error occured - Failed to get logs");
            }
        }

        public async Task<bool> RecieveActivity1(int count = 10)
        {
            var config = new ConsumerConfig
            {
                GroupId = "test-group",
                BootstrapServers = "localhost:9092",
                AutoOffsetReset = AutoOffsetReset.Latest,
                EnableAutoCommit = false
            };
            using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
            consumer.Subscribe("test-topic");

            var logs = new List<ActivityLog>();
            var timeout = DateTime.UtcNow.AddSeconds(3); // Don’t block too long

            while (logs.Count < count && DateTime.UtcNow < timeout)
            {
                try
                {

                    var cr = consumer.Consume(TimeSpan.FromMilliseconds(200));

                    if (string.IsNullOrWhiteSpace(cr.Message.Value) || !cr.Message.Value.TrimStart().StartsWith("{"))
                    {
                        Console.WriteLine($"Skipping non-JSON message: {cr.Message.Value}");
                        continue;
                    }

                    if (cr?.Message?.Value != null)
                    {
                        var log = JsonConvert.DeserializeObject<ActivityLog>(cr.Message.Value);
                        if (log != null)
                            logs.Add(log);
                    }
                }
                catch (ConsumeException e)
                {
                    Console.WriteLine($"Kafka error: {e.Error.Reason}");
                    break;
                }
            }

            consumer.Close();

            return true;
        }
    }
}
