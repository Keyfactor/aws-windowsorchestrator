using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.CertificateManager;
using Amazon.CertificateManager.Model;
using Keyfactor.AnyAgent.AwsCertificateManager.Jobs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Keyfactor.AnyAgent.AwsCertificateManager.Tests
{
    [TestClass]
    public class AcmConfigurableTests
    {
        [TestMethod]
        public void TestInternalGetSetProperties()
        {
            var acmConfigTest = new Mock<AcmConfigurable> { CallBase = true };
            acmConfigTest.SetupAllProperties();
            acmConfigTest.Object.AccessKey = "TestAccessKey";
            var y = acmConfigTest.Object.AccessKey;
            Assert.AreEqual(y, acmConfigTest.Object.AccessKey);
            Assert.IsInstanceOfType(y, typeof(string));

            acmConfigTest.Object.SecretKey = "TestSecretKey";
            var z = acmConfigTest.Object.SecretKey;
            Assert.AreEqual(z, acmConfigTest.Object.SecretKey);
            Assert.IsInstanceOfType(z, typeof(string));
        }
    }
}
