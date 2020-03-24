﻿using System;
using System.Threading;
using StackExchange.Profiling;
using StackExchange.Profiling.Internal;
using Umbraco.Core.Cache;

namespace Umbraco.Web.Common.Runtime.Profiler
{
    public class WebProfilerProvider : DefaultProfilerProvider
    {
        private readonly ReaderWriterLockSlim _locker = new ReaderWriterLockSlim();
        private readonly IRequestCache _requestCache;
        private volatile BootPhase _bootPhase;
        private int _first;
        private MiniProfiler _startupProfiler;

        public WebProfilerProvider(IRequestCache requestCache)
        {
            _requestCache = requestCache;
            // booting...
            _bootPhase = BootPhase.Boot;
        }


        /// <summary>
        ///     Gets the current profiler.
        /// </summary>
        /// <remarks>
        ///     If the boot phase is not Booted, then this will return the startup profiler (this), otherwise
        ///     returns the base class
        /// </remarks>
        public override MiniProfiler CurrentProfiler
        {
            get
            {
                // if not booting then just use base (fast)
                // no lock, _bootPhase is volatile
                if (_bootPhase == BootPhase.Booted)
                    return base.CurrentProfiler;

                // else
                try
                {
                    var current = base.CurrentProfiler;
                    return current ?? _startupProfiler;
                }
                catch
                {
                    return _startupProfiler;
                }
            }
        }

        public void BeginBootRequest()
        {
            _locker.EnterWriteLock();
            try
            {
                if (_bootPhase != BootPhase.Boot)
                    throw new InvalidOperationException("Invalid boot phase.");
                _bootPhase = BootPhase.BootRequest;

                //TODO is this necessary? :mini-profiler: seems like a magic string
                // assign the profiler to be the current MiniProfiler for the request
                // is's already active, starting and all
                _requestCache.Set(":mini-profiler:", _startupProfiler);
            }
            finally
            {
                _locker.ExitWriteLock();
            }
        }

        public void EndBootRequest()
        {
            _locker.EnterWriteLock();
            try
            {
                if (_bootPhase != BootPhase.BootRequest)
                    throw new InvalidOperationException("Invalid boot phase.");
                _bootPhase = BootPhase.Booted;

                _startupProfiler = null;
            }
            finally
            {
                _locker.ExitWriteLock();
            }
        }

        /// <summary>
        ///     Starts a new MiniProfiler.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This is called when WebProfiler calls MiniProfiler.Start() so,
        ///         - as a result of WebRuntime starting the WebProfiler, and
        ///         - assuming profiling is enabled, on every BeginRequest that should be profiled,
        ///         - except for the very first one which is the boot request.
        ///     </para>
        /// </remarks>
        public override MiniProfiler Start(string profilerName, MiniProfilerBaseOptions options)
        {
            var first = Interlocked.Exchange(ref _first, 1) == 0;
            if (first == false) return base.Start(profilerName, options);

            _startupProfiler = new MiniProfiler("StartupProfiler", options);
            CurrentProfiler = _startupProfiler;
            return _startupProfiler;
        }

        /// <summary>
        ///     Indicates the boot phase.
        /// </summary>
        private enum BootPhase
        {
            Boot = 0, // boot phase (before the 1st request even begins)
            BootRequest = 1, // request boot phase (during the 1st request)
            Booted = 2 // done booting
        }
    }
}
