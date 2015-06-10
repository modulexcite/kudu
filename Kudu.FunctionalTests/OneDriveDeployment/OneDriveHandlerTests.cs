using Kudu.Contracts.Settings;
using Kudu.Contracts.Tracing;
using Kudu.Core;
using Kudu.Services.ServiceHookHandlers;
using Kudu.TestHarness.Xunit;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Kudu.FunctionalTests.OneDriveDeployment
{
    [KuduXunitTestClass]
    public class OneDriveHandlerTests
    {
        [Fact]
        public void TryParseDeploymentInfoShouldReturnUnknownPayload()
        {
            var oneDriveHandler = CreateMockOneDriveHandler();
            JObject payload = JObject.FromObject(new { });
            DeploymentInfo deploymentInfo = null;

            DeployAction result = oneDriveHandler.TryParseDeploymentInfo(null, payload, null, out deploymentInfo);
            Assert.Equal(DeployAction.UnknownPayload, result);
        }

        [Fact]
        public void TryParseDeploymentInfoShouldReturnNoOp()
        {
            var oneDriveHandler = CreateMockOneDriveHandler();
            JObject payload = JObject.FromObject(new { deployer = "OneDrive" });
            DeploymentInfo deploymentInfo = null;

            DeployAction result = oneDriveHandler.TryParseDeploymentInfo(null, payload, null, out deploymentInfo);
            Assert.Equal(DeployAction.NoOp, result);
        }

        [Fact]
        public void TryParseDeploymentInfoShouldReturnProcessDeployment()
        {
            var oneDriveHandler = CreateMockOneDriveHandler();
            JObject payload = JObject.FromObject(new { deployer = "OneDrive", url = "http://ondrive.api.com", access_token = "one-drive-access-token" });
            DeploymentInfo deploymentInfo = null;

            DeployAction result = oneDriveHandler.TryParseDeploymentInfo(null, payload, null, out deploymentInfo);
            Assert.Equal(DeployAction.ProcessDeployment, result);
        }

        private static OneDriveHandler CreateMockOneDriveHandler()
        {
            var mockTracer = new Mock<ITracer>();
            var mockSettings = new Mock<IDeploymentSettingsManager>();
            var mockEnv = new Mock<IEnvironment>();
            return new OneDriveHandler(mockTracer.Object, mockSettings.Object, mockEnv.Object);
        }
    }
}
