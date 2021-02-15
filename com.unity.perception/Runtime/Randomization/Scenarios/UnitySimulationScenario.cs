using System;
using Unity.Simulation;
using UnityEngine.Perception.GroundTruth;

namespace UnityEngine.Perception.Randomization.Scenarios
{
    /// <summary>
    /// Defines a scenario that is compatible with the Run in Unity Simulation window
    /// </summary>
    /// <typeparam name="T">The type of constants to serialize</typeparam>
    public abstract class UnitySimulationScenario<T> : Scenario<T> where T : UnitySimulationScenarioConstants, new()
    {
        const string k_ScenarioIterationMetricDefinitionId = "DB1B258E-D1D0-41B6-8751-16F601A2E230";
        MetricDefinition m_IterationMetricDefinition;

        /// <summary>
        /// The Unity capture package cannot capture the first frame when running locally so this flag is used to
        /// track whether the first frame has been skipped.
        /// </summary>
        bool m_SkippedFirstFrame;

        /// <inheritdoc/>
        protected override bool isScenarioReadyToStart
        {
            get
            {
                if (!Configuration.Instance.IsSimulationRunningInCloud() && !m_SkippedFirstFrame)
                {
                    m_SkippedFirstFrame = true;
                    return false;
                }
                return true;
            }
        }

        /// <inheritdoc/>
        protected sealed override bool isScenarioComplete => currentIteration >= constants.totalIterations;

        /// <inheritdoc/>
        protected sealed override void IncrementIteration()
        {
            currentIteration += constants.instanceCount;
        }

        protected override void OnAwake()
        {
            m_IterationMetricDefinition = DatasetCapture.RegisterMetricDefinition(
                "scenario_iteration", "Iteration information for dataset sequences",
                Guid.Parse(k_ScenarioIterationMetricDefinitionId));
        }

        /// <inheritdoc/>
        protected override void OnStart()
        {
            var randomSeedMetricDefinition = DatasetCapture.RegisterMetricDefinition(
                "random-seed",
                "The random seed used to initialize the random state of the simulation. Only triggered once per simulation.",
                Guid.Parse("14adb394-46c0-47e8-a3f0-99e754483b76"));
            DatasetCapture.ReportMetric(randomSeedMetricDefinition, new[] { genericConstants.randomSeed });

            if (Configuration.Instance.IsSimulationRunningInCloud())
            {
                DeserializeFromFile(new Uri(Configuration.Instance.SimulationConfig.app_param_uri).LocalPath);
                constants.instanceIndex = int.Parse(Configuration.Instance.GetInstanceId()) - 1;
            }
            else
                base.OnStart();
            currentIteration = constants.instanceIndex;
        }

        protected override void OnIterationStart()
        {
            DatasetCapture.StartNewSequence();
            ResetRandomStateOnIteration();
            DatasetCapture.ReportMetric(m_IterationMetricDefinition, new[]
            {
                new IterationMetricData { iteration = currentIteration }
            });
        }

        /// <inheritdoc/>
        protected override void OnComplete()
        {
            DatasetCapture.ResetSimulation();
            Manager.Instance.Shutdown();
        }

        /// <inheritdoc/>
        protected override void OnIdle()
        {
            if (!Manager.FinalUploadsDone)
                Quit();
        }


        struct IterationMetricData
        {
            // ReSharper disable once NotAccessedField.Local
            public int iteration;
        }
    }
}
