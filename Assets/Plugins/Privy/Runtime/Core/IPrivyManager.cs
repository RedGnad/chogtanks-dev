using System.Threading.Tasks;

namespace Privy
{
    public interface IPrivyManager
    {
        /// <summary>
        /// Gets the current instance of the Privy.
        /// </summary>
        IPrivy Instance { get; }

        /// <summary>
        /// Initializes the Privy instance with the specified configuration.
        /// </summary>
        /// <param name="config">The configuration to initialize the Privy instance with.</param>
        /// <returns>The initialized Privy instance.</returns>
        IPrivy Initialize(PrivyConfig config);

        /// <summary>
        /// Waits until the Privy instance is fully initialized and ready.
        /// </summary>
        /// <returns>A task that completes when the Privy instance is ready.</returns>
        Task AwaitReady();
    }
}