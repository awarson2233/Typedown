using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Text.RegularExpressions;

namespace Typedown.Test.ArchitectureTests
{
    [TestClass]
    public class Phase6AppActivationBoundaryTests
    {
        private static readonly string RepoRoot = Path.GetFullPath(Path.Combine(TestContext.TestRunDirectory, "..", "..", "..", ".."));

        public TestContext TestContext { get; set; }

        [TestMethod]
        public void AppActivationInterface_DoesNotExposePipeMutexOrPlatformDispatcherTypes()
        {
            var source = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.Core", "Interfaces", "IAppActivationService.cs"));

            AssertNoTypeReference(source, "NamedPipeServerStream");
            AssertNoTypeReference(source, "NamedPipeClientStream");
            AssertNoTypeReference(source, "Mutex");
            AssertNoTypeReference(source, "CoreDispatcher");
            AssertNoTypeReference(source, "Windows.UI");
            AssertHasTypeReference(source, "AppActivationKind");
            AssertHasTypeReference(source, "ForwardFailedStartNewInstance");
        }

        [TestMethod]
        public void App_UsesActivationServiceInsteadOfOwningMutexAndPipeImplementation()
        {
            var source = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown", "App.cs"));

            AssertNoTypeReference(source, "NamedPipeServerStream");
            AssertNoTypeReference(source, "NamedPipeClientStream");
            AssertNoTypeReference(source, "Mutex");
            AssertNoTypeReference(source, "SetForegroundWindow");
            AssertHasTypeReference(source, "IAppActivationService");
            AssertHasTypeReference(source, "ActivationRequested");
            AssertHasTypeReference(source, "StartListening");
        }

        [TestMethod]
        public void App_Launch_StartsNewInstanceForPrimaryLaunchAndForwardFailure()
        {
            var source = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown", "App.cs"));

            AssertContainsInOrder(
                source,
                "activationResult.Kind == AppActivationKind.FirstLaunch",
                "activationResult.Kind == AppActivationKind.ForwardFailedStartNewInstance",
                "LaunchNewApplication();");
        }

        [TestMethod]
        public void Injection_RegistersActivationServiceAsShellSingleton()
        {
            var source = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown", "Injection.cs"));

            AssertHasTypeReference(source, "AddSingleton<IAppActivationService, AppActivationService>()");
        }

        [TestMethod]
        public void AppActivationService_ContainsPipeForwardingAndForegroundActivationBehavior()
        {
            var source = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown", "Services", "AppActivationService.cs"));

            AssertHasTypeReference(source, "NamedPipeServerStream");
            AssertHasTypeReference(source, "NamedPipeClientStream");
            AssertHasTypeReference(source, "SetForegroundWindow");
            AssertHasTypeReference(source, "ActivationRequested");
            AssertHasTypeReference(source, "RunIdleAsync");
        }

        [TestMethod]
        public void AppActivationService_ReturnsExplicitFallbackKindWhenPipeForwardingFails()
        {
            var source = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown", "Services", "AppActivationService.cs"));

            AssertContainsInOrder(
                source,
                "if (TryForwardToPrimaryInstance(commandLineArgs, out var windowHandle))",
                "return new(AppActivationKind.ForwardedToExistingInstance, windowHandle);",
                "return new(AppActivationKind.ForwardFailedStartNewInstance);");
        }

        private static void AssertNoTypeReference(string source, string typeName)
        {
            Assert.IsFalse(Regex.IsMatch(StripComments(source), CreateBoundaryPattern(typeName)), $"Unexpected reference to {typeName}.");
        }

        private static void AssertHasTypeReference(string source, string typeName)
        {
            Assert.IsTrue(Regex.IsMatch(StripComments(source), CreateBoundaryPattern(typeName)), $"Expected reference to {typeName}.");
        }

        private static string CreateBoundaryPattern(string typeName)
        {
            return $@"(?<![A-Za-z0-9_]){Regex.Escape(typeName)}(?![A-Za-z0-9_])";
        }

        private static string StripComments(string source)
        {
            source = Regex.Replace(source, @"//.*?$", string.Empty, RegexOptions.Multiline);
            source = Regex.Replace(source, @"/\*[\s\S]*?\*/", string.Empty);
            return source;
        }
    }
}
