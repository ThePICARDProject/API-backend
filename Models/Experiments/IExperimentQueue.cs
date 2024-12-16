using API_Backend.Models;

namespace API_Backend.Models;

public interface IExperimentQueue
{
    void QueueExperiment(ExperimentRequest experiment);
    ValueTask<ExperimentRequest> DequeueAsync(CancellationToken cancellationToken);
}