using System;
using Amazon.CertificateManager;
using Amazon.CertificateManager.Model;
using FluentAssertions;
using Keyfactor.AnyAgent.AwsCertificateManager.Jobs;
using Keyfactor.Platform.Extensions.Agents;
using Keyfactor.Platform.Extensions.Agents.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using Org.BouncyCastle.Pkcs;

namespace Keyfactor.AnyAgent.AwsCertificateManager.Tests
{
    [TestClass]
    public class ManagementTests
    {
        [TestMethod]
        public void ReturnsTheCorrectJobClassAndStoreType()
        {
            var inventory = new Management();
            inventory.GetJobClass().Should().Be("Management");
            inventory.GetStoreType().Should().Be("AwsCerMan");
        }

        [TestMethod]
        public void TestInternalGetSetProperties()
        {
            var managementMock = new Mock<Management> { CallBase = true };
            managementMock.SetupAllProperties();
            var managementMockObject = managementMock.Object;
            var acm = new Mock<IAmazonCertificateManager> { CallBase = true };
            managementMockObject.AcmClient = acm.Object;
            var y = managementMockObject.AcmClient;
            Assert.IsInstanceOfType(y, typeof(IAmazonCertificateManager));
            var describeCertResponse = new Mock<DescribeCertificateResponse>() { CallBase = true };
            managementMockObject.DescribeCertificateResponse = describeCertResponse.Object;
            var z = managementMockObject.DescribeCertificateResponse;
            Assert.IsInstanceOfType(z, typeof(DescribeCertificateResponse));
            var importCertResponse = new Mock<ImportCertificateResponse>() { CallBase = true };
            managementMockObject.IcrResponse = importCertResponse.Object;
            var b = managementMockObject.IcrResponse;
            Assert.IsInstanceOfType(b, typeof(ImportCertificateResponse));
            var deleteCertResponse = new Mock<DeleteCertificateResponse>() { CallBase = true };
            managementMockObject.DeleteResponse = deleteCertResponse.Object;
            var c = managementMockObject.DeleteResponse;
            Assert.IsInstanceOfType(c, typeof(DeleteCertificateResponse));

        }

        [TestMethod]
        public void JobForPfxAddIsCalledWithSuccess()
        {
            var managementMock = new Mock<Management> { CallBase = true };
            var mockAcmClient = new Mock<IAmazonCertificateManager>() { CallBase = true };
            managementMock.Protected().Setup<IAmazonCertificateManager>("AcmClient").Returns(mockAcmClient.Object);
            var mockDescribeCertResponse = Mocks.GetDescribeCertificateResponse("IMPORTED");
            managementMock.Protected().Setup<DescribeCertificateResponse>("DescribeCertificateResponse").Returns(mockDescribeCertResponse);
            var mockImportCertResponse = Mocks.GetImportCertificateResponse();
            managementMock.Protected().Setup<ImportCertificateResponse>("IcrResponse").Returns(mockImportCertResponse);
            var config = Mocks.GetMockConfig(AnyJobOperationType.Add, "rB7DYWKnVvrk");
            var result = managementMock.Object.processJob(config, Mocks.GetSubmitInventoryDelegateMock().Object,
                Mocks.GetSubmitEnrollmentDelegateMock().Object, Mocks.GetSubmitDiscoveryDelegateMock().Object);
            result.Status.Should().Equals(2);
            result.Message.Should().Be("Management Complete");
        }

        [TestMethod]
        public void JobForPfxNotImportedType()
        {
            var managementMock = new Mock<Management> { CallBase = true };
            var mockAcmClient = new Mock<IAmazonCertificateManager>() { CallBase = true };
            managementMock.Protected().Setup<IAmazonCertificateManager>("AcmClient").Returns(mockAcmClient.Object);
            var mockDescribeCertResponse = Mocks.GetDescribeCertificateResponse("SOMEOTHERTYPE");
            managementMock.Protected().Setup<DescribeCertificateResponse>("DescribeCertificateResponse").Returns(mockDescribeCertResponse);
            var mockImportCertResponse = Mocks.GetImportCertificateResponse();
            managementMock.Protected().Setup<ImportCertificateResponse>("IcrResponse").Returns(mockImportCertResponse);
            var config = Mocks.GetMockConfig(AnyJobOperationType.Add, "rB7DYWKnVvrk");
            var result = managementMock.Object.processJob(config, Mocks.GetSubmitInventoryDelegateMock().Object,
                Mocks.GetSubmitEnrollmentDelegateMock().Object, Mocks.GetSubmitDiscoveryDelegateMock().Object);
            result.Status.Should().Equals(4);
            result.Message.Should().Be("Amazon Web Services Certificate Manager only supports overwriting user-imported certificates.");
        }


        [TestMethod]
        public void JobForNonPfxNotImportedType()
        {
            var managementMock = new Mock<Management> { CallBase = true };
            var mockAcmClient = new Mock<IAmazonCertificateManager>() { CallBase = true };
            managementMock.Protected().Setup<IAmazonCertificateManager>("AcmClient").Returns(mockAcmClient.Object);
            var mockDescribeCertResponse = Mocks.GetDescribeCertificateResponse("IMPORTED");
            managementMock.Protected().Setup<DescribeCertificateResponse>("DescribeCertificateResponse").Returns(mockDescribeCertResponse);
            var mockImportCertResponse = Mocks.GetImportCertificateResponse();
            managementMock.Protected().Setup<ImportCertificateResponse>("IcrResponse").Returns(mockImportCertResponse);
            var config = Mocks.GetMockConfig(AnyJobOperationType.Add);
            var result = managementMock.Object.processJob(config, Mocks.GetSubmitInventoryDelegateMock().Object,
                Mocks.GetSubmitEnrollmentDelegateMock().Object, Mocks.GetSubmitDiscoveryDelegateMock().Object);
            result.Status.Should().Equals(4);
            result.Message.Should().Be("Certificate Must Be A PFX");
        }


        [TestMethod]
        public void JobForPfxNotImportedTypeCannotRetrievePrivateKey()
        {
            var managementMock = new Mock<Management> { CallBase = true };
            var mockAcmClient = new Mock<IAmazonCertificateManager>() { CallBase = true };
            managementMock.Protected().Setup<IAmazonCertificateManager>("AcmClient").Returns(mockAcmClient.Object);
            var mockDescribeCertResponse = Mocks.GetDescribeCertificateResponse("IMPORTED");
            managementMock.Protected().Setup<DescribeCertificateResponse>("DescribeCertificateResponse").Returns(mockDescribeCertResponse);
            var mockImportCertResponse = Mocks.GetImportCertificateResponse();
            managementMock.Protected().Setup<ImportCertificateResponse>("IcrResponse").Returns(mockImportCertResponse);
            var mockPrivateKey = new Mock<AsymmetricKeyEntry>{CallBase = true};
            managementMock.Protected().Setup<AsymmetricKeyEntry>("KeyEntry").Returns((AsymmetricKeyEntry)null);
            var config = Mocks.GetMockConfig(AnyJobOperationType.Add, "rB7DYWKnVvrk");
            var result = managementMock.Object.processJob(config, Mocks.GetSubmitInventoryDelegateMock().Object,
                Mocks.GetSubmitEnrollmentDelegateMock().Object, Mocks.GetSubmitDiscoveryDelegateMock().Object);
            result.Status.Should().Equals(4);
            result.Message.Should().Be("Unable to retrieve private key");
        }

        [TestMethod]
        public void JobForPfxAddIsCalledHttpStatusNotOk()
        {
            var managementMock = new Mock<Management> { CallBase = true };
            var mockAcmClient = new Mock<IAmazonCertificateManager>() { CallBase = true };
            managementMock.Protected().Setup<IAmazonCertificateManager>("AcmClient").Returns(mockAcmClient.Object);
            var mockDescribeCertResponse = Mocks.GetDescribeCertificateResponse("IMPORTED");
            managementMock.Protected().Setup<DescribeCertificateResponse>("DescribeCertificateResponse").Returns(mockDescribeCertResponse);
            var mockImportCertResponse = Mocks.GetImportCertificateResponseNotOk();
            managementMock.Protected().Setup<ImportCertificateResponse>("IcrResponse").Returns(mockImportCertResponse);
            var config = Mocks.GetMockConfig(AnyJobOperationType.Add, "rB7DYWKnVvrk");
            var result = managementMock.Object.processJob(config, Mocks.GetSubmitInventoryDelegateMock().Object,
                Mocks.GetSubmitEnrollmentDelegateMock().Object, Mocks.GetSubmitDiscoveryDelegateMock().Object);
            result.Status.Should().Equals(4);
            result.Message.Should().Be("Failure");
        }

        [TestMethod]
        public void JobForRemoveCertFromStore()
        {
            var managementMock = new Mock<Management> { CallBase = true };
            var mockAcmClient = new Mock<IAmazonCertificateManager>() { CallBase = true };
            managementMock.Protected().Setup<IAmazonCertificateManager>("AcmClient").Returns(mockAcmClient.Object);
            var mockDeleteResponse = Mocks.GetDeleteResponseOk();
            managementMock.Protected().Setup<DeleteCertificateResponse>("DeleteResponse").Returns(mockDeleteResponse);
            var config = Mocks.GetMockConfig(AnyJobOperationType.Remove, "rB7DYWKnVvrk");
            var result = managementMock.Object.processJob(config, Mocks.GetSubmitInventoryDelegateMock().Object,
                Mocks.GetSubmitEnrollmentDelegateMock().Object, Mocks.GetSubmitDiscoveryDelegateMock().Object);
            result.Status.Should().Equals(2);
            result.Message.Should().Be("Management Complete");
        }

        [TestMethod]
        public void JobForRemoveCertFromStoreHttpFailureCode()
        {
            var managementMock = new Mock<Management> { CallBase = true };
            var mockAcmClient = new Mock<IAmazonCertificateManager>() { CallBase = true };
            managementMock.Protected().Setup<IAmazonCertificateManager>("AcmClient").Returns(mockAcmClient.Object);
            var mockDeleteResponse = Mocks.GetDeleteResponseError();
            managementMock.Protected().Setup<DeleteCertificateResponse>("DeleteResponse").Returns(mockDeleteResponse);
            var config = Mocks.GetMockConfig(AnyJobOperationType.Remove, "rB7DYWKnVvrk");
            var result = managementMock.Object.processJob(config, Mocks.GetSubmitInventoryDelegateMock().Object,
                Mocks.GetSubmitEnrollmentDelegateMock().Object, Mocks.GetSubmitDiscoveryDelegateMock().Object);
            result.Status.Should().Equals(4);
            result.Message.Should().Be("Failure");
        }

        [TestMethod]
        public void JobForRemoveCertFromStoreException()
        {
            var managementMock = new Mock<Management> { CallBase = true };
            var mockAcmClient = new Mock<IAmazonCertificateManager>() { CallBase = true };
            managementMock.Protected().Setup<IAmazonCertificateManager>("AcmClient").Throws(new Exception("Cannot create AcmClient"));
            var mockDeleteResponse = Mocks.GetDeleteResponseError();
            managementMock.Protected().Setup<DeleteCertificateResponse>("DeleteResponse").Returns(mockDeleteResponse);
            var config = Mocks.GetMockConfig(AnyJobOperationType.Remove, "rB7DYWKnVvrk");
            var result = managementMock.Object.processJob(config, Mocks.GetSubmitInventoryDelegateMock().Object,
                Mocks.GetSubmitEnrollmentDelegateMock().Object, Mocks.GetSubmitDiscoveryDelegateMock().Object);
            result.Status.Should().Equals(4);
            result.Message.Should().Be("Cannot create AcmClient");
        }
    }
}
