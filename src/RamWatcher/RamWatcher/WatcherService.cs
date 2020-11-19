using System;
using System.Diagnostics;
using System.Management;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace Jlits.RamWatcher
{
    /// <summary>
    ///     Class WatcherService.
    ///     Implements the <see cref="System.ServiceProcess.ServiceBase" />
    /// </summary>
    /// <seealso cref="System.ServiceProcess.ServiceBase" />
    internal class WatcherService : ServiceBase
    {
        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="Program" /> class.
        /// </summary>
        public WatcherService()
        {
            CanStop = true;
        }

        #endregion

        #region Fields

        private CancellationTokenSource _cts;
        private Task _task;

        #endregion

        #region Methods

        /// <summary>
        ///     Starts the interactive.
        /// </summary>
        /// <param name="args">The arguments.</param>
        internal void StartInteractive(string[] args)
        {
            OnStart(args);
            _task.Wait();
        }

        #endregion

        #region Overrides of ServiceBase

        /// <inheritdoc />
        protected override void OnStart(string[] args)
        {
            if (_task != null)
                return;

            _cts = new CancellationTokenSource();
            _task = Task.Run(RunTask, _cts.Token);

            base.OnStart(args);
        }

        /// <summary>
        ///     Runs the task.
        /// </summary>
        /// <exception cref="OperationCanceledException"></exception>
        private async Task RunTask()
        {
            while (true)
            {
                if (_cts.IsCancellationRequested)
                    throw new OperationCanceledException();

                MeasureRam();

                await Task.Delay(1_000);
            }
        }

        /// <summary>
        ///     Measures the RAM.
        /// </summary>
        private static void MeasureRam()
        {
            using var committedBytesInUsePercentCounter = new PerformanceCounter("Memory", "% Committed Bytes In Use");
            var committedBytesInUsePercent = committedBytesInUsePercentCounter.NextValue();

            using var committedBytesCounter = new PerformanceCounter("Memory", "Committed Bytes");
            var committedGigaBytes = committedBytesCounter.NextValue() / Math.Pow(1024, 3);

            using var commitLimitCounter = new PerformanceCounter("Memory", "Commit Limit");
            var commitLimitGigaBytes = commitLimitCounter.NextValue() / Math.Pow(1024, 3);

            Console.WriteLine(
                $"Virtual (physical + page file) Bytes In Use: {committedBytesInUsePercent:0.0} %, i.e. {committedGigaBytes:0.0} GB of {commitLimitGigaBytes:0.0} GB");

            using var availableBytesCounter = new PerformanceCounter("Memory", "Available Bytes");
            var availableGigaBytes = availableBytesCounter.NextValue() / Math.Pow(1024, 3);

            ulong totalPhysicalMemory = 0;
            var query = new SelectQuery("select * from Win32_ComputerSystem");
            using var searcher = new ManagementObjectSearcher(query);
            foreach (var obj in searcher.Get())
            {
                totalPhysicalMemory = (ulong) obj["TotalPhysicalMemory"];
                break;
            }

            Console.WriteLine(
                $"\t'Physical' Bytes Available: {availableGigaBytes:0.0} GB (of installed {totalPhysicalMemory / Math.Pow(1024, 3):0.0} GB)");
        }

        /// <inheritdoc />
        protected override void OnStop()
        {
            _cts.Cancel();
            _task.Wait(5_000);

            _task = null;

            base.OnStop();
        }

        #endregion
    }
}