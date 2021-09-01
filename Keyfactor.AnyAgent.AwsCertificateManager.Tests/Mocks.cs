using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using Amazon.CertificateManager.Model;
using Keyfactor.Platform.Extensions.Agents;
using Keyfactor.Platform.Extensions.Agents.Delegates;
using Keyfactor.Platform.Extensions.Agents.Enums;
using Moq;
using Newtonsoft.Json;

[assembly: InternalsVisibleTo("Keyfactor.AnyAgent.vThunder")]
namespace Keyfactor.AnyAgent.AwsCertificateManager.Tests
{
    public static class Mocks
    {
        public static AnyJobConfigInfo GetMockConfig(AnyJobOperationType jobOperationType,string pfxPassword="")
        {
            var ajStore = new AnyJobStoreInfo
            {
                Inventory = new List<AnyJobInventoryItem>(),
                StorePath = "US East 1",
                ClientMachine = "someusers@aws.com",
                Storetype = 1
            };
            var ajJob = new AnyJobJobInfo
            {
                OperationType = jobOperationType,
                Alias = "testJob",
                JobId = Guid.NewGuid(),
                JobTypeId = Guid.NewGuid(),
                PfxPassword = pfxPassword,
                EntryContents = @"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"
            };
            var ajServer = new AnyJobServerInfo { Username = "someAwsApiKey", Password = "someAwsApiSecret", UseSSL = true };
            var ajc = new AnyJobConfigInfo
            {
                Store = ajStore,
                Job = ajJob,
                Server = ajServer
            };

            return ajc;
        }
        public static Mock<SubmitInventoryUpdate> GetSubmitInventoryDelegateMock()
        {
            return new Mock<SubmitInventoryUpdate>();
        }

        public static Mock<SubmitEnrollmentRequest> GetSubmitEnrollmentDelegateMock()
        {
            return new Mock<SubmitEnrollmentRequest>();
        }

        public static Mock<SubmitDiscoveryResults> GetSubmitDiscoveryDelegateMock()
        {
            return new Mock<SubmitDiscoveryResults>();
        }

        public static DeleteCertificateResponse GetDeleteResponseOk()
        {
            var delResponse=new DeleteCertificateResponse();
            delResponse.HttpStatusCode = HttpStatusCode.OK;
            return delResponse;
        }

        public static DeleteCertificateResponse GetDeleteResponseError()
        {
            var delResponse = new DeleteCertificateResponse();
            delResponse.HttpStatusCode = HttpStatusCode.InternalServerError;
            return delResponse;
        }

        public static ImportCertificateResponse GetImportCertificateResponse()
        {
            var icrResponse=new ImportCertificateResponse();
            icrResponse.HttpStatusCode = HttpStatusCode.OK;
            icrResponse.CertificateArn =
                "arn:aws:acm:region:123456789012:certificate/12345678-1234-1234-1234-123456789012";
            return icrResponse;
        }

        public static ImportCertificateResponse GetImportCertificateResponseNotOk()
        {
            var icrResponse = new ImportCertificateResponse();
            icrResponse.HttpStatusCode = HttpStatusCode.InternalServerError;
            icrResponse.CertificateArn =
                "arn:aws:acm:region:123456789012:certificate/12345678-1234-1234-1234-123456789012";
            return icrResponse;
        }

        public static DescribeCertificateResponse GetDescribeCertificateResponse(string certType)
        {
            string response = @"{
                              ""Certificate"": {
                                ""CertificateArn"": ""arn:aws:acm:us-east-1:111122223333:certificate/12345678-1234-1234-1234-123456789012"",
                                ""CreatedAt"": ""2020-11-04"",
                                ""DomainName"": ""example.com"",
                                ""DomainValidationOptions"": [
                                  {
                                    ""DomainName"": ""example.com"",
                                    ""ValidationDomain"": ""example.com"",
                                    ""ValidationEmails"": [
                                      ""hostmaster@example.com"",
                                      ""admin@example.com"",
                                      ""admin@example.com.whoisprivacyservice.org"",
                                      ""tech@example.com.whoisprivacyservice.org"",
                                      ""owner@example.com.whoisprivacyservice.org"",
                                      ""postmaster@example.com"",
                                      ""webmaster@example.com"",
                                      ""administrator@example.com""
                                    ]
                                  },
                                  {
                                    ""DomainName"": ""www.example.com"",
                                    ""ValidationDomain"": ""www.example.com"",
                                    ""ValidationEmails"": [
                                      ""hostmaster@example.com"",
                                      ""admin@example.com"",
                                      ""admin@example.com.whoisprivacyservice.org"",
                                      ""tech@example.com.whoisprivacyservice.org"",
                                      ""owner@example.com.whoisprivacyservice.org"",
                                      ""postmaster@example.com"",
                                      ""webmaster@example.com"",
                                      ""administrator@example.com""
                                    ]
                                  }
                                ],
                                ""InUseBy"": [
                                  ""arn:aws:cloudfront::111122223333:distribution/E12KXPQHVLSYVC""
                                ],
                                ""IssuedAt"": ""2020-11-04"",
                                ""Issuer"": ""Amazon"",
                                ""KeyAlgorithm"": ""RSA-2048"",
                                ""NotAfter"": ""2020-11-04"",
                                ""NotBefore"": ""2020-11-04"",
                                ""Renewal Elegibility"": ""ELIGIBLE"",
                                ""RenewalSummary"": { 
                                     ""DomainValidationOptions"": [ 
                                        { 
                                           ""DomainName"": ""www.example.com"",
                                           ""ResourceRecord"": { 
                                              ""Name"": ""example"",
                                              ""Type"": ""CNAME"",
                                              ""Value"": ""example""
                                           },
                                           ""ValidationDomain"": ""www.amazon.com"",
                                           ""ValidationEmails"": [ ""example@amazon.com"" ],
                                           ""ValidationMethod"": ""DNS"",
                                           ""ValidationStatus"": ""SUCCESS""
                                        }
                                     ],
                                     ""RenewalStatus"": ""SUCCESS"",
                                     ""UpdatedAt"": ""2020-11-04""
                                  },
                                ""Serial"": ""07:71:71:f4:6b:e7:bf:63:87:e6:ad:3c:b2:0f:d0:5b"",
                                ""SignatureAlgorithm"": ""SHA256WITHRSA"",
                                ""Status"": ""ISSUED"",
                                ""Subject"": ""CN=example.com"",
                                ""SubjectAlternativeNames"": [
                                  ""example.com"",
                                  ""www.example.com""
                                ],
                                ""Type"":""IMPORTED""
                              }
                            }";
            var cerResponse= JsonConvert.DeserializeObject<DescribeCertificateResponse>(response);
            cerResponse.Certificate.Type = certType;
            return cerResponse;
        }

        public static GetCertificateResponse GetJunkCertificate()
        {
            string response = @"{
                                  ""Certificate"":
                                    ""
                                MIIBwjCCASugAwIBAgIJAJmlrFH2kNcOMA0GCSqGSIb3DQEBCwUAMCMxCzAJBgNV
                                BAYTAlVTMRQwEgYDVQQDDAsxeTVlMDN2dGYxczAeFw0yMDEyMTYxNTI0MzRaFw0y
                                MjEyMTYxNTI0MzRaMCMxCzAJBgNVBAYTAlVTMRQwEgYDVQQDDAsxeTVlMDN2dGYx
                                czCBnzANBgkqhkiG9w0BAQEFAAOBjQAwgYkCgYEA47y39aSnVRaRXuHD3S8ZcImf
                                UUGZ8Iho6ecWFEp61zyOgyGggvH+mjmOV0+/v1CExRnLFaA4JSiR9fyNpvN0Ht2N
                                8KUqNAn3WaOG4jbC3BnD0Y9hnddLXGKMkQspP+k62DnoW6YYKN/yIivBlbTA70qx
                                GSvBx26PN5gQisjetC8CAwEAATANBgkqhkiG9w0BAQsFAAOBgQCJ9yNFPVE2A3F9
                                FmqPjG25XrJlrj+/i6WyAYPiNjUAf/dfbkX8YfcdSsmLiM/lcv1CF1ar514jeXZD
                                eGpg/ZZ4RPk835jVzGt20k1lZBlv23pyEbv7RC7h4TsOHX8LM+ZogIto7b/ZgZCI
                                WFIMvt9Qb2Ee3DSP/wRnPIV3Dlaq0w==
                                ""
                                }";
            return JsonConvert.DeserializeObject<GetCertificateResponse>(response);
        }

        public static GetCertificateResponse GetCertificateResponse()
        {
            string response = @"{
                                  ""Certificate"":
                                    ""-----BEGIN CERTIFICATE-----
                                MIIBwjCCASugAwIBAgIJAJmlrFH2kNcOMA0GCSqGSIb3DQEBCwUAMCMxCzAJBgNV
                                BAYTAlVTMRQwEgYDVQQDDAsxeTVlMDN2dGYxczAeFw0yMDEyMTYxNTI0MzRaFw0y
                                MjEyMTYxNTI0MzRaMCMxCzAJBgNVBAYTAlVTMRQwEgYDVQQDDAsxeTVlMDN2dGYx
                                czCBnzANBgkqhkiG9w0BAQEFAAOBjQAwgYkCgYEA47y39aSnVRaRXuHD3S8ZcImf
                                UUGZ8Iho6ecWFEp61zyOgyGggvH+mjmOV0+/v1CExRnLFaA4JSiR9fyNpvN0Ht2N
                                8KUqNAn3WaOG4jbC3BnD0Y9hnddLXGKMkQspP+k62DnoW6YYKN/yIivBlbTA70qx
                                GSvBx26PN5gQisjetC8CAwEAATANBgkqhkiG9w0BAQsFAAOBgQCJ9yNFPVE2A3F9
                                FmqPjG25XrJlrj+/i6WyAYPiNjUAf/dfbkX8YfcdSsmLiM/lcv1CF1ar514jeXZD
                                eGpg/ZZ4RPk835jVzGt20k1lZBlv23pyEbv7RC7h4TsOHX8LM+ZogIto7b/ZgZCI
                                WFIMvt9Qb2Ee3DSP/wRnPIV3Dlaq0w==
                                -----END CERTIFICATE-----""
                                }";
            return JsonConvert.DeserializeObject<GetCertificateResponse>(response);
        }

        public static GetCertificateRequest GetCertificateRequest()
        {
            string request = @"{
                                  ""CertificateArn"": ""arn:aws:acm:us-east-1:111122223333:certificate/12345678-1234-1234-1234-123456789012""
                                }";
            return JsonConvert.DeserializeObject<GetCertificateRequest>(request);
        }


        public static ListCertificatesResponse GetCerts()
        {
            var strResponse = @"{
                                    ""CertificateSummaryList"": [
                                        {
                                            ""CertificateArn"": ""arn:aws:acm:us-east-2:220531701667:certificate/ab9afce6-d46d-49a0-8403-80e24bc2b16c"",
                                            ""DomainName"": ""testapproval1.demo.cms""
                                        },
                                        {
                                            ""CertificateArn"": ""arn:aws:acm:us-east-2:220531701667:certificate/674dbf84-bb76-40d4-adf4-e5809fea3482"",
                                            ""DomainName"": ""JacktestAWS-1""
                                        },
                                        {
                                            ""CertificateArn"": ""arn:aws:acm:us-east-2:220531701667:certificate/aeb0bc63-a32a-4403-93ec-cb54dd162a1c"",
                                            ""DomainName"": ""server1.css-vstc.com""
                                        },
                                        {
                                            ""CertificateArn"": ""arn:aws:acm:us-east-2:220531701667:certificate/4d975a31-ec2d-4e58-bf58-a2e93c2a4620"",
                                            ""DomainName"": ""server1.css-vstc.com""
                                        },
                                        {
                                            ""CertificateArn"": ""arn:aws:acm:us-east-2:220531701667:certificate/d63eed2c-2af9-4f03-bc7b-9c2a007a8235"",
                                            ""DomainName"": ""JT0108AWS-1.css-vstc.com""
                                        },
                                        {
                                            ""CertificateArn"": ""arn:aws:acm:us-east-2:220531701667:certificate/f8898646-2b0b-4852-a427-a1a01d878c20"",
                                            ""DomainName"": ""www.keyfactor.com""
                                        },
                                        {
                                            ""CertificateArn"": ""arn:aws:acm:us-east-2:220531701667:certificate/cf7f06ac-4f70-42b4-94a9-f0ff2c543acf"",
                                            ""DomainName"": ""www.keyfactortest.com""
                                        },
                                        {
                                            ""CertificateArn"": ""arn:aws:acm:us-east-2:220531701667:certificate/57c7d5ec-2a48-483c-84b9-15aef7c05d8e"",
                                            ""DomainName"": ""AWS.test.9.2020""
                                        },
                                        {
                                            ""CertificateArn"": ""arn:aws:acm:us-east-2:220531701667:certificate/c8858109-b4df-46a5-b35f-0bae2576e7c8"",
                                            ""DomainName"": ""EV.test.AWS""
                                        },
                                        {
                                            ""CertificateArn"": ""arn:aws:acm:us-east-2:220531701667:certificate/297cc21f-9610-4c3e-9043-97f829bb5d73"",
                                            ""DomainName"": ""www.boingy.com""
                                        }
                                    ]
                                }";
            var sslColResponse = JsonConvert.DeserializeObject<ListCertificatesResponse>(strResponse);
            return sslColResponse;
        }
    }
}
