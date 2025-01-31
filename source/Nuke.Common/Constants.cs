// Copyright 2019 Maintainers of NUKE.
// Distributed under the MIT License.
// https://github.com/nuke-build/nuke/blob/master/LICENSE

using System;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Nuke.Common.IO;

namespace Nuke.Common
{
    internal static class Constants
    {
        internal static readonly string[] KnownWords = { "GitHub", "NuGet", "MSBuild", "GitVersion" };

        internal const string ConfigurationFileName = ".nuke";

        internal const string TargetsSeparator = "+";
        internal const string InvokedTargetsParameterName = "Target";
        internal const string SkippedTargetsParameterName = "Skip";

        public const string VisualStudioDebugParameterName = "visual-studio-debug";
        internal const string CompletionParameterName = "shell-completion";

        [CanBeNull]
        internal static PathConstruction.AbsolutePath TryGetRootDirectoryFrom(string startDirectory)
        {
            return (PathConstruction.AbsolutePath) FileSystemTasks.FindParentDirectory(
                startDirectory,
                predicate: x => x.GetFiles(ConfigurationFileName).Any());
        }

        internal static PathConstruction.AbsolutePath GetTemporaryDirectory(PathConstruction.AbsolutePath rootDirectory)
        {
            return rootDirectory / ".tmp";
        }

        internal static PathConstruction.AbsolutePath GetCompletionFile(PathConstruction.AbsolutePath rootDirectory)
        {
            var completionFileName = CompletionParameterName + ".yml";
            return File.Exists(rootDirectory / completionFileName)
                ? rootDirectory / completionFileName
                : GetTemporaryDirectory(rootDirectory) / completionFileName;
        }

        internal static PathConstruction.AbsolutePath GetBuildAttemptFile(PathConstruction.AbsolutePath rootDirectory)
        {
            return GetTemporaryDirectory(rootDirectory) / "build-attempt.log";
        }

        public static PathConstruction.AbsolutePath GetVisualStudioDebugFile(PathConstruction.AbsolutePath rootDirectory)
        {
            return GetTemporaryDirectory(rootDirectory) / $"{VisualStudioDebugParameterName}.log";
        }
    }
}
