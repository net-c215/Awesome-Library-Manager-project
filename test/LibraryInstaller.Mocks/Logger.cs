﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using LibraryInstaller.Contracts;
using System;
using System.Collections.Generic;

namespace LibraryInstaller.Mocks
{
    /// <summary>
    /// A mock of the <see cref="ILogger"/> interface.
    /// </summary>
    /// <seealso cref="LibraryInstaller.Contracts.ILogger" />
    public class Logger : ILogger
    {
        /// <summary>
        /// A list of all the log messages recorded by the <see cref="Log"/> method.
        /// </summary>
        public List<Tuple<string, LogLevel>> Messages = new List<Tuple<string, LogLevel>>();

        /// <summary>
        /// Logs the specified message to the host.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="level">The level of the message.</param>
        public void Log(string message, LogLevel level)
        {
            Messages.Add(Tuple.Create(message, level));
        }
    }
}
