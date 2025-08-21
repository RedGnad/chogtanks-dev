using System;
using System.Threading.Tasks;

namespace Privy
{
    public class PrivyManager
    {
        private static PrivyImpl _privyInstance;

        // Property to access the Privy instance
        public static IPrivy Instance
        {
            get
            {
                if (_privyInstance == null)
                {
                    throw new Exception("Call PrivyManager.Initialize before attempting to get the Privy instance.");
                }

                return _privyInstance;
            }
        }

        // Method to initialize a single Privy instance
        public static IPrivy Initialize(PrivyConfig config)
        {
            if (_privyInstance == null)
            {
                _privyInstance = new PrivyImpl(config);
                //This function is called from a static class, which lets us bypass the inability to call async functions in a constructor
                _ = _privyInstance.InitializeAsync(); // Start async initialization without awaiting
            }

            return _privyInstance; // Return the instance immediately
        }

        [Obsolete("Use privy.GetAuthState() instead, which handles awaiting ready under the hood.")]
        public static async Task AwaitReady()
        {
            //Accesses _privyInstance, which is static
            if (_privyInstance != null)
            {
                await _privyInstance.InitializationTask; // Await the completion of the initialization task
            }
            else
            {
                throw new InvalidOperationException("Privy has not been initialized.");
            }
        }
    }
}