using System.Linq;
using Epinova.ElasticSearch.Core.Admin;
using Epinova.ElasticSearch.Core.Models.Admin;
using Xunit;

namespace Core.Tests.Admin
{
    public class HealthTests
    {
        [Fact]
        public void GetClusterHealth_DeserializesProperly()
        {
            const string json = "[{" +
                                "\"epoch\": \"1507123453\"," +
                                "\"timestamp\": \"15:24:13\"," +
                                "\"cluster\": \"elasticsearch\"," +
                                "\"status\": \"yellow\"," +
                                "\"node.total\": \"1\"," +
                                "\"node.data\": \"1\"," +
                                "\"shards\": \"40\"," +
                                "\"pri\": \"40\"," +
                                "\"relo\": \"0\"," +
                                "\"init\": \"0\"," +
                                "\"unassign\": \"40\"," +
                                "\"pending_tasks\": \"0\"," +
                                "\"max_task_wait_time\": \"-\"," +
                                "\"active_shards_percent\": \"50.0%\"" +
                                "}]";

            HealthInformation info = Health.GetClusterHealth(json);

            Assert.Equal("50.0%", info.ActiveShardsPercent);
            Assert.Equal("elasticsearch", info.Cluster);
            Assert.Equal("yellow", info.Status);
            Assert.Equal("orange", info.StatusColor);
            Assert.Equal(1507123453, info.Epoch);
            Assert.Equal(0, info.Init);
            Assert.Equal(1, info.NodeData);
            Assert.Equal(1, info.NodeTotal);
            Assert.Equal(0, info.PendingTasks);
            Assert.Equal(40, info.Pri);
            Assert.Equal(0, info.Relo);
            Assert.Equal(40, info.Shards);
            Assert.Equal("15:24:13", info.Timestamp);
            Assert.Equal(40, info.Unassign);
        }

        [Fact]
        public void GetNodeInfo_DeserializesProperly()
        {
            const string json = "[{" +
                                "\"m\": \"*\"," +
                                "\"v\": \"5.2.0\"," +
                                "\"http\": \"127.0.0.1:9200\"," +
                                "\"d\": \"260.6gb\"," +
                                "\"rc\": \"25.6gb\"," +
                                "\"rm\": \"63.9gb\"," +
                                "\"u\": \"22.5h\"," +
                                "\"n\": \"kJLWnVe\"" +
                                "}]";

            Node node = Health.GetNodeInfo(json).First();

            Assert.True(node.Master);
            Assert.Equal("260.6gb", node.HddAvailable);
            Assert.Equal("127.0.0.1:9200", node.Ip);
            Assert.Equal("25.6gb", node.MemoryCurrent);
            Assert.Equal("63.9gb", node.MemoryTotal);
            Assert.Equal("kJLWnVe", node.Name);
            Assert.Equal("22.5h", node.Uptime);
            Assert.Equal("5.2.0", node.Version);
        }
    }
}