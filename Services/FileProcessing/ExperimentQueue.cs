// File: Services/FileProcessing/ExperimentQueue.cs

using API_Backend.Models;
using System.Threading.Channels;
using API_backend.Models;

namespace API_backend.Services.FileProcessing
{
    /// <summary>
    /// Implementation of a background task queue for experiments.
    /// </summary>
    public class ExperimentQueue : IExperimentQueue
    {
        private readonly Channel<ExperimentRequest> _queue;

        public ExperimentQueue()
        {
            // Configure the channel with appropriate options
            var options = new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            };
            _queue = Channel.CreateUnbounded<ExperimentRequest>(options);
        }

        /// <summary>
        /// Adds an experiment to the queue.
        /// </summary>
        /// <param name="experiment">The experiment request to enqueue.</param>
        public void QueueExperiment(ExperimentRequest experiment)
        {
            if (experiment == null)
                throw new ArgumentNullException(nameof(experiment));

            _queue.Writer.TryWrite(experiment);
        }

        /// <summary>
        /// Dequeues an experiment from the queue.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The next experiment request.</returns>
        public async ValueTask<ExperimentRequest> DequeueAsync(CancellationToken cancellationToken)
        {
            var experiment = await _queue.Reader.ReadAsync(cancellationToken);
            return experiment;
        }
    }
}