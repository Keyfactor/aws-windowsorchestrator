using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Keyfactor.Platform.Extensions.Agents.Interfaces;

namespace Keyfactor.AnyAgent.AwsCertificateManager.Tests
{
    [TestClass]
    public class AssemblyTests
    {
        private const string FolderPath = @"../../../../AnyAgentAwsCertificateManager/Keyfactor.AnyAgent.AwsCertificateManager/bin/Debug/";
        private const string AssemblyName = @"Keyfactor.AnyAgent.AwsCertificateManager.dll";

        [TestMethod, TestCategory("IgnoreOnBuild")]
        public void AssemblyReflectsIncludedJobTypes()
        {
            var dllFile = new FileInfo($"{FolderPath}{AssemblyName}");
            var assembly = Assembly.LoadFile(dllFile.FullName);

            var jobExtensionTypes = new List<AgentJobExtensionType>();

            if (assembly == null)
            {
                throw new ArgumentNullException("Assembly cannot be null");
            }

            var extensionType = typeof(IAgentJobExtension);

            var extensionClasses = GetLoadableTypes(assembly)
                .Where(type =>
                    type.GetInterfaces()
                        .Any(i => i.FullName ==
                                  extensionType
                                      .FullName) // Checking just the fully-qualified namespace/class, no assembly version
                    && type.IsClass
                    && !type.IsAbstract)
                .ToList();

            if (extensionClasses.Count != 0)
                foreach (var type in extensionClasses)
                {
                    extensionType.IsAssignableFrom(type).Should().BeTrue();

                    var instance = (IAgentJobExtension)Activator.CreateInstance(type);

                    var jobExtensionType = new AgentJobExtensionType
                    {
                        Assembly = assembly.GetName().Name,
                        Class = type.FullName,
                        JobType = $"{instance.GetStoreType()}{instance.GetJobClass()}", // <----- bug?
                        ShortName = instance.GetStoreType(),
                        Directory = FolderPath
                    };

                    var dependecies = assembly.GetReferencedAssemblies();
                    foreach (var dependency in dependecies)
                    {
                        var localPath = $"{FolderPath}\\{dependency.Name}.dll";
                        if (File.Exists(localPath))
                            jobExtensionType.LocalDependecies.Add(new AssemblyFileInfo
                            {
                                Name = dependency.Name,
                                Directory = FolderPath
                            });
                    }

                    jobExtensionTypes.Add(jobExtensionType);
                }

            jobExtensionTypes.Any(j => j.JobType == "AwsCerManInventory").Should().BeTrue();
            jobExtensionTypes.Any(j => j.JobType == "AwsCerManManagement").Should().BeTrue();
        }

        private IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(type => type != null);
            }
        }
    }

    public class AgentJobExtensionType : MarshalByRefObject
    {
        public string Assembly { get; set; }
        public string Class { get; set; }
        public string FullName => $"{Class}, {Assembly}";
        public string ShortName { get; set; }
        public string Directory { get; set; }
        public string JobType { get; set; }
        public List<AssemblyFileInfo> LocalDependecies { get; set; } = new List<AssemblyFileInfo>();
    }

    public class AssemblyFileInfo : MarshalByRefObject
    {
        public string Name { get; set; }
        public string Directory { get; set; }
        public string FullPath => $"{Directory}\\{Name}.dll";
    }
}
