// Copyright 2019 Maintainers of NUKE.
// Distributed under the MIT License.
// https://github.com/nuke-build/nuke/blob/master/LICENSE

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Nuke.Common.CI.AzurePipelines.Configuration;
using Nuke.Common.Execution;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.IO.PathConstruction;

namespace Nuke.Common.CI.AzurePipelines
{
    [PublicAPI]
    public class AzurePipelinesAttribute : ChainedConfigurationAttributeBase
    {
        private readonly AzurePipelinesImage[] _images;

        public AzurePipelinesAttribute(AzurePipelinesImage image, params AzurePipelinesImage[] images)
        {
            _images = new[] { image }.Concat(images).ToArray();
        }

        private string ConfigurationFile => NukeBuild.RootDirectory / "azure-pipelines.yml";

        protected override HostType HostType => HostType.AzurePipelines;
        protected override IEnumerable<string> GeneratedFiles => new[] { ConfigurationFile };
        protected override IEnumerable<string> RelevantTargetNames => InvokedTargets;

        public string[] InvokedTargets { get; set; } = new string[0];

        public bool TriggerBatch { get; set; }
        public string[] TriggerBranchesInclude { get; set; } = new string[0];
        public string[] TriggerBranchesExclude { get; set; } = new string[0];
        public string[] TriggerTagsInclude { get; set; } = new string[0];
        public string[] TriggerTagsExclude { get; set; } = new string[0];
        public string[] TriggerPathsInclude { get; set; } = new string[0];
        public string[] TriggerPathsExclude { get; set; } = new string[0];

        public bool PullRequestsAutoCancel { get; set; }
        public string[] PullRequestsBranchesInclude { get; set; } = new string[0];
        public string[] PullRequestsBranchesExclude { get; set; } = new string[0];
        public string[] PullRequestsPathsInclude { get; set; } = new string[0];
        public string[] PullRequestsPathsExclude { get; set; } = new string[0];

        protected override CustomFileWriter CreateWriter()
        {
            return new CustomFileWriter(ConfigurationFile, indentationFactor: 2, commentPrefix: "#");
        }

        protected override ConfigurationEntity GetConfiguration(NukeBuild build, IReadOnlyCollection<ExecutableTarget> relevantTargets)
        {
            return new AzurePipelinesConfiguration
                   {
                       Stages = _images.Select(x => GetStage(x, relevantTargets)).ToArray()
                   };
        }

        protected virtual AzurePipelinesStage GetStage(
            AzurePipelinesImage image,
            IReadOnlyCollection<ExecutableTarget> relevantTargets)
        {
            var lookupTable = new LookupTable<ExecutableTarget, AzurePipelinesJob>();
            var jobs = relevantTargets
                .Select(x => (ExecutableTarget: x, Job: GetJob(x, lookupTable)))
                .ForEachLazy(x => lookupTable.Add(x.ExecutableTarget, x.Job))
                .Select(x => x.Job).ToArray();

            return new AzurePipelinesStage
                   {
                       Name = image.GetValue().Replace("-", "_"),
                       DisplayName = image.GetValue(),
                       Image = image,
                       Dependencies = new AzurePipelinesStage[0],
                       Jobs = jobs
                   };
        }

        protected virtual AzurePipelinesJob GetJob(
            ExecutableTarget executableTarget,
            LookupTable<ExecutableTarget, AzurePipelinesJob> jobs)
        {
            var (partitionName, totalPartitions) = ArtifactExtensions.Partitions.GetValueOrDefault(executableTarget.Definition);
            var publishedArtifacts = ArtifactExtensions.ArtifactProducts[executableTarget.Definition]
                .Select(x => (AbsolutePath) x)
                .Select(x => x.DescendantsAndSelf(y => y.Parent).FirstOrDefault(y => !y.ToString().ContainsOrdinalIgnoreCase("*")))
                .Distinct()
                .Select(x => x.ToString().TrimStart(x.Parent.ToString()).TrimStart('/', '\\')).ToArray();

            // var artifactDependencies = (
            //     from artifactDependency in ArtifactExtensions.ArtifactDependencies[executableTarget.Definition]
            //     let dependency = executableTarget.ExecutionDependencies.Single(x => x.Factory == artifactDependency.Item1)
            //     let rules = (artifactDependency.Item2.Any()
            //             ? artifactDependency.Item2
            //             : ArtifactExtensions.ArtifactProducts[dependency.Definition])
            //         .Select(GetArtifactRule).ToArray()
            //     select new TeamCityArtifactDependency
            //            {
            //                BuildType = buildTypes[dependency].Single(x => x.Partition == null),
            //                ArtifactRules = rules
            //            }).ToArray<TeamCityDependency>();

            var chainLinkNames = GetInvokedTargets(executableTarget).ToArray();
            var dependencies = GetTargetDependencies(executableTarget).SelectMany(x => jobs[x]).ToArray();
            return new AzurePipelinesJob
                   {
                       Name = executableTarget.Name,
                       DisplayName = executableTarget.Name,
                       ScriptPath = PowerShellScript,
                       Dependencies = dependencies,
                       Parallel = totalPartitions,
                       PartitionName = partitionName,
                       InvokedTargets = chainLinkNames,
                       PublishArtifacts = publishedArtifacts
                   };
        }

        protected virtual string GetArtifact(string artifact)
        {
            if (IsDescendantPath(NukeBuild.RootDirectory, artifact))
                artifact = GetRelativePath(NukeBuild.RootDirectory, artifact);

            return HasPathRoot(artifact)
                ? artifact
                : (UnixRelativePath) artifact;
        }
    }
}
