using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Reflection;
using System.Threading.Tasks;
using Kudu.Contracts.Settings;
using Kudu.Contracts.Tracing;
using Kudu.Core;
using Kudu.Core.Infrastructure;
using Kudu.Services.FetchHelpers;
using Kudu.TestHarness.Xunit;
using Moq;
using Xunit;

namespace Kudu.FunctionalTests.OneDriveDeployment
{
    [KuduXunitTestClass]
    public class OneDriveHelperRetryHandlerTests
    {
        [Fact]
        public async Task RetryHandlerReturnFalse()
        {
            OneDriveHelper helper = CreateMockOneDriveHelper();

            Func<Task> dummyTask = () =>
            {
                return Task.Run(() =>
                {
                    throw new Exception();
                });
            };

            MethodInfo methodInfo = helper.GetType().GetMethod("RetryHandler", BindingFlags.NonPublic | BindingFlags.Instance);
            bool result = await ((Task<bool>)methodInfo.Invoke(helper, new object[] { dummyTask }));
            Assert.Equal(false, result);
        }

        [Fact]
        public async Task RetryHandlerReturnTrue()
        {
            OneDriveHelper helper = CreateMockOneDriveHelper();

            Func<Task> dummyTask = () =>
            {
                return Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                });
            };

            MethodInfo methodInfo = helper.GetType().GetMethod("RetryHandler", BindingFlags.NonPublic | BindingFlags.Instance);
            bool result = await ((Task<bool>)methodInfo.Invoke(helper, new object[] { dummyTask }));
            Assert.Equal(true, result);
        }

        private static OneDriveHelper CreateMockOneDriveHelper()
        {
            var mockTracer = new Mock<ITracer>();
            var mockSettings = new Mock<IDeploymentSettingsManager>();
            var mockEnv = new Mock<IEnvironment>();
            return new OneDriveHelper(mockTracer.Object, mockSettings.Object, mockEnv.Object);
        }
    }

    [KuduXunitTestClass]
    public class OneDriveHelperHandlingDeletionTests
    {
        [Fact]
        public void HandlingDeletionBasicTests()
        {
            var mockTracer = new Mock<ITracer>();
            mockTracer
                .Setup(m => m.Trace(It.IsAny<string>(), It.IsAny<IDictionary<string, string>>()));

            // prepare change from OneDrive
            OneDriveModel.OneDriveChange change = new OneDriveModel.OneDriveChange();
            change.IsDeleted = true;


            // prepare file in file system
            var fileSystem = new Mock<IFileSystem>();
            var fileBase = new Mock<FileBase>();
            var dirBase = new Mock<DirectoryBase>();
            var dirInfoFactory = new Mock<IDirectoryInfoFactory>(); // mock dirInfo to make FileSystemHelpers.DeleteDirectorySafe not throw exception
            var dirInfoBase = new Mock<DirectoryInfoBase>();
            fileSystem.Setup(f => f.File).Returns(fileBase.Object);
            fileSystem.Setup(f => f.Directory).Returns(dirBase.Object);
            fileSystem.Setup(f => f.DirectoryInfo).Returns(dirInfoFactory.Object);
            dirInfoFactory.Setup(d => d.FromDirectoryName(It.IsAny<string>())).Returns(dirInfoBase.Object);
            fileBase.Setup(fb => fb.Exists(It.IsAny<string>())).Returns((string path) =>
            {
                return (path != null && path.EndsWith(@"bar.txt"));
            });
            dirBase.Setup(d => d.Exists(It.IsAny<string>())).Returns((string path) =>
            {
                return (path != null && path.EndsWith(@"/wa"));
            });
            FileSystemHelpers.Instance = fileSystem.Object;

            // prepare path for wwwroot
            string wwwroot = @"C:\temp\wwwroot";

            // prepare invokeing HandlingDeletion
            var mockSettings = new Mock<IDeploymentSettingsManager>();
            var mockEnv = new Mock<IEnvironment>();
            var helper = new OneDriveHelper(mockTracer.Object, mockSettings.Object, mockEnv.Object);
            MethodInfo methodInfo = helper.GetType().GetMethod("HandlingDeletion", BindingFlags.NonPublic | BindingFlags.Instance);

            // should be able to delete file
            change.Path = "/foo/bar.txt";
            methodInfo.Invoke(helper, new object[] { change, wwwroot });
            mockTracer.Verify(v => v.Trace(It.Is<string>(m => m.Equals("Deleted file " + change.Path)), It.Is<IDictionary<string, string>>(d => d.Count == 0)));

            // should be able to delete directory
            change.Path = "/wa";
            methodInfo.Invoke(helper, new object[] { change, wwwroot });
            mockTracer.Verify(v => v.Trace(It.Is<string>(m => m.Equals("Deleted directory " + change.Path)), It.Is<IDictionary<string, string>>(d => d.Count == 0)));
        }
    }
}
