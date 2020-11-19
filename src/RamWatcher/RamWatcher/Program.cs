using System;
using System.ServiceProcess;

namespace Jlits.RamWatcher
{
    /// <summary>
    ///     Class Program.
    /// </summary>
    internal static class Program
    {
        #region Methods

        /// <summary>
        ///     Defines the entry point of the application.
        /// </summary>
        private static void Main(string[] args)
        {
            var service = new WatcherService();

            if (Environment.UserInteractive)
                service.StartInteractive(args);
            else
                ServiceBase.Run(service);
        }

        #endregion
    }
}